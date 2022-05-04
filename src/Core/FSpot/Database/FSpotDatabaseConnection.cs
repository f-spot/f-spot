//
// FSpotDatabaseConnection.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Hyena;
using Hyena.Data.Sqlite;

namespace FSpot.Database
{
	public class FSpotDatabaseConnection : HyenaSqliteConnection
	{
		public FSpotDatabaseConnection (string dbpath) : base (dbpath)
		{
			//Execute ("PRAGMA synchronous = OFF");
			//Execute ("PRAGMA temp_store = MEMORY");
			//Execute ("PRAGMA count_changes = OFF");

			if (ApplicationContext.CommandLine.Contains ("debug-sql")) {
				HyenaSqliteCommand.LogAll = true;
			}
		}
	}
}
