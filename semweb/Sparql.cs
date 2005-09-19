using System;
using System.Collections;
using System.IO;
using System.Text;

using SemWeb;
using SemWeb.IO;
using SemWeb.Stores;

namespace SemWeb.Query {

	public class SparqlParser {
		MyReader reader;
		
		NamespaceManager nsmgr = new NamespaceManager();
		
		string baseuri = null;
		SparqlQuestion question;
		
		ArrayList variables = new ArrayList();
		ArrayList selectvariables = new ArrayList();
		bool selectdistinct = false;
		bool selectall = false;
		
		MemoryStore graph = new MemoryStore();
		
		public SparqlParser(TextReader reader) {
			this.reader = new MyReader(reader);
			
			Parse();
		}
		
		public string BaseUri { get { return baseuri; } }
		
		public SparqlQuestion Question { get { return question; } }
		
		public bool Distinct { get { return selectdistinct; } }
		
		public bool All { get { return selectall; } }
		
		public Store Graph { get { return graph; } }
		
		public IList Variables { get { return ArrayList.ReadOnly(variables); } }
		
		public IList Select { get { return ArrayList.ReadOnly(selectvariables); } }
		
		public enum SparqlQuestion {
			Select,
			Construct,
			Describe,
			Ask
		}
		
		public QueryEngine CreateQuery() {
			QueryEngine query = new QueryEngine();
			
			foreach (string var in variables) {
				query.Select(new Entity(var));
			}
			
			foreach (Statement s in graph)
				query.AddFilter(s);
			
			//graph.Select(new N3Writer(Console.Out));
			
			return query;
		}
		
		private void ReadWhitespace() {
			while (true) {
				while (char.IsWhiteSpace((char)reader.Peek()))
					reader.Read();
				
				if (reader.Peek() == '#') {
					while (true) {
						int c = reader.Read();
						if (c == -1 || c == 10 || c == 13) break;
					}
					continue;
				}
				
				break;
			}
		}
		
		private string ReadToken() {
			ReadWhitespace();
			
			StringBuilder b = new StringBuilder();
			while (reader.Peek() != -1 && char.IsLetter((char)reader.Peek()))
				b.Append((char)reader.Read());
			
			return b.ToString();
		}
		
		private string ReadName() {
			ReadWhitespace();
			
			StringBuilder b = new StringBuilder();
			while (true) {
				int c = reader.Peek();
				if (c == -1 || char.IsWhiteSpace((char)c)
					|| (!char.IsLetterOrDigit((char)c) && c != '?' && c != '$' && c != '-' && c != '_')) break;
				c = reader.Read();
				b.Append((char)c);
			}
			return b.ToString();
		}

		private string ReadQuotedUri() {
			ReadWhitespace();
			Location loc = reader.Location;
			
			int open = reader.Read();
			if (open != '<')
				OnError("Expecting a quoted URI starting with a '<'", loc);
			
			StringBuilder b = new StringBuilder();
			while (true) {
				int c = reader.Read();
				
				if (c == -1)
					OnError("End of file while reading a URI", loc);
				if (c == '>')
					break;
				if (char.IsWhiteSpace((char)c))
					OnError("White space cannot appear in a URI", loc);
				
				b.Append((char)c);
			}
			
			return b.ToString();
		}
		
		private string ReadQNamePrefix() {
			ReadWhitespace();
			Location loc = reader.Location;
			
			StringBuilder b = new StringBuilder();
			while (true) {
				int c = reader.Read();
				
				if (c == -1)
					OnError("End of file while reading a QName prefix", loc);
				if (char.IsWhiteSpace((char)c))
					OnError("Expecting a colon, " + b, loc);
				
				b.Append((char)c);

				if (c == ':')
					break;
			}
			
			return b.ToString();
		}
		
