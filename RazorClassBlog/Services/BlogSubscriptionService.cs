using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RazorClassBlog.EnumsandConstants;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Services;

public class BlogSubscriptionService : IBlogSubscriptionService
{
  private readonly IBlogDbContext _db;
  private readonly BlogOptions _options;

  public BlogSubscriptionService(
      IBlogDbContext db,
      IOptions<BlogOptions> options)
  {
    _db = db;
    _options = options.Value;
  }

  public async Task RequestSubscriptionAsync(string email, CancellationToken ct = default)
  {
    email = email.Trim();
    if (!new EmailAddressAttribute().IsValid(email))
      throw new ArgumentException("A valid email address is required.", nameof(email));

    var sendEmail = _options.SendEmailAsync
        ?? throw new InvalidOperationException("Configure BlogOptions.SendEmailAsync to enable subscriptions.");
    var normalizedEmail = email.ToUpperInvariant();
    var subscriber = await _db.BlogSubscribers.FirstOrDefaultAsync(
        s => s.BlogKey == _options.BlogKey && s.NormalizedEmail == normalizedEmail, ct);

    if (subscriber is { IsConfirmed: true, IsActive: true })
      return;

    var isNew = subscriber is null;
    var token = CreateToken();
    var confirmationUrl = BuildUrl("/SubscribeConfirm", token);
    subscriber ??= new BlogSubscriber
    {
      Id = HashToken($"{_options.BlogKey}:{normalizedEmail}"),
      BlogKey = _options.BlogKey
    };
    subscriber.Email = email;
    subscriber.NormalizedEmail = normalizedEmail;
    subscriber.ConfirmationTokenHash = HashToken(token);
    subscriber.IsConfirmed = false;
    subscriber.IsActive = false;

    if (isNew)
      _db.BlogSubscribers.Add(subscriber);
    await _db.SaveChangesAsync(ct);

    var template = await GetEmailTemplateAsync(ct);
    var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["[Email]"] = email,
      ["[ConfirmationMessage]"] = _options.SubscriptionConfirmationMessage ?? string.Empty,
      ["[ConfirmationUrl]"] = confirmationUrl
    };
    var subject = RenderTemplate(template.ConfirmationSubject ?? string.Empty, replacements, encodeHtml: false);
    var body = RenderTemplate(template.ConfirmationHtml ?? string.Empty, replacements, encodeHtml: true);
    await sendEmail(email, CleanSubject(subject), body, ct);
  }

  public async Task ConfirmSubscriptionAsync(string token, CancellationToken ct = default)
  {
    var tokenHash = HashToken(token);
    var subscriber = await _db.BlogSubscribers.FirstOrDefaultAsync(
        s => s.BlogKey == _options.BlogKey && s.ConfirmationTokenHash == tokenHash, ct);
    if (subscriber is null)
      return;

    subscriber.ConfirmationTokenHash = null;
    subscriber.IsConfirmed = true;
    subscriber.IsActive = true;
    await _db.SaveChangesAsync(ct);
  }

  public async Task<bool> IsUnsubscribeTokenValidAsync(string token, CancellationToken ct = default)
  {
    var tokenHash = HashToken(token);
    return (await _db.BlogPostDeliveries.CountAsync(
        d => d.BlogKey == _options.BlogKey && d.UnsubscribeTokenHash == tokenHash, ct)) > 0;
  }

  public async Task UnsubscribeAsync(string token, CancellationToken ct = default)
  {
    var tokenHash = HashToken(token);
    var delivery = await _db.BlogPostDeliveries.FirstOrDefaultAsync(
        d => d.BlogKey == _options.BlogKey && d.UnsubscribeTokenHash == tokenHash, ct);
    if (delivery is null)
      return;

    var subscriber = await _db.BlogSubscribers.FirstOrDefaultAsync(
        s => s.BlogKey == _options.BlogKey && s.Id == delivery.SubscriberId, ct);
    if (subscriber is null || !subscriber.IsActive)
      return;

    subscriber.IsActive = false;
    await _db.SaveChangesAsync(ct);
  }

  public async Task<IReadOnlyList<string>> GetSubscribedEmailsAsync(CancellationToken ct = default)
  {
    return await _db.BlogSubscribers.AsNoTracking()
        .Where(s => s.BlogKey == _options.BlogKey && s.IsConfirmed && s.IsActive)
        .Select(s => s.Email)
        .ToListAsync(ct);
  }

  public async Task SendPostToSubscribersAsync(BlogPost post, CancellationToken ct = default)
  {
    if (post.Status != BlogPostStatus.Published ||
        post.DeletedUtc is not null ||
        post.PublishedUtc is not { } publishedUtc ||
        publishedUtc > DateTimeOffset.UtcNow)
      return;

    await SendPostToSubscribersCoreAsync(post, ct);
  }

  public async Task SendDuePostNotificationsAsync(DateTimeOffset throughUtc, CancellationToken ct = default)
  {
    if (_options.SendEmailAsync is null)
      return;

    var posts = await _db.BlogPosts.AsNoTracking()
        .Where(p => p.BlogKey == _options.BlogKey &&
                    p.Status == BlogPostStatus.Published &&
                    p.PublishedUtc != null &&
                    p.PublishedUtc <= throughUtc &&
                    p.DeletedUtc == null)
        .ToListAsync(ct);

    foreach (var post in posts)
      await SendPostToSubscribersCoreAsync(post, ct);
  }

  public async Task<BlogEmailTemplate> GetEmailTemplateAsync(CancellationToken ct = default)
  {
    return await _db.BlogEmailTemplates.AsNoTracking()
        .FirstOrDefaultAsync(t => t.BlogKey == _options.BlogKey, ct)
        ?? new BlogEmailTemplate
        {
          Id = HashToken(_options.BlogKey),
          BlogKey = _options.BlogKey
        };
  }

  public async Task SaveEmailTemplateAsync(BlogEmailTemplate template, CancellationToken ct = default)
  {
    var stored = await _db.BlogEmailTemplates.FirstOrDefaultAsync(
        t => t.BlogKey == _options.BlogKey, ct);
    if (stored is null)
    {
      stored = new BlogEmailTemplate
      {
        Id = HashToken(_options.BlogKey),
        BlogKey = _options.BlogKey
      };
      _db.BlogEmailTemplates.Add(stored);
    }

    stored.ConfirmationSubject = template.ConfirmationSubject ?? string.Empty;
    stored.ConfirmationHtml = template.ConfirmationHtml ?? string.Empty;
    stored.PostSubject = template.PostSubject ?? string.Empty;
    stored.PostHtml = template.PostHtml ?? string.Empty;
    stored.UpdatedUtc = DateTimeOffset.UtcNow;
    await _db.SaveChangesAsync(ct);
  }

  private async Task SendPostToSubscribersCoreAsync(BlogPost post, CancellationToken ct)
  {
    var sendEmail = _options.SendEmailAsync;
    if (sendEmail is null)
      return;

    var publishedUtc = post.PublishedUtc ?? post.CreatedUtc;
    var subscribers = await _db.BlogSubscribers
        .Where(s => s.BlogKey == post.BlogKey &&
                    s.IsConfirmed &&
                    s.IsActive &&
                    s.CreatedUtc < publishedUtc)
        .ToListAsync(ct);
    if (subscribers.Count == 0)
      return;

    var postUrl = BuildPostUrl(post);
    var template = await GetEmailTemplateAsync(ct);

    foreach (var subscriber in subscribers)
    {
      //switched to countasync to support db systems like cosmosdb that don't support .AnyAsync
      var alreadySent = await _db.BlogPostDeliveries.CountAsync(
          d => d.BlogKey == post.BlogKey && d.SubscriberId == subscriber.Id && d.PostId == post.Id, ct);
      if (alreadySent > 0)
        continue;

      var token = CreateToken();
      var unsubscribeUrl = BuildUrl("/Unsubscribe", token);
      var replacements = GetPostReplacements(post, subscriber.Email, postUrl, unsubscribeUrl);
      var subject = RenderTemplate(template.PostSubject ?? string.Empty, replacements, encodeHtml: false);
      var body = RenderTemplate(template.PostHtml ?? string.Empty, replacements, encodeHtml: true);

      await sendEmail(subscriber.Email, CleanSubject(subject), body, ct);
      _db.BlogPostDeliveries.Add(new BlogPostDelivery
      {
        Id = HashToken($"{subscriber.Id}:{post.Id}"),
        BlogKey = post.BlogKey,
        SubscriberId = subscriber.Id,
        PostId = post.Id,
        UnsubscribeTokenHash = HashToken(token),
        SentUtc = DateTimeOffset.UtcNow
      });
      await _db.SaveChangesAsync(ct);
    }
  }

  private static Dictionary<string, string> GetPostReplacements(
      BlogPost post,
      string email,
      string postUrl,
      string unsubscribeUrl)
  {
    return new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["[Email]"] = email,
      ["[PostUrl]"] = postUrl,
      ["[UnsubscribeUrl]"] = unsubscribeUrl,
      ["[Id]"] = post.Id,
      ["[BlogKey]"] = post.BlogKey,
      ["[Title]"] = post.Title,
      ["[Slug]"] = post.Slug ?? string.Empty,
      ["[Summary]"] = post.Summary ?? string.Empty,
      ["[HeroImageUrl]"] = post.HeroImageUrl ?? string.Empty,
      ["[Tags]"] = string.Join(", ", post.Tags),
      ["[Status]"] = post.Status.ToString(),
      ["[CreatedUtc]"] = post.CreatedUtc.ToString("O", CultureInfo.InvariantCulture),
      ["[PublishedUtc]"] = post.PublishedUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
      ["[UpdatedUtc]"] = post.UpdatedUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
      ["[AuthorId]"] = post.AuthorId ?? string.Empty,
      ["[AuthorName]"] = post.AuthorName ?? string.Empty,
      ["[DeletedUtc]"] = post.DeletedUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
      ["[DeletedBy]"] = post.DeletedBy ?? string.Empty,
      ["[Content]"] = post.Content,
      ["[RelatedPostIds]"] = string.Join(", ", post.RelatedPostIds)
    };
  }

  private static string RenderTemplate(
      string template,
      IReadOnlyDictionary<string, string> replacements,
      bool encodeHtml)
  {
    return Regex.Replace(template, @"\[[A-Za-z]+\]", match =>
        replacements.TryGetValue(match.Value, out var value)
            ? encodeHtml ? HtmlEncoder.Default.Encode(value) : value
            : match.Value);
  }

  private static string CleanSubject(string subject) =>
      subject.Replace("\r", string.Empty, StringComparison.Ordinal)
          .Replace("\n", string.Empty, StringComparison.Ordinal).Trim();

  private string BuildPostUrl(BlogPost post)
  {
    var prefix = "/" + (_options.PublicRoutePrefix ?? "/blog").Trim('/');
    if (prefix == "/")
      prefix = "/blog";
    var published = post.PublishedUtc ?? post.CreatedUtc;
    var path = $"{prefix}/{published.Year:0000}/{published.Month:00}/{Uri.EscapeDataString(post.Slug ?? string.Empty)}";
    return BuildUrl(path);
  }

  private string BuildUrl(string path, string? token = null)
  {
    var baseUrl = _options.PublicBaseUrl?.TrimEnd('/');
    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ||
        (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps) ||
        !string.IsNullOrEmpty(baseUri.Query) ||
        !string.IsNullOrEmpty(baseUri.Fragment))
      throw new InvalidOperationException("BlogOptions.PublicBaseUrl must be an absolute HTTP or HTTPS URL.");

    var url = baseUri.ToString().TrimEnd('/') + "/" + path.TrimStart('/');
    return token is null ? url : QueryHelpers.AddQueryString(url, "token", token);
  }

  private static string CreateToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

  private static string HashToken(string token) =>
      Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
