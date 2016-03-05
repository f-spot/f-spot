//
// IOChannel.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
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

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FSpot.Imaging
{
	[Flags]
	public enum IOFlags
	{
		Append = 1,
		Nonblock = 1 << 1,
		Readable = 1 << 2,
		Writable = 1 << 3,
		Seekable = 1 << 4
	}

	public enum IOStatus
	{
		Error,
		Normal,
		Eof,
		Again
	}

	public enum IOCondition
	{
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

	public class DataReadEventArgs : EventArgs
	{
		public bool Continue;

		public IOCondition Condition { get; private set; }

		public DataReadEventArgs (IOCondition condition)
		{
			Condition = condition;
			Continue = true;
		}
	}

	public class IOChannel : Stream
	{
		HandleRef handle;

		private delegate bool IOFunc (IntPtr sourceChannel, IOCondition cond, IntPtr data);

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
				throw new GLib.GException (error);
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern IOStatus g_io_channel_flush (HandleRef channel, out IntPtr error);

		public override void Flush ()
		{
			IOStatus status;
			IntPtr error;

			status = g_io_channel_flush (handle, out error);

			if (status != IOStatus.Normal && status != IOStatus.Eof)
				Hyena.Log.DebugFormat ("IOChannel status = {0}", status);

			if (error != IntPtr.Zero)
				throw new GLib.GException (error);
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern unsafe IOStatus g_io_channel_write_chars (HandleRef channel, byte *data, int count, out int bytesWritten, out IntPtr error);

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
						throw new GLib.GException (error);

					offset += written;
					count -= written;
				}
			}
		}

		[DllImport("libglib-2.0-0.dll")]
		static unsafe extern IOStatus g_io_channel_read_chars (HandleRef channel, byte *data, int count, out int bytesRead, out IntPtr error);

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
				Hyena.Log.DebugFormat ("IOChannel status = {0}", status);

			if (error != IntPtr.Zero)
				throw new GLib.GException (error);

			return read;
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern uint g_io_add_watch (HandleRef handle, IOCondition cond, IOFunc func, IntPtr data);

		uint AddWatch (IOCondition ioCondition, IOFunc ioFunc)
		{
			return g_io_add_watch (handle, ioCondition, ioFunc, IntPtr.Zero);
		}

		// FIXME this should hold more than one source in a table
		// but I am lazy
		uint data_ready_source;
		EventHandler<DataReadEventArgs> data_ready;
		IOFunc func;
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

		bool DataReadyHandler (IntPtr channel, IOCondition condition, IntPtr data)
		{
			var args = new DataReadEventArgs (condition);
			if (data_ready != null)
				data_ready (this, args);

			return args.Continue;
		}

		public override void SetLength (long value)
		{
			throw new NotSupportedException ();
		}

		enum SeekType {
			Current,
			Set,
			End
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotSupportedException ();
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
				throw new GLib.GException (error);

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
