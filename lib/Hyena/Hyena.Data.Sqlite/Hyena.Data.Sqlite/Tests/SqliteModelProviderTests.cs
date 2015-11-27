//
// SqliteModelProviderTests.cs
//
// Author:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
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
using NUnit.Framework;
using Hyena.Data.Sqlite;

namespace Hyena.Data.Sqlite.Tests
{
    [TestFixture]
    public class SqliteModelProviderTests
    {
        private HyenaSqliteConnection connection;
        private ModelProvider provider;

        [TestFixtureSetUp]
        public void Init ()
        {
            connection = new HyenaSqliteConnection ("test.db");
            provider = new ModelProvider (connection);
        }

        [TestFixtureTearDown]
        public void Dispose ()
        {
            connection.Dispose ();
            File.Delete ("test.db");
        }

        [Test]
        public void IntMembers ()
        {
            DbBoundType newed_item = new DbBoundType ();
            newed_item.PublicIntField = 3141592;
            newed_item.PublicIntProperty = 13;
            newed_item.SetPrivateIntField (128);
            newed_item.SetPrivateIntProperty (42);

            provider.Save (newed_item);

            DbBoundType loaded_item = provider.FetchSingle (newed_item.PrimaryKey);
            Assert.AreEqual (newed_item.PublicIntField, loaded_item.PublicIntField);
            Assert.AreEqual (newed_item.PublicIntProperty, loaded_item.PublicIntProperty);
            Assert.AreEqual (newed_item.GetPrivateIntField (), loaded_item.GetPrivateIntField ());
            Assert.AreEqual (newed_item.GetPrivateIntProperty (), loaded_item.GetPrivateIntProperty ());
        }

        [Test]
        public void LongMembers ()
        {
            DbBoundType newed_item = new DbBoundType ();
            newed_item.PublicLongField = 4926227057;
            newed_item.PublicLongProperty = -932;
            newed_item.SetPrivateLongField (3243);
            newed_item.SetPrivateLongProperty (1);

            provider.Save (newed_item);

            DbBoundType loaded_item = provider.FetchSingle (newed_item.PrimaryKey);
            Assert.AreEqual (newed_item.PublicLongField, loaded_item.PublicLongField);
            Assert.AreEqual (newed_item.PublicLongProperty, loaded_item.PublicLongProperty);
            Assert.AreEqual (newed_item.GetPrivateLongField (), loaded_item.GetPrivateLongField ());
            Assert.AreEqual (newed_item.GetPrivateLongProperty (), loaded_item.GetPrivateLongProperty ());
        }

        [Test]
        public void StringMembers ()
        {
            DbBoundType newed_item = new DbBoundType ();
            newed_item.PublicStringField = "Surely you're joking, Mr. Feynman.";
            newed_item.PublicStringProperty = "Even as a splitted bark, so sunder we: This way fall I to death.";
            newed_item.SetPrivateStringField ("Who is John Galt?");
            newed_item.SetPrivateStringProperty ("The most formidable weapon against errors of every kind is Reason.");

            provider.Save (newed_item);

            DbBoundType loaded_item = provider.FetchSingle (newed_item.PrimaryKey);
            Assert.AreEqual (newed_item.PublicStringField, loaded_item.PublicStringField);
            Assert.AreEqual (newed_item.PublicStringProperty, loaded_item.PublicStringProperty);
            Assert.AreEqual (newed_item.GetPrivateStringField (), loaded_item.GetPrivateStringField ());
            Assert.AreEqual (newed_item.GetPrivateStringProperty (), loaded_item.GetPrivateStringProperty ());
        }

        [Test]
        public void BlankStringMembers ()
        {
            DbBoundType newed_item = new DbBoundType ();
            newed_item.PublicStringField = "";
            newed_item.PublicStringProperty = null;
            newed_item.SetPrivateStringField (" \t ");
            newed_item.SetPrivateStringProperty (" foo ");

            provider.Save (newed_item);

            DbBoundType loaded_item = provider.FetchSingle (newed_item.PrimaryKey);
            Assert.AreEqual (null, loaded_item.PublicStringField);
            Assert.AreEqual (null, loaded_item.PublicStringProperty);
            Assert.AreEqual (null, loaded_item.GetPrivateStringField ());
            Assert.AreEqual (" foo ", loaded_item.GetPrivateStringProperty ());
        }

        [Test]
        public void NullStringMembers ()
        {
            DbBoundType newed_item = new DbBoundType ();
            newed_item.PublicStringField = null;
            newed_item.PublicStringProperty = null;
            newed_item.SetPrivateStringField (null);
            newed_item.SetPrivateStringProperty (null);

            provider.Save (newed_item);

            DbBoundType loaded_item = provider.FetchSingle (newed_item.PrimaryKey);
            Assert.AreEqual (newed_item.PublicStringField, loaded_item.PublicStringField);
            Assert.AreEqual (newed_item.PublicStringProperty, loaded_item.PublicStringProperty);
            Assert.AreEqual (newed_item.GetPrivateStringField (), loaded_item.GetPrivateStringField ());
            Assert.AreEqual (newed_item.GetPrivateStringProperty (), loaded_item.GetPrivateStringProperty ());
        }

