using RazorClassBlog.Abstractions;
using RazorClassBlog.Models;

namespace RazorClassBlog.Interfaces;

public interface IBlogRepository
{
  Task<BlogPost?> GetByIdAsync(string blogKey, string id, CancellationToken ct = default);

  Task<BlogPost?> GetBySlugAsync(string blogKey, string slug, bool includeUnpublished = false, CancellationToken ct = default);

  Task<PagedResult<BlogPostMini>> QueryAsync(BlogQuery query, CancellationToken ct = default);

  Task<BlogPost> SaveAsync(BlogPost post, CancellationToken ct = default);

  Task DeleteAsync(string blogKey, string id, string user, CancellationToken ct = default);

  Task<IReadOnlyList<BlogPost>> GetPublishedAfterAsync(string blogKey, DateTimeOffset utcNow, DateTimeOffset? lastPublishedUtc, string? lastId, int take, CancellationToken ct);
}