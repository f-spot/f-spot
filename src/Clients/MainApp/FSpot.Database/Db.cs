using System.IO;
using System;
using Hyena;

// A Store maps to a SQL table.  We have separate stores (i.e. SQL tables) for tags, photos and imports.

namespace FSpot.Database
{

// The Database puts the stores together.

    public class Db : IDisposable
    {

        TagStore tag_store;
        PhotoStore photo_store;
        RollStore roll_store;
        ExportStore export_store;
        JobStore job_store;
        MetaStore meta_store;
        bool empty;
        string path;

        public TagStore Tags {
            get { return tag_store; }
        }

        public RollStore Rolls {
            get { return roll_store; }
        }

        public ExportStore Exports {
            get { return export_store; }
        }

        public JobStore Jobs {
            get { return job_store; }
        }

        public PhotoStore Photos {
            get { return photo_store; }
        }

        public MetaStore Meta {
            get { return meta_store; }
        }

        // This affects how often the database writes data back to disk, and
        // therefore how likely corruption is in the event of power loss.
        public bool Sync {
            set {
                string query = "PRAGMA synchronous = " + (value ? "ON" : "OFF");
                Database.Execute (query);
            }
        }

        FSpotDatabaseConnection database;
        public FSpotDatabaseConnection Database {
            get { return database; }
        }


        public string Repair ()
        {
            string backup_path = path;
            int i = 0;
            
            while (File.Exists (backup_path)) {
                backup_path = String.Format ("{0}-{1}-{2}{3}", Path.GetFileNameWithoutExtension (path), System.DateTime.Now.ToString ("yyyyMMdd"), i++, Path.GetExtension (path));
            }
            
            File.Move (path, backup_path);
            Init (path, true);
            
            return backup_path;
        }

        public void Init (string path, bool create_if_missing)
        {
            uint timer = Log.DebugTimerStart ();
            bool new_db = !File.Exists (path);
            this.path = path;
            
            if (new_db && !create_if_missing)
                throw new Exception (path + ": File not found");
            
            database = new FSpotDatabaseConnection (path);
            
            // Load or create the meta table
            meta_store = new MetaStore (Database, new_db);
            
            // Update the database schema if necessary
            FSpot.Database.Updater.Run (Database);
            
            Database.BeginTransaction ();
            
            tag_store = new TagStore (Database, new_db);
            roll_store = new RollStore (Database, new_db);
            export_store = new ExportStore (Database, new_db);
            job_store = new JobStore (Database, new_db);
            photo_store = new PhotoStore (Database, new_db);
            
            Database.CommitTransaction ();
            
            empty = new_db;
            Log.DebugTimerPrint (timer, "Db Initialization took {0}");
        }

        public bool Empty {
            get { return empty; }
        }

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        bool already_disposed = false;
        protected virtual void Dispose (bool is_disposing)
        {
            if (already_disposed)
                return;
            if (is_disposing) {
                //Free managed resources
                Database.Dispose ();
            }
            //Free unmanaged resources
            
            already_disposed = true;
        }

        ~Db ()
        {
            Log.DebugFormat ("Finalizer called on {0}. Should be Disposed", GetType ());
            Dispose (false);
        }

        public void BeginTransaction ()
        {
            Database.BeginTransaction ();
        }

        public void CommitTransaction ()
        {
            Database.CommitTransaction ();
        }

        public void RollbackTransaction ()
        {
            Database.RollbackTransaction ();
        }
    }
}
