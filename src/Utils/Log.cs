//
// Log.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Text;
using System.Collections.Generic;

namespace FSpot.Utils
{
    public class LogNotifyEventArgs : EventArgs
    {
        private LogEntry entry;
        
        public LogNotifyEventArgs (LogEntry entry)
        {
            this.entry = entry;
        }
        
        public LogEntry Entry {
            get { return entry; }
        }
    }
        
    public enum LogEntryType
    {
        Trace,
        Debug,
        Warning,
        Error,
        Information
    }
    
    public class LogEntry
    {
        private LogEntryType type;
        private string message;
        private string details;
        private DateTime timestamp;
        
        internal LogEntry (LogEntryType type, string message, string details)
        {
            this.type = type;
            this.message = message;
            this.details = details;
            this.timestamp = DateTime.Now;
        }

        public LogEntryType Type { 
            get { return type; }
        }
        
        public string Message { 
            get { return message; } 
        }
        
        public string Details { 
            get { return details; } 
        }

        public DateTime TimeStamp { 
            get { return timestamp; } 
        }
    }
    
    public static class Log
    {
        public static event EventHandler<LogNotifyEventArgs> Notify;
        
        private static Dictionary<uint, DateTime> timers = new Dictionary<uint, DateTime> ();
        private static uint next_timer_id = 1;

        private static bool debugging = false;
        public static bool Debugging {
            get { return debugging; }
            set { debugging = value; }
        }

        private static bool tracing = false;
        public static bool Tracing {
            get { return tracing; }
            set { tracing = value; }
        }
        
