/*
 * GPObject.cs
 *
 * Author(s):
 *	Ewen Cheslack-Postava <echeslack@gmail.com>
 *	Larry Ewing <lewing@novell.com>
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (c) 2005-2009 Novelll, Inc.
 *
 * This is open source software. See COPYING for details.
 */
using System;
using System.Runtime.InteropServices;

namespace GPhoto2 {
	public abstract class GPObject : System.IDisposable {
		protected HandleRef handle;
		Func<HandleRef, ErrorCode> gp_object_cleaner;

		public HandleRef Handle
		{
			get { return handle; }
		}
		
		public GPObject () 
		{
		}

		public GPObject (Func<HandleRef, ErrorCode> gp_object_cleaner)
		{
			this.gp_object_cleaner = gp_object_cleaner;
		}

		protected virtual void Cleanup ()
		{
			if (gp_object_cleaner != null)
				gp_object_cleaner (handle);
		}
		
		bool is_disposed = false;
		public void Dispose () {
			lock (this) {
				if (is_disposed)
					return;
				is_disposed = true;
				Cleanup ();
				System.GC.SuppressFinalize (this);
			}
		}
		
		~GPObject ()
		{
			Cleanup ();
		}
	}
}
