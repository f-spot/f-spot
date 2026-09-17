// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Filters.Tests
{
	[TestClass]
	public sealed class FilterSetTests
	{
		static readonly SafeUri UnusedSource = new SafeUri ("file:///photo.jpg");

		[TestMethod]
		public void Convert_Empty_ReturnsFalse ()
		{
			using var request = new FilterRequest (UnusedSource);
			Assert.IsFalse (new FilterSet ().Convert (request));
		}

		[TestMethod]
		public void Convert_RunsEveryFilterInAddOrderAndOrsTheResults ()
		{
			var order = new List<string> ();
			var set = new FilterSet ();
			set.Add (new RecordingFilter ("a", false, order));
			set.Add (new RecordingFilter ("b", true, order));
			set.Add (new RecordingFilter ("c", false, order));

			using var request = new FilterRequest (UnusedSource);
			Assert.IsTrue (set.Convert (request));
			Assert.AreSequenceEqual (new[] { "a", "b", "c" }, order);
		}

		[TestMethod]
		public void Convert_AllFalse_ReturnsFalse ()
		{
			var set = new FilterSet ();
			set.Add (new RecordingFilter ("a", false, new List<string> ()));
			set.Add (new RecordingFilter ("b", false, new List<string> ()));

			using var request = new FilterRequest (UnusedSource);
			Assert.IsFalse (set.Convert (request));
		}

		sealed class RecordingFilter : IFilter
		{
			readonly string name;
			readonly bool result;
			readonly List<string> order;

			public RecordingFilter (string name, bool result, List<string> order)
			{
				this.name = name;
				this.result = result;
				this.order = order;
			}

			public bool Convert (FilterRequest request)
			{
				order.Add (name);
				return result;
			}
		}
	}
}
