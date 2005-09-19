using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Text;

using SemWeb.Util;

namespace SemWeb.Stores {
	// TODO: It's not safe to have two concurrent accesses to the same database
	// because the creation of new entities will use the same IDs.
	
	public abstract class SQLStore : Store {
		string table;
		
		bool firstUse = true;
		IDictionary lockedIdCache = null;
		int cachedNextId = -1;
		
		Hashtable literalCache = new Hashtable();
		int literalCacheSize = 0;

		bool Debug = false;
		
		StringBuilder cmdBuffer = new StringBuilder();
		
		// Buffer statements to process together.
		ArrayList addStatementBuffer = null;
		
		string 	INSERT_INTO_LITERALS_VALUES,
				INSERT_INTO_STATEMENTS_VALUES,
				INSERT_INTO_ENTITIES_VALUES;
				
		private class ResourceKey {
			public int ResId;
			
			public ResourceKey(int id) { ResId = id; }
			
			public override int GetHashCode() { return ResId; }
			public override bool Equals(object other) { return (other is ResourceKey) && ((ResourceKey)other).ResId == ResId; }
		}
		
		private static readonly string[] fourcols = new string[] { "subject", "predicate", "object", "meta" };
		private static readonly string[] predcol = new string[] { "predicate" };

		protected SQLStore(string table) {
			this.table = table;
			
			INSERT_INTO_LITERALS_VALUES = "INSERT INTO " + table + "_literals VALUES ";
			INSERT_INTO_STATEMENTS_VALUES = "INSERT INTO " + table + "_statements VALUES ";
			INSERT_INTO_ENTITIES_VALUES = "INSERT INTO " + table + "_entities VALUES ";
		}
		
		protected string TableName { get { return table; } }
		
		protected abstract bool SupportsInsertCombined { get; }
		protected abstract bool SupportsUseIndex { get; }
		protected virtual bool SupportsFastJoin { get { return true; } }
		
		protected abstract string CreateNullTest(string column);

		private void Init() {
			if (!firstUse) return;
			firstUse = false;
			
			CreateTable();
			CreateIndexes();
		}
		
		public override int StatementCount { get { Init(); RunAddBuffer(); return RunScalarInt("select count(subject) from " + table + "_statements", 0); } }
		
		private int NextId() {
			if (lockedIdCache != null && cachedNextId != -1)
				return ++cachedNextId;
			
			RunAddBuffer();
			
			// The 0 id is not used.
			// The 1 id is reserved for Statement.DefaultMeta.
			int nextid = 2;
			
			CheckMax("select max(subject) from " + table + "_statements", ref nextid);
			CheckMax("select max(predicate) from " + table + "_statements", ref nextid);
			CheckMax("select max(object) from " + table + "_statements where objecttype=0", ref nextid);
			CheckMax("select max(meta) from " + table + "_statements", ref nextid);
			CheckMax("select max(id) from " + table + "_literals", ref nextid);
			CheckMax("select max(id) from " + table + "_entities", ref nextid);
			
			cachedNextId = nextid;
			
			return nextid;
		}
		
		private void CheckMax(string command, ref int nextid) {
			int maxid = RunScalarInt(command, 0);
			if (maxid >= nextid) nextid = maxid + 1;
		}
		
		public override void Clear() {
			Init();
			if (addStatementBuffer != null) addStatementBuffer.Clear();
			RunCommand("DELETE FROM " + table + "_statements;");
			RunCommand("DELETE FROM " + table + "_literals;");
			RunCommand("DELETE FROM " + table + "_entities;");
		}
		
		private int GetLiteralId(Literal literal, bool create, bool cacheIsComplete, StringBuilder buffer, bool insertCombined) {
			// Returns the literal ID associated with the literal.  If a literal
			// doesn't exist and create is true, a new literal is created,
			// otherwise 0 is returned.
			
			if (literalCache.Count > 0) {
				object ret = literalCache[literal];
				if (ret != null) return (int)ret;
			}

			if (!cacheIsComplete) { 
				StringBuilder b = cmdBuffer; cmdBuffer.Length = 0;
				b.Append("SELECT id FROM ");
				b.Append(table);
				b.Append("_literals WHERE ");
				WhereLiteral(b, literal);
				b.Append(" LIMIT 1;");
				
				object id = RunScalar(b.ToString());
				if (id != null) return AsInt(id);
			}
				
			if (create) {
				int id = AddLiteral(literal.Value, literal.Language, literal.DataType, buffer, insertCombined);
				if (literal.Value.Length < 75) {
					literalCache[literal] = id;
					literalCacheSize += literal.Value.Length;
					
					if (literalCacheSize > 10000000 + 32*literalCache.Count) {
						literalCacheSize = 0;
						literalCache.Clear();
					}
				}
				return id;
			}
			
			return 0;
		}
		
		private void WhereLiteral(StringBuilder b, Literal literal) {
			b.Append("value = ");
			EscapedAppend(b, literal.Value);
			//b.Append(" AND BINARY value = ");
			//EscapedAppend(b, literal.Value);
			b.Append(" AND ");
			if (literal.Language != null) {
				b.Append("language = ");
				EscapedAppend(b, literal.Language);
			} else {
				b.Append(CreateNullTest("language"));
			}
			b.Append(" AND ");
			if (literal.DataType != null) {
				b.Append("datatype = ");
				EscapedAppend(b, literal.DataType);
			} else {
				b.Append(CreateNullTest("datatype"));
			}
		}
		
