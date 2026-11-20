using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorClassBlog.Interfaces;

namespace RazorClassBlog.Areas.Blog.Pages;

public class UnsubscribeModel : PageModel
{
  private readonly IBlogSubscriptionService _subscriptions;

  public UnsubscribeModel(IBlogSubscriptionService subscriptions)
  {
    _subscriptions = subscriptions;
  }

  [BindProperty(SupportsGet = true)]
  public string? Token { get; set; }

  public bool IsValid { get; private set; }
  public string? Message { get; private set; }

  public async Task OnGetAsync(CancellationToken ct)
  {
    IsValid = !string.IsNullOrWhiteSpace(Token) &&
        await _subscriptions.IsUnsubscribeTokenValidAsync(Token, ct);
  }

  public async Task<IActionResult> OnPostAsync(CancellationToken ct)
  {
    IsValid = !string.IsNullOrWhiteSpace(Token) &&
        await _subscriptions.IsUnsubscribeTokenValidAsync(Token, ct);
    if (!IsValid)
    {
      Message = "This unsubscribe link is invalid or has expired.";
      return Page();
    }

    await _subscriptions.UnsubscribeAsync(Token!, ct);
    Message = "You have been unsubscribed from future posts.";
    return Page();
  }
}
