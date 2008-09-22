using System;
using System.Collections;
using System.Data;

using SemWeb.Util;

namespace SemWeb {
	
	public interface StatementSource {
		bool Distinct { get; }
		void Select(StatementSink sink);
	}
	
	public interface SelectableSource : StatementSource {
		bool Contains(Statement template);
		void Select(Statement template, StatementSink sink);
		void Select(SelectFilter filter, StatementSink sink);
	}

	public interface QueryableSource : SelectableSource {
		void Query(Statement[] graph, SemWeb.Query.QueryResultSink sink);
		void Query(SelectFilter[] graph, SemWeb.Query.QueryResultSink sink);
	}
	
	public interface StatementSink {
		bool Add(Statement statement);
	}

	public interface ModifiableSource : StatementSink {
		void Clear();
		void Import(StatementSource source);
		void Remove(Statement template);
		void RemoveAll(Statement[] templates);
		void Replace(Entity find, Entity replacement);
		void Replace(Statement find, Statement replacement);
	}
	
	internal class StatementCounterSink : StatementSink {
		int counter = 0;
		
		public int StatementCount { get { return counter; } }
		
		public bool Add(Statement statement) {
			counter++;
			return true;
		}
	}

	internal class StatementExistsSink : StatementSink {
		bool exists = false;
		
		public bool Exists { get { return exists; } }
		
		public bool Add(Statement statement) {
			exists = true;
			return false;
		}
	}

