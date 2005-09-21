using SemWeb;
using SemWeb.Util;

namespace FSpot {
	public class MetadataStore : MemoryStore
	{
		public static NamespaceManager Namespaces;
		
		static MetadataStore ()
		{
			Namespaces = new NamespaceManager ();
			
			Namespaces.AddNamespace ("http://ns.adobe.com/photoshop/1.0/", "photoshop");
			Namespaces.AddNamespace ("http://iptc.org/std/Iptc4xmpCore/1.0/xmlns/", "Iptc4xmpCore");
			Namespaces.AddNamespace ("http://purl.org/dc/elements/1.1/", "dc");
			Namespaces.AddNamespace ("http://ns.adobe.com/xap/1.0/", "xmp");
			Namespaces.AddNamespace ("http://ns.adobe.com/xmp/Identifier/qual/1.0", "xmpidq");
			Namespaces.AddNamespace ("http://ns.adobe.com/xap/1.0/rights/", "xmpRights");
			Namespaces.AddNamespace ("http://ns.adobe.com/xap/1.0/bj/", "xmpBJ");
			Namespaces.AddNamespace ("http://ns.adobe.com/xap/1.0/mm/", "xmpMM");
			Namespaces.AddNamespace ("http://ns.adobe.com/exif/1.0/", "exif");
			Namespaces.AddNamespace ("http://ns.adobe.com/tiff/1.0/", "tiff");
		}
		
		public void Dump ()
		{
			foreach (SemWeb.Statement stmt in this) {
				System.Console.WriteLine(stmt);
			}

			/*
			XPathSemWebNavigator navi = new XPathSemWebNavigator (this, Namespaces);
			navi.MoveToRoot ();
			navi.MoveToFirstChild ();
			navi.MoveToFirstChild ();
			DumpNode (navi, 0);
			*/

			/* Use the statement writer to filter the messages */
			foreach (string nspace in Namespaces.GetNamespaces ()) {
				this.Select (new StatementWriter (nspace));
			}
		}

		private class StatementWriter : StatementSink 
		{
			string name;
			public StatementWriter (string name)
			{
				this.name = name;
			}

			public bool Add (Statement stmt)
			{
				string predicate = stmt.Predicate.ToString ();

				if (predicate.StartsWith (name))
					System.Console.WriteLine ("----------- {0}", stmt);

				return true;
			}
		}

		public void DumpNode (XPathSemWebNavigator navi, int depth)
		{
			do { 
				System.Console.WriteLine ("node [{0}] {1} {2}", depth, navi.Name, navi.Value);
			} while (navi.MoveToNext ());
		}
	       
	}	       
}
