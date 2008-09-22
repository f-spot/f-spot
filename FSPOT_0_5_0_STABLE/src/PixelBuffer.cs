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

			rowstride = width * nchannels;

			data = new ushort [width * height * nchannels];
		}

		public UInt16Buffer (Gdk.Pixbuf pixbuf, Cms.Profile profile)
		{
			this.profile = profile;
			width = pixbuf.Width;
			height = pixbuf.Height;
			this.nchannels = pixbuf.HasAlpha ? 4 : 3;
			
			depth = PixelBufferDepth.UInt16;

			data = new ushort [width * height * nchannels];
			
			unsafe {
				byte * src_pixels = (byte *) pixbuf.Pixels;
				int src_stride = pixbuf.Rowstride;
				int count = pixbuf.Width * nchannels;

				for (int row = 0; row < height; row++) {
					Fill8 (row, 0, src_pixels, row * src_stride, count);
				}
			}
		}

		public Cms.Format Format {
			get {
				return nchannels > 3 ? Cms.Format.Rgba16 : Cms.Format.Rgb16;
			}
		}

		public unsafe void Fill8 (int i, int j, byte * src_data, int offset, int count)
		{
			ushort * rowpix;
			
			fixed (ushort * pixels = &data [0]) {
				rowpix = pixels + i * rowstride + j;
				
				for (int col = 0; col < count; col++) {
					int val = src_data [col];
					rowpix [col] = (ushort) (val << 8 & val);
				}
			}
		}

		public override void Fill8 (int row, int col, byte [] value, int start_offset, int len)
		{
			unsafe {
				fixed (byte * src_data = &value[0]) {
					Fill8 (row, col, src_data, start_offset, len);
				}
			}
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
					fixed (byte * bytes = &src_data [offset]) {
						ushort * src_pixels= (ushort *) bytes;

						rowpix = pixels + i * rowstride + j;

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
					fixed (byte * bytes = &src_data [offset]) {
						ushort * src_pixels = (ushort *) bytes;
						rowpix = pixels + i * rowstride + j;

						for (int col = 0; col < count; col ++)
							rowpix [col] = BitConverter.Swap (src_pixels [col]);
					}
				}
			}
		}
		
		public override Gdk.Pixbuf ToPixbuf (Cms.Profile destination_profile)
		{
#if true //USE_LCMS
			profile = Cms.Profile.CreateStandardRgb ();
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
						t.Apply ((IntPtr) (srcpix + row * rowstride),
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
						
						srcpix += rowstride;
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
		protected int rowstride;
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
