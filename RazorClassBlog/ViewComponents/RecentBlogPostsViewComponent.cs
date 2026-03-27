using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.ViewComponents;

public class RecentBlogPostsViewComponent : ViewComponent
{
  private const int MinCount = 1;
  private const int MaxCount = 20;

  private readonly IBlogService _blogService;
  private readonly BlogOptions _options;

  public RecentBlogPostsViewComponent(IBlogService blogService, IOptions<BlogOptions> options)
  {
    _blogService = blogService;
    _options = options.Value;
  }

  public async Task<IViewComponentResult> InvokeAsync(int? count = null, string? layout = null)
  {
    var resolvedCount = ResolveCount(count, _options.RecentPostsCount);
    var resolvedLayout = ResolveLayout(layout, _options.RecentPostsLayout);

    var query = new BlogQuery
    {
      BlogKey = _options.BlogKey,
      Page = 1,
      PageSize = resolvedCount,
      IncludeUnpublished = false,
      PublishedBeforeUtc = DateTimeOffset.UtcNow
    };

    var page = await _blogService.GetPublicPostsAsync(query, HttpContext.RequestAborted);

    var model = new RecentPostsWidgetModel
    {
      Posts = page.Items,
      Layout = resolvedLayout,
      PublicRoutePrefix = NormalizePrefix(_options.PublicRoutePrefix)
    };

    return View(model);
  }

  private static int ResolveCount(int? requestedCount, int defaultCount)
  {
    var value = requestedCount ?? defaultCount;
    if (value < MinCount) return MinCount;
    if (value > MaxCount) return MaxCount;
    return value;
  }

  private static string ResolveLayout(string? requestedLayout, string defaultLayout)
  {
    var value = string.IsNullOrWhiteSpace(requestedLayout) ? defaultLayout : requestedLayout;
    return string.Equals(value, "horizontal", StringComparison.OrdinalIgnoreCase)
      ? "horizontal"
      : "vertical";
  }

  private static string NormalizePrefix(string? raw)
  {
    var prefix = (raw ?? "/blog").Trim();
    prefix = "/" + prefix.Trim('/');
    return prefix == "/" ? "/blog" : prefix;
  }
}
