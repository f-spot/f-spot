//
// UpdaterTests.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using NUnit.Framework;
using System;
using Hyena;
using FSpot;
using Moq;

namespace FSpot.Database.Tests
{
    [TestFixture]
    public class UpdaterTests
    {
        static bool initialized = false;
        static void Initialize () {
            GLib.GType.Init ();
            Updater.silent = true;
            initialized = true;
        }

        [Test]
        public void Test_0_6_1_5 ()
        {
            TestUpdate ("0.6.1.5", "17.0");
        }

        [Test]
        public void Test_0_6_2 ()
        {
            TestUpdate ("0.6.2", "17.1");
        }

        [Test]
        public void Test_0_7_0_17_2 ()
        {
            TestUpdate ("0.7.0-17.2", "17.2");
        }

        [Test]
        public void Test_0_7_0_18_0 ()
        {
            TestUpdate ("0.7.0-18.0", "18");
        }

        private void TestUpdate (string version, string revision)
        {
            if (!initialized)
                Initialize ();

            var uri = new SafeUri (Environment.CurrentDirectory + "/../tests/data/f-spot-"+version+".db");
            var file = GLib.FileFactory.NewForUri (uri);
            Assert.IsTrue (file.Exists, string.Format ("Test database for version {0} not found", version));

            var tmp = System.IO.Path.GetTempFileName ();
            var uri2 = new SafeUri (tmp);
            var file2 = GLib.FileFactory.NewForUri (uri2);
            file.Copy (file2, GLib.FileCopyFlags.Overwrite, null, null);

            var db = new FSpotDatabaseConnection (uri2.AbsolutePath);
            ValidateRevision (db, revision);

            var updaterUI = new Mock<IUpdaterUI> ().Object;
            Updater.Run (db, updaterUI);
            ValidateRevision (db, Updater.LatestVersion.ToString ());

            ValidateTableStructure (db);

            CheckPhotosTable (db);
            CheckPhotoVersionsTable (db);
            CheckTagsTable (db);

            file2.Delete ();
        }

        private void ValidateRevision (FSpotDatabaseConnection db, string revision)
        {
            var query = "SELECT data FROM meta WHERE name = 'F-Spot Database Version'";
            var found = db.Query<string> (query).ToString ();
            Assert.AreEqual (revision, found);
        }

        private void ValidateTableStructure (FSpotDatabaseConnection db)
        {
            CheckTableExistance (db, "exports");
            CheckTableExistance (db, "jobs");
            CheckTableExistance (db, "meta");
            CheckTableExistance (db, "photo_tags");
            CheckTableExistance (db, "photo_versions");
            CheckTableExistance (db, "photos");
            CheckTableExistance (db, "rolls");
            CheckTableExistance (db, "tags");
        }

        private void CheckTableExistance (FSpotDatabaseConnection db, string name)
        {
            Assert.IsTrue (db.TableExists (name), string.Format ("Expected table {0} does not exist.", name));
        }

        private void CheckPhotosTable (FSpotDatabaseConnection db)
        {
            CheckPhoto (db, 1, 1249579156, "file:///tmp/database/", "sample.jpg", "Testing!", 1, 2, 5);
            CheckPhoto (db, 2, 1276191607, "file:///tmp/database/", "sample_canon_bibble5.jpg", "", 1, 1, 0);
            CheckPhoto (db, 3, 1249834364, "file:///tmp/database/", "sample_canon_zoombrowser.jpg", "%test comment%", 1, 1, 0);
            CheckPhoto (db, 4, 1276191607, "file:///tmp/database/", "sample_gimp_exiftool.jpg", "", 1, 1, 5);
            CheckPhoto (db, 5, 1242995279, "file:///tmp/database/", "sample_nikon1.jpg", "", 1, 1, 1);
            CheckPhoto (db, 6, 1276191607, "file:///tmp/database/", "sample_nikon1_bibble5.jpg", "", 1, 1, 0);
            CheckPhoto (db, 7, 1167646774, "file:///tmp/database/", "sample_nikon2.jpg", "", 1, 1, 0);
            CheckPhoto (db, 8, 1276191607, "file:///tmp/database/", "sample_nikon2_bibble5.jpg", "", 1, 1, 0);
            CheckPhoto (db, 9, 1256140553, "file:///tmp/database/", "sample_nikon3.jpg", "                                    ", 1, 1, 0);
            CheckPhoto (db, 10, 1238587697, "file:///tmp/database/", "sample_nikon4.jpg", "                                    ", 1, 1, 0);
            CheckPhoto (db, 11, 1276191607, "file:///tmp/database/", "sample_no_metadata.jpg", "", 1, 1, 0);
            CheckPhoto (db, 12, 1265446642, "file:///tmp/database/", "sample_null_orientation.jpg", "", 1, 1, 0);
            CheckPhoto (db, 13, 1161575860, "file:///tmp/database/", "sample_olympus1.jpg", "", 1, 1, 0);
            CheckPhoto (db, 14, 1236006332, "file:///tmp/database/", "sample_olympus2.jpg", "", 1, 1, 0);
            CheckPhoto (db, 15, 1246010310, "file:///tmp/database/", "sample_panasonic.jpg", "", 1, 1, 0);
            CheckPhoto (db, 16, 1258799979, "file:///tmp/database/", "sample_sony1.jpg", "", 1, 1, 0);
            CheckPhoto (db, 17, 1257533767, "file:///tmp/database/", "sample_sony2.jpg", "", 1, 1, 0);
            CheckPhoto (db, 18, 1026565108, "file:///tmp/database/", "sample_xap.jpg", "", 1, 1, 4);
            CheckPhoto (db, 19, 1093249257, "file:///tmp/database/", "sample_xmpcrash.jpg", "", 1, 1, 0);
            CheckPhoto (db, 20, 1276191607, "file:///tmp/database/test/", "sample_tangled1.jpg", "test comment", 1, 1, 0);
            CheckCount (db, "photos", 20);
        }

