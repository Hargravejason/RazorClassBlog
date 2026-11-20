namespace RazorClassBlog.Models;

public class BlogEmailTemplate
{
  public string Id { get; set; } = Guid.NewGuid().ToString("N");
  public string BlogKey { get; set; } = "main";
  public string ConfirmationSubject { get; set; } = "Confirm your blog subscription";
  public string ConfirmationHtml { get; set; } =
      "<p>[ConfirmationMessage]</p><p><a href=\"[ConfirmationUrl]\">Confirm subscription</a></p>";
  public string PostSubject { get; set; } = "New blog post: [Title]";
  public string PostHtml { get; set; } =
      "<h1>[Title]</h1><p>[Summary]</p><p><a href=\"[PostUrl]\">Read the post</a></p><p><a href=\"[UnsubscribeUrl]\">Unsubscribe</a></p>";
  public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
