using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Web.Data;

public class SQLBlogDbContext : DbContext, IBlogDbContext
{
  public SQLBlogDbContext(DbContextOptions<SQLBlogDbContext> options) : base(options) { }

  public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
  public DbSet<BlogSubscriber> BlogSubscribers => Set<BlogSubscriber>();
  public DbSet<BlogPostDelivery> BlogPostDeliveries => Set<BlogPostDelivery>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    var blog = modelBuilder.Entity<BlogPost>();

    blog.ToTable("BlogPosts");

    blog.HasKey(p => p.Id);

    blog.Property(p => p.BlogKey)
        .HasMaxLength(64)
        .IsRequired();

    blog.Property(p => p.Title)
        .HasMaxLength(256)
        .IsRequired();

    blog.Property(p => p.Slug)
        .HasMaxLength(256)
        .IsRequired();

    blog.Property(p => p.Summary)
        .HasMaxLength(512);

    blog.Property(p => p.HeroImageUrl)
        .HasMaxLength(512);

    blog.Property(p => p.AuthorId)
        .HasMaxLength(128);

    blog.Property(p => p.AuthorName)
        .HasMaxLength(256);

    // Tags: List<string> -> comma-separated string
    var tagsConverter = new ValueConverter<List<string>, string>(
        v => string.Join(",", v ?? new List<string>()),
        v => string.IsNullOrWhiteSpace(v)
            ? new List<string>()
            : v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());

    blog.Property(p => p.Tags)
        .HasConversion(tagsConverter)
        .HasMaxLength(1024);

    // RelatedPostIds: List<string> -> comma-separated string
    var relatedPostIdsConverter = new ValueConverter<List<string>, string>(
        v => string.Join(",", v ?? new List<string>()),
        v => string.IsNullOrWhiteSpace(v)
            ? new List<string>()
            : v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());

    blog.Property(p => p.RelatedPostIds)
        .HasConversion(relatedPostIdsConverter)
        .HasMaxLength(2048);

    // Indexes for quick lookups and SEO
    blog.HasIndex(p => new { p.BlogKey, p.Slug })
        .IsUnique();

    blog.HasIndex(p => new { p.BlogKey, p.Status, p.PublishedUtc });

    blog.Property(p => p.Status)
        .HasConversion<int>(); // store enum as int

    var subscriber = modelBuilder.Entity<BlogSubscriber>();
    subscriber.ToTable("BlogSubscribers");
    subscriber.HasKey(s => s.Id);
    subscriber.Property(s => s.Id).HasMaxLength(64).IsRequired();
    subscriber.Property(s => s.BlogKey).HasMaxLength(64).IsRequired();
    subscriber.Property(s => s.Email).HasMaxLength(320).IsRequired();
    subscriber.Property(s => s.NormalizedEmail).HasMaxLength(320).IsRequired();
    subscriber.Property(s => s.ConfirmationTokenHash).HasMaxLength(64);
    subscriber.HasIndex(s => new { s.BlogKey, s.NormalizedEmail }).IsUnique();

    var delivery = modelBuilder.Entity<BlogPostDelivery>();
    delivery.ToTable("BlogPostDeliveries");
    delivery.HasKey(d => d.Id);
    delivery.Property(d => d.Id).HasMaxLength(64).IsRequired();
    delivery.Property(d => d.BlogKey).HasMaxLength(64).IsRequired();
    delivery.Property(d => d.SubscriberId).HasMaxLength(64).IsRequired();
    delivery.Property(d => d.PostId).HasMaxLength(450).IsRequired();
    delivery.Property(d => d.UnsubscribeTokenHash).HasMaxLength(64).IsRequired();
    delivery.HasIndex(d => new { d.BlogKey, d.SubscriberId, d.PostId }).IsUnique();
    delivery.HasIndex(d => new { d.BlogKey, d.UnsubscribeTokenHash }).IsUnique();
  }
}
