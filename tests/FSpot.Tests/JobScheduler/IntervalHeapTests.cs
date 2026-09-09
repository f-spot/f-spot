//
// IntervalHeapTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Banshee.Kernel;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.JobScheduler
{
	// The priority queue behind Scheduler, which orders every background job in the
	// application -- thumbnailing above all. It had no tests, so the ordering it exists
	// to provide was never pinned.
	[TestClass]
	public class IntervalHeapTests
	{
		[TestMethod]
		public void Pop_ReturnsTheHighestPriorityFirst ()
		{
			var heap = new IntervalHeap<string> ();
			heap.Push ("low", 1);
			heap.Push ("high", 100);
			heap.Push ("middle", 50);

			Assert.AreEqual ("high", heap.Pop ());
			Assert.AreEqual ("middle", heap.Pop ());
			Assert.AreEqual ("low", heap.Pop ());
		}

		[TestMethod]
		public void Pop_HandlesNegativeAndZeroPriorities ()
		{
			var heap = new IntervalHeap<string> ();
			heap.Push ("negative", -10);
			heap.Push ("zero", 0);
			heap.Push ("positive", 10);

			Assert.AreEqual ("positive", heap.Pop ());
			Assert.AreEqual ("zero", heap.Pop ());
			Assert.AreEqual ("negative", heap.Pop ());
		}

		[TestMethod]
		public void Peek_ReturnsTheNextItemWithoutRemovingIt ()
		{
			var heap = new IntervalHeap<string> ();
			heap.Push ("first", 10);
			heap.Push ("second", 5);

			Assert.AreEqual ("first", heap.Peek ());
			Assert.HasCount (2, heap);
			Assert.AreEqual ("first", heap.Pop ());
		}

		[TestMethod]
		public void Pop_OnAnEmptyHeapThrows ()
		{
			Assert.ThrowsExactly<InvalidOperationException> (() => new IntervalHeap<string> ().Pop ());
		}

		[TestMethod]
		public void Count_TracksPushesAndPops ()
		{
			var heap = new IntervalHeap<string> ();
			Assert.IsEmpty (heap);

			heap.Push ("a", 1);
			heap.Push ("b", 1);
			Assert.HasCount (2, heap);

			heap.Pop ();
			Assert.HasCount (1, heap);
		}

		[TestMethod]
		public void Remove_TakesTheItemOutAndLeavesTheOrderIntact ()
		{
			var heap = new IntervalHeap<string> ();
			heap.Push ("high", 100);
			heap.Push ("doomed", 50);
			heap.Push ("low", 1);

			Assert.IsTrue (heap.Remove ("doomed"));
			Assert.IsFalse (heap.Contains ("doomed"));
			Assert.AreEqual ("high", heap.Pop ());
			Assert.AreEqual ("low", heap.Pop ());
		}

		[TestMethod]
		public void Remove_ReportsFalseForAnItemThatWasNeverPushed ()
		{
			var heap = new IntervalHeap<string> ();
			heap.Push ("present", 1);

			Assert.IsFalse (heap.Remove ("absent"));
			Assert.HasCount (1, heap);
		}

		[TestMethod]
		public void Clear_EmptiesTheHeap ()
		{
			var heap = new IntervalHeap<string> ();
			heap.Push ("a", 1);
			heap.Push ("b", 2);

			heap.Clear ();

			Assert.IsEmpty (heap);
			Assert.IsFalse (heap.Contains ("a"));
		}

		// Scheduler pushes far more jobs than the initial capacity, so growth has to
		// preserve ordering rather than just not crash.
		[TestMethod]
		public void Pop_KeepsOrderingAfterGrowingBeyondTheInitialCapacity ()
		{
			var heap = new IntervalHeap<int> ();
			var random = new Random (1234);
			var priorities = Enumerable.Range (0, 200).Select (_ => random.Next (0, 1000)).ToList ();

			for (int i = 0; i < priorities.Count; i++)
				heap.Push (i, priorities[i]);

			int previous = int.MaxValue;
			for (int i = 0; i < priorities.Count; i++) {
				int priority = priorities[heap.Pop ()];
				Assert.IsLessThanOrEqualTo (previous, priority, $"priority {priority} came out after {previous}");
				previous = priority;
			}

			Assert.IsEmpty (heap);
		}

		[TestMethod]
		public void GetEnumerator_YieldsEveryItem ()
		{
			var heap = new IntervalHeap<string> ();
			heap.Push ("a", 1);
			heap.Push ("b", 2);
			heap.Push ("c", 3);

			Assert.AreSequenceEqual (new[] { "a", "b", "c" }, heap.OrderBy (item => item).ToArray ());
		}

		// GetEnumerator_YieldsEveryItem covers this only incidentally: OrderBy happens to
		// buffer an ICollection<T> through CopyTo, which is an implementation detail of
		// OrderBy rather than a contract. This calls it directly.
		[TestMethod]
		public void CopyTo_WritesEveryItemAtTheGivenOffset ()
		{
			var heap = new IntervalHeap<string> ();
			heap.Push ("low", 1);
			heap.Push ("high", 100);

			var array = new string[4];
			heap.CopyTo (array, 1);

			Assert.IsNull (array[0]);
			Assert.IsNull (array[3]);
			// Sorted, because the order CopyTo writes in is the internal heap layout
			// rather than anything ICollection<T> promises.
			Assert.AreSequenceEqual (new[] { "high", "low" }, array.Skip (1).Take (2).OrderBy (item => item).ToArray ());
		}

		// Array.Copy raised ArgumentException for a destination too small to hold the
		// items; the hand-rolled unwrap loop that replaced it has to keep that contract
		// rather than walking off the end with IndexOutOfRangeException.
		[TestMethod]
		public void CopyTo_WhenTheDestinationIsTooSmallThrowsArgumentException ()
		{
			var heap = new IntervalHeap<string> ();
			heap.Push ("a", 1);
			heap.Push ("b", 2);

			Assert.ThrowsExactly<ArgumentException> (() => heap.CopyTo (new string[2], 1));
		}

		[TestMethod]
		public void Peek_OnAnEmptyHeapThrows ()
		{
			Assert.ThrowsExactly<InvalidOperationException> (() => new IntervalHeap<string> ().Peek ());
		}

		[TestMethod]
		[DynamicData (nameof (NullRejectingOperations))]
		public void Operations_RejectANullItem (string name, Action<IntervalHeap<string>> operation)
		{
			Assert.ThrowsExactly<ArgumentNullException> (() => operation (new IntervalHeap<string> ()), name);
		}

		public static IEnumerable<(string name, Action<IntervalHeap<string>> operation)> NullRejectingOperations =>
		[
			("Push", heap => heap.Push (null, 0)),
			("Add", heap => heap.Add (null)),
			("Contains", heap => heap.Contains (null)),
			("Remove", heap => heap.Remove (null)),
		];

		// generation is what makes a stale enumerator throw instead of walking a mutated
		// heap. Clear() used to reset it to 0, so two pushes after a clear returned it to
		// a value an enumerator taken before the clear was still holding.
		[TestMethod]
		public void GetEnumerator_AfterClearAndTheSameNumberOfPushesStillInvalidates ()
		{
			var heap = new IntervalHeap<string> ();
			heap.Push ("a", 1);
			heap.Push ("b", 2);

			var enumerator = heap.GetEnumerator ();
			heap.Clear ();
			heap.Push ("x", 1);
			heap.Push ("y", 2);

			Assert.ThrowsExactly<InvalidOperationException> (() => enumerator.MoveNext ());
		}

		[TestMethod]
		public void GetEnumerator_AfterAPushInvalidates ()
		{
			var heap = new IntervalHeap<string> ();
			heap.Push ("a", 1);

			var enumerator = heap.GetEnumerator ();
			heap.Push ("b", 2);

			Assert.ThrowsExactly<InvalidOperationException> (() => enumerator.MoveNext ());
		}
	}
}
