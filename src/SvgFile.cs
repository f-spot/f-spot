using System;

namespace FSpot.Svg {
	public class SvgFile : ImageFile // SemWeb.StatementSource 
	{
		MetadataStore store;

                // false seems a safe default
                public bool Distinct {
                        get { return false; }
                }

		
		public SvgFile (Uri uri) : base (uri)
		{
		}

		public SvgFile (string path) : base (path) 
		{
		}

		public MetadataStore Store {
			get {
				if (store == null) {
					store = new MetadataStore ();
					using (System.IO.Stream input = Open ()) {
						Load (input);
					}
				}
				return store;
			}
		}

		public void Load (System.IO.Stream stream)
		{
			try {
				store.Import (new SemWeb.RdfXmlReader (stream));
				store.Dump ();

			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			}
		}

		public void Select (SemWeb.StatementSink sink)
		{
			Store.Select (sink);
		}
	}
}
