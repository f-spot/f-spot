/*
 * Cms.GammaTable.cs A very incomplete wrapper for lcms
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Serialization;

namespace Cms {
	public class GammaTable : IDisposable {
		private HandleRef handle;
		public HandleRef Handle {
			get {
				return handle;
			}
		}
		
//		internal struct GammaTableStruct {
//			public int Count;
//			public ushort StartOfData;  // ushort array Count entries long
//		}

		public GammaTable (int count, double gamma)
		{
			handle = new HandleRef (this, NativeMethods.CmsBuildGamma (count, gamma));
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

		// FIXME type should be an enum
		public GammaTable (int entry_count, int type, double [] values)
		{
			handle = new HandleRef (this, NativeMethods.CmsBuildParametricGamma (entry_count, type, values));
		}
		

		public GammaTable (ushort [] values) : this (values, 0, values.Length)
		{
		}

		public int Count {
			get {
				return NativeMethods.FCmsGammaTableGetCount (handle);
			}
		}

		public IntPtr Values {
			get {
				return NativeMethods.FCmsGammaTableGetValues (handle);
			}
		}

		public ushort this [int index] {
			get {
				unsafe {
					if (handle.Handle == (IntPtr)0)
						throw new CmsException ();
					
					if (index < 0 || index >= Count)
						throw new ArgumentOutOfRangeException (String.Format ("index {0} outside of count {1} for {2}", index, Count, handle.Handle));

					ushort *data = (ushort *)Values;
					return data [index];
				}
			}
			set {
				unsafe {
					if (handle.Handle == (IntPtr)0)
						throw new CmsException ();
					
					if (index < 0 || index >= Count)
						throw new ArgumentOutOfRangeException (String.Format ("index {0} outside of count {1} for handle {2}", index, Count, handle.Handle));


					ushort *data = (ushort *)Values;
					data [index] = value;
				}
			}
		}
		
		public GammaTable (ushort [] values, int start_offset, int length)
		{
#if true
			if (values == null)
				throw new ArgumentNullException ("values");

			handle = new HandleRef (this, NativeMethods.FCmsGammaTableNew (values, start_offset, length));
			//System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXhandle = {0}", handle.Handle);
#else
			handle = new HandleRef (this, cmsAllocGamma (length));
			//System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXhandle = {0}", handle.Handle);
			for (int i = 0; i < length; i++)
				this [i] = values [start_offset + i];
#endif
		}

		protected virtual void Cleanup ()
		{
			NativeMethods.CmsFreeGamma (handle);
		}
		
		public void Dispose ()
		{
			Cleanup ();
			System.GC.SuppressFinalize (this);
		}
		
		~GammaTable ()
		{
			Cleanup ();
		}
	}
}
