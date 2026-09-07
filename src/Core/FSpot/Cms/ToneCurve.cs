//
// ToneCurve.cs, modified from:
// GammaTable.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using lcmsNET;

namespace FSpot.Cms
{
	public class ToneCurve : IDisposable
	{
		bool disposed;

		internal lcmsNET.ToneCurve Native { get; }

		public ToneCurve (double gamma)
		{
			try {
				Native = lcmsNET.ToneCurve.BuildGamma (null, gamma);
			} catch (LcmsNETException e) {
				throw new CmsException ($"Unable to build a gamma {gamma} tone curve", e);
			}
		}

		/// <summary>
		/// Gamma table tone curve
		/// </summary>
		public ToneCurve (ushort[] values)
		{
			if (values == null)
				throw new ArgumentNullException (nameof (values));
			if (values.Length == 0)
				throw new ArgumentException ("a tabulated tone curve needs at least one entry", nameof (values));

			try {
				Native = lcmsNET.ToneCurve.BuildTabulated (null, values);
			} catch (LcmsNETException e) {
				throw new CmsException ("Unable to build a tabulated tone curve", e);
			}
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			// lcmsNET.ToneCurve is a SafeHandle and frees itself when finalised.
			if (disposing)
				Native.Dispose ();
		}

		~ToneCurve ()
		{
			Dispose (false);
		}
	}
}
