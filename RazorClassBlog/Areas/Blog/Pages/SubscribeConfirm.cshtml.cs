using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorClassBlog.Interfaces;

namespace RazorClassBlog.Areas.Blog.Pages;

public class SubscribeConfirmModel : PageModel
{
  private readonly IBlogSubscriptionService _subscriptions;

  public SubscribeConfirmModel(IBlogSubscriptionService subscriptions)
  {
    _subscriptions = subscriptions;
  }

  [BindProperty(SupportsGet = true)]
  public string? Token { get; set; }

  public async Task OnGetAsync(CancellationToken ct)
  {
    if (!string.IsNullOrWhiteSpace(Token))
      await _subscriptions.ConfirmSubscriptionAsync(Token, ct);
  }
}
