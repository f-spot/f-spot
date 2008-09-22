// Photo.cs
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
namespace DPAP
{
	
	
	public class Photo
	{
		private string author;
        private string album;
        private string title;
        private int year;
        private string format;
        private TimeSpan duration;
        private int id;
        private int size;
		private int width;
		private int height;
        private string genre;
        private int photo_number;
        private int photo_count;
        private string filename;
		private string thumbnail;
		private string path;
		private int thumbsize;
        private DateTime date_added = DateTime.Now;
        private DateTime date_modified = DateTime.Now;
        private short bitrate;

        public event EventHandler Updated;
        
        public string Author {
            get { return author; }
            set {
                author = value;
                EmitUpdated ();
            }
        }
        
        public string Album {
            get { return album; }
            set {
                album = value;
                EmitUpdated ();
            }
        }
        
        public string Title {
            get { return title; }
            set {
                title = value;
                EmitUpdated ();
            }
        }
        
        public int Year {
            get { return year; }
            set {
                year = value;
                EmitUpdated ();
            }
        }
        
        public string Format {
            get { return format; }
            set {
                format = value;
                EmitUpdated ();
            }
        }
        
        public TimeSpan Duration {
            get { return duration; }
            set {
                duration = value;
                EmitUpdated ();
            }
        }
        
        public int Id {
            get { return id; }
        }
        
        public int Size {
            get { return size; }
            set {
                size = value;
                EmitUpdated ();
            }
        }
                
        public string FileName {
            get { return filename; }
            set {
                filename = value;
                EmitUpdated ();
            }
        }
        
		public string Path {
            get { return path; }
            set {
                path = value;
                EmitUpdated ();
            }
        }
		
		public string Thumbnail {
            get { return thumbnail; }
            set {
                thumbnail = value;
                EmitUpdated ();
            }
        }		
		
		public int ThumbSize {
            get { return thumbsize; }
            set {
                thumbsize = value;
                EmitUpdated ();
            }
        }				
		
		public int Width {
			get { return width; }
			set {
				width = value;
				EmitUpdated ();
			}
		}
		
		public int Height {
			get { return height; }
			set {
				height = value;
				EmitUpdated ();
			}
		}
		
        public DateTime DateAdded {
            get { return date_added; }
            set {
                date_added = value;
                EmitUpdated ();
            }
        }
        
        public DateTime DateModified {
            get { return date_modified; }
            set {
                date_modified = value;
                EmitUpdated ();
            }
        }

        
        public object Clone () {
            Photo photo = new Photo ();
            photo.author = author;
            photo.album = album;
            photo.title = title;
            photo.year = year;
            photo.format = format;
            photo.duration = duration;
            photo.id = id;
            photo.size = size;
            photo.filename = filename;
			photo.thumbnail = thumbnail;
			photo.thumbsize = thumbsize;
            photo.date_added = date_added;
            photo.date_modified = date_modified;
			photo.path = path;
            return photo;
        }

        public override string ToString () {
            return String.Format ("fname={0}, title={1}, format={2}, id={3}, path={4}", filename, title, format, id, path);
        }

        internal void SetId (int id) {
            this.id = id;
        }
		internal ContentNode ToFileData (bool thumb) {
			
			ArrayList nodes = new ArrayList ();
			Console.WriteLine ("Requested "+ ( (thumb)?"thumb":"file") +", thumbnail=" + thumbnail + ", hires=" + path);
			nodes.Add (new ContentNode ("dmap.itemkind", (byte)3));
			nodes.Add (new ContentNode ("dmap.itemid", id));
			
			// pass the appropriate file path, depending on wether 
			//we are sending the thumbnail or the hi-res image
			
			nodes.Add (new ContentNode ("dpap.filedata",
			                                             new ContentNode ("dpap.imagefilesize", (thumb)?thumbsize:size),
			                                             new ContentNode ("dpap.imagefilename", (thumb)?thumbnail:path),
			                                             new ContentNode ("dpap.imagefilename", (thumb)?thumbnail:filename)));
			
			return (new ContentNode ("dmap.listingitem", nodes));
			
		}
        internal ContentNode ToNode (string [] fields) {

            ArrayList nodes = new ArrayList ();
            
            foreach (string field in fields) {
                object val = null;
                
                switch (field) {
                case "dmap.itemid":
                    val = id;
                    break;
                case "dmap.itemname":
                    val = title;
                    break;
                case "dmap.itemkind":
                    val = (byte) 3;
                    break;
                case "dmap.persistentid":
                    val = (long) id;
                    break;
                case "dpap.photoalbum":
                    val = album;
                    break;
                
                case "dpap.author":
                    val = author;
                    break;
                
                case "dpap.imageformat":
                    val = format;
                    break;
				case "dpap.imagefilename":
					val = filename;
					break;
				case "dpap.imagefilesize":
					val = thumbsize;
					break;
				case "dpap.imagelargefilesize":
					val = size;
					break;
				/*case "dpap.aspectratio":
					val = "0";
					break;*/
				case "dpap.creationdate":
					val = 7799;
					break;
				case "dpap.pixelheight":
					val = 0;
					break;
				case "dpap.pixelwidth":
					val = 0;
					break;
				case "dpap.imagerating":
					val = 0;
					break;
					
                default:
                    break;
                }
                
                if (val != null) {
                    // iTunes wants this to go first, sigh
                    if (field == "dmap.itemkind")
                        nodes.Insert (0, new ContentNode (field, val));
                    else
                        nodes.Add (new ContentNode (field, val));
                }
            }
            
            return new ContentNode ("dmap.listingitem", nodes);
        }
		
