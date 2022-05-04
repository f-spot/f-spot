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
using System.Runtime.InteropServices;

namespace FSpot.Cms
{
	public class ToneCurve : IDisposable
	{
		bool disposed;

		public enum Type
		{
			GAMMA = 1,
			CIE_122_1966 = 2,
			IEC_61966_3 = 3,
			IEC_61966_2_1_SRGB = 4,
			TYPE_5 = 5,
			TYPE_6 = 6,
			TYPE_7 = 7,
			TYPE_8 = 8,
			S_SHAPED_SIGMOIDAL = 108,
			REVERSED_GAMMA = -1,
			REVERSED_CIE_122_1966 = -2,
			REVERSED_IEC_61966_3 = -3,
			REVERSED_IEC_61966_2_1_SRGB = -4,
			REVERSED_TYPE_5 = -5,
			REVERSED_TYPE_6 = -6,
			REVERSED_TYPE_7 = -7,
			REVERSED_TYPE_8 = -8,
			REVERSED_S_SHAPED_SIGMOIDAL = -108
		};

		public HandleRef Handle { get; private set; }

		//		internal struct GammaTableStruct {
		//			public int Count;
		//			public ushort StartOfData;  // ushort array Count entries long
		//		}

		public ToneCurve (double gamma)
		{
			Handle = new HandleRef (this, NativeMethods.CmsBuildGamma (0, gamma));
		}

		/*
		  Does build a parametric curve based on parameters.
		  Params[] does contain Gamma, a, b, c, d, e, f

		  Type 1 :
		  X = Y ^ Gamma   | Gamma = Params[0]
		  
		  Type 2 :
		  CIE 122-1966
		  Y = (aX + b)^Gamma  | X >= -b/a
		  Y = 0               | else
		  Type 3:
		  
		  IEC 61966-3
		  Y = (aX + b)^Gamma | X <= -b/a
		  Y = c              | else
		  
		  Type 4:
		  IEC 61966-2.1 (sRGB)
		  Y = (aX + b)^Gamma | X >= d
		  Y = cX             | X < d
		  
		  Type 5:
		  
		  Y = (aX + b)^Gamma + e | X <= d
		  Y = cX + f             | else
		  
		  Giving negative values for Type does result in reversed curve.
		*/

		public ToneCurve (Type type, double[] values)
		{
			Handle = new HandleRef (this, NativeMethods.CmsBuildParametricToneCurve (0, (int)type, values));
		}

		public ToneCurve (ushort[] values) : this (values, 0, values.Length)
		{
		}

		public uint Count {
			get {
				return NativeMethods.CmsGetToneCurveEstimatedTableEntries (Handle);
			}
		}

		public IntPtr Values {
			get {
				return NativeMethods.CmsGetToneCurveEstimatedTable (Handle);
			}
		}

		public ushort this[int index] {
			get {
				unsafe {
					if (Handle.Handle == (IntPtr)0)
						throw new CmsException ();

					if (index < 0 || index >= Count)
						throw new ArgumentOutOfRangeException ($"index {index} outside of count {Count} for {Handle.Handle}");

					ushort* data = (ushort*)Values;
					return data[index];
				}
			}
			set {
				unsafe {
					if (Handle.Handle == (IntPtr)0)
						throw new CmsException ();

					if (index < 0 || index >= Count)
						throw new ArgumentOutOfRangeException ($"index {index} outside of count {Count} for handle {Handle.Handle}");

					ushort* data = (ushort*)Values;
					data[index] = value;
				}
			}
		}

		/// <summary>
		/// Gamma table tone curve
		/// </summary>
		public ToneCurve (ushort[] values, int startOffset, int length)
		{
#if true
			if (values == null)
				throw new ArgumentNullException (nameof (values));

			if (startOffset != 0)
				Array.Copy (values, startOffset, values, 0, length);

			Handle = new HandleRef (
				this, NativeMethods.CmsBuildTabulatedToneCurve16 (
					IntPtr.Zero,
					length,
					values
				));
			//System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXhandle = {0}", handle.Handle);
#else
			handle = new HandleRef (this, cmsAllocGamma (length));
			//System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXhandle = {0}", handle.Handle);
			for (int i = 0; i < length; i++)
				this [i] = values [start_offset + i];
#endif
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

			if (disposing) {
				// free managed resources
			}
			// free unmanaged resources
			NativeMethods.CmsFreeToneCurve (Handle);
		}

		~ToneCurve ()
		{
			Dispose (false);
		}
	}
}
