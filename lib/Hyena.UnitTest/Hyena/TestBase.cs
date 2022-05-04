//
// BansheeTests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using NUnit.Framework;

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

		public static TransformPair<F, T>[] GetFrom (params object[] objects)
		{
			var pairs = new TransformPair<F, T>[objects.Length / 2];
			for (int i = 0; i < objects.Length; i += 2) {
				pairs[i / 2] = new TransformPair<F, T> ((F)objects[i], (T)objects[i + 1]);
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
		static string bin_dir;
		public static string BinDir {
			get { return bin_dir ?? (bin_dir = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location)); }
		}

		static string tests_dir;
		public static string TestsDir {
			get { return tests_dir ?? (tests_dir = Path.Combine (Path.GetDirectoryName (BinDir), "tests")); }
		}

		public static void AssertForEach<T> (IEnumerable<T> objects, Action<T> runner)
		{
			var sb = new System.Text.StringBuilder ();
			foreach (T o in objects) {
				try { runner (o); } catch (AssertionException e) { sb.AppendFormat ("Failed assertion on {0}: {1}\n", o, e.Message); } catch (Exception e) { sb.AppendFormat ("\nCaught exception on {0}: {1}\n", o, e.ToString ()); }
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
