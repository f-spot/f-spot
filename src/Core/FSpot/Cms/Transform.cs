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
using System.Linq;

using lcmsNET;

namespace FSpot.Cms
{
	public class Transform : IDisposable
	{
		bool disposed;

		internal lcmsNET.Transform Native { get; }

		readonly int inputBytesPerPixel;
		readonly int outputBytesPerPixel;

		public Transform (Profile[] profiles,
				  Format inputFormat,
				  Format outputFormat,
				  Intent intent, uint flags)
		{
			if (profiles == null)
				throw new ArgumentNullException (nameof (profiles));
			if (profiles.Length == 0 || profiles.Any (p => p == null))
				throw new ArgumentException ("a transform needs at least one profile and no nulls", nameof (profiles));

			inputBytesPerPixel = inputFormat.BytesPerPixel ();
			outputBytesPerPixel = outputFormat.BytesPerPixel ();

			try {
				Native = lcmsNET.Transform.Create (profiles.Select (p => p.Native).ToArray (),
					(uint)inputFormat, (uint)outputFormat, (lcmsNET.Intent)intent, (CmsFlags)flags);
			} catch (LcmsNETException e) {
				// cmsDoTransform segfaults on a null handle rather than reporting an
				// error, so a transform lcms declined to build has to fail here.
				throw new CmsException ("Unable to create the colour transform", e);
			} finally {
				// lcmsNET reads the profile handles and drops its own array before the
				// native call returns; Adjustment.Adjust has no other reference to the
				// abstract profiles it just built, so nothing else keeps them alive.
				GC.KeepAlive (profiles);
			}
		}

		public Transform (Profile input, Format inputFormat,
				  Profile output, Format outputFormat,
				  Intent intent, uint flags)
			: this (new[] { input ?? throw new ArgumentNullException (nameof (input)),
					output ?? throw new ArgumentNullException (nameof (output)) },
				inputFormat, outputFormat, intent, flags)
		{
		}

		// Input and output may alias: ColorManagement.ApplyProfile converts a pixbuf in
		// place, which cmsDoTransform supports for matching formats.
		public unsafe void Apply (IntPtr input, IntPtr output, uint size)
		{
			if (disposed)
				throw new ObjectDisposedException (nameof (Transform));
			if (input == IntPtr.Zero)
				throw new ArgumentNullException (nameof (input));
			if (output == IntPtr.Zero)
				throw new ArgumentNullException (nameof (output));

			checked {
				var source = new ReadOnlySpan<byte> ((void*)input, (int)size * inputBytesPerPixel);
				var destination = new Span<byte> ((void*)output, (int)size * outputBytesPerPixel);
				Native.DoTransform (source, destination, (int)size);
			}
		}

		// No finalizer: lcmsNET.Transform is a SafeHandle and frees itself when finalised.
		public void Dispose ()
		{
			if (disposed)
				return;

			disposed = true;
			Native.Dispose ();
		}
	}
}
