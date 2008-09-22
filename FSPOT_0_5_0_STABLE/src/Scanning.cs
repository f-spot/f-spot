using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FSpot.Scanning {
	public enum Status {
		Good,
		Unsupported,
		Canceled,
		DeviceBusy,
		Invalid,
		Eof,
		Jammed,
		NoDocuments,
		CoverOpen,
		InputOuputError,
		OutOfMemory,
		AccessDenied
	}

	public enum Unit {
		None,
		Pixel,
		Bit,
		Millimeter,
		Dpi,
		Percent,
		Microsecond
	}

	public enum Frame {
		Gray,
		Rgb,
		Red,
		Green,
		Blue
	}
	
	public enum OptionType {
		Bool,
		Int,
		String,
		Button,
		Group
	}
	
	public class ScanException : Exception {
		Status status;
		
		public Status Status {
			get { return status; }
		}

		public ScanException (Status status) : base ()
		{
			this.status = status;
		}
	}

	public class ScanAuthorizeEventArgs : System.EventArgs {
		IntPtr resource;
		IntPtr username_buffer;
		IntPtr password_buffer;
		private const BufferLength = 128;

		public string ResourceName {
			get { return Marshal.PtrToStringAnsi (resource); }
		}
		
		internal AuthorizeEventArgs (IntPtr resource, IntPtr username, IntPtr password)
		{
			this.resource = resource;
			this.username_buffer = username;
			this.password_buffer = password;
		}
		
		public SetUsername (string username)
		{
			CopyToBuffer (username, username_buffer);
		}
		
		public SetPassword (string password)
		{
			CopyToBuffer (password, password_buffer);
		}
		
		private CopyToBuffer (string value, IntPtr buffer)
		{
			byte [] data;
			int length;

			data = System.Text.Encoding.UTF8.GetBytes (value + "\0");
			
			length = Math.Min (data.Length, BufferLength);
			data [length - 1] = 0;
			Marshal.Copy (data, 0,  buffer, Math.Min (data.Length, length));
		}
	}
	
	public delegate AuthorizeEvent (object sender, ScanAuthorizeEventArgs args);

	public class DeviceList {
		int version;
		
		private struct NativeDevice {
			IntPtr name;
			IntPtr vendor;
			IntPtr model;
			IntPtr type;
		}

		// The sane authorization function is as follows
		// 
		// #define SANE_MAX_USERNAME_LEN   128
		// #define SANE_MAX_PASSWORD_LEN   128
		//
		// typedef void (*SANE_Authorization_Callback)
		//	 (SANE_String_Const resource,
		//	  SANE_Char username[SANE_MAX_USERNAME_LEN],
		//	  SANE_Char password[SANE_MAX_PASSWORD_LEN]);
		

		private delegate void AuthorizeCallback (IntPtr resource, IntPtr username, IntPtr password);

		[DllImport("libsane.so.1")]
		static extern Status sane_init (out version, AuthorizeCallback func);
		
		private Init ()
		{
			Status status = sane_init (out version, new AuthorizeCallback (HandleAuthorize));
		}
		
		private void HandleAuthorize (IntPtr resource, IntPtr username, IntPtr password)
		{
			AuthorizeEventArgs args = new AuthorizeEventArgs (resource, username, password);
			if (CheckAuthority != null)
				CheckAuthority (this, args);
			else
				thow new ApplicationException ("You must provide credentials");
		}

		public AuthorizeEvent CheckAuthority;

		[DllImport("libsane.so.1")]
		static extern Status sane_get_devices (IntPtr list_pointer, bool local_only);

		QueryDevices (bool local_only)
		{
			
		}
	}

	public class Option {
		private class OptionRange {
			int min;
			int max;
			int quant;
		}

		private class _OptionDescriptor {
			IntPtr name;
			IntPtr title;
			IntPtr desc;
			OptionType ValueType;
			Unit unit;
			int size;
			int capabilities;
			int constraint_type;
			IntPtr contstraint;
			
			public object [] GetConstraint {
				
			}
		}
	}

	public struct Parameters {
		public Frame Frame;
		public bool LastFrame;
		public int Lines;
		public int Depth;
		public int PixelsPerLine;
		public int BytesPerLine;
	} 

	internal class FrameStream : Stream {
		Device device;

		public bool CanRead {
			get { return true; }
		}

		public bool CanSeek {
			get { return false; }
		}

		public bool CanWrite {
			get { return false; }
		}

		public override long Length {
			get {
				throw new NotSupportedException ();
			}
		}

		public override long Position {
			get {
				throw new NotSupportedException ();
			}
			set {
				throw new NotSupportedException ();
			}
		}

		internal FrameStream (Device device) {
			this.device = device;
		}

		public override int Read (byte [] buffer, int offset, int count)
		{
			return device.Read (buffer, offset, count);
		}
	}
	
	public class Device {
		HandleRef handle;
		bool blocking = true;

		private struct PrivateDevice {
			public IntPtr Vendor;
			public IntPtr Model;
			public IntPtr Type;
			
			public PrivateDevice (IntPtr raw) 
			{
				Marshal.PtrToStructure (raw, this);
			}
		}

		public HandleRef Handle {
			get { return handle; }
		}

		private PrivateDevice PrivateDevice {
			get { return new PrivateDevice (handle.Handle); }
		}

		public Vendor {
			get { 
				return Marshall.PtrToStringAnsi (InternalDevice.Vendor);
			}
		}
		
		public Model {
			get { 
				return Marshall.PtrToStringAnsi (InternalDevice.Model);
			}
		}
		
		public Type {
			get { 
				return Marshall.PtrToStringAnsi (InternalDevice.Type);
			}
		}

		[DllImport("libsane.so.1")]
		static extern Status sane_get_select_fd (HandleRef handle, out int fd);

		internal int FileDescriptor {
			get {
				int fd;
				sane_get_select_fd (handle, out in fd);
				return fd;
			}
		}

		[DllImport("libsane.so.1")]
		static extern Status sane_set_io_mode (HandleRef handle, bool blocking);

		public bool Blocking {
			get { return blocking };
			set {
				Status status;

				blocking = value;
				status = sane_set_io_mode (Handel, blocking);
				
				if (status != Status.Good)
					throw new StatusException (status);
			}
		}

		public Device (string name)
		{
			Open (name);
		}
		
		[DllImport("libsane.so.1")]
		static extern Status sane_open (string name, out IntPtr handle);

		private void Open (string name)
		{
			IntPtr native;
			Status status;
			
			status = sane_open (name, out native);

			if (status != Status.Good)
				throw new ScanException (status);

			handle = new HandleRef (this, native);
		}

		[DllImport ("libsane.so.1")]
		static extern Status sane_read (HandleRef handle, byte *buffer, int max_length, out int length);
		
		public override int Read (byte [] buffer, int offset, int count)
		{
			Status status;
			int length;

			unsafe {
				fixed (byte *data = &buffer[offset]) {
					status = sane_read (handle, data, count, out length);
				}
			}
			
			if (status != Status.Good)
				throw new ScanException (status);
			
			return length;
		}
				
		
		[DllImport ("libsane.so.1")]
		static extern Status sane_start (HandleRef handle);

		private void SystemStart ()
		{
			Status status;

			status = sane_start (handle);

			if (Status != Status.Good)
				throw new ScanException (status);
		}

		[DllImport ("libsane.so.1")]
		static extern void sane_cancel (HandleRef handle);

		private Cancel ()
		{
			sane_cancel (handle);
		}

		[DllImport ("libsane.so.1")]
		static extern Status sane_get_parameters (HandleRef handle, out Parameters parameters);
		
		public Parameters GetParameters ()
		{
			Parameters parameters;

			sane_get_parameters (handle, out parameters);
			return parameters;
		}
	}
}
