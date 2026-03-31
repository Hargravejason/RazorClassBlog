using Microsoft.EntityFrameworkCore;
using RazorClassBlog.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace RazorClassBlog.Models;

public class BlogOptions
{
  /// <summary>
  /// Default logical blog partition. Useful if you ever want multiple blogs.
  /// </summary>
  public string BlogKey { get; set; } = "main";

  /// <summary>
  /// Default blog title
  /// </summary>
  public string BlogDescription { get; set; } = "News, updates, and tips from Our Company.";

  /// <summary>
  /// Default for message to user on why the blog exists, shows on the blog page
  /// </summary>
  public string BlogReason { get; set; } = "We share product updates, how-tos, and best practices to help you get more out of our platform.";

  /// <summary>
  /// Default Organization name if no user info is available.
  /// </summary>
  public string? DefaultOrganizationName { get; set; }

  /// <summary>
  /// Default Organization image URL if no user info is available.
  /// </summary>
  public string? DefaultOrganizationImageURL { get; set; }

  /// <summary>
  /// Default page size for public blog list.
  /// </summary>
  public int PublicPageSize { get; set; } = 10;

  /// <summary>
  /// Default page size for admin list.
  /// </summary>
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

  /// <summary>
  /// Public route prefix used by the host site (e.g. "/blog" or "/property-insights").
  /// Must start with "/" and have no trailing slash.
  /// </summary>
  public string PublicRoutePrefix { get; set; } = "/blog";

  /// <summary>
  /// Default number of items shown by the recent posts widget.
  /// </summary>
  public int RecentPostsCount { get; set; } = 5;

  /// <summary>
  /// Default layout for the recent posts widget. Supported values: "vertical", "horizontal".
  /// </summary>
  public string RecentPostsLayout { get; set; } = "vertical";

  /// <summary>What the site calls the blog in UI (nav, headings, breadcrumbs).</summary>
  public string PublicDisplayName { get; set; } = "Blog";

  /// <summary>
  /// Azure Blob Storage connection string for image uploads.
  /// When null or empty, image upload functionality is disabled.
  /// </summary>
  public string? BlobStorageConnectionString { get; set; }

  /// <summary>
  /// CDN base URL used to build public image URLs (e.g. "https://cdn.example.com").
  /// When null, the blob storage URL is used directly.
  /// </summary>
  public string? BlobStorageCdnBaseUrl { get; set; }

  /// <summary>
  /// Container name and optional folder prefix for uploaded images
  /// (e.g. "media" or "media/blog-images"). No leading or trailing slashes.
  /// </summary>
  public string BlobStorageContainerPath { get; set; } = "media";

  /// <summary>
  /// Allows previewing of posts that have been published, but not yet reached their scheduled publish date.
  /// </summary>
  public bool AllowPreviewOfPublishedPosts { get; set; } = false;
}