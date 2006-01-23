using System.Xml;
using SemWeb;


namespace FSpot.Xmp {
	public class XmpFile : SemWeb.StatementSource, SemWeb.StatementSink
	{
		MetadataStore store;

		public MetadataStore Store {
			get { return store; }
		}

		public XmpFile (System.IO.Stream stream) : this ()
		{
			Load (stream);
		}
		
		public XmpFile ()
		{
			store = new MetadataStore ();
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

		public void Save (System.IO.Stream stream)
		{
			try {
				XmlTextWriter text;
				RdfXmlWriter writer;

				text = new XmlTextWriter (stream, System.Text.Encoding.UTF8);
				using (writer = new RdfXmlWriter (text, MetadataStore.Namespaces)) {
					text.WriteProcessingInstruction ("xpacket", "begin=\"\ufeff\" id=\"testing\"");
					text.WriteStartElement ("x:xmpmeta");
					text.WriteAttributeString ("xmlns", "x", null, "adobe:ns:meta/");
					store.Select (writer);

				}
				text.WriteEndElement ();
				text.WriteProcessingInstruction ("xpacket", "end=\"r\"");
				text.Close ();
				
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
			}
		}

		public bool Add (Statement stmt)
		{
			return ((SemWeb.StatementSink)store).Add (stmt);
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
