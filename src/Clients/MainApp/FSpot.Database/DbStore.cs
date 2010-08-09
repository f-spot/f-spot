using System;
using System.Collections.Generic;
using Hyena;
using FSpot.Core;

namespace FSpot.Database
{
    public abstract class DbStore<T> where T : DbItem
    {
        // DbItem cache.

        public event EventHandler<DbItemEventArgs<T>> ItemsAdded;
        public event EventHandler<DbItemEventArgs<T>> ItemsRemoved;
        public event EventHandler<DbItemEventArgs<T>> ItemsChanged;

        protected Dictionary<uint, object> item_cache;
        bool cache_is_immortal;

        protected void AddToCache (T item)
        {
            if (item_cache.ContainsKey (item.Id))
                item_cache.Remove (item.Id);
            
            if (cache_is_immortal)
                item_cache.Add (item.Id, item);
            else
                item_cache.Add (item.Id, new WeakReference (item));
        }

        protected T LookupInCache (uint id)
        {
            if (!item_cache.ContainsKey (id))
                return null;
            
            if (cache_is_immortal)
                return item_cache[id] as T;
            
            WeakReference weakref = item_cache[id] as WeakReference;
            return (T)weakref.Target;
        }

        protected void RemoveFromCache (T item)
        {
            item_cache.Remove (item.Id);
        }

        protected void EmitAdded (T item)
        {
            EmitAdded (new T[] { item });
        }

        protected void EmitAdded (T[] items)
        {
            EmitEvent (ItemsAdded, new DbItemEventArgs<T> (items));
        }

        protected void EmitChanged (T item)
        {
            EmitChanged (new T[] { item });
        }

        protected void EmitChanged (T[] items)
        {
            EmitChanged (items, new DbItemEventArgs<T> (items));
        }

        protected void EmitChanged (T[] items, DbItemEventArgs<T> args)
        {
            EmitEvent (ItemsChanged, args);
        }

        protected void EmitRemoved (T item)
        {
            EmitRemoved (new T[] { item });
        }

        protected void EmitRemoved (T[] items)
        {
            EmitEvent (ItemsRemoved, new DbItemEventArgs<T> (items));
        }

        private void EmitEvent (EventHandler<DbItemEventArgs<T>> evnt, DbItemEventArgs<T> args)
        {
            if (evnt == null)
                // No subscribers.
                return;
            
            ThreadAssist.ProxyToMain (() => { evnt (this, args); });
        }

        public bool CacheEmpty {
            get { return item_cache.Count == 0; }
        }


        FSpotDatabaseConnection database;
        protected FSpotDatabaseConnection Database {
            get { return database; }
        }


        // Constructor.

        public DbStore (FSpotDatabaseConnection database, bool cache_is_immortal)
        {
            this.database = database;
            this.cache_is_immortal = cache_is_immortal;
            
            item_cache = new Dictionary<uint, object> ();
        }


        // Abstract methods.

        public abstract T Get (uint id);
        public abstract void Remove (T item);
        // If you have made changes to "obj", you have to invoke Commit() to have the changes
        // saved into the database.
        public abstract void Commit (T item);
    }
}