		private int AddLiteral(string value, string language, string datatype, StringBuilder buffer, bool insertCombined) {
			int id = NextId();
			
			StringBuilder b;
			if (buffer != null) {
				b = buffer;
			} else {
				b = cmdBuffer; cmdBuffer.Length = 0;
			}
			
			if (!insertCombined) {
				b.Append(INSERT_INTO_LITERALS_VALUES);
			} else {
				if (b.Length > 0)
					b.Append(",");
			}
			b.Append("(");
			b.Append(id);
			b.Append(",");
			EscapedAppend(b, value);
			b.Append(",");
			if (language != null)
				EscapedAppend(b, language);
			else
				b.Append("NULL");
			b.Append(",");
			if (datatype != null)
				EscapedAppend(b, datatype);
			else
				b.Append("NULL");
			b.Append(")");
			if (!insertCombined)
				b.Append(";");
			
			if (buffer == null)
				RunCommand(b.ToString());
			
			return id;
		}
		
		private int GetEntityId(string uri, bool create, StringBuilder entityInsertBuffer, bool insertCombined) {
			// Returns the resource ID associated with the URI.  If a resource
			// doesn't exist and create is true, a new resource is created,
			// otherwise 0 is returned.
			
			int id;	
			
			if (lockedIdCache != null) {
				object idobj = lockedIdCache[uri];
				if (idobj == null && !create) return 0;
				if (idobj != null) return (int)idobj;
			} else {
				StringBuilder cmd = cmdBuffer; cmdBuffer.Length = 0;
				cmd.Append("SELECT id FROM ");
				cmd.Append(table);
				cmd.Append("_entities WHERE value =");
				EscapedAppend(cmd, uri);
				cmd.Append(" LIMIT 1;");
				id = RunScalarInt(cmd.ToString(), 0);
				if (id != 0 || !create) return id;
			}
			
			// If we got here, no such resource exists and create is true.
			
			if (uri.Length > 255)
				throw new NotSupportedException("URIs must be a maximum of 255 characters for this store due to indexing constraints (before MySQL 4.1.2).");

			id = NextId();
			
			StringBuilder b;
			if (entityInsertBuffer != null) {
				b = entityInsertBuffer;
			} else {
				b = cmdBuffer; cmdBuffer.Length = 0;
			}
			
			if (!insertCombined) {
				b.Append(INSERT_INTO_ENTITIES_VALUES);
			} else {
				if (b.Length > 0)
					b.Append(",");
			}
			b.Append("(");
			b.Append(id);
			b.Append(",");
			EscapedAppend(b, uri);
			b.Append(")");
			if (!insertCombined)
				b.Append(";");
			
			if (entityInsertBuffer == null)
				RunCommand(b.ToString());
				
			// Add it to the URI map
					
			if (lockedIdCache != null)
				lockedIdCache[uri] = id;
			
			return id;
		}
		
		private int GetResourceId(Resource resource, bool create) {
			return GetResourceIdBuffer(resource, create, false, null, null, false);
		}
		
		private int GetResourceIdBuffer(Resource resource, bool create, bool literalCacheComplete, StringBuilder literalInsertBuffer, StringBuilder entityInsertBuffer, bool insertCombined) {
			if (resource == null) return 0;
			
			if (resource is Literal) {
				Literal lit = (Literal)resource;
				return GetLiteralId(lit, create, literalCacheComplete, literalInsertBuffer, insertCombined);
			}
			
			if (object.ReferenceEquals(resource, Statement.DefaultMeta))
				return 1;
			
			ResourceKey key = (ResourceKey)GetResourceKey(resource);
			if (key != null) return key.ResId;
			
			int id;
			
			if (resource.Uri != null) {
				id = GetEntityId(resource.Uri, create, entityInsertBuffer, insertCombined);
			} else {
				// This anonymous node didn't come from the database
				// since it didn't have a resource key.  If !create,
				// then just return 0 to signal the resource doesn't exist.
				if (!create) return 0;

				if (lockedIdCache != null) {
					// Can just increment the counter.
					id = NextId();
				} else {
					// We need to reserve an id for this resource so that
					// this function returns other ids for other anonymous
					// resources.  Don't know how to do this yet, so
					// just throw an exception.
					throw new NotImplementedException("Anonymous nodes cannot be added to this store outside of an Import operation.");
				}
			}
				
			if (id != 0)
				SetResourceKey(resource, new ResourceKey(id));
			return id;
		}

		private int ObjectType(Resource r) {
			if (r is Literal) return 1;
			return 0;
		}
		
		private Entity MakeEntity(int resourceId, string uri, Hashtable cache) {
			if (resourceId == 0)
				return null;
			if (resourceId == 1)
				return Statement.DefaultMeta;
			
			ResourceKey rk = new ResourceKey(resourceId);
			
			if (cache != null && cache.ContainsKey(rk))
				return (Entity)cache[rk];
			
			Entity ent = new Entity(uri);
			
			SetResourceKey(ent, rk);
			
			if (cache != null)
				cache[rk] = ent;
				
			return ent;
		}
		
		public override void Add(Statement statement) {
			if (statement.AnyNull) throw new ArgumentNullException();
			
			if (addStatementBuffer != null) {
				addStatementBuffer.Add(statement);
				if (addStatementBuffer.Count >= 400)
					RunAddBuffer();
				return;
			}
			
			Init();
			
			int subj = GetResourceId(statement.Subject, true);
			int pred = GetResourceId(statement.Predicate, true);
			int objtype = ObjectType(statement.Object);
			int obj = GetResourceId(statement.Object, true);
			int meta = GetResourceId(statement.Meta, true);
			
			StringBuilder addBuffer = cmdBuffer; addBuffer.Length = 0;
			
			addBuffer.Append(INSERT_INTO_STATEMENTS_VALUES);
			addBuffer.Append("(");

			addBuffer.Append(subj);
			addBuffer.Append(", ");
			addBuffer.Append(pred);
			addBuffer.Append(", ");
			addBuffer.Append(objtype);
			addBuffer.Append(", ");
			addBuffer.Append(obj);
			addBuffer.Append(", ");
			addBuffer.Append(meta);
			addBuffer.Append("); ");
			
			RunCommand(addBuffer.ToString());
		}
		
