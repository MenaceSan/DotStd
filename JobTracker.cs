using System.Threading;

namespace DotStd
{
    public class JobTracker
    {
        // Track some long process/job/task that is running async on the server.
        // Web browser can periodically request updates to its progress.
        // HangFire might do this better? use SignalR ?  
        // Similar to System.Progress<T>

        // TODO Push updates to UI ? So we dont have the UI polling for this?

        private string JobTypeName { get; set; }        // Id for what am i doing ? e.g. nameof(x) not friendly name. 
        private int UserId { get; set; }             // What user is this for ? 0 = doesnt matter.

        public bool IsComplete { get; private set; }    // code exited.
        public string FailureMsg { get; private set; }  // null = ok

        private long Size { get; set; }         // arbitrary estimated total size.
        private long Progress { get; set; }     // how much of Size is done?

        private CancellationTokenSource Cancellation { get; set; }   // we can try to cancel this?

        public CancellationToken CancellationToken
        {
            get
            {
                return Cancellation.Token;
            }
        }

        public static string MakeKey(string typeName, int userId)
        {
            // Make a unique id for this
            return typeName + userId.ToString();
        }
        public string Key
        {
            get
            {
                return MakeKey(JobTypeName, UserId);
            }
        }

        public static JobTracker FindJobTracker(string typeName, int userId)
        {
            // Find the job in the global name space.
            return CacheObj<JobTracker>.Get(MakeKey(typeName, userId));
        }

        public bool IsCancelled
        {
            // should be Called by worker periodically to see if it should stop.
            get
            {
                if (this.FailureMsg != null)
                    return true;
                if (Cancellation == null)
                    return false;
                return Cancellation.IsCancellationRequested;
            }
        }

        public void Cancel()
        {
            // Called by watcher. Cancel the JobWorker.
            this.FailureMsg = "Canceled";
            Cancellation?.Cancel();
        }
        public void SetFailureMsg(string failureMsg)
        {
            // Cancel the worker because it failed.
            if (IsCancelled)
                return;
            if (string.IsNullOrWhiteSpace(failureMsg))
                failureMsg = null;
            this.FailureMsg = failureMsg;
            if (failureMsg != null)
            {
                Cancellation?.Cancel();
            }
        }

        public int GetProgressPercent()
        {
            // %
            return (int)((this.Progress * 100) / Size);
        }

        public static string GetProgressPercent(string typeName, int userId, bool cancel)
        {
            // Called by watcher.
            var job = FindJobTracker(typeName, userId);
            if (job == null)
                return "";  // never started.
            if (job.IsCancelled)
            {
                return job.FailureMsg;
            }
            if (job.IsComplete)
            {
                return "Complete";
            }
            if (cancel)
            {
                job.Cancel();
            }
            return job.GetProgressPercent().ToString() + "% Complete";
        }

        public void SetStartSize(long size)
        {
            if (size <= 0)
                size = 1;
            this.Progress = 0;
            this.Size = size;
        }

        public void SetProgress(long progress)
        {
            // Called by worker to say what is done.
            if (progress < 0 || progress > Size)
            {
                Progress = Size;
            }
            else
            {
                Progress = progress;
            }
        }

        public void AddStartSize(long size)
        {
            // We discovered the job is bigger.
            if (size <= 0)
                size = 1;
            this.Size += size;
        }

        public void AddProgress(int length)
        {
            if (length < 0)
            {
                return;
            }
            this.Progress += length;
            if (this.Progress > this.Size)
            {
                this.Progress = this.Size;
            }
        }

        public void SetComplete()
        {
            // assume we call this even if canceled.
            IsComplete = true;
            if (!IsCancelled)
            {
                this.Progress = this.Size; // true end.
            }
            CacheObj<JobTracker>.Set(Key, this, 5 * 60);  // no need to hang around too long
        }

        public static JobTracker CreateJobTracker(string typeName, int userId, long size, bool cancelable = false)
        {
            if (size <= 0)
            {
                return null;    // not allowed.
            }

            string cacheKey = MakeKey(typeName, userId);
            var job = CacheObj<JobTracker>.Get(cacheKey);
            if (job == null)
            {
                job = new JobTracker { JobTypeName = typeName, UserId = userId, Size = size, Progress = 0 };
            }
            else if (job.IsComplete)
            {
                // just re-use done job
                job.FailureMsg = null;
                job.IsComplete = false;
                job.SetStartSize(size);
            }
            else
            {
                return null;    // cant dupe the active job.
            }

            if (cancelable)
            {
                job.Cancellation = new CancellationTokenSource();
            }

            CacheObj<JobTracker>.Set(cacheKey, job, 24 * 60 * 60);    // update time
            return job;
        }
    }
}
