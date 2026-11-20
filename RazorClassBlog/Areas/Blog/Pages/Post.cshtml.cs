using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Areas.Blog.Pages;

public class PostModel : PageModel
{
  private readonly IBlogService _blogService;
  private readonly IBlogSubscriptionService _subscriptions;
  private readonly BlogOptions _options;

  public PostModel(
      IBlogService blogService,
      IBlogSubscriptionService subscriptions,
      IOptions<BlogOptions> options)
  {
    _blogService = blogService;
    _subscriptions = subscriptions;
    _options = options.Value;
  }

  public BlogPost? Post { get; private set; }

  public IReadOnlyList<BlogPostMini> RelatedPosts { get; private set; } = Array.Empty<BlogPostMini>();

  [BindProperty, Required, EmailAddress]
  public string SubscriptionEmail { get; set; } = string.Empty;

  [TempData]
  public string? SubscriptionMessage { get; set; }

  public bool CanSubscribe =>
      _options.SendEmailAsync is not null &&
      Uri.TryCreate(_options.PublicBaseUrl, UriKind.Absolute, out var baseUri) &&
      (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps) &&
      string.IsNullOrEmpty(baseUri.Query) &&
      string.IsNullOrEmpty(baseUri.Fragment);

  public async Task<IActionResult> OnGetAsync(int year, int month, string slug, CancellationToken ct)
  {
    var post = await GetPostAsync(slug, ct);
    if (post == null)
      return NotFound();

    var actualYear = (post.PublishedUtc ?? post.CreatedUtc).Year;
    var actualMonth = (post.PublishedUtc ?? post.CreatedUtc).Month;

    if (actualYear != year || actualMonth != month)
    {
      return RedirectToPage("Post", new
      {
        year = actualYear,
        month = actualMonth,
        slug = post.Slug
      });
    }

    await LoadRelatedPostsAsync(post, ct);
    return Page();
  }

  public async Task<IActionResult> OnPostSubscribeAsync(int year, int month, string slug, CancellationToken ct)
  {
    if (!CanSubscribe)
      return NotFound();

    if (!ModelState.IsValid)
    {
      var post = await GetPostAsync(slug, ct);
      if (post is null)
        return NotFound();
      await LoadRelatedPostsAsync(post, ct);
      return Page();
    }

    await _subscriptions.RequestSubscriptionAsync(SubscriptionEmail, ct);
    SubscriptionMessage = "If this address can be subscribed, a confirmation email will be sent.";
    return RedirectToPage(new { year, month, slug });
  }

  private Task<BlogPost?> GetPostAsync(string slug, CancellationToken ct)
  {
    return _blogService.GetPublicPostBySlugAsync(
        _options.BlogKey, slug, _options.AllowPreviewOfPublishedPosts, ct);
  }

  private async Task LoadRelatedPostsAsync(BlogPost post, CancellationToken ct)
  {
    Post = post;
    if (post.RelatedPostIds is { Count: > 0 })
      RelatedPosts = await _blogService.GetRelatedPostsAsync(_options.BlogKey, post.RelatedPostIds, ct);
  }
}