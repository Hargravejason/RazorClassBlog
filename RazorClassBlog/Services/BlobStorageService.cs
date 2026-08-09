using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using RazorClassBlog.Interfaces;
using RazorClassBlog.Models;

namespace RazorClassBlog.Services;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// Images are stored at: {container}/{folderPrefix}/{postId}/{fileName}
/// </summary>
public class BlobStorageService : IBlobStorageService
{
  private readonly BlogOptions _options;

  public BlobStorageService(IOptions<BlogOptions> options)
  {
    _options = options.Value;
  }

  /// <inheritdoc/>
  public bool IsConfigured =>
      !string.IsNullOrWhiteSpace(_options.BlobStorageConnectionString);

  /// <inheritdoc/>
  public async Task<string> UploadAsync(
      string postId,
      string fileName,
      string contentType,
      Stream stream,
      CancellationToken ct = default)
  {
    var (containerClient, folderPrefix) = GetContainerAndFolder();

    try
    {
      await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
      // Note: PublicAccessType.Blob grants anonymous read for individual blobs (not container listing).
      // Blog images are intended to be publicly reachable (embedded in published posts / served via CDN).
    }
    catch (RequestFailedException ex) when (ex.Status == 409)
    {
      // The container already exists (or public access is restricted at the storage-account level).
      // Either way the container is present and we can safely continue uploading.
    }

    var blobName = BuildBlobName(folderPrefix, postId, fileName);
    var blobClient = containerClient.GetBlobClient(blobName);

    await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

    return BuildUrl(blobClient.Uri.ToString(), blobName);
  }

  /// <inheritdoc/>
  public async Task<IReadOnlyList<string>> ListImagesAsync(string postId, CancellationToken ct = default)
  {
    var (containerClient, folderPrefix) = GetContainerAndFolder();

    var prefix = string.IsNullOrEmpty(folderPrefix)
        ? $"{postId}/"
        : $"{folderPrefix}/{postId}/";

    var results = new List<string>();

    await foreach (var item in containerClient.GetBlobsAsync(new GetBlobsOptions { Prefix = prefix }, cancellationToken: ct))
    {
      var blobClient = containerClient.GetBlobClient(item.Name);
      results.Add(BuildUrl(blobClient.Uri.ToString(), item.Name));
    }

    return results;
  }

  /// <inheritdoc/>
  public async Task DeleteImageAsync(string blobUrl, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(blobUrl))
      return;

    var blobName = ExtractBlobName(blobUrl);
    if (blobName == null)
      return;

    var (containerClient, _) = GetContainerAndFolder();
    var blobClient = containerClient.GetBlobClient(blobName);
    await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
  }

  /// <inheritdoc/>
  public async Task DeleteAllImagesAsync(string postId, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(postId))
      return;

    var (containerClient, folderPrefix) = GetContainerAndFolder();

    var prefix = string.IsNullOrEmpty(folderPrefix)
        ? $"{postId}/"
        : $"{folderPrefix}/{postId}/";

    await foreach (var item in containerClient.GetBlobsAsync(new GetBlobsOptions { Prefix = prefix }, cancellationToken: ct))
    {
      await containerClient.GetBlobClient(item.Name).DeleteIfExistsAsync(cancellationToken: ct);
    }
  }

  // ── helpers ─────────────────────────────────────────────────────────────

  private (BlobContainerClient container, string folderPrefix) GetContainerAndFolder()
  {
    var connectionString = _options.BlobStorageConnectionString!;
    var containerPath = (_options.BlobStorageContainerPath ?? "media").Trim('/');

    // Split "container/optional/folder" into container name + optional prefix
    var slashIndex = containerPath.IndexOf('/');
    string containerName;
    string folderPrefix;

    if (slashIndex < 0)
    {
      containerName = containerPath;
      folderPrefix = string.Empty;
    }
    else
    {
      containerName = containerPath[..slashIndex];
      folderPrefix = containerPath[(slashIndex + 1)..];
    }

    var serviceClient = new BlobServiceClient(connectionString);
    return (serviceClient.GetBlobContainerClient(containerName), folderPrefix);
  }

  private static string BuildBlobName(string folderPrefix, string postId, string fileName)
  {
    // Sanitise the file name: keep only the last segment and make it lower-case
    var safeName = Path.GetFileName(fileName).ToLowerInvariant();
    // Prefix with a short timestamp so repeated uploads with the same name don't overwrite
    var unique = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{safeName}";

    return string.IsNullOrEmpty(folderPrefix)
        ? $"{postId}/{unique}"
        : $"{folderPrefix}/{postId}/{unique}";
  }

  private string BuildUrl(string blobUri, string blobName)
  {
    var cdnBase = _options.BlobStorageCdnBaseUrl?.TrimEnd('/');
    if (string.IsNullOrEmpty(cdnBase))
      return blobUri;

    return $"{cdnBase}/{blobName}";
  }

  /// <summary>
  /// Reverses <see cref="BuildUrl"/> — extracts the blob name from a public URL.
  /// Works for both CDN URLs and direct storage URLs.
  /// </summary>
  private string? ExtractBlobName(string url)
  {
    // Try CDN URL first: {cdnBase}/{blobName}
    var cdnBase = _options.BlobStorageCdnBaseUrl?.TrimEnd('/');
    if (!string.IsNullOrEmpty(cdnBase) && url.StartsWith(cdnBase + "/", StringComparison.OrdinalIgnoreCase))
      return url[(cdnBase.Length + 1)..];

    // Try direct Azure storage URL: {containerUri}/{blobName}
    var (containerClient, _) = GetContainerAndFolder();
    var containerUri = containerClient.Uri.ToString().TrimEnd('/') + "/";
    if (url.StartsWith(containerUri, StringComparison.OrdinalIgnoreCase))
      return url[containerUri.Length..];

    return null;
  }
}
