//
// ResourceAssembly.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace FSpot.Resources
{
	public static class ResourceAssembly
	{
		public static Assembly Instance { get; } = typeof (ResourceAssembly).Assembly;
	}
}
