using System;
using System.IO;
using System.Runtime.InteropServices;
using GLib;

namespace FSpot {

	[Flags]
	public enum IOFlags {
		Append = 1,
		Nonblock = 1 << 1,
		Readable = 1 << 2,
		Writable = 1 << 3,
		Seekable = 1 << 4
	}

	public enum IOStatus {
		Error,
		Normal,
		Eof,
		Again
	}

	public enum IOCondition {
		// FIXME this is not a real condition
		Unknown
	}

	public class IOChannel : System.IO.Stream {
		private HandleRef handle;
		
		private delegate void IOFunc (HandleRef source_channel, IOCondition cond, IntPtr data);

		[DllImport("libglib-2.0-0.dll")]
		static extern IOFlags g_io_channel_get_flags ();

		public override bool CanRead {
			get { 
				IOFlags flags = g_io_channel_get_flags ();

				return (flags & IOFlags.Readable) == IOFlags.Readable; 
			}
		}

		public override bool CanSeek {
			get {
#if NOTDONE				
				IOFlags flags = g_io_channel_get_flags ();

				return (flags & IOFlags.Seekable) == IOFlags.Seekable; 
#else
				return false;
#endif
			}
		}

		public override bool CanWrite {
			get {
				IOFlags flags = g_io_channel_get_flags ();

				return (flags & IOFlags.Writable) == IOFlags.Writable; 
			}
		}

		public override long Length {
			get { 
				throw new NotSupportedException ("IOChannel doesn't support seeking");
			}
		}
		
		public override long Position {
			get {
				throw new NotSupportedException ("IChannle doesn't support seeking");
			}
			set {
				throw new NotSupportedException ("IChannle doesn't support seeking");
			}
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern IntPtr g_io_channel_unix_new (int fd);

		public IOChannel (int fd)
		{
			IntPtr raw = g_io_channel_unix_new (fd);

			handle = new HandleRef (this, raw);
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern IOStatus g_io_channel_flush (HandleRef channel, out IntPtr error);

		public override void Flush ()
		{
			IOStatus status;
			IntPtr error;
			
			status = g_io_channel_flush (handle, out error);

			if (error != IntPtr.Zero)
				throw new GException (error);
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern unsafe IOStatus g_io_channel_write_chars (HandleRef channel, byte *data, long count, out long bytes_written, out IntPtr error);
		
		public override void Write (byte [] buffer, int offset, int count)
		{
			IOStatus status = IOStatus.Again;
			IntPtr error;
			long real_offset = offset;
			long written;

			if (buffer == null)
				throw new ArgumentNullException ();
			
			unsafe {
				while (status == IOStatus.Again && real_offset < offset + count) {
					fixed (byte *data = &buffer [real_offset]) {
						status = g_io_channel_write_chars (handle, data, count, out written, out error);
					}

					if (error != IntPtr.Zero)
						throw new GException (error);
					
					real_offset += written;
					count -= (int) written;
				}
			}
		}
		
		[DllImport("libglib-2.0-0.dll")]
		static unsafe extern IOStatus g_io_channel_read_chars (HandleRef channel, byte *data, long count, out long bytes_read, out IntPtr error);

		public override int Read (byte [] buffer, int offset, int count)
		{
			long read;
			IOStatus status;
			IntPtr error;

			unsafe {
				fixed (byte *data = &buffer[offset]) {
					status = g_io_channel_read_chars (handle, data, (long)count, out read, out error);
				}
				
				if (error != IntPtr.Zero)
					throw new GException (error);
			}
			
			return (int)read;
		}

		public override void SetLength (long length)
		{
			throw new NotSupportedException ();
		}
		
		public override long Seek (long position, SeekOrigin origin)
		{
			//FIXME this should be supported
			throw new NotSupportedException ();
		}
	}
}
