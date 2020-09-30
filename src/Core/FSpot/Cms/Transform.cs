//
// Transform.cs
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
	public class Transform : IDisposable
	{
		bool disposed;

		public HandleRef Handle { get; private set; }

		public Transform (Profile[] profiles, Format inputFormat, Format outputFormat, Intent intent, uint flags)
		{
			if (profiles == null)
				throw new ArgumentNullException (nameof (profiles));

			var Handles = new HandleRef[profiles.Length];
			for (int i = 0; i < profiles.Length; i++) {
				Handles[i] = profiles[i].Handle;
			}

			Handle = new HandleRef (this, NativeMethods.CmsCreateMultiprofileTransform (Handles, Handles.Length, inputFormat, outputFormat, (int)intent, flags));
		}

		public Transform (Profile input, Format inputFormat, Profile output, Format outputFormat, Intent intent, uint flags)
		{
			if (input == null)
				throw new ArgumentNullException (nameof (input));
			if (output == null)
				throw new ArgumentNullException (nameof (output));

			Handle = new HandleRef (this, NativeMethods.CmsCreateTransform (input.Handle, inputFormat, output.Handle, outputFormat, (int)intent, flags));
		}

		// Fixme this should probably be more type stafe 
		public void Apply (IntPtr input, IntPtr output, uint size)
		{
			NativeMethods.CmsDoTransform (Handle, input, output, size);
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
			NativeMethods.CmsDeleteTransform (Handle);
		}

		~Transform ()
		{
			Dispose (false);
		}
	}
}
