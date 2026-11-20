using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Areas.BlogAdmin;

[Authorize(Policy = "BlogAdmin")]
public class SubscriptionEmailsModel : PageModel
{
  private readonly IBlogSubscriptionService _subscriptions;

  public SubscriptionEmailsModel(IBlogSubscriptionService subscriptions)
  {
    _subscriptions = subscriptions;
  }

  [BindProperty, Required, StringLength(998)]
  public string ConfirmationSubject { get; set; } = string.Empty;

  [BindProperty, Required]
  public string ConfirmationHtml { get; set; } = string.Empty;

  [BindProperty, Required, StringLength(998)]
  public string PostSubject { get; set; } = string.Empty;

  [BindProperty, Required]
  public string PostHtml { get; set; } = string.Empty;

  [TempData]
  public string? StatusMessage { get; set; }

  public async Task OnGetAsync(CancellationToken ct)
  {
    var template = await _subscriptions.GetEmailTemplateAsync(ct);
    ConfirmationSubject = template.ConfirmationSubject;
    ConfirmationHtml = template.ConfirmationHtml;
    PostSubject = template.PostSubject;
    PostHtml = template.PostHtml;
  }

  public async Task<IActionResult> OnPostAsync(CancellationToken ct)
  {
    if (!ModelState.IsValid)
      return Page();

    await _subscriptions.SaveEmailTemplateAsync(new BlogEmailTemplate
    {
      ConfirmationSubject = ConfirmationSubject,
      ConfirmationHtml = ConfirmationHtml,
      PostSubject = PostSubject,
      PostHtml = PostHtml
    }, ct);

    StatusMessage = "Subscription email templates saved.";
    return RedirectToPage();
  }
}
