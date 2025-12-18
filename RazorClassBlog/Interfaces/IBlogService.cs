using RazorClassBlog.Abstractions;
using RazorClassBlog.Models;

namespace RazorClassBlog.Interfaces;

public interface IBlogService
{
  Task<PagedResult<BlogPostMini>> GetPublicPostsAsync(BlogQuery query, CancellationToken ct = default);

  Task<BlogPost?> GetPublicPostBySlugAsync(string blogKey, string slug, CancellationToken ct = default);
  Task<BlogPost?> GetPostByIdAsync(string blogKey, string id, CancellationToken ct = default);
  Task<BlogPost> CreateOrUpdatePostAsync(BlogPost post, bool publish, string? authorId, CancellationToken ct = default);

  Task DeletePostAsync(string blogKey, string id, string userId, CancellationToken ct = default);

  IAsyncEnumerable<PublicSitemapItem> GetPublicSitemapItemsAsync(CancellationToken ct = default);
}