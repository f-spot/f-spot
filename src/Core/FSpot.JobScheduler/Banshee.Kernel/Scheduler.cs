//
// Scheduler.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2007 Novell, Inc.
// Copyright (C) 2007 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

/***************************************************************************
 *  Scheduler.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Threading;
using System.Collections.Generic;

using Hyena;

namespace Banshee.Kernel
{
    public delegate void JobEventHandler(IJob job);

    public static class Scheduler
    {
        private static object this_mutex = new object();
        private static IntervalHeap<IJob> heap = new IntervalHeap<IJob>();
        private static Thread job_thread;
        private static bool disposed;
        private static IJob current_running_job;
        private static int suspend_count;
        
        public static event JobEventHandler JobStarted;
        public static event JobEventHandler JobFinished;
        public static event JobEventHandler JobScheduled;
        public static event JobEventHandler JobUnscheduled;
        
        public static void Schedule(IJob job)
        {
            Schedule(job, JobPriority.Normal);
        }
        
        public static void Schedule(IJob job, JobPriority priority)
        {
            lock(this_mutex) {
                if(IsDisposed()) {
                    return;
                }
                
                heap.Push(job, (int)priority);
                Debug("Job scheduled ({0}, {1})", job, priority);
                OnJobScheduled(job);
                CheckRun();
            }
        }

        public static void Unschedule(IJob job)
        {
            lock(this_mutex) {
                if(IsDisposed()) {
                    return;
                }
                
                if(heap.Remove(job)) {
                    Debug("Job unscheduled ({0}), job", job);
                    OnJobUnscheduled(job);
                } else {
                    Debug("Job not unscheduled; not located in heap");
                }
            }
        }
        
        public static void Unschedule(Type type)
        {
            lock(this_mutex) {
                Queue<IJob> to_remove = new Queue<IJob>();
                
                foreach(IJob job in ScheduledJobs) {
                    Type job_type = job.GetType();
                    
                    if((type.IsInterface && job_type.GetInterface(type.Name) != null) ||
                        job_type == type || job_type.IsSubclassOf(job_type)) {
                        to_remove.Enqueue(job);
                    }
                }
                
                while(to_remove.Count > 0) {
                    Unschedule(to_remove.Dequeue());
                }
            }
        }
        
        public static void Suspend()
        {
            lock(this_mutex) {
                Interlocked.Increment(ref suspend_count);
            }
        }
        
        public static void Resume()
        {
            lock(this_mutex) {
                Interlocked.Decrement(ref suspend_count);
            }
        }
        
        public static bool IsScheduled(IJob job)
        {
            lock(this_mutex) {
                if(IsDisposed()) {
                    return false;
                }
                
                return heap.Contains(job);
            }
        }
        
        public static bool IsScheduled(Type type)
        {
            lock(this_mutex) {
                if(IsDisposed()) {
                    return false;
                }
                
                foreach(IJob job in heap) {
                    if(job.GetType() == type) {
                        return true;
                    }
                }
                
                return false;
            }
        }
        
        public static bool IsInstanceCriticalJobScheduled {
            get {
                lock(this_mutex) {
                    if(IsDisposed()) {
                        return false;
                    }
                    
                    foreach(IJob job in heap) {
                        if(job is IInstanceCriticalJob) {
                            return true;
                        }
                    }
                    
                    return false;
                }
            }
        }
        
        public static IEnumerable<IJob> ScheduledJobs {
            get { lock(this_mutex) { return heap; } }
        }
       
        public static int ScheduledJobsCount {
            get { lock(this_mutex) { return heap.Count; } }
        }
        
        public static void Dispose()
        {
            lock(this_mutex) {
                disposed = true;
            }
        }

        private static bool IsDisposed()
        {
            if(disposed) {
                Debug("Job not unscheduled; disposing scheduler");
                return true;
            }
            
            return false;
        }
        
        private static void CheckRun()
        {
            if(heap.Count <= 0) {
                return;
            } else if(job_thread == null) {
                Debug("execution thread created");
                job_thread = new Thread(new ThreadStart(ProcessJobThread));
                job_thread.Priority = ThreadPriority.BelowNormal;
                job_thread.IsBackground = true;
                job_thread.Start();
            }   
        }
        
        private static void ProcessJobThread()
        {
            while(true) {
                current_running_job = null;
                
                if(suspend_count > 0) {
                    Thread.Sleep(10);
                    continue;
                }
                    
                lock(this_mutex) {
                    if(disposed) {
                        Log.Debug ("execution thread destroyed, dispose requested");
                        return;
                    }
                
                    try {
                        current_running_job = heap.Pop();
                    } catch(InvalidOperationException) {
                        Debug("execution thread destroyed, no more jobs scheduled");
                        job_thread = null;
                        return;
                    }
                }
                
                try {
                    Debug("Job started ({0})", current_running_job);
                    OnJobStarted(current_running_job);
                    current_running_job.Run();
                    Debug("Job ended ({0})", current_running_job);
                    OnJobFinished(current_running_job);
                } catch(Exception e) {
                    Debug("Job threw an unhandled exception: {0}", e);
                }
            }
        }
        
        public static IJob CurrentJob {
            get { return current_running_job; }
        }
        
        static void OnJobStarted(IJob job)
        {
			JobStarted?.Invoke (job);
        }
        
        static void OnJobFinished(IJob job)
        {
			JobFinished?.Invoke (job);
        }
        
        static void OnJobScheduled(IJob job)
        {
			JobScheduled?.Invoke (job);
        }
        
        static void OnJobUnscheduled(IJob job)
        {
			JobUnscheduled?.Invoke (job);
        }
        
        static void Debug(string message, params object [] args)
        {
            if(Banshee.Base.Globals.Debugging) {
                Console.Error.WriteLine(string.Format("** Scheduler: {0}", message), args);
            }
        }
    }
}
