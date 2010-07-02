using System.Xml;
using System.Collections;
using SemWeb;

using Hyena;

namespace FSpot.Imaging.Xmp {
	public class XmpFile : SemWeb.StatementSource, SemWeb.StatementSink
	{
		MetadataStore store;

                // false seems like a safe default
                public bool Distinct {
                        get { return false; }
                }

		public MetadataStore Store {
			get { return store; }
			set { store = value; }
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
				RdfXmlReader reader = new RdfXmlReader (stream);
				reader.BaseUri = MetadataStore.FSpotXMPBase;
				store.Import (reader);
				//Dump ();
			} catch (System.Exception e) {
				Log.DebugFormat ("Caught an exception :{0}", e.ToString ());
			}
		}

		private class XmpWriter : RdfXmlWriter {
			public XmpWriter (XmlDocument dest) : base (dest)
			{
				BaseUri = MetadataStore.FSpotXMPBase;
			}
			
			public override void Add (Statement stmt) 
			{
				string predicate = stmt.Predicate.Uri;
				string prefix;
				string localname;

				// Fill in the namespaces with nice prefixes
				if (MetadataStore.Namespaces.Normalize (predicate, out prefix, out localname)) {
					if (prefix != null)
						Namespaces.AddNamespace (predicate.Remove (predicate.Length - localname.Length, localname.Length), prefix);
				}
				base.Add (stmt);
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
				Log.Debug(stmt.ToString());
			}
		}
	}
}
