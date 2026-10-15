// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class PhotoPathChangeTests
	{
		[TestMethod]
		public void Rewrite_MovesAYearFolderAndKeepsAHashInTheFileName ()
		{
			string rewritten = PhotoPathChange.Rewrite ("/old/2020/a#b.jpg", "/old/", "/new/");

			SafeUri uri = SafeUri.FromPath (rewritten);
			Assert.AreEqual ("/new/2020/a#b.jpg", WithSlashes (uri.AbsolutePath));
			Assert.AreEqual ("a#b.jpg", System.IO.Path.GetFileName (uri.AbsolutePath));
		}

		[TestMethod]
		public void Rewrite_LeavesAFileThatSitsInTheOldFolder ()
		{
			Assert.IsNull (PhotoPathChange.Rewrite ("/old/file.jpg", "/old/", "/new/"));
		}

		[TestMethod]
		public void Rewrite_KeepsTheExtraCharactersWhenTheOldBaseHasNoSlash ()
		{
			Assert.AreEqual ("/new-other/a.jpg", PhotoPathChange.Rewrite ("/old-other/a.jpg", "/old", "/new"));
		}

		[TestMethod]
		public void Rewrite_MovesAWindowsYearFolderWithoutAskingTheRuntimeToRewriteSeparators ()
		{
			Assert.AreEqual (@"C:\new\2020\a#b.jpg", PhotoPathChange.Rewrite (@"C:\old\2020\a#b.jpg", @"C:\old\", @"C:\new\"));
		}

		[TestMethod]
		public void GuessOldBase_StopsAtTheFirstYearFolder ()
		{
			Assert.AreEqual ("/photos/", PhotoPathChange.GuessOldBase (new[] {
				"/photos/2020/a.jpg",
				"/other/2019/b.jpg"
			}));
		}

		[TestMethod]
		public void GuessOldBase_StopsAtAWindowsYearFolder ()
		{
			Assert.AreEqual (@"C:\photos\", PhotoPathChange.GuessOldBase (new[] { @"C:\photos\2020\a.jpg" }));
		}

		[TestMethod]
		public void GuessOldBase_IsJustASlashWhenNoYearFolderExists ()
		{
			Assert.AreEqual ("/", PhotoPathChange.GuessOldBase (new[] { "/2020/a.jpg", "/photos/a.jpg" }));
		}

		[TestMethod]
		public void SameBase_IgnoresARepeatedTrailingSlash ()
		{
			Assert.IsTrue (PhotoPathChange.SameBase ("/photos/", "/photos//"));
			Assert.IsFalse (PhotoPathChange.SameBase ("/photos", "/pictures"));
		}

		[TestMethod]
		public void Apply_RewritesAYearFolderAndLeavesAFileInTheOldFolder ()
		{
			Photo moved = PhotoAt ("/old/2020/a#b.jpg");
			Photo stayed = PhotoAt ("/old/file.jpg");

			int changed = PhotoPathChange.Apply (new[] { moved, stayed }, "/old/", "/new/");

			Assert.AreEqual (1, changed);
			Assert.AreEqual ("/new/2020/a#b.jpg", WithSlashes (moved.VersionUri (Photo.OriginalVersionId).AbsolutePath));
			Assert.IsTrue (moved.Changes.UriChanged);
			Assert.AreEqual ("/old/file.jpg", WithSlashes (stayed.VersionUri (Photo.OriginalVersionId).AbsolutePath));
			Assert.IsFalse (stayed.Changes.UriChanged);
		}

		static string WithSlashes (string path)
			=> (path ?? string.Empty).Replace ('\\', '/');

		static Photo PhotoAt (string absolutePath)
		{
			SafeUri uri = SafeUri.FromPath (absolutePath);
			var photo = new Photo (null, null, 1, DateTimeUtil.FromDateTime (new DateTime (2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
			photo.AddVersionUnsafely (Photo.OriginalVersionId, uri.GetBaseUri (), uri.GetFilename (), string.Empty, "Original", true);
			return photo;
		}
	}
}
