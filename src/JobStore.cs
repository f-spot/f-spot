/*
 * JobStore.cs
 *
 * Author(s):
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using Mono.Data.SqliteClient;
using System.Collections;
using System.IO;
using System;
using Banshee.Database;
using Banshee.Kernel;
using FSpot.Jobs;
using FSpot.Utils;
using FSpot;

public abstract class Job : DbItem, IJob
{
	public Job (uint id, string job_options, JobPriority job_priority, DateTime run_at, bool persistent) : base (id)
	{
		this.job_options = job_options;
		this.job_priority = job_priority;
		this.run_at = run_at;
		this.persistent = persistent;
	}

	private string job_options;
	public string JobOptions {
		get { return job_options; }
		set { job_options = value; }
	}

	private JobPriority job_priority;
	internal JobPriority JobPriority {
		get { return job_priority; }
		set { job_priority = value; }
	}

	//Not in use yet !
	private DateTime run_at;
	public DateTime RunAt {
		get { return run_at; }
//		set { run_at = value; }
	}

	private bool persistent;
	public bool Persistent {
		get { return persistent; }
	}

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

public class JobStore : DbStore {
	
	private void CreateTable ()
	{
		Database.ExecuteNonQuery (
			@"CREATE TABLE jobs (
				id		 INTEGER PRIMARY KEY NOT NULL,
				job_type         TEXT NOT NULL,
                                job_options      TEXT NOT NULL,
                                run_at           INTEGER,
				job_priority     INTEGER NOT NULL
			)");
	}

	private Job LoadItem (SqliteDataReader reader)
	{
		return (Job) Activator.CreateInstance (
				Type.GetType (reader [1].ToString ()), 
				Convert.ToUInt32 (reader[0]), 
				reader[2].ToString (), 
				Convert.ToInt32 (reader[3]), 
				(JobPriority) Convert.ToInt32 (reader[4]),
				true);
	}
	
	private void LoadAllItems ()
	{
		SqliteDataReader reader = Database.Query ("SELECT id, job_type, job_options, run_at, job_priority FROM jobs");

		Scheduler.Suspend ();
		while (reader.Read ()) {
			Job job = LoadItem (reader);
			AddToCache (job);
			job.Finished += HandleRemoveJob;
			Scheduler.Schedule (job, job.JobPriority);
			job.Status = JobStatus.Scheduled;
		}

		reader.Close ();
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
		int id = 0;
		if (persistent)
			id = Database.Execute (new DbCommand ("INSERT INTO jobs (job_type, job_options, run_at, job_priority) VALUES (:job_type, :job_options, :run_at, :job_priority)",
						"job_type", job_type.ToString (), 
						"job_options", job_options, 
						"run_at", DbUtils.UnixTimeFromDateTime (run_at),
						"job_priority", Convert.ToInt32 (job_priority)));
		
                Job job = (Job) Activator.CreateInstance (job_type, (uint)id, job_options, run_at, job_priority, true);

		AddToCache (job);
		job.Finished += HandleRemoveJob;
		Scheduler.Schedule (job, job.JobPriority);
		job.Status = JobStatus.Scheduled;
		EmitAdded (job);

		return job;
	}
	
	public override void Commit (DbItem dbitem)
	{
		Job item = dbitem as Job;

		if (item.Persistent)
			Database.ExecuteNonQuery(new DbCommand("UPDATE jobs " 					+
									"SET job_type = :job_type "		+
									"SET job_options = :job_options "	+
									"SET run_at = :run_at "			+
									"SET job_priority = :job_priority "	+
									"WHERE id = :item_id", 
									"job_type", "Empty", //FIXME
									"job_options", item.JobOptions,
									"run_at", DbUtils.UnixTimeFromDateTime (item.RunAt),
									"job_priority", item.JobPriority));
		
		EmitChanged (item);
	}
	
	public override DbItem Get (uint id)
	{
            // we never use this
            return null;
	}

	public override void Remove (DbItem item)
	{
		RemoveFromCache (item);

		if ((item as Job).Persistent)
			Database.ExecuteNonQuery (new DbCommand ("DELETE FROM jobs WHERE id = :item_id", "item_id", item.Id));

		EmitRemoved (item);
	}

	public void HandleRemoveJob (object o, EventArgs e)
	{
		Remove (o as DbItem);
	}

	public JobStore (QueuedSqliteDatabase database, bool is_new) : base (database, true)
	{
		if (is_new || !Database.TableExists ("jobs")) {
			CreateTable ();
		} else {
			LoadAllItems ();
                }
	}
}
