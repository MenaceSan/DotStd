using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotStd
{

    public class JobAttribute : Attribute
    {
        // Declare this as some job/task i want to execute from external source. checked in Compile.
        // Attach some attribute to the job/task so i can know more about it. Must accompany IJobWorker.
        // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/attributes/accessing-attributes-by-using-reflection
        // [Job("MyJobName")]

        public string Name; // primary name
        public List<string>? Aliases;    // Other attribute tags.

        public JobAttribute(string name)
        {
            Name = name;
        }
    }

    public enum JobIsolationId
    {
        // How is the code for this job executed ? What isolation level?
        Await,          // Await the async entry. (default)
        NoAwait,        // run directly inline. No thread. No await.
        Thread,         // Create a thread and call the entry point assembly in my process space.
        ProcAssembly,   // Use a process isolation wrapper on the assembly. (thread assumed)
        Process,        // Load a new process with arguments. EXE
    }

    public class JobState
    {
        // Persist Last/Current run definition/state of a job. Produce a JobWorker to run.
        // keep this in persistent storage.
        // leave scheduling definition out. when should this run again? 

        public int Id { get; set; }   // JobId, PK in some persistent storage.

        public int RunningAppId { get; set; } = ValidState.kInvalidId;  // What AppId is running this now? 0 = not running. else its currently running. (as far as we know on ConfigApp.AppId);
        public int RunningStatus { get; private set; }      // estimated percent complete. 0-1000

        // NOTE: LastRun can be set into the future to delay start.
        public DateTime? LastRun { get; set; }       // last UTC start time when we tried to run this. or retry this. It might have failed or succeeded. Might not have been the official scheduled time it was supposed to run.
        public string? LastResult { get; set; }      // what happened at/after LastRun? null = never run, "" = success or error description summary.
        public DateTime? LastSuccess { get; set; }   // The last UTC start time we ran this and it succeeded. LastResult == ""

        public bool IsRunning
        {
            get
            {
                // Never run the same job reentrant. always wait for the first to finish or figure out why its failing.
                return ValidState.IsValidId(RunningAppId);
            }
        }

        public JobState()
        { }

        public JobState(int id)
        {
            Id = id;
        }

        public virtual void SetRunningStatus(int statusPercent)
        {
            // override this to push the update or persist in DB.
            RunningStatus = statusPercent;
        }
    }

    public interface IJobWorker
    {
        // abstraction for some job/task to execute. exposed by assembly.
        // Assume this is paired with JobAttribute
        // Code must expose this interface so i can call it.
        // Might Compile code from sources ? or just load some external assembly

        JobState State { get; set; }    // Link back to my job state/definition. is it running ? last status etc.

        Task ExecuteAsync(string? args); // Run it now.
    }

    public abstract class JobWorker : IJobWorker
    {
        // Implement IJobWorker
        // Can be used with JobTracker ?  

        public JobState State { get; set; }

        public abstract Task ExecuteAsync(string? args);

        public JobWorker(JobState state)
        {
            State = state;
        }
    }
}
