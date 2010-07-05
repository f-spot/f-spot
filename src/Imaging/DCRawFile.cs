using System.Diagnostics;
using System.IO;
using System;
using Hyena;

namespace FSpot.Imaging {
	public class DCRawFile : BaseImageFile {
		const string dcraw_command = "dcraw";

		public DCRawFile (SafeUri uri) : base (uri)
		{
		}

		public override System.IO.Stream PixbufStream ()
		{
			return RawPixbufStream (Uri);
		}

		internal static System.IO.Stream RawPixbufStream (SafeUri location)
		{
			string path = location.LocalPath;
			string [] args = new string [] { dcraw_command, "-h", "-w", "-c", "-t", "0", path };
			
			InternalProcess proc = new InternalProcess (System.IO.Path.GetDirectoryName (path), args);
			proc.StandardInput.Close ();
			return proc.StandardOutput;
		}
	}
}