	public abstract class Store : StatementSource, StatementSink,
		SelectableSource, ModifiableSource,
		IDisposable {
		
		Entity rdfType;
		
		public static StatementSource CreateForInput(string spec) {
			if (spec.StartsWith("rdfs+")) {
				StatementSource s = CreateForInput(spec.Substring(5));
				if (!(s is SelectableSource)) s = new MemoryStore(s);
				return new SemWeb.Inference.RDFS(s, (SelectableSource)s);
			}
			return (StatementSource)Create(spec, false);
		}		
		
		public static StatementSink CreateForOutput(string spec) {
			return (StatementSink)Create(spec, true);
		}
		
		private static object Create(string spec, bool output) {
			string[] multispecs = spec.Split('\n', '|');
			if (multispecs.Length > 1) {
				SemWeb.Stores.MultiStore multistore = new SemWeb.Stores.MultiStore();
				foreach (string mspec in multispecs) {
					object mstore = Create(mspec.Trim(), output);
					if (mstore is SelectableSource) {
						multistore.Add((SelectableSource)mstore);
					} else if (mstore is StatementSource) {
						MemoryStore m = new MemoryStore((StatementSource)mstore);
						multistore.Add(m);
					}
				}
				return multistore;
			}
		
			string type = spec;
			
			int c = spec.IndexOf(':');
			if (c != -1) {
				type = spec.Substring(0, c);
				spec = spec.Substring(c+1);
			} else {
				spec = "";
			}
			
			Type ttype;
			
			switch (type) {
				case "mem":
					return new MemoryStore();
				case "xml":
					if (spec == "") throw new ArgumentException("Use: xml:filename");
					if (output) {
						return new RdfXmlWriter(spec);
					} else {
						return new RdfXmlReader(spec);
					}
				case "n3":
				case "ntriples":
				case "nt":
				case "turtle":
					if (spec == "") throw new ArgumentException("Use: format:filename");
					if (output) {
						N3Writer ret = new N3Writer(spec); // turtle is default format
						switch (type) {
							case "nt": case "ntriples":
								ret.Format = N3Writer.Formats.NTriples;
								break;
						}
						return ret;
					} else {
						return new N3Reader(spec);
					}
				/*case "file":
					if (spec == "") throw new ArgumentException("Use: format:filename");
					if (output) throw new ArgumentException("The FileStore does not support writing.");
					return new SemWeb.Stores.FileStore(spec);*/
				case "sqlite":
				case "mysql":
				case "postgresql":
					if (spec == "") throw new ArgumentException("Use: sqlite|mysql|postgresql:table:connection-string");
				
					c = spec.IndexOf(':');
					if (c == -1) throw new ArgumentException("Invalid format for SQL spec parameter (table:constring).");
					string table = spec.Substring(0, c);
					spec = spec.Substring(c+1);
					
					string classtype = null;
					if (type == "sqlite") {
						classtype = "SemWeb.Stores.SqliteStore, SemWeb.SqliteStore";
						spec = spec.Replace(";", ",");
					} else if (type == "mysql") {
						classtype = "SemWeb.Stores.MySQLStore, SemWeb.MySQLStore";
					} else if (type == "postgresql") {
						classtype = "SemWeb.Stores.PostgreSQLStore, SemWeb.PostgreSQLStore";
					}
					ttype = Type.GetType(classtype);
					if (ttype == null)
						throw new NotSupportedException("The storage type in <" + classtype + "> could not be found.");
					return Activator.CreateInstance(ttype, new object[] { spec, table });
				/*case "bdb":
					return new SemWeb.Stores.BDBStore(spec);*/
				case "sparql-http":
					return new SemWeb.Remote.SparqlHttpSource(spec);
				case "class":
					ttype = Type.GetType(spec);
					if (ttype == null)
						throw new NotSupportedException("The class <" + spec + "> could not be found.");
					return Activator.CreateInstance(ttype);
				default:
					throw new ArgumentException("Unknown parser type: " + type);
			}
		}
		
		protected Store() {
			rdfType = new Entity("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
		}
		
		void IDisposable.Dispose() {
			Close();
		}
		
		public virtual void Close() {
		}
		
		public abstract bool Distinct { get; }
		
		public abstract int StatementCount { get; }

		public abstract void Clear();

		public Entity[] GetEntitiesOfType(Entity type) {
			ArrayList entities = new ArrayList();
			
			IEnumerable result = Select(new Statement(null, rdfType, type));
			foreach (Statement s in result) {
				entities.Add(s.Subject);
			}
			
			return (Entity[])entities.ToArray(typeof(Entity));
		}
		
		bool StatementSink.Add(Statement statement) {
			Add(statement);
			return true;
		}
		
		public abstract void Add(Statement statement);
		
		public abstract void Remove(Statement statement);

		public virtual void Import(StatementSource source) {
			source.Select(this);
		}
		
		public void RemoveAll(Statement[] templates) {
			foreach (Statement t in templates)
				Remove(t);
		}
		
		public abstract Entity[] GetEntities();
		
		public abstract Entity[] GetPredicates();
		
		public abstract Entity[] GetMetas();
		
		public virtual bool Contains(Statement template) {
			return DefaultContains(this, template);
		}

		public static bool DefaultContains(SelectableSource source, Statement template) {
			StatementExistsSink sink = new StatementExistsSink();
			SelectFilter filter = new SelectFilter(template);
			filter.Limit = 1;
			source.Select(filter, sink);
			return sink.Exists;
		}
		
		public static void DefaultSelect(SelectableSource source, SelectFilter filter, StatementSink sink) {
			// This method should be avoided...
			if (filter.LiteralFilters != null)
				sink = new SemWeb.Filters.FilterSink(filter.LiteralFilters, sink, source);
			foreach (Entity subject in filter.Subjects == null ? new Entity[] { null } : filter.Subjects)
			foreach (Entity predicate in filter.Predicates == null ? new Entity[] { null } : filter.Predicates)
			foreach (Resource objct in filter.Objects == null ? new Resource[] { null } : filter.Objects)
			foreach (Entity meta in filter.Metas == null ? new Entity[] { null } : filter.Metas)
				source.Select(new Statement(subject, predicate, objct, meta), sink);
		}		
		
		public void Select(StatementSink result) {
			Select(Statement.All, result);
		}
		
		public abstract void Select(Statement template, StatementSink result);
		
		public abstract void Select(SelectFilter filter, StatementSink result);
		
		public SelectResult Select(Statement template) {
			return new SelectResult.Single(this, template);
		}
		
		public SelectResult Select(SelectFilter filter) {
			return new SelectResult.Multi(this, filter);
		}
		
		public Resource[] SelectObjects(Entity subject, Entity predicate) {
			Hashtable resources = new Hashtable();
			ResourceCollector collector = new ResourceCollector();
			collector.SPO = 2;
			collector.Table = resources;
			Select(new Statement(subject, predicate, null, null), collector);
			return (Resource[])new ArrayList(resources.Keys).ToArray(typeof(Resource));
		}
		public Entity[] SelectSubjects(Entity predicate, Resource @object) {
			Hashtable resources = new Hashtable();
			ResourceCollector collector = new ResourceCollector();
			collector.SPO = 0;
			collector.Table = resources;
			Select(new Statement(null, predicate, @object, null), collector);
			return (Entity[])new ArrayList(resources.Keys).ToArray(typeof(Entity));
		}
		class ResourceCollector : StatementSink {
			public Hashtable Table;
			public int SPO;
			public bool Add(Statement s) {
				if (SPO == 0) Table[s.Subject] = Table;
				if (SPO == 2) Table[s.Object] = Table;
				return true;
			}
		}
		
		public virtual void Replace(Entity find, Entity replacement) {
			MemoryStore deletions = new MemoryStore();
			MemoryStore additions = new MemoryStore();
			
			Select(new Statement(find, null, null, null), deletions);
			Select(new Statement(null, find, null, null), deletions);
			Select(new Statement(null, null, find, null), deletions);
			Select(new Statement(null, null, null, find), deletions);
			
			foreach (Statement s in deletions) {
				Remove(s);
				additions.Add(s.Replace(find, replacement));
			}
			
			foreach (Statement s in additions) {
				Add(s);
			}
		}
		
		public virtual void Replace(Statement find, Statement replacement) {
			Remove(find);
			Add(replacement);
		}
				public void Write(System.IO.TextWriter writer) {
			using (RdfWriter w = new N3Writer(writer)) {
				Select(w);
			}
		}
		
		protected object GetResourceKey(Resource resource) {
			return resource.GetResourceKey(this);
		}

		protected void SetResourceKey(Resource resource, object value) {
			resource.SetResourceKey(this, value);
		}
		
	}

	public abstract class SelectResult : StatementSource, IEnumerable {
		internal Store source;
		MemoryStore ms;
		internal SelectResult(Store source) { this.source = source; }
		public bool Distinct { get { return source.Distinct; } }
		public abstract void Select(StatementSink sink);
		public IEnumerator GetEnumerator() {
			return Buffer().Statements.GetEnumerator();
		}
		public long StatementCount { get { return Buffer().StatementCount; } }
		public MemoryStore Load() { return Buffer(); }
		public Statement[] ToArray() { return Load().ToArray(); }
		private MemoryStore Buffer() {
			if (ms != null) return ms;
			ms = new MemoryStore();
			ms.allowIndexing = false;
			Select(ms);
			return ms;
		}
		
		internal class Single : SelectResult {
			Statement template;
			public Single(Store source, Statement template) : base(source) {
				this.template = template;
			}
			public override void Select(StatementSink sink) {
				source.Select(template, sink);
			}
		}
		
		internal class Multi : SelectResult {
			SelectFilter filter;
			public Multi(Store source, SelectFilter filter)
				: base(source) {
				this.filter = filter;
			}
			public override void Select(StatementSink sink) {
				source.Select(filter, sink);
			}
		}
	}
}

namespace SemWeb.Stores {

