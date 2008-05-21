/*
 * Cms.Transform.cs A very incomplete wrapper for lcms
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
	public class Transform : IDisposable {
		private HandleRef handle;
		public HandleRef Handle {
			get {
				return handle;
			}
		}

		public Transform (Profile [] profiles,
				  Format input_format,
				  Format output_format,
				  Intent intent, uint flags)
		{
			HandleRef [] handles = new HandleRef [profiles.Length];
			for (int i = 0; i < profiles.Length; i++) {
				handles [i] = profiles [i].Handle;
			}
			
			this.handle = new HandleRef (this, NativeMethods.CmsCreateMultiprofileTransform (handles, handles.Length, 
											   input_format,
											   output_format, 
											   (int)intent, flags));
		}
		
		public Transform (Profile input, Format input_format,
				  Profile output, Format output_format,
				  Intent intent, uint flags)
		{
			this.handle = new HandleRef (this, NativeMethods.CmsCreateTransform (input.Handle, input_format,
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
			NativeMethods.CmsDeleteTransform (this.handle);
		}
		
		~Transform () 
		{
			Cleanup ();
		}	       
	}
}
