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
using Gdk;

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

        private const int ChunkLength = 8192;
		
        private const string PhotoQuery = "meta=dpap.aspectratio,dmap.itemid,dmap.itemname,dpap.imagefilename," +
			"dpap.imagefilesize,dpap.creationdate,dpap.imagepixelwidth," +
			"dpap.imagepixelheight,dpap.imageformat,dpap.imagerating," +
			"dpap.imagecomments,dpap.imagelargefilesize,dpap.filedata&type=photo"; 
			

        private static int nextid = 1;
        private Client client;
        private int id;
        private long persistentId;
        private string name;

        private List<Photo> photos = new List<Photo> ();
        private List<Album> albums = new List<Album> ();
        private Album baseAlbum = new Album ();
        private int nextPhotoId = 1;

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
                baseAlbum.Name = value;
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

        public Photo PhotoAt(int index)
        {
            return photos[index] as Photo;
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
            foreach (ContentNode item in (ContentNode[]) dbNode.Value) {

                switch (item.Name) {
                case "dmap.itemid":
                    id = (int) item.Value;
                    break;
                case "dmap.persistentid":
                    persistentId = (long) item.Value;
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
            if (id == baseAlbum.Id)
                return baseAlbum;

            foreach (Album pl in albums) {
                if (pl.Id == id)
                    return pl;
            }

            return null;
        }

        internal ContentNode ToPhotosNode (string[] fields, int[] deletedIds) {

            ArrayList photoNodes = new ArrayList ();
            foreach (Photo photo in photos) {
                photoNodes.Add (photo.ToNode (fields));
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

            if (deletedNodes != null) {
                children.Add (new ContentNode ("dmap.deletedidlisting", deletedNodes));
            }
            
            return new ContentNode ("dpap.databasesongs", children);
        }

        internal ContentNode ToAlbumsNode () {
            ArrayList nodes = new ArrayList ();

            nodes.Add (baseAlbum.ToNode (true));
            
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
            byte[] albumsData;

            try {
                albumsData = client.Fetcher.Fetch (String.Format ("/databases/{0}/containers", id, revquery));
            } catch (WebException) {
                return;
            }
            
            ContentNode albumsNode = ContentParser.Parse (client.Bag, albumsData);
			// DEBUG data			
			albumsNode.Dump();
			Console.WriteLine("after dump!");
			
            if (IsUpdateResponse (albumsNode))
                return;

            // handle album additions/changes
            ArrayList plids = new ArrayList ();
			if(albumsNode.GetChild("dmap.listing")==null) return;
			
            foreach (ContentNode albumNode in (ContentNode[]) albumsNode.GetChild ("dmap.listing").Value) {
                
				// DEBUG
				Console.WriteLine("foreach loop");
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
			Console.WriteLine("delete albums that don't exist");
            // delete albums that no longer exist
            foreach (Album pl in new List<Album> (albums)) {
                if (!plids.Contains (pl.Id)) {
                    RemoveAlbum (pl);
                }
            }

            plids = null;
			// DEBUG
			Console.WriteLine("Add/remove photos in the albums");
            // add/remove photos in the albums
            foreach (Album pl in albums) {
                byte[] albumPhotosData = client.Fetcher.Fetch (String.Format ("/databases/{0}/containers/{1}/items",
                                                                                id, pl.Id), revquery);
                ContentNode albumPhotosNode = ContentParser.Parse (client.Bag, albumPhotosData);

                if (IsUpdateResponse (albumPhotosNode))
                    return;

                if ((byte) albumPhotosNode.GetChild ("dmap.updatetype").Value == 1) {

                    // handle album photo deletions
                    ContentNode deleteList = albumPhotosNode.GetChild ("dmap.deletedidlisting");

                    if (deleteList != null) {
                        foreach (ContentNode deleted in (ContentNode[]) deleteList.Value) {
                            int index = pl.LookupIndexByContainerId ((int) deleted.Value);

                            if (index < 0)
                                continue;

                            pl.RemoveAt (index);
                        }
                    }
                }

                // add new photos, or reorder existing ones

                int plindex = 0;
                foreach (ContentNode plPhotoNode in (ContentNode[]) albumPhotosNode.GetChild ("dmap.listing").Value) {
                    Photo plphoto = null;
                    int containerId = 0;
                    Photo.FromAlbumNode (this, plPhotoNode, out plphoto, out containerId);

                    if (pl[plindex] != null && pl.GetContainerId (plindex) != containerId) {
                        pl.RemoveAt (plindex);
                        pl.InsertPhoto (plindex, plphoto, containerId);
                    } else if (pl[plindex] == null) {
                        pl.InsertPhoto (plindex, plphoto, containerId);
                    }

                    plindex++;
                }
            }
        }

        private void RefreshPhotos (string revquery) {
			foreach (Album pl in albums){
	            byte[] photosData = client.Fetcher.Fetch (String.Format ("/databases/{0}/containers/{1}/items", id,pl.getId()),
	                                                     PhotoQuery);
	            ContentNode photosNode = ContentParser.Parse (client.Bag, photosData);

	            if (IsUpdateResponse (photosNode))
	                return;

	            // handle photo additions/changes
	            foreach (ContentNode photoNode in (ContentNode[]) photosNode.GetChild ("dmap.listing").Value) {
					// DEBUG data					
					//photoNode.Dump();
	                Photo photo = Photo.FromNode (photoNode);
					
	                Photo existing = LookupPhotoById (photo.Id);
					
	                if (existing == null){
					//	Console.WriteLine("adding " + photo.Title);
	                    AddPhoto (photo);
					}
	                else
					{
					//	Console.WriteLine("updating " + existing.Title);
	                    existing.Update (photo);
					}
	            }

	            if ((byte) photosNode.GetChild ("dmap.updatetype").Value == 1) {

	                // handle photo deletions
	                ContentNode deleteList = photosNode.GetChild ("dmap.deletedidlisting");

	                if (deleteList != null) {
	                    foreach (ContentNode deleted in (ContentNode[]) deleteList.Value) {
	                        Photo photo = LookupPhotoById ((int) deleted.Value);

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
			                                     String.Format("meta=dpap.filedata&query=('dmap.itemid:{0}')",photo.Id),
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
                    byte[] buf = new byte[ChunkLength];
                
					// Skip the header    
					//count = reader.Read(buf,0,89);
					
					//if(count < 89)
					//	count+=reader.Read(buf,0,89-count);
					
					while (true) {
                    buf = reader.ReadBytes (8192);
                    if (buf.Length == 0)
                        break;

                    data.Write (buf, 0, buf.Length);
					//Console.Write(buf.);
					}
					
			*/		
/*                    do {
                        count = reader.Read (buf, 0, ChunkLength);
                        writer.Write (buf, 0, count);
						data.Write (buf, 0, count);
                    } while (count != 0);
	*/				
					/*data.Flush();
					
					ContentNode node = ContentParser.Parse(client.Bag, data.GetBuffer());
					node.Dump();
					reader.Close ();
					
                }
            } finally {
				data.Close ();
                
                writer.Close ();
            }*/
			// maybe use FetchResponse to get a stream and feed it to pixbuf?
			 byte[] photosData = client.Fetcher.Fetch (String.Format ("/databases/{0}/items",id), 
			                                     String.Format("meta=dpap.filedata&query=('dmap.itemid:{0}')",photo.Id));
			ContentNode node = ContentParser.Parse(client.Bag, photosData);
			
			// DEBUG
			Console.WriteLine("About to dump the photo!");
			node.Dump();
			ContentNode fileDataNode = node.GetChild("dpap.filedata");
			Console.WriteLine("Photo starts at index " + fileDataNode.Value);
			BinaryWriter writer = new BinaryWriter (File.Open (dest, FileMode.Create));
			
			int count = 0;
			int off = System.Int32.Parse(fileDataNode.Value.ToString());
			byte[] photoBuf;
			MemoryStream data = new MemoryStream ();
			//while ( count < photosData.Length - fileDataNode.Value)
			data.Write(photosData, (int)off, (int)photosData.Length-off);
			writer.Write(photosData, (int)off, (int)photosData.Length-off);
			data.Position = 0;
			Gdk.Pixbuf pb = new Gdk.Pixbuf(data);
			data.Close();
			writer.Close();
			
			
			
			Console.Write("Written " + count + " out of " + (photosData.Length-off));
        }

        public void AddPhoto (Photo photo) {
            if (photo.Id == 0)
                photo.SetId (nextPhotoId++);
            
            photos.Add (photo);
            baseAlbum.AddPhoto (photo);

            if (PhotoAdded != null)
                PhotoAdded (this, new PhotoArgs (photo));
        }

        public void RemovePhoto (Photo photo) {
            photos.Remove (photo);
            baseAlbum.RemovePhoto (photo);

            foreach (Album pl in albums) {
                pl.RemovePhoto (photo);
            }

            if (PhotoRemoved != null)
                PhotoRemoved (this, new PhotoArgs (photo));
        }

        public void AddAlbum (Album pl) {
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
            Album clonePl = new Album (pl.Name);
            clonePl.Id = pl.Id;

            IList<Photo> plphotos = pl.Photos;
            for (int i = 0; i < plphotos.Count; i++) {
                clonePl.AddPhoto (db.LookupPhotoById (plphotos[i].Id), pl.GetContainerId (i));
            }

            return clonePl;
        }

        public object Clone () {
            Database db = new Database (this.name);
            db.id = id;
            db.persistentId = persistentId;

            List<Photo> clonePhotos = new List<Photo> ();
            foreach (Photo photo in photos) {
                clonePhotos.Add ((Photo) photo.Clone ());
            }

            db.photos = clonePhotos;

            List<Album> cloneAlbums = new List<Album> ();
            foreach (Album pl in albums) {
                cloneAlbums.Add (CloneAlbum (db, pl));
            }

            db.albums = cloneAlbums;
            db.baseAlbum = CloneAlbum (db, baseAlbum);
            return db;
        }
    }
}
