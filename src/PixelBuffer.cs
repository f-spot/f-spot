using System;

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
			this.nchannels = 3;

			depth = PixelBufferDepth.UInt16;

			row_stride = width * 3;

			data = new ushort [width * height * nchannels];
		}

		public override void Fill8 (int i, int j, byte [] src_data, int offset, int count)
		{

		}

		public override void Fill16 (int i, int j, byte [] src_data, int offset, int count, bool little)
		{
			if (System.BitConverter.IsLittleEndian == little)
				Fill16 (i, j, src_data, offset, count);
			else
				Fill16Swap (i, j, src_data, offset, count);
		}

		public void Fill16 (int i, int j, byte [] src_data, int offset, int count)
		{ 
			unsafe {
				ushort * rowpix;

				fixed (ushort * pixels = &data [0]) {
					fixed (ushort * src_pixels = &src_data [offset]) {
						rowpix = pixels + i * row_stride + j;

						for (int col = 0; col < count; col ++)
							rowpix [col] = src_pixels [col];
					}
				}
			}
		}

		public void Fill16Swap (int i, int j, byte [] src_data, int offset, int count)
		{
			unsafe {
				ushort * rowpix;

				fixed (ushort * pixels = &data [0]) {
					fixed (ushort * src_pixels = &src_data [offset]) {
						rowpix = pixels + i * row_stride + j;

						for (int col = 0; col < count; col ++)
							rowpix [col] = BitConverter.Swap (src_pixels [col]);
					}
				}
			}
		}
		
		public override Gdk.Pixbuf ToPixbuf (Cms.Profile destination_profile)
		{
#if true //USE_LCMS
			profile = Cms.Profile.CreateSRgb ();
			Cms.Profile [] list = new Cms.Profile [] { profile, destination_profile };
			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, false, 8, 
							    width, height);
			
			Cms.Transform t = new Cms.Transform (list,
							     Cms.Format.Rgb16,
							     PixbufUtils.PixbufCmsFormat (pixbuf),
							     Cms.Intent.Perceptual, 0x0);
			
			unsafe {
				fixed (ushort * srcpix  = &data[0]) {
					byte * destpix = (byte *) pixbuf.Pixels;
					
					for (int row = 0; row < height; row++)
						t.Apply ((IntPtr) (srcpix + row * row_stride),
							 (IntPtr) (destpix + row * pixbuf.Rowstride),
							 (uint)width);
				}
			}

			return pixbuf;
#else
			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, false, 8, 
							    width, height);

			unsafe {
				fixed (ushort * src  = &data[0]) {
					ushort * srcpix = src;
					byte * destpix = (byte *) pixbuf.Pixels;
					
					for (int row = 0; row < height; row++) {
						for (int col = 0; col < width * nchannels; col++)
							destpix [col] = (byte) (srcpix [col] >> 8);
						
						srcpix += row_stride;
						destpix += pixbuf.Rowstride;
					}
				}
			}

			return pixbuf;
#endif
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
		protected int nchannels;
		protected PixelBufferDepth depth;
		protected Cms.Profile profile;

		protected PixelBuffer ()
		{

		}

		public int Width {
			get {
				return width;
			}
		}

		public int Height {
			get {
				return height;
			}
		}
					   
		public abstract void Fill8 (int row, int col, byte [] value, int start_offset, int len);

		public abstract void Fill16 (int row, int col, byte [] data, int offset, int len, bool little);

		public abstract Gdk.Pixbuf ToPixbuf (Cms.Profile profile);
	}
}
