// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Core.Tests
{
	[TestClass]
	public sealed class PhotoTagTests
	{
		[TestMethod]
		public void AddTagUnsafely_IgnoresNullTags ()
		{
			var photo = new Photo (null, null, 1, DateTimeUtil.FromDateTime (DateTime.UtcNow));
			photo.AddTagUnsafely (null);
			Assert.IsEmpty (photo.Tags);
		}
	}
}