		private void RunAddBuffer() {
			if (addStatementBuffer == null || addStatementBuffer.Count == 0) return;
			
			bool insertCombined = SupportsInsertCombined;
			
			Init();
			
			// Prevent recursion through NextId=>StatementCount
			ArrayList statements = addStatementBuffer;
			addStatementBuffer = null;
			
			// Prefetch the IDs of all literals that aren't
			// in the literal map.
			StringBuilder cmd = new StringBuilder();
			cmd.Append("SELECT id, value, language, datatype FROM ");
			cmd.Append(table);
			cmd.Append("_literals WHERE 0 ");
			bool hasLiterals = false;
			foreach (Statement s in statements) {
				Literal lit = s.Object as Literal;
				if (lit == null) continue;
				
				if (literalCache.ContainsKey(lit))
					continue;
				
				hasLiterals = true;
				
				cmd.Append(" or (");
				WhereLiteral(cmd, lit);
				cmd.Append(")");
			}
			if (hasLiterals) {
				cmd.Append(";");
				IDataReader reader = RunReader(cmd.ToString());
				try {
					while (reader.Read()) {
						int literalid = AsInt(reader[0]);
						
						string val = AsString(reader[1]);
						string lang = AsString(reader[2]);
						string dt = AsString(reader[3]);
						Literal lit = new Literal(val, lang, dt);
						
						literalCache[lit] = literalid;
						literalCacheSize += val.Length;
					}
				} finally {
					reader.Close();
				}
			}
			
			StringBuilder entityInsertions = new StringBuilder();
			StringBuilder literalInsertions = new StringBuilder();
			
			cmd = new StringBuilder();
			if (insertCombined)
				cmd.Append(INSERT_INTO_STATEMENTS_VALUES);

			for (int i = 0; i < statements.Count; i++) {
				Statement statement = (Statement)statements[i];
			
				int subj = GetResourceIdBuffer(statement.Subject, true, true, literalInsertions, entityInsertions, insertCombined);
				int pred = GetResourceIdBuffer(statement.Predicate, true,  true, literalInsertions, entityInsertions, insertCombined);
				int objtype = ObjectType(statement.Object);
				int obj = GetResourceIdBuffer(statement.Object, true, true, literalInsertions, entityInsertions, insertCombined);
				int meta = GetResourceIdBuffer(statement.Meta, true, true, literalInsertions, entityInsertions, insertCombined);
				
				if (!insertCombined)
					cmd.Append(INSERT_INTO_STATEMENTS_VALUES);
				
				cmd.Append("(");
				cmd.Append(subj);
				cmd.Append(", ");
				cmd.Append(pred);
				cmd.Append(", ");
				cmd.Append(objtype);
				cmd.Append(", ");
				cmd.Append(obj);
				cmd.Append(", ");
				cmd.Append(meta);
				if (i == statements.Count-1 || !insertCombined)
					cmd.Append(");");
				else
					cmd.Append("),");
			}
			
			if (literalInsertions.Length > 0) {
				if (insertCombined) {
					literalInsertions.Insert(0, INSERT_INTO_LITERALS_VALUES);
					literalInsertions.Append(';');
				}
				RunCommand(literalInsertions.ToString());
			}
			
			if (entityInsertions.Length > 0) {
				if (insertCombined) {
					entityInsertions.Insert(0, INSERT_INTO_ENTITIES_VALUES);
					entityInsertions.Append(';');
				}
				RunCommand(entityInsertions.ToString());
			}
			
			RunCommand(cmd.ToString());
			
			// Clear the array and reuse it.
			statements.Clear();
			addStatementBuffer = statements;
		}
		
		public override void Remove(Statement template) {
			Init();
			RunAddBuffer();

			System.Text.StringBuilder cmd = new System.Text.StringBuilder("DELETE FROM ");
			cmd.Append(table);
			cmd.Append("_statements ");
			if (!WhereClause(template, cmd)) return;
			cmd.Append(";");
			
			RunCommand(cmd.ToString());
		}
		
		public override Entity[] GetAllEntities() {
			return GetAllEntities(fourcols);
		}
			
		public override Entity[] GetAllPredicates() {
			return GetAllEntities(predcol);
		}
		
		private Entity[] GetAllEntities(string[] cols) {
			RunAddBuffer();
			ArrayList ret = new ArrayList();
			Hashtable seen = new Hashtable();
			foreach (string col in cols) {
				IDataReader reader = RunReader("SELECT " + col + ", value FROM " + table + "_statements LEFT JOIN " + table + "_entities ON " + col + "=id " + (col == "object" ? " WHERE objecttype=0" : "") + " GROUP BY " + col + ";");
				try {
					while (reader.Read()) {
						int id = AsInt(reader[0]);
						if (id <= 1) continue; // don't return DefaultMeta.
						
						if (seen.ContainsKey(id)) continue;
						seen[id] = seen;
						
						string uri = AsString(reader[1]);
						ret.Add(MakeEntity(id, uri, null));
					}
				} finally {
					reader.Close();
				}
			}
			return (Entity[])ret.ToArray(typeof(Entity));;
		}
		
