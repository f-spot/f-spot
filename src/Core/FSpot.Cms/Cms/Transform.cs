//
// Transform.cs
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

namespace Cms
{
	public class Transform : IDisposable
	{
		public HandleRef Handle { get; private set; }

		public Transform (Profile [] profiles,
				  Format input_format,
				  Format output_format,
				  Intent intent, uint flags)
		{
			if (profiles == null)
				throw new ArgumentNullException ("profiles");

			HandleRef [] Handles = new HandleRef [profiles.Length];
			for (int i = 0; i < profiles.Length; i++) {
				Handles [i] = profiles [i].Handle;
			}
			
			Handle = new HandleRef (this, NativeMethods.CmsCreateMultiprofileTransform (Handles, Handles.Length,
											   input_format,
											   output_format,
											   (int)intent, flags));
		}
		
		public Transform (Profile input, Format input_format,
				  Profile output, Format output_format,
				  Intent intent, uint flags)
		{
			if (input == null)
				throw new ArgumentNullException ("input");
			if (output == null)
				throw new ArgumentNullException ("output");

			Handle = new HandleRef (this, NativeMethods.CmsCreateTransform (input.Handle, input_format,
									       output.Handle, output_format,
									       (int)intent, flags));
		}
		
		// Fixme this should probably be more type stafe 
		public void Apply (IntPtr input, IntPtr output, uint size)
		{
			NativeMethods.CmsDoTransform (Handle, input, output, size);
		}
		
		public void Dispose () 
		{
			Cleanup ();
			System.GC.SuppressFinalize (this);
		}
		
		protected virtual void Cleanup ()
		{
			NativeMethods.CmsDeleteTransform (Handle);
		}
		
		~Transform ()
		{
			Cleanup ();
		}	       
	}
}