		private string ReadUri() {
			ReadWhitespace();
			if (reader.Peek() == '<')
				return ReadQuotedUri();
			
			Location loc = reader.Location;
			string prefix = ReadQNamePrefix();
			string localname = ReadName();

			if (prefix == ":") {
				if (baseuri == null)
					OnError("No BASE URI was specified", loc);
				return baseuri + localname;
			}
			
			return nsmgr.Resolve(prefix + localname);
		}
		
		private string ReadVarOrUri() {
			ReadWhitespace();

			if (reader.Peek() == '?' || reader.Peek() == '$')
				return ReadName();

			return ReadUri(); 
		}
		
		private object ReadNumber() {
			ReadWhitespace();
			Location loc = reader.Location;
			string num = ReadName();
			try {
				if (num.StartsWith("0x") || num.StartsWith("0X"))
					return int.Parse(num.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
				if (num.IndexOf(".") >= 0 || num.IndexOf("e") >= 0 || num.IndexOf("E") >= 0)
					return decimal.Parse(num);
				return long.Parse(num);
			} catch (Exception e) {
				OnError("Invalid number: " + num, loc);
				return null;
			}
		}
		
		private string ReadLiteralText() {
			char quotechar = (char)reader.Read();
			
			StringBuilder b = new StringBuilder();
			
			bool escaped = false;
			while (true) {
				Location loc = reader.Location;
				int c = reader.Read();
				if (c == -1)
					OnError("End of file while reading a text literal", loc);
				
				if (escaped) {
					switch ((char)c) {
						case 'n': b.Append('\n'); break;
						case 'r': b.Append('\r'); break;
						default: b.Append(c); break;
					}
					escaped = false;
				} else if (c == '\\') {
					escaped = true;
				} else if (c == quotechar) {
					break;
				} else {
					b.Append((char)c);
				}				
			}
			
			return b.ToString();
		}
		
		private object ReadLiteral() {
			ReadWhitespace();
			int firstchar = reader.Peek();
			if (firstchar == -1) OnError("End of file expecting a literal", reader.Location);
			
			if (char.IsDigit((char)firstchar) || firstchar == '.')
				return new Literal(ReadNumber().ToString(), null, null);
			
			if (firstchar == '\'' || firstchar == '\"') {
				string text = ReadLiteralText();
				string lang = null, datatype = null;
				if (reader.Peek() == '@') {
					reader.Read();
					lang = ReadName();
				}
				if (reader.Peek() == '^') {
					reader.Read();
					reader.Read();
					datatype = ReadUri();
				}
				return new Literal(text, lang, datatype);
			}
			
			return ReadVarOrUri();
		}
		
		private Entity GetEntity(string name) {
			if (name[0] == '?' || name[0] == '$') {
				if (!variables.Contains(name))
					variables.Add(name);
			}
			
			return new Entity(name);
		}
		
		private int ReadPatternGroup() {
			//bool nextoptional = false;
			
			while (true) {
				ReadWhitespace();
				Location loc = reader.Location;
				
				int next = reader.Peek();
				if (next == -1)
					return -1;
				
				if (next == 'U') {
					string union = ReadToken();
					if (union != "UNION")
						OnError("Expecting UNION", loc);
					
					OnError("UNION is not supported", loc);
					
				} else if (next == 'G') {
					string graph = ReadToken();
					if (graph != "GRAPH")
						OnError("Expecting GRAPH", loc);
					
					OnError("GRAPH is not supported", loc);

				} else if (next == 'O') {
					string optional = ReadToken();
					if (optional != "OPTIONAL")
						OnError("Expecting OPTIONAL", loc);
					
					OnError("OPTIONAL is not supported", loc);
					
					//nextoptional = true;
					continue;

				} else if (next == 'A') {
					string and = ReadToken();
					if (and != "AND")
						OnError("Expecting AND", loc);
					
					loc = reader.Location;
					OnError("Expressions are not supported", loc);

				} else if (next == '(') {
					// Triple Pattern
					reader.Read(); // open paren
					string subj = ReadVarOrUri();
					string pred = ReadVarOrUri();
					object obj = ReadLiteral();
					
					ReadWhitespace();
					loc = reader.Location;
					int close = reader.Read();
					if (close != ')')
						OnError("Expecting close parenthesis: " + (char)close, loc);
					
					graph.Add( new Statement (
						GetEntity(subj),
						GetEntity(pred),
						obj is string ? (Resource)GetEntity((string)obj) : (Resource)obj
						) );
				
				} else if (next == '{') {
					ReadPatternGroup();
						
				} else if (next == '}') {
					return next;
					
				} else {
					return -1;
				}
				
				//nextoptional = false;
			}
			
		}

