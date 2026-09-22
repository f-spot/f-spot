// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Query;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Query
{
	[TestClass]
	public sealed class QueryChipFormatTests
	{
		[TestMethod]
		public void Rating_UsesAPlaceholderFormat ()
		{
			string label = QueryChipFormat.Rating (3, 5);
			Assert.Contains ("3", label);
			Assert.Contains ("5", label);
			Assert.DoesNotContain ("{0}", label);
		}

		[TestMethod]
		public void Date_JoinsBoundsWithAPlaceholder ()
		{
			string label = QueryChipFormat.Date (new DateTime (2026, 6, 1), new DateTime (2026, 7, 1));
			Assert.Contains ("2026-06-01", label);
			Assert.Contains ("2026-07-01", label);
		}

		[TestMethod]
		public void Roll_IncludesTheLocalizedImportRollWord ()
		{
			string label = QueryChipFormat.Roll ("3");
			Assert.Contains ("3", label);
			Assert.DoesNotContain ("{0}", label);
			Assert.DoesNotContain ("{1}", label);
		}

		[TestMethod]
		public void TimelineMonth_PutsTheCountInThePlaceholder ()
		{
			string label = QueryChipFormat.TimelineMonth (new DateTime (2026, 6, 1), 4);
			Assert.Contains ("2026", label);
			Assert.Contains ("4", label);
			Assert.DoesNotContain ("{0}", label);
		}
	}
}
