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
  public DbSet<BlogSubscriber> BlogSubscribers => Set<BlogSubscriber>();
  public DbSet<BlogPostDelivery> BlogPostDeliveries => Set<BlogPostDelivery>();
  public DbSet<BlogEmailTemplate> BlogEmailTemplates => Set<BlogEmailTemplate>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    var blog = modelBuilder.Entity<BlogPost>();

    blog.ToContainer("BlogPosts");          // Cosmos-specific
    blog.Property(p => p.PostPartitionKey);
    blog.HasPartitionKey(p => p.PostPartitionKey);
    blog.HasNoDiscriminator();

    var subscribers = modelBuilder.Entity<BlogSubscriber>();
    subscribers.ToContainer("BlogSubscribers");
    subscribers.HasPartitionKey(s => s.BlogKey);
    subscribers.HasNoDiscriminator();

    var deliveries = modelBuilder.Entity<BlogPostDelivery>();
    deliveries.ToContainer("BlogPostDeliveries");
    deliveries.HasPartitionKey(d => d.BlogKey);
    deliveries.HasNoDiscriminator();

    var emailTemplates = modelBuilder.Entity<BlogEmailTemplate>();
    emailTemplates.ToContainer("BlogEmailTemplates");
    emailTemplates.HasPartitionKey(t => t.BlogKey);
    emailTemplates.HasNoDiscriminator();
  }
}