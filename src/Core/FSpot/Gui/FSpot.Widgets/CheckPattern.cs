//
// CheckPattern.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gdk;

namespace FSpot.Widgets
{
	public struct CheckPattern
	{
		uint color1, color2;
		int check_size;

		public CheckPattern (uint color1, uint color2, int checkSize)
		{
			this.color1 = color1;
			this.color2 = color2;
			check_size = checkSize;
		}

		public CheckPattern (string color1, string color2, int checkSize) : this (s_to_h (color1), s_to_h (color2), checkSize)
		{
		}

		public CheckPattern (uint transparentColor)
		{
			color1 = color2 = transparentColor;
			check_size = 32;
		}

		//format "#000000"
		public CheckPattern (string transparentColor) : this (s_to_h (transparentColor))
		{
		}

		public CheckPattern (Color transparentColor) : this (c_to_h (transparentColor))
		{
		}

		public uint Color1 {
			get { return color1; }
		}

		public uint Color2 {
			get { return color2; }
		}

		public int CheckSize {
			get { return check_size; }
		}

		public static CheckPattern Dark = new CheckPattern (0x00000000, 0x00555555, 8);
		public static CheckPattern Midtone = new CheckPattern (0x00555555, 0x00aaaaaa, 8);
		public static CheckPattern Light = new CheckPattern (0x00aaaaaa, 0x00ffffff, 8);
		public static CheckPattern Black = new CheckPattern (0x00000000, 0x00000000, 8);
		public static CheckPattern Gray = new CheckPattern (0x00808080, 0x00808080, 8);
		public static CheckPattern White = new CheckPattern (0x00ffffff, 0x00ffffff, 8);

		public static bool operator == (CheckPattern left, CheckPattern right)
		{
			return (left.color1 == right.color1) &&
				   (left.color2 == right.color2) &&
				   (left.color1 == left.color2 || left.check_size == right.check_size);
		}

		public static bool operator != (CheckPattern left, CheckPattern right)
		{
			return (left.color1 != right.color1) ||
				   (left.color2 != right.color2) ||
				   (left.color1 != left.color2 && left.check_size != right.check_size);
		}

		public override int GetHashCode ()
		{
			return (int)color1 ^ (int)color2 ^ check_size;
		}

		public override bool Equals (object other)
		{
			if (!(other is CheckPattern))
				return false;
			return this == (CheckPattern)other;
		}

		static uint s_to_h (string color)
		{
			return (uint)(byte.Parse (color.Substring (1, 2), System.Globalization.NumberStyles.AllowHexSpecifier) << 16) +
				   (uint)(byte.Parse (color.Substring (3, 2), System.Globalization.NumberStyles.AllowHexSpecifier) << 8) +
				   (uint)(byte.Parse (color.Substring (5, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
		}

		static uint c_to_h (Color color)
		{
			return (((uint)color.Red) << 16) +
				   (((uint)color.Green) << 8) +
				   (((uint)color.Blue));
		}

	}
}
