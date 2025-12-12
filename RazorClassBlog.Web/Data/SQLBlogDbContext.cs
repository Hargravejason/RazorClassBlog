using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Web.Data;

public class SQLBlogDbContext : DbContext, IBlogDbContext
{
  public SQLBlogDbContext(DbContextOptions<SQLBlogDbContext> options) : base(options) { }

  public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

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

    // Indexes for quick lookups and SEO
    blog.HasIndex(p => new { p.BlogKey, p.Slug })
        .IsUnique();

    blog.HasIndex(p => new { p.BlogKey, p.Status, p.PublishedUtc });

    blog.Property(p => p.Status)
        .HasConversion<int>(); // store enum as int
  }
}
