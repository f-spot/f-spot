/*
 * Object.cs
 *
 * Author(s):
 *	Ewen Cheslack-Postava <echeslack@gmail.com>
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */
using System;
using System.Runtime.InteropServices;

namespace LibGPhoto2 {
	public abstract class Object : System.IDisposable {
		protected HandleRef handle;
		
		public HandleRef Handle {
			get {
				return handle;
			}
		}
		
		public Object () {}

		public Object (IntPtr ptr)
		{
			handle = new HandleRef (this, ptr);
		}
		
		protected abstract void Cleanup ();
		
		private bool is_disposed = false;

		public void Dispose () {
			lock (this) {
				if (is_disposed)
					return;
				is_disposed = true;
				Cleanup ();
				System.GC.SuppressFinalize (this);
			}
		}
		
		~Object ()
		{
			Cleanup ();
		}
	}
}
