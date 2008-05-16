using System;
using System.Collections;

namespace SemWeb {
	
	public abstract class Resource : IComparable {
		internal object ekKey, ekValue;
		internal ArrayList extraKeys;
		
		internal class ExtraKey {
			public object Key;
			public object Value; 
			public ExtraKey(object k, object v) { Key = k; Value = v; }
		}
		
		public abstract string Uri { get; }
		
		internal Resource() {
		}
		
		// These get rid of the warning about overring ==, !=.
		// Since Entity and Literal override these, we're ok.
		public override bool Equals(object other) {
			return base.Equals(other);
		}
		public override int GetHashCode() {
			return base.GetHashCode();
		}
		
		public static bool operator ==(Resource a, Resource b) {
			if ((object)a == null && (object)b == null) return true;
			if ((object)a == null || (object)b == null) return false;
			return a.Equals(b);
		}
		public static bool operator !=(Resource a, Resource b) {
			return !(a == b);
		}
		
		internal object GetResourceKey(object key) {
			if (ekKey == key) return ekValue;
			if (extraKeys == null) return null;
			for (int i = 0; i < extraKeys.Count; i++) {
				Resource.ExtraKey ekey = (Resource.ExtraKey)extraKeys[i];
				if (ekey.Key == key)
					return ekey.Value;
			}
			return null;
		}
		internal void SetResourceKey(object key, object value) {
			if (ekKey == null || ekKey == key) {
				ekKey = key;
				ekValue = value;
				return;
			}
			
			if (this is BNode) throw new InvalidOperationException("Only one resource key can be set for a BNode.");
		
			if (extraKeys == null) extraKeys = new ArrayList();
			
			foreach (Resource.ExtraKey ekey in extraKeys)
				if (ekey.Key == key) { ekey.Value = value; return; }
			
			Resource.ExtraKey k = new Resource.ExtraKey(key, value);
			extraKeys.Add(k);
		}

		int IComparable.CompareTo(object other) {
			// We'll make an ordering over resources.
			// First named entities, then bnodes, then literals.
			// Named entities are sorted by URI.
			// Bnodes by hashcode.
			// Literals by their value, language, datatype.
		
			Resource r = (Resource)other;
			if (Uri != null && r.Uri == null) return -1;
			if (Uri == null && r.Uri != null) return 1;
			if (this is BNode && r is Literal) return -1;
			if (this is Literal && r is BNode) return 1;
			
			if (Uri != null) return String.Compare(Uri, r.Uri, false, System.Globalization.CultureInfo.InvariantCulture);
			
			if (this is BNode) return GetHashCode().CompareTo(r.GetHashCode());

			if (this is Literal) {
				int x = String.Compare(((Literal)this).Value, ((Literal)r).Value, false, System.Globalization.CultureInfo.InvariantCulture);
				if (x != 0) return x;
				x = String.Compare(((Literal)this).Language, ((Literal)r).Language, false, System.Globalization.CultureInfo.InvariantCulture);
				if (x != 0) return x;
				x = String.Compare(((Literal)this).DataType, ((Literal)r).DataType, false, System.Globalization.CultureInfo.InvariantCulture);
				return x;
			}
			
			return 0; // unreachable
		}
	}
	
	public class Entity : Resource {
		private string uri;
		
		public Entity(string uri) {
			if (uri == null) throw new ArgumentNullException("To construct entities with no URI, use the BNode class.");
			//if (uri.Length == 0) throw new ArgumentException("uri cannot be the empty string");
			this.uri = uri;
		}
		
		// For the BNode constructor only.
		internal Entity() {
		}
		
		public override string Uri {
			get {
				return uri;
			}
		}
		
		public static implicit operator Entity(string uri) { return new Entity(uri); }
		
		public override int GetHashCode() {
			if (uri == null) return base.GetHashCode(); // this is called from BNode.GetHashCode().
			return uri.GetHashCode();
		}
			
		public override bool Equals(object other) {
			if (!(other is Resource)) return false;
			if (object.ReferenceEquals(this, other)) return true;
			return ((Resource)other).Uri != null && ((Resource)other).Uri == Uri;
		}
		
