using RazorClassBlog.EnumsandConstants;

namespace RazorClassBlog.Models;

public class BlogPost : BlogPostMini
{
  /// <summary>Main body; Markdown or HTML.</summary>
  public string Content { get; set; } = string.Empty;

  /// <summary>IDs of related posts to display at the bottom of the article.</summary>
  public List<string> RelatedPostIds { get; set; } = new();

}