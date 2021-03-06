﻿using System;
using System.Net;
using System.IO;

namespace DotStd
{
    public class WebDownloader
    {
        // helper to pull a file from some HTTP or FTP server.
        // Has Progress similar to System.IProgress<float>.Report
        // SEE ALSO: HttpDownloader

        public string SrcURL { get; set; }
        public string DestPath { get; set; }    // local dest file path.

        public event Progress2.EventHandler ProgressEvent;        // Called as the download progresses. Report

        public event FailedEventHandler FailedEvent;
        public delegate void FailedEventHandler(Exception ex);

        public WebDownloader()
        {
        }
        public WebDownloader(string srcUrl, string dstPath)
        {
            SrcURL = srcUrl;
            DestPath = dstPath;
        }

        public void DownloadFileRaw(bool allowRedirect = false)
        {
            // Synchronous get file. Doesn't call any events. no protection from throw.
            // https://github.com/aspnet/LibraryManager/blob/master/src/LibraryManager/CacheService/WebRequestHandler.cs

            DirUtil.DirCreateForFile(DestPath);

            if (!allowRedirect)
            {
                using (var wc = new WebClient())
                {
                    wc.DownloadFile(SrcURL, DestPath);  // vs DownloadData()
                }
            }
            else
            {
                // Make sure we allow redirects and such.
                // TODO convert to use HttpClient
                // https://docs.microsoft.com/en-us/dotnet/framework/network-programming/how-to-request-data-using-the-webrequest-class

                var req = WebRequest.Create(SrcURL);
                HttpWebRequest reqH = ((HttpWebRequest)req);

                // reqH.AuthenticationLevel = AuthenticationLevel.None;
                // reqH.AllowAutoRedirect = true;
                reqH.KeepAlive = false;
                reqH.Proxy = null;      // makes it slow.
                                        // reqH.ServicePoint.ConnectionLeaseTimeout = 0;
                                        // reqH.ReadWriteTimeout = System.Threading.Timeout.Infinite;
                reqH.ServicePoint.Expect100Continue = false;
                reqH.ProtocolVersion = HttpVersion.Version10;

                reqH.Credentials = CredentialCache.DefaultCredentials;
                reqH.UseDefaultCredentials = true;
                reqH.Date = TimeNow.Utc;

                // reqH.Timeout = 5000;67
                reqH.Method = "GET";
                reqH.Headers.Set(HttpRequestHeader.CacheControl, "max-age=0, no-cache, no-store");

                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                //var cookieJar = new CookieContainer();
                //reqH.CookieContainer = cookieJar;

                // Pretend to be a browser.
                reqH.UserAgent = "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.71 Safari/537.36 Edge/12.0";
                // reqH.Accept = "*/*";
                // reqH.Headers.Add("Accept-Encoding: gzip, deflate");
                // reqH.Headers.Add("Accept-Language: en-US");

                // Send the 'WebRequest' and wait for response.
                // WebRequest HttpWebRequest timeout timed out at Associating Connection
                using (var rsp = req.GetResponse())
                {
                    using (var dst = File.Create(DestPath))
                    {
                        rsp.GetResponseStream().CopyTo(dst);
                    }
                    rsp.Close();
                }
            }
        }

        public bool DownloadFile()
        {
            // Use this method as target of Thread
            // No ProgressEvent called.
            try
            {
                DownloadFileRaw();
                if (ProgressEvent != null)
                {
                    ProgressEvent(100, 100);    // done "100%"
                }
                return true;
            }
            catch (Exception ex)
            {
                if (FailedEvent != null)
                {
                    FailedEvent(ex);
                }
                return false;
            }
        }

        public bool DownloadFileWithProgress()
        {
            // Download the larger payload files. Call ProgressEvent periodically.
            try
            {
                const int nChunkSize = FileUtil.kDefaultBufferSize;
                int iTotalBytesRead = 0;
                using (var oFS = new FileStream(DestPath, FileMode.Create, FileAccess.Write))
                {
                    WebRequest wRemote = WebRequest.Create(SrcURL);
                    WebResponse myWebResponse = wRemote.GetResponse();
                    var bBuffer = new byte[nChunkSize + 1];
                    using (Stream sChunks = myWebResponse.GetResponseStream())
                    {
                        for (; ; )
                        {
                            if (ProgressEvent != null)
                            {
                                ProgressEvent(iTotalBytesRead, myWebResponse.ContentLength);
                            }
                            if (iTotalBytesRead >= myWebResponse.ContentLength) // done.
                                break;
                            int iBytesRead = sChunks.Read(bBuffer, 0, nChunkSize);
                            if (iBytesRead == 0)
                            {
                                // This is a failure !
                                throw new IOException("Not enough data was provided to complete the file.");
                            }
                            oFS.Write(bBuffer, 0, iBytesRead);
                            iTotalBytesRead += iBytesRead;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                if (FailedEvent != null)
                {
                    FailedEvent(ex);
                }
                LoggerUtil.DebugException("DownloadFileWithProgress", ex);
                return false;
            }
        }
    }
}
