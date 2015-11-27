//
// BansheeTests.cs
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
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using NUnit.Framework;

using Hyena;

namespace Hyena.Tests
{
    public struct TransformPair<F, T>
    {
        public F From;
        public T To;

        public TransformPair (F from, T to)
        {
            From = from;
            To = to;
        }

        public static TransformPair<F, T> [] GetFrom (params object [] objects)
        {
            TransformPair<F, T> [] pairs = new TransformPair<F, T> [objects.Length / 2];
            for (int i = 0; i < objects.Length; i += 2) {
                pairs[i/2] = new TransformPair<F, T> ((F)objects[i], (T)objects[i+1]);
            }
            return pairs;
        }

        public override string ToString ()
        {
            return From.ToString ();
        }
    }

    public delegate To Transform<F, To> (F from);

    public abstract class TestBase
    {
        private static string bin_dir;
        public static string BinDir {
            get { return bin_dir ?? (bin_dir = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location)); }
        }

        private static string tests_dir;
        public static string TestsDir {
            get { return tests_dir ?? (tests_dir = Path.Combine (Path.GetDirectoryName (BinDir), "tests")); }
        }

        public static void AssertForEach<T> (IEnumerable<T> objects, Action<T> runner)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder ();
            foreach (T o in objects) {
                try { runner (o); }
                catch (AssertionException e) { sb.AppendFormat ("Failed assertion on {0}: {1}\n", o, e.Message); }
                catch (Exception e) { sb.AppendFormat ("\nCaught exception on {0}: {1}\n", o, e.ToString ()); }
            }

            if (sb.Length > 0)
                Assert.Fail ("\n" + sb.ToString ());
        }

        // Fails to compile, causes SIGABRT in gmcs; boo
        /*public static void AssertTransformsEach<A, B> (IEnumerable<TransformPair<A, B>> pairs, Transform<A, B> transform)
        {
            AssertForEach (pairs, delegate (TransformPair<A, B> pair) {
                Assert.AreEqual (pair.To, transform (pair.From));
            });
        }*/
    }
}

#endif
