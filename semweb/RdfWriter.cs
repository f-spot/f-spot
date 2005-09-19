using System;
using System.Collections;
using System.IO;

namespace SemWeb {
	public abstract class RdfWriter : IDisposable, StatementSink {
		string baseuri;
		bool closed;
		
		public abstract NamespaceManager Namespaces { get; }
		
		public string BaseUri {
			get {
				return baseuri;
			}
			set {
				baseuri = value;
			}
		}

		protected object GetResourceKey(Resource resource) {
			return resource.GetResourceKey(this);
		}

		protected void SetResourceKey(Resource resource, object value) {
			resource.SetResourceKey(this, value);
		}
		
		internal static TextWriter GetWriter(string dest) {
			if (dest == "-")
				return Console.Out;
			return new StreamWriter(dest);
		}
		
		bool StatementSink.Add(Statement statement) {
			Add(statement);
			return true;
		}
		
		public void Add(Statement statement) {
			if (statement.AnyNull)
				throw new ArgumentNullException();

			string s = getUri(statement.Subject);
			string p = getUri(statement.Predicate);
			
			if (statement.Object is Literal) {
				Literal lit = (Literal)statement.Object;
				WriteStatement(s, p, lit);
			} else {
				string o = getUri((Entity)statement.Object);
				WriteStatement(s, p, o);
			}
		}
		
		private string getUri(Entity e) {
			if (e.Uri != null) return e.Uri;
			string uri = (string)GetResourceKey(e);
			if (uri != null) return uri;
			uri = CreateAnonymousEntity();
			SetResourceKey(e, uri);
			return uri;
		}
		
		public abstract void WriteStatement(string subj, string pred, string obj);
		
		public abstract void WriteStatement(string subj, string pred, Literal literal);
		
		public abstract string CreateAnonymousEntity();
		
		public virtual void Close() {
			if (closed) return;
			closed = true;
		}
		
		void IDisposable.Dispose() {
			Close();
		}
	}
}
