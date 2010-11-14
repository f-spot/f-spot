/*
 * JobStore.cs
 *
 * Author(s):
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using System.Collections;
using System.IO;
using System;
using Banshee.Kernel;
using FSpot.Jobs;
using FSpot.Database;
using FSpot;
using FSpot.Core;
using Hyena;
using Hyena.Data.Sqlite;

namespace FSpot {
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

	private Job LoadItem (IDataReader reader)
	{
		return (Job) Activator.CreateInstance (
				Type.GetType (reader ["job_type"].ToString ()),
				Convert.ToUInt32 (reader["id"]),
				reader["job_options"].ToString (),
				Convert.ToInt32 (reader["run_at"]),
				(JobPriority) Convert.ToInt32 (reader["job_priority"]),
				true);
	}

	private void LoadAllItems ()
	{
		IDataReader reader = Database.Query ("SELECT id, job_type, job_options, run_at, job_priority FROM jobs");

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
		int id = 0;
		if (persistent)
			id = Database.Execute (new HyenaSqliteCommand ("INSERT INTO jobs (job_type, job_options, run_at, job_priority) VALUES (?, ?, ?, ?)",
						job_type.ToString (),
						job_options,
						DateTimeUtil.FromDateTime (run_at),
						Convert.ToInt32 (job_priority)));

                Job job = (Job) Activator.CreateInstance (job_type, (uint)id, job_options, run_at, job_priority, true);

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

		if ((item as Job).Persistent)
			Database.Execute (new HyenaSqliteCommand ("DELETE FROM jobs WHERE id = ?", item.Id));

		EmitRemoved (item);
	}

	public void HandleRemoveJob (Object o, EventArgs e)
	{
		Remove (o as Job);
	}

	public JobStore (FSpotDatabaseConnection database, bool is_new) : base (database, true)
	{
		if (is_new || !Database.TableExists ("jobs")) {
			CreateTable (database);
		} else {
			LoadAllItems ();
                }
	}
}
}