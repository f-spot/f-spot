using System;
using System.Collections;
using System.IO;
using System.Text;

using SemWeb;

namespace SemWeb {
	public class N3Writer : RdfWriter {
		TextWriter writer;
		NamespaceManager ns = new NamespaceManager();
		bool hasWritten = false;
		bool closed = false;
		
		string lastSubject = null, lastPredicate = null;
		
		Hashtable anonNames = new Hashtable();
		Hashtable anonNameMap = new Hashtable();
		
		Formats format = Formats.Turtle;
		
		public enum Formats {
			NTriples,
			Turtle,
			Notation3
		}
		
		public N3Writer(string file) : this(GetWriter(file)) { }

		public N3Writer(TextWriter writer) {
			this.writer = writer;
		}
		
		public override NamespaceManager Namespaces { get { return ns; } }
		
		public Formats Format { get { return format; } set { format = value; } }
		
		public override void Add(Statement statement) {
			if (statement.AnyNull) throw new ArgumentNullException();
			WriteStatement2(URI(statement.Subject), URI(statement.Predicate),
				statement.Object is Literal ? ((Literal)statement.Object).ToString() : URI((Entity)statement.Object));
		}

		public override void Close() {
			base.Close();
			if (closed) return;
			if (hasWritten)
				writer.WriteLine(".");
			closed = true;
			hasWritten = false;
			writer.Flush();
		}

		
		private string URI(Entity entity) {
			if (entity is Variable && ((Variable)entity).LocalName != null)
				return "?" + ((Variable)entity).LocalName;
				
			if (entity is BNode) {
				string name = ((BNode)entity).LocalName;
				if (name != null &&
					(anonNameMap[name] == null || (BNode)anonNameMap[name] == entity)
					&& !name.StartsWith("bnode")) {
					return "_:" + name;
				} else if (anonNames[entity] != null) {
					return (string)anonNames[entity];
				} else {
					string id = "_:bnode" + anonNames.Count;
					anonNames[entity] = id;
					return id;
				}
			}
			
			string uri = entity.Uri;
			string effectiveBaseUri = BaseUri == null ? "#" : BaseUri;
			if (effectiveBaseUri != null && uri.StartsWith(effectiveBaseUri)) {
				int len = effectiveBaseUri.Length;
				bool ok = true;
				for (int i = len; i < uri.Length; i++) {
					if (!char.IsLetterOrDigit(uri[i])) { ok = false; break; }
				}
				if (ok)
					return ":" + uri.Substring(len);
			}
			if (Format == Formats.NTriples || ns == null) return "<" + Escape(uri) + ">";
			
			string ret = ns.Normalize(uri);
			if (ret[0] != '<') return ret;
			
			return "<" + Escape(uri) + ">";
		}
		
		private static char HexDigit(char c, int digit) {
			int n = (((int)c) >> (digit * 4)) & 0xF;
			if (n <= 9)
				return (char)('0' + n);
			else
				return (char)('A' + (n-10));
		}
		
		internal static string Escape(string str) {
			// Check if any escaping is necessary, following the NTriples spec.
			bool needed = false;
			for (int i = 0; i < str.Length; i++) {
				char c = str[i];
				if (!((c >= 0x20 && c <= 0x21) || (c >= 0x23 && c <= 0x5B) || (c >= 0x5D && c <= 0x7E))) {
					needed = true;
					break;
				}
			}
			
			if (!needed) return str;
			
			StringBuilder b = new StringBuilder();
			for (int i = 0; i < str.Length; i++) {
				char c = str[i];
				if ((c >= 0x20 && c <= 0x21) || (c >= 0x23 && c <= 0x5B) || (c >= 0x5D && c <= 0x7E)) {
					b.Append(c);
				} else if (c == 0x9) {
					b.Append("\\t");				
				} else if (c == 0xA) {
					b.Append("\\n");				
				} else if (c == 0xD) {
					b.Append("\\r");				
				} else if (c == 0x22) {
					b.Append("\\\"");				
				} else if (c == 0x5C) {
					b.Append("\\\\");				
				} else if (c <= 0x8 || c == 0xB || c == 0xC || (c >= 0xE && c <= 0x1F) || (c >= 0x7F && c <= 0xFFFF)) {
					b.Append("\\u");
					b.Append(HexDigit(c, 3));
					b.Append(HexDigit(c, 2));
					b.Append(HexDigit(c, 1));
					b.Append(HexDigit(c, 0));
				/*} else if (c >= 0x10000) {
					b.Append("\\U");
					b.Append(HexDigit(c, 7));
					b.Append(HexDigit(c, 6));
					b.Append(HexDigit(c, 5));
					b.Append(HexDigit(c, 4));
					b.Append(HexDigit(c, 3));
					b.Append(HexDigit(c, 2));
					b.Append(HexDigit(c, 1));
					b.Append(HexDigit(c, 0));*/
				}
			}
			return b.ToString();
		}
		
		private void WriteStatement2(string subj, string pred, string obj) {
			closed = false;
			
			// Write the prefix directives at the beginning.
			if (!hasWritten && ns != null && !(Format == Formats.NTriples)) {
				foreach (string prefix in ns.GetPrefixes()) {
					writer.Write("@prefix ");
					writer.Write(prefix);
					writer.Write(": <");
					writer.Write(ns.GetNamespace(prefix));
					writer.Write("> .\n");
				}
			}

			// Repeated subject.
			if (lastSubject != null && lastSubject == subj && !(Format == Formats.NTriples)) {
				// Repeated predicate too.
				if (lastPredicate != null && lastPredicate == pred) {
					writer.Write(",\n\t\t");
					WriteThing(obj);
					
				// Just a repeated subject.
				} else {
					writer.Write(";\n\t");
					WriteThing(pred);
					WriteThing(obj);
					lastPredicate = pred;
				}
			
			// The subject became the object.  Abbreviate with
			// is...of notation (Notation3 format only).
			} else if (lastSubject != null && lastSubject == obj && (Format == Formats.Notation3)) {
				writer.Write(";\n\tis ");
				WriteThing(pred);
				writer.Write("of ");
				WriteThing(subj);
				lastPredicate = null;
			
			// Start a new statement.
			} else {
				if (hasWritten)
					writer.Write(".\n");
					
				WriteThing(subj);
				WriteThing(pred);
				WriteThing(obj);
				
				lastSubject = subj;
				lastPredicate = pred;
			}
			
			hasWritten = true;
		}
		
		private void WriteThing(string text) {
			writer.Write(text);
			writer.Write(" ");
		}
	}
}
