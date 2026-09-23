// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;

using FSpot.Avalonia.Input;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class DragGestureTests
{
	[TestMethod]
	public void MovedFarEnough_IgnoresAClickAndAcceptsADrag ()
	{
		var start = new Point (10, 10);
		Assert.IsFalse (DragGesture.MovedFarEnough (start, new Point (12, 11)));
		Assert.IsTrue (DragGesture.MovedFarEnough (start, new Point (10, 16)));
		Assert.IsTrue (DragGesture.MovedFarEnough (start, new Point (4, 10)));
	}
}
