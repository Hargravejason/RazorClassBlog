using RazorClassBlog.EnumsandConstants;

namespace RazorClassBlog.Models;

public class BlogPost
{
  /// <summary>Storage id (Cosmos id, SQL PK, etc).</summary>
  public string Id { get; set; } = Guid.NewGuid().ToString("N");

  /// <summary>Logical blog partition, e.g. "main" or "support".</summary>
  public string BlogKey { get; set; } = "main";

  public string Title { get; set; } = string.Empty;

  /// <summary>URL slug, unique per BlogKey.</summary>
  public string? Slug { get; set; }

  /// <summary>Main body; Markdown or HTML.</summary>
  public string Content { get; set; } = string.Empty;

  public string? Summary { get; set; }

  /// <summary>Optional hero/cover image URL.</summary>
  public string? HeroImageUrl { get; set; }

  /// <summary>Tags for filtering.</summary>
  public List<string> Tags { get; set; } = new();

  public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft;

  public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
  public DateTimeOffset? PublishedUtc { get; set; }
  public DateTimeOffset? UpdatedUtc { get; set; }

  public DateTimeOffset? DeletedUtc { get; set; }
  public string? DeletedBy { get; set; }

  /// <summary>Author identifier (e.g. Identity user id).</summary>
  public string? AuthorId { get; set; }

  /// <summary>Author display name stored at publish-time.</summary>
  public string? AuthorName { get; set; }
}