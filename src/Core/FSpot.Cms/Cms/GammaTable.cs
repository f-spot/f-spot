//
// GammaTable.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Runtime.InteropServices;

namespace Cms {
	public class GammaTable : IDisposable {
		public HandleRef Handle { get; private set; }
		
//		internal struct GammaTableStruct {
//			public int Count;
//			public ushort StartOfData;  // ushort array Count entries long
//		}

		public GammaTable (int count, double gamma)
		{
			Handle = new HandleRef (this, NativeMethods.CmsBuildGamma (count, gamma));
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
			Handle = new HandleRef (this, NativeMethods.CmsBuildParametricGamma (entry_count, type, values));
		}
		

		public GammaTable (ushort [] values) : this (values, 0, values.Length)
		{
		}

		public int Count {
			get {
				return NativeMethods.FCmsGammaTableGetCount (Handle);
			}
		}

		public IntPtr Values {
			get {
				return NativeMethods.FCmsGammaTableGetValues (Handle);
			}
		}

		public ushort this [int index] {
			get {
				unsafe {
					if (Handle.Handle == (IntPtr)0)
						throw new CmsException ();
					
					if (index < 0 || index >= Count)
						throw new ArgumentOutOfRangeException (String.Format ("index {0} outside of count {1} for {2}", index, Count, Handle.Handle));

					ushort *data = (ushort *)Values;
					return data [index];
				}
			}
			set {
				unsafe {
					if (Handle.Handle == (IntPtr)0)
						throw new CmsException ();
					
					if (index < 0 || index >= Count)
						throw new ArgumentOutOfRangeException (String.Format ("index {0} outside of count {1} for handle {2}", index, Count, Handle.Handle));


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

			Handle = new HandleRef (this, NativeMethods.FCmsGammaTableNew (values, start_offset, length));
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
			NativeMethods.CmsFreeGamma (Handle);
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
