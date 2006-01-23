using SemWeb;
using SemWeb.Util;
using Mono.Posix;

namespace FSpot {
        internal class Description {
		string predicate;
		string description;
		string title;
		ValueFormat formater;
		
		static System.Collections.Hashtable table;

		static Description ()
		{
			Description [] preset = new Description [] {
				new Description ("dc:creator", Catalog.GetString ("Creator")),
				new Description ("dc:title", Catalog.GetString ("Title")),
				new Description ("dc:rights", Catalog.GetString ("Copyright")),
				new Description ("dc:subject", Catalog.GetString ("Subject and Keywords")),
				new Description ("tiff:Compression", Catalog.GetString ("Compression"), 
						 typeof (FSpot.Tiff.Compression)),
				new Description ("tiff:PlanarConfiguration", Catalog.GetString ("Planar Configuration"), 
						 typeof (FSpot.Tiff.PlanarConfiguration)),
				new Description ("tiff:Orientation", Catalog.GetString ("Orientation"), 
						 typeof (PixbufOrientation)),
				new Description ("tiff:PhotometricInterpretation", Catalog.GetString ("Photometric Interpretation"), 
						 typeof (FSpot.Tiff.PhotometricInterpretation)),
				new Description ("tiff:ResolutionUnit", Catalog.GetString ("Resolution Unit"),
						 typeof (FSpot.Tiff.ResolutionUnit)),
				new Description ("exif:ExposureProgram", Catalog.GetString ("Exposure Program"), 
						 typeof (FSpot.Tiff.ExposureProgram)),
				new Description ("exif:MeteringMode", Catalog.GetString ("Metering Mode"), 
						 typeof (FSpot.Tiff.MeteringMode)),
				new Description ("exif:ExposureMode", Catalog.GetString ("Exposure Mode"), 
						 typeof (FSpot.Tiff.ExposureMode)),
				new Description ("exif:CustomRendered", Catalog.GetString ("Custom Rendered"), 
						 typeof (FSpot.Tiff.CustomRendered)),
				new Description ("exif:ComponentsConfiguration", Catalog.GetString ("Components Configuration"),
						 typeof (FSpot.Tiff.ComponentsConfiguration)),
				new Description ("exif:LightSource", Catalog.GetString ("Light Source"),
						 typeof (FSpot.Tiff.LightSource)),
				new Description ("exif:SensingMethod", Catalog.GetString ("Sensing Method"),
						 typeof (FSpot.Tiff.SensingMethod)),
				new Description ("exif:ColorSpace", Catalog.GetString ("Color Space"),
						 typeof (FSpot.Tiff.ColorSpace)),
				new Description ("exif:WhiteBalance", Catalog.GetString ("White Balance"),
						 typeof (FSpot.Tiff.WhiteBalance)),
				new Description ("exif:FocalPlaneResolutionUnit", Catalog.GetString ("Focal Plane Resolution Unit"),
						 typeof (FSpot.Tiff.ResolutionUnit)),
				new Description ("exif:FileSource", Catalog.GetString ("File Source Type"),
						 typeof (FSpot.Tiff.FileSource)),
				new Description ("exif:SceneCaptureType", Catalog.GetString ("Scene Capture Type"),
						 typeof (FSpot.Tiff.SceneCaptureType)),
				new Description ("exif:GainControl", Catalog.GetString ("Gain Control"),
						 typeof (FSpot.Tiff.GainControl)),
				new Description ("exif:Contrast", Catalog.GetString ("Contrast"),
						 typeof (FSpot.Tiff.Contrast)),
				new Description ("exif:Saturation", Catalog.GetString ("Saturation"),
						 typeof (FSpot.Tiff.Saturation)),
				new Description ("exif:Sharpness", Catalog.GetString ("Sharpness"),
						 typeof (FSpot.Tiff.Sharpness)),
				new Description ("exif:SceneType", Catalog.GetString ("Scene Type"),
						 typeof (FSpot.Tiff.SceneType))



			};
			
			table = new System.Collections.Hashtable ();

			foreach (Description d in preset) {
				table [MetadataStore.Namespaces.Resolve (d.predicate)] = d;
			}
		}
		
		public Description (string predicate, string title) : this (predicate, title, null, null) {}

		public Description (string predicate, string title, string description) : this (predicate, title, description, null) {}
		
		public Description (string predicate, string title, System.Type type) : this (predicate, title)
		{
			formater = new ValueFormat (type);
		}

		public Description (string predicate, string title, string description, ValueFormat formater)
		{
			this.predicate = predicate;
			this.description = description;
			this.title = title;
			this.formater = formater;
		}
		
		public static void GetDescription (MemoryStore store, Statement stmt, out string label, out string value)
		{
			string predicate = stmt.Predicate.ToString ();

			Description d = (Description) table [predicate];

			label = System.IO.Path.GetFileName (predicate);
			value = null;
			if (stmt.Object is Literal)
			        value = ((Literal)(stmt.Object)).Value;

			if (d != null) {
				label = d.title;
				if (d.formater != null && stmt.Object is Literal)
					value = d.formater.GetValue (store, (Literal)stmt.Object);

			} else {
				Statement sstmt = new Statement (stmt.Predicate,
								 (Entity)MetadataStore.Namespaces.Resolve ("rdfs:label"),
								 null);
				
				foreach (Statement tstmt in MetadataStore.Descriptions.Select (sstmt))
					if (tstmt.Object is Literal)
						label = ((Literal)(tstmt.Object)).Value;
			}
			return;
		}
	}
	
