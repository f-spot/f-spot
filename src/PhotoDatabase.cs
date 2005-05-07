using System;
using System.IO;
using System.Collections;

namespace iPodSharp {
	internal class ImageItemRecord : Record {
		public int ChildCount;
		public int Id;
		public long ImageId;  // this corresponds to the itunes id for album artwork, not sure what is is here.
		public int unknownFour = 0;
		public int unknownFive = 0;
		public int unknownSix = 0;
		public int unknownSeven = 0;
		public int unknownEight = 0;
		public int Size; // Size of the image in bytes
		
		public ArrayList Versions;

		public override void Read (BinaryReader reader) {
			
			base.Read (reader);
			if (this.Name != "mhii")
				throw new ApplicationException ("Unexpected name: " + this.Name);

			byte [] body = reader.ReadBytes (this.HeaderOne - 12);
			
			ChildCount = BitConverter.ToInt32 (body, 0);
			
			Id = BitConverter.ToInt32 (body, 4);
			ImageId = BitConverter.ToInt32 (body, 8);

			unknownFour = BitConverter.ToInt32 (body, 12);
			unknownFive = BitConverter.ToInt32 (body, 16);
			unknownSix = BitConverter.ToInt32 (body, 20);
			unknownSeven = BitConverter.ToInt32 (body, 24);
			unknownFive = BitConverter.ToInt32 (body, 28);

			Size = BitConverter.ToInt32 (body, 32);

			Versions = new ArrayList ();
			for (int i = 0; i < ChildCount; i++) {
				ImageDataObjectRecord rec = new ImageDataObjectRecord ();
				rec.Read (reader);
				Versions.Add (rec);
			}
		}
	}

	internal class AlbumBookRecord : Record {
		public int unknownOne;  // This may be the count of mhod records preceeding the album items
		public int ItemCount;
		public int unknownTwo;  // 101, 102, 512 .... not sure yet what these represent
		public int unknownThree;  // appears that this might be two short records both unknown
		public int unknownFour;
		public int unknownFive;
		public int unknownSix;
		public int unknownSeven;

		public ImageDataObjectRecord TitleRecord;  // I'm not sure this is the best name
		public ArrayList Items;

		public string Title {
			get {
				return TitleRecord.StringValue;
			}
		}

		public override void Read (BinaryReader reader)
		{
			base.Read (reader);
			if (this.Name != "mhba")
				throw new ApplicationException ("unexpected record type: " + this.Name);

			byte [] body = reader.ReadBytes (this.HeaderOne - 12);

			unknownOne = BitConverter.ToInt32 (body, 0);
			ItemCount = BitConverter.ToInt32 (body, 4);
			unknownTwo = BitConverter.ToInt32 (body, 8); //

			if (unknownOne != 1)
				throw new System.Exception ("Unexpected title count or something, figure it out");

			TitleRecord = new ImageDataObjectRecord ();
			TitleRecord.Read (reader);

			Items = new ArrayList ();
			for (int i = 0; i < ItemCount; i++) {
				AlbumItemRecord rec = new AlbumItemRecord ();
				rec.Read (reader);
				Items.Add (rec);
			}
		}
	}

	internal class FileItemRecord : Record {
		public override void Read (BinaryReader reader) 
		{
			base.Read (reader);
			byte [] body = reader.ReadBytes (this.HeaderOne - 12);			
			System.Console.WriteLine ("Reading record {0} {1} {2}", this.Name, this.HeaderOne, this.HeaderTwo);
		}
	}

	internal class AlbumItemRecord : Record {
		// This appears inside album books and appears to contain references to the IDs in the main dataset
		public int unknownOne; // Always Zero?
		public int Id; // the id of the corresponding image
		// followed by five bytes of zero

		public override void Read (BinaryReader reader) 
		{
			base.Read (reader);
			byte [] body = reader.ReadBytes (this.HeaderOne - 12);			
			
			Id = BitConverter.ToInt32 (body, 4);
		}
	}

	internal class FileListRecord : ListRecord {
		public override Record ConstructChild ()
		{
			return new FileItemRecord ();
		}

		public FileItemRecord [] Files {
			get {
				return (FileItemRecord [])Items.ToArray (typeof (FileItemRecord));
			}
		}
	}

	internal class AlbumListRecord : ListRecord {
		public override Record ConstructChild ()
		{
			return new AlbumBookRecord ();
		}

		public AlbumBookRecord [] Books {
			get {
				return (AlbumBookRecord []) Items.ToArray (typeof (AlbumBookRecord));
			}
		}
	}
	
	internal class ImageListRecord : ListRecord {
		public override Record ConstructChild ()
		{
			return new ImageItemRecord ();
		}
		
