//
// StringUtilTests.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.IO;
using System.Linq;
using NUnit.Framework;
using Hyena;

namespace Hyena.Tests
{
    [TestFixture]
    public class StringUtilTests
    {
        private class Map
        {
            public Map (string camel, string under)
            {
                Camel = camel;
                Under = under;
            }

            public string Camel;
            public string Under;
        }

        private Map [] u_to_c_maps = new Map [] {
            new Map ("Hello", "hello"),
            new Map ("HelloWorld", "hello_world"),
            new Map ("HelloWorld", "hello__world"),
            new Map ("HelloWorld", "hello___world"),
            new Map ("HelloWorld", "hello____world"),
            new Map ("HelloWorld", "_hello_world"),
            new Map ("HelloWorld", "__hello__world"),
            new Map ("HelloWorld", "___hello_world_"),
            new Map ("HelloWorldHowAreYou", "_hello_World_HOW_ARE__YOU__"),
            new Map (null, ""),
            new Map ("H", "h")
        };

        [Test]
        public void UnderCaseToCamelCase ()
        {
            foreach (Map map in u_to_c_maps) {
                Assert.AreEqual (map.Camel, StringUtil.UnderCaseToCamelCase (map.Under));
            }
        }

        private Map [] c_to_u_maps = new Map [] {
            new Map ("Hello", "hello"),
            new Map ("HelloWorld", "hello_world"),
            new Map ("HiWorldHowAreYouDoingToday", "hi_world_how_are_you_doing_today"),
            new Map ("SRSLYHowAreYou", "srsly_how_are_you"),
            new Map ("OMGThisShitIsBananas", "omg_this_shit_is_bananas"),
            new Map ("KTHXBAI", "kthxbai"),
            new Map ("nereid.track_view_columns.MusicLibrarySource-Library/composer", "nereid.track_view_columns._music_library_source_-_library/composer"),
            new Map ("", null),
            new Map ("H", "h")
        };

        [Test]
        public void CamelCaseToUnderCase ()
        {
            foreach (Map map in c_to_u_maps) {
                Assert.AreEqual (map.Under, StringUtil.CamelCaseToUnderCase (map.Camel));
            }
        }

        [Test]
        public void DoubleToTenthsPrecision ()
        {
            // Note we are testing with locale = it_IT, hence the commas
            Assert.AreEqual ("15",      StringUtil.DoubleToTenthsPrecision (15.0));
            Assert.AreEqual ("15",      StringUtil.DoubleToTenthsPrecision (15.0334));
            Assert.AreEqual ("15,1",    StringUtil.DoubleToTenthsPrecision (15.052));
            Assert.AreEqual ("15,5",    StringUtil.DoubleToTenthsPrecision (15.5234));
            Assert.AreEqual ("15",      StringUtil.DoubleToTenthsPrecision (14.9734));
            Assert.AreEqual ("14,9",    StringUtil.DoubleToTenthsPrecision (14.92));
            Assert.AreEqual ("0,4",     StringUtil.DoubleToTenthsPrecision (0.421));
            Assert.AreEqual ("0",       StringUtil.DoubleToTenthsPrecision (0.01));
            Assert.AreEqual ("1.000,3", StringUtil.DoubleToTenthsPrecision (1000.32));
            Assert.AreEqual ("9.233",   StringUtil.DoubleToTenthsPrecision (9233));
        }

        [Test]
        public void DoubleToPluralInt ()
        {
            // This method helps us pluralize doubles. Probably a horrible i18n idea.
            Assert.AreEqual (0,     StringUtil.DoubleToPluralInt (0));
            Assert.AreEqual (1,     StringUtil.DoubleToPluralInt (1));
            Assert.AreEqual (2,     StringUtil.DoubleToPluralInt (2));
            Assert.AreEqual (1,     StringUtil.DoubleToPluralInt (0.5));
            Assert.AreEqual (2,     StringUtil.DoubleToPluralInt (1.8));
            Assert.AreEqual (22,    StringUtil.DoubleToPluralInt (21.3));
        }