		private bool WhereItem(string col, Resource r, System.Text.StringBuilder cmd, bool and) {
			if (and) cmd.Append(" and ");
			
			if (col.EndsWith("object")) {
				string colprefix = "";
				if (col != "object")
					colprefix = col.Substring(0, col.Length-"object".Length);
			
				if (r is MultiRes) {
					// Assumption that ID space of literals and entities are the same.
					cmd.Append("( ");
					cmd.Append(col);
					cmd.Append(" IN (");
					if (!AppendMultiRes((MultiRes)r, cmd)) return false;
					cmd.Append(" ))");
				} else if (r is Literal) {
					Literal lit = (Literal)r;
					int id = GetResourceId(lit, false);
					if (id == 0) return false;
					cmd.Append(" (");
					cmd.Append(colprefix);
					cmd.Append("objecttype = 1 and ");
					cmd.Append(col);
					cmd.Append(" = ");
					cmd.Append(id);
					cmd.Append(")");
				} else {
					int id = GetResourceId(r, false);
					if (id == 0) return false;
					cmd.Append(" (");
					cmd.Append(colprefix);
					cmd.Append("objecttype = 0 and ");
					cmd.Append(col);
					cmd.Append(" = ");
					cmd.Append(id);
					cmd.Append(")");
				}
			
			} else if (r is MultiRes) {
				cmd.Append("( ");
				cmd.Append(col);
				cmd.Append(" IN (");
				if (!AppendMultiRes((MultiRes)r, cmd)) return false;
				cmd.Append(" ))");
				
			} else {
				int id = GetResourceId(r, false);
				if (id == 0) return false;
				
				cmd.Append("( ");
				cmd.Append(col);
				cmd.Append(" = ");
				cmd.Append(id);
				cmd.Append(" )");
			}
			
			return true;
		}
		
		private bool AppendMultiRes(MultiRes r, StringBuilder cmd) {
			for (int i = 0; i < r.items.Count; i++) {
				if (i != 0) cmd.Append(",");
				int id = GetResourceId((Resource)r.items[i], false);
				if (id == 0) return false;
				cmd.Append(id);
			}
			return true;
		}
		
		private bool WhereClause(Statement template, System.Text.StringBuilder cmd) {
			return WhereClause(template.Subject, template.Predicate, template.Object, template.Meta, cmd);
		}

		private bool WhereClause(Resource templateSubject, Resource templatePredicate, Resource templateObject, Resource templateMeta, System.Text.StringBuilder cmd) {
			if (templateSubject == null && templatePredicate == null && templateObject == null && templateMeta == null)
				return true;
			
			cmd.Append(" WHERE ");
			
			if (templateSubject != null)
				if (!WhereItem("subject", templateSubject, cmd, false)) return false;
			
			if (templatePredicate != null)
				if (!WhereItem("predicate", templatePredicate, cmd, templateSubject != null)) return false;
			
			if (templateObject != null)
				if (!WhereItem("object", templateObject, cmd, templateSubject != null || templatePredicate != null)) return false;
			
			if (templateMeta != null)
				if (!WhereItem("meta", templateMeta, cmd, templateSubject != null || templatePredicate != null || templateObject != null)) return false;
			
			return true;
		}
		
		private int AsInt(object r) {
			if (r is int) return (int)r;
			if (r is uint) return (int)(uint)r;
			if (r is string) return int.Parse((string)r);
			throw new ArgumentException(r.ToString());
		}
		
		private string AsString(object r) {
			if (r == null)
				return null;
			else if (r is System.DBNull)
				return null;
			else if (r is string)
				return (string)r;
			else if (r is byte[])
				return System.Text.Encoding.UTF8.GetString((byte[])r);
			else
				throw new FormatException("SQL store returned a literal value as " + r.GetType());
		}
		
		private struct SPOLM {
			public int S, P, OT, OID, M;
		}
		
		private static void AppendComma(StringBuilder builder, string text, bool comma) {
			if (comma)
				builder.Append(", ");
			builder.Append(text);
		}
		
		private static void SelectFilter(SelectPartialFilter partialFilter, StringBuilder cmd) {
			bool f = true;
			
			if (partialFilter.Subject) { cmd.Append("q.subject, suri.value"); f = false; }
			if (partialFilter.Predicate) { AppendComma(cmd, "q.predicate, puri.value", !f); f = false; }
			if (partialFilter.Object) { AppendComma(cmd, "q.objecttype, q.object, ouri.value", !f); f = false; }
			if (partialFilter.Meta) { AppendComma(cmd, "q.meta, muri.value", !f); f = false; }
		}
		
		public override void Select(Statement[] templates, SelectPartialFilter partialFilter, StatementSink result) {
			if (templates == null) throw new ArgumentNullException();
			if (result == null) throw new ArgumentNullException();
			if (templates.Length == 0) return;
	
			bool first = true;
			Resource sv = null, pv = null, ov = null, mv = null;
			bool sm = false, pm = false, om = false, mm = false;
			ArrayList sl = new ArrayList(), pl = new ArrayList(), ol = new ArrayList(), ml = new ArrayList();
			foreach (Statement template in templates) {
				if (first) {
					first = false;
					sv = template.Subject;
					pv = template.Predicate;
					ov = template.Object;
					mv = template.Meta;
				} else {
					if (sv != template.Subject) sm = true;
					if (pv != template.Predicate) pm = true;
					if (ov != template.Object) om = true;
					if (mv != template.Meta) mm = true;
				}
				if (template.Subject != null) sl.Add(template.Subject);
				if (template.Predicate != null) pl.Add(template.Predicate);
				if (template.Object != null) ol.Add(template.Object);
				if (template.Meta != null) ml.Add(template.Meta);
			}
			
			if (!sm && !pm && !om && !mm) {
				Select(templates[0], partialFilter, result);
				return;
			} else if (sm && !pm && !om && !mm) {
				Select(new MultiRes(sl), pv, ov, mv, partialFilter, result);
			} else if (!sm && pm && !om && !mm) {
				Select(sv, new MultiRes(pl), ov, mv, partialFilter, result);
			} else if (!sm && !pm && om && !mm) {
				Select(sv, pv, new MultiRes(ol), mv, partialFilter, result);
			} else if (!sm && !pm && !om && mm) {
				Select(sv, pv, ov, new MultiRes(ml), partialFilter, result);
			} else {
				foreach (Statement template in templates)
					Select(template, partialFilter, result);
			}
		}
		