        internal class ValueFormat 
	{
		System.Type type;
		
		public ValueFormat (System.Type type)
		{
			this.type = type;
		}

		public virtual string GetValue (MemoryStore store, Literal obj)
		{
			string result = obj.Value;

			if (type.IsEnum) {
				try {
					object o = System.Enum.Parse (type, obj.Value);
					result = o.ToString ();
				} catch (System.Exception e) {
					System.Console.WriteLine ("Value \"{2}\" not found in {0}\n{1}", type, e, result);
				}
			}
			/*
			else if (type == typeof (Rational)) {
				object o = FSpot.Tiff.Rational.Parse (obj.Value);
			} 
			*/
			return result;
		}
	}

	public class MetadataStore : MemoryStore
	{
		public static NamespaceManager Namespaces;
		private static MetadataStore descriptions;

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
			Namespaces.AddNamespace ("http://www.w3.org/1999/02/22-rdf-syntax-ns#", "rdf");
			Namespaces.AddNamespace ("http://www.w3.org/2000/01/rdf-schema#", "rdfs");
		}

		public static MetadataStore Descriptions {
			get {
				if (descriptions == null) {
					descriptions = new MetadataStore ();
					System.IO.Stream stream = System.Reflection.Assembly.GetCallingAssembly ().GetManifestResourceStream ("dces.rdf");
					if (stream != null) {
						descriptions.Import (new RdfXmlReader (stream));
					} else {
						System.Console.WriteLine ("Can't find resource");
					}
				}
				
				return descriptions;
			}
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
			/*
			foreach (string nspace in Namespaces.GetNamespaces ()) {
				this.Select (new StatementWriter (nspace));
			}
			*/
		}

		public static void AddLiteral (StatementSink sink, string predicate, string type, Literal value)
		{
			Entity empty = new Entity (null);
			Statement top = new Statement ("", (Entity)MetadataStore.Namespaces.Resolve (predicate), empty);
			Statement desc = new Statement (empty, 
							(Entity)MetadataStore.Namespaces.Resolve ("rdf:type"), 
							(Entity)MetadataStore.Namespaces.Resolve (type));
			sink.Add (desc);
			Statement literal = new Statement (empty,
							   (Entity)MetadataStore.Namespaces.Resolve ("rdf:li"),
							   value);
			sink.Add (literal);
			sink.Add (top);
		}

		public static void AddLiteral (StatementSink sink, string predicate, string value)
		{
			Statement stmt = new Statement ((Entity)"", 
							(Entity)MetadataStore.Namespaces.Resolve (predicate), 
							new Literal (value));
			sink.Add (stmt);
		}

		public static void Add (StatementSink sink, string predicate, string type, string [] values)
		{
			Add (sink, new Entity (""), predicate, type, values);
		}

		public void Update (string predicate, string type, string [] values)
		{
			Entity anon = null;

			foreach (Statement stmt in this) {
				if (stmt.Predicate == MetadataStore.Namespaces.Resolve (predicate)) {
					anon = (Entity) stmt.Object;
					break;
				}
			}

			if (anon == null) {
				System.Console.WriteLine ("Did not find subject");
				Add (this, predicate, type, values);
				return;
			}

			System.Collections.Hashtable list = new System.Collections.Hashtable ();
			System.Collections.ArrayList to_remove = new System.Collections.ArrayList ();

			foreach (string name in values)
				list [name] = name;

			foreach (Statement stmt in this) {
				if (stmt.Subject == anon) {
					if (stmt.Object is Literal) {
						string literal = ((Literal)stmt.Object).Value;
						if (list.Contains (literal))
							list.Remove (literal);
						else
							to_remove.Add (stmt);
					}

				}
			}

			foreach (Statement stmt in to_remove)
				this.Remove (stmt);
			
			foreach (string name in list.Keys) {
				Statement stmt  = new Statement (anon, (Entity)MetadataStore.Namespaces.Resolve ("rdf:li"),
								 new Literal (name, null, null));
				this.Add (stmt);
			}
		}
		
		public static void Add (StatementSink sink, Entity subject, string predicate, string type, string [] values)
		{
			if (values == null) {
				System.Console.WriteLine ("{0} has no values; skipping", predicate);
				return;
			}

			Entity empty = new Entity (null);
			Statement top = new Statement (subject, (Entity)MetadataStore.Namespaces.Resolve (predicate), empty);
			Statement desc = new Statement (empty, 
							(Entity)MetadataStore.Namespaces.Resolve ("rdf:type"), 
							(Entity)MetadataStore.Namespaces.Resolve (type));
			sink.Add (desc);
			foreach (string value in values) {
				Statement literal = new Statement (empty,
								   (Entity)MetadataStore.Namespaces.Resolve ("rdf:li"),
								   new Literal (value, null, null));
				sink.Add (literal);
			}
			sink.Add (top);
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

		private class SelectFirst : StatementSink
		{
			public Statement Statement;

			public bool Add (Statement stmt)
			{
				this.Statement = stmt;
				return false;
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
