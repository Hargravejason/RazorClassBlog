using Microsoft.EntityFrameworkCore;
using RazorClassBlog.Abstractions;
using RazorClassBlog.EnumsandConstants;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Repositories;

public class BlogRepository : IBlogRepository
{
  private readonly IBlogDbContext _db;

  public BlogRepository(IBlogDbContext db)
  {
    _db = db;
  }

  public async Task<BlogPost?> GetByIdAsync(string blogKey, string id, CancellationToken ct = default) =>
    await _db.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.BlogKey == blogKey, ct);

  public async Task<BlogPost?> GetBySlugAsync(string blogKey, string slug, bool includeUnpublished = false, CancellationToken ct = default)
  {
    var query = _db.BlogPosts.AsNoTracking().Where(p => p.BlogKey == blogKey && p.Slug == slug);

    if (!includeUnpublished)
    {
      var now = DateTimeOffset.UtcNow;
      query = query.Where(p =>
          p.Status == BlogPostStatus.Published &&
          p.PublishedUtc <= now);
    }

    return await query.FirstOrDefaultAsync(ct);
  }

  public async Task<PagedResult<BlogPost>> QueryAsync(BlogQuery query, CancellationToken ct = default)
  {
    query.EnsureValid();

    var q = _db.BlogPosts.AsNoTracking()
        .Where(p => p.BlogKey == query.BlogKey);

    if (!query.IncludeUnpublished)
    {
      var now = query.PublishedBeforeUtc ?? DateTimeOffset.UtcNow;
      q = q.Where(p =>
          p.Status == BlogPostStatus.Published &&
          p.PublishedUtc <= now);
    }

    if (!string.IsNullOrWhiteSpace(query.Tag))
    {
      var tagLower = query.Tag.ToLowerInvariant();
      q = q.Where(p => p.Tags.Any(t => t.ToLower() == tagLower));
    }

    if (!string.IsNullOrWhiteSpace(query.SearchTerm))
    {
      var term = query.SearchTerm.Trim();
      q = q.Where(p =>
          p.Title.Contains(term) ||
          p.Content.Contains(term));
    }

    if (!query.IncludeUnpublished)
      q = q.OrderByDescending(p => p.PublishedUtc);
    else
      q = q.OrderByDescending(p => p.CreatedUtc);

    var totalCount = await q.CountAsync(ct);

    var skip = (query.Page - 1) * query.PageSize;

    var items = await q
        .Skip(skip)
        .Take(query.PageSize)
        .ToListAsync(ct);

    return new PagedResult<BlogPost>
    {
      Items = items,
      Page = query.Page,
      PageSize = query.PageSize,
      TotalCount = totalCount
    };
  }

  public async Task<BlogPost> SaveAsync(BlogPost post, CancellationToken ct = default)
  {
    // string Id: empty/null means new
    var existing = await _db.BlogPosts.FirstOrDefaultAsync(p => p.Id == post.Id && p.BlogKey == post.BlogKey, ct);

    if (existing == null)
    {
      existing = new BlogPost() { Id = post.Id, CreatedUtc = DateTime.UtcNow, BlogKey = post.BlogKey };
      _db.BlogPosts.Add(existing);
    }

    existing.AuthorId = post.AuthorId;
    existing.AuthorName = post.AuthorName;
    existing.UpdatedUtc = DateTime.UtcNow;
    existing.PublishedUtc = post.PublishedUtc;
    existing.Status = post.Status;
    existing.Title = post.Title;
    existing.Slug = post.Slug;
    existing.Content = post.Content;
    existing.Summary = post.Summary;
    existing.HeroImageUrl = post.HeroImageUrl;
    existing.Tags = post.Tags;

    await _db.SaveChangesAsync(ct);
    return post;
  }

  public async Task DeleteAsync(string blogKey, string id, string user, CancellationToken ct = default)
  {
    var entity = await _db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id && p.BlogKey == blogKey, ct);

    if (entity != null)
    {
      if(entity.Status == BlogPostStatus.Draft)
        _db.BlogPosts.Remove(entity);
      else
      {
        entity.DeletedUtc = DateTime.UtcNow;
        entity.DeletedBy = user;
      }
      await _db.SaveChangesAsync(ct);
    }
  }
}