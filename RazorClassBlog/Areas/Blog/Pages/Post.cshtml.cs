using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Areas.Blog.Pages;

public class PostModel : PageModel
{
  private readonly IBlogService _blogService;
  private readonly BlogOptions _options;

  public PostModel(IBlogService blogService, IOptions<BlogOptions> options)
  {
    _blogService = blogService;
    _options = options.Value;
  }

  public BlogPost? Post { get; private set; }

  public async Task<IActionResult> OnGetAsync(int year, int month, string slug, CancellationToken ct)
  {
    var blogKey = _options.BlogKey;

    var post = await _blogService.GetPublicPostBySlugAsync(blogKey, slug, ct);
    if (post == null)
    {
      return NotFound();
    }

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

    Post = post;
    return Page();
  }
}