		// Although these do the same as Resource's operator overloads,
		// having these plus the implict string conversion allows
		// these operators to work with entities and strings.

		public static bool operator ==(Entity a, Entity b) {
			if ((object)a == null && (object)b == null) return true;
			if ((object)a == null || (object)b == null) return false;
			return a.Equals(b);
		}
		public static bool operator !=(Entity a, Entity b) {
			return !(a == b);
		}
		
		public override string ToString() {
			return "<" + Uri + ">";
		}
	}
	
	public class BNode : Entity {
		string localname;
	
		public BNode() {
		}
		
		public BNode(string localName) {
			localname = localName;
			if (localname != null && localname.Length == 0) throw new ArgumentException("localname cannot be the empty string");
		}
		
		public string LocalName { get { return localname; } }

		public override int GetHashCode() {
			if (ekKey != null)
				return ekKey.GetHashCode() ^ ekValue.GetHashCode();
			
			// If there's no ExtraKeys info, then this
			// object is only equal to itself.  It's then safe
			// to use object.GetHashCode().
			return base.GetHashCode();
		}
			
		public override bool Equals(object other) {
			if (object.ReferenceEquals(this, other)) return true;
			if (!(other is BNode)) return false;
			
			object okKey = ((Resource)other).ekKey;
			object okValue = ((Resource)other).ekValue;
			
			return (ekKey != null && okKey != null)
				&& (ekKey == okKey)
				&& ekValue.Equals(okValue);
		}
		
		public override string ToString() {
			if (LocalName != null)
				return "_:" + LocalName;
			else
				return "_:bnode" + GetHashCode();
		}
	}
	
	public class Variable : BNode {
		public Variable() : base() {
		}
		
		public Variable(string variableName) : base(variableName) {
		}
		
		public override string ToString() {
			if (LocalName != null)
				return "?" + LocalName;
			else
				return "?var" + GetHashCode();
		}
	}

	public sealed class Literal : Resource { 
		private const string XMLSCHEMANS = "http://www.w3.org/2001/XMLSchema#";

		private string value, lang, type;
		
		public Literal(string value) : this(value, null, null) {
		}
		
		public Literal(string value, string language, string dataType) {
		  if (value == null)
			  throw new ArgumentNullException("value");
		  this.value = string.Intern(value);
		  this.lang = language;
		  this.type = dataType;
		  
		  if (language != null && language.Length == 0) throw new ArgumentException("language cannot be the empty string");
		  if (dataType != null && dataType.Length == 0) throw new ArgumentException("dataType cannot be the empty string");
		}
		
		public static explicit operator Literal(string value) { return new Literal(value); }

		public override string Uri { get { return null; } }
		
		public string Value { get { return value; } }
		public string Language { get { return lang; } }
		public string DataType { get { return type; } }
		
		public override bool Equals(object other) {
			if (other == null) return false;
			if (!(other is Literal)) return false;
			Literal literal = (Literal)other;
			if (Value != literal.Value) return false;
			if (different(Language, literal.Language)) return false;
			if (different(DataType, literal.DataType)) return false;		
			return true;
		}
		
		private bool different(string a, string b) {
			if ((object)a == (object)b) return false;
			if (a == null || b == null) return true;
			return a != b;
		}
		
		public override int GetHashCode() {
			return Value.GetHashCode(); 
		 }
		
		public override string ToString() {
			System.Text.StringBuilder ret = new System.Text.StringBuilder();
			ret.Append('"');
			ret.Append(N3Writer.Escape(Value));
			ret.Append('"');
			
			if (Language != null) {
				ret.Append('@');
				ret.Append(N3Writer.Escape(Language));
			}
			
			if (DataType != null) {
				ret.Append("^^<");
				ret.Append(N3Writer.Escape(DataType));
				ret.Append(">");
			}
			return ret.ToString();
		}
		
