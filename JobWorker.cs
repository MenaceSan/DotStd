using System;
using System.Collections.Generic;
using System.Text;
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
        public List<string> Aliases;    // Other attribute tags.

        public JobAttribute()
        { }
        public JobAttribute(string name)
        {
            Name = name;
        }
    }

    public class JobState
    {
        // Persist Last/Current run state of the job. Produce a JobWorker to run.
        // keep this in persistent storage.
        // leave scheduling definition out. when should this run again? 

        public int Id { get; set; }   // PK in some persistent storage.

        public int RunningAppId { get; set; } = ValidState.kInvalidId;  // 0 = not running. else its currently running. (as far as we know on ConfigApp.AppId);

        // NOTE: LastRun can be set into the future to delay start.
        public DateTime? LastRun { get; set; }       // last UTC start time when we tried to run this. or retry this. It might have failed or succeeded. Might not have been the official scheduled time it was supposed to run.
        public string LastResult { get; set; }      // what happened at/after LastRun? null = never run, "" = success or error description summary.
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
    }

    public interface IJobWorker
    {
        // abstraction for some job/task i want to execute. exposed by assembly.
        // Assume this is paired with JobAttribute
        // Code must expose this interface so i can call it externally.

        JobState State { get; set; }    // is it running ?

        Task ExecuteAsync(string args); // Run it now.
    }

    public abstract class JobWorker : IJobWorker
    {
        // Can be used with JobTracker ? JobTypeName ?

        public JobState State { get; set; }

        public abstract Task ExecuteAsync(string args);

        public JobWorker(JobState state)
        {
            State = state;
        }
    }
}
