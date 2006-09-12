using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
 
namespace SemWeb.Remote {

	public class SparqlHttpSource : SelectableSource {
	
		string url;
		
		public SparqlHttpSource(string url) {
			this.url = url;
		}

		public bool Distinct { get { return true; } }
		
		public bool Contains(Statement template) {
			return Select(template, null, true);
		}
		
		public void Select(StatementSink sink) {
			Select(Statement.All, sink);
		}
		
		public void Select(Statement template, StatementSink sink) {
			Select(template, sink, false);
		}
		
		bool Select(Statement template, StatementSink sink, bool ask) {
			return Select(
				template.Subject == null ? null : new Entity[] { template.Subject },
				template.Predicate == null ? null : new Entity[] { template.Predicate },
				template.Object == null ? null : new Resource[] { template.Object },
				template.Meta == null ? null : new Entity[] { template.Meta },
				null,
				0,
				sink,
				ask
				);
		}
		
		public void Select(SelectFilter filter, StatementSink sink) {
			Select(filter.Subjects, filter.Predicates, filter.Objects, filter.Metas, filter.LiteralFilters, filter.Limit, sink, false);
		}
		
		bool Select(Entity[] subjects, Entity[] predicates, Resource[] objects, Entity[] metas, LiteralFilter[] litFilters, int limit, StatementSink sink, bool ask) {
			// TODO: Change meta into named graphs.  Anything but a null or DefaultMeta
			// meta returns no statements immediately.
			if (metas != null && (metas.Length != 1 || metas[0] != Statement.DefaultMeta))
				return false;
		
			string query;
			bool nonull = false;
		
			if (subjects != null && subjects.Length == 1
				&& predicates != null && predicates.Length == 1
				&& objects != null && objects.Length == 1) {
				query = "ASK WHERE { " + S(subjects[0], null) + " " + S(predicates[0], null) + " " + S(objects[0], null) + "}";
				nonull = true;
			} else {
				if (ask)
					query = "ASK";
				else
					query = "SELECT *";
				query += " WHERE { ";
				query += S(subjects, "subject");
				query += " ";
				query += S(predicates, "predicate");
				query += " ";
				query += S(objects, "object");
				query += " . ";
				query += SL(subjects, "subject");
				query += SL(predicates, "predicate");
				query += SL(objects, "object");
				query += " }";
				
				// TODO: Pass literal filters to server.
			}
			
			if (limit >= 1)
				query += " LIMIT " + limit;
			
			XmlDocument result = Load(query);
			
			if (ask || nonull) {
				foreach (XmlElement boolean in result.DocumentElement) {
					if (boolean.Name != "boolean") continue;
					bool ret = boolean.InnerText == "true";
					if (ask)
						return ret;
					else if (ret)
						sink.Add(new Statement(subjects[0], predicates[0], objects[0]));
					return false;
				}
				throw new ApplicationException("Invalid server response: No boolean node.");
			}
			
			XmlElement bindings = null;
			foreach (XmlElement e in result.DocumentElement)
				if (e.Name == "results")
					bindings = e;
			if (bindings == null)
				throw new ApplicationException("Invalid server response: No result node.");
			
			MemoryStore distinctCheck = null;
			if (bindings.GetAttribute("distinct") != "true")
				distinctCheck = new MemoryStore();
			
			Hashtable bnodes = new Hashtable();
			
			foreach (XmlNode bindingnode in bindings) {
				if (!(bindingnode is XmlElement)) continue;
				XmlElement binding = (XmlElement)bindingnode;
				Resource subj = GetBinding(binding, "subject", subjects, bnodes);
				Resource pred = GetBinding(binding, "predicate", predicates, bnodes);
				Resource obj = GetBinding(binding, "object", objects, bnodes);
				if (!(subj is Entity) || !(pred is Entity)) continue;
				Statement s = new Statement((Entity)subj, (Entity)pred, obj);
				if (distinctCheck != null && distinctCheck.Contains(s)) continue;
				if (litFilters != null && !LiteralFilter.MatchesFilters(s.Object, litFilters, this)) continue;
				if (!sink.Add(s)) return true;
				if (distinctCheck != null) distinctCheck.Add(s);
			}
			
			return true;
		}
		
