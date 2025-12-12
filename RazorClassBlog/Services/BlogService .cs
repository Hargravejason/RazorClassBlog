using Microsoft.Extensions.Options;
using RazorClassBlog.Abstractions;
using RazorClassBlog.EnumsandConstants;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Services;

public class BlogService : IBlogService
{
  private readonly IBlogRepository _repository;
  private readonly BlogOptions _options;

  public BlogService(IBlogRepository repository, IOptions<BlogOptions> options)
  {
    _repository = repository;
    _options = options.Value;
  }

  public Task<PagedResult<BlogPost>> GetPublicPostsAsync(BlogQuery query, CancellationToken ct = default)
  {
    // Apply defaults
    if (string.IsNullOrWhiteSpace(query.BlogKey))
      query.BlogKey = _options.BlogKey;

    if (query.PageSize <= 0)
      query.PageSize = _options.PublicPageSize;

    query.IncludeUnpublished = false;
    query.PublishedBeforeUtc ??= DateTimeOffset.UtcNow;
    query.EnsureValid();

    return _repository.QueryAsync(query, ct);
  }

  public Task<BlogPost?> GetPublicPostBySlugAsync(string blogKey, string slug, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(blogKey))
      blogKey = _options.BlogKey;

    return _repository.GetBySlugAsync(blogKey, slug, includeUnpublished: false, ct);
  }

  public async Task<BlogPost> CreateOrUpdatePostAsync(BlogPost post, bool publish, string? authorId, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(post.BlogKey))
      post.BlogKey = _options.BlogKey;

    if (string.IsNullOrWhiteSpace(post.Slug))
      post.Slug = SlugGenerator.GenerateSlug(post.Title);

    var now = DateTimeOffset.UtcNow;

    if (post.CreatedUtc == default)
      post.CreatedUtc = now;

    post.UpdatedUtc = now;

    if (publish)
    {
      post.Status = BlogPostStatus.Published;
      post.PublishedUtc ??= now;
    }

    // Only set AuthorId when it's not already set (first creation)
    if (string.IsNullOrWhiteSpace(post.AuthorId) && !string.IsNullOrWhiteSpace(authorId))
    {
      post.AuthorId = authorId;
    }

    return await _repository.SaveAsync(post, ct);
  }

  public Task DeletePostAsync(string blogKey, string id, string userId, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(blogKey))
      blogKey = _options.BlogKey;

    return _repository.DeleteAsync(blogKey, id, userId, ct);
  }
}