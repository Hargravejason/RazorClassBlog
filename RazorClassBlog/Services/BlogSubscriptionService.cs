using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

    var message = HtmlEncoder.Default.Encode(_options.SubscriptionConfirmationMessage ?? string.Empty)
        .Replace("\r\n", "<br>", StringComparison.Ordinal)
        .Replace("\n", "<br>", StringComparison.Ordinal);
    var body = $"<p>{message}</p><p><a href=\"{HtmlEncoder.Default.Encode(confirmationUrl)}\">Confirm subscription</a></p>";
    await sendEmail(email, "Confirm your blog subscription", body, ct);
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
    return await _db.BlogPostDeliveries.AnyAsync(
        d => d.BlogKey == _options.BlogKey && d.UnsubscribeTokenHash == tokenHash, ct);
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
    var sendEmail = _options.SendEmailAsync;
    if (sendEmail is null)
      return;

    var subscribers = await _db.BlogSubscribers
        .Where(s => s.BlogKey == post.BlogKey && s.IsConfirmed && s.IsActive)
        .ToListAsync(ct);
    if (subscribers.Count == 0)
      return;

    var postUrl = BuildPostUrl(post);

    foreach (var subscriber in subscribers)
    {
      var alreadySent = await _db.BlogPostDeliveries.AnyAsync(
          d => d.BlogKey == post.BlogKey && d.SubscriberId == subscriber.Id && d.PostId == post.Id, ct);
      if (alreadySent)
        continue;

      var token = CreateToken();
      var unsubscribeUrl = BuildUrl("/Unsubscribe", token);
      var title = HtmlEncoder.Default.Encode(post.Title);
      var summary = HtmlEncoder.Default.Encode(post.Summary ?? string.Empty);
      var body = $"<h1>{title}</h1><p>{summary}</p><p><a href=\"{HtmlEncoder.Default.Encode(postUrl)}\">Read the post</a></p><p><a href=\"{HtmlEncoder.Default.Encode(unsubscribeUrl)}\">Unsubscribe</a></p>";
      var subjectTitle = post.Title.Replace("\r", string.Empty, StringComparison.Ordinal)
          .Replace("\n", string.Empty, StringComparison.Ordinal);

      await sendEmail(subscriber.Email, $"New blog post: {subjectTitle}", body, ct);
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
