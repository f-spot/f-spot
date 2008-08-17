// Database.cs
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
using System.Net;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace DPAP {

    public delegate void PhotoHandler (object o, PhotoArgs args);

    public class PhotoArgs : EventArgs {
        private Photo photo;

        public Photo Photo {
            get { return photo; }
        }
        
        public PhotoArgs (Photo photo) {
            this.photo = photo;
        }
    }
        
    public delegate void AlbumHandler (object o, AlbumArgs args);

    public class AlbumArgs : EventArgs {
        private Album pl;

        public Album Album {
            get { return pl; }
        }
        
        public AlbumArgs (Album pl) {
            this.pl = pl;
        }
    }

    public class Database : ICloneable {

        private const int chunk_length = 8192;
		
        private const string photo_query = "meta=dpap.aspectratio,dmap.itemid,dmap.itemname,dpap.imagefilename," +
			"dpap.imagefilesize,dpap.creationdate,dpap.imagepixelwidth," +
			"dpap.imagepixelheight,dpap.imageformat,dpap.imagerating," +
			"dpap.imagecomments,dpap.imagelargefilesize,dpap.filedata&type=photo"; 
			

        private static int nextid = 1;
        private Client client;
        private int id;
        private long persistent_id;
        private string name;

        private List<Photo> photos = new List<Photo> ();
        private List<Album> albums = new List<Album> ();
        private Album base_album = new Album ();
        private int next_photo_id = 1;

        public event PhotoHandler PhotoAdded;
        public event PhotoHandler PhotoRemoved;
        public event AlbumHandler AlbumAdded;
        public event AlbumHandler AlbumRemoved;

        public int Id {
            get { return id; }
        }

        public string Name {
            get { return name; }
            set {
                name = value;
                base_album.Name = value;
            }
        }
        
        public IList<Photo> Photos {
            get {
                return new ReadOnlyCollection<Photo> (photos);
            }
        }
        
        public int PhotoCount {
            get { return photos.Count; }
        }

        public Photo PhotoAt (int index)
        {
            return photos [index] as Photo;
        }

        public IList<Album> Albums {
            get {
                return new ReadOnlyCollection<Album> (albums);
            }
        }

        internal Client Client {
            get { return client; }
        }

        private Database () {
            this.id = nextid++;
        }

        public Database (string name) : this () {
            this.Name = name;
        }

        internal Database (Client client, ContentNode dbNode) : this () {
            this.client = client;

            Parse (dbNode);
        }

        private void Parse (ContentNode dbNode) {
            foreach (ContentNode item in (ContentNode []) dbNode.Value) {

                switch (item.Name) {
                case "dmap.itemid":
                    id = (int) item.Value;
                    break;
                case "dmap.persistentid":
                    persistent_id = (long) item.Value;
                    break;
                case "dmap.itemname":
                    name = (string) item.Value;
                    break;
                default:
                    break;
                }
            }
        }

        public Photo LookupPhotoById (int id) {
            foreach (Photo photo in photos) {
                if (photo.Id == id)
                    return photo;
            }

            return null;
        }

        public Album LookupAlbumById (int id) {
            if (id == base_album.Id)
                return base_album;

            foreach (Album pl in albums) {
                if (pl.Id == id)
                    return pl;
            }

            return null;
        }

        internal ContentNode ToPhotosNode (string [] fields, int [] deleted_ids) {

            ArrayList photo_nodes = new ArrayList ();
            foreach (Photo photo in photos) {
                photo_nodes.Add (photo.ToNode (fields));
            }

            ArrayList deleted_nodes = null;

            if (deleted_ids.Length > 0) {
                deleted_nodes = new ArrayList ();
                
                foreach (int id in deleted_ids) {
                    deleted_nodes.Add (new ContentNode ("dmap.itemid", id));
                }
            }

            ArrayList children = new ArrayList ();
            children.Add (new ContentNode ("dmap.status", 200));
            children.Add (new ContentNode ("dmap.updatetype", deleted_nodes == null ? (byte) 0 : (byte) 1));
            children.Add (new ContentNode ("dmap.specifiedtotalcount", photos.Count));
            children.Add (new ContentNode ("dmap.returnedcount", photos.Count));
            children.Add (new ContentNode ("dmap.listing", photo_nodes));

            if (deleted_nodes != null) {
                children.Add (new ContentNode ("dmap.deletedidlisting", deleted_nodes));
            }
            
            return new ContentNode ("dpap.databasesongs", children);
        }

        internal ContentNode ToAlbumsNode () {
            ArrayList nodes = new ArrayList ();

            nodes.Add (base_album.ToNode (true));
            
            foreach (Album pl in albums) {
                nodes.Add (pl.ToNode (false));
            }

            return new ContentNode ("dpap.databasecontainers",
                                    new ContentNode ("dmap.status", 200),
                                    new ContentNode ("dmap.updatetype", (byte) 0),
                                    new ContentNode ("dmap.specifiedtotalcount", nodes.Count),
                                    new ContentNode ("dmap.returnedcount", nodes.Count),
                                    new ContentNode ("dmap.listing", nodes));
        }

        internal ContentNode ToDatabaseNode () {
            return new ContentNode ("dmap.listingitem",
                                    new ContentNode ("dmap.itemid", id),
                                    new ContentNode ("dmap.persistentid", (long) id),
                                    new ContentNode ("dmap.itemname", name),
                                    new ContentNode ("dmap.itemcount", photos.Count),
                                    new ContentNode ("dmap.containercount", albums.Count + 1));
        }

        public void Clear () {
            if (client != null)
                throw new InvalidOperationException ("cannot clear client databases");

            ClearAlbums ();
            ClearPhotos ();
        }

        private void ClearAlbums () {
            foreach (Album pl in new List<Album> (albums)) {
                RemoveAlbum (pl);
            }
        }

        private void ClearPhotos () {
            foreach (Photo photo in new List<Photo> (photos)) {
                RemovePhoto (photo);
            }
        }

        private bool IsUpdateResponse (ContentNode node) {
            return node.Name == "dmap.updateresponse";
        }

        private void RefreshAlbums (string revquery) {
            byte [] albums_data;

            try {
                albums_data = client.Fetcher.Fetch (String.Format ("/databases/{0}/containers", id), "meta=dpap.aspectratio,dmap.itemid,dmap.itemname,dpap.imagefilename,dpap.imagefilesize,dpap.creationdate,dpap.imagepixelwidth,dpap.imagepixelheight,dpap.imageformat,dpap.imagerating,dpap.imagecomments,dpap.imagelargefilesize&type=photo");
            } catch (WebException) {
                return;
            }
            
            ContentNode albums_node = ContentParser.Parse (client.Bag, albums_data);
			// DEBUG data			
			albums_node.Dump ();
			Console.WriteLine ("after dump!");
			
            if (IsUpdateResponse (albums_node))
                return;

            // handle album additions/changes
            ArrayList plids = new ArrayList ();
			if (albums_node.GetChild ("dmap.listing")==null) return;
			
            foreach (ContentNode albumNode in (ContentNode []) albums_node.GetChild ("dmap.listing").Value) {
                
				// DEBUG
				Console.WriteLine ("foreach loop");
				Album pl = Album.FromNode (albumNode);
                if (pl != null) {
                    plids.Add (pl.Id);
                    Album existing = LookupAlbumById (pl.Id);

                    if (existing == null) {
                        AddAlbum (pl);
                    } else {
                        existing.Update (pl);
                    }
                }
            }
			// DEBUG
			Console.WriteLine ("delete albums that don't exist");
            // delete albums that no longer exist
            foreach (Album pl in new List<Album> (albums)) {
                if (!plids.Contains (pl.Id)) {
                    RemoveAlbum (pl);
                }
            }

            plids = null;
			// DEBUG
			Console.WriteLine ("Add/remove photos in the albums");
            // add/remove photos in the albums
            foreach (Album pl in albums) {
                byte [] album_photos_data = client.Fetcher.Fetch (String.Format ("/databases/{0}/containers/{1}/items",
                                                                                id, pl.Id), "meta=dpap.aspectratio,dmap.itemid,dmap.itemname,dpap.imagefilename,dpap.imagefilesize,dpap.creationdate,dpap.imagepixelwidth,dpap.imagepixelheight,dpap.imageformat,dpap.imagerating,dpap.imagecomments,dpap.imagelargefilesize&type=photo");
                ContentNode album_photos_node = ContentParser.Parse (client.Bag, album_photos_data);

                if (IsUpdateResponse (album_photos_node))
                    return;

                if ( (byte) album_photos_node.GetChild ("dmap.updatetype").Value == 1) {

                    // handle album photo deletions
                    ContentNode delete_list = album_photos_node.GetChild ("dmap.deletedidlisting");

                    if (delete_list != null) {
                        foreach (ContentNode deleted in (ContentNode []) delete_list.Value) {
                            int index = pl.LookupIndexByContainerId ( (int) deleted.Value);

                            if (index < 0)
                                continue;

                            pl.RemoveAt (index);
                        }
                    }
                }

                // add new photos, or reorder existing ones

                int plindex = 0;
                foreach (ContentNode pl_photo_node in (ContentNode []) album_photos_node.GetChild ("dmap.listing").Value) {
                    Photo plphoto = null;
                    int container_id = 0;
                    Photo.FromAlbumNode (this, pl_photo_node, out plphoto, out container_id);

                    if (pl [plindex] != null && pl.GetContainerId (plindex) != container_id) {
                        pl.RemoveAt (plindex);
                        pl.InsertPhoto (plindex, plphoto, container_id);
                    } else if (pl [plindex] == null) {
                        pl.InsertPhoto (plindex, plphoto, container_id);
                    }

                    plindex++;
                }
            }
        }

        private void RefreshPhotos (string revquery) {
			foreach (Album pl in albums){
				//Console.WriteLine ("Refreshing photos in album " + pl.Name);
	            byte [] photos_data = client.Fetcher.Fetch (String.Format ("/databases/{0}/containers/{1}/items", id,pl.getId ()),
	                                                     photo_query);
	            ContentNode photos_node = ContentParser.Parse (client.Bag, photos_data);
				//photos_node.Dump ();
	            //if (IsUpdateResponse (photos_node))
	             //   return;

	            // handle photo additions/changes
	            foreach (ContentNode photoNode in (ContentNode []) photos_node.GetChild ("dmap.listing").Value) {
					// DEBUG data					
					//photoNode.Dump ();
	                Photo photo = Photo.FromNode (photoNode);
					
	                Photo existing = LookupPhotoById (photo.Id);
					pl.AddPhoto (photo);
	                if (existing == null){
						Console.WriteLine ("adding " + photo.Title + " to album " +pl.Name);
					
	                    AddPhoto (photo);
					}
	                else
					{
						Console.WriteLine ("updating " + existing.Title);
	                    existing.Update (photo);
					}
	            }

	            if ( (byte) photos_node.GetChild ("dmap.updatetype").Value == 1) {

	                // handle photo deletions
	                ContentNode delete_list = photos_node.GetChild ("dmap.deletedidlisting");

	                if (delete_list != null) {
	                    foreach (ContentNode deleted in (ContentNode []) delete_list.Value) {
	                        Photo photo = LookupPhotoById ( (int) deleted.Value);

	                        if (photo != null)
	                            RemovePhoto (photo);
	                    }
	                }
				}
			}
        }

        internal void Refresh (int newrev) {
            if (client == null)
                throw new InvalidOperationException ("cannot refresh server databases");

            string revquery = null;

            if (client.Revision != 0)
                revquery = String.Format ("revision-number={0}&delta={1}", newrev, newrev - client.Revision);
            
            RefreshAlbums ("");
			RefreshPhotos ("");
        }

        private HttpWebResponse FetchPhoto (Photo photo, long offset) {
            return client.Fetcher.FetchResponse (String.Format ("/databases/{0}/items",id), offset, 
			                                     String.Format ("meta=dpap.filedata&query=('dmap.itemid:{0}')",photo.Id),
			                                     null, 1, true);
                                             
        }

        public Stream StreamPhoto (Photo photo, out long length) {
            return StreamPhoto (photo, -1, out length);
        }
        
        public Stream StreamPhoto (Photo photo, long offset, out long length) {
            HttpWebResponse response = FetchPhoto (photo, offset);
            length = response.ContentLength;
            return response.GetResponseStream ();
        }

        public void DownloadPhoto (Photo photo, string dest) {

/*            BinaryWriter writer = new BinaryWriter (File.Open (dest, FileMode.Create));
			MemoryStream data = new MemoryStream ();
            try {
                long len;
                using (BinaryReader reader = new BinaryReader (StreamPhoto (photo, out len))) {
                    int count = 0;
                    byte [] buf = new byte [chunk_length];
                
					// Skip the header    
					//count = reader.Read (buf,0,89);
					
					//if (count < 89)
					//	count+=reader.Read (buf,0,89-count);
					
					while (true) {
                    buf = reader.ReadBytes (8192);
                    if (buf.Length == 0)
                        break;

                    data.Write (buf, 0, buf.Length);
					//Console.Write (buf.);
					}
					
			*/		
/*                    do {
                        count = reader.Read (buf, 0, chunk_length);
                        writer.Write (buf, 0, count);
						data.Write (buf, 0, count);
                    } while (count != 0);
	*/				
					/*data.Flush ();
					
					ContentNode node = ContentParser.Parse (client.Bag, data.GetBuffer ());
					node.Dump ();
					reader.Close ();
					
                }
            } finally {
				data.Close ();
                
                writer.Close ();
            }*/
			// maybe use FetchResponse to get a stream and feed it to pixbuf?
			 byte [] photos_data = client.Fetcher.Fetch (String.Format ("/databases/{0}/items",id), 
			                                     String.Format ("meta=dpap.thumb,dpap.filedata&query=('dmap.itemid:{0}')",photo.Id));
			ContentNode node = ContentParser.Parse (client.Bag, photos_data);
			
			// DEBUG
			Console.WriteLine ("About to dump the photo!");
			node.Dump ();
			ContentNode filedata_node = node.GetChild ("dpap.filedata");
			Console.WriteLine ("Photo starts at index " + filedata_node.Value);
			BinaryWriter writer = new BinaryWriter (File.Open (dest, FileMode.Create));
			
			int count = 0;
			int off = System.Int32.Parse (filedata_node.Value.ToString ());
			byte [] photo_buf;
			MemoryStream data = new MemoryStream ();
			writer.Write (photos_data, (int)off, (int)photos_data.Length-off);
			data.Position = 0;
			//	Gdk.Pixbuf pb = new Gdk.Pixbuf (data);
			data.Close ();
			Console.Write ("Written " + count + " out of " + (photos_data.Length-off));
        }

        public void AddPhoto (Photo photo) {
            if (photo.Id == 0)
                photo.SetId (next_photo_id++);
            
            photos.Add (photo);
            base_album.AddPhoto (photo);

            if (PhotoAdded != null)
                PhotoAdded (this, new PhotoArgs (photo));
        }

        public void RemovePhoto (Photo photo) {
            photos.Remove (photo);
            base_album.RemovePhoto (photo);

            foreach (Album pl in albums) {
                pl.RemovePhoto (photo);
            }

            if (PhotoRemoved != null)
                PhotoRemoved (this, new PhotoArgs (photo));
        }

        public void AddAlbum (Album pl) {
			// DEBUG			
			Console.WriteLine ("Adding album " + pl.Name);
            albums.Add (pl);			
            if (AlbumAdded != null)
                AlbumAdded (this, new AlbumArgs (pl));
        }

        public void RemoveAlbum (Album pl) {
            albums.Remove (pl);

            if (AlbumRemoved != null)
                AlbumRemoved (this, new AlbumArgs (pl));
        }

        private Album CloneAlbum (Database db, Album pl) {
            Album clone_pl = new Album (pl.Name);
            clone_pl.Id = pl.Id;

            IList<Photo> plphotos = pl.Photos;
            for (int i = 0; i < plphotos.Count; i++) {
                clone_pl.AddPhoto (db.LookupPhotoById (plphotos [i].Id), pl.GetContainerId (i));
            }

            return clone_pl;
        }

        public object Clone () {
            Database db = new Database (this.name);
            db.id = id;
            db.persistent_id = persistent_id;

            List<Photo> clone_photos = new List<Photo> ();
            foreach (Photo photo in photos) {
                clone_photos.Add ( (Photo) photo.Clone ());
            }

            db.photos = clone_photos;

            List<Album> clone_albums = new List<Album> ();
            foreach (Album pl in albums) {
                clone_albums.Add (CloneAlbum (db, pl));
            }

            db.albums = clone_albums;
            db.base_album = CloneAlbum (db, base_album);
            return db;
        }
    }
}