		public static Literal Parse(string literal, NamespaceManager namespaces) {
			if (literal.Length < 2 || literal[0] != '\"') throw new FormatException("Literal value must start with a quote.");
			int quote = literal.LastIndexOf('"');
			if (quote <= 0) throw new FormatException("Literal value must have an end quote (" + literal + ")");
			string value = literal.Substring(1, quote-1);
			literal = literal.Substring(quote+1);
			
			value = value.Replace("\\\"", "\"");
			value = value.Replace("\\\\", "\\");
			
			string lang = null;
			string datatype = null;
			
			if (literal.Length >= 2 && literal[0] == '@') {
				int type = literal.IndexOf("^^");
				if (type == -1) lang = literal.Substring(1);
				else {
					lang = literal.Substring(1, type);
					literal = literal.Substring(type);
				}
			}
			
			if (literal.StartsWith("^^")) {
				if (literal.StartsWith("^^<") && literal.EndsWith(">")) {
					datatype = literal.Substring(3, literal.Length-4);
				} else {
					if (namespaces == null)
						throw new ArgumentException("No NamespaceManager was given to resolve the QName in the literal string.");
					datatype = namespaces.Resolve(literal.Substring(2));
				}
			}
			
			return new Literal(value, lang, datatype);
		}
		
		public object ParseValue() {
			string dt = DataType;
			if (dt == null || !dt.StartsWith(XMLSCHEMANS)) return Value;
			dt = dt.Substring(XMLSCHEMANS.Length);
			
			if (dt == "string" || dt == "normalizedString" || dt == "anyURI") return Value;
			if (dt == "boolean") return (Value == "true" || Value == "1");
			if (dt == "decimal" || dt == "integer" || dt == "nonPositiveInteger" || dt == "negativeInteger" || dt == "nonNegativeInteger" || dt == "positiveInteger") return Decimal.Parse(Value);
			if (dt == "float") return float.Parse(Value);
			if (dt == "double") return double.Parse(Value);
			if (dt == "duration") return TimeSpan.Parse(Value); // syntax?
			if (dt == "dateTime" || dt == "time" || dt == "date") return DateTime.Parse(Value); // syntax?
			if (dt == "long") return long.Parse(Value);
			if (dt == "int") return int.Parse(Value);
			if (dt == "short") return short.Parse(Value);
			if (dt == "byte") return sbyte.Parse(Value);
			if (dt == "unsignedLong") return ulong.Parse(Value);
			if (dt == "unsignedInt") return uint.Parse(Value);
			if (dt == "unsignedShort") return ushort.Parse(Value);
			if (dt == "unsignedByte") return byte.Parse(Value);
			
			return Value;
		}
		
		public Literal Normalize() {
			if (DataType == null) return this;
			return new Literal(ParseValue().ToString(), Language, DataType);
		}
		
		public static Literal Create(bool value) {
			return new Literal(value ? "true" : "false", null, XMLSCHEMANS + "boolean");
		}
	}

	/*
	public abstract class LiteralFilter : Resource {
		public LiteralFilter() : base(null) { }
		
		public override string Uri { get { return null; } }
		
		public abstract bool Matches(Literal literal);
	}
	
	public interface SQLLiteralFilter {
		string GetSQLFunction();
	}
	
	public class LiteralNumericComparison : LiteralFilter, SQLLiteralFilter {
		double value;
		Op comparison;
		
		public LiteralNumericComparison(double value, Op comparison) {
			this.value = value; this.comparison = comparison;
		}
		
		public enum Op {
			Equal,
			NotEqual,
			GreaterThan,
			GreaterThanOrEqual,
			LessThan,
			LessThanOrEqual,
		}
		
		public override bool Matches(Literal literal) {
			double v;
			if (!double.TryParse(literal.Value, System.Globalization.NumberStyles.Any, null, out v)) return false;
			
			switch (comparison) {
				case Op.Equal: return v == value;
				case Op.NotEqual: return v != value;
				case Op.GreaterThan: return v > value;
				case Op.GreaterThanOrEqual: return v >= value;
				case Op.LessThan: return v < value;
				case Op.LessThanOrEqual: return v <= value;
				default: return false;
			}
		}
		
		public string GetSQLFunction() {
			switch (comparison) {
				case Op.Equal: return "literal = " + value;
				case Op.NotEqual: return "literal != " + value;
				case Op.GreaterThan: return "literal > " + value;
				case Op.GreaterThanOrEqual: return "literal >= " + value;
				case Op.LessThan: return "literal < " + value;
				case Op.LessThanOrEqual: return "literal <= " + value;
				default: return null;
			}
		}
	}
	
	public class LiteralStringComparison : LiteralFilter, SQLLiteralFilter {
		string value;
		Op comparison;
		
		public LiteralStringComparison(string value, Op comparison) {
			this.value = value; this.comparison = comparison;
		}
		
		public enum Op {
			Equal,
			NotEqual,
			GreaterThan,
			GreaterThanOrEqual,
			LessThan,
			LessThanOrEqual,
		}
		
		public override bool Matches(Literal literal) {
			string v = literal.Value;
			
			switch (comparison) {
				case Op.Equal: return v == value;
				case Op.NotEqual: return v != value;
				case Op.GreaterThan: return v.CompareTo(value) > 0;
				case Op.GreaterThanOrEqual: return v.CompareTo(value) >= 0;
				case Op.LessThan: return v.CompareTo(value) < 0;
				case Op.LessThanOrEqual: return v.CompareTo(value) <= 0;
				default: return false;
			}
		}
		
		public string GetSQLFunction() {
			switch (comparison) {
				case Op.Equal: return "literal = " + value;
				case Op.NotEqual: return "literal != " + value;
				case Op.GreaterThan: return "literal > " + value;
				case Op.GreaterThanOrEqual: return "literal >= " + value;
				case Op.LessThan: return "literal < " + value;
				case Op.LessThanOrEqual: return "literal <= " + value;
				default: return null;
			}
		}
	}
	*/
}

