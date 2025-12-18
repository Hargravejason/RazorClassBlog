using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using RazorClassBlog.Abstractions;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Areas.Blog.Pages;

public class IndexModel : PageModel
{
  private readonly IBlogService _blogService;
  private readonly BlogOptions _options;

  public IndexModel(IBlogService blogService, IOptions<BlogOptions> options)
  {
    _blogService = blogService;
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

  public async Task OnGetAsync(CancellationToken ct)
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
