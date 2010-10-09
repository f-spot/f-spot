//
// ImageFileTests.cs
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

#if ENABLE_TESTS
using NUnit.Framework;
using System;
using Hyena;

namespace FSpot.Imaging.Tests
{
    [TestFixture]
    public class ImageFileTests
    {
        [SetUp]
        public void Initialize () {
            GLib.GType.Init ();
        }

        [Test]
        public void CheckLoadableTypes ()
        {
            bool missing = false;

            // Test that we have loaders defined for all Taglib# parseable types.
            foreach (var key in TagLib.FileTypes.AvailableTypes.Keys) {
                Type type = TagLib.FileTypes.AvailableTypes [key];
                if (!type.IsSubclassOf (typeof (TagLib.Image.File))) {
                    continue;
                }

                var test_key = key;
                if (key.StartsWith ("taglib/")) {
                    test_key = "." + key.Substring (7);
                }

                if (!ImageFile.NameTable.ContainsKey (test_key)) {
                    Log.InformationFormat ("Missing key for {0}", test_key);
                    missing = true;
                }
            }

            Assert.IsFalse (missing, "No missing loaders for Taglib# parseable files.");
        }

        [Test]
        public void CheckTaglibSupport ()
        {
            bool missing = false;

            foreach (var key in ImageFile.NameTable.Keys) {
                string type = key;
                if (type.StartsWith ("."))
                    type = String.Format ("taglib/{0}", type.Substring (1));

                if (!TagLib.FileTypes.AvailableTypes.ContainsKey (type)) {
                    Log.InformationFormat ("Missing type support in Taglib# for {0}", type);
                    missing = true;
                }
            }

            Assert.IsFalse (missing, "No missing type support in Taglib#.");
        }
    }
}
#endif