        internal static Photo FromNode (ContentNode node) {
            Photo photo = new Photo ();
            
            foreach (ContentNode field in (ContentNode []) node.Value) {
                switch (field.Name) {
                case "dmap.itemid":
                    photo.id = (int) field.Value;
                    break;
                case "dmap.itemname":
                    photo.title = (string) field.Value;
                    break;
                case "dpap.imageformat":
                    photo.format = (string) field.Value;
                    break;
                case "dpap.imagefilename":
					photo.filename = (string) field.Value;
					break;
               /* case "dpap.imagefilesize":
                    photo.size = (int) field.Value;
                    break;
                case "dpap.imagepixelwidth":
					photo.width = (int) field.Value;
					break;
				case "dpap.imagepixelheight":
					photo.height = (int) field.Value;
					break;*/
                default:
                    break;
                }
            }

            return photo;
        }

        internal ContentNode ToAlbumNode (string [] fields) {
			ArrayList nodes = new ArrayList ();
            
            foreach (string field in fields) {
                object val = null;
                
                switch (field) {
                case "dmap.itemid":
                    val = id;
                    break;
                case "dmap.itemname":
                    val = title;
                    break;
                case "dmap.itemkind":
                    val = (byte) 3;
                    break;
                case "dmap.persistentid":
                    val = (long) id;
                    break;
                case "dpap.photoalbum":
                    val = album;
                    break;
                
                case "dpap.author":
                    val = author;
                    break;
                
                case "dpap.imageformat":
                    val = format;
                    break;
				case "dpap.imagefilename":
					val = filename;
					break;
				case "dpap.imagefilesize":
					val = thumbsize;
					break;
				case "dpap.imagelargefilesize":
					val = size;
					break;
				// Apparently this has to be sent even with bogus data, 
				// otherwise iPhoto '08 wont show the thumbnails
				case "dpap.aspectratio":
					val = "1.522581";
					break;
				case "dpap.creationdate":
					val = 7799;
					break;
				case "dpap.pixelheight":
					val = 0;
					break;
				case "dpap.pixelwidth":
					val = 0;
					break;
				case "dpap.imagerating":
					val = 0;
					break;
					
                default:
                    break;
                }
                
                if (val != null) {
                    // iTunes wants this to go first, sigh
                    if (field == "dmap.itemkind")
                        nodes.Insert (0, new ContentNode (field, val));
                    else
                        nodes.Add (new ContentNode (field, val));
                }
            }			
            return new ContentNode ("dmap.listingitem", 
                                    new ContentNode ("dmap.itemkind", (byte) 3),
			                        nodes);
                                   /* new ContentNode ("dpap.imagefilename", filename),
                                    new ContentNode ("dmap.itemid", Id),
                                    //new ContentNode ("dmap.containeritemid", containerId),
                                    new ContentNode ("dmap.itemname", Title == null ? String.Empty : Title));
                                    */
        }

        internal static void FromAlbumNode (Database db, ContentNode node, out Photo photo, out int containerId) {
            photo = null;
            containerId = 0;
            
            foreach (ContentNode field in (ContentNode []) node.Value) {
                switch (field.Name) {
                case "dmap.itemid":
                    photo = db.LookupPhotoById ( (int) field.Value);
                    break;
                case "dmap.containeritemid":
                    containerId = (int) field.Value;
                    break;
                default:
                    break;
                }
            }
        }

        private bool Equals (Photo photo) {
            return author == photo.Author &&
                album == photo.Album &&
                title == photo.Title &&
                year == photo.Year &&
                format == photo.Format &&
                size == photo.Size &&
                date_added == photo.DateAdded &&
                date_modified == photo.DateModified;
        }

        internal void Update (Photo photo) {
            if (Equals (photo))
                return;

            author = photo.Author;
            album = photo.Album;
            title = photo.Title;
            year = photo.Year;
            format = photo.Format;
            size = photo.Size;
            date_added = photo.DateAdded;
            date_modified = photo.DateModified;

            EmitUpdated ();
        }

        private void EmitUpdated () {
            if (Updated != null)
                Updated (this, new EventArgs ());
        }
    }
}