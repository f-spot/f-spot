using SemWeb;

namespace FSpot.Xmp {
	public class XmpFile : SemWeb.StatementSource
	{
		MetadataStore store;

		public XmpFile (System.IO.Stream stream)
		{
			store = new MetadataStore ();
			Load (stream);
		}

		public void Load (System.IO.Stream stream)
		{
			try {
				store.Import (new SemWeb.RdfXmlReader (stream));
				//Dump ();
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			}
		}
		
		public void Select (SemWeb.StatementSink sink)
		{
			store.Select (sink);
		}
		
		public void Dump ()
		{
			foreach (SemWeb.Statement stmt in store) {
				System.Console.WriteLine(stmt);
			}
		}

#if TEST_XMP
		static void Main (string [] args)
		{
			XmpFile xmp = new XmpFile (System.IO.File.OpenRead (args [0]));
			//xmp.Store.Dump ();
#if false
			System.IO.StreamReader stream = new System.IO.StreamReader (System.IO.File.OpenRead (args [0]));

			while (stream.BaseStream.Position < stream.BaseStream.Length) {
				System.Console.WriteLine (stream.ReadLine ());
			}
#endif
		}
#endif
	}
}
