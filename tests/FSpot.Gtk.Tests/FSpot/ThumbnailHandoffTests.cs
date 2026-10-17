// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Loaders;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Gtk.Tests
{
	[TestClass]
	public sealed class ThumbnailHandoffTests
	{
		[TestMethod]
		public void Release_DisposesAThumbnailTheViewNeverClaimed ()
		{
			var handoff = new ThumbnailHandoff ();
			handoff.Hold ();

			Assert.IsTrue (handoff.Release ());
			Assert.IsFalse (handoff.Release ());
		}

		[TestMethod]
		public void Release_LeavesAClaimedThumbnailForTheViewToDisposeOnce ()
		{
			var handoff = new ThumbnailHandoff ();
			handoff.Hold ();
			handoff.Claim ();

			Assert.IsFalse (handoff.Release ());
			Assert.IsFalse (handoff.Release ());
		}

		[TestMethod]
		public void Release_WithoutAThumbnailDoesNotDispose ()
		{
			var handoff = new ThumbnailHandoff ();

			Assert.IsFalse (handoff.Release ());
		}
	}
}
