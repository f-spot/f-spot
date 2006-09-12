using System;
using System.Collections;

using SemWeb;
using SemWeb.Stores;
using SemWeb.Util;

namespace SemWeb {
	public class MemoryStore : Store, SupportsPersistableBNodes, IEnumerable {
		StatementList statements;
		
		Hashtable statementsAboutSubject = new Hashtable();
		Hashtable statementsAboutObject = new Hashtable();
		
		bool isIndexed = false;
		internal bool allowIndexing = true;
		internal bool checkForDuplicates = false;
		bool distinct = true;
		
		string guid = null;
		Hashtable pbnodeToId = null;
		Hashtable pbnodeFromId = null;
		
		public MemoryStore() {
			statements = new StatementList();
		}
		
		public MemoryStore(StatementSource source) : this() {
			Import(source);
		}
		
		public MemoryStore(Statement[] statements) {
			this.statements = new StatementList(statements);
		}

		public Statement[] ToArray() {
			return (Statement[])statements.ToArray(typeof(Statement));
		}

		public IList Statements { get { return statements.ToArray(); } }
		
		public override bool Distinct { get { return distinct; } }
		
		public override int StatementCount { get { return statements.Count; } }
		
		public Statement this[int index] {
			get {
				return statements[index];
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator() {
			return statements.GetEnumerator();
		}
		
		public override void Clear() {
			statements.Clear();
			statementsAboutSubject.Clear();
			statementsAboutObject.Clear();
			distinct = true;
		}
		
		private StatementList GetIndexArray(Hashtable from, Resource entity) {
			StatementList ret = (StatementList)from[entity];
			if (ret == null) {
				ret = new StatementList();
				from[entity] = ret;
			}
			return ret;
		}
		
		public override void Add(Statement statement) {
			if (statement.AnyNull) throw new ArgumentNullException();
			if (checkForDuplicates && Contains(statement)) return;
			statements.Add(statement);
			if (isIndexed) {
				GetIndexArray(statementsAboutSubject, statement.Subject).Add(statement);
				GetIndexArray(statementsAboutObject, statement.Object).Add(statement);
			}
			if (!checkForDuplicates) distinct = false;
		}
		
		public override void Import(StatementSource source) {
			bool newDistinct = checkForDuplicates || ((StatementCount==0) && source.Distinct);
			base.Import(source); // distinct set to false if !checkForDuplicates
			distinct = newDistinct;
		}
		
		public override void Remove(Statement statement) {
			if (statement.AnyNull) {
				for (int i = 0; i < statements.Count; i++) {
					Statement s = (Statement)statements[i];
					if (statement.Matches(s)) {
						statements.RemoveAt(i); i--;
						if (isIndexed) {
							GetIndexArray(statementsAboutSubject, s.Subject).Remove(s);
							GetIndexArray(statementsAboutObject, s.Object).Remove(s);
						}
					}
				}
			} else {
				statements.Remove(statement);
				if (isIndexed) {
					GetIndexArray(statementsAboutSubject, statement.Subject).Remove(statement);
					GetIndexArray(statementsAboutObject, statement.Object).Remove(statement);
				}
			}
		}
		
		public override Entity[] GetEntities() {
			Hashtable h = new Hashtable();
			foreach (Statement s in Statements) {
				if (s.Subject != null) h[s.Subject] = h;
				if (s.Predicate != null) h[s.Predicate] = h;
				if (s.Object != null && s.Object is Entity) h[s.Object] = h;
				if (s.Meta != null && s.Meta != Statement.DefaultMeta) h[s.Meta] = h;
			}
			return (Entity[])new ArrayList(h.Keys).ToArray(typeof(Entity));
		}
		
		public override Entity[] GetPredicates() {
			Hashtable h = new Hashtable();
			foreach (Statement s in Statements)
				h[s.Predicate] = h;
			return (Entity[])new ArrayList(h.Keys).ToArray(typeof(Entity));
		}

		public override Entity[] GetMetas() {
			Hashtable h = new Hashtable();
			foreach (Statement s in Statements)
				h[s.Meta] = h;
			return (Entity[])new ArrayList(h.Keys).ToArray(typeof(Entity));
		}

		private void ShorterList(ref StatementList list1, StatementList list2) {
			if (list2.Count < list1.Count)
				list1 = list2;
		}
		
		public override void Select(Statement template, StatementSink result) {
			StatementList source = statements;
			
			// The first time select is called, turn indexing on for the store.
			// TODO: Perform this index in a background thread if there are a lot
			// of statements.
			if (!isIndexed && allowIndexing) {
				isIndexed = true;
				for (int i = 0; i < StatementCount; i++) {
					Statement statement = this[i];
					GetIndexArray(statementsAboutSubject, statement.Subject).Add(statement);
					GetIndexArray(statementsAboutObject, statement.Object).Add(statement);
				}
			}
			
			if (template.Subject != null) ShorterList(ref source, GetIndexArray(statementsAboutSubject, template.Subject));
			else if (template.Object != null) ShorterList(ref source, GetIndexArray(statementsAboutObject, template.Object));
			
			if (source == null) return;
			
			for (int i = 0; i < source.Count; i++) {
				Statement statement = source[i];
				if (!template.Matches(statement))
					continue;
				if (!result.Add(statement)) return;
			}
		}

		public override void Select(SelectFilter filter, StatementSink result) {
			ResSet
				s = filter.Subjects == null ? null : new ResSet(filter.Subjects),
				p = filter.Predicates == null ? null : new ResSet(filter.Predicates),
				o = filter.Objects == null ? null : new ResSet(filter.Objects),
				m = filter.Metas == null ? null : new ResSet(filter.Metas);
				
			foreach (Statement st in statements) {
				if (s != null && !s.Contains(st.Subject)) continue;
				if (p != null && !p.Contains(st.Predicate)) continue;
				if (o != null && !o.Contains(st.Object)) continue;
				if (m != null && !m.Contains(st.Meta)) continue;
				if (filter.LiteralFilters != null && !LiteralFilter.MatchesFilters(st.Object, filter.LiteralFilters, this)) continue;
				if (!result.Add(st)) return;
			}
		}

		public override void Replace(Entity a, Entity b) {
			MemoryStore removals = new MemoryStore();
			MemoryStore additions = new MemoryStore();
			foreach (Statement statement in statements) {
				if ((statement.Subject != null && statement.Subject == a) || (statement.Predicate != null && statement.Predicate == a) || (statement.Object != null && statement.Object == a) || (statement.Meta != null && statement.Meta == a)) {
					removals.Add(statement);
					additions.Add(statement.Replace(a, b));
				}
			}
			RemoveAll(removals.ToArray());
			Import(additions);
		}
		
		public override void Replace(Statement find, Statement replacement) {
			if (find.AnyNull) throw new ArgumentNullException("find");
			if (replacement.AnyNull) throw new ArgumentNullException("replacement");
			if (find == replacement) return;
			
			foreach (Statement match in Select(find)) {
				Remove(match);
				Add(replacement);
				break; // should match just one statement anyway
			}
		}

		string SupportsPersistableBNodes.GetStoreGuid() {
			if (guid == null) guid = Guid.NewGuid().ToString("N");;
			return guid;
		}
		
		string SupportsPersistableBNodes.GetNodeId(BNode node) {
			if (pbnodeToId == null) {
				pbnodeToId = new Hashtable();
				pbnodeFromId = new Hashtable();
			}
			if (pbnodeToId.ContainsKey(node)) return (string)pbnodeToId[node];
			string id = pbnodeToId.Count.ToString();
			pbnodeToId[node] = id;
			pbnodeFromId[id] = node;
			return id;
		}
		
		BNode SupportsPersistableBNodes.GetNodeFromId(string persistentId) {
			if (pbnodeFromId == null) return null;
			return (BNode)pbnodeFromId[persistentId];
		}
	}
}
