namespace FSpot {
	public class DCRawFile : ImageFile {
		public DCRawFile (string path) : base (path) {}
			
		public override System.IO.Stream PixbufStream ()
		{
			// FIXME this filename quoting is super lame
			string args = System.String.Format ("-h -w -c \"{0}\"", this.path);

			System.Diagnostics.Process process = new System.Diagnostics.Process ();
			process.StartInfo = new System.Diagnostics.ProcessStartInfo ("dcraw", args);
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.Start ();
			return new System.IO.BufferedStream (process.StandardOutput.BaseStream);
		}
		
		public static Gdk.Pixbuf Load (string path, string args)
		{
			// FIXME this filename quoting is super lame
			args = System.String.Format ("-h -w -c \"{0}\"", path);

			System.Console.WriteLine ("path = {0}, args = \"{1}\"", path, args);
			 
			using (System.Diagnostics.Process process = new System.Diagnostics.Process ()) {
				process.StartInfo = new System.Diagnostics.ProcessStartInfo ("dcraw", args);
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.UseShellExecute = false;
				process.Start ();
				return PixbufUtils.LoadFromStream (new System.IO.BufferedStream (process.StandardOutput.BaseStream));
			}
		}
	}
}
