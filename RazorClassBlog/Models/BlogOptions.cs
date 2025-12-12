namespace RazorClassBlog.Models;

public class BlogOptions
{
  /// <summary>
  /// Default logical blog partition. Useful if you ever want multiple blogs.
  /// </summary>
  public string BlogKey { get; set; } = "main";

  public string BlogDescription { get; set; } = "News, updates, and tips from Our Company.";

  public string BlogReason { get; set; } = "We share product updates, how-tos, and best practices to help you get more out of our platform.";

  public string? DefaultOrganizationName { get; set; }

  /// <summary>Default page size for public blog list.</summary>
  public int PublicPageSize { get; set; } = 10;

  /// <summary>Default page size for admin list.</summary>
  public int AdminPageSize { get; set; } = 20;

  /// <summary>
  /// Roles that are allowed to manage the blog (admin pages: create/edit/delete).
  /// </summary>
  public string[] AdminRoles { get; set; } = new[] { "Administrator" };

  /// <summary>
  /// Roles that are considered "blog readers" (e.g. can comment).
  /// </summary>
  public string[] ReaderRoles { get; set; } = Array.Empty<string>();

  /// <summary>
  /// When false, the BlogAdmin area routes are removed (no /BlogAdmin endpoints at all).
  /// </summary>
  public bool EnableAdminUi { get; set; } = true;
}