		private class MultiRes : Resource {
			public MultiRes(ArrayList a) { items = a; }
			public ArrayList items;
			public override string Uri { get { return null; } }
		}
		
		public override void Select(Statement template, SelectPartialFilter partialFilter, StatementSink result) {
			if (result == null) throw new ArgumentNullException();
			Select(template.Subject, template.Predicate, template.Object, template.Meta, partialFilter, result);
		}

		private void Select(Resource templateSubject, Resource templatePredicate, Resource templateObject, Resource templateMeta, SelectPartialFilter partialFilter, StatementSink result) {
			if (result == null) throw new ArgumentNullException();
	
			Init();
			RunAddBuffer();
			
			bool limitOne = partialFilter.SelectFirst;
			
			// Don't select on columns that we already know from the template
			partialFilter = new SelectPartialFilter(
				(partialFilter.Subject && templateSubject == null) || templateSubject is MultiRes,
				(partialFilter.Predicate && templatePredicate == null) || templatePredicate is MultiRes,
				(partialFilter.Object && templateObject == null) || templateObject is MultiRes,
				(partialFilter.Meta && templateMeta == null) || templateMeta is MultiRes
				);
			
			if (partialFilter.SelectNone)
				partialFilter = SelectPartialFilter.All;
				
			// SQLite has a problem with LEFT JOIN: When a condition is made on the
			// first table in the ON clause (q.objecttype=0/1), when it fails,
			// it excludes the row from the first table, whereas it should only
			// exclude the results of the join, but include the row.  Thus, the space
			// of IDs between literals and entities must be shared!
			
			System.Text.StringBuilder cmd = new System.Text.StringBuilder("SELECT ");
			SelectFilter(partialFilter, cmd);
			if (partialFilter.Object)
				cmd.Append(", lit.value, lit.language, lit.datatype");
			cmd.Append(" FROM ");
			cmd.Append(table);
			cmd.Append("_statements AS q");
			if (SupportsUseIndex) {
				// When selecting on mutliple resources at once, assume that it's faster
				// to select for each resource, rather than based on another index (say,
				// the predicate that the templates share).
				if (templateSubject is MultiRes) cmd.Append(" USE INDEX(subject_index)");
				if (templatePredicate is MultiRes) cmd.Append(" USE INDEX(predicate_index)");
				if (templateObject is MultiRes) cmd.Append(" USE INDEX(object_index)");
				if (templateMeta is MultiRes) cmd.Append(" USE INDEX(meta_index)");
			}
			
			if (partialFilter.Object) {
				cmd.Append(" LEFT JOIN ");
				cmd.Append(table);
				//cmd.Append("_literals AS lit ON q.objecttype=1 AND q.object=lit.id LEFT JOIN ");
				cmd.Append("_literals AS lit ON q.object=lit.id");
			}
			if (partialFilter.Subject) {
				cmd.Append(" LEFT JOIN ");
				cmd.Append(table);
				cmd.Append("_entities AS suri ON q.subject = suri.id");
			}
			if (partialFilter.Predicate) {
				cmd.Append(" LEFT JOIN ");
				cmd.Append(table);
				cmd.Append("_entities AS puri ON q.predicate = puri.id");
			}
			if (partialFilter.Object) {
				cmd.Append(" LEFT JOIN ");
				cmd.Append(table);
				//cmd.Append("_entities AS ouri ON q.objecttype=0 AND q.object = ouri.id LEFT JOIN ");
				cmd.Append("_entities AS ouri ON q.object = ouri.id");
			}
			if (partialFilter.Meta) {
				cmd.Append(" LEFT JOIN ");
				cmd.Append(table);
				cmd.Append("_entities AS muri ON q.meta = muri.id");
			}
			cmd.Append(' ');
			if (!WhereClause(templateSubject, templatePredicate, templateObject, templateMeta, cmd)) return;
			cmd.Append(";");
			
			if (limitOne)
				cmd.Append(" LIMIT 1");
			
			if (Debug || false) {
				string cmd2 = cmd.ToString();
				//if (cmd2.Length > 80) cmd2 = cmd2.Substring(0, 80);
				Console.Error.WriteLine(cmd2);
			}
			
			IDataReader reader = RunReader(cmd.ToString());
			
			Hashtable entMap = new Hashtable();
			
			try {
				while (reader.Read()) {
					int col = 0;
					int sid = -1, pid = -1, ot = -1, oid = -1, mid = -1;
					string suri = null, puri = null, ouri = null, muri = null;
					
					if (partialFilter.Subject) { sid = AsInt(reader[col++]); suri = AsString(reader[col++]); }
					if (partialFilter.Predicate) { pid = AsInt(reader[col++]); puri = AsString(reader[col++]); }
					if (partialFilter.Object) { ot = AsInt(reader[col++]); oid = AsInt(reader[col++]); ouri = AsString(reader[col++]); }
					if (partialFilter.Meta) { mid = AsInt(reader[col++]); muri = AsString(reader[col++]); }
					
					string lv = null, ll = null, ld = null;
					if (ot == 1 && partialFilter.Object) {
						lv = AsString(reader[col++]);
						ll = AsString(reader[col++]);
						ld = AsString(reader[col++]);
					}
					
					bool ret = result.Add(new Statement(
						!partialFilter.Subject ? (Entity)templateSubject : MakeEntity(sid, suri, entMap),
						!partialFilter.Predicate ? (Entity)templatePredicate : MakeEntity(pid, puri, entMap),
						!partialFilter.Object ? templateObject : 
							(ot == 0 ? (Resource)MakeEntity(oid, ouri, entMap)
								     : (Resource)new Literal(lv, ll, ld)),
						(!partialFilter.Meta || mid == 0) ? (Entity)templateMeta : MakeEntity(mid, muri, entMap)
						));
					if (!ret) break;

				}
			} finally {
				reader.Close();
			}
		}
		
