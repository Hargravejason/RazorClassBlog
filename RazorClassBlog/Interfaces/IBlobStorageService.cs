namespace RazorClassBlog.Interfaces;

/// <summary>
/// Provides image upload and listing operations against Azure Blob Storage.
/// </summary>
public interface IBlobStorageService
{
  /// <summary>
  /// Returns <c>true</c> when blob storage is properly configured and available.
  /// </summary>
  bool IsConfigured { get; }

  /// <summary>
  /// Uploads a stream as a blob and returns its public CDN (or storage) URL.
  /// </summary>
  /// <param name="postId">Blog post ID used as a sub-folder prefix.</param>
  /// <param name="fileName">Original file name (extension is preserved).</param>
  /// <param name="contentType">MIME type of the file (e.g. "image/jpeg").</param>
  /// <param name="stream">Content to upload.</param>
  /// <param name="ct">Cancellation token.</param>
  /// <returns>The absolute public URL of the uploaded blob.</returns>
  Task<string> UploadAsync(string postId, string fileName, string contentType, Stream stream, CancellationToken ct = default);

  /// <summary>
  /// Lists all blob URLs stored under the given blog post sub-folder.
  /// </summary>
  /// <param name="postId">Blog post ID whose images should be listed.</param>
  /// <param name="ct">Cancellation token.</param>
  Task<IReadOnlyList<string>> ListImagesAsync(string postId, CancellationToken ct = default);

  /// <summary>
  /// Deletes a single blob identified by its public URL (CDN or storage URL).
  /// </summary>
  /// <param name="blobUrl">The URL previously returned by <see cref="UploadAsync"/> or <see cref="ListImagesAsync"/>.</param>
  /// <param name="ct">Cancellation token.</param>
  Task DeleteImageAsync(string blobUrl, CancellationToken ct = default);

  /// <summary>
  /// Deletes all blobs stored under the given blog post sub-folder.
  /// Called when the blog post itself is deleted.
  /// </summary>
  /// <param name="postId">Blog post ID whose images should be removed.</param>
  /// <param name="ct">Cancellation token.</param>
  Task DeleteAllImagesAsync(string postId, CancellationToken ct = default);
}