namespace SemWeb.Util.Bind {
	public class Any {
		Entity ent;
		Store model;
				
		public Any(Entity entity, Store model) {
			this.ent = entity;
			this.model = model;
		}
		
		public Entity Entity { get { return ent; } }
		public Store Model { get { return model; } }
		
		public string Uri { get { return ent.Uri; } }
		
		private Resource toRes(object value) {
			if (value == null) return null;
			if (value is Resource) return (Resource)value; // shouldn't happen
			if (value is string) return new Literal((string)value);
			if (value is Any) return ((Any)value).ent;
			throw new ArgumentException("value is not of a recognized type");
		}
		
		protected void AddValue(Entity predicate, object value, bool forward) {
			if (value == null) throw new ArgumentNullException("value");
			Resource v = toRes(value);
			if (!forward && !(v is Entity)) throw new ArgumentException("Cannot set this property to a literal value.");
			Statement add = new Statement(ent, predicate, v);
			if (!forward) add = add.Invert();
			model.Add(add);
		}
		protected void RemoveValue(Entity predicate, object value, bool forward) {
			if (value == null) throw new ArgumentNullException("value");
			Resource v = toRes(value);
			if (!forward && !(v is Entity)) throw new ArgumentException("Cannot set this property to a literal value.");
			Statement rem = new Statement(ent, predicate, v);
			if (!forward) rem = rem.Invert();
			model.Remove(rem);
		}
		
		protected void SetFuncProperty(Entity predicate, object value, bool forward) {
			Resource v = toRes(value);
			Statement search = new Statement(ent, predicate, null);
			Statement replace = new Statement(ent, predicate, v);
			if (!forward) {
				if (v != null && !(v is Entity)) throw new ArgumentException("Cannot set this property to a literal value.");
				search = search.Invert();
				replace = replace.Invert();
			}
			
			if (v != null) {
				foreach (Statement s in model.Select(search)) {
					model.Replace(s, replace);
					return;
				}
				model.Add(replace);
			} else {
				model.Remove(search);
			}
		}
		
		protected void SetNonFuncProperty(Entity predicate, object[] values, bool forward) {
			Statement search = new Statement(ent, predicate, null);
			if (!forward)
				search = search.Invert();
			
			model.Remove(search);
			if (values != null) {
				foreach (object value in values) {
					Resource v = toRes(value);
					if (v == null) throw new ArgumentNullException("element of values array");
					if (!forward && !(v is Entity)) throw new ArgumentException("Cannot set this property to a literal value.");
					Statement add = new Statement(ent, predicate, v);
					if (!forward) add = add.Invert();
					model.Add(add);
				}
			}
		}
	}
}
