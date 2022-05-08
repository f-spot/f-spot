// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2006 Gabriel Burt
// Copyright (C) 2009-2010 Ruben Vermeersch
// Copyright (C) 2022 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using FSpot.Models;
using FSpot.Settings;

namespace FSpot.Database
{
	public class MetaStore : DbStore<Meta>
	{
		public const string Version = "F-Spot Version";
		public const string Hidden = "Hidden Tag Id";

		public Meta FSpotVersion {
			get { return GetByName (Version); }
		}

		public Meta HiddenTagId {
			get { return GetByName (Hidden); }
		}

		public MetaStore ()
		{
			VerifyStoredVersion ();
		}

		Meta GetByName (string name)
		{
			using var context = new FSpotContext ();
			var item = context.Meta.First (x => string.Equals (x.Name, name));

			if (item != null)
				return item;

			// Otherwise make it and return it
			return Create (name, null);
		}

		void VerifyStoredVersion ()
		{
			if (FSpotVersion.Data != FSpotConfiguration.Version) {
				FSpotVersion.Data = FSpotConfiguration.Version;
				Commit (FSpotVersion);
			}
		}

		Meta Create (string name, string data)
		{
			using var context = new FSpotContext ();
			var item = new Meta { Name = name, Data = data };
			var result = context.Add (item);
			//uint id = (uint)Database.Execute (new HyenaSqliteCommand ("INSERT INTO meta (name, data) VALUES (?, ?)", name, data ?? "NULL"));

			context.SaveChanges ();
			//FIXME This smells bad. This line used to be *before* the
			//Command.executeNonQuery. It smells of a bug, but there might
			//have been a reason for this

			EmitAdded (item);

			return item;
		}

		public override void Commit (Meta item)
		{
			using var context = new FSpotContext ();
			context.Update (item);
			context.SaveChanges ();

			EmitChanged (item);
		}

		public override Meta Get (Guid id)
		{
			using var context = new FSpotContext ();
			var item = context.Meta.Find (id);
			return item;
		}

		public override void Remove (Meta item)
		{
			using var context = new FSpotContext ();
			context.Remove (item);

			EmitRemoved (item);
		}
	}
}
