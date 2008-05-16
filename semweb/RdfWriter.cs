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
		
		public abstract void Add(Statement statement);

		public virtual void Close() {
			if (closed) return;
			closed = true;
		}
		
		public virtual void Write(StatementSource source) {
			source.Select(this);
		}
		
		void IDisposable.Dispose() {
			Close();
		}
	}
}
