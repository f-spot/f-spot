//
// CmsDisposalTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.UnitTest
{
	[TestClass]
	public class CmsDisposalTests
	{
		// Every handle these types own is an lcmsNET SafeHandle, which already carries a
		// critical finalizer that closes it. A finalizer here releases nothing extra and
		// puts each instance on the finalizer queue -- including the transform
		// CollectionCellGridView builds per thumbnail per repaint. Written as a sweep so
		// the next wrapper added to the namespace is checked too.
		[TestMethod]
		public void NoCmsWrapperDeclaresAFinalizer ()
		{
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

			var offenders = typeof (Profile).Assembly.GetTypes ()
				.Where (type => type.Namespace == "FSpot.Cms" && typeof (IDisposable).IsAssignableFrom (type))
				.Where (type => type.GetMethod ("Finalize", flags) != null)
				.Select (type => type.FullName)
				.OrderBy (name => name, StringComparer.Ordinal)
				.ToList ();

			Assert.IsEmpty (offenders, $"these wrap a SafeHandle and need no finalizer: {string.Join (", ", offenders)}");
		}
	}
}
