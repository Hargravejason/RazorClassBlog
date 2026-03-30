using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
  private readonly IBlobStorageService _blobStorage;
  private readonly BlogOptions _options;

  public EditModel(IBlogService blogService, IBlobStorageService blobStorage, IOptions<BlogOptions> options)
  {
    _blogService = blogService;
    _blobStorage = blobStorage;
    _options = options.Value;
  }

  /// <summary>
  /// Set to true when blob storage is configured so the view can show the upload UI.
  /// </summary>
  public bool ImageUploadEnabled => _blobStorage.IsConfigured;

  /// <summary>
  /// Images previously uploaded for the current post.
  /// Populated on GET (when editing an existing post) and after a successful upload.
  /// </summary>
  public IReadOnlyList<string> UploadedImages { get; private set; } = Array.Empty<string>();

  /// <summary>
  /// File selected by the user for upload. Not bound on GET.
  /// </summary>
  [BindProperty]
  public IFormFile? UploadImage { get; set; }

  [BindProperty]
  public BlogPost Post { get; set; } = new();

  [BindProperty]
  public bool Publish { get; set; }

  [BindProperty]
  public string TagsCsv { get; set; } = string.Empty;

  /// <summary>
  /// Published posts available for selection as related articles, excluding the current post.
  /// Populated on GET only (not bound on POST).
  /// </summary>
  public IReadOnlyList<BlogPostMini> AvailablePostsForRelated { get; private set; } = Array.Empty<BlogPostMini>();

  public async Task<IActionResult> OnGetAsync(string? id, CancellationToken ct)
  {
    if (!string.IsNullOrEmpty(id))
    {
      var existing = await _blogService.GetPostByIdAsync(_options.BlogKey, id, ct);
      if (existing == null)
        return NotFound();

      Post = existing;
      Publish = Post.Status == BlogPostStatus.Published;
      TagsCsv = Post.Tags is { Count: > 0 }
          ? string.Join(", ", Post.Tags)
          : string.Empty;

      if (_blobStorage.IsConfigured)
        UploadedImages = await _blobStorage.ListImagesAsync(id, ct);
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

    // Load published posts for the related articles selection UI (exclude current post)
    await LoadAvailablePostsForRelatedAsync(ct);

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

    Post.RelatedPostIds ??= new List<string>();

    // Ensure CreatedUtc is set for new posts before we validate
    if (string.IsNullOrEmpty(Post.Id))
    {
      if (Post.CreatedUtc == default)
        Post.CreatedUtc = DateTimeOffset.UtcNow;
    }

    // If publishing and we have a publish time, ensure it isn't before creation
    if (Publish && Post.PublishedUtc.HasValue)
    {
      if (Post.PublishedUtc.Value.ToUniversalTime() < Post.CreatedUtc)
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

    Post.RelatedPostIds ??= new List<string>();

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

  /// <summary>
  /// Handles image file upload. Requires that the post already exists (has an Id).
  /// After upload, reloads the post and image list then re-renders the Edit page.
  /// </summary>
  public async Task<IActionResult> OnPostUploadImageAsync(CancellationToken ct)
  {
    if (!_blobStorage.IsConfigured)
      return BadRequest("Image upload is not configured.");

    if (string.IsNullOrEmpty(Post.Id))
    {
      ModelState.AddModelError(string.Empty, "Save the post before uploading images.");
      return Page();
    }

    if (UploadImage is null || UploadImage.Length == 0)
    {
      ModelState.AddModelError(string.Empty, "Please select a file to upload.");
      // Reload images so the list stays visible
      UploadedImages = await _blobStorage.ListImagesAsync(Post.Id, ct);

      // Reload post to keep form state
      var existing = await _blogService.GetPostByIdAsync(_options.BlogKey, Post.Id, ct);
      if (existing != null)
      {
        Post = existing;
        Publish = Post.Status == BlogPostStatus.Published;
        TagsCsv = Post.Tags is { Count: > 0 } ? string.Join(", ", Post.Tags) : string.Empty;
      }

      await LoadAvailablePostsForRelatedAsync(ct);

      return Page();
    }

    using var stream = UploadImage.OpenReadStream();
    await _blobStorage.UploadAsync(Post.Id, UploadImage.FileName, UploadImage.ContentType, stream, ct);

    // Redirect to GET so the full image list is refreshed cleanly (PRG pattern)
    return RedirectToPage("Edit", new { id = Post.Id });
  }

  /// <summary>
  /// Deletes a single uploaded image by its blob URL. Redirects back to the Edit page (PRG).
  /// </summary>
  public async Task<IActionResult> OnPostDeleteImageAsync([FromForm] string blobUrl, CancellationToken ct)
  {
    if (!_blobStorage.IsConfigured)
      return BadRequest("Image upload is not configured.");

    if (!string.IsNullOrWhiteSpace(blobUrl))
      await _blobStorage.DeleteImageAsync(blobUrl, ct);

    return RedirectToPage("Edit", new { id = Post.Id });
  }

  private async Task LoadAvailablePostsForRelatedAsync(CancellationToken ct)
  {
    var allPublished = await _blogService.GetAllPublishedPostsAsync(_options.BlogKey, ct);
    AvailablePostsForRelated = allPublished.Where(p => p.Id != Post.Id).ToList();
  }

}