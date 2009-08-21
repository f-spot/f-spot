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
		// FIXME these are system dependent and I'm hardcoding them because I don't
		// want to write glue today.  If you are debugging image loading and get
		// to this point and find that that your problem is my fault, well...  we all
		// have bad days.
		In = 1,
		Out = 4,
		Priority = 2,
		Error = 8,
		HungUp = 16,
		Invalid = 32
	}

	public class DataReadEventArgs : EventArgs {
		public bool Continue;
		IOCondition condition;
		
		public IOCondition Condition {
			get { return condition; }
		}

		public DataReadEventArgs (IOCondition condition)
		{
			this.condition = condition;
			Continue = true;
		}
	}
	
	public class IOChannel : System.IO.Stream {
		private HandleRef handle;
		
		private delegate bool IOFunc (IntPtr source_channel, IOCondition cond, IntPtr data);

		[DllImport("libglib-2.0-0.dll")]
		static extern IOFlags g_io_channel_get_flags (HandleRef channel);

		public override bool CanRead {
			get { 
				IOFlags flags = g_io_channel_get_flags (handle);

				return (flags & IOFlags.Readable) == IOFlags.Readable; 
			}
		}

		public override bool CanSeek {
			get {
#if NOTDONE				
				IOFlags flags = g_io_channel_get_flags (handle);

				return (flags & IOFlags.Seekable) == IOFlags.Seekable; 
#else
				return false;
#endif
			}
		}

		public override bool CanWrite {
			get {
				IOFlags flags = g_io_channel_get_flags (handle);

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
				throw new NotSupportedException ("IOChannel doesn't support seeking");
			}
			set {
				throw new NotSupportedException ("IOChannel doesn't support seeking");
			}
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern IntPtr g_io_channel_unix_new (int fd);

		[DllImport("libglib-2.0-0.dll")]
		static extern IOStatus g_io_channel_set_encoding (HandleRef handle, string encoding, out IntPtr error);

		public IOChannel (int fd)
		{
			IntPtr raw = g_io_channel_unix_new (fd);

			handle = new HandleRef (this, raw);

			IntPtr error;
			g_io_channel_set_encoding (handle, null, out error);
			if (error != IntPtr.Zero)
				throw new GException (error);
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern IOStatus g_io_channel_flush (HandleRef channel, out IntPtr error);

		public override void Flush ()
		{
			IOStatus status;
			IntPtr error;
			
			status = g_io_channel_flush (handle, out error);

			if (status != IOStatus.Normal && status != IOStatus.Eof)
				System.Console.WriteLine ("IOChannel status = {0}", status);

			if (error != IntPtr.Zero)
				throw new GException (error);
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern unsafe IOStatus g_io_channel_write_chars (HandleRef channel, byte *data, int count, out int bytes_written, out IntPtr error);
		
		public override void Write (byte [] buffer, int offset, int count)
		{
			IOStatus status = IOStatus.Again;
			IntPtr error;
			int written;

			if (buffer == null)
				throw new ArgumentNullException ();
			
			unsafe {
				while (status == IOStatus.Again && count > 0) {
					fixed (byte *data = &buffer [offset]) {
						status = g_io_channel_write_chars (handle, data, count, out written, out error);
					}

					if (error != IntPtr.Zero)
						throw new GException (error);
					
					offset += written;
					count -= written;
				}
			}
		}
		
		[DllImport("libglib-2.0-0.dll")]
		static unsafe extern IOStatus g_io_channel_read_chars (HandleRef channel, byte *data, int count, out int bytes_read, out IntPtr error);

		public override int Read (byte [] buffer, int offset, int count)
		{
			int read;
			IOStatus status;
			IntPtr error;

			unsafe {
				fixed (byte *data = &buffer[offset]) {
					status = g_io_channel_read_chars (handle, data, count, out read, out error);
				}
			}

			if (status != IOStatus.Normal && status != IOStatus.Eof)
				System.Console.WriteLine ("IOChannel status = {0}", status);

			if (error != IntPtr.Zero)
				throw new GException (error);

			return (int)read;
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern uint g_io_add_watch (HandleRef handle, IOCondition cond, IOFunc func, IntPtr data);

		private uint AddWatch (IOCondition condition, IOFunc func)
		{
			return g_io_add_watch (handle, condition, func, IntPtr.Zero);
		}
		
		// FIXME this should hold more than one source in a table
		// but I am lazy
		uint data_ready_source;
		private EventHandler<DataReadEventArgs> data_ready;
		private IOFunc func;
		public event EventHandler<DataReadEventArgs> DataReady {
			add {
				data_ready += value;
				func = new IOFunc (DataReadyHandler);
				data_ready_source = AddWatch (IOCondition.In, func);
			}
			remove {
				GLib.Source.Remove (data_ready_source);
				data_ready_source = 0;
				data_ready -= value;
			}
		}

		private bool DataReadyHandler (IntPtr channel, IOCondition condition, IntPtr data)
		{
			DataReadEventArgs args = new DataReadEventArgs (condition);
			if (data_ready != null) 
				data_ready (this, args);
			
			return args.Continue;
		}

		public override void SetLength (long length)
		{
			throw new NotSupportedException ();
		}
		
		private enum SeekType {
			Current,
			Set,
			End
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern IOStatus g_io_channel_seek_position (HandleRef handle, long offset, SeekType type, out IntPtr error);

		public override long Seek (long position, SeekOrigin origin)
		{
#if false			
			// GIOChannels have the interesting property of having a seek interface
			// but no method to retrieve the current position or length.
			// we could support these actions for unix iochannels with extra work
			// but for now we'll just disable them.

			SeekType type;
			IntPtr error;
			long final;
			
			switch (origin) {
			case SeekOrigin.Begin:
				type = SeekType.Set;
				break;
			case SeekOrigin.Current:
				
				break;
			}

			g_io_channel_seek_position (handle, position, type, out error);

			if (error != IntPtr.Zero)
				throw new GException (error);
			
			if (SeekOrigin == SeekOrigin.Begin)
				return position;
			else
#else
				throw new NotSupportedException ();
#endif
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern IOStatus g_io_channel_shutdown (HandleRef handle, bool flush, out IntPtr error);

		[DllImport("libglib-2.0-0.dll")]
		static extern void g_io_channel_unref (HandleRef handle);

		public override void Close ()
		{
			IntPtr error;

			if (data_ready_source != 0)
				GLib.Source.Remove (data_ready_source);
			data_ready_source = 0;

			g_io_channel_shutdown (handle, false, out error);
			
			base.Close ();

			if (error != IntPtr.Zero)
				throw new GException (error);

		}

		~IOChannel ()
		{
			if (data_ready_source != 0)
				GLib.Source.Remove (data_ready_source);
			data_ready_source = 0;
			g_io_channel_unref (handle);
		}
	}
}
