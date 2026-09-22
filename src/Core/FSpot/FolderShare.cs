// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot
{
	public static class FolderShare
	{
		public static double Fraction (int count, int libraryTotal)
		{
			if (libraryTotal <= 0 || count <= 0)
				return 0;
			return Math.Min (1.0, (double)count / libraryTotal);
		}
	}
}
