using System;
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
			get { return Marshal.PtrToStringAnsi (resource) }
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
			Marshal.Copy (data, 0,  buffer, Math.Min (data.Length, length);
		}
	}

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
		

		internal delegate void AuthorizeCallback (IntPtr resource, IntPtr username, IntPtr password);

		[DllImport("libsane.so.1")]
		Status sane_init (out version, AuthorizeCallback func);
		
		private Init ()
		{
			Status status = sane_init (out version, new AuthorizeCallback (Authorize));
		}
		
		private void Authorize (IntPtr resource, IntPtr username, IntPtr password)
		{
			AuthorizeEventArgs = new AuthorizeEventArgs (resource, username, password);
		}

		[DllImport("libsane.so.1")]
		Status sane_get_devices (IntPtr list_pointer, bool local_only);

		QueryDevices (bool local_only)
		{
			
		}
	}
		
	public class Scan {
			
	}

	public class Device {
		HandleRef handle;
		bool blocking = true;
		
		public HandleRef Handle {
			get { return handle; }
		}

		[DllImport("libsane.so.1")]
		internal int FileDescriptor {
			get {
				int fd;
				
				Sane
				

		[DllImport("libsane.so.1")]
		Status sane_set_io_mode (HandleRef handle, bool blocking);

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

		internal Device (string name)
		{
			Open (name)
		}

		[DllImport("libsane.so.1")]
		Status same_open (string name, out IntPtr handle)

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
		Status sane_start (HandleRef handle);

		private Start ()
		{
			Status status;

			status = sane_start (handle);

			if (Status != Status.Good)
				throw new ScanException (status);
		}

		[DllImport ("libsane.so.1")]
		void sane_cancel (HandleRef handle);

		private Cancel ()
		{
			sane_cancel (handle);
		}

	}
}
