using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using RazorClassBlog.EnumsandConstants;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Areas.BlogAdmin;

[Authorize(Policy = "BlogAdmin")]
public class EditModel : PageModel
{
  private readonly IBlogService _blogService;
  private readonly IBlogRepository _blogRepository;
  private readonly BlogOptions _options;

  public EditModel(IBlogService blogService, IBlogRepository blogRepository, IOptions<BlogOptions> options)
  {
    _blogService = blogService;
    _blogRepository = blogRepository;
    _options = options.Value;
  }

  [BindProperty]
  public BlogPost Post { get; set; } = new();

  [BindProperty]
  public bool Publish { get; set; }

  [BindProperty]
  public string TagsCsv { get; set; } = string.Empty;

  public async Task<IActionResult> OnGetAsync(string? id, CancellationToken ct)
  {
    if (!string.IsNullOrEmpty(id))
    {
      var existing = await _blogRepository.GetByIdAsync(_options.BlogKey, id, ct);
      if (existing == null)
        return NotFound();

      Post = existing;
      Publish = Post.Status == BlogPostStatus.Published;
      TagsCsv = Post.Tags is { Count: > 0 }
          ? string.Join(", ", Post.Tags)
          : string.Empty;
    }
    else
    {
      Post = new BlogPost
      {
        BlogKey = _options.BlogKey,
        CreatedUtc = DateTimeOffset.UtcNow,
        Status = BlogPostStatus.Draft
      };
      Publish = false;

      if(!string.IsNullOrEmpty(_options.DefaultOrganizationName))
        Post.AuthorName = _options.DefaultOrganizationName;

      // Default author name from logged-in user, but allow override in UI
      else  if (User?.Identity?.IsAuthenticated == true)
        Post.AuthorName = User.Identity!.Name;
    }

    return Page();
  }

  public async Task<IActionResult> OnPostAsync(CancellationToken ct)
  {
    if (!ModelState.IsValid)
      return Page();

    Post.BlogKey ??= _options.BlogKey;

    Post.Tags = string.IsNullOrWhiteSpace(TagsCsv)
        ? new List<string>()
        : TagsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    // Ensure CreatedUtc is set for new posts before we validate
    if (string.IsNullOrEmpty(Post.Id))
    {
      if (Post.CreatedUtc == default)
        Post.CreatedUtc = DateTimeOffset.UtcNow;
    }

    // If publishing and we have a publish time, ensure it isn't before creation
    if (Publish && Post.PublishedUtc.HasValue)
    {
      if (Post.PublishedUtc.Value < Post.CreatedUtc)
      {
        ModelState.AddModelError("Post.PublishedUtc", "Publish date cannot be earlier than the created date.");

        // Re-show page with validation error
        return Page();
      }
    }

    string? authorId = User?.Identity?.IsAuthenticated == true ? User.Identity!.Name : null;

    var saved = await _blogService.CreateOrUpdatePostAsync(Post, publish: Publish, authorId: authorId, ct);

    return RedirectToPage("Edit", new { id = saved.Id });
  }

  public async Task<IActionResult> OnPostAutosaveAsync(CancellationToken ct)
  {
    Post.BlogKey ??= _options.BlogKey;

    Post.Tags = string.IsNullOrWhiteSpace(TagsCsv)
        ? new List<string>()
        : TagsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    // For new posts, make sure CreatedUtc is set
    if (string.IsNullOrEmpty(Post.Id) && Post.CreatedUtc == default)
      Post.CreatedUtc = DateTimeOffset.UtcNow;

    // IMPORTANT: do NOT touch Post.Status or Post.PublishedUtc here.
    // Autosave should only persist what the form currently has.
    // Publishing is controlled by the main Save (OnPostAsync) and the Publish toggle.

    string? authorId = User?.Identity?.IsAuthenticated == true ? User.Identity!.Name : null;

    var saved = await _blogService.CreateOrUpdatePostAsync(Post, publish: false, authorId: authorId, ct);

    var payload = new
    {
      id = saved.Id,
      updatedUtc = saved.UpdatedUtc?.UtcDateTime.ToString("O")
    };

    return new JsonResult(payload);
  }

}