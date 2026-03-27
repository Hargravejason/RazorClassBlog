namespace RazorClassBlog.Models;

public class RecentPostsWidgetModel
{
  public required IReadOnlyList<BlogPostMini> Posts { get; init; }

  public required string Layout { get; init; }

  public required string PublicRoutePrefix { get; init; }
}
