using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    public class Progress2 
    {
        // like System.Progress<float> or System.IProgress<T> but has 2 params.
        public long Current { get; private set; }     // how much of Size is done? updated by worker. ALWAYS <= Size
        public long Total { get; private set; }         // arbitrary estimated total size.

        // Progress similar to System.IProgress<long>.Report
        public delegate void EventHandler(long nSizeCurrent, long nSizeTotal);    // Report

        public Progress2(long size = 0)
        {
            Total = size;
        }

        public int GetPercent()
        {
            // % complete 
            return (int)((this.Current * 100) / Total);
        }
        public void SetSize(long size)
        {
            // Estimated size of the job to be done.
            if (size <= 0)
                size = 1;
            this.Current = 0;
            this.Total = size;
        }
        public void SetEnd()
        {
            // We are done.
            this.Current = this.Total; // true end.
        }

        public void Add(long length)
        {
            // Add to current progress.
            if (length < 0)
            {
                return;
            }
            length += this.Current;
            this.Current = (length > this.Total) ? this.Total : length;
        }

        public void AddSize(long size)
        {
            // We discovered the job is bigger.
            if (size <= 0)
                size = 1;
            this.Total += size;
        }
    }
}
