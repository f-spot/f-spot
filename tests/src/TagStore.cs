using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;

namespace FSpot.Tests
{
	[TestFixture]
	public class TagStoreTests
	{
		Db db;
		const string path = "./TagStoreTest.db";

		[SetUp]
		public void SetUp ()
		{
			Gtk.Application.Init ();
			try {
				File.Delete (path);
			} catch {}

			db = new Db ();
			db.Init (path, true);
		}

		[TearDown]
		public void TearDown ()
		{
			db.Dispose ();
		}

		[Test]
		public void InsertCloseAndCheck ()
		{
			Category people_category = db.Tags.GetTagByName ("People") as Category;
			db.Tags.CreateTag (people_category, "Anna", true);
			db.Tags.CreateTag (people_category, "Ettore", true);
			Tag miggy_tag = db.Tags.CreateTag (people_category, "Miggy", true);
			miggy_tag.SortPriority = -1;
			db.Tags.Commit (miggy_tag);
	
			Category places_category = db.Tags.GetTagByName ("Places") as Category;
			db.Tags.CreateTag (places_category, "Milan", true);
			db.Tags.CreateTag (places_category, "Boston", true);
	
			Category exotic_category = db.Tags.CreateCategory (places_category, "Exotic", true);
			db.Tags.CreateTag (exotic_category, "Bengalore", true);
			db.Tags.CreateTag (exotic_category, "Manila", true);
			Tag tokyo_tag = db.Tags.CreateTag (exotic_category, "Tokyo", true);
	
			tokyo_tag.Category = places_category;
			tokyo_tag.Name = "Paris";
			db.Tags.Commit (tokyo_tag);
	
			db.Dispose ();
	
			db.Init (path, false);
			Category cat = db.Tags.GetTagByName ("People") as Category;
			Assert.AreEqual ("People", cat.Name);
			List<Tag> list = new List<Tag> (cat.Children);
			Assert.IsTrue (list.Contains (db.Tags.GetTagByName ("Anna")));
			Assert.IsTrue (list.Contains (db.Tags.GetTagByName ("Ettore")));
			Assert.IsTrue (list.Contains (db.Tags.GetTagByName ("Miggy")));

			cat = db.Tags.GetTagByName ("Places") as Category;
			Assert.AreEqual ("Places", cat.Name);
			list = new List<Tag> (cat.Children);
			Assert.IsTrue (list.Contains (db.Tags.GetTagByName ("Milan")));
			Assert.IsTrue (list.Contains (db.Tags.GetTagByName ("Boston")));
			Assert.IsTrue (list.Contains (db.Tags.GetTagByName ("Exotic")));
			Assert.IsTrue (db.Tags.GetTagByName ("Exotic") is Category);

			cat = db.Tags.GetTagByName ("Exotic") as Category;
			Assert.AreEqual ("Exotic", cat.Name);
			list = new List<Tag> (cat.Children);
			Assert.IsTrue (list.Contains (db.Tags.GetTagByName ("Bengalore")));
			Assert.IsTrue (list.Contains (db.Tags.GetTagByName ("Manila")));
		}

	}
}
