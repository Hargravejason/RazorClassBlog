namespace RazorClassBlog.Models;

public class BlogSubscriber
{
  public string Id { get; set; } = Guid.NewGuid().ToString("N");
  public string BlogKey { get; set; } = "main";
  public string Email { get; set; } = string.Empty;
  public string NormalizedEmail { get; set; } = string.Empty;
  public string? ConfirmationTokenHash { get; set; }
  public bool IsConfirmed { get; set; }
  public bool IsActive { get; set; }
  public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
