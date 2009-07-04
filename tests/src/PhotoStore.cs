#if ENABLE_NUNIT
using FSpot.Utils;
using NUnit.Framework;
using System.Collections;
using System.IO;
using System;
using Gdk;

namespace FSpot.Tests
{
	[TestFixture]
	public class PhotoStoreTests
	{
		Db db;
		const string path = "./PhotoStoreTest.db";

		string [] images = {
			"pano.jpg",
		};

		[SetUp]
		public void SetUp ()
		{
			Gtk.Application.Init ();
			try {
				File.Delete (path);
			} catch {}

			db = new Db ();
			db.Init (path, true);

			foreach (string image in images) {
				File.Copy ("../images/" + image, "./" + image, true);
			}

		}

		[TearDown]
		public void TearDown ()
		{
			db.Dispose ();
			foreach (string image in images)
				try {
					File.Delete ("./" + image);
				} catch {}
		}

//	static void Dump (Photo photo)
//	{
//	//	Console.WriteLine ("\t[{0}] {1}", photo.Id, photo.Path);
//		Console.WriteLine ("\t{0}", photo.Time.ToLocalTime ());
//
//		if (photo.Description != String.Empty)
//			Console.WriteLine ("\t{0}", photo.Description);
//		else
//			Console.WriteLine ("\t(no description)");
//
//		Console.WriteLine ("\tTags:");
//
//		if (photo.Tags.Count == 0) {
//			Console.WriteLine ("\t\t(no tags)");
//		} else {
//			foreach (Tag t in photo.Tags)
//				Console.WriteLine ("\t\t{0}", t.Name);
//		}
//
//		Console.WriteLine ("\tVersions:");
//
//		foreach (uint id in photo.VersionIds)
//			Console.WriteLine ("\t\t[{0}] {1}", id, photo.GetVersionName (id));
//	}

//	static void Dump (ArrayList photos)
//	{
//		foreach (Photo p in photos)
//			Dump (p);
//	}
//
//	static void DumpAll (Db db)
//	{
//		Console.WriteLine ("\n*** All pictures");
//		Dump (db.Photos.Query (null));
//	}
//
//	static void DumpForTags (Db db, ArrayList tags)
//	{
//		Console.Write ("\n*** Pictures for tags: ");
//		foreach (Tag t in tags)
//			Console.Write ("{0} ", t.Name);
//		Console.WriteLine ();
//
//		Dump (db.Photos.Query (tags));
//	}

		[Test]
		public void PopulatendRetrieve ()
		{
			/*Tag portraits_tag = */db.Tags.CreateTag (null, "Portraits", false);
			Tag landscapes_tag = db.Tags.CreateTag (null, "Landscapes", false);
			Tag favorites_tag = db.Tags.CreateTag (null, "Street", false);
	
			//uint portraits_tag_id = portraits_tag.Id;
			//uint landscapes_tag_id = landscapes_tag.Id;
			//uint favorites_tag_id = favorites_tag.Id;
	
			Pixbuf unused_thumbnail;
	
			Photo ny_landscape = db.Photos.Create (UriUtils.PathToFileUri ("../images/pano.jpg"), 0, out unused_thumbnail);
			ny_landscape.Description = "Snowy landscape";
			ny_landscape.AddTag (landscapes_tag);
			ny_landscape.AddTag (favorites_tag);
			db.Photos.Commit (ny_landscape);
	
//			Photo me_in_sf = db.Photos.Create (DateTime.Now.ToUniversalTime (), 2, "/home/ettore/Photos/me_in_sf.jpg",
//							   out unused_thumbnail);
//			me_in_sf.AddTag (landscapes_tag);
//			me_in_sf.AddTag (portraits_tag);
//			me_in_sf.AddTag (favorites_tag);
//			db.Photos.Commit (me_in_sf);
//	
//			me_in_sf.RemoveTag (favorites_tag);
//			me_in_sf.Description = "Myself and the SF skyline";
//			me_in_sf.CreateVersion ("cropped", Photo.OriginalVersionId);
//			me_in_sf.CreateVersion ("UM-ed", Photo.OriginalVersionId);
//			db.Photos.Commit (me_in_sf);
//	
//			Photo macro_shot = db.Photos.Create (DateTime.Now.ToUniversalTime (), 2, "/home/ettore/Photos/macro_shot.jpg",
//							     out unused_thumbnail);
			db.Dispose ();
	
			db.Init (path, false);
	
//			DumpAll (db);
//	
//			portraits_tag = db.Tags.Get (portraits_tag_id) as Tag;
//			landscapes_tag = db.Tags.Get (landscapes_tag_id) as Tag;
//			favorites_tag = db.Tags.Get (favorites_tag_id) as Tag;
//	
//			ArrayList query_tags = new ArrayList ();
//			query_tags.Add (portraits_tag);
//			query_tags.Add (landscapes_tag);
//			DumpForTags (db, query_tags);
//	
//			query_tags.Clear ();
//			query_tags.Add (favorites_tag);
//			DumpForTags (db, query_tags);
		}
	}
}
#endif

