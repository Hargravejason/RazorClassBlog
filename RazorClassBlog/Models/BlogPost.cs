using System.ComponentModel.DataAnnotations.Schema;
using RazorClassBlog.EnumsandConstants;

namespace RazorClassBlog.Models;

public class BlogPost : BlogPostMini
{
  /// <summary>Storage partition key for a post, separate from the logical blog key.</summary>
  [NotMapped]
  public string PostPartitionKey { get; set; } = string.Empty;

  /// <summary>Configured enum member name used as the schema.org type; null for legacy posts.</summary>
  public string? BlogType { get; set; }

  /// <summary>Main body; Markdown or HTML.</summary>
  public string Content { get; set; } = string.Empty;

  /// <summary>IDs of related posts to display at the bottom of the article.</summary>
  public List<string> RelatedPostIds { get; set; } = new();

}