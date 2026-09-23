// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class TagTypingTests
	{
		[TestMethod]
		public void Names_SplitsOnCommasAndDropsBlanks ()
		{
			var names = TagTyping.Names (" Holiday, ,Beach ;Night ");
			Assert.AreSequenceEqual (new[] { "Holiday", "Beach", "Night" }, names);
			Assert.IsEmpty (TagTyping.Names ("  "));
			Assert.IsEmpty (TagTyping.Names (null));
		}
	}
}
