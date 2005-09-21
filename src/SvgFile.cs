namespace FSpot.Svg {
	public class SvgFile : ImageFile, SemWeb.StatementSource 
	{
		MetadataStore store;
		
		public SvgFile (string path) : base (path) {}

		public MetadataStore Store {
			get {
				if (store == null) {
					store = new MetadataStore ();
					using (System.IO.Stream input = System.IO.File.OpenRead (this.Path)) {
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
