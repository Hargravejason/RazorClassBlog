using RazorClassBlog.Models;

namespace RazorClassBlog.Interfaces;

public interface IBlogSubscriptionService
{
  Task RequestSubscriptionAsync(string email, CancellationToken ct = default);
  Task ConfirmSubscriptionAsync(string token, CancellationToken ct = default);
  Task<bool> IsUnsubscribeTokenValidAsync(string token, CancellationToken ct = default);
  Task UnsubscribeAsync(string token, CancellationToken ct = default);
  Task<IReadOnlyList<string>> GetSubscribedEmailsAsync(CancellationToken ct = default);
  Task SendPostToSubscribersAsync(BlogPost post, CancellationToken ct = default);
}
