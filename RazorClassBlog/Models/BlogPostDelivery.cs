namespace RazorClassBlog.Models;

public class BlogPostDelivery
{
  public string Id { get; set; } = Guid.NewGuid().ToString("N");
  public string BlogKey { get; set; } = "main";
  public string SubscriberId { get; set; } = string.Empty;
  public string PostId { get; set; } = string.Empty;
  public string UnsubscribeTokenHash { get; set; } = string.Empty;
  public DateTimeOffset SentUtc { get; set; } = DateTimeOffset.UtcNow;
}
