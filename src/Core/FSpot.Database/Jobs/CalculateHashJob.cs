//
// CalculateHashJob.cs
//
// Author:
//   Thomas Van Machelen <thomasvm@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Thomas Van Machelen
// Copyright (C) 2008, 2010 Ruben Vermeersch
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

using FSpot.Database;

using Hyena;

namespace FSpot.Database.Jobs {
	public class CalculateHashJob : Job
	{
		public CalculateHashJob (IDb db, uint id, string job_options, int run_at, JobPriority job_priority, bool persistent)
			: this (db, id, job_options, DateTimeUtil.ToDateTime (run_at), job_priority, persistent)
		{
		}

		public CalculateHashJob (IDb db, uint id, string job_options, DateTime run_at, JobPriority job_priority, bool persistent)
			: base (db, id, job_options, job_priority, run_at, persistent)
		{
		}

		public static CalculateHashJob Create (JobStore job_store, uint photo_id)
		{
			return (CalculateHashJob) job_store.CreatePersistent (typeof(CalculateHashJob), photo_id.ToString ());
		}

		protected override bool Execute ()
		{
			//this will add some more reactivity to the system
			System.Threading.Thread.Sleep (200);

			uint photo_id = Convert.ToUInt32 (JobOptions);
			Log.DebugFormat ("Calculating Hash {0}...", photo_id);

			try {
				Photo photo = Db.Photos.Get (Convert.ToUInt32 (photo_id));
				Db.Photos.CalculateMD5Sum (photo);
				return true;
			} catch (System.Exception e) {
				Log.DebugFormat ("Error Calculating Hash for photo {0}: {1}", JobOptions, e.Message);
			}
			return false;
		}
	}
}
