//
// Profile.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using lcmsNET;

namespace FSpot.Cms
{
	public class Profile : IDisposable
	{
		static Profile ()
		{
			LcmsNativeLibrary.Register ();
		}

		// Little-CMS is thread-safe per handle, but the profile-info readers share the
		// process-wide sRGB profile, so they still serialise on this.
		static readonly object lcmsLock = new object ();

		static Profile srgbProfile;

		// Built on first use rather than in a static initialiser: creating it eagerly
		// meant that merely touching this type loaded liblcms, so any code path that
		// mentions Profile depended on a native library whether it did colour work or
		// not -- and nothing in this assembly could be tested without one.
		static Profile Srgb {
			get {
				lock (lcmsLock) {
					if (srgbProfile == null)
						srgbProfile = new Profile (lcmsNET.Profile.Create_sRGB (), shared: true);

					return srgbProfile;
				}
			}
		}

		bool disposed;

		// Set only by the process-wide sRGB profile, and set through the constructor so
		// the flag is never observable as false on a profile that is already published.
		readonly bool shared;

		internal lcmsNET.Profile Native { get; }

		Profile (lcmsNET.Profile native) : this (native, false)
		{
		}

		Profile (lcmsNET.Profile native, bool shared)
		{
			Native = native ?? throw new ArgumentNullException (nameof (native));
			this.shared = shared;
		}

		// Process-wide and deliberately not the caller's to dispose -- see Dispose.
		public static Profile CreateStandardRgb ()
		{
			return Srgb;
		}

		public static Profile CreateAlternateRgb ()
		{
			// Adobe RGB (1998): D65, those primaries, encoding gamma 563/256.
			var wp = new ColorCIExyY (.3127, .329, 1.0);
			var primaries = new ColorCIExyYTriple (
				new ColorCIExyY (.64, .33, 1.0),
				new ColorCIExyY (.21, .71, 1.0),
				new ColorCIExyY (.15, .06, 1.0));

			// lcms copies the curves into the profile, so ours can go straight away.
			using var tc = new ToneCurve (563.0 / 256.0);
			return new Profile (wp, primaries, new[] { tc, tc, tc });
		}

		// Brightness does not reach the CLUT: it becomes a gamma curve on L ahead of
		// it, and the CLUT's own brightness term is always zero. That is how the C
		// implementation behaved and the golden tests pin it.
		public static Profile CreateAbstract (int nLUTPoints,
							  double Exposure,
							  double Bright,
							  double Contrast,
							  double Hue,
							  double Saturation,
							  ToneCurve[] tables,
							  ColorCIExyY srcWp,
							  ColorCIExyY destWp)
		{
			ToneCurve[] owned = null;
			if (tables == null) {
				var gamma = new ToneCurve (Math.Pow (10, -Bright / 100));
				var line = new ToneCurve (1.0);
				tables = owned = new[] { gamma, line, line };
			}

			try {
				return new Profile (BchswAbstractProfile.Create (nLUTPoints, Exposure, 0.0, Contrast, Hue, Saturation,
					srcWp.ToNative (), destWp.ToNative (), tables.Select (t => t.Native).ToArray ()));
			} catch (LcmsNETException e) {
				throw new CmsException ("Unable to create the BCHSW abstract profile", e);
			} finally {
				if (owned != null)
					foreach (var curve in owned.Distinct ())
						curve.Dispose ();
			}
		}

		public Profile (IccColorSpace colorSpace, ToneCurve[] gamma)
			: this (Wrap (() => lcmsNET.Profile.CreateLinearizationDeviceLink ((ColorSpaceSignature)colorSpace, Handles (gamma)),
				"Unable to create the linearization device link"))
		{
		}

		Profile (ColorCIExyY whitepoint, ColorCIExyYTriple primaries, ToneCurve[] gamma)
			: this (Wrap (() => lcmsNET.Profile.CreateRGB (whitepoint.ToNative (), primaries.ToNative (), Handles (gamma)),
				"Unable to create the RGB profile"))
		{
		}

		static lcmsNET.ToneCurve[] Handles (ToneCurve[] curves)
		{
			if (curves == null)
				throw new ArgumentNullException (nameof (curves));

			return curves.Select (c => c.Native).ToArray ();
		}

