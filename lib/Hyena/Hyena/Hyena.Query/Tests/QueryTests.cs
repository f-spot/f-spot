//
// QueryTests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#if ENABLE_TESTS

using System;
using System.Reflection;
using NUnit.Framework;

using Hyena.Query;

namespace Hyena.Query.Tests
{
    [TestFixture]
    public class QueryTests : Hyena.Tests.TestBase
    {
        private static QueryField ArtistField = new QueryField (
            "artist", "ArtistName", "Artist", "CoreArtists.NameLowered", true,
            "by", "artist", "artists"
        );

        private static QueryField AlbumField = new QueryField (
            "album", "AlbumTitle", "Album", "CoreAlbums.TitleLowered", true,
            "on", "album", "from", "albumtitle"
        );

        private static QueryField PlayCountField = new QueryField (
            "playcount", "PlayCount", "Play Count", "CoreTracks.PlayCount", typeof(IntegerQueryValue),
            "plays", "playcount", "numberofplays", "listens"
        );

        private static QueryField DurationField = new QueryField (
            "duration", "Duration", "Duration", "CoreTracks.Duration", typeof(TimeSpanQueryValue),
            "duration", "length", "time"
        );

        private static QueryField MimeTypeField = new QueryField (
            "mimetype", "MimeType", "Mime Type", "CoreTracks.MimeType {0} OR CoreTracks.Uri {0}", typeof(ExactStringQueryValue),
            "type", "mimetype", "format", "ext", "mime"
        );

        private static QueryField UriField = new QueryField (
            "uri", "Uri", "File Location", "CoreTracks.Uri", typeof(ExactUriStringQueryValue),
            "uri", "path", "file", "location"
        );

        private static QueryFieldSet FieldSet = new QueryFieldSet (
            ArtistField, AlbumField, PlayCountField, MimeTypeField, DurationField
        );

        [Test]
        public void QueryValueSql ()
        {
            QueryValue qv;

            qv = new DateQueryValue (); qv.ParseUserQuery ("2007-03-9");
            Assert.AreEqual (new DateTime (2007, 3, 9), qv.Value);
            Assert.AreEqual ("2007-03-09", qv.ToUserQuery ());
            Assert.AreEqual ("1173420000", qv.ToSql ());

            qv = new StringQueryValue (); qv.ParseUserQuery ("foo 'bar'");
            Assert.AreEqual ("foo 'bar'", qv.Value);
            Assert.AreEqual ("foo 'bar'", qv.ToUserQuery ());
            Assert.AreEqual ("foo bar", qv.ToSql ());

            qv = new StringQueryValue (); qv.ParseUserQuery ("Foo Baño");
            Assert.AreEqual ("Foo Baño", qv.Value);
            Assert.AreEqual ("Foo Baño", qv.ToUserQuery ());
            Assert.AreEqual ("foo bano", qv.ToSql ());

            qv = new ExactStringQueryValue (); qv.ParseUserQuery ("foo 'bar'");
            Assert.AreEqual ("foo 'bar'", qv.Value);
            Assert.AreEqual ("foo 'bar'", qv.ToUserQuery ());
            Assert.AreEqual ("foo ''bar''", qv.ToSql ());

            qv = new IntegerQueryValue (); qv.ParseUserQuery ("22");
            Assert.AreEqual (22, qv.Value);
            Assert.AreEqual ("22", qv.ToUserQuery ());
            Assert.AreEqual ("22", qv.ToSql ());

            qv = new FileSizeQueryValue (); qv.ParseUserQuery ("2048 KB");
            Assert.AreEqual (2097152, qv.Value);
            Assert.AreEqual ("2.048 KB", qv.ToUserQuery ());
            Assert.AreEqual ("2097152", qv.ToSql ());

            // TODO this will break once an it_IT translation for "days ago" etc is committed
            qv = new RelativeTimeSpanQueryValue (); qv.ParseUserQuery ("2 days ago");
            Assert.AreEqual (-172800, qv.Value);
            Assert.AreEqual ("2 days ago", qv.ToUserQuery ());

            // TODO this will break once an it_IT translation for "minutes" etc is committed
            qv = new TimeSpanQueryValue (); qv.ParseUserQuery ("4 minutes");
            Assert.AreEqual (240, qv.Value);
            Assert.AreEqual ("4 minutes", qv.ToUserQuery ());
            Assert.AreEqual ("240000", qv.ToSql ());
        }

        [Test]
        public void QueryParsing ()
        {
            string [] tests = new string [] {
                "foo",
                "foo bar",
                "foo -bar",
                "-foo -bar",
                "-(foo bar)",
                "-(foo or bar)",
                "-(foo (-bar or baz))",
                "-(foo (-bar or -baz))",
                "by:foo",
                "-by:foo",
                "-by!=foo",
                "duration>\"2 minutes\"",
                "plays>3",
                "-plays>3",
                "by:baz -on:bar",
                "by:baz -on:bar",
                "by:baz (plays>3 or plays<2)",
                "by:on", // bgo#601065
                "on:by",
            };

            AssertForEach<string> (tests, UserQueryParsesAndGenerates);
        }

