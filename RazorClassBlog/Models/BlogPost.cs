using RazorClassBlog.EnumsandConstants;

namespace RazorClassBlog.Models;

public class BlogPost : BlogPostMini
{
  /// <summary>Main body; Markdown or HTML.</summary>
  public string Content { get; set; } = string.Empty;

}