		enum ReadState {
			StartOfProlog,
			Prolog,
			Limits
		}
		
		private void Parse() {
			ReadState state = ReadState.StartOfProlog;
			
			while (true) {
				Location loc = reader.Location;
				string clause = ReadToken();
				
				switch (clause) {
					case "":
						if (state <= ReadState.Prolog)
							OnError("No query was given", loc);
						return;
					
					case "BASE":
						if (state != ReadState.StartOfProlog)
							OnError("BASE must be the first clause", loc);
						baseuri = ReadQuotedUri();
						state = ReadState.Prolog;
						break;
					
					case "PREFIX":
						if (state > ReadState.Prolog)
							OnError("PREFIX must occur in the prolog", loc);
						string prefix = ReadQNamePrefix();
						string uri = ReadQuotedUri();
						nsmgr.AddNamespace(uri, prefix.Substring(0, prefix.Length-1)); // strip trailing ':'
						state = ReadState.Prolog;
						break;
						
					case "SELECT":
						if (state > ReadState.Prolog)
							OnError("SELECT cannot occur here", loc);
						state = ReadState.Limits;
						
						question = SparqlQuestion.Select;
						while (true) {
							ReadWhitespace();
							loc = reader.Location;
							int next = reader.Peek();
							if (next == ',') { reader.Read(); continue; }
							if (next != 'D' && next != '?' && next != '$' && next != '*')
								break;
							string var = ReadName();
							if (var == "DISTINCT") {
								selectdistinct = true;
							} else if (var[0] == '?' || var[0] == '$') {
								if (selectall)
									OnError("Cannot select * and also name other variables", loc);
								if (!selectvariables.Contains(var))
									selectvariables.Add(var);
							} else if (var == "*") {
								if (selectvariables.Count > 0)
									OnError("Cannot select * and also name other variables", loc);
								selectall = true;
								break;
							} else {
								OnError("Invalid variable: " + var, loc);
							}
						}
						
						break;

					case "DESCRIBE":
						if (state > ReadState.Prolog)
							OnError("DESCRIBE cannot occur here", loc);
						state = ReadState.Limits;
						OnError("DESCRIBE is not supported", loc);

						question = SparqlQuestion.Describe;
						break;

					case "CONSTRUCT":
						if (state > ReadState.Prolog)
							OnError("CONSTRUCT cannot occur here", loc);
						state = ReadState.Limits;
						OnError("CONSTRUCT is not supported", loc);

						question = SparqlQuestion.Construct;
						break;

					case "ASK":
						if (state > ReadState.Prolog)
							OnError("ASK cannot occur here", loc);
						state = ReadState.Limits;

						question = SparqlQuestion.Ask;
						break;

					case "WITH":
					case "FROM":
						if (state != ReadState.Limits)
							OnError("WITH cannot occur here", loc);
						OnError("WITH is not supported", loc);
						break;

					case "WHERE":
						if (state != ReadState.Limits)
							OnError("WHERE cannot occur here", loc);
						ReadPatternGroup();						
						break;
				}
			}
		}
		
		private void OnError(string message, Location position) {
			throw new ParserException(message + ", line " + position.Line + " col " + position.Col);
		}
		
	}
	
}
