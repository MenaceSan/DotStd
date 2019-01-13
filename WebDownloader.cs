using System;
using System.Net;
using System.IO;

namespace DotStd
{
    public class WebDownloader
    {
        // helper to pull a file from some HTTP server.
        // Progress similar to System.IProgress<float>.Report

        public event ProgressEventHandler ProgressEvent;
        public delegate void ProgressEventHandler(long nSizeCurrent, long nSizeTotal);

        public event FailedEventHandler FailedEvent;
        public delegate void FailedEventHandler(Exception ex);

        public string SrcURL
        { get; set; }
        public string DestPath      // local dest file path.
        { get; set; }
        public string UserString    // User can store any string value here for context.
        { get; set; }

        public WebDownloader()
        {
        }
        public WebDownloader(string sSrcURL, string sDestPath, string sUserString = "")
        {
            SrcURL = sSrcURL;
            DestPath = sDestPath;
            UserString = sUserString;
        }

        public bool DownloadFile(bool bRaiseEvent)
        {
            try
            {
                DirUtil.DirCreateForFile(DestPath);
                var WC = new WebClient();
                WC.DownloadFile(SrcURL, DestPath);
                if (bRaiseEvent && ProgressEvent != null)
                {
                    ProgressEvent(100, 100);    // done "100%"
                }
                return true;
            }
            catch (Exception ex)
            {
                if (bRaiseEvent)
                {
                    if (FailedEvent != null)
                    {
                        FailedEvent(ex);
                    }
                    return false;
                }
                throw;
            }
        }

        public void DownloadFile()
        {
            // Use this method as target of Thread
            DownloadFile(true);
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
                        for(;;)
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
