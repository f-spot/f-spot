using System.Runtime.InteropServices;

namespace FSpot {
	class Unix {

		[DllImport ("libc")]
		static extern int rename (string oldpath, string newpath);
		
		public static int Rename (string oldpath, string newpath)
		{
			return rename (oldpath, newpath);
		}

		[DllImport ("libc")]
		static extern int mkstemp (byte []template);

		public static Mono.Unix.UnixStream MakeSafeTemp (ref string template)
		{
			byte [] template_bytes = System.Text.Encoding.UTF8.GetBytes (template + ".XXXXXX\0");

			int fd = mkstemp (template_bytes);

			if (fd < 0) {
				//Mono.Unix.Error error = Mono.Unix.Stdlib.GetLastError ();
				throw new System.ApplicationException (Mono.Unix.Catalog.GetString ("Unable to create temporary file"));
			}

			template = System.Text.Encoding.UTF8.GetString (template_bytes, 0, template_bytes.Length - 1);
			return new Mono.Unix.UnixStream (fd);
		}
	}
}