        private void CheckPhotoVersionsTable (FSpotDatabaseConnection db)
        {
            CheckPhotoVersion (db, 1, 1, "Original", "file:///tmp/database/", "sample.jpg", "", 1);
            CheckPhotoVersion (db, 2, 1, "Original", "file:///tmp/database/", "sample_canon_bibble5.jpg", "", 1);
            CheckPhotoVersion (db, 3, 1, "Original", "file:///tmp/database/", "sample_canon_zoombrowser.jpg", "", 1);
            CheckPhotoVersion (db, 4, 1, "Original", "file:///tmp/database/", "sample_gimp_exiftool.jpg", "", 1);
            CheckPhotoVersion (db, 5, 1, "Original", "file:///tmp/database/", "sample_nikon1.jpg", "", 1);
            CheckPhotoVersion (db, 6, 1, "Original", "file:///tmp/database/", "sample_nikon1_bibble5.jpg", "", 1);
            CheckPhotoVersion (db, 7, 1, "Original", "file:///tmp/database/", "sample_nikon2.jpg", "", 1);
            CheckPhotoVersion (db, 8, 1, "Original", "file:///tmp/database/", "sample_nikon2_bibble5.jpg", "", 1);
            CheckPhotoVersion (db, 9, 1, "Original", "file:///tmp/database/", "sample_nikon3.jpg", "", 1);
            CheckPhotoVersion (db, 10, 1, "Original", "file:///tmp/database/", "sample_nikon4.jpg", "", 1);
            CheckPhotoVersion (db, 1, 2, "Modified", "file:///tmp/database/", "sample%20(Modified).jpg", "", 0);
            CheckPhotoVersion (db, 11, 1, "Original", "file:///tmp/database/", "sample_no_metadata.jpg", "", 1);
            CheckPhotoVersion (db, 12, 1, "Original", "file:///tmp/database/", "sample_null_orientation.jpg", "", 1);
            CheckPhotoVersion (db, 13, 1, "Original", "file:///tmp/database/", "sample_olympus1.jpg", "", 1);
            CheckPhotoVersion (db, 14, 1, "Original", "file:///tmp/database/", "sample_olympus2.jpg", "", 1);
            CheckPhotoVersion (db, 15, 1, "Original", "file:///tmp/database/", "sample_panasonic.jpg", "", 1);
            CheckPhotoVersion (db, 16, 1, "Original", "file:///tmp/database/", "sample_sony1.jpg", "", 1);
            CheckPhotoVersion (db, 17, 1, "Original", "file:///tmp/database/", "sample_sony2.jpg", "", 1);
            CheckPhotoVersion (db, 18, 1, "Original", "file:///tmp/database/", "sample_xap.jpg", "", 1);
            CheckPhotoVersion (db, 19, 1, "Original", "file:///tmp/database/", "sample_xmpcrash.jpg", "", 1);
            CheckPhotoVersion (db, 20, 1, "Original", "file:///tmp/database/test/", "sample_tangled1.jpg", "", 1);
            CheckCount (db, "photo_versions", 21);
            CheckOriginalVersionCount (db);
        }

        private void CheckTagsTable (FSpotDatabaseConnection db)
        {
            CheckTag (db, 1, "Favorites", 0, 1, -10, "stock_icon:emblem-favorite");
            CheckTag (db, 2, "Hidden", 0, 0, -9, "stock_icon:emblem-readonly");
            CheckTag (db, 3, "People", 0, 1, -8, "stock_icon:emblem-people");
            CheckTag (db, 4, "Places", 0, 1, -8, "stock_icon:emblem-places");
            CheckTag (db, 5, "Events", 0, 1, -7, "stock_icon:emblem-event");
            CheckTag (db, 6, "Imported Tags", 0, 1, 0, "stock_icon:gtk-new");
            CheckTag (db, 7, "keyword1", 6, 1, 0, "");
            CheckTag (db, 8, "keyword2", 6, 1, 0, "");
            CheckTag (db, 9, "keyword3", 6, 1, 0, "");
            CheckTag (db, 10, "keyword 1", 6, 1, 0, "");
            CheckTag (db, 11, "keyword 2", 6, 1, 0, "");
            CheckTag (db, 12, "Kirche Sulzbach", 6, 1, 0, "");
            CheckTag (db, 13, "Nikon D70s", 6, 1, 0, "");
            CheckTag (db, 14, "Food", 6, 1, 0, "");
            CheckTag (db, 15, "2007", 6, 1, 0, "");
            CheckTag (db, 16, "2006", 6, 1, 0, "");
            CheckTag (db, 17, "Neujahr", 6, 1, 0, tag_icon_emblem);
            CheckTag (db, 18, "Sylvester", 6, 1, 0, "");
            CheckTag (db, 19, "Olympus µ 700", 6, 1, 0, "");
            CheckTag (db, 20, "Rom 2006-10", 6, 1, 0, "");
            CheckTag (db, 21, "Architecture", 5, 1, 0, tag_icon_img);
            CheckTag (db, 22, "Flughafen", 6, 1, 0, "");
            CheckTag (db, 23, "Basel", 6, 1, 0, "");
            CheckTag (db, 24, "FreeFoto.com", 6, 1, 0, "");
            CheckTag (db, 25, "City", 6, 1, 0, "stock_icon:emblem-places");
            CheckTag (db, 26, " ", 25, 1, 0, "");
            CheckTag (db, 27, "State", 6, 1, 0, "stock_icon:emblem-places");
            CheckTag (db, 28, "Country", 6, 1, 0, "stock_icon:emblem-places");
            CheckTag (db, 29, "Ubited Kingdom", 28, 1, 0, "");
            CheckTag (db, 30, "Communications", 6, 1, 0, "");
            CheckTag (db, 31, "Türkei 2004", 6, 1, 0, "");
            CheckCount (db, "tags", 31);
        }

        private void CheckPhoto (FSpotDatabaseConnection db, uint id, uint time, string base_uri, string filename, string description, uint roll_id, uint default_version_id, uint rating)
        {
            var reader = db.Query ("SELECT id, time, base_uri, filename, description, roll_id, default_version_id, rating FROM photos WHERE id = " + id);
            var found = false;
            while (reader.Read ()) {
                Assert.AreEqual (id, Convert.ToUInt32 (reader[0]), "id on photo "+id);
                Assert.AreEqual (time, Convert.ToUInt32 (reader[1]), "time on photo "+id);
                Assert.AreEqual (base_uri, reader[2], "base_uri on photo "+id);
                Assert.AreEqual (filename, reader[3], "filename on photo "+id);
                Assert.AreEqual (description, reader[4], "description on photo "+id);
                Assert.AreEqual (roll_id, Convert.ToUInt32 (reader[5]), "roll_id on photo "+id);
                Assert.AreEqual (default_version_id, Convert.ToUInt32 (reader[6]), "default_version_id on photo "+id);
                Assert.AreEqual (rating, Convert.ToUInt32 (reader[7]), "rating on photo "+id);
                found = true;
            }
            Assert.IsTrue (found, "photo "+id+" missing");
        }

        private void CheckPhotoVersion (FSpotDatabaseConnection db, uint photo_id, uint version_id, string name, string base_uri, string filename, string import_md5, uint is_protected)
        {
            var reader = db.Query ("SELECT photo_id, version_id, name, base_uri, filename, import_md5, protected FROM photo_versions WHERE photo_id = " + photo_id + " AND version_id = " + version_id);
            var found = false;
            while (reader.Read ()) {
                Assert.AreEqual (photo_id, Convert.ToUInt32 (reader[0]), "photo_id on photo version "+photo_id+"/"+version_id);
                Assert.AreEqual (version_id, Convert.ToUInt32 (reader[1]), "version_id on photo version "+photo_id+"/"+version_id);
                Assert.AreEqual (name, reader[2], "name on photo version "+photo_id+"/"+version_id);
                Assert.AreEqual (base_uri, reader[3], "base_uri on photo version "+photo_id+"/"+version_id);
                Assert.AreEqual (filename, reader[4], "filename on photo version "+photo_id+"/"+version_id);
                Assert.AreEqual (import_md5, reader[5], "import_md5 on photo version "+photo_id+"/"+version_id);
                Assert.AreEqual (is_protected, Convert.ToUInt32 (reader[6]), "protected on photo version "+photo_id+"/"+version_id);
                found = true;
            }
            Assert.IsTrue (found, "photo version "+photo_id+"/"+version_id+" missing");
        }

        private void CheckOriginalVersionCount (FSpotDatabaseConnection db)
        {
            var photo_count = GetCount (db, "photos", "1");
            var orig_version_count = GetCount (db, "photo_versions", "version_id = 1");
            Assert.AreEqual (photo_count, orig_version_count, "Expecting an original version for each photo");
        }

        private void CheckTag (FSpotDatabaseConnection db, uint id, string name, uint cat_id, int is_cat, int sort, string icon)
        {
            var reader = db.Query ("SELECT id, name, category_id, is_category, sort_priority, icon FROM tags WHERE id = " + id);
            var found = false;
            while (reader.Read ()) {
                Assert.AreEqual (id, Convert.ToUInt32 (reader[0]), "id on tag "+id);
                Assert.AreEqual (name, reader[1], "name on tag "+id);
                Assert.AreEqual (cat_id, Convert.ToUInt32 (reader[2]), "category_id on tag "+id);
                Assert.AreEqual (is_cat, Convert.ToInt32 (reader[3]), "is_cat on tag "+id);
                Assert.AreEqual (sort, reader[4], "sort_priority on tag "+id);
                Assert.AreEqual (icon, reader[5], "icon on tag "+id);
                found = true;
            }
            Assert.IsTrue (found, "tag "+id+" missing");
        }

        private int GetCount (FSpotDatabaseConnection db, string table, string where)
        {
            return db.Query<int> ("SELECT COUNT(*) FROM "+table+" WHERE "+where);
        }

        private void CheckCount (FSpotDatabaseConnection db, string table, int count)
        {
            var counted = GetCount (db, table, "1");
            Assert.AreEqual (count, counted, "Count on "+table);
        }

        private const string tag_icon_img = "R2RrUAAAJBgBAQACAAAAwAAAADAAAAAw////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wBRlAATTZoFYE+dCKFRnQfBUZ4H3E+dBvFOmwb8UJwG71GeB9pRnge/UJ4InU+ZBlpVmQAP////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wBJkgAOTZsHblCfBsxPnAf4c7cx9KHVb/+14Yr/x+mk/9fxv//i9s//1vC8/8Xpov+z4Ij/ndRp/2uxJ/NPmwf5UJ0Gxk6bBWZNmQAK////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8AVaoABk6bB3NQnQbxeb04967egP/b88P/3vTH/73qkP+u5Xj/oeFj/5TdTv+L2j7/l95S/6TiaP+x5n3/wuuZ/+T20v/X8r7/qtx6/3G3L/ZPnAbuUJsFZlWqAAP///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wBNmgZTUJ0H6m+1LvbA6Jr/1PG4/6vkc/+H2Df/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3TSF/+M2kD/seZ9/9vzw/+45I7/Z7Ai9E+cB+FPmgdH////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8AgIAAAk+eBY5apBT1sOCC/9Txt/+f4GD/dNIY/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3fTHf+q5HH/2fO//6fbdf9VoAz2UJwIfP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wBVlQAMUZwHumuyJ/XJ7af/quRy/3fTHf9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/fNUm/7joif/E6p//Yaoa9VCcCKpVqgAG////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AE6dAA1QnAfZfsA/+Mzuqv+U3U7/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv+i4WT/zO6q/3K4LvdQnQbJVaoABv///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8AgIAAAk+dB7uAwUH4x+2i/4fYN/9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/4PXMP+t5Xf/x+2i/9byu//l99P/3vTH/87vrf+76Y//ld1P/3XTGf9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/j9tF/8vuqf9zuDH3UJwIpf///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wD///8AT5wGi2yyKfTH7aL/hdg0/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv+h4WL/4/bQ//////////////////////////////////////////////////X87v/C65n/ftYo/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/47bQ//G7KH/YKgb9lGcCXH///8A////AP///wD///8A////AP///wD///8A////AP///wD///8A////AP///wBNmgZTXKQW9cDplv+R3Ej/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/jdpB/+D1zP//////////////////////////////////////////////////////////////////////9/zx/7PmgP900hf/c9IW/3PSFv9z0hb/c9IW/3PSFv+c31r/uuaO/1ahDfZOmAQ+////AP///wD///8A////AP///wD///8A////AP///wD///8A////AEmSAAdQnQfqqd52/5/gYP9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv+i4WT/+v32///////////////////////////////////////////////////////////////////////////////////////T8bX/etQh/3PSFv9z0hb/c9IW/3PSFv9z0hb/q+Rz/5vVYv9PnQfbAP8AAf///wD///8A////AP///wD///8A////AP///wD///8A////AE6ZB3VytzH0uumM/3XTGv9z0hb/c9IW/3PSFv9z0hb/c9IW/67leP/+//7/////////////////////////////////////////////////////////////////////////////////////////////////5PbR/3rUIf9z0hb/c9IW/3PSFv9z0hb/etQh/8Drlv9irBzzTpoGW////wD///8A////AP///wD///8A////AP///wD///8AVZkAD1GcCPKu4X3/j9tF/3PSFv9z0hb/c9IW/3PSFv9z0hb/md5V//3+/P////////////////////////////////////////////X58P/u9ej/7vXo//r8+P///////////////////////////////////////////9Pxtv900hf/c9IW/3PSFv9z0hb/c9IW/5rfV/+i22v/UJ0H6VWqAAb///8A////AP///wD///8A////AP///wD///8ATpsHc3m8Ofaw5nv/dNIX/3PSFv9z0hb/c9IW/3PSFv+A1iz/9vzv////////////////////////////////////////////9fnw/1WeD/9UnQ7/UpwN/3qzRP////////////////////////////////////////////////+054P/c9IW/3PSFv9z0hb/c9IW/3bTG/+56Ir/arIk806aBlv///8A////AP///wD///8A////AP///wD///8AUJ4Hz57XZ/+P20X/c9IW/3PSFv9z0hb/c9IW/3PSFv/G7aH/////////////////////////////////////////////////7vXo/1SdDv9Omgb/TpoG/2mpLP/////////////////////////////////////////////////4/fP/f9Yq/3PSFv9z0hb/c9IW/3PSFv+Z3lb/ktFU/1GdB7f///8A////AP///wD///8A////AP///wBNmQAUUp4L+bXmhf941B7/c9IW/3PSFv9z0hb/c9IW/4jZOv/9/vv//////////////////////+jy3//l8Nv/////////////////7vXo/1SdDv9Omgb/TpoG/2mpLP//////////////////////1ujF//b68v//////////////////////xOyc/3PSFv9z0hb/c9IW/3PSFv+B1y7/sOR+/1CbB/ZVqgAG////AP///wD///8A////AP///wBOmQVfcbUw9qbiav9z0hb/c9IW/3PSFv9z0hb/c9IW/73qkf//////////////////////+/35/1+jHf9TnQ7/kMBk/+/26f//////7vXo/1SdDv9Omgb/TpoG/2mpLP///////////9npyf9wrTb/UZwK/4+/Yv//////////////////////9/zx/3bTG/9z0hb/c9IW/3PSFv9z0hb/suZ+/2avH/FPmgdH////AP///wD///8A////AP///wBQngetj85R/5DbRv9z0hb/c9IW/3PSFv9z0hb/c9IW/+r43P//////////////////////sdKR/1efEf9Omgb/UpwM/1WfEP+ly4D/5/Le/1SdDv9Omgb/TpoG/2mpLP/n8d3/g7hQ/1WdD/9Pmgf/TpoG/1OdDv/l8Nr//////////////////////5jeVP9z0hb/c9IW/3PSFv9z0hb/nN9a/4THQ/5OmwWP////AP///wD///8A////AP///wBRngbNmdZf/4bYNf9z0hb/c9IW/3PSFv9z0hb/htg1////////////////////////////fbVI/1WeDv9Omgb/TpoG/06aBv9VnhD/Vp8S/1SdDv9Omgb/TpoG/1OdDv9VnhD/UZsK/06aBv9Omgb/TpoG/1SdDv+82aH//////////////////////7/rlf9z0hb/c9IW/3PSFv9z0hb/kdxH/47PT/9Rnwe0////AP///wD///8A////AP///wBQnQfjodxo/3/WKf9z0hb/c9IW/3PSFv9z0hb/l95S////////////////////////////+fz3/6/Rjv9YoBP/VZ0P/06aBv9Omgb/TpoG/0+bCP9Omgb/TpoG/1CbCP9Omgb/TpoG/06aBv9XnxP/aKgq/8ziuP///////////////////////////9LxtP9z0hb/c9IW/3PSFv9z0hb/idk7/5fWWv9RnwfR////AP///wD///8A////AP///wBPnAf1qOFw/3jUHv9z0hb/c9IW/3PSFv9z0hb/peJp///////////////////////////////////////3+vP/p8yC/1efEv9UnQ7/TpoG/06aBv9Omgb/TpoG/06aBv9Omgb/V58S/2ClIP/D3ar//v/+/////////////////////////////////+H1zv9z0hb/c9IW/3PSFv9z0hb/gtcv/5/cY/9QnQfo////AP///wD///8A////AFWqAANPmgf+rOR1/3PSFv9z0hb/c9IW/3PSFv9z0hb/rOR1/////////////////////////////////////////////f78/7DSkP9Pmgf/UJsI/06aBv9Omgb/TpoG/06aBv9UnhD/Vp8S/9LlwP///////////////////////////////////////////+n42v9z0hb/c9IW/3PSFv9z0hb/fNUl/6Tgaf9Pmwf4////AP///wD///8A////AP///wBOmwb4puFt/3bTG/9z0hb/c9IW/3PSFv9z0hb/n+Bg///////////////////////////////////////G36//YqUi/1afEf9Omgb/TpoG/06aBv9Omgb/TpoG/06aBv9Omgb/T5sH/1aeEf95s0P/3uzR/////////////////////////////////9v0xP9z0hb/c9IW/3PSFv9z0hb/gNYr/53cYP9QnQbs////AP///wD///8A////AP///wBRngfnnNtg/3zVJf9z0hb/c9IW/3PSFv9z0hb/ktxJ////////////////////////////1ujF/2qqLf9XnxL/TpoG/06aBv9Omgb/U50N/1efE/9Omgb/TpoG/1aeEf9Omgf/TpoG/06aBv9Pmwj/VZ4P/4W5U//u9ef//////////////////////8zvqv9z0hb/c9IW/3PSFv9z0hb/hdg0/5PVU/9RngfW////AP///wD///8A////AP///wBRnwfRktRS/4HWLf9z0hb/c9IW/3PSFv9z0hb/fNUl//z++v//////////////////////e7RF/06aBv9Omgb/TpoG/1CbCf9VnQ//irxa/1SdDv9Omgb/TpoG/1+kHf9zrzn/Vp8S/06aBv9Omgb/TpoG/1KdDP+52J3//////////////////////7PmgP9z0hb/c9IW/3PSFv9z0hb/itk8/4nORv9QnQe7////AP///wD///8A////AP///wBRnge2iM1F/4fYN/9z0hb/c9IW/3PSFv9z0hb/c9IW/9nzwP//////////////////////1ujF/1aeEf9Omgb/VZ4Q/362Sv/l8Nv/7vXo/1SdDv9Omgb/TpoG/2mpLP//////yuG1/2KlIv9XnhH/TpoG/1qhFv/5+/b//////////////////////4jZOf9z0hb/c9IW/3PSFv9z0hb/kNtG/37GOf9QnQWZ////AP///wD///8A////AP///wBPnAdxcbkr95TdTf9z0hb/c9IW/3PSFv9z0hb/c9IW/6zkdf///////////////////////////4a6Vf9rqy//1efE////////////7vXo/1SdDv9Omgb/TpoG/2mpLP////////////z9+/+31pj/XKIZ/73Zof//////////////////////6fja/3PSFv9z0hb/c9IW/3PSFv9z0hb/nN9b/2exIPJOmgZY////AP///wD///8A////AP///wBLnggiVKAM9p/fX/910xn/c9IW/3PSFv9z0hb/c9IW/3jUH//x+uf/////////////////////////////////////////////////7vXo/1SdDv9Omgb/TpoG/2mpLP//////////////////////////////////////////////////////p+Nt/3PSFv9z0hb/c9IW/3PSFv951CD/nt9e/1GcCPdVmQAP////AP///wD///8A////AP///wD///8AUZwH4I7TS/+B1i3/c9IW/3PSFv9z0hb/c9IW/3PSFv+q5HH/////////////////////////////////////////////////7vXo/1OdDf9Omgb/TpoG/2mpLP/////////////////////////////////////////////////m99X/dNIY/3PSFv9z0hb/c9IW/3PSFv+H2Tj/hs5A/1GeBsr///8A////AP///wD///8A////AP///wD///8ATZsGh3K8K/qR3Ej/c9IW/3PSFv9z0hb/c9IW/3PSFv910xn/3PTF////////////////////////////////////////////+/35/4e6Vf90rzv/dK87/63Qi/////////////////////////////////////////////z++v+S3Er/c9IW/3PSFv9z0hb/c9IW/3PSFv+X3lL/arUh9k2aB23///8A////AP///wD///8A////AP///wD///8ATZsIIVGdCfeU2VL/fdUm/3PSFv9z0hb/c9IW/3PSFv9z0hb/ftYo/+354f///////////////////////////////////////////////////////////////////////////////////////////////////////////6njcP9z0hb/c9IW/3PSFv9z0hb/c9IW/4LXMP+Q1kr/UJwH81WcABL///8A////AP///wD///8A////AP///wD///8A////AFCcBZVttyb4ktxJ/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/4rZO//t+eL////////////////////////////////////////////////////////////////////////////////////////////+//7/tuiF/3PSFv9z0hb/c9IW/3PSFv9z0hb/dNIY/5feUv9lsBr2T5sIe////wD///8A////AP///wD///8A////AP///wD///8A////AFGUABNQnQj0jdVI/4HWLf9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9+1ij/3PTF//////////////////////////////////////////////////////////////////////////////////n99f+f4GD/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/htg2/4jSP/9QnAbtVY4ACf///wD///8A////AP///wD///8A////AP///wD///8A////AAAAAAFOmgZ5XaoW9ZTcTv961CL/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/ddMa/6zkdf/y++n////////////////////////////////////////////////////////////+//7/zu+t/4XYM/9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9+1ij/lNtO/1mlEPROmQVfAAAAAf///wD///8A////AP///wD///8A////AP///wD///8AAAAAAQAAAAEAVQADT5wHvGm3IPeS3En/dtMb/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv951CD/ruV5/9rzwv/8/vr/////////////////////////////////7fnh/8Lrmv+N2kL/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/c9IW/3nUIP+U3U3/Yq8Y9k+cCKUAAAACAAAAAgAAAAEAAAAB////AP///wD///8A////AP///wAAAAABAAAAAQAAAAIAAAAEOXEAElCdB992wyv8jdpC/3bTG/9z0hb/c9IW/3PSFv9z0hb/c9IW/3PSFv9z0hb/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";

        private const string tag_icon_emblem = "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LTI1Mi0vPjQySDo0VUI3X0k/Z09FcFZNeFxVfWFagmdhiG1pg2tlfmhgeGVbdmRYd2RXeGRWeWRVeWRV9sDN9sDN3bO8s5ugkYmKa3NyV2dqR11kM1BcNFNgOlllQWFsSGZvTWdtVGhqW2lnV2ZmUmNjTWBhTV9hT19hUmBhVGFiR1pdO1RYMU9ULUdMLj1BLzQ5My4xQDY0Szw3WEQ6YUtBaVFHclhPeF5Xe2JcfmdjgWxqgGxnfm1lfW1iemtec2ZYbGFRZl1NZl1N6Km36Km30J+qqY6ViYCFZnJzWW1xT2lvQmRsQWRuQ2ZvRWhyR2dwR2NrR15kR1ldSVpdTFtdTl1dUl9fWWRiX2hlZWxoVWVkRV1gOFdcMkxTMUBFMTc7My8xQjc2Tj45XEc9Z1FHbllPd2Nae2lhd2ljcmhmbWhpcmtpeXBpgHRqfXFmcGZaY1tPWFJGWFJG2pGh2pGhxIuYnoGLgHiAYXF1W3N4V3V6UXh9TnV8THJ6Sm53RmhyQF9pOlReM0pUO05VRVRXT1pZV19cYmhjbXFqdnhvY29rUGVnQF5kN1JZNERKMjk9MzAyQzk3UUE8YUpBbFZNdGFXfW5kfXRsc29rZmppWWVoZWtrdHNug3pygXdtbmZdWlRMSkdASkdAz36Pz36PunuKlnaCenJ8XXB2XXh+XX+DXYeLWIOHVHyCTnR8RWlzO1xoL01aIz1MMEVPQE5SUFdWXGBbamxkeHhtg4F0bXdxWGxtR2RqO1dfN0hNMzs/MzEyRTs5U0I+ZUxEcVtReGdegXdtf3x0cHVwXGtsSWJnWmtscHVyhYB5hHtza2VfU1BKPz46Pz46u3B9u3B9rHF8knR8fXd8aHp9ZH6AYYGDXYWHVnyAT3N4RmhuPVtkNFBZKUJMHjRALT5FP0tMUlhUXGBZZGhha3BocnduYXBsUGlrQmNpO1phOk9TOkVJPD4/TENBWEhDZ05FcV1TeGphgXtyf4J6cXp2YHFyTmhtWm5wanV0eXx4eHhyZGViUVJRQkREQkREqGFqqGFqnmhvjnN3gXx9coSDa4ODZYODXYKDU3Z5SmpuP1xhNE5VLERLIjg/GCwzKjg8P0hHVFhRXWBYXmVeX2lkYGxoVGloR2VoPWNoOl1jPlZaQVBTRkpLU0xJXU5IaVBHcl9VeG5kgIB2f4d/coB8Y3d3U29zW3F0ZHV1bXl3bHRyXWVlT1VYRElORElOmVZcmVZck2Bki3JyhIB+eo2JcIiGaISDXX+AUXFzRmNmOVJXLUNJJjpAHS80FCUpJzM0P0ZCVllQXmFXWmJbVWNgUmRjSWNlQWNmOmJoOl9lQVtfRlhaTlRVWFNQYFNMalJIcmFXeHBmgIN6f4uEc4WAZXx8V3R4W3R3X3V3ZHZ2YnFyWGRnTlhdRk5VRk5VmF5dmF5dk2dlinVyg4F8eouGboWDZYCBWXp+TGxwQl5jNk5SKz9EJDc8Gy0xEiMmJDEyOkRAUVZPV15WUl5bTV9gSV9kQ2BlPWFnOWJoO2FnQl9kSF5iUFxfWlpZYllUbFdPcmVcdnNre4R9eoyHc4eEaYCAYHp9YHl8YXh7YXh7X3N3WmluVV5lUlZdUlZdo3Vso3Vsm3hvjHx1gIB6coJ+Zn19XXl9UXR9R2dwQFxkN05ULUJHJTo/HC81EiUrITI1M0FBRlFOTFhWSVtcR11jRV9oQWBpPWFqOmJqPGJqQmFpRmFpTWBnWGBiYV9ebF9ZcGpkcXVwc4J/c4mIcYeGboSEbIGCaoCCZ36DZH2DY3mAY3B2Y2hsY2FlY2FlrYd4rYd4oYV4joJ4fn93bHt3YHZ5V3N6S257Q2RwPltkN09WL0RJJzxBHDE4EicuHjI3LkBCPU1NQ1RVQlhdQltlQV5sP19sPWBsO2FsPWJsQWNtRGNuSmNuV2RpYGVmbWVhbm5rbXZ1bIGBbYeIb4eHcoaGdoaFcYWHbIOIZoGKZX6HanZ9bm9ycWlqcWlqr5R/r5R/oo58i4R3eXx0ZXNxWnF1Um55R2x9QmRzP11oPFNaNkpOLEFGIDU8FCoyHTM6KT9ENUpOOlBVPVZfP1toQF9wP2BvP2JvPmNvP2RwQmVyRGZ0SGd1VWlyX2tva21sa3JyZ3h6ZH+DZIOIa4WIc4eIe4iHdoiKb4eOaYaSaYSPcn6Eend5gHJxgHJxjnhljnhlhndneXVrb3RuY3NyXXZ6WHeBUnqJUnR/U250VWdmT1xZQFBQLkBDGzE3JDk/LkNIOU1RP1NZQ1piR2JsS2dzSmh0SGp1R2t1R2x2R2x4SGx5Sm15Um94WHB3YHF2X3R6XXd9WnqBWnyDXnyCYnyAZnx/Z4CFaYWNa4qVb4mUeISJgX5+iHp1iHp1c2FQc2FQcGRWa2pgZ25pYnNzX3p+XX+HWoWSXoGJY3x+aXZwY2tiUFxXOUlJIjY8KT1DM0ZMPE9UQlZcSF5lTmduU252Um94UHB5T3F7TXJ8THF8S3F8S3J9T3N9U3R+V3V/V3Z/VXaAU3eAUnZ/U3V9VHN6VXF4XHmBZIOMbYyXdI6YfYmMhoSBjX94jX94UkU2UkU2VE1BWVtUXGZiYHN0Yn+DY4iPZZOebpGWd46LgYl9fH1uZGthR1RRKT1BMENIOEpQQFJYR1lfT2NoV25yXnZ5XHd8Wnh/WHmBVXmCUniBT3iBTHeBTHiETHmGTHqJS3iGS3WDSXJ+SG96Rmx3Q2hzQGVvTnF7XoCLb5CbepScg4+RjYqFlId8lId8PTIlPTIlRT0yUVFJW2FcZXNxZX6BZoiOZpOebpOYdo+Pf4uDeoB1Y21mR1ZVKj9DMENINklPPVBVRFZcTGFmVWxwXXV4WnZ7WHd+VniBVHiCUniCUHeBTXeBTHiES3iGSXmJS3iGTnaCUXR+T293R2dwPV1mNFNcQ2BpVnF6aYKKdIiOfYaHhoSAjIJ7jIJ7LyQZLyQZOjIoTUpCXF1Wa3JuaH1+Z4aLZJGbapGXcY6QeYqGc395Xm1qQ1ZXKT9ELkNINEhNOk1SQFNYSV5iUmltWXJ1V3N5VXR9U3WAUXaBUHaBT3aBTnaBTHaDSneFSHeHTXeEU3eBWnd+WHF1S2RpO1NaLENLO1FYT2JoY3R5bnt+dnt8fXx7g315g315HhQKHhQKLSQbSEE4XVhPcXFqbHx6aISHY4+YZo6Wa4yRcImKan5+V2xuQFZZKEBELENHMEZKNUpOO1BTRFpeTWVpVW1yUm92UHF6T3J9TnN/T3SAT3SAT3WATHWCSnWCR3WEUHeCWXmAZXt9Y3NzUGBiOUhMITA2Mj5DRlBTW2JjZmprbG5vcnJzd3V3d3V3HBEIHBEILCIYST81X1ZMdHBmbXl1Z4CCYIqRY4uTZ4qRbImPZoCFVW90P1lfKURKK0VKLkZLMUhLNkxQP1ZbSGFmT2luTWtzS213Sm56SnB9THJ+TXN/TnSASnOASHKARHGAUHV/XXl+bX18bHVyWGBePkVGJCotMzc5RkhIWVhXY2JhaGlqbnBzc3V6c3V6LB8WLB8WOS0jUEU6YVhNcm1ianVwZHt7XYOJYYeOZomQbIyTaIWMV3V8Q2FoLkxULktSLklOLkdLMUpOOlRZQ15jSWVrSGdvR2lzRmt2R216SXB8SnF+S3OASHF/RXB+Qm59T3N8Xnd7b317cXdyYGVhSk1LNTY1QEFATk5NXVxaZWVkbW5vdHZ7en2Een2EQTAoQTAoSjsxWUxBZFpPb2leZ29qYXRzWXt/XoKIZYiPbY+XaoyVW32GR2pzM1dhMVJbLkxTKkZMLEdMNFBWPFlfQmFnQmNrQmVvQWdyQ2l2RW16RnB9R3KARG99Qm17P2p4Tm94XnV4cn15eHpya2pkW1dSSkRAUUxIWVZTYmBeaWlocnR2e3+FgoiQgoiQVkI5VkI5Wkk/YlRJZ1xQa2ZaZGpjXW5rVnJ0XH2CZIeObpOcbZOeXoWQS3N/OWFtNFlkLU9YJ0VMJ0RLLk1TNVVcO1xjPF5mPGBqPWJtPmZyQWt4Qm58Q3GAQW18Pmp4PGZ0TWx0X3R2dX13fnxydXBna2JYYFNKYlhRZV5ZZ2VibGxsd3p+goePipKdipKdbltQbltQbVxSal9VaGFXZWRaXmhjWWtqU29zWHl/X4KKaI6YZ4+bW4SRTXeEP2l3OF9rMFJcKEZNJkNKK0pRMVBXNVVcN1hgOVtlOl5oPmNuQ2t2R3B9SnWCR3F+RG17QGl3TW11W3J1bXh0d3lxdXNqdGxicmZacmlhcW1pcXFxdHd6foOJiI+Yj5mjj5mjjXltjXlthHRqdW1kamZfXWFbWGRjVGdpT2pxU3R9WX2GYIiTYIuXWISRT3yKRXSCPWd0M1diKUdPJkJKKEZNK0lRLUxUMVFZNVVeOFliPmFqRmt1TXJ+UnqFTnWBSnJ+Rm56Tm53V3B0Y3JwbnRudnduf3puiX1uhX50gX98fICEfoWLh4+Xj5qilqKrlqKrq5iKq5iKm42BgHpza2xnVV5cUWFjT2NoS2ZvTm96U3eDWIGOWYaUVYSSUYGQTH6OQm59NltnKkhRJUFJJUJKJUNLJURLK0lRME9XNVRcPl5mSWt0UnV+W36IVXqEUXaBTHJ9T3B4VG5zWWxtZXBsdnpyi4d6oJWCmZOIkZKPiJCXiJOdkJull6Stnaq0naq0taOVtaOVo5aLhoF6bnBsVWBfUGFkTGJoR2NtSWt2TXN/UnyJVIKQU4KQUYKRUIKSR3OBO2BsME1XKkVOKEVNJkRMJENLKklRME9YNVRdP19nTGx0VnZ/X4CJWn2GVnqDUXaAUnJ6VG91VmtuYnBud313kY6CqqCNoJqPkpSShY2Vgo6Zi5ahk5+omaaumaausKCUsKCUoZaLiIR8dHZxXWhlU2RmSmJnQF9oQmVwRm14S3aDTn2KUH6MUYGOU4OQTXeDRWlyPVpiOFNaM1BXLU1VKUtTLlBZNFZgOFtlQmRtTm94WHeAYX+IXn6GW36GWHyEWHh/WXV6WnB0ZXR0eYF9lJKIrqOUnJaNhoaGcHZ+a3R/dX6IgImRiJGYiJGYrJ2TrJ2Tn5WMi4d/enx1ZnBrVmhoSWJmOVtjOmBqPmdyRHB8SXeETXuHUX+LVoSPU3uFT3F5S2dtRmBmPVtiNVZeLlJbM1hhOF5oPGJtRWl0UXJ7WniBY3+GYoCHYYGIX4OIXn6EXnp/XXV5Z3h7fIaEl5aPsaebmZKMenl5W19nVFpkYGZvbXN6d32Cd32CqJuSqJuSnpSMjYqCf4F5bXZwWGtqSGJlM1dfNFxmOWNuPmt3RXN/SniDUX6IWISNV3+GVnh+VXF1UWtwRmRrO15lMlhhNl5oO2NuPmhzR255U3R+XHmBZX+FZYKIZYSJZYeMY4OIYn+EX3l+aXyAfomJmZmUtKqglo+LcG5wSkxVQUVPT1NbXmFnaWxxaWxxqJuSqJuSnpSMjYqCf4F5bXZwWGtqSGJlM1dfNFxmOWNuPmt3RXN/SniDUX6IWISNV3+GVnh+VXF1UWtwRmRrO15lMlhhNl5oO2NuPmhzR255U3R+XHmBZX+FZYKIZYSJZYeMY4OIYn+EX3l+aXyAfomJmZmUtKqglo+LcG5wSkxVQUVPT1NbXmFnaWxxaWxx";
    }
}
