using System.Threading;

namespace DotStd
{
    public class JobTracker
    {
        // Track some long process/job/task that is running async on the server.
        // Web browser can periodically request updates to its progress.
        // HangFire might do this better? use SignalR ?  
 
        // TODO Push updates to UI ? So we dont have the UI polling for this?

        private string JobTypeName { get; set; }        // Id for what am i doing ? e.g. nameof(x) not friendly name. 
        private int UserId { get; set; }             // What user is this for ? 0 = doesnt matter (all users share).

        public bool IsComplete { get; private set; }    // code exited. fail or success.
        public string FailureMsg { get; private set; }  // null = ok, else i failed and returned prematurely.

        private Progress2 Progress = new Progress2();

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
            // Make a unique id/name for this job
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
            return CacheT<JobTracker>.Get(MakeKey(typeName, userId));
        }

        public bool IsCancelled
        {
            // should be Called by worker periodically to see if it should stop.
            get
            {
                if (this.FailureMsg != null)
                    return true;
                if (Cancellation == null)   // no external cancel is possible.
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
            // Cancel the job/worker because it failed.
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
 
        public static string GetProgressPercent(string typeName, int userId, bool cancel)
        {
            // Find some async job in the namespace and get its status.
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
            return job.Progress.GetPercent().ToString() + "% Complete";
        }

        public void SetStartSize(long size)
        {
            // Estimated size of the job to be done.
            Progress.SetSize(size);
        }
 

        public void AddStartSize(long size)
        {
            // We discovered the job is bigger.
            this.Progress.AddSize(size);
        }

        public void AddProgress(int length)
        {
            // a chunk has been completed.
            Progress.Add(length);
        }

        public void SetComplete()
        {
            // assume we call this even if canceled.
            IsComplete = true;
            if (!IsCancelled)
            {
                this.Progress.SetEnd(); // true end.
            }
            CacheT<JobTracker>.Set(Key, this, 5 * 60);  // no need to hang around too long
        }

        public static JobTracker CreateJobTracker(string typeName, int userId, long size, bool cancelable = false)
        {
            // We are starting some async job that we want to track the progress of.
            if (size <= 0)
            {
                return null;    // not allowed.
            }

            string cacheKey = MakeKey(typeName, userId);
            var job = CacheT<JobTracker>.Get(cacheKey);
            if (job == null)
            {
                job = new JobTracker { JobTypeName = typeName, UserId = userId, Progress = new Progress2(size) };
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

            CacheT<JobTracker>.Set(cacheKey, job, 24 * 60 * 60);    // update time
            return job;
        }
    }
}
