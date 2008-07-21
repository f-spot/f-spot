// Photo.cs created with MonoDevelop
// User: andrzej at 11:42 2008-07-15
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
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
        private int photoNumber;
        private int photoCount;
        private string fileName;
        private DateTime dateAdded = DateTime.Now;
        private DateTime dateModified = DateTime.Now;
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
            get { return fileName; }
            set {
                fileName = value;
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
            get { return dateAdded; }
            set {
                dateAdded = value;
                EmitUpdated ();
            }
        }
        
        public DateTime DateModified {
            get { return dateModified; }
            set {
                dateModified = value;
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
            photo.fileName = fileName;
            photo.dateAdded = dateAdded;
            photo.dateModified = dateModified;
			
            return photo;
        }

        public override string ToString () {
            return String.Format ("{0} - {1}.{2} ({3})", fileName, title, format, id);
        }

        internal void SetId (int id) {
            this.id = id;
        }
		internal ContentNode ToFileData () {
			return new ContentNode ("dpap.databasesongs",
			                        new ContentNode ("dpap.filedata",
                                    new ContentNode ("dpap.imagefilesize", size),
                                    new ContentNode ("dpap.imagefilename", fileName))
			                        );
		}
        internal ContentNode ToNode (string[] fields) {

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
                    val = (byte) 2;
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
					val = fileName;
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
            
            foreach (ContentNode field in (ContentNode[]) node.Value) {
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
					photo.fileName = (string) field.Value;
					break;
                case "dpap.imagefilesize":
                    photo.size = (int) field.Value;
                    break;
                case "dpap.imagepixelwidth":
					photo.width = (int) field.Value;
					break;
				case "dpap.imagepixelheight":
					photo.height = (int) field.Value;
					break;
                default:
                    break;
                }
            }

            return photo;
        }

        internal ContentNode ToAlbumNode (int containerId) {
            return new ContentNode ("dmap.listingitem",
                                    new ContentNode ("dmap.itemkind", (byte) 2),
                                    new ContentNode ("dpap.imagefilename", fileName),
                                    new ContentNode ("dmap.itemid", Id),
                                    new ContentNode ("dmap.containeritemid", containerId),
                                    new ContentNode ("dmap.itemname", Title == null ? String.Empty : Title));
        }

        internal static void FromAlbumNode (Database db, ContentNode node, out Photo photo, out int containerId) {
            photo = null;
            containerId = 0;
            
            foreach (ContentNode field in (ContentNode[]) node.Value) {
                switch (field.Name) {
                case "dmap.itemid":
                    photo = db.LookupPhotoById ((int) field.Value);
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
                dateAdded == photo.DateAdded &&
                dateModified == photo.DateModified;
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
            dateAdded = photo.DateAdded;
            dateModified = photo.DateModified;

            EmitUpdated ();
        }

        private void EmitUpdated () {
            if (Updated != null)
                Updated (this, new EventArgs ());
        }
    }
}