		private string Escape(string str, bool quotes) {
			if (str == null) return "NULL";
			StringBuilder b = new StringBuilder();
			EscapedAppend(b, str, quotes);
			return b.ToString();
		}
		
		protected virtual void EscapedAppend(StringBuilder b, string str) {
			EscapedAppend(b, str, true);
		}

		protected virtual void EscapedAppend(StringBuilder b, string str, bool quotes) {
			if (quotes) b.Append('"');
			for (int i = 0; i < str.Length; i++) {
				char c = str[i];
				switch (c) {
					case '\n': b.Append("\\n"); break;
					case '\\':
					case '\"':
					case '%':
					case '*':
						b.Append('\\');
						b.Append(c);
						break;
					default:
						b.Append(c);
						break;
				}
			}
			if (quotes) b.Append('"');
		}
		
		internal static void Escape(StringBuilder b) {
			b.Replace("\\", "\\\\");
			b.Replace("\"", "\\\"");
			b.Replace("\n", "\\n");
			b.Replace("%", "\\%");
			b.Replace("*", "\\*");
		}

		public override void Import(StatementSource source) {
			if (source == null) throw new ArgumentNullException();
			if (lockedIdCache != null) throw new InvalidOperationException("Store is already importing.");
			
			Init();
			RunAddBuffer();
			
			cachedNextId = -1;
			lockedIdCache = new UriMap();
			addStatementBuffer = new ArrayList(); 
			
			IDataReader reader = RunReader("SELECT id, value from " + table + "_entities;");			
			try {
				while (reader.Read())
					lockedIdCache[AsString(reader[1])] = AsInt(reader[0]);
			} finally {
				reader.Close();
			}
			
			BeginTransaction();
			
			try {
				base.Import(source);
			} finally {
				RunAddBuffer();
				EndTransaction();
				
				lockedIdCache = null;
				addStatementBuffer = null;
				
				// Remove duplicate literals
				/*
				while (true) {
					bool foundDupLiteral = false;
					StringBuilder litdupremove = new StringBuilder("DELETE FROM " + table + "_literals WHERE id IN (");
					StringBuilder litdupreplace = new StringBuilder();
					Console.Error.WriteLine("X");
					reader = RunReader("select a.id, b.id from " + table + "_literals as a inner join " + table + "_literals as b on a.value=b.value and a.language<=>b.language and a.datatype <=> b.datatype and a.id<b.id LIMIT 10000");
					while (reader.Read()) {
						int lit1 = AsInt(reader[0]);
						int lit2 = AsInt(reader[1]);
						
						if (foundDupLiteral) litdupremove.Append(",");
						litdupremove.Append(lit2);
						
						litdupreplace.Append("UPDATE " + table + "_statements SET object = " + lit1 + " WHERE objecttype=1 AND object=" + lit2 + "; ");
						
						foundDupLiteral = true;
					}
					reader.Close();
					if (!foundDupLiteral) break;
					litdupremove.Append(");");
					RunCommand(litdupremove.ToString());
					RunCommand(litdupreplace.ToString());
				}
				*/
				
				literalCache.Clear();
				literalCacheSize = 0;			
			}
		}

		public override void Replace(Entity a, Entity b) {
			Init();
			RunAddBuffer();
			int id = GetResourceId(b, true);
			
			foreach (string col in fourcols) {
				StringBuilder cmd = new StringBuilder();
				cmd.Append("UPDATE ");
				cmd.Append(table);
				cmd.Append("_statements SET ");
				cmd.Append(col);
				cmd.Append("=");
				cmd.Append(id);
				if (!WhereItem(col, a, cmd, false)) return;
				cmd.Append(";");
				RunCommand(cmd.ToString());
			}			
		}
		
		public override void Replace(Statement find, Statement replacement) {
			if (find.AnyNull) throw new ArgumentNullException("find");
			if (replacement.AnyNull) throw new ArgumentNullException("replacement");
			if (find == replacement) return;
			
			Init();
			RunAddBuffer();

			int subj = GetResourceId(replacement.Subject, true);
			int pred = GetResourceId(replacement.Predicate, true);
			int objtype = ObjectType(replacement.Object);
			int obj = GetResourceId(replacement.Object, true);
			int meta = GetResourceId(replacement.Meta, true);

			StringBuilder cmd = cmdBuffer; cmd.Length = 0;
			
			cmd.Append("UPDATE ");
			cmd.Append(table);
			cmd.Append("_statements SET subject=");
			cmd.Append(subj);
			cmd.Append(", predicate=");
			cmd.Append(pred);
			cmd.Append(", objecttype=");
			cmd.Append(objtype);
			cmd.Append(", object=");
			cmd.Append(obj);
			cmd.Append(", meta=");
			cmd.Append(meta);
			cmd.Append(" ");
			
			if (!WhereClause(find, cmd))
				return;
			
			RunCommand(cmd.ToString());
		}
		
