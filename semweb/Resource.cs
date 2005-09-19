using System;
using System.Collections;

namespace SemWeb {
	
	public abstract class Resource {
		internal ArrayList extraKeys;
		
		internal class ExtraKey {
			public object Key;
			public object Value; 
		}
		
		public abstract string Uri { get; }
		
		internal Resource() {
		}
		
		public override string ToString() {
			if (Uri != null) return Uri;
			return "_";
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
			if (extraKeys == null) return null;
			for (int i = 0; i < extraKeys.Count; i++) {
				Resource.ExtraKey ekey = (Resource.ExtraKey)extraKeys[i];
				if (ekey.Key == key)
					return ekey.Value;
			}
			return null;
		}
		internal void SetResourceKey(object key, object value) {
			if (extraKeys == null) extraKeys = new ArrayList();
			
			foreach (Resource.ExtraKey ekey in extraKeys)
				if (ekey.Key == key) { extraKeys.Remove(ekey); break; }
			
			Resource.ExtraKey k = new Resource.ExtraKey();
			k.Key = key;
			k.Value = value;
			
			extraKeys.Add(k);
		}
		
	}
	
	public sealed class Entity : Resource {
		private string uri;
		int cachedHashCode = -1;
		
		public Entity(string uri) { if (uri != null) this.uri = string.Intern(uri); }
		
		public override string Uri {
			get {
				return uri;
			}
		}
		
		public static implicit operator Entity(string uri) { return new Entity(uri); }
		
		public override int GetHashCode() {
			if (cachedHashCode != -1) return cachedHashCode;
			
			if (Uri != null) {
				cachedHashCode = Uri.GetHashCode();
			} else if (extraKeys != null && extraKeys.Count == 1) {
				ExtraKey v = (ExtraKey)extraKeys[0];
				cachedHashCode = unchecked(v.Key.GetHashCode() + v.Value.GetHashCode());
			}
			
			// If there's no Uri or ExtraKeys info, then this
			// object is only equal to itself.  It's then safe
			// to use object.GetHashCode().
			if (cachedHashCode == -1) cachedHashCode = base.GetHashCode();
			
			return cachedHashCode;
		}
			
		public override bool Equals(object other) {
			if (object.ReferenceEquals(this, other)) return true;
			if (!(other is Entity)) return false;
			
			// If anonymous, then we have to compare extraKeys.
			if ((Uri == null && ((Resource)other).Uri == null)) {
				ArrayList otherkeys = ((Resource)other).extraKeys;
				if (otherkeys != null && extraKeys != null) {
					for (int vi1 = 0; vi1 < extraKeys.Count; vi1++) {
						ExtraKey v1 = (ExtraKey)extraKeys[vi1];
						for (int vi2 = 0; vi2 < otherkeys.Count; vi2++) {
							ExtraKey v2 = (ExtraKey)otherkeys[vi2];
							if (v1.Key == v2.Key)
								return v1.Value.Equals(v2.Value);
						}
					}
				}
				
				return false;
			}				
			
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
	}
	
	public sealed class Literal : Resource { 
		private string value, lang, type;
		
		public Literal(string value) : this(value, null, null) {
		}
		
		public Literal(string value, string language, string dataType) {
		  if (value == null)
			  throw new ArgumentNullException("value");
		  this.value = string.Intern(value);
		  this.lang = language;
		  this.type = dataType;
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

namespace SemWeb.Bind {
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
