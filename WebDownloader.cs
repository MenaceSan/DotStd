using System;
using System.Net;
using System.IO;

namespace DotStd
{
    public class WebDownloader
    {
        // helper to pull a file from some HTTP server.
        // Progress similar to System.IProgress<float>.Report

        public string SrcURL { get; set; }
        public string DestPath { get; set; }    // local dest file path.
        public string UserString { get; set; }  // User can store any string value here for context.

        public event ProgressEventHandler ProgressEvent;        // Called as the download progresses.
        public delegate void ProgressEventHandler(long nSizeCurrent, long nSizeTotal);

        public event FailedEventHandler FailedEvent;
        public delegate void FailedEventHandler(Exception ex);

        public WebDownloader()
        {
        }
        public WebDownloader(string sSrcURL, string sDestPath, string sUserString = "")
        {
            SrcURL = sSrcURL;
            DestPath = sDestPath;
            UserString = sUserString;
        }

        public void DownloadFileRaw()
        {
            // Synchronous get file. Doesnt call any events. no protection from throw.
            DirUtil.DirCreateForFile(DestPath);

#if true // true
            var wc = new WebClient();
            wc.DownloadFile(SrcURL, DestPath);
#else
            // https://docs.microsoft.com/en-us/dotnet/framework/network-programming/how-to-request-data-using-the-webrequest-class
            var req = WebRequest.Create(SrcURL);
            var reqH = ((HttpWebRequest)req);

            reqH.Credentials = CredentialCache.DefaultCredentials;
            reqH.UseDefaultCredentials = true;
            reqH.Date = DateTime.Now;

            // Pretend to be a browser.
            reqH.UserAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Mobile Safari/537.36";

            // Send the 'WebRequest' and wait for response.
            using (var rsp = req.GetResponse())
            {
                using (var dst = File.Create(DestPath))
                {
                    rsp.GetResponseStream().CopyTo(dst);
                }
                rsp.Close();
            }
#endif
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
                const int nChunkSize = 1024;
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
                LoggerBase.DebugException("DownloadFileWithProgress", ex);
                return false;
            }
        }
    }
}
