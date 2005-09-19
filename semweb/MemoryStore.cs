using System;
using System.Collections;

using SemWeb;

namespace SemWeb {
	public class MemoryStore : Store, IEnumerable {
		ArrayList statements = new ArrayList();
		
		Hashtable statementsAboutSubject = new Hashtable();
		Hashtable statementsAboutObject = new Hashtable();
		
		bool isIndexed = false;
		internal bool allowIndexing = true;
		
		public MemoryStore() {
		}
		
		public MemoryStore(StatementSource source) {
			Import(source);
		}

		public IList Statements { get { return ArrayList.ReadOnly(statements); } }
		  
		public override int StatementCount { get { return statements.Count; } }
		
		IEnumerator IEnumerable.GetEnumerator() {
			return statements.GetEnumerator();
		}
		
		public override void Clear() {
			statements.Clear();
			statementsAboutSubject.Clear();
			statementsAboutObject.Clear();
		}
		
		private ArrayList GetIndexArray(Hashtable from, Resource entity) {
			ArrayList ret = (ArrayList)from[entity];
			if (ret == null) {
				ret = new ArrayList();
				from[entity] = ret;
			}
			return ret;
		}
		
		public override void Add(Statement statement) {
			if (statement.AnyNull) throw new ArgumentNullException();
			statements.Add(statement);
			if (isIndexed) {
				GetIndexArray(statementsAboutSubject, statement.Subject).Add(statement);
				GetIndexArray(statementsAboutObject, statement.Object).Add(statement);
			}
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
		
		public override Entity[] GetAllEntities() {
			Hashtable h = new Hashtable();
			foreach (Statement s in Statements) {
				if (s.Subject != null) h[s.Subject] = h;
				if (s.Predicate != null) h[s.Predicate] = h;
				if (s.Object != null && s.Object is Entity) h[s.Object] = h;
				if (s.Meta != null && s.Meta != Statement.DefaultMeta) h[s.Meta] = h;
			}
			return (Entity[])new ArrayList(h.Keys).ToArray(typeof(Entity));
		}
		
		public override Entity[] GetAllPredicates() {
			Hashtable h = new Hashtable();
			foreach (Statement s in Statements)
				h[s.Predicate] = h;
			return (Entity[])new ArrayList(h.Keys).ToArray(typeof(Entity));
		}

		private void ShorterList(ref IList list1, IList list2) {
			if (list2.Count < list1.Count)
				list1 = list2;
		}
		
		public override void Select(Statement template, SelectPartialFilter partialFilter, StatementSink result) {
			IList source = statements;
			
			// The first time select is called, turn indexing on for the store.
			// TODO: Perform this index in a background thread if there are a lot
			// of statements.
			if (!isIndexed && allowIndexing) {
				isIndexed = true;
				foreach (Statement statement in statements) {
					GetIndexArray(statementsAboutSubject, statement.Subject).Add(statement);
					GetIndexArray(statementsAboutObject, statement.Object).Add(statement);
				}
			}
			
			if (template.Subject != null) ShorterList(ref source, GetIndexArray(statementsAboutSubject, template.Subject));
			else if (template.Object != null) ShorterList(ref source, GetIndexArray(statementsAboutObject, template.Object));
			
			if (source == null) return;
			
			foreach (Statement statement in source) {
				if (!template.Matches(statement))
					continue;
				if (!result.Add(statement)) return;
			}
		}

		public override void Select(Statement[] templates, SelectPartialFilter partialFilter, StatementSink result) {
			foreach (Statement t in templates)
				Select(t, result);
		}

		public override void Replace(Entity a, Entity b) {
			foreach (Statement statement in statements) {
				if ((statement.Subject != null && statement.Subject == a) || (statement.Predicate != null && statement.Predicate == a) || (statement.Object != null && statement.Object == a) || (statement.Meta != null && statement.Meta == a)) {
					Remove(statement);
					Add(new Statement(
						statement.Subject == a ? b : a,
						statement.Predicate == a ? b : a,
						statement.Object == a ? b : a,
						statement.Meta == a ? b : a
						));
				}
			}
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
		
		public override Entity[] FindEntities(Statement[] filters) {
			ArrayList ents = new ArrayList();
			foreach (Statement s in Select(filters[0])) {
				if (s.Subject != null && filters[0].Subject == null)
					ents.Add(s.Subject);
				if (s.Predicate != null && filters[0].Predicate == null)
					ents.Add(s.Predicate);
				if (s.Object != null && filters[0].Object == null && s.Object is Entity)
					ents.Add(s.Object);
				if (s.Meta != null && filters[0].Meta == null)
					ents.Add(s.Meta);
			}
			
			foreach (Statement f in filters) {
				if (f == filters[0]) continue;
				
				ArrayList e2 = new ArrayList();
				foreach (Entity e in ents) {
					Statement fe = new Statement(
						f.Subject == null ? e : f.Subject,
						f.Predicate == null ? e : f.Predicate,
						f.Object == null ? e : f.Object,
						f.Meta == null ? e : f.Meta);
					if (Contains(fe))
						e2.Add(e);
				}
				
				ents = e2;
			}
			
			return (Entity[])ents.ToArray(typeof(Entity));
		}

	}
}
