//
// NativeStringTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Utils.UnitTest
{
	[TestClass]
	public class NativeStringTests
	{
		[TestMethod]
		public void FromUtf8_DecodesMultiByteSequences ()
		{
			const string expected = "café 日本語 ✓";
			using var native = new Utf8Buffer (expected);

			Assert.AreEqual (expected, NativeString.FromUtf8 (native.Pointer));
		}

		[TestMethod]
		public void FromUtf8_StopsAtNulTerminator ()
		{
			using var native = new Utf8Buffer ("visible\0hidden");

			Assert.AreEqual ("visible", NativeString.FromUtf8 (native.Pointer));
		}

		[TestMethod]
		public void FromUtf8_ReturnsNullForNullPointer ()
		{
			Assert.IsNull (NativeString.FromUtf8 (IntPtr.Zero));
		}

		[TestMethod]
		public void FromUtf8_ReturnsEmptyForEmptyString ()
		{
			using var native = new Utf8Buffer (string.Empty);

			Assert.AreEqual (string.Empty, NativeString.FromUtf8 (native.Pointer));
		}

		sealed class Utf8Buffer : IDisposable
		{
			public IntPtr Pointer { get; private set; }

			public Utf8Buffer (string value)
			{
				var bytes = Encoding.UTF8.GetBytes (value);
				Pointer = Marshal.AllocHGlobal (bytes.Length + 1);
				Marshal.Copy (bytes, 0, Pointer, bytes.Length);
				Marshal.WriteByte (Pointer, bytes.Length, 0);
			}

			public void Dispose ()
			{
				if (Pointer == IntPtr.Zero)
					return;

				Marshal.FreeHGlobal (Pointer);
				Pointer = IntPtr.Zero;
			}
		}
	}
}
