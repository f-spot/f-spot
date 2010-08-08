//
// FSpotDatabaseConnection.cs
//
// Author:
//   Mike Gemuende <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (c) 2010 Mike Gemuende <mike@gemuende.de>
// Copyright (c) 2010 Ruben Vermeersch <ruben@savanne.be>
//
// This is free software. See COPYING for details.
//

using System;

using Hyena.Data.Sqlite;

namespace FSpot.Database
{


    public class FSpotDatabaseConnection : HyenaSqliteConnection
    {

        public FSpotDatabaseConnection (string dbpath) : base(dbpath)
        {
            //Execute ("PRAGMA synchronous = OFF");
            //Execute ("PRAGMA temp_store = MEMORY");
            //Execute ("PRAGMA count_changes = OFF");
        }
    }
}
