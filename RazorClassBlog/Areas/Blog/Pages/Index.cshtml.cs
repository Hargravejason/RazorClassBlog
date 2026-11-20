using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using RazorClassBlog.Abstractions;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Areas.Blog.Pages;

public class IndexModel : PageModel
{
  private readonly IBlogService _blogService;
  private readonly IBlogSubscriptionService _subscriptions;
  private readonly BlogOptions _options;

  public IndexModel(IBlogService blogService, IBlogSubscriptionService subscriptions, IOptions<BlogOptions> options)
  {
    _blogService = blogService;
    _subscriptions = subscriptions;
    _options = options.Value;
  }

  public PagedResult<BlogPostMini> Posts { get; private set; } =
      new() { Items = Array.Empty<BlogPostMini>(), Page = 1, PageSize = 10, TotalCount = 0 };

  [FromQuery]
  public int PageNumber { get; set; } = 1;

  [FromQuery]
  public string? Tag { get; set; }

  [FromQuery]
  public string? Search { get; set; }

  [BindProperty]
  [Required, EmailAddress]
  public string Email { get; set; } = string.Empty;

  [TempData]
  public string? SubscriptionMessage { get; set; }

  public bool CanSubscribe =>
      _options.SendEmailAsync is not null &&
      Uri.TryCreate(_options.PublicBaseUrl, UriKind.Absolute, out var baseUri) &&
      (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps) &&
      string.IsNullOrEmpty(baseUri.Query) &&
      string.IsNullOrEmpty(baseUri.Fragment);

  public Task OnGetAsync(CancellationToken ct) => LoadPostsAsync(ct);

  public async Task<IActionResult> OnPostSubscribeAsync(CancellationToken ct)
  {
    if (!CanSubscribe)
      return NotFound();

    if (!ModelState.IsValid)
    {
      await LoadPostsAsync(ct);
      return Page();
    }

    await _subscriptions.RequestSubscriptionAsync(Email, ct);
    SubscriptionMessage = "If this address can be subscribed, a confirmation email will be sent.";
    return RedirectToPage();
  }

  private async Task LoadPostsAsync(CancellationToken ct)
  {
    var query = new BlogQuery
    {
      // let service default BlogKey, but we can set explicitly for clarity
      BlogKey = _options.BlogKey,
      Page = PageNumber,
      PageSize = _options.PublicPageSize,
      Tag = Tag,
      SearchTerm = Search,
      IncludeUnpublished = false
    };

    Posts = await _blogService.GetPublicPostsAsync(query, ct);
  }
}
