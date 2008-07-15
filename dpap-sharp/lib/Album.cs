// Album.cs created with MonoDevelop
// User: andrzej at 11:41 2008-07-15
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DPAP
{
	public delegate void AlbumPhotoHandler (object o, int index, Photo track);
	
	public class Album
	{
		private static int nextid = 1;
        
        private int id;
        private string name = String.Empty;
        private List<Photo> photos = new List<Photo> ();
        private List<int> containerIds = new List<int> ();
		
        public event AlbumPhotoHandler PhotoAdded;
        public event AlbumPhotoHandler PhotoRemoved;
        public event EventHandler NameChanged;

        public Photo this[int index] {
            get {
                if (photos.Count > index)
                    return photos[index];
                else
                    return null;
            }
            set { photos[index] = value; }
        }
        
        public IList<Photo> Photos {
            get { return new ReadOnlyCollection<Photo> (photos); }
        }

        internal int Id {
            get { return id; }
            set { id = value; }
        }

        public string Name {
            get { return name; }
            set {
                name = value;
                if (NameChanged != null)
                    NameChanged (this, new EventArgs ());
            }
        }

        internal Album () {
            id = nextid++;
        }
		
        public Album (string name) : this () {
            this.name = name;
        }

        public void InsertPhoto (int index, Photo photo) {
            InsertPhoto (index, photo, photos.Count + 1);
        }

        internal void InsertPhoto (int index, Photo photo, int id) {
            photos.Insert (index, photo);
            containerIds.Insert (index, id);

            if (PhotoAdded != null)
                PhotoAdded (this, index, photo);
        }

        public void Clear () {
            photos.Clear ();
        }

        public void AddPhoto (Photo photo) {
            AddPhoto (photo, photos.Count + 1);
        }
        
        internal void AddPhoto (Photo photo, int id) {
            photos.Add (photo);
            containerIds.Add (id);

            if (PhotoAdded != null)
                PhotoAdded (this, photos.Count - 1, photo);
        }

        public void RemoveAt (int index) {
            Photo photo = (Photo) photos[index];
            photos.RemoveAt (index);
            containerIds.RemoveAt (index);
            
            if (PhotoRemoved != null)
                PhotoRemoved (this, index, photo);
        }

        public bool RemovePhoto (Photo photo) {
            int index;
            bool ret = false;
            
            while ((index = IndexOf (photo)) >= 0) {
                ret = true;
                RemoveAt (index);
            }

            return ret;
        }

        public int IndexOf (Photo photo) {
            return photos.IndexOf (photo);
        }

        internal int GetContainerId (int index) {
            return (int) containerIds[index];
        }

        internal ContentNode ToPhotosNode (int[] deletedIds) {
            ArrayList photoNodes = new ArrayList ();

            for (int i = 0; i < photos.Count; i++) {
                Photo photo = photos[i] as Photo;
                photoNodes.Add (photo.ToAlbumNode ((int) containerIds[i]));
            }

            ArrayList deletedNodes = null;
            if (deletedIds.Length > 0) {
                deletedNodes = new ArrayList ();

                foreach (int id in deletedIds) {
                    deletedNodes.Add (new ContentNode ("dmap.itemid", id));
                }
            }

            ArrayList children = new ArrayList ();
            children.Add (new ContentNode ("dmap.status", 200));
            children.Add (new ContentNode ("dmap.updatetype", deletedNodes == null ? (byte) 0 : (byte) 1));
            children.Add (new ContentNode ("dmap.specifiedtotalcount", photos.Count));
            children.Add (new ContentNode ("dmap.returnedcount", photos.Count));
            children.Add (new ContentNode ("dmap.listing", photoNodes));

            if (deletedNodes != null)
                children.Add (new ContentNode ("dmap.deletedidlisting", deletedNodes));
            
            
            return new ContentNode ("dpap.playlistsongs", children);
        }

        internal ContentNode ToNode (bool baseAlbum) {

            ArrayList nodes = new ArrayList ();

            nodes.Add (new ContentNode ("dmap.itemid", id));
            nodes.Add (new ContentNode ("dmap.persistentid", (long) id));
            nodes.Add (new ContentNode ("dmap.itemname", name));
            nodes.Add (new ContentNode ("dmap.itemcount", photos.Count));
            if (baseAlbum)
                nodes.Add (new ContentNode ("dpap.baseplaylist", (byte) 1));
            
            return new ContentNode ("dmap.listingitem", nodes);
        }

        internal static Album FromNode (ContentNode node) {
            Album pl = new Album ();

            foreach (ContentNode child in (ContentNode[]) node.Value) {
                switch (child.Name) {
                case  "dpap.baseplaylist":
                    return null;
                case "dmap.itemid":
                    pl.Id = (int) child.Value;
                    break;
                case "dmap.itemname":
                    pl.Name = (string) child.Value;
                    break;
                default:
                    break;
                }
            }

            return pl;
        }

        internal void Update (Album pl) {
            if (pl.Name == name)
                return;

            Name = pl.Name;
        }

        internal int LookupIndexByContainerId (int id) {
            return containerIds.IndexOf (id);
        }

		public int getId() {
			return Id;
		}
    }

}
