using Microsoft.Extensions.Options;
using RazorClassBlog.Abstractions;
using RazorClassBlog.EnumsandConstants;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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

  public Task<PagedResult<BlogPostMini>> GetPublicPostsAsync(BlogQuery query, CancellationToken ct = default)
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

  public Task<BlogPost?> GetPostByIdAsync(string blogKey, string id, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(blogKey))
      blogKey = _options.BlogKey;

    return _repository.GetByIdAsync(blogKey, id, ct);
  }

  public async Task<BlogPost> CreateOrUpdatePostAsync(BlogPost post, bool publish, string? authorId, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(post.BlogKey))
      post.BlogKey = _options.BlogKey;

    if (string.IsNullOrWhiteSpace(post.Slug))
      post.Slug = SlugGenerator.GenerateSlug(post.Title);

    var now = DateTimeOffset.UtcNow;

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

  public async IAsyncEnumerable<PublicSitemapItem> GetPublicSitemapItemsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
  {
    var now = DateTimeOffset.UtcNow;

    // normalize prefix: "/property-insights" (no trailing slash)
    var prefix = NormalizePrefix(_options.PublicRoutePrefix);

    // stream in batches; keep it provider-friendly
    const int take = 250;
    DateTimeOffset? lastPublishedUtc = null;
    string? lastId = null;

    while (true)
    {
      var posts = await _repository.GetPublishedAfterAsync(blogKey: _options.BlogKey, utcNow: now, lastPublishedUtc: lastPublishedUtc, lastId: lastId, take: take, ct: ct);

      if (posts.Count == 0)
        yield break;

      foreach (var p in posts)
      {
        // You already enforce “public” in the repo query, but be defensive:
        if (p.PublishedUtc is null) continue;
        if (p.PublishedUtc.Value > now) continue;

        var pub = p.PublishedUtc.Value;

        // IMPORTANT: URLs use UTC year/month for stability
        var relativeUrl = $"{prefix}/{pub.Year:0000}/{pub.Month:00}/{p.Slug}";

        var lastMod = p.UpdatedUtc ?? p.CreatedUtc;

        yield return new PublicSitemapItem(relativeUrl, lastMod);

        lastPublishedUtc = pub;
        lastId = p.Id;
      }
    }

    static string NormalizePrefix(string? raw)
    {
      var p = (raw ?? "/blog").Trim();
      p = "/" + p.Trim('/');     // ensure exactly one leading slash
      return p == "/" ? "/blog" : p;
    }
  }
}