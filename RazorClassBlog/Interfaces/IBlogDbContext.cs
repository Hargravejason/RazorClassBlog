using Microsoft.EntityFrameworkCore;
using RazorClassBlog.Models;

namespace RazorClassBlog.Interfaces;

public interface IBlogDbContext: IDisposable
{
  DbSet<BlogPost> BlogPosts { get; }

  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
