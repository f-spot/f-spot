//
// BitConverter.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2005-2010 Novell, Inc.
// Copyright (C) 2005 Larry Ewing
// Copyright (C) 2009-2010 Ruben Vermeersch
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

namespace FSpot
{
	//[Obsolete ("use Mono.DataConvert instead")]
	public static class BitConverter
	{
		public static uint Swap (uint val, bool little)
		{
			return (little != System.BitConverter.IsLittleEndian) ?
				((uint)((((uint)(val) & (uint)0x000000ffU) << 24) |
					 (((uint)(val) & (uint)0x0000ff00U) << 8) |
					 (((uint)(val) & (uint)0x00ff0000U) >> 8) |
					 (((uint)(val) & (uint)0xff000000U) >> 24)))
				: val;
		}

		public static ushort Swap (ushort val, bool little)
		{
			return (little != System.BitConverter.IsLittleEndian) ?
				((ushort)((ushort)(val >> 8) | (ushort)(val << 8)))
				: val;
		}

		public static ushort Swap (ushort val)
		{
			return ((ushort)((ushort)(val >> 8) | (ushort)(val << 8)));
		}

		public static ulong Swap (ulong val, bool little)
		{
			return (little != System.BitConverter.IsLittleEndian) ?
		((ulong)((((ulong)(val) & (ulong)0x00000000000000ffU) << 56) |
			  (((ulong)(val) & (ulong)0x000000000000ff00U) << 40) |
			  (((ulong)(val) & (ulong)0x0000000000ff0000U) << 24) |
			  (((ulong)(val) & (ulong)0x00000000ff000000U) << 8) |
			  (((ulong)(val) & (ulong)0x000000ff00000000U) >> 8) |
			  (((ulong)(val) & (ulong)0x0000ff0000000000U) >> 24) |
			  (((ulong)(val) & (ulong)0x00ff000000000000U) >> 40) |
			  (((ulong)(val) & (ulong)0xff00000000000000U) >> 56)))
			: val;
		}

		public static byte [] GetBytes (uint val, bool little)
		{
			val = Swap (val, little);
			return System.BitConverter.GetBytes (val);
		}

		public static byte [] GetBytes (ushort val, bool little)
		{
			val = Swap (val, little);
			return System.BitConverter.GetBytes (val);
		}

		public static byte [] GetBytes (ulong val, bool little)
		{
			val = Swap (val, little);
			return System.BitConverter.GetBytes (val);
		}

		public static ushort ToUInt16 (byte [] data, int position, bool little)
		{
			ushort val = System.BitConverter.ToUInt16 (data, position);
			return Swap (val, little);
		}

		public static uint ToUInt32 (byte [] data, int position, bool little)
		{
			uint val = System.BitConverter.ToUInt32 (data, position);
			return Swap (val, little);
		}

		public static float ToSingle (byte [] data, int position, bool little)
		{
			float retval;
			unsafe {
				uint* ptr;
				ptr = (uint*)&retval;
				*ptr = ToUInt32 (data, position, little);
			}
			return retval;
		}

		public static int ToInt32 (byte [] data, int position, bool little)
		{
			return unchecked((int)ToUInt32 (data, position, little));
		}

		public static ulong ToUInt64 (byte [] data, int position, bool little)
		{
			ulong val = System.BitConverter.ToUInt64 (data, position);
			return Swap (val, little);
		}
	}
}
