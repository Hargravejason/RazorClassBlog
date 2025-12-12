using System.Text.RegularExpressions;

namespace RazorClassBlog.EnumsandConstants;

public static class SlugGenerator
{
  private static readonly Regex InvalidChars = new(@"[^a-z0-9\s-]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
  private static readonly Regex MultipleSpaces = new(@"\s+", RegexOptions.Compiled);
  private static readonly Regex MultipleHyphens = new("-{2,}", RegexOptions.Compiled);

  public static string GenerateSlug(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
      return string.Empty;

    var slug = input.Trim().ToLowerInvariant();

    slug = InvalidChars.Replace(slug, string.Empty);
    slug = MultipleSpaces.Replace(slug, "-");
    slug = MultipleHyphens.Replace(slug, "-");

    return slug.Trim('-');
  }
}