	public interface SupportsPersistableBNodes {
		string GetStoreGuid();
		string GetNodeId(BNode node);
		BNode GetNodeFromId(string persistentId);
	}

	public class MultiStore : Store {
		ArrayList stores = new ArrayList();
		Hashtable namedgraphs = new Hashtable();
		ArrayList allsources = new ArrayList();
		
		public MultiStore() { }
		
		public override bool Distinct { get { return false; } }
		
		public void Add(SelectableSource store) {
			stores.Add(store);
			allsources.Add(store);
		}
		
		public void Add(string uri, SelectableSource store) {
			namedgraphs[uri] = store;
			allsources.Add(store);
		}
		
		public void Add(RdfReader source) {
			MemoryStore store = new MemoryStore(source);
			Add(store);
		}
		
		public void Add(string uri, RdfReader source) {
			MemoryStore store = new MemoryStore(source);
			Add(uri, store);
		}
		
		public void Remove(SelectableSource store) {
			stores.Remove(store);
			allsources.Remove(store);
		}
		
		public void Remove(string uri) {
			allsources.Remove(namedgraphs[uri]);
			namedgraphs.Remove(uri);
		}
		
		public override int StatementCount {
			get {
				int ret = 0;
				foreach (StatementSource s in allsources) {
					if (s is Store)
						ret += ((Store)s).StatementCount;
					else
						throw new InvalidOperationException("Not all sources are stores supporting StatementCount.");
				}
				return ret;
			}
		}

