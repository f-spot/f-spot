//
// FSpot.Widgets.CheckPattern.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details.
//

namespace FSpot.Widgets
{
	public struct CheckPattern
	{
		uint color1, color2;
		int check_size;

		public CheckPattern (uint color1, uint color2, int check_size)
		{
			this.color1 = color1;
			this.color2 = color2;
			this.check_size = check_size;
		}

		public uint Color1
		{
			get { return color1; }
		}

		public uint Color2
		{
			get { return color2; }
		}

		public int CheckSize
		{
			get { return check_size; }
		}

		public static CheckPattern Dark = new CheckPattern (0x00000000, 0x00555555, 8);
		public static CheckPattern Midtone = new CheckPattern (0x00555555, 0x00aaaaaa, 8);
		public static CheckPattern Light = new CheckPattern (0x00aaaaaa, 0x00ffffff, 8);
		public static CheckPattern Black = new CheckPattern (0x00000000, 0x00000000, 8);
		public static CheckPattern Gray = new CheckPattern (0x00808080, 0x00808080, 8);
		public static CheckPattern White = new CheckPattern (0x00ffffff, 0x00ffffff, 8);
	}
}