		static lcmsNET.Profile Wrap (Func<lcmsNET.Profile> create, string failure)
		{
			try {
				return create ();
			} catch (LcmsNETException e) {
				throw new CmsException (failure, e);
			} finally {
				// lcmsNET hands lcms the raw handles and keeps nothing alive, so the
				// caller's ToneCurve wrappers are collectable during the native call. The
				// closure is what still holds them.
				GC.KeepAlive (create);
			}
		}

		public Profile (string path) : this (ReadProfile (path))
		{
		}

		// The file is read here rather than by lcms so that a non-ASCII path resolves
		// the same way under .NET Framework, .NET and Mono, and so the bytes then go
		// through the same path as an embedded profile.
		static byte[] ReadProfile (string path)
		{
			try {
				return File.ReadAllBytes (path);
			} catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
					|| e is NotSupportedException || e is System.Security.SecurityException) {
				// ColorManagement.AddProfilesByExtension walks a directory and catches
				// CmsException per file; an IO failure escaping here would abandon every
				// profile after the bad one.
				throw new CmsException ($"Error opening ICC profile in file {path}", e);
			}
		}

		public Profile (byte[] data) : this (data, 0, data?.Length ?? 0)
		{
		}

		internal Profile (byte[] data, int startOffset, int length) : this (Open (data, startOffset, length))
		{
		}

		static lcmsNET.Profile Open (byte[] data, int startOffset, int length)
		{
			if (data == null)
				throw new ArgumentNullException (nameof (data));

			if (startOffset < 0)
				throw new ArgumentOutOfRangeException (nameof (startOffset), "startOffset < 0");

			if (data.Length - startOffset < 0)
				throw new ArgumentOutOfRangeException (nameof (startOffset), "startOffset > data.Length");

			if (length < 0 || data.Length - length - startOffset < 0)
				throw new ArgumentOutOfRangeException (nameof (length), "startOffset + length > data.Length");

			// lcmsNET pins the buffer with fixed (byte* p = &memory[0]), so an empty one
			// raises IndexOutOfRangeException before lcms sees it. A truncated download or
			// a placeholder 0-byte .icc in a scanned directory has to reach the caller as
			// the CmsException they catch per file, not as an escape that abandons the scan.
			if (length == 0)
				throw new CmsException ("Invalid Profile Data: the profile is empty");

			byte[] slice = data;
			if (startOffset != 0 || length != data.Length) {
				slice = new byte[length];
				Array.Copy (data, startOffset, slice, 0, length);
			}

			try {
				return lcmsNET.Profile.Open (slice);
			} catch (LcmsNETException e) {
				string reason = CmsDiagnostics.Take ();
				string message = string.IsNullOrEmpty (reason)
					? "Invalid Profile Data"
					: "Invalid Profile Data: " + reason;
				throw new CmsException (message, e);
			}
		}

		public IccColorSpace ColorSpace => (IccColorSpace)Native.ColorSpace;

		public IccProfileClass DeviceClass => (IccProfileClass)Native.DeviceClass;

		string ReadInfo (InfoType info)
		{
			lock (lcmsLock) {
				// lcmsNET reads the wchar_t buffer at the platform's width, which is what
				// went wrong here before: the LPWStr marshalling read "s" for "sRGB
				// built-in", and ColorManagement keys its dictionary on this string.
				return Native.GetProfileInfo (info, "en", "US") ?? string.Empty;
			}
		}

		public string ProductDescription => ReadInfo (InfoType.Description);

		// No finalizer: lcmsNET.Profile is a SafeHandle with a critical finalizer, so an
		// undisposed profile still releases its handle.
		public void Dispose ()
		{
			// The sRGB profile is process-wide: CreateStandardRgb hands the same instance
			// to every caller, and it escapes further through IImageFile.GetProfile.
			// Closing it would leave every later caller holding a dead lcms handle, so
			// the shared instance refuses disposal rather than depending on six call
			// sites each knowing they do not own it.
			if (shared || disposed)
				return;

			disposed = true;
			Native.Dispose ();
		}
	}
}