		string S(Resource[] r, string v) {
			if (r == null || r.Length != 1) return "?" + v;
			return S(r[0], null);
		}
		string SL(Resource[] r, string v) {
			if (r == null || r.Length <= 1) return "";
			StringBuilder ret = new StringBuilder();
			ret.Append("FILTER(");
			bool first = true;
			for (int i = 0; i < r.Length; i++) {
				if (r[i].Uri == null) continue;
				if (!first) ret.Append(" || "); first = false;
				ret.Append('?');
				ret.Append(v);
				ret.Append("=<");
				if (r[i].Uri != null)
					ret.Append(r[i].Uri);
				ret.Append('>');
			}
			ret.Append(").");
			if (first) return "";
			return ret.ToString();
		}
		
		string S(Resource r, string v) {
			if (r == null) {
				return v;
			} else if (r is Literal) {
				return r.ToString();
			} else if (r.Uri != null) {
				if (r.Uri.IndexOf('>') != -1)
					throw new ArgumentException("Invalid URI: " + r.Uri);
				return "<" + r.Uri + ">";
			} else {
				throw new ArgumentException("Blank node in select not supported.");
			}
		}
		
		Resource GetBinding(XmlElement binding, string v, Resource[] values, Hashtable bnodes) {
			if (values != null && values.Length == 1) return values[0];
			
			XmlElement b = (XmlElement)binding.FirstChild;
			while (b != null && b.GetAttribute("name") != v)
				b = (XmlElement)b.NextSibling;
			if (b == null)
				throw new ApplicationException("Invalid server response: Not all bindings present (" + v + "): " + binding.OuterXml);
			
			b = (XmlElement)b.FirstChild;
			if (b.Name == "uri")
				return new Entity(b.InnerText);
			else if (b.Name == "literal")
				return new Literal(b.InnerText); // datatype/lang
			else if (b.Name == "bnode") {
				string id = b.InnerText;
				if (bnodes.ContainsKey(id)) return (Entity)bnodes[id];
				Entity ret = new BNode();
				bnodes[id] = ret;
				return ret;
			}
			throw new ApplicationException("Invalid server response: " + b.OuterXml);
		}
		
		XmlDocument Load(string query) {
			string qstr = "query=" + System.Web.HttpUtility.UrlEncode(query);
			
			string method = "POST";
			
			System.Net.WebRequest rq;
			
			if (method == "GET") {
				string qurl = url + "?" + qstr;
				rq = System.Net.WebRequest.Create(qurl);
			} else {
				ASCIIEncoding encoding = new ASCIIEncoding(); // ?
				byte[] data = encoding.GetBytes(qstr);

				rq = System.Net.WebRequest.Create(url);
				rq.Method = "POST";
				rq.ContentType="application/x-www-form-urlencoded";
				rq.ContentLength = data.Length;
				
				using (Stream stream = rq.GetRequestStream())
					stream.Write(data, 0, data.Length);
			}
			
			System.Net.WebResponse resp = rq.GetResponse();
			
			string mimetype = resp.ContentType;
			if (mimetype.IndexOf(';') > -1)
				mimetype = mimetype.Substring(0, mimetype.IndexOf(';'));
			
			if (mimetype != "application/sparql-results+xml")
				throw new ApplicationException("The result of the query was not a SPARQL Results document.");

			XmlDocument ret = new XmlDocument();
			ret.Load(new StreamReader(resp.GetResponseStream(), System.Text.Encoding.UTF8));
						
			if (ret.DocumentElement.Name != "sparql")
				throw new ApplicationException("Invalid server response: Not a sparql results document.");
			
			return ret;
		}
		
		public Entity[] FindEntities(Statement[] graph) {
			string query = "SELECT ?entity WHERE { ";
			
			foreach (Statement s in graph) {
				query += S(s.Subject, "?entity");
				query += " ";
				query += S(s.Predicate, "?entity");
				query += " ";
				query += S(s.Object, "?entity");
				query += " . ";
				if (s.Meta != Statement.DefaultMeta) return new Entity[0];
			}
			
			query += "}";
			
			XmlDocument result = Load(query);
			
			XmlElement bindings = null;
			foreach (XmlElement e in result.DocumentElement)
				if (e.Name == "results")
					bindings = e;
			if (bindings == null)
				throw new ApplicationException("Invalid server response: No result node.");
			
			Hashtable bnodes = new Hashtable();
			ArrayList ret = new ArrayList();
			
			foreach (XmlElement binding in bindings) {
				Entity e = (Entity)GetBinding(binding, "entity", null, bnodes);
				ret.Add(e);
			}
			
			return (Entity[])ret.ToArray(typeof(Entity));
		}

	}
}

