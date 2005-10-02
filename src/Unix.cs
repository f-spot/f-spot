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
		static extern int strerror_r (Mono.Unix.Error err, System.Text.StringBuilder sb, int count);

		public static string ErrorString (Mono.Unix.Error errno)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder (256);
			strerror_r (errno, sb, sb.Capacity);

			return sb.ToString ();
		}
		

		[DllImport ("libc")]
		static extern int mkstemp (byte []template);

		public static Mono.Unix.UnixStream MakeSafeTemp (ref string template)
		{
			byte [] template_bytes = System.Text.Encoding.UTF8.GetBytes (template + ".XXXXXX\0");

			int fd = mkstemp (template_bytes);

			if (fd < 0) {
				//Mono.Unix.Error error = Mono.Unix.Stdlib.GetLastError ();
				throw new System.ApplicationException (Mono.Posix.Catalog.GetString ("Unable to create temporary file"));
			}

			template = System.Text.Encoding.UTF8.GetString (template_bytes, 0, template_bytes.Length - 1);
			return new Mono.Unix.UnixStream (fd);
		}
	}
}