		public ImageItemRecord [] Images {
			get {
				return (ImageItemRecord []) Items.ToArray (typeof (ImageItemRecord));
			}
		}
	}
	
	internal abstract class ListRecord : Record {
		public ArrayList Items = new ArrayList ();

		public abstract Record ConstructChild ();
		
		public override void Read (BinaryReader reader) {

			base.Read (reader);
			
			byte [] body = reader.ReadBytes (this.HeaderOne - 12);
			
			int count = this.HeaderTwo;
			
			for (int i = 0; i < count; i++) {
				Record rec = ConstructChild ();
				rec.Read (reader);
				
				Items.Add (rec);
			}
		}

		public override void Save (BinaryWriter writer) {
			
			MemoryStream stream = new MemoryStream ();
			BinaryWriter childWriter = new BinaryWriter (stream);
			
			foreach (Record rec in Items) {
				rec.Save (childWriter);
			}
			
			childWriter.Flush ();
			byte[] childData = stream.GetBuffer ();
			int childDataLength = (int) stream.Length;
			childWriter.Close ();
			
			writer.Write (System.Text.Encoding.ASCII.GetBytes (this.Name));
			writer.Write (12 + PadLength);
			writer.Write (Items.Count);
			writer.Write (new byte[PadLength]);
			writer.Write (childData, 0, childDataLength);
		}
	}

	internal class ImageDataObjectRecord : Record {
		public ushort Type;
		public ushort unknownOne;
		public int unknownTwo;
		public int unknownThree;
		
		public ImageNameRecord Child;
		
		// length placeholder;
		public int Version;
		public int unknownFour;
		public string StringValue;

		public override void Read (BinaryReader reader) {
			base.Read (reader);

			if (this.Name != "mhod")
				throw new ApplicationException ("Unexpected header name: " + this.Name);

			byte [] body = reader.ReadBytes (this.HeaderOne - 12);
			
			Type = BitConverter.ToUInt16 (body, 0);
			unknownOne = BitConverter.ToUInt16 (body, 2);

			unknownTwo  = BitConverter.ToInt32 (body, 4);
			unknownThree = BitConverter.ToInt32 (body, 8);
			int length;

			switch (Type) {
			case 1: // This appears to be a string in ascii or 8859-1 or utf8 (I've only seen ascii so far)
				body = reader.ReadBytes (this.HeaderTwo - this.HeaderOne);
				length = BitConverter.ToInt32 (body, 0);
				Version = BitConverter.ToInt32 (body, 4);
				unknownFour = BitConverter.ToInt32 (body, 8);
				StringValue = System.Text.Encoding.UTF8.GetString (body, 12, length);
				break;
			case 2:  // This data object is a thumbnail file?
			case 5: // This data object is a full resolution file?
				Child = new ImageNameRecord ();
				Child.Read (reader);
				break;
			case 3:
				// This is a string in utf-16
				body = reader.ReadBytes (this.HeaderTwo - this.HeaderOne);
				length = BitConverter.ToInt32 (body, 0);
				Version = BitConverter.ToInt32 (body, 4);
				unknownFour = BitConverter.ToInt32 (body, 8);
				StringValue = System.Text.Encoding.Unicode.GetString (body, 12, length);
				break;
			default:
				throw new ApplicationException ("Unknown data object type");
			}
		}
	}

	internal class ImageNameRecord : Record {
		public int ChildCount;
		public int CorrelationID;
		public int ThumbPosition;
		public int ThumbSize;
		public int unknownThree;
		public ushort Width;
		public ushort Height;
		
		public ImageDataObjectRecord Record;
		
		public string Path {
			get {
				if (Record.Type != 3)
					throw new ApplicationException ("Unknown image name child type");
				
				return Record.StringValue;
			}
		}

		public override void Read (BinaryReader reader) {
			base.Read (reader);
			
			if (this.Name != "mhni")
				throw new ApplicationException ("Unexpected header name: " + this.Name);

			byte [] body = reader.ReadBytes (this.HeaderOne - 12);
			ChildCount = BitConverter.ToInt32 (body, 0);
			CorrelationID = BitConverter.ToInt32 (body, 4);
			ThumbPosition = BitConverter.ToInt32 (body, 8);
			ThumbSize = BitConverter.ToInt32 (body, 12);
			unknownThree = BitConverter.ToInt32 (body, 16);
			Width = BitConverter.ToUInt16 (body, 20);
			Height = BitConverter.ToUInt16 (body, 22);
			
			Record = new ImageDataObjectRecord ();
			Record.Read (reader);
		}

	}

	internal class ImageDataSetRecord : Record {
		public int Index;
		
		public ListRecord Record;

