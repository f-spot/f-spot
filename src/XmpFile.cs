using SemWeb;

namespace FSpot.Xmp {
	public class XmpFile : SemWeb.StatementSource
	{
		SemWeb.MemoryStore store;

		public XmpFile (System.IO.Stream stream)
		{
			store = new SemWeb.MemoryStore ();
			Load (stream);
		}

		public void Load (System.IO.Stream stream)
		{
			store.Import (new SemWeb.RdfXmlReader (stream));
			Dump ();
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
			new XmpFile (System.IO.File.OpenRead (args [0]));
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
