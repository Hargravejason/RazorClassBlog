using Microsoft.EntityFrameworkCore;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.EntityFramework;

public class CosmosBlogDbContext : DbContext, IBlogDbContext
{
  public CosmosBlogDbContext(DbContextOptions<CosmosBlogDbContext> options) : base(options)
  {
  }

  public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    var blog = modelBuilder.Entity<BlogPost>();

    blog.ToContainer("BlogPosts");          // Cosmos-specific
    blog.HasPartitionKey(p => p.BlogKey);   // choose your partition key
    blog.HasNoDiscriminator();
  }
}