		public override void Read (BinaryReader reader) {
			base.Read (reader);
			
			if (this.Name != "mhsd")
				throw new ApplicationException ("Unknown header name: " + this.Name);
			
			byte [] body = reader.ReadBytes (this.HeaderOne - 12);
			
			Index = BitConverter.ToInt32 (body, 0);


			
			switch (Index) {
			case 1:
				this.Record = new ImageListRecord ();
				this.Record.Read (reader);
				break;
			case 2:
				this.Record = new AlbumListRecord ();
				this.Record.Read (reader);
				//reader.ReadBytes (this.HeaderTwo - this.HeaderOne);
				break;
			case 3:
				this.Record = new FileListRecord ();	
				this.Record.Read (reader);
			// Skip For Now.
				//reader.ReadBytes (this.HeaderTwo - this.HeaderOne);
				break;
			default:
				throw new ApplicationException ("Unknown ImageDataSet record type: " + Index);
			}
		}
	}
	
	internal class PhotoDatabaseRecord : Record {
		private int unknownOne = 0;
		private int unknownTwo = 1;
		
	        public ArrayList Datasets;

		public int ChildrenCount;
		
		private int unknownThree = 3;
		private int unknownFour;
		private long unknownFive;
		private long unknownSix;
		private int unknownSeven = 2;
		private int unknownEight = 0;
		private int unknownNine = 0;
		private int unknownTen;
		private int unknownEleven;
		
		public override void Read (BinaryReader reader) {
			base.Read (reader);
			
			byte[] body = reader.ReadBytes (this.HeaderOne - 12);

			unknownOne = BitConverter.ToInt32 (body, 0);
			unknownOne = BitConverter.ToInt32 (body, 4);
			
			ChildrenCount = BitConverter.ToInt32 (body, 8);

			if (ChildrenCount != 3)
				System.Console.WriteLine ("Got unexpected child count");
			
			
			Datasets = new ArrayList ();
			for (uint i = 0; i < ChildrenCount; i++) {
				ImageDataSetRecord rec = new ImageDataSetRecord ();
				rec.Read (reader);
				Datasets.Add (rec);
			}	
		}
	}

	internal class Record {
		protected const int PadLength = 2;
		
		public string Name;
		public int HeaderOne; // usually the size of this record
		public int HeaderTwo; // usually the size of this record + size of  children
		
		public virtual void Read (BinaryReader reader) {
			this.Name = System.Text.Encoding.ASCII.GetString (reader.ReadBytes (4));
			this.HeaderOne = reader.ReadInt32 ();
			this.HeaderTwo = reader.ReadInt32 ();

			//System.Console.WriteLine ("Reading record {0} {1} {2}", this.Name, this.HeaderOne, this.HeaderTwo);
		}

		public virtual void Save (BinaryWriter writer) {
			throw new ApplicationException ("Save Unimplemented");
		}
	}

	
	internal class PhotoDatabase {
		public string Path; 
		private PhotoDatabaseRecord dbrec;

		public PhotoDatabase (string path)
		{
			this.Path = path;
			Load (this.Path);
		}

		public void Load (string path) {
			using (BinaryReader reader = new BinaryReader (new FileStream (path, FileMode.Open))) {
				dbrec = new PhotoDatabaseRecord ();
				dbrec.Read (reader);
			}
		}

		public void Dump () {
			System.Console.WriteLine (dbrec.Datasets.Count);

			foreach (ImageDataSetRecord dataset in dbrec.Datasets) {
				ListRecord list = dataset.Record;

				System.Console.WriteLine ("{0}.Count = {1}", list.Name, list.Items.Count);
				
				if (list is ImageListRecord) {
					ImageItemRecord image = ((ImageListRecord)list).Images [0] as ImageItemRecord;
					IthmbDb.DisplayItem (image, System.IO.Path.GetDirectoryName (this.Path)); 

					foreach (ImageItemRecord iirec in ((ImageListRecord)list).Images) {
						System.Console.WriteLine ("Id = {0}", iirec.Id);
						foreach (ImageDataObjectRecord file in iirec.Versions) {
							System.Console.WriteLine ("   path={0} size {1} position {2} ID {3}", 
										  file.Child.Path, 
										  file.Child.ThumbPosition,
										  file.Child.ThumbSize,
										  file.Child.CorrelationID);
							
						}
					}
				} else if (list is AlbumListRecord) {
					foreach (AlbumBookRecord book in list.Items) {
						System.Console.WriteLine ("Book Title = {0}", book.Title);
						foreach (AlbumItemRecord entry in book.Items) {
							System.Console.WriteLine ("entry {0}", entry.Id);
						}
					}
				}
			}
		}

		static void Main (string [] args) 
		{
			Gtk.Application.Init ();

			PhotoDatabase db = new PhotoDatabase (args [0]);
			db.Dump ();

			Gtk.Application.Run ();
		}
	}
}
