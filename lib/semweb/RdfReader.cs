using System;
using System.Collections;
using System.IO;
using System.Web;
 
namespace SemWeb {
	public class ParserException : ApplicationException {
		public ParserException (string message) : base (message) {}
		public ParserException (string message, Exception cause) : base (message, cause) {}
	}

	public abstract class RdfReader : StatementSource, IDisposable {
		Entity meta = Statement.DefaultMeta;
		string baseuri = null;
		ArrayList warnings = new ArrayList();
		Hashtable variables = new Hashtable();
		bool reuseentities = false;
		NamespaceManager nsmgr = new NamespaceManager();

		public Entity Meta {
			get {
				return meta;
			}
			set {
				meta = value;
			}
		}
		
		public string BaseUri {
			get {
				return baseuri;
			}
			set {
				baseuri = value;
			}
		}
		
		public bool ReuseEntities {
			get {
				return reuseentities;
			}
			set {
				reuseentities = value;
			}
		}
		
		bool StatementSource.Distinct { get { return false; } }
		
		public NamespaceManager Namespaces { get { return nsmgr; } }
		
		public ICollection Variables { get { return variables.Keys; } }
		
		public IList Warnings { get { return ArrayList.ReadOnly(warnings); } }
		
		protected void AddVariable(Variable variable) {
			variables[variable] = variable;
		}

		public abstract void Select(StatementSink sink);
		
		public virtual void Dispose() {
		}
		
		public static RdfReader Create(string type, string source) {
			switch (type) {
				case "xml":
				case "text/xml":
					return new RdfXmlReader(source);
				case "n3":
				case "text/n3":
					return new N3Reader(source);
				default:
					throw new ArgumentException("Unknown parser type: " + type);
			}
		}
		
		public static RdfReader LoadFromUri(Uri webresource) {
			// TODO: Add Accept header for HTTP resources.
			
			System.Net.WebRequest rq = System.Net.WebRequest.Create(webresource);
			System.Net.WebResponse resp = rq.GetResponse();
			
			string mimetype = resp.ContentType;
			if (mimetype.IndexOf(';') > -1)
				mimetype = mimetype.Substring(0, mimetype.IndexOf(';'));
			
			switch (mimetype.Trim()) {
				case "text/xml":
				case "application/xml":
				case "application/rss+xml":
				case "application/rdf+xml":
					return new RdfXmlReader(resp.GetResponseStream());
					
				case "text/rdf+n3":
				case "application/n3":
				case "application/turtle":
				case "application/x-turtle":
					return new N3Reader(new StreamReader(resp.GetResponseStream(), System.Text.Encoding.UTF8));
			}
			
			if (webresource.LocalPath.EndsWith(".rdf") || webresource.LocalPath.EndsWith(".xml") || webresource.LocalPath.EndsWith(".rss"))
				return new RdfXmlReader(resp.GetResponseStream());
			
			if (webresource.LocalPath.EndsWith(".n3") || webresource.LocalPath.EndsWith(".ttl") || webresource.LocalPath.EndsWith(".nt"))
				return new N3Reader(new StreamReader(resp.GetResponseStream(), System.Text.Encoding.UTF8));

			throw new InvalidOperationException("Could not determine the RDF format of the resource.");
		}
		
		internal static TextReader GetReader(string file) {
			if (file == "-") return Console.In;
			return new StreamReader(file);
		}
		
		protected void OnWarning(string message) {
			warnings.Add(message);
		}
		
		internal string GetAbsoluteUri(string baseuri, string uri) {
			if (baseuri == null) {
				//if (uri == "")
				//throw new ParserException("An empty relative URI was found in the document but could not be converted into an absolute URI because no base URI is known for the document.");
				return uri;
			}
			if (uri.IndexOf(':') != -1) return uri;
			try {
				UriBuilder b = new UriBuilder(baseuri);
				b.Fragment = null; // per W3 RDF/XML test suite
				return new Uri(b.Uri, uri, true).ToString();
			} catch (UriFormatException) {
				return baseuri + uri;
			}			
		}

	}
	
	internal class MultiRdfReader : RdfReader {
		private ArrayList parsers = new ArrayList();
		
		public ArrayList Parsers { get { return parsers; } }
		
		public override void Select(StatementSink storage) {
			foreach (RdfReader p in Parsers)
				p.Select(storage);
		}
	}
}

