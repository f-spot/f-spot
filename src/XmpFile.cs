using SemWeb;

namespace FSpot.Xmp {
	public class XmpFile 
	{
		SemWeb.MemoryStore store = new SemWeb.MemoryStore ();

		public XmpFile (System.IO.Stream stream)
		{
			Load (stream);
		}

		public void Load (System.IO.Stream stream)
		{
			store.Import (new SemWeb.RdfXmlReader (stream));
			Dump ();
		}
		
		public void Dump ()
		{
			foreach (SemWeb.Statement stmt in store) {
				System.Console.WriteLine(stmt);
			}
		}

		/*
		public void Build ()
		{
			MemoryStore query = store.Select (new Statement (null, (Entity)"http://
		}
		*/
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
