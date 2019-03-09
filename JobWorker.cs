using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    public class JobAttribute : Attribute
    {
        // Declare this as some job/task i want to execute from external source. checked in Compile.
        // Attach some attribute to the job/task so i can know more about it. Must accompany IJobWorker.

        public string Name;
        public List<string> Aliases;
    }
    
    public class JobState
    {
        // Last/Current run state of the job.
        // keep this in persistent storage.

        public int JobTypeId;   // PK in storage.

        public DateTime LastRunSuccess;       // The last time we ran this and it succeeded.

        public DateTime LastRun;       // We tried to run this. It might have failed or succeeded.
        public string LastResult;      // what happened last time this ran? null or "" = success or running incomplete..

        public int RunningAppId;   // 0 = not running. else its currently running. (as far as we know). ConfigApp.AppId;
    }

    public interface IJobWorker
    {
        // abstraction for some job/task i want to execute. exposed by assembly.
        // Code must expose this interface so i can call it externally.

        void Execute(string[] args);
    }
    
    public abstract class JobWorker : IJobWorker
    {
        // Can be used with JobTracker ? JobTypeName ?

        public JobState State { get; set; }

        public abstract void Execute(string[] args);
    }
}
