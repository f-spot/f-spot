//
//  BrowsablePointerTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
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

using FSpot.Core.UnitTest.Mocks;
using Hyena;
using NUnit.Framework;

namespace FSpot.Core.UnitTest
{
	[TestFixture]
	public class BrowsablePointerTests
	{
		IPhoto photo1 = new FilePhoto (new SafeUri ("/1"));
		IPhoto photo2 = new FilePhoto (new SafeUri ("/2"));
		IPhoto photo3 = new FilePhoto (new SafeUri ("/3"));

		[Test]
		public void BrowsablePointer_IndexIsNullForEmptyCollection ()
		{
			var list = new BrowsableCollectionMock ();
			var pointer = new BrowsablePointer (list, 0);
			Assert.AreEqual (0, pointer.Index);
			Assert.IsNull (pointer.Current);
		}

		[Test]
		public void BrowsablePointer_IndexPointsToFirstItem ()
		{
			var list = new BrowsableCollectionMock (photo1);
			var pointer = new BrowsablePointer (list, 0);
			Assert.AreEqual (0, pointer.Index);
			Assert.AreEqual (photo1, pointer.Current);
		}

		[Test]
		public void BrowsablePointerTest_IndexIsOutOfBounds ()
		{
			var list = new BrowsableCollectionMock (photo1);
			var pointer = new BrowsablePointer (list, 1);
			// should this be fixed?
			Assert.AreEqual (1, pointer.Index);
			Assert.IsNull (pointer.Current);
		}

		[Test]
		public void BrowsablePointer_PointsToSecond_WhenFirstIsDeleted ()
		{
			var list = new BrowsableCollectionMock (photo1, photo2, photo3);
			var pointer = new BrowsablePointer (list, 0);
			list.RemoveAt (0);
			Assert.AreEqual (0, pointer.Index);
			Assert.AreEqual (photo2, pointer.Current);
		}

		[Test]
		public void BrowsablePointer_PointsToThird_WhenSecondIsDeleted ()
		{
			var list = new BrowsableCollectionMock (photo1, photo2, photo3);
			var pointer = new BrowsablePointer (list, 1);
			list.RemoveAt (1);
			Assert.AreEqual (1, pointer.Index);
			Assert.AreEqual (photo3, pointer.Current);
		}

		[Test]
		public void BrowsablePointer_PointsToSecond_WhenThirdIsDeleted ()
		{
			var list = new BrowsableCollectionMock (photo1, photo2, photo3);
			var pointer = new BrowsablePointer (list, 2);
			list.RemoveAt (2);
			Assert.AreEqual (1, pointer.Index);
			Assert.AreEqual (photo2, pointer.Current);
		}

		[Test]
		public void BrowsablePointer_StillPointsToSecond_WhenFirstIsDeleted ()
		{
			var list = new BrowsableCollectionMock (photo1, photo2, photo3);
			var pointer = new BrowsablePointer (list, 1);
			list.RemoveAt (0);
			Assert.AreEqual (0, pointer.Index);
			Assert.AreEqual (photo2, pointer.Current);
		}

		[Test]
		public void BrowsablePointer_StillPointsToSecond_WhenThirdIsDeleted ()
		{
			var list = new BrowsableCollectionMock (photo1, photo2, photo3);
			var pointer = new BrowsablePointer (list, 1);
			list.RemoveAt (2);
			Assert.AreEqual (1, pointer.Index);
			Assert.AreEqual (photo2, pointer.Current);
		}
	}
}
