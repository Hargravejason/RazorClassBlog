using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using RazorClassBlog.Abstractions;
using RazorClassBlog.EnumsandConstants;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Areas.BlogAdmin;

[Authorize(Policy = "BlogAdmin")]
public class IndexModel : PageModel
{
  private readonly IBlogRepository _blogRepository;
  private readonly BlogOptions _options;

  public IndexModel(IBlogRepository blogRepository, IOptions<BlogOptions> options)
  {
    _blogRepository = blogRepository;
    _options = options.Value;
  }

  public PagedResult<BlogPost> Posts { get; private set; } =
      new() { Items = Array.Empty<BlogPost>(), Page = 1, PageSize = 20, TotalCount = 0 };

  [FromQuery]
  public int PageNumber { get; set; } = 1;

  [FromQuery]
  public string? Search { get; set; }

  [FromQuery]
  public BlogPostStatus? Status { get; set; }

  [FromQuery]
  public string? Tag { get; set; }

  public async Task OnGetAsync(CancellationToken ct)
  {
    var query = new BlogQuery
    {
      BlogKey = _options.BlogKey,
      Page = PageNumber,
      PageSize = _options.AdminPageSize,
      SearchTerm = Search,
      Tag = Tag,
      IncludeUnpublished = true
    };

    var result = await _blogRepository.QueryAsync(query, ct);

    if (Status.HasValue)
    {
      result = new PagedResult<BlogPost>
      {
        Items = result.Items.Where(p => p.Status == Status.Value).ToList(),
        Page = result.Page,
        PageSize = result.PageSize,
        TotalCount = result.TotalCount
      };
    }

    Posts = result;
  }

  public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken ct)
  {
    if (!string.IsNullOrEmpty(id))
    {
      string userId = User.Identity!.Name!;
      await _blogRepository.DeleteAsync(_options.BlogKey, id, userId, ct);
    }

    // Keep current filters/page when redirecting back
    return RedirectToPage(new
    {
      PageNumber,
      Search,
      Status,
      Tag
    });
  }

}