        public static void Commit (LogEntryType type, string message, string details, bool showUser)
        {
            if (type == LogEntryType.Debug && !Debugging) {
                return;
            }

            if (type == LogEntryType.Trace && !Tracing) {
                return;
            }
        
            if (type != LogEntryType.Information || (type == LogEntryType.Information && !showUser)) {
                switch (type) {
                    case LogEntryType.Error: ConsoleCrayon.ForegroundColor = ConsoleColor.Red; break;
                    case LogEntryType.Warning: ConsoleCrayon.ForegroundColor = ConsoleColor.Yellow; break;
                    case LogEntryType.Information: ConsoleCrayon.ForegroundColor = ConsoleColor.Green; break;
                    case LogEntryType.Debug: ConsoleCrayon.ForegroundColor = ConsoleColor.Blue; break;
                    case LogEntryType.Trace: ConsoleCrayon.ForegroundColor = ConsoleColor.Magenta; break;
                }
                
                Console.Write ("[{0} {1:00}:{2:00}:{3:00}.{4:000}]", TypeString (type), DateTime.Now.Hour,
                    DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
                
                ConsoleCrayon.ResetColor ();
                               
                if (details != null) {
                    Console.WriteLine (" {0} - {1}", message, details);
                } else {
                    Console.WriteLine (" {0}", message);
                }
            }
            
            if (showUser) {
                OnNotify (new LogEntry (type, message, details));
            }

            if (type == LogEntryType.Trace) {
                string str = String.Format ("MARK: {0}: {1}", message, details);
                Mono.Unix.Native.Syscall.access(str, Mono.Unix.Native.AccessModes.F_OK);
            }
        }

        private static string TypeString (LogEntryType type)
        {
            switch (type) {
                case LogEntryType.Debug:         return "Debug";
                case LogEntryType.Warning:       return "Warn ";
                case LogEntryType.Error:         return "Error";
                case LogEntryType.Information:   return "Info ";
                case LogEntryType.Trace:         return "Trace";
            }
            return null;
        }
        
        private static void OnNotify (LogEntry entry)
        {
            EventHandler<LogNotifyEventArgs> handler = Notify;
            if (handler != null) {
                handler (null, new LogNotifyEventArgs (entry));
            }
        }
        
        #region Timer Methods
        
        public static uint DebugTimerStart (string message)
        {
            return TimerStart (message, false);
        }
        
        public static uint InformationTimerStart (string message)
        {
            return TimerStart (message, true);
        }
        
        private static uint TimerStart (string message, bool isInfo)
        {
            if (!Debugging && !isInfo) {
                return 0;
            }
            
            if (isInfo) {
                Information (message);
            } else {
                Debug (message);
            }
            
            return TimerStart (isInfo);
        }
        
        public static uint DebugTimerStart ()
        {
            return TimerStart (false);
        }
        
        public static uint InformationTimerStart ()
        {
            return TimerStart (true);
        }
            
        private static uint TimerStart (bool isInfo)
        {
            if (!Debugging && !isInfo) {
                return 0;
            }
            
            uint timer_id = next_timer_id++;
            timers.Add (timer_id, DateTime.Now);
            return timer_id;
        }
        
        public static void DebugTimerPrint (uint id)
        {
            if (!Debugging) {
                return;
            }
            
            TimerPrint (id, "Operation duration: {0}", false);
        }
        
        public static void DebugTimerPrint (uint id, string message)
        {
            if (!Debugging) {
                return;
            }
            
            TimerPrint (id, message, false);
        }
        
        public static void InformationTimerPrint (uint id)
        {
            TimerPrint (id, "Operation duration: {0}", true);
        }
        
        public static void InformationTimerPrint (uint id, string message)
        {
            TimerPrint (id, message, true);
        }
        
        private static void TimerPrint (uint id, string message, bool isInfo)
        {
            if (!Debugging && !isInfo) {
                return;
            }
            
            DateTime finish = DateTime.Now;
            
            if (!timers.ContainsKey (id)) {
                return;
            }
            
            TimeSpan duration = finish - timers[id];
            string d_message;
            if (duration.TotalSeconds < 60) {
                d_message = String.Format ("{0}s", duration.TotalSeconds);
            } else {
                d_message = duration.ToString ();
            }
            
            if (isInfo) {
                Information (message, d_message);
            } else {
                Debug (message, d_message);
            }
        }
        
        #endregion

        #region Public Trace Methods
        public static void Trace (string group, string format, params object [] args)
        {
            if (Tracing) {
                Commit (LogEntryType.Trace, group, String.Format (format, args), false);
            }
        }
        #endregion
        
        #region Public Debug Methods
        public static void Debug (string message)
        {
            if (Debugging) {
                Commit (LogEntryType.Debug, message, null, false);
            }
        }
 
		public static void Debug (string format, params object [] args)
        {
            if (Debugging) {
                Debug (String.Format (format, args));
            }
        }
	
		public static void DebugException (Exception e)
		{
		    if (Debugging)
		        Exception (e);
		}
	
		public static void DebugException (string message, Exception e)
		{
		    if (Debugging)
			Exception (message, e);
	
		}
        #endregion
        
        #region Public Information Methods
        public static void Information (string message, string details, bool showUser)
        {
            Commit (LogEntryType.Information, message, details, showUser);
        }
        
        public static void Information (string message, bool showUser)
        {
            Information (message, null, showUser);
        }
        
        public static void Information (string format, params object [] args)
        {
            Information (String.Format (format, args), null, false);
        }
        #endregion
        
        #region Public Warning Methods
        public static void Warning (string message, string details, bool showUser)
        {
            Commit (LogEntryType.Warning, message, details, showUser);
        }
        
        public static void Warning (string message, bool showUser)
        {
            Warning (message, null, showUser);
        }

		public static void Warning (string format, params object [] args)
        {
            Warning (String.Format (format, args), false);
        }
        #endregion
        
        #region Public Error Methods
        public static void Error (string message, string details, bool showUser)
        {
            Commit (LogEntryType.Error, message, details, showUser);
        }
        
        public static void Error (string message, bool showUser)
        {
            Error (message, null, showUser);
        }

        public static void Error (string format, params object [] args)
        {
            Error (String.Format (format, args), null, false);
        }
        #endregion
        
        #region Public Exception Methods
        
        public static void Exception (Exception e)
        {
            Exception (null, e);
        }
        
        public static void Exception (string message, Exception e)
        {
            Stack<Exception> exception_chain = new Stack<Exception> ();
            StringBuilder builder = new StringBuilder ();
            
            while (e != null) {
                exception_chain.Push (e);
                e = e.InnerException;
            }
            
            while (exception_chain.Count > 0) {
                e = exception_chain.Pop ();
                builder.AppendFormat ("{0} (in `{1}')", e.Message, e.Source).AppendLine ();
                builder.Append (e.StackTrace);
                if (exception_chain.Count > 0) {
                    builder.AppendLine ();
                }
            }
        
            // FIXME: We should save these to an actual log file
            Log.Warning (message ?? "Caught an exception", builder.ToString (), false);
        }
		
		public static void Exception (Exception e, string format, params object [] args)
		{
			Exception (String.Format (format, args), e);
		}
		
	#endregion
    }
}