		public override void Clear() {
			throw new InvalidOperationException("Clear is not a valid operation on a MultiStore.");
		}
		
		public override Entity[] GetEntities() {
			Hashtable h = new Hashtable();
			foreach (StatementSource s in allsources) {
				if (s is Store) {
					foreach (Resource r in ((Store)s).GetEntities())
						h[r] = h;
				} else {
					throw new InvalidOperationException("Not all sources are stores supporting GetEntities.");
				}
			}
			return (Entity[])new ArrayList(h.Keys).ToArray(typeof(Entity));
		}
		
		public override Entity[] GetPredicates() {
			Hashtable h = new Hashtable();
			foreach (StatementSource s in allsources) {
				if (s is Store) {
					foreach (Resource r in ((Store)s).GetPredicates())
						h[r] = h;
				} else {
					throw new InvalidOperationException("Not all sources are stores supporting GetEntities.");
				}
			}
			return (Entity[])new ArrayList(h.Keys).ToArray(typeof(Entity));
		}

		public override Entity[] GetMetas() {
			Hashtable h = new Hashtable();
			foreach (StatementSource s in allsources) {
				if (s is Store) {
					foreach (Resource r in ((Store)s).GetMetas())
						h[r] = h;
				} else {
					throw new InvalidOperationException("Not all sources are stores supporting GetEntities.");
				}
			}
			return (Entity[])new ArrayList(h.Keys).ToArray(typeof(Entity));
		}

		public override void Add(Statement statement) { throw new InvalidOperationException("Add is not a valid operation on a MultiStore."); }
		
		SelectableSource[] GetSources(Entity graph) {
			if (graph == null || namedgraphs.Count == 0)
				return (SelectableSource[])allsources.ToArray(typeof(SelectableSource));
			else if (graph == Statement.DefaultMeta)
				return (SelectableSource[])stores.ToArray(typeof(SelectableSource));
			else if (graph.Uri != null && namedgraphs.ContainsKey(graph.Uri))
				return new SelectableSource[] { (SelectableSource)namedgraphs[graph.Uri] };
			else
				return null;
		}
		
		public override bool Contains(Statement statement) {
			SelectableSource[] sources = GetSources(statement.Meta);
			if (sources == null) return false;
			foreach (SelectableSource s in sources)
				if (s.Contains(statement))
					return true;
			return false;
		}
			
		public override void Remove(Statement statement) { throw new InvalidOperationException("Remove is not a valid operation on a MultiStore."); }
		
		public override void Select(Statement template, StatementSink result) {
			SelectableSource[] sources = GetSources(template.Meta);
			if (sources == null) return;
			template.Meta = null;
			foreach (SelectableSource s in sources)
				s.Select(template, result);
		}
		
		public override void Select(SelectFilter filter, StatementSink result) {
			Entity[] scanMetas = filter.Metas;
			filter.Metas = null;
			if (scanMetas == null || namedgraphs.Count == 0) scanMetas = new Entity[] { null };
			foreach (Entity meta in scanMetas) {
				SelectableSource[] sources = GetSources(meta);
				if (sources == null) continue;
				foreach (SelectableSource s in sources)
					s.Select(filter, result);
			}
		}

		public override void Replace(Entity a, Entity b) { throw new InvalidOperationException("Replace is not a valid operation on a MultiStore."); }
		
		public override void Replace(Statement find, Statement replacement) { throw new InvalidOperationException("Replace is not a valid operation on a MultiStore."); }
		
	}
	
	public abstract class SimpleSourceWrapper : SelectableSource {
	
		public virtual bool Distinct { get { return true; } }
		
