using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotStd
{
    public static class HttpUtil
    {
        // Share stuff with other users of HttpClient

        public const string kAuthBearer = "Bearer";

    }

    public class HttpDownloader
    {
        // Use HttpClient instead of WebClient

        public string SrcURL { get; set; }
        public string DestPath { get; set; }    // local dest file path.

        public event Progress2.EventHandler ProgressEvent;        // Called as the download progresses. Report

        public HttpDownloader()
        {
        }
        public HttpDownloader(string srcUrl, string dstPath)
        {
            SrcURL = srcUrl;
            DestPath = dstPath;
        }

        public async Task DownloadFileAsync(HttpClient client)
        {
            // Get some HTTP/HTTPS URL and put in local file.

            DirUtil.DirCreateForFile(DestPath);

            HttpResponseMessage response = await client.GetAsync(SrcURL);
            response.EnsureSuccessStatusCode();

            using (var dst = File.Create(DestPath))
            {
                if (ProgressEvent == null)
                {
                    // No progress events
                    await response.Content.CopyToAsync(dst);
                }
                else
                {
                    // Send progress events.

                    DateTime lastEvent = DateTime.UtcNow;
                    const int kBlockSize = 8192;
                    long? totalBytes1 = response.Content.Headers.ContentLength;    // may not be known/sent.
                    bool isEstimated = totalBytes1 == null;
                    long totalBytes = totalBytes1 ?? kBlockSize;

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        long currentBytesRead = 0L;
                        var buffer = new byte[kBlockSize];

                        while (true)
                        {
                            if (currentBytesRead > totalBytes)
                            {
                                isEstimated = true;
                                totalBytes += kBlockSize;       // re-estimated total.
                            }

                            int bytesRead = await contentStream.ReadAsync(buffer, 0, kBlockSize);
                            if (bytesRead == 0)
                            {
                                ProgressEvent(currentBytesRead, isEstimated ? currentBytesRead : totalBytes);
                                break;  // we are done!
                            }

                            await dst.WriteAsync(buffer, 0, bytesRead);

                            currentBytesRead += bytesRead;

                            var now = DateTime.UtcNow; 
                            var elapsed = now - lastEvent;
                            if (elapsed.TotalSeconds > 0.50)
                            {
                                ProgressEvent(currentBytesRead, totalBytes);
                                lastEvent = now;
                            }
                        }
                    }
                }
            }
        }

        public async Task DownloadFileAsync(HttpClientHandler httpClientHandler)
        {
            // Get some HTTP/HTTPS URL and put in local file.

            using (var client = new HttpClient(httpClientHandler))
            {
                await DownloadFileAsync(client);
            }
        }

        public async Task DownloadFileAsync(bool allowRedirect = false)
        {
            // Get some HTTP/HTTPS URL and put in local file.

            await DownloadFileAsync(new HttpClientHandler()
            {
                AllowAutoRedirect = allowRedirect,
            });
        }
        
        public void DownloadFile(bool allowRedirect = false)
        {
            // Get some HTTP/HTTPS URL and put in local file.
            DownloadFileAsync(allowRedirect).Wait();
        }
    }
}
