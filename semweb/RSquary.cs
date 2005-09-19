using System;
using System.Collections;
using System.IO;
using System.Text;

using SemWeb;
using SemWeb.Stores;

namespace SemWeb.Query {
	public class RSquary : QueryEngine {
		
		// TODO: Optional statements
		// TODO: Grouping and disjunctions
		
		public static Entity qSelect = "http://purl.oclc.org/NET/rsquary/select";
		public static Entity qLimit = "http://purl.oclc.org/NET/rsquary/returnLimit";
		public static Entity qStart = "http://purl.oclc.org/NET/rsquary/returnStart";
		public static Entity qDistinctFrom = "http://purl.oclc.org/NET/rsquary/distinctFrom";
		public static Entity qOptional = "http://purl.oclc.org/NET/rsquary/optional";
		
		public RSquary(Store queryModel, string queryUri) : this(queryModel, queryUri, null) {
		}
		
		public RSquary(Store queryModel, string queryUri, Hashtable extraValueFilters) {
			Entity query = new Entity(queryUri);
			
			// Find the query options
			ReturnStart = GetIntOption(queryModel, query, qStart);
			ReturnLimit = GetIntOption(queryModel, query, qLimit);

			// Search the query for 'select'
			foreach (Resource r in queryModel.SelectObjects(query, qSelect)) {
				if (!(r is Entity)) throw new QueryException("Query variables cannot be literals.");
				Select((Entity)r);
			}
			
			// Search the query for 'distinct' predicates between variables.
			foreach (Statement s in queryModel.Select(new Statement(null, qDistinctFrom, null))) {
				if (!(s.Object is Entity)) throw new QueryException("The distinctFrom predicate cannot have a literal as its object.");
				MakeDistinct(s.Subject, (Entity)s.Object);
			}
			
			// Add all statements except the query predicates and value filters into a
			// new store with just the statements relevant to the search.
			foreach (Statement s in queryModel.Select(new Statement(null,null,null))) {
				if (IsQueryPredicate(s.Predicate)) continue;
				
				if (s.Predicate.Uri != null && extraValueFilters != null && extraValueFilters.ContainsKey(s.Predicate.Uri)) {
					ValueFilterFactory f = (ValueFilterFactory)extraValueFilters[s.Predicate.Uri];
					AddValueFilter(s.Subject, f.GetValueFilter(s.Predicate.Uri, s.Object));
					continue;
				} else {
					ValueFilter f = ValueFilter.GetValueFilter(s.Predicate, s.Object);
					if (f != null) {
						AddValueFilter(s.Subject, f);
						continue;
					}
				}
				
				if (s.Meta == Statement.DefaultMeta)
					AddFilter(s);
				else if (queryModel.Contains(new Statement(query, qOptional, s.Meta)))
					AddOptionalFilter(s);
			}
		}
		
		private int GetIntOption(Store queryModel, Entity query, Entity predicate) {
			Resource[] rr = queryModel.SelectObjects(query, predicate);
			if (rr.Length == 0) return -1;
			Resource r = rr[0];
			if (r == null || !(r is Literal)) return -1;
			try {
				return int.Parse(((Literal)r).Value);
			} catch (Exception e) {
				throw new QueryException("Invalid integer value for <" + predicate + ">, '" + ((Literal)r).Value + "'.", e);
			}
		}		

		private bool IsQueryPredicate(Entity e) {
			if (e == qSelect) return true;
			if (e == qDistinctFrom) return true;
			if (e == qLimit) return true;
			if (e == qStart) return true;
			if (e == qOptional) return true;
			return false;
		}
	}
	
	public class PrintQuerySink : QueryResultSink {
		public override void Init(Entity[] variables) { }
		public override void Finished() { }
		public override bool Add(VariableBinding[] result) {
			foreach (VariableBinding var in result)
				if (var.Variable.Uri != null && var.Target != null)
					Console.WriteLine(var.Variable + " ==> " + var.Target.ToString());
			Console.WriteLine();
			return true;
		}
	}
	
	public class HTMLQuerySink : QueryResultSink {
		TextWriter output;
		
