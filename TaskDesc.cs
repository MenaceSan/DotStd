using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace DotStd
{
    public enum TaskState
    {
        // Track the state of some task/process i run.
        // Corresponds with the Process.ExitCodes >= 0.

        Aborting = -3,      // Sent the Kill request.
        Scheduling = -2,    // Waiting to run.
        Running = -1,       // Started and probably running.

        Success = 0,        // POSIX success return for process.
        Error = 1,          // Process responded with its own error and text. POSIX
        Abort = 2,          // We aborted. Might have been hung/timeout ? POSIX

        Exception = 3,      // threw an exception.
        FailStart = 4,      // Failed to start.
        Ghost = 5,          // Task is gone. No idea why.
    };

    public class TaskInst
    {
        // Load the task instance dynamically . prepare to call it.

        Assembly _oAsm;
        Type _oType;               // m_oInstance type.
        MethodInfo _oMethod;       // The method i want to call.
        object _oInstance;         // The object whose method i want to call.

        public TaskInst(string sAssemblyFullPath, string sClassName)
        {
            // Loads the class library. But leave the method for later.
            // Will throw on failure.
            _oAsm = Assembly.LoadFrom(sAssemblyFullPath);
            _oType = _oAsm.GetType(sClassName, false, true);
            // Assume SetMethod will be called.
        }
        public TaskInst(string sAssemblyFullPath, string sClassName, string sMethodCall, Type[] aMethodArgTypes)
        {
            // Loads the class library. connect to the method.
            // Will throw on failure.
            _oAsm = Assembly.LoadFrom(sAssemblyFullPath);
            _oType = _oAsm.GetType(sClassName, false, true);
            SetMethod(sMethodCall, aMethodArgTypes);
        }
        public bool SetMethod(string sMethodCall, Type[] aMethodArgTypes)
        {
            // Will throw on failure (ambiguous). or return null if no match.
            // set up the full signature of the method.
            if (aMethodArgTypes != null)
            {
                _oMethod = _oType.GetMethod(sMethodCall, aMethodArgTypes);
            }
            else
            {
                _oMethod = _oType.GetMethod(sMethodCall);
            }
            return _oMethod != null;
        }
        public object GetInstance()
        {
            if (_oInstance == null)
            {
                // Calls the constructor of the object instance. This can do some real work and throw exceptions.
                _oInstance = Activator.CreateInstance(_oType);
            }
            return _oInstance;
        }
        public object Invoke(object[] aMethodArgs)
        {
            // This can throw Exception on failure.
            return _oMethod.Invoke(GetInstance(), aMethodArgs);
        }

        public static string GetArgString(object[] aMethodArgs, int iStart=0)
        {
            // Rebuild the aMethodArgs as a single string for display purposes.
            // Quote strings that have spaces. Cant use String.Join()
            // ASSUME strings dont have special chars like quotes.

            if (aMethodArgs == null)
                return null;
            var sOut = new StringBuilder();
            for (int i = iStart; i < aMethodArgs.Length; i++)
            {
                if (i > 0)
                    sOut.Append(" ");
                string sArg = aMethodArgs[i].ToString();
                if (String.IsNullOrWhiteSpace(sArg))
                    continue;
                if (sArg.Contains(' '))
                {
                    sOut.Append("\"");
                    sOut.Append(sArg);
                    sOut.Append("\"");
                }
                else 
                {
                    sOut.Append(sArg);
                }
            }
            return sOut.ToString();
        }
    }

    public class TaskDesc
    {
        // Describe a task as a method call (with args) on some class in a class library.
        // Use this to get TaskInst

        protected string _sAssemblyFullPath;        // "SampleAssembly, Version=1.0.2004.0, Culture=neutral, PublicKeyToken=8744b20f8da049e3"
        protected string _sClassName;               // Create an instance of this class.
        protected string _sMethodCall;              // What method of the class are we calling? ASSUME not overloaded.

        protected Type[] _aMethodArgTypes = null;   // What types for the arguments to m_sMethodCall ? So we can deal with signatures and overloads.
        protected object[] _aMethodArgs = null;     // Argument values to pass to the method on invoke. 

        public TaskDesc(string _sAssemblyFullPath, string _sClassName, string _sMethodCall, object[] _aMethodArgs = null)
        {
            // This should not throw exceptions.
            this._sAssemblyFullPath = _sAssemblyFullPath;
            this._sClassName = _sClassName;
            this._sMethodCall = _sMethodCall;
            this._aMethodArgs = _aMethodArgs;
            if (this._aMethodArgs != null)
            {
                // aMethodArgTypes ?? use GetType() ?
            }
        }
        public TaskDesc(string[] args)
            : this(args[0], args[1], args[2])
        {
            // This should not throw exceptions.
            if (args.Length > 3)    // anything else is arguments to the method.
            {
                _aMethodArgs = new object[args.Length - 3];
                Array.Copy(args, 3, _aMethodArgs, 0, _aMethodArgs.Length);
                _aMethodArgTypes = new Type[] { typeof(string) };
            }
        }

        public string GetMethodArgs()
        {
            // Get as a single string.
            return TaskInst.GetArgString(_aMethodArgs);
        }

        public TaskInst MakeInst()
        {
            // This can throw exceptions. calls constructor.
            return new TaskInst(_sAssemblyFullPath, _sClassName, _sMethodCall, _aMethodArgTypes);
        }
        public object Invoke()
        {
            // Loads the library and invokes the method in one step.
            // This can throw Exception on failure.
            return MakeInst().Invoke(_aMethodArgs); // calls the method on the object instance.
        }
    }
}
