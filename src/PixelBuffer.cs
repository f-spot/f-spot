namespace FSpot.Imaging {
	public enum PixelBufferDepth {
		UInt8 = 8,
		UInt16 = 16
	}

	public class UInt16Buffer : PixelBuffer {
		protected ushort [] data;

		public UInt16Buffer (int width, int height)
		{
			this.width = width;
			this.height = height;
			depth = PixelBufferDepth.UInt16;

			row_stride = width;

			data = new ushort [width * height];
		}

		public UInt16Buffer (Gdk.Pixbuf pixbuf, Cms.Profile profile)
		{
			for (int r = 0; r < height; r++) {
				

			}
		}

		public ToPixbuf (Cms.Profile destination_profile)
		{
			Cms.Profile [] list = new Cms.Profile { profile, destination_profile };
			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, false, 8, 
							    orig.Width, orig.Height);
			Cms.Transform t = new Cms.Transform (list,
							     
			
		}

		public override void Fill (int x, int y, ushort [] data, int offset, int length)
		{
			
		}

		public override void Fill (int x, int y, byte [] data, int offset, int length)
		{
			
		}
	}

#if false
	public class UInt8Buffer : PixelBuffer {
		protected ushort [] data;
		
		
	}

	public class PixbufBuffer : PixelBuffer {
		protected Gdk.Pixbuf pixbuf;

		public PixbufBuffer (Gdk.Pixbuf pixbuf)
		{

		}
	}
#endif

	public abstract class PixelBuffer {
		protected int row_stride;
		protected int width;
		protected int height;
		protected PixelBufferDepth depth;
		protected Cms.Profile profile;

		new PixelBuffer (int width, int height, PixelBufferDepth depth)
		{
			if 

		}

		void Fill (int row, int col, ushort [] value)
		{
			Fill (row, col, value, 0, value.Length);
		}

		void abstract Fill (int x, int y, ushort [] value, int start_offset, int len)
		{
			
		}

		void Fill (int row, int col, byte [] value)
		{
			Fill (row, col, value, 0, value.Length);
		}

		void abstract Fill (int row, int col, byte [] value, int start_offset, int len)
		{
			
		}
	}
}