		public HTMLQuerySink(TextWriter output) { this.output = output; }

		public override void Init(Entity[] variables) {
			output.WriteLine("<tr>");
			foreach (Entity var in variables)
				if (var.Uri != null)
					output.WriteLine("<th>" + var + "</th>");
			output.WriteLine("</tr>");
		}
		
		public override void Finished() { }
		
		public override bool Add(VariableBinding[] result) {
			output.WriteLine("<tr>");
			foreach (VariableBinding var in result) {
				if (var.Variable.Uri == null) continue;
				string t = var.Target.ToString();
				if (var.Target is Literal) t = ((Literal)var.Target).Value;
				output.WriteLine("<td>" + t + "</td>");
			}
			output.WriteLine("</tr>");			
			return true;
		}
	}

	public class SQLQuerySink : QueryResultSink {
		TextWriter output;
		string table;
		
		public SQLQuerySink(TextWriter output, string table) { this.output = output; this.table = table; }
		
		public override void Finished() { }

		private string GetFieldType(string datatype) {
			switch (datatype) {
				case "http://www.w3.org/2001/XMLSchema#string":
				case "http://www.w3.org/2001/XMLSchema#normalizedString":
					return "TEXT";

				case "http://www.w3.org/2001/XMLSchema#float":
					return "FLOAT";
				
				case "http://www.w3.org/2001/XMLSchema#double":
					return "DOUBLE PRECISION";
				
				case "http://www.w3.org/2001/XMLSchema#decimal":
					return "DECIMAL";
				
				case "http://www.w3.org/2001/XMLSchema#integer":
				case "http://www.w3.org/2001/XMLSchema#nonPositiveInteger":
				case "http://www.w3.org/2001/XMLSchema#negativeInteger":
				case "http://www.w3.org/2001/XMLSchema#int":
				case "http://www.w3.org/2001/XMLSchema#short":
					return "INT";
				
				case "http://www.w3.org/2001/XMLSchema#long":
					return "BIGINT";
				
				
				case "http://www.w3.org/2001/XMLSchema#boolean":
				case "http://www.w3.org/2001/XMLSchema#byte":
				case "http://www.w3.org/2001/XMLSchema#unsignedByte":
					return "SMALLINT";
				
				case "http://www.w3.org/2001/XMLSchema#nonNegativeInteger":
				case "http://www.w3.org/2001/XMLSchema#unsignedInt":
				case "http://www.w3.org/2001/XMLSchema#unsignedShort":
				case "http://www.w3.org/2001/XMLSchema#positiveInteger":
					return "UNSIGNED INT";
				
				case "http://www.w3.org/2001/XMLSchema#unsignedLong":
					return "UNSIGNED BIGINT";
					
				case "http://www.w3.org/2001/XMLSchema#dateTime":
					return "DATETIME";
					
				case "http://www.w3.org/2001/XMLSchema#date":
					return "DATE";
				
				case "http://www.w3.org/2001/XMLSchema#time":
				case "http://www.w3.org/2001/XMLSchema#duration":
					return "TIME";

				case "http://www.w3.org/2001/XMLSchema#base64Binary":
					return "BLOB";

				case "http://www.w3.org/2001/XMLSchema#anyURI":
					// shouldn't be case-insensitive, but using BLOB
					// instead seems to make things too complex.
					return "TEXT";
			}
			
			return "TEXT";
		}
		
		public override void Init(Entity[] variables) {
			output.Write("CREATE TABLE " + table + " (");
			
			bool f = true;
			foreach (Entity var in variables) {
				if (var.Uri == null) continue;
				string name;
				int hash = var.Uri.LastIndexOf("#");
				if (hash == -1) name = "`" + var.Uri + "`";
				else name = var.Uri.Substring(hash+1);
				
				string type = "BLOB";
				//if (var.Target is Literal && ((Literal)var.Target).DataType != null)
				//	type = GetFieldType(((Literal)var.Target).DataType);

				if (!f)  { output.Write(", "); } f = false; 
				output.Write(name + " " + type);
			}
			
			output.WriteLine(");");
		}
		
