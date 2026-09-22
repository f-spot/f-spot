// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Resources.Lang;

namespace FSpot
{
	public static class DeviceImport
	{
		public static string EmptyMessage => Strings.NoCamerasDetected;

		public static IReadOnlyList<string> Cameras ()
			=> Array.Empty<string> ();
	}
}