		public virtual void Select(StatementSink sink) {
			// The default implementation does not return
			// anything for this call.
		}

		public virtual bool Contains(Statement template) {
			template.Object = null; // reduce to another case (else there would be recursion)
			return Store.DefaultContains(this, template);
		}
		
		protected virtual void SelectAllSubject(Entity subject, StatementSink sink) {
		}

		protected virtual void SelectAllObject(Resource @object, StatementSink sink) {
		}

		protected virtual void SelectRelationsBetween(Entity subject, Resource @object, StatementSink sink) {
		}
		
		protected virtual void SelectAllPairs(Entity predicate, StatementSink sink) {
		}

		protected virtual void SelectSubjects(Entity predicate, Resource @object, StatementSink sink) {
		}

		protected virtual void SelectObjects(Entity subject, Entity predicate, StatementSink sink) {
		}

		public void Select(Statement template, StatementSink sink) {
			if (template.Meta != null && template.Meta != Statement.DefaultMeta) return;
			if (template.Predicate != null && template.Predicate.Uri == null) return;
			
			if (template.Subject == null && template.Predicate == null && template.Object == null) {
				Select(sink);
			} else if (template.Subject != null && template.Predicate != null && template.Object != null) {
				template.Meta = Statement.DefaultMeta;
				if (Contains(template))
					sink.Add(template);
			} else if (template.Predicate == null) {
				if (template.Subject == null)
					SelectAllObject(template.Object, sink); 
				else if (template.Object == null)
					SelectAllSubject(template.Subject, sink); 
				else
					SelectRelationsBetween(template.Subject, template.Object, sink);
			} else if (template.Subject != null && template.Object == null) {
				SelectObjects(template.Subject, template.Predicate, sink);
			} else if (template.Subject == null && template.Object != null) {
				SelectSubjects(template.Predicate, template.Object, sink);
			} else if (template.Subject == null && template.Object == null) {
				SelectAllPairs(template.Predicate, sink);
			}
		}
	
		public void Select(SelectFilter filter, StatementSink sink) {
			Store.DefaultSelect(this, filter, sink);
		}
	}
	
	public class DebuggedSource : SelectableSource {
		SelectableSource source;
		System.IO.TextWriter output;
		
		public DebuggedSource(SelectableSource source, System.IO.TextWriter output) {
			this.source = source;
			this.output = output;
		}
		
		public bool Distinct { get { return source.Distinct; } }
		
		public void Select(StatementSink sink) {
			Select(Statement.All, sink);
		}

		public bool Contains(Statement template) {
			output.WriteLine("CONTAINS: " + template);
			return source.Contains(template);
		}
		
		public void Select(Statement template, StatementSink sink) {
			output.WriteLine("SELECT: " + template);
			source.Select(template, sink);
		}
	
		public void Select(SelectFilter filter, StatementSink sink) {
			output.WriteLine("SELECT: " + filter);
			source.Select(filter, sink);
		}
	}
	
	public class CachedSource : SelectableSource {
		SelectableSource source;

		StatementMap containsresults = new StatementMap();
		StatementMap selectresults = new StatementMap();
		Hashtable selfilterresults = new Hashtable();
	
		public CachedSource(SelectableSource s) { source = s; }
	
		public bool Distinct { get { return source.Distinct; } }
		
		public void Select(StatementSink sink) {
			Select(Statement.All, sink);
		}

		public bool Contains(Statement template) {
			if (!containsresults.ContainsKey(template))
				containsresults[template] = source.Contains(template);
			return (bool)containsresults[template];
		}
		
		public void Select(Statement template, StatementSink sink) {
			if (!selectresults.ContainsKey(template)) {
				MemoryStore s = new MemoryStore();
				source.Select(template, s);
				selectresults[template] = s;
			}
			((MemoryStore)selectresults[template]).Select(sink);
		}
	
		public void Select(SelectFilter filter, StatementSink sink) {
			if (!selfilterresults.ContainsKey(filter)) {
				MemoryStore s = new MemoryStore();
				source.Select(filter, s);
				selfilterresults[filter] = s;
			}
			((MemoryStore)selfilterresults[filter]).Select(sink);
		}

	}
}
