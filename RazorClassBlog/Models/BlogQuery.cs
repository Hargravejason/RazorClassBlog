namespace RazorClassBlog.Models;
public class BlogQuery
{
  public string BlogKey { get; set; } = "main";

  /// <summary>1-based page index.</summary>
  public int Page { get; set; } = 1;

  public int PageSize { get; set; } = 10;

  /// <summary>Optional tag filter.</summary>
  public string? Tag { get; set; }

  /// <summary>Optional search text (title/content).</summary>
  public string? SearchTerm { get; set; }

  /// <summary>
  /// Include drafts/archived posts.
  /// For public site use false; for admin use true.
  /// </summary>
  public bool IncludeUnpublished { get; set; } = false;

  /// <summary>Only posts up to this UTC time.</summary>
  public DateTimeOffset? PublishedBeforeUtc { get; set; }

  public BlogQuery EnsureValid()
  {
    if (Page < 1) Page = 1;
    if (PageSize < 1) PageSize = 10;
    if (PageSize > 100) PageSize = 100;
    if (string.IsNullOrWhiteSpace(BlogKey)) BlogKey = "main";
    return this;
  }
}