        [Test]
        public void CustomFormatParenthesisBugFixed ()
        {
            QueryValue val = new StringQueryValue ();
            val.ParseUserQuery ("mp3");

            Assert.AreEqual (
                "(CoreTracks.MimeType LIKE '%mp3%' ESCAPE '\\' OR CoreTracks.Uri LIKE '%mp3%' ESCAPE '\\')",
                MimeTypeField.ToSql (StringQueryValue.Contains, val)
            );
        }

        [Test]
        public void EscapeSingleQuotes ()
        {
            QueryValue val = new StringQueryValue ();
            val.ParseUserQuery ("Kelli O'Hara");

            Assert.AreEqual (
                "(CoreArtists.NameLowered IS NOT NULL AND CoreArtists.NameLowered LIKE '%kelli ohara%' ESCAPE '\\')",
                ArtistField.ToSql (StringQueryValue.Contains, val)
            );
        }

        [Test] // http://bugzilla.gnome.org/show_bug.cgi?id=570312
        public void EscapeSqliteWildcards1 ()
        {
            QueryValue val = new StringQueryValue ();
            val.ParseUserQuery ("100% Techno");

            Assert.AreEqual (
                "(CoreAlbums.TitleLowered IS NOT NULL AND CoreAlbums.TitleLowered LIKE '%100 techno%' ESCAPE '\\')",
                AlbumField.ToSql (StringQueryValue.Contains, val)
            );
        }

        [Test] // http://bugzilla.gnome.org/show_bug.cgi?id=570312
        public void EscapeSqliteWildcards2 ()
        {
            QueryValue val = new StringQueryValue ();
            val.ParseUserQuery ("-_-");

            Assert.AreEqual (
                "(CoreAlbums.TitleLowered IS NOT NULL AND CoreAlbums.TitleLowered LIKE '%-\\_-%' ESCAPE '\\')",
                AlbumField.ToSql (StringQueryValue.Contains, val)
            );
        }

        [Test] // http://bugzilla.gnome.org/show_bug.cgi?id=570312
        public void EscapeSqliteWildcards3 ()
        {
            QueryValue val = new StringQueryValue ();
            val.ParseUserQuery ("Metallic/\\");

            Assert.AreEqual (
                "(CoreAlbums.TitleLowered IS NOT NULL AND CoreAlbums.TitleLowered LIKE '%metallic%' ESCAPE '\\')",
                AlbumField.ToSql (StringQueryValue.Contains, val)
            );
        }

        [Test] // http://bugzilla.gnome.org/show_bug.cgi?id=570312
        public void EscapeSqliteWildcards4Real ()
        {
            QueryValue val = new ExactStringQueryValue ();
            val.ParseUserQuery ("/\\_%`'");

            Assert.AreEqual (
                "(CoreTracks.MimeType LIKE '%/\\\\\\_\\%`''%' ESCAPE '\\' OR CoreTracks.Uri LIKE '%/\\\\\\_\\%`''%' ESCAPE '\\')",
                MimeTypeField.ToSql (StringQueryValue.Contains, val)
            );
        }

        [Test] // http://bugzilla.gnome.org/show_bug.cgi?id=612152
        public void EscapeUri ()
        {
            QueryValue val = new ExactUriStringQueryValue ();
            val.ParseUserQuery ("space 3quotes`'\"underscore_percentage%slash/backslash\\");

            Assert.AreEqual (
                @"(CoreTracks.Uri IS NOT NULL AND CoreTracks.Uri LIKE '%space\%203quotes\%60''\%22underscore\_percentage\%25slash/backslash\%5C%' ESCAPE '\')",
                UriField.ToSql (StringQueryValue.Contains, val)
            );
        }

        [Test] // http://bugzilla.gnome.org/show_bug.cgi?id=644145
        public void EscapeUriWithStartsWithOperator ()
        {
            QueryValue val = new ExactUriStringQueryValue ();
            val.ParseUserQuery ("/mnt/mydrive/rock & roll");

            Assert.AreEqual (
                @"(CoreTracks.Uri IS NOT NULL AND" + 
                @" CoreTracks.Uri LIKE 'file:///mnt/mydrive/rock\%20&\%20roll%' ESCAPE '\')",
                UriField.ToSql (StringQueryValue.StartsWith, val)
            );
        }

        [Test]
        // Test behavior issues described in
        // http://bugzilla.gnome.org/show_bug.cgi?id=547078
        public void ParenthesesInQuotes ()
        {
            string query = "artist==\"foo (disc 2)\"";

            QueryNode query_tree = UserQueryParser.Parse (query, FieldSet);
            Assert.IsNotNull (query_tree, "Query should parse");
            Assert.AreEqual ("by==\"foo (disc 2)\"", query_tree.ToUserQuery ());
        }

        private static void UserQueryParsesAndGenerates (string query)
        {
            QueryNode node = UserQueryParser.Parse (query, FieldSet);
            if (query == null || query.Trim () == String.Empty) {
                Assert.AreEqual (node, null);
                return;
            }

            Assert.AreEqual (query, node.ToUserQuery ());
        }
    }
}

#endif
