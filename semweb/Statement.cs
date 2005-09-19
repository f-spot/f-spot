using System;

namespace SemWeb {
	public struct Statement {
		private Entity s;
		private Entity p;
		private Resource o;
		private Entity m;
		
		public static Entity DefaultMeta = new Entity(null);
		
		public Statement(Entity subject, Entity predicate, Resource @object)
		: this(subject, predicate, @object, DefaultMeta) {
		}
		
		public Statement(Entity subject, Entity predicate, Resource @object, Entity meta) {
		  s = subject;
		  p = predicate;
		  o = @object;
		  m = meta;
		}
		
		public Entity Subject { get { return s; } }
		public Entity Predicate { get { return p; } }
		public Resource Object { get { return o; } }
		
		public Entity Meta { get { return m; } }
		
		internal bool AnyNull {
			get {
				return Subject == null || Predicate == null || Object == null || Meta == null;
			}
		}
		
		public Statement Invert() {
			if (!(Object is Entity)) throw new ArgumentException("The object of the statement must be an entity.");
			return new Statement((Entity)Object, Predicate, Subject, Meta);
		}
		
		public bool Matches(Statement statement) {
			if (Subject != null && Subject != statement.Subject) return false;
			if (Predicate != null && Predicate != statement.Predicate) return false;
			if (Object != null && Object != statement.Object) return false;
			if (Meta != null && Meta != statement.Meta) return false;
			return true;
		}
		
		public override string ToString() {
			string ret = "";
			if (Subject != null) ret += "<" + Subject + "> "; else ret += "? ";
			if (Predicate != null) ret += "<" + Predicate + "> "; else ret += "? ";
			if (Object != null) {
				if (Object is Literal)
					ret += Object;
				else
					ret += "<" + Object + ">";
			} else {
				ret += "?";
			}
			if (Meta != null && Meta != DefaultMeta) ret += " meta=<" + Meta + ">";
			return ret + ".";
		}
		
		public override bool Equals(object other) {
			return (Statement)other == this;
		}
		
		public override int GetHashCode() {
			int ret = 0;
			if (s != null) ret = unchecked(ret + s.GetHashCode());
			if (p != null) ret = unchecked(ret + p.GetHashCode());
			if (o != null) ret = unchecked(ret + o.GetHashCode());
			if (m != null) ret = unchecked(ret + m.GetHashCode());
			return ret;
		}
		
		public static bool operator ==(Statement a, Statement b) {
			if ((a.Subject == null) != (b.Subject == null)) return false;
			if ((a.Predicate == null) != (b.Predicate == null)) return false;
			if ((a.Object == null) != (b.Object == null)) return false;
			if ((a.Meta == null) != (b.Meta == null)) return false;
			if (a.Subject != null && !a.Subject.Equals(b.Subject)) return false;
			if (a.Predicate != null && !a.Predicate.Equals(b.Predicate)) return false;
			if (a.Object != null && !a.Object.Equals(b.Object)) return false;
			if (a.Meta != null && !a.Meta.Equals(b.Meta)) return false;
			return true;
		}
		public static bool operator !=(Statement a, Statement b) {
			return !(a == b);
		}
	}
	
	public struct SelectPartialFilter {
		bool s, p, o, m;
		bool first;
		
		public static readonly SelectPartialFilter All = new SelectPartialFilter(true, true, true, true);
		
		public SelectPartialFilter(bool subject, bool predicate, bool @object, bool meta) {
			s = subject;
			p = predicate;
			o = @object;
			m = meta;
			
			first = false;
		}
		
		public bool Subject { get { return s; } }
		public bool Predicate { get { return p; } }
		public bool Object { get { return o; } }
		public bool Meta { get { return m; } }
		
		public bool SelectAll { get { return s && p && o && m; } }
		public bool SelectNone { get { return !s && !p && !o && !m; } }
		
		public bool SelectFirst { get { return first; } set { first = value; } }
		
		public override string ToString() {
			if (SelectAll) return "All";
			if (SelectNone) return "None";
			string ret = "";
			if (Subject) ret += "S";
			if (Predicate) ret += "P";
			if (Object) ret += "O";
			if (Meta) ret += "M";
			return ret;
		}
	}
}