		public override bool Add(VariableBinding[] result) {
			output.Write("INSERT INTO " + table + " VALUES (");
			bool firstx = true;
			foreach (VariableBinding var in result) {
				if (var.Variable.Uri == null) continue;
				
				if (!firstx)  { output.Write(", "); } firstx = false;
				if (var.Target == null)
					output.Write("NULL");
				else if (var.Target is Literal)
					output.Write(Escape(((Literal)var.Target).Value));
				else if (var.Target.Uri != null)
					output.Write("\"" + var.Target.Uri + "\"");
				else
					output.Write("\"\"");
			}
			output.WriteLine(");");
			
			return true;
		}
		
		private string Escape(string str) {
			if (str == null) return "NULL";
			return "\"" + EscapeUnquoted(str) + "\"";
		}
		
		StringBuilder EscapeUnquotedBuffer = new StringBuilder();
		private string EscapeUnquoted(string str) {
			StringBuilder b = EscapeUnquotedBuffer;
			b.Length = 0;
			b.Append(str);
			SQLStore.Escape(b);
			return b.ToString();
		}
		
	}

	public class SparqlXmlQuerySink : QueryResultSink {
		System.Xml.XmlWriter output;
		string variableNamespace;
		
		int blankNodeCounter = 0;
		Hashtable blankNodes = new Hashtable();
		
		private static System.Xml.XmlWriter GetWriter(System.IO.TextWriter writer) {
			System.Xml.XmlTextWriter w = new System.Xml.XmlTextWriter(writer);
			w.Formatting = System.Xml.Formatting.Indented;
			return w;
		}
		
		public SparqlXmlQuerySink(TextWriter output, string variableNamespace)
		 : this(GetWriter(output), variableNamespace) {
		}

		public SparqlXmlQuerySink(System.Xml.XmlWriter output, string variableNamespace) {
			this.output = output;
			this.variableNamespace = variableNamespace;
			output.WriteStartElement("sparql");
			output.WriteAttributeString("xmlns", "http://www.w3.org/2001/sw/DataAccess/rf1/result");
			output.WriteStartElement("head");
		}
		
		string GetName(string uri) {
			if (uri.StartsWith(variableNamespace))
				uri = uri.Substring(variableNamespace.Length);
			if (uri.StartsWith("?") || uri.StartsWith("$"))
				uri = uri.Substring(1);
			return uri;
		}
		
		public override void Init(Entity[] variables) {
			foreach (Entity var in variables) {
				if (var.Uri == null) continue;
				output.WriteStartElement("variable");
				output.WriteAttributeString("name", GetName(var.Uri));
				output.WriteEndElement();
			}
			output.WriteEndElement(); // head
			output.WriteStartElement("results");
		}
		
		public override bool Add(VariableBinding[] result) {
			output.WriteStartElement("result");
			foreach (VariableBinding var in result) {
				if (var.Variable.Uri == null) continue;
				
				output.WriteStartElement(GetName(var.Variable.Uri));
				if (var.Target == null) {
					output.WriteAttributeString("bound", "false");
				} else if (var.Target.Uri != null) {
					output.WriteAttributeString("uri", var.Target.Uri);
				} else if (var.Target is Literal) {
					Literal literal = (Literal)var.Target;
					if (literal.DataType != null)
						output.WriteAttributeString("datatype", literal.DataType);
					if (literal.Language != null)
						output.WriteAttributeString("language", literal.Language);
					output.WriteString(literal.Value);				
				} else {
					string id;
					if (blankNodes.ContainsKey(var.Target))
						id = (string)blankNodes[var.Target];
					else {
						id = "r" + (++blankNodeCounter);
						blankNodes[var.Target] = id;
					}
					output.WriteAttributeString("bnodeid", id);
				}
				
				output.WriteEndElement();
			}
			output.WriteEndElement();
			
			return true;
		}
		
		public override void Finished() {
			output.WriteEndElement(); // results
			output.WriteEndElement(); // sparql
			output.Close();
		}
	}

}	

