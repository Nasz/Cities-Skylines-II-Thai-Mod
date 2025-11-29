using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using UnityEngine;

namespace Services
{
    internal static class GitHubLocaleFetcher
    {
        // Fetch the raw file from GitHub raw URL and place it into StreamingAssets if it's different/newer.
        // Returns true when the StreamingAssets file was created/updated.
        public static bool FetchToStreamingAssetsIfNew(string rawUrl, string destFileName)
        {
            if (string.IsNullOrEmpty(rawUrl)) throw new ArgumentNullException(nameof(rawUrl));
            if (string.IsNullOrEmpty(destFileName)) throw new ArgumentNullException(nameof(destFileName));

            var streamingRoot = Path.Combine(Application.dataPath, "StreamingAssets");
            Directory.CreateDirectory(streamingRoot);
            var destPath = Path.Combine(streamingRoot, destFileName);

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var bytes = client.GetByteArrayAsync(rawUrl).Result;
                    if (bytes == null || bytes.Length == 0) return false;

                    var newHash = ComputeSHA256(bytes);
                    if (File.Exists(destPath))
                    {
                        var existingBytes = File.ReadAllBytes(destPath);
                        var existingHash = ComputeSHA256(existingBytes);
                        if (string.Equals(newHash, existingHash, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.Log($"GitHubLocaleFetcher: remote file matches existing StreamingAssets file ({destFileName})");
                            return false;
                        }
                    }

                    // write to temp then replace
                    var temp = Path.Combine(streamingRoot, destFileName + ".tmp");
                    File.WriteAllBytes(temp, bytes);
                    // Replace or copy
                    try
                    {
                        if (File.Exists(destPath))
                        {
                            File.Copy(temp, destPath, true);
                            File.Delete(temp);
                        }
                        else
                        {
                            File.Move(temp, destPath);
                        }
                    }
                    catch (Exception)
                    {
                        // try best-effort
                        File.Copy(temp, destPath, true);
                        try { File.Delete(temp); } catch { }
                    }

                    Debug.Log($"GitHubLocaleFetcher: updated StreamingAssets file {destPath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"GitHubLocaleFetcher error: {ex}");
                return false;
            }
        }

        private static string ComputeSHA256(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