        [Test]
        public void RemovesNewlines ()
        {
            Assert.AreEqual ("", StringUtil.RemoveNewlines (""));
            Assert.AreEqual (null, StringUtil.RemoveNewlines (null));
            Assert.AreEqual ("foobar", StringUtil.RemoveNewlines (@"foo
bar"));
            Assert.AreEqual ("foobar baz", StringUtil.RemoveNewlines ("foo\nbar \nbaz"));
            Assert.AreEqual ("haswindows newline andunix", StringUtil.RemoveNewlines ("has\nwindows\r\n newline \nandunix"));
        }

        [Test]
        public void RemovesHtml ()
        {
            Assert.AreEqual ("", StringUtil.RemoveHtml (""));
            Assert.AreEqual (null, StringUtil.RemoveHtml (null));
            Assert.AreEqual ("foobar", StringUtil.RemoveHtml ("foobar"));
            Assert.AreEqual ("foobar", StringUtil.RemoveHtml ("foo<baz>bar"));
            Assert.AreEqual ("foobar", StringUtil.RemoveHtml ("foo<baz/>bar"));
            Assert.AreEqual ("foobar", StringUtil.RemoveHtml ("foo</baz>bar"));
            Assert.AreEqual ("foobazbar", StringUtil.RemoveHtml ("foo<a href=\"http://lkjdflkjdflkjj\">baz</a>bar"));
            Assert.AreEqual ("foobaz foo bar", StringUtil.RemoveHtml (@"foo<a
href=http://lkjdflkjdflkjj>baz foo< /a> bar"));
        }

        [Test]
        public void TestJoin ()
        {
            var s = new string [] { "foo", "bar" };
            Assert.AreEqual ("foo, bar", s.Join (", "));
            Assert.AreEqual ("foobar", s.Join (""));
            Assert.AreEqual ("foobar", s.Join (null));
            Assert.AreEqual ("", new string [] {}.Join (", "));

            s = new string [] { "foo", "bar", "baz" };
            Assert.AreEqual ("foo -- bar -- baz", s.Join (" -- "));
        }

        [Test]
        public void TestFormatInterleaved ()
        {
            var objects = new object [] { "one", 2 };
            var format = new Func<string, string> ((fmt) => StringUtil.FormatInterleaved (fmt, objects).Select (o => o.ToString ()).Join (""));

            Assert.AreEqual ("onefoo2bar", format ("{0} foo {1} bar"));
            Assert.AreEqual ("fooone2bar", format ("foo {0} {1} bar"));
            Assert.AreEqual ("fooonebar2", format ("foo {0} bar {1}"));
            Assert.AreEqual ("onefoo bar2", format ("{0} foo bar {1}"));
        }

        [Test]
        public void TestSubstringBetween ()
        {
            Assert.AreEqual ("bar", "foobarbaz".SubstringBetween ("foo", "baz"));
            Assert.AreEqual ("barfoobam", "erefoobarfoobambazabc".SubstringBetween ("foo", "baz"));
            Assert.AreEqual (null, "foobar".SubstringBetween ("foo", "baz"));
            Assert.AreEqual (null,  "bar".SubstringBetween ("foo", "baz"));
            Assert.AreEqual (null,  "".SubstringBetween ("foo", "baz"));
        }
    }

    [TestFixture]
    public class SearchKeyTests
    {
        private void AssertSearchKey (string before, string after)
        {
            Assert.AreEqual (after, StringUtil.SearchKey (before));
        }

        [Test]
        public void TestEmpty ()
        {
            AssertSearchKey ("", "");
            AssertSearchKey (null, null);
        }

        // Test that resulting search keys are in lower-case
        [Test]
        public void TestLowercase ()
        {
            AssertSearchKey ("A", "a");
            AssertSearchKey ("\u0104", "a");
        }

        // Test that combining diacritics are removed from Latin characters.
        [Test]
        public void TestRemoveDiacritics ()
        {
            AssertSearchKey ("\u00e9", "e");
            AssertSearchKey ("e\u0301", "e");

            AssertSearchKey ("\u014d", "o");
            AssertSearchKey ("o\u0304", "o");

            AssertSearchKey ("Español", "espanol");
            AssertSearchKey ("30 años de la revolución iraní", "30 anos de la revolucion irani");
            AssertSearchKey ("FRANCÉS", "frances");

            // Polish letters
            AssertSearchKey ("ą", "a");
            AssertSearchKey ("Ą", "a");
            AssertSearchKey ("ć", "c");
            AssertSearchKey ("Ć", "c");
            AssertSearchKey ("ę", "e");
            AssertSearchKey ("Ę", "e");
            AssertSearchKey ("ł", "l");
            AssertSearchKey ("Ł", "l");
            AssertSearchKey ("ń", "n");
            AssertSearchKey ("Ń", "n");
            AssertSearchKey ("ó", "o");
            AssertSearchKey ("Ó", "o");
            AssertSearchKey ("ś", "s");
            AssertSearchKey ("Ś", "s");
            AssertSearchKey ("ź", "z");
            AssertSearchKey ("Ź", "z");
            AssertSearchKey ("ż", "z");
            AssertSearchKey ("Ż", "z");

            // Hiragana
            AssertSearchKey ("\u304c", "\u304b");

            // Cyrillic
            AssertSearchKey ("\u0451", "\u0435");
            AssertSearchKey ("\u0401", "\u0435");
            AssertSearchKey ("\u0439", "\u0438");
            AssertSearchKey ("\u0419", "\u0438");
        }

        // Test that some non-Latin characters are converted to Latin counterparts.
        [Test]
        public void TestEquivalents ()
        {
            AssertSearchKey ("\u00f8", "o");
            AssertSearchKey ("\u0142", "l");
        }

        // Test that some kinds of punctuation are removed.
        [Test]
        public void TestRemovePunctuation ()
        {
            AssertSearchKey ("'", "");
            AssertSearchKey ("\"", "");
            AssertSearchKey ("!", "");
            AssertSearchKey ("?", "");
            AssertSearchKey ("/", "");
        }

        [Test] // http://bugzilla.gnome.org/show_bug.cgi?id=573484
        public void TestCollapseSpaces ()
        {
            AssertSearchKey ("  a  \t  b  ", "a b");
            AssertSearchKey ("100 % techno", "100 techno");

            // Character in the set of special overrides
            AssertSearchKey ("a \u00f8", "a o");

            // Invalid combining character
            AssertSearchKey ("a \u0301", "a");
        }
    }

    [TestFixture]
    public class EscapeFilenameTests
    {
        private void AssertProduces (string input, string output)
        {
            Assert.AreEqual (output, StringUtil.EscapeFilename (input));
        }

        private void AssertProducesSame (string input)
        {
            AssertProduces (input, input);
        }

        [Test]
        public void TestEmpty ()
        {
            AssertProduces (null,   "");
            AssertProduces ("",     "");
            AssertProduces (" ",    "");
            AssertProduces ("   ",  "");
        }

        [Test]
        public void TestNotChanged ()
        {
            AssertProducesSame ("a");
            AssertProducesSame ("aaa");
            AssertProducesSame ("Foo Bar");
            AssertProducesSame ("03-Nur geträumt");
            AssertProducesSame ("你好");
            AssertProducesSame ("nǐ hǎo");
        }

        [Test]
        public void TestStripped ()
        {
            AssertProduces ("Foo*bar", "Foo_bar");
            AssertProduces ("</foo:bar?>", "_foo_bar_");
            AssertProduces ("</:?>", "_");
            AssertProduces ("Greetings! -* 你好?", "Greetings! -_ 你好_");
        }
    }

    [TestFixture]
    public class SortKeyTests
    {
        private void AssertSortKey (string before, object after)
        {
            Assert.AreEqual (after, StringUtil.SortKey (before));
        }

        [Test]
        public void TestNull ()
        {
            AssertSortKey (null, null);
        }

        [Test]
        public void TestEmpty ()
        {
            AssertSortKey ("", new byte[] {1, 1, 1, 1, 0});
        }

        [Test]
        public void TestSortKey ()
        {
            AssertSortKey ("a", new byte[] {14, 2, 1, 1, 1, 1, 0});
            AssertSortKey ("A", new byte[] {14, 2, 1, 1, 1, 1, 0});
            AssertSortKey ("\u0104", new byte[] {14, 2, 1, 27, 1, 1, 1, 0,});
        }
    }

    [TestFixture]
    public class EscapePathTests
    {
        private readonly char dir_sep = Path.DirectorySeparatorChar;

        private void AssertProduces (string input, string output)
        {
            Assert.AreEqual (output, StringUtil.EscapePath (input));
        }

        private void AssertProducesSame (string input)
        {
            AssertProduces (input, input);
        }

        [Test]
        public void TestEmpty ()
        {
            AssertProduces (null,   "");
            AssertProduces ("",     "");
            AssertProduces (" ",    "");
            AssertProduces ("   ",  "");
        }

        [Test]
        public void TestNotChanged ()
        {
            AssertProducesSame ("a");
            AssertProducesSame ("aaa");
            AssertProducesSame ("Foo Bar");
            AssertProducesSame ("03-Nur geträumt");
            AssertProducesSame ("превед");
            AssertProducesSame ("nǐ hǎo");

            AssertProducesSame (String.Format ("a{0}b.ogg", dir_sep));
            AssertProducesSame (String.Format ("foo{0}bar{0}01. baz.ogg", dir_sep));
            AssertProducesSame (String.Format ("{0}foo*?:", dir_sep)); // rooted, shouldn't change
        }

        [Test]
        public void TestStripped ()
        {
            AssertProduces (
                String.Format ("foo*bar{0}ham:spam.ogg", dir_sep),
                String.Format ("foo_bar{0}ham_spam.ogg", dir_sep));
            AssertProduces (
                String.Format ("..lots..{0}o.f.{0}.dots.ogg.", dir_sep),
                String.Format ("lots{0}o.f{0}dots.ogg", dir_sep));
            AssertProduces (
                String.Format ("foo{0}..{0}bar.ogg", dir_sep),
                String.Format ("foo{0}bar.ogg", dir_sep));
            AssertProduces (
                String.Format (". foo{0}01. bar.ogg. ", dir_sep),
                String.Format ("foo{0}01. bar.ogg", dir_sep));
        }
    }

    [TestFixture]
    public class SubstringCountTests
    {
        private void AssertCount (string haystack, string needle, uint expected)
        {
            Assert.AreEqual (expected, StringUtil.SubstringCount (haystack, needle));
        }

        [Test]
        public void TestEmpty ()
        {
            AssertCount ("", "a", 0);
            AssertCount ("a", "", 0);
        }

        [Test]
        public void TestNoMatches ()
        {
            AssertCount ("a", "b", 0);
            AssertCount ("with needle in", "long needle", 0);
        }

        [Test]
        public void TestMatches ()
        {
            AssertCount ("abbcbba", "a", 2);
            AssertCount ("abbcbba", "b", 4);
            AssertCount ("with needle in", "needle", 1);
        }
    }
}

#endif
