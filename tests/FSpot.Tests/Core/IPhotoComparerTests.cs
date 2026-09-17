// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;

using Hyena;

using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Core.Tests
{
	[TestClass]
	public class IPhotoComparerTests
	{
		static readonly DateTime Early = new DateTime (2020, 1, 1);
		static readonly DateTime Late = new DateTime (2021, 1, 1);

		[TestMethod]
		public void CompareDateName_OrdersByDateThenName ()
		{
			var comparer = new IPhotoComparer.CompareDateName ();
			var early = Photo (Early, "z", "/photos", "z.jpg");
			var late = Photo (Late, "a", "/photos", "a.jpg");
			var sameDayA = Photo (Early, "a", "/photos", "a.jpg");
			var sameDayB = Photo (Early, "b", "/photos", "b.jpg");

			Assert.IsLessThan (0, comparer.Compare (early, late));
			Assert.IsGreaterThan (0, comparer.Compare (late, early));
			Assert.IsLessThan (0, comparer.Compare (sameDayA, sameDayB));
			Assert.AreEqual (0, comparer.Compare (sameDayA, Photo (Early, "a", "/other", "x.jpg")));
		}

		[TestMethod]
		public void CompareDirectory_OrdersByUriThenName ()
		{
			var comparer = new IPhotoComparer.CompareDirectory ();
			var first = Photo (Late, "z", "/album-a", "z.jpg");
			var second = Photo (Early, "a", "/album-b", "a.jpg");
			var sameUriA = Photo (Early, "a", "/album-a", "same.jpg");
			var sameUriB = Photo (Early, "b", "/album-a", "same.jpg");

			Assert.IsLessThan (0, comparer.Compare (first, second));
			Assert.IsLessThan (0, comparer.Compare (sameUriA, sameUriB));
			Assert.AreEqual (0, comparer.Compare (sameUriA, Photo (Late, "a", "/album-a", "same.jpg")));
		}

		[TestMethod]
		public void Compare_OrdersByDateThenUriThenName ()
		{
			var early = Photo (Early, "z", "/b", "z.jpg");
			var late = Photo (Late, "a", "/a", "a.jpg");
			Assert.IsLessThan (0, early.Compare (late));

			var uriFirst = Photo (Early, "z", "/a", "a.jpg");
			var uriSecond = Photo (Early, "a", "/b", "a.jpg");
			Assert.IsLessThan (0, uriFirst.Compare (uriSecond));

			var nameFirst = Photo (Early, "a", "/a", "same.jpg");
			var nameSecond = Photo (Early, "b", "/a", "same.jpg");
			Assert.IsLessThan (0, nameFirst.Compare (nameSecond));
			Assert.AreEqual (0, nameFirst.Compare (Photo (Early, "a", "/a", "same.jpg")));
		}

		static IPhoto Photo (DateTime time, string name, string directory, string filename)
		{
			var version = new Mock<IPhotoVersion> ();
			version.Setup (v => v.BaseUri).Returns (new SafeUri (directory, true));
			version.Setup (v => v.Filename).Returns (filename);

			var photo = new Mock<IPhoto> ();
			photo.Setup (p => p.Time).Returns (time);
			photo.Setup (p => p.Name).Returns (name);
			photo.Setup (p => p.DefaultVersion).Returns (version.Object);
			return photo.Object;
		}
	}
}
