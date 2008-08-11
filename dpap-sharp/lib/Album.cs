// Album.cs
//
// Authors:
//   Andrzej Wytyczak-Partyka <iapart@gmail.com>
//   James Willcox <snorp@snorp.net>
//
// Copyright (C) 2008 Andrzej Wytyczak-Partyka
// Copyright (C) 2005  James Willcox <snorp@snorp.net>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
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
        private List<int> container_ids = new List<int> ();
		
        public event AlbumPhotoHandler PhotoAdded;
        public event AlbumPhotoHandler PhotoRemoved;
        public event EventHandler NameChanged;

        public Photo this [int index] {
            get {
                if (photos.Count > index)
                    return photos [index];
                else
                    return null;
            }
            set { photos [index] = value; }
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
            container_ids.Insert (index, id);

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
            container_ids.Add (id);

            if (PhotoAdded != null)
                PhotoAdded (this, photos.Count - 1, photo);
        }

        public void RemoveAt (int index) {
            Photo photo = (Photo) photos [index];
            photos.RemoveAt (index);
            container_ids.RemoveAt (index);
            
            if (PhotoRemoved != null)
                PhotoRemoved (this, index, photo);
        }

        public bool RemovePhoto (Photo photo) {
            int index;
            bool ret = false;
            
            while ( (index = IndexOf (photo)) >= 0) {
                ret = true;
                RemoveAt (index);
            }

            return ret;
        }

        public int IndexOf (Photo photo) {
            return photos.IndexOf (photo);
        }

        internal int GetContainerId (int index) {
            return (int) container_ids [index];
        }

        internal ContentNode ToPhotosNode (string [] fields) {
            ArrayList photo_nodes = new ArrayList ();

            for (int i = 0; i < photos.Count; i++) {
                Photo photo = photos [i] as Photo;
				photo_nodes.Add (photo.ToAlbumNode (fields));
                //photo_nodes.Add (photo.ToAlbumsNode ( (int) container_ids [i]));
            }

            /*ArrayList deletedNodes = null;
            if (deletedIds.Length > 0) {
                deletedNodes = new ArrayList ();

                foreach (int id in deletedIds) {
                    deletedNodes.Add (new ContentNode ("dmap.itemid", id));
                }
            }
*/
            ArrayList children = new ArrayList ();
            children.Add (new ContentNode ("dmap.status", 200));
            children.Add (new ContentNode ("dmap.updatetype", (byte) 0));
            children.Add (new ContentNode ("dmap.specifiedtotalcount", photos.Count));
            children.Add (new ContentNode ("dmap.returnedcount", photos.Count));
            children.Add (new ContentNode ("dmap.listing", photo_nodes));

  //          if (deletedNodes != null)
    //            children.Add (new ContentNode ("dmap.deletedidlisting", deletedNodes));
            
            
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

            foreach (ContentNode child in (ContentNode []) node.Value) {
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
            return container_ids.IndexOf (id);
        }

		public int getId () {
			return Id;
		}
    }

}
