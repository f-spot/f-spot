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
using System.Threading.Tasks;

using Banshee.Kernel;

using FSpot.Core;

using Hyena;

namespace FSpot.Database.Jobs
{
	public class CalculateHashJob : Job
	{
		public CalculateHashJob (IDb db, uint id, string jobOptions, int runAt, JobPriority jobPriority, bool persistent)
			: this (db, id, jobOptions, DateTimeUtil.ToDateTime (runAt), jobPriority, persistent)
		{
		}

		public CalculateHashJob (IDb db, uint id, string jobOptions, DateTime runAt, JobPriority jobPriority, bool persistent)
			: base (db, id, jobOptions, jobPriority, runAt, persistent)
		{
		}

		public static CalculateHashJob Create (JobStore jobStore, uint photoId)
		{
			return (CalculateHashJob) jobStore.CreatePersistent (typeof(CalculateHashJob), photoId.ToString ());
		}

		protected override bool Execute ()
		{
			//this will add some more reactivity to the system
			Task.Delay (200);

			var photoId = Convert.ToUInt32 (JobOptions);
			Log.DebugFormat ($"Calculating Hash {photoId}...");

			try {
				Photo photo = Db.Photos.Get (Convert.ToUInt32 (photoId));
				Db.Photos.CalculateMD5Sum (photo);
				return true;
			} catch (Exception e) {
				Log.DebugFormat ($"Error Calculating Hash for photo {JobOptions}: {e.Message}");
			}
			return false;
		}
	}
}