		public override Entity[] FindEntities(Statement[] filters) {
			if (filters.Length == 0) return new Entity[0];
		
			if (!SupportsFastJoin)
				return base.FindEntities(filters);
		
			Init();
			
			string f1pos = is_spom(filters[0]);
			if (f1pos == null) throw new ArgumentException("Null must appear in every statement.");
			
			StringBuilder cmd = new StringBuilder();
			cmd.Append("SELECT s.");
			cmd.Append(f1pos);
			cmd.Append(", uri.value FROM ");
			cmd.Append(table);
			cmd.Append("_statements AS s LEFT JOIN ");
			cmd.Append(table);
			cmd.Append("_entities AS uri ON uri.id=s.");
			cmd.Append(f1pos);
			
			if (isliteralmatch(filters[0].Object))
				appendLiteralMatch(cmd, "l0", "s", ((Literal)filters[0].Object).Value);
			
			for (int i = 1; i < filters.Length; i++) {
				cmd.Append(" INNER JOIN ");
				cmd.Append(table);
				cmd.Append("_statements AS f");
				cmd.Append(i);
				cmd.Append(" ON s.");
				cmd.Append(f1pos);
				cmd.Append("=f");
				cmd.Append(i);
				cmd.Append(".");
				string fipos = is_spom(filters[i]);
				if (fipos == null) throw new ArgumentException("Null must appear in every statement.");
				cmd.Append(fipos);
				
				if (filters[i].Subject != null && filters[i].Subject != null)
					if (!WhereItem("f" + i + ".subject", filters[i].Subject, cmd, true)) return new Entity[0];
				if (filters[i].Predicate != null && filters[i].Predicate != null)
					if (!WhereItem("f" + i + ".predicate", filters[i].Predicate, cmd, true)) return new Entity[0];
				if (filters[i].Object != null && filters[i].Object != null && !isliteralmatch(filters[i].Object))
					if (!WhereItem("f" + i + ".object", filters[i].Object, cmd, true)) return new Entity[0];
				if (filters[i].Meta != null && filters[i].Meta != null)
					if (!WhereItem("f" + i + ".meta", filters[i].Meta, cmd, true)) return new Entity[0];
				
				if (filters[i].Object == null)
					cmd.Append("AND f" + i + ".objecttype=0 ");
					
				if (isliteralmatch(filters[i].Object)) {
					cmd.Append("AND f" + i + ".objecttype=1 ");
					appendLiteralMatch(cmd, "l" + i, "f" + i, ((Literal)filters[i].Object).Value);
				}
			}
			
			cmd.Append(" WHERE 1 ");
			
			if (filters[0].Subject != null && filters[0].Subject != null)
				if (!WhereItem("s.subject", filters[0].Subject, cmd, true)) return new Entity[0];
			if (filters[0].Predicate != null && filters[0].Predicate != null)
				if (!WhereItem("s.predicate", filters[0].Predicate, cmd, true)) return new Entity[0];
			if (filters[0].Object != null && filters[0].Object != null && !isliteralmatch(filters[0].Object))
				if (!WhereItem("s.object", filters[0].Object, cmd, true)) return new Entity[0];
			if (isliteralmatch(filters[0].Object))
				cmd.Append("AND s.objecttype=1 ");
			if (filters[0].Meta != null && filters[0].Meta != null)
				if (!WhereItem("s.meta", filters[0].Meta, cmd, true)) return new Entity[0];
			
			if (filters[0].Object == null)
				cmd.Append(" AND s.objecttype=0");
				
			cmd.Append(";");
			
			//Console.Error.WriteLine(cmd.ToString());
			
			IDataReader reader = RunReader(cmd.ToString());
			ArrayList entities = new ArrayList();
			Hashtable seen = new Hashtable();
			try {
				while (reader.Read()) {
					int id = AsInt(reader[0]);
					string uri = AsString(reader[1]);
					if (seen.ContainsKey(id)) continue;
					seen[id] = seen;
 					entities.Add(MakeEntity(id, uri, null));
 				}
			} finally {
				reader.Close();
			}
			
			return (Entity[])entities.ToArray(typeof(Entity));
		}
		
		private string is_spom(Statement s) {
			if (s.Subject == null) return "subject";
			if (s.Predicate == null) return "predicate";
			if (s.Object == null) return "object";
			if (s.Meta == null) return "meta";
			return null;
		}
		
		private bool isliteralmatch(Resource r) {
			if (r == null || !(r is Literal)) return false;
			return ((Literal)r).DataType == "SEMWEB::LITERAL::CONTAINS";
		}
		
		private void appendLiteralMatch(StringBuilder cmd, string joinalias, string lefttable, string pattern) {
			cmd.Append(" INNER JOIN ");
			cmd.Append(table);
			cmd.Append("_literals AS ");
			cmd.Append(joinalias);
			cmd.Append(" ON ");
			cmd.Append(joinalias);
			cmd.Append(".id=");
			cmd.Append(lefttable);
			cmd.Append(".object");
			cmd.Append(" AND ");
			cmd.Append(joinalias);
			cmd.Append(".value LIKE \"%");
			cmd.Append(Escape(pattern, false));
			cmd.Append("%\" ");
		}
		
		protected abstract void RunCommand(string sql);
		protected abstract object RunScalar(string sql);
		protected abstract IDataReader RunReader(string sql);
		
		private int RunScalarInt(string sql, int def) {
			object ret = RunScalar(sql);
			if (ret == null) return def;
			if (ret is int) return (int)ret;
			try {
				return int.Parse(ret.ToString());
			} catch (FormatException e) {
				return def;
			}
		}
		