        // Some fidelity is lost in the conversion from DT to DB time format
        private void AssertArePrettyClose (DateTime time1, DateTime time2)
        {
            Assert.AreEqual (time1.Year, time2.Year);
            Assert.AreEqual (time1.Month, time2.Month);
            Assert.AreEqual (time1.Day, time2.Day);
            Assert.AreEqual (time1.Hour, time2.Hour);
            Assert.AreEqual (time1.Minute, time2.Minute);
            Assert.AreEqual (time1.Second, time2.Second);
        }

        [Test]
        public void DateTimeMembers ()
        {
            DbBoundType newed_item = new DbBoundType ();
            newed_item.PublicDateTimeField = DateTime.Now;
            newed_item.PublicDateTimeProperty = new DateTime (1986, 4, 23);
            newed_item.SetPrivateDateTimeField (DateTime.MinValue);
            newed_item.SetPrivateDateTimeProperty (DateTime.Now);

            provider.Save (newed_item);

            string command = String.Format ("SELECT PrivateDateTimeField FROM {0} WHERE PrimaryKey = {1}", provider.TableName, newed_item.PrimaryKey);

            using (IDataReader reader = connection.Query (command)) {
                reader.Read ();
                Assert.IsTrue (reader[0] == null);
            }

            DbBoundType loaded_item = provider.FetchSingle (newed_item.PrimaryKey);
            AssertArePrettyClose (newed_item.PublicDateTimeField, loaded_item.PublicDateTimeField);
            AssertArePrettyClose (newed_item.PublicDateTimeProperty, loaded_item.PublicDateTimeProperty);
            AssertArePrettyClose (newed_item.GetPrivateDateTimeField (), loaded_item.GetPrivateDateTimeField ());
            AssertArePrettyClose (newed_item.GetPrivateDateTimeProperty (), loaded_item.GetPrivateDateTimeProperty ());
        }

        [Test]
        public void TimeSpanMembers ()
        {
            DbBoundType newed_item = new DbBoundType ();
            newed_item.PublicTimeSpanField = new TimeSpan (0, 0, 1);
            newed_item.PublicTimeSpanProperty = new TimeSpan (1, 0, 0);
            newed_item.SetPrivateTimeSpanField (new TimeSpan (1, 39, 12));
            newed_item.SetPrivateTimeSpanProperty (TimeSpan.MinValue);

            provider.Save (newed_item);

            string command = String.Format ("SELECT PrivateTimeSpanProperty FROM {0} WHERE PrimaryKey = {1}", provider.TableName, newed_item.PrimaryKey);
            using (IDataReader reader = connection.Query (command)) {
                reader.Read ();
                Assert.IsTrue (reader[0] == null);
            }

            // NUnit boxes and uses reference equality, rather than Equals()
            DbBoundType loaded_item = provider.FetchSingle (newed_item.PrimaryKey);
            Assert.AreEqual (newed_item.PublicTimeSpanField, loaded_item.PublicTimeSpanField);
            Assert.AreEqual (newed_item.PublicTimeSpanProperty, loaded_item.PublicTimeSpanProperty);
            Assert.AreEqual (newed_item.GetPrivateTimeSpanField (), loaded_item.GetPrivateTimeSpanField ());
            Assert.AreEqual (newed_item.GetPrivateTimeSpanProperty (), loaded_item.GetPrivateTimeSpanProperty ());
        }

        [Test]
        public void IntEnumMembers ()
        {
            DbBoundType newed_item = new DbBoundType ();
            newed_item.PublicIntEnumField = IntEnum.Zero;
            newed_item.PublicIntEnumProperty = IntEnum.One;
            newed_item.SetPrivateIntEnumField (IntEnum.Two);
            newed_item.SetPrivateIntEnumProperty (IntEnum.Three);

            provider.Save (newed_item);

            DbBoundType loaded_item = provider.FetchSingle (newed_item.PrimaryKey);
            Assert.AreEqual (newed_item.PublicIntEnumField, loaded_item.PublicIntEnumField);
            Assert.AreEqual (newed_item.PublicIntEnumProperty, loaded_item.PublicIntEnumProperty);
            Assert.AreEqual (newed_item.GetPrivateIntEnumField (), loaded_item.GetPrivateIntEnumField ());
            Assert.AreEqual (newed_item.GetPrivateIntEnumProperty (), loaded_item.GetPrivateIntEnumProperty ());
        }

        [Test]
        public void LongEnumMembers ()
        {
            DbBoundType newed_item = new DbBoundType ();
            newed_item.PublicLongEnumField = LongEnum.Cero;
            newed_item.PublicLongEnumProperty = LongEnum.Uno;
            newed_item.SetPrivateLongEnumField (LongEnum.Dos);
            newed_item.SetPrivateLongEnumProperty (LongEnum.Tres);

            provider.Save (newed_item);

            DbBoundType loaded_item = provider.FetchSingle (newed_item.PrimaryKey);
            Assert.AreEqual (newed_item.PublicLongEnumField, loaded_item.PublicLongEnumField);
            Assert.AreEqual (newed_item.PublicLongEnumProperty, loaded_item.PublicLongEnumProperty);
            Assert.AreEqual (newed_item.GetPrivateLongEnumField (), loaded_item.GetPrivateLongEnumField ());
            Assert.AreEqual (newed_item.GetPrivateLongEnumProperty (), loaded_item.GetPrivateLongEnumProperty ());
        }
    }
}

#endif
