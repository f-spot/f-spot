//
// JobStore.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2007-2008 Stephane Delcroix
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

using System;

using Banshee.Kernel;

using FSpot.Core;
using FSpot.Jobs;

using Hyena;
using Hyena.Data.Sqlite;

namespace FSpot.Database
{
    public abstract class Job : DbItem, IJob
    {
    	public Job (IDb db, uint id, string job_options, JobPriority job_priority, DateTime run_at, bool persistent) : base (id)
    	{
    		JobOptions = job_options;
    		JobPriority = job_priority;
    		RunAt = run_at;
    		Persistent = persistent;
			Db = db;
    	}
    
    	public string JobOptions { get; set; }
    	internal JobPriority JobPriority { get; set; }
    	//Not in use yet !
    	public DateTime RunAt { get; private set; }
    	public bool Persistent { get; private set; }
		protected IDb Db { get; private set; }
    
    	public event EventHandler Finished;
    
    	private JobStatus status;
    	public JobStatus Status
    	{
    		get { return status; }
    		set {
    			status = value;
    			switch (value) {
    			case JobStatus.Finished:
    			case JobStatus.Failed:
    				if (Finished != null)
    					Finished (this, new EventArgs ());
    				break;
    			default:
    				break;
    			}
    		}
    	}
    
    	public void Run ()
    	{
    		Status = JobStatus.Running;
    		if (Execute ())
    			Status = JobStatus.Finished;
    		else
    			Status = JobStatus.Failed;
    	}
    
    	protected abstract bool Execute ();
    }

    public class JobStore : DbStore<Job> {
    
    	internal static void CreateTable (FSpotDatabaseConnection database)
    	{
    		if (database.TableExists ("jobs")) {
    			return;
    		}
    
    		database.Execute (
    			"CREATE TABLE jobs (\n" +
    			"	id		INTEGER PRIMARY KEY NOT NULL, \n" +
    			"	job_type	TEXT NOT NULL, \n" +
    			"	job_options	TEXT NOT NULL, \n" +
    			"	run_at		INTEGER, \n" +
    			"	job_priority	INTEGER NOT NULL\n" +
    			")");
    	}
    
    	private Job LoadItem (Hyena.Data.Sqlite.IDataReader reader)
    	{
    		return (Job) Activator.CreateInstance (
    				Type.GetType (reader ["job_type"].ToString ()),
    				Db,
    				Convert.ToUInt32 (reader["id"]),
    				reader["job_options"].ToString (),
    				Convert.ToInt32 (reader["run_at"]),
    				(JobPriority) Convert.ToInt32 (reader["job_priority"]),
    				true);
    	}
    
    	private void LoadAllItems ()
    	{
    		Hyena.Data.Sqlite.IDataReader reader = Database.Query ("SELECT id, job_type, job_options, run_at, job_priority FROM jobs");
    
    		Scheduler.Suspend ();
    		while (reader.Read ()) {
    			Job job = LoadItem (reader);
    			AddToCache (job);
    			job.Finished += HandleRemoveJob;
    			Scheduler.Schedule (job, job.JobPriority);
    			job.Status = JobStatus.Scheduled;
    		}
    
    		reader.Dispose ();
    	}
    
    	public Job Create (Type job_type, string job_options)
    	{
    		return Create (job_type, job_options, DateTime.Now, JobPriority.Lowest, false);
    	}
    
    	public Job CreatePersistent (Type job_type, string job_options)
    	{
    		return Create (job_type, job_options, DateTime.Now, JobPriority.Lowest, true);
    	}
    
    	internal Job Create (Type job_type, string job_options, DateTime run_at, JobPriority job_priority, bool persistent)
    	{
			long id = 0;
    		if (persistent)
    			id = Database.Execute (new HyenaSqliteCommand ("INSERT INTO jobs (job_type, job_options, run_at, job_priority) VALUES (?, ?, ?, ?)",
    						job_type.ToString (),
    						job_options,
    						DateTimeUtil.FromDateTime (run_at),
    						Convert.ToInt32 (job_priority)));
    
			Job job = (Job)Activator.CreateInstance (job_type, Db, (uint) id, job_options, run_at, job_priority, true);
    
    		AddToCache (job);
    		job.Finished += HandleRemoveJob;
    		Scheduler.Schedule (job, job.JobPriority);
    		job.Status = JobStatus.Scheduled;
    		EmitAdded (job);
    
    		return job;
    	}
    
    	public override void Commit (Job item)
    	{
    		if (item.Persistent)
    			Database.Execute(new HyenaSqliteCommand("UPDATE jobs " 					+
    									"SET job_type = ? "		+
    									"SET job_options = ? "	+
    									"SET run_at = ? "			+
    									"SET job_priority = ? "	+
    									"WHERE id = ?",
    									"Empty", //FIXME
    									item.JobOptions,
    									DateTimeUtil.FromDateTime (item.RunAt),
    									item.JobPriority,
    									item.Id));
    
    		EmitChanged (item);
    	}
    
    	public override Job Get (uint id)
    	{
                // we never use this
                return null;
    	}
    
    	public override void Remove (Job item)
    	{
    		RemoveFromCache (item);
    
    		if (item.Persistent)
    			Database.Execute (new HyenaSqliteCommand ("DELETE FROM jobs WHERE id = ?", item.Id));
    
    		EmitRemoved (item);
    	}
    
    	public void HandleRemoveJob (Object o, EventArgs e)
    	{
    		Remove (o as Job);
    	}
    
		public JobStore (IDb db, bool is_new) : base (db, true)
    	{
    		if (is_new || !Database.TableExists ("jobs")) {
    			CreateTable (Database);
    		} else {
    			LoadAllItems ();
            }
    	}
    }
}