		/*
		private string RunScalarString(string sql) {
			object ret = RunScalar(sql);
			if (ret == null) return null;
			if (ret is string) return (string)ret;
			if (ret is byte[]) return System.Text.Encoding.UTF8.GetString((byte[])ret);
			throw new FormatException("SQL store returned a literal value as " + ret);
		}
		*/

		protected virtual void CreateTable() {
			foreach (string cmd in GetCreateTableCommands(table)) {
				try {
					RunCommand(cmd);
				} catch (Exception e) {
				}
			}
		}
		
		protected virtual void CreateIndexes() {
			foreach (string cmd in GetCreateIndexCommands(table)) {
				try {
					RunCommand(cmd);
				} catch (Exception e) {
				}
			}
		}
		
		protected virtual void BeginTransaction() { }
		protected virtual void EndTransaction() { }
		
		internal static string[] GetCreateTableCommands(string table) {
			return new string[] {
				"CREATE TABLE " + table + "_statements" +
				"(subject int UNSIGNED NOT NULL, predicate int UNSIGNED NOT NULL, objecttype int NOT NULL, object int UNSIGNED NOT NULL, meta int UNSIGNED NOT NULL);",
				
				"CREATE TABLE " + table + "_literals" +
				"(id INT NOT NULL, value BLOB NOT NULL, language TEXT, datatype TEXT, PRIMARY KEY(id));",
				
				"CREATE TABLE " + table + "_entities" +
				"(id INT NOT NULL, value BLOB NOT NULL, PRIMARY KEY(id));"
				};
		}
		
		internal static string[] GetCreateIndexCommands(string table) {
			return new string[] {
				"CREATE INDEX subject_index ON " + table + "_statements(subject);",
				"CREATE INDEX predicate_index ON " + table + "_statements(predicate);",
				"CREATE INDEX object_index ON " + table + "_statements(objecttype, object);",
				"CREATE INDEX meta_index ON " + table + "_statements(meta);",
			
				"CREATE INDEX literal_index ON " + table + "_literals(value(30));",
				"CREATE UNIQUE INDEX entity_index ON " + table + "_entities(value(255));"
				};
		}
	}
	
}

namespace SemWeb.IO {
	using SemWeb;
	using SemWeb.Stores;
	
	// NEEDS TO BE UPDATED
	/*class SQLWriter : RdfWriter {
		TextWriter writer;
		string table;
		
		int resourcecounter = 0;
		Hashtable resources = new Hashtable();
		
		NamespaceManager m = new NamespaceManager();
		
		string[,] fastmap = new string[3,2];
		
		public SQLWriter(string spec) : this(GetWriter("-"), spec) { }
		
		public SQLWriter(string file, string tablename) : this(GetWriter(file), tablename) { }

		public SQLWriter(TextWriter writer, string tablename) {
			this.writer = writer;
			this.table = tablename;
			
			foreach (string cmd in SQLStore.GetCreateTableCommands(table))
				writer.WriteLine(cmd);
		}
		
		public override NamespaceManager Namespaces { get { return m; } }
		
		public override void WriteStatement(string subj, string pred, string obj) {
			writer.WriteLine("INSERT INTO {0}_statements VALUES ({1}, {2}, 0, {3}, 0);", table, ID(subj, 0), ID(pred, 1), ID(obj, 2)); 
		}
		
		public override void WriteStatement(string subj, string pred, Literal literal) {
			writer.WriteLine("INSERT INTO {0}_statements VALUES ({1}, {2}, 1, {3}, 0);", table, ID(subj, 0), ID(pred, 1), ID(literal)); 
		}
		
		public override string CreateAnonymousEntity() {
			int id = ++resourcecounter;
			string uri = "_anon:" + id;
			return uri;
		}
		
		public override void Close() {
			base.Close();
			foreach (string cmd in SQLStore.GetCreateIndexCommands(table))
				writer.WriteLine(cmd);
			writer.Close();
		}

		private string ID(Literal literal) {
			string id = (string)resources[literal];
			if (id == null) {
				id = (++resourcecounter).ToString();
				resources[literal] = id;
				writer.WriteLine("INSERT INTO {0}_literals VALUES ({1}, {2}, {3}, {4});", table, id, Escape(literal.Value), Escape(literal.Language), Escape(literal.DataType));
			}
			return id;
		}
		
		private string Escape(string str) {
			if (str == null) return "NULL";
			return "\"" + EscapeUnquoted(str) + "\"";
		}
		
		StringBuilder EscapeUnquotedBuffer = new StringBuilder();
		private string EscapeUnquoted(string str) {
			StringBuilder b = EscapeUnquotedBuffer;
			b.Length = 0;
			b.Append(str);
			SQLStore.Escape(b);
			return b.ToString();
		}
		
		private string ID(string uri, int x) {
			if (uri.StartsWith("_anon:")) return uri.Substring(6);
			
			// Make this faster when a subject, predicate, or object is repeated.
			if (fastmap[0,0] != null && uri == fastmap[0, 0]) return fastmap[0, 1];
			if (fastmap[1,0] != null && uri == fastmap[1, 0]) return fastmap[1, 1];
			if (fastmap[2,0] != null && uri == fastmap[2, 0]) return fastmap[2, 1];
			
			string id;
			
			if (resources.ContainsKey(uri)) {
				id = (string)resources[uri];
			} else {
				id = (++resourcecounter).ToString();
				resources[uri] = id;
				
				string literalid = ID(new Literal(uri));
				writer.WriteLine("INSERT INTO {0}_statements VALUES ({1}, 0, 1, {2}, 0);", table, id, literalid);
			}
			
			fastmap[x, 0] = uri;
			fastmap[x, 1] = id;
			
			return id;
		}

	}*/
	
}
