using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using SemWeb.Util;

namespace SemWeb.Stores {
	// TODO: It's not safe to have two concurrent accesses to the same database
	// because the creation of new entities will use the same IDs.
	
	public abstract class SQLStore : Store, SupportsPersistableBNodes {
		int dbformat = 1;
	
		string table;
		string guid;
		
		bool firstUse = true;
		
		bool isImporting = false;
		int cachedNextId = -1;
		Hashtable entityCache = new Hashtable();
		Hashtable literalCache = new Hashtable();
		
		ArrayList anonEntityHeldIds = new ArrayList();
		
		bool statementsRemoved = false;

		static bool Debug = System.Environment.GetEnvironmentVariable("SEMWEB_DEBUG_SQL") != null;
		
		StringBuilder cmdBuffer = new StringBuilder();
		
		// Buffer statements to process together.
		StatementList addStatementBuffer = null;
		
		string 	INSERT_INTO_LITERALS_VALUES,
				INSERT_INTO_STATEMENTS_VALUES,
				INSERT_INTO_ENTITIES_VALUES;
		char quote;
		
		object syncroot = new object();
		
		Hashtable metaEntities;
		
		SHA1 sha = SHA1.Create();
		
		int importAddBufferSize = 200, importAddBufferRotation = 0;
		TimeSpan importAddBufferTime = TimeSpan.MinValue;
				
		private class ResourceKey {
			public int ResId;
			
			public ResourceKey(int id) { ResId = id; }
			
			public override int GetHashCode() { return ResId.GetHashCode(); }
			public override bool Equals(object other) { return (other is ResourceKey) && ((ResourceKey)other).ResId == ResId; }
		}
		
		private static readonly string[] fourcols = new string[] { "subject", "predicate", "object", "meta" };
		private static readonly string[] predcol = new string[] { "predicate" };
		private static readonly string[] metacol = new string[] { "meta" };

		protected SQLStore(string table) {
			this.table = table;
			
			INSERT_INTO_LITERALS_VALUES = "INSERT INTO " + table + "_literals VALUES ";
			INSERT_INTO_ENTITIES_VALUES = "INSERT INTO " + table + "_entities VALUES ";
			INSERT_INTO_STATEMENTS_VALUES = "INSERT " + (SupportsInsertIgnore ? "IGNORE " : "") + "INTO " + table + "_statements VALUES ";
			
			quote = GetQuoteChar();
		}
		
		protected string TableName { get { return table; } }
		
		protected abstract bool SupportsNoDuplicates { get; }
		protected abstract bool SupportsInsertIgnore { get; }
		protected abstract bool SupportsInsertCombined { get; }
		protected virtual bool SupportsFastJoin { get { return true; } }
		protected abstract bool SupportsSubquery { get; }
		
		protected abstract void CreateNullTest(string column, System.Text.StringBuilder command);
		
		private void Init() {
			if (!firstUse) return;
			firstUse = false;
			
			CreateTable();
			CreateIndexes();
			CreateVersion();
		}
		
		private void CreateVersion() {	
			string verdatastr = RunScalarString("SELECT value FROM " + table + "_literals WHERE id = 0");
			NameValueCollection verdata = ParseVersionInfo(verdatastr);
			
			if (verdatastr != null && verdata["ver"] == null)
				throw new InvalidOperationException("The SQLStore adapter in this version of SemWeb cannot read databases created in previous versions.");
			
			verdata["ver"] = dbformat.ToString();
			
			if (verdata["guid"] == null) {
				guid = Guid.NewGuid().ToString("N");
				verdata["guid"] = guid;
			} else {
				guid = verdata["guid"];
			}
			
			string newverdata = SerializeVersionInfo(verdata);
			if (verdatastr == null)
				RunCommand("INSERT INTO " + table + "_literals (id, value) VALUES (0, " + Escape(newverdata, true) + ")");
			else if (verdatastr != newverdata)
				RunCommand("UPDATE " + table + "_literals SET value = " + newverdata + " WHERE id = 0");
		}
		
		NameValueCollection ParseVersionInfo(string verdata) {
			NameValueCollection nvc = new NameValueCollection();
			if (verdata == null) return nvc;
			foreach (string s in verdata.Split('\n')) {
				int c = s.IndexOf(':');
				if (c == -1) continue;
				nvc[s.Substring(0, c)] = s.Substring(c+1);
			}
			return nvc;
		}
		string SerializeVersionInfo(NameValueCollection verdata) {
			string ret = "";
			foreach (string k in verdata.Keys)
				ret += k + ":" + verdata[k] + "\n";
			return ret;
		}
		
		public override bool Distinct { get { return true; } }
		
		public override int StatementCount { get { Init(); RunAddBuffer(); return RunScalarInt("select count(subject) from " + table + "_statements", 0); } }
		
		public string GetStoreGuid() { return guid; }
		
		public string GetNodeId(BNode node) {
			ResourceKey rk = (ResourceKey)GetResourceKey(node);
			if (rk == null) return null;
			return rk.ResId.ToString();
		}
		
		public BNode GetNodeFromId(string persistentId) {
			try {
				int id = int.Parse(persistentId);
				return (BNode)MakeEntity(id, null, null);
			} catch (Exception) {
				return null;
			}
		}
		
		private int NextId() {
			if (isImporting && cachedNextId != -1)
				return ++cachedNextId;
			
			RunAddBuffer();
			
			// The 0 id is not used.
			// The 1 id is reserved for Statement.DefaultMeta.
			int nextid = 2;
			
			CheckMax("select max(subject) from " + table + "_statements", ref nextid);
			CheckMax("select max(predicate) from " + table + "_statements", ref nextid);
			CheckMax("select max(object) from " + table + "_statements", ref nextid);
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
			// Drop the tables, if they exist.
			try { RunCommand("DROP TABLE " + table + "_statements;"); } catch (Exception) { }
			try { RunCommand("DROP TABLE " + table + "_literals;"); } catch (Exception) { }
			try { RunCommand("DROP TABLE " + table + "_entities;"); } catch (Exception) { }
			firstUse = true;
		
			Init();
			if (addStatementBuffer != null) addStatementBuffer.Clear();
			
			metaEntities = null;

			//RunCommand("DELETE FROM " + table + "_statements;");
			//RunCommand("DELETE FROM " + table + "_literals;");
			//RunCommand("DELETE FROM " + table + "_entities;");
		}
		
		private string GetLiteralHash(Literal literal) {
			byte[] data = System.Text.Encoding.Unicode.GetBytes(literal.ToString());
			byte[] hash = sha.ComputeHash(data);
			return Convert.ToBase64String(hash);
		}
		
		private int GetLiteralId(Literal literal, bool create, StringBuilder buffer, bool insertCombined) {
			// Returns the literal ID associated with the literal.  If a literal
			// doesn't exist and create is true, a new literal is created,
			// otherwise 0 is returned.
			
			if (isImporting) {
				object ret = literalCache[literal];
				if (ret != null) return (int)ret;
			} else {
				StringBuilder b = cmdBuffer; cmdBuffer.Length = 0;
				b.Append("SELECT id FROM ");
				b.Append(table);
				b.Append("_literals WHERE hash =");
				b.Append("\"");
				b.Append(GetLiteralHash(literal));
				b.Append("\"");
				b.Append(" LIMIT 1;");
				
				object id = RunScalar(b.ToString());
				if (id != null) return AsInt(id);
			}
				
			if (create) {
				int id = AddLiteral(literal, buffer, insertCombined);
				if (isImporting)
					literalCache[literal] = id;
				return id;
			}
			
			return 0;
		}
		
		private int AddLiteral(Literal literal, StringBuilder buffer, bool insertCombined) {
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
					b.Append(',');
			}
			b.Append('(');
			b.Append(id);
			b.Append(',');
			EscapedAppend(b, literal.Value);
			b.Append(',');
			if (literal.Language != null)
				EscapedAppend(b, literal.Language);
			else
				b.Append("NULL");
			b.Append(',');
			if (literal.DataType != null)
				EscapedAppend(b, literal.DataType);
			else
				b.Append("NULL");
			b.Append(",\"");
			b.Append(GetLiteralHash(literal));
			b.Append("\")");
			if (!insertCombined)
				b.Append(';');
			
			if (buffer == null) {
				if (Debug) Console.Error.WriteLine(b.ToString());
				RunCommand(b.ToString());
			}
			
			return id;
		}

		private int GetEntityId(string uri, bool create, StringBuilder entityInsertBuffer, bool insertCombined, bool checkIfExists) {
			// Returns the resource ID associated with the URI.  If a resource
			// doesn't exist and create is true, a new resource is created,
			// otherwise 0 is returned.
			
			int id;	
			
			if (isImporting) {
				object idobj = entityCache[uri];
				if (idobj == null && !create) return 0;
				if (idobj != null) return (int)idobj;
			} else if (checkIfExists) {
				StringBuilder cmd = cmdBuffer; cmdBuffer.Length = 0;
				cmd.Append("SELECT id FROM ");
				cmd.Append(table);
				cmd.Append("_entities WHERE value =");
				EscapedAppend(cmd, uri);
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
					b.Append(',');
			}
			b.Append('(');
			b.Append(id);
			b.Append(',');
			EscapedAppend(b, uri);
			b.Append(')');
			if (!insertCombined)
				b.Append(';');
			
			if (entityInsertBuffer == null)
				RunCommand(b.ToString());
				
			// Add it to the URI map
					
			if (isImporting)
				entityCache[uri] = id;
			
			return id;
		}
		
		private int GetResourceId(Resource resource, bool create) {
			return GetResourceIdBuffer(resource, create, null, null, false);
		}
		
		private int GetResourceIdBuffer(Resource resource, bool create, StringBuilder literalInsertBuffer, StringBuilder entityInsertBuffer, bool insertCombined) {
			if (resource == null) return 0;
			
			if (resource is Literal) {
				Literal lit = (Literal)resource;
				return GetLiteralId(lit, create, literalInsertBuffer, insertCombined);
			}
			
			if (object.ReferenceEquals(resource, Statement.DefaultMeta))
				return 1;
			
			ResourceKey key = (ResourceKey)GetResourceKey(resource);
			if (key != null) return key.ResId;
			
			int id;
			
			if (resource.Uri != null) {
				id = GetEntityId(resource.Uri, create, entityInsertBuffer, insertCombined, true);
			} else {
				// This anonymous node didn't come from the database
				// since it didn't have a resource key.  If !create,
				// then just return 0 to signal the resource doesn't exist.
				if (!create) return 0;

				if (isImporting) {
					// Can just increment the counter.
					id = NextId();
				} else {
					// We need to reserve an id for this resource so that
					// this function returns other ids for other anonymous
					// resources.  To do that, we'll insert a record
					// into the entities table with a GUID for the entity.
					// Inserting something into the table also gives us
					// concurrency, I hope.  Then, once the resource is
					// added to the statements table, this row can be
					// removed.
					string guid = "semweb-bnode-guid://taubz.for.net,2006/"
						+ Guid.NewGuid().ToString("N");
					id = GetEntityId(guid, create, entityInsertBuffer, insertCombined, false);
					anonEntityHeldIds.Add(id);
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
			
			Entity ent;
			if (uri != null) {
				ent = new Entity(uri);
			} else {
				ent = new BNode();
			}
			
			SetResourceKey(ent, rk);
			
			if (cache != null)
				cache[rk] = ent;
				
			return ent;
		}
		
		public override void Add(Statement statement) {
			if (statement.AnyNull) throw new ArgumentNullException();
			
			metaEntities = null;

			if (addStatementBuffer != null) {
				addStatementBuffer.Add(statement);
				
				// This complicated code here adjusts the size of the add
				// buffer dynamically to maximize performance.
				int thresh = importAddBufferSize;
				if (importAddBufferRotation == 1) thresh += 100; // experiment with changing
				if (importAddBufferRotation == 2) thresh -= 100; // the buffer size
				
				if (addStatementBuffer.Count >= thresh) {
					DateTime start = DateTime.Now;
					RunAddBuffer();
					TimeSpan duration = DateTime.Now - start;
					
					// If there was an improvement in speed, per statement, on an 
					// experimental change in buffer size, keep the change.
					if (importAddBufferRotation != 0
						&& duration.TotalSeconds/thresh < importAddBufferTime.TotalSeconds/importAddBufferSize
						&& thresh >= 200 && thresh <= 4000)
						importAddBufferSize = thresh;

					importAddBufferTime = duration;
					importAddBufferRotation++;
					if (importAddBufferRotation == 3) importAddBufferRotation = 0;
				}
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
			addBuffer.Append('(');

			addBuffer.Append(subj);
			addBuffer.Append(',');
			addBuffer.Append(pred);
			addBuffer.Append(',');
			addBuffer.Append(objtype);
			addBuffer.Append(',');
			addBuffer.Append(obj);
			addBuffer.Append(',');
			addBuffer.Append(meta);
			addBuffer.Append("); ");
			
			RunCommand(addBuffer.ToString());
			
			// Remove any entries in the entities table
			// for anonymous nodes.
			if (anonEntityHeldIds.Count > 0) {
				addBuffer.Length = 0;
				addBuffer.Append("DELETE FROM ");
				addBuffer.Append(table);
				addBuffer.Append("_entities where id IN (");
				bool first = true;
				foreach (int id in anonEntityHeldIds) {
					if (!first) addBuffer.Append(','); first = false;
					addBuffer.Append(id);
				}
				addBuffer.Append(')');
				RunCommand(addBuffer.ToString());
				anonEntityHeldIds.Clear();
			}
		}
		
		private void RunAddBuffer() {
			if (addStatementBuffer == null || addStatementBuffer.Count == 0) return;
			
			bool insertCombined = SupportsInsertCombined;
			
			Init();
			
			// Prevent recursion through NextId=>StatementCount
			StatementList statements = addStatementBuffer;
			addStatementBuffer = null;
			
			try {
				// Prefetch the IDs of all entities mentioned in statements.
				StringBuilder cmd = new StringBuilder();
				cmd.Append("SELECT id, value FROM ");
				cmd.Append(table);
				cmd.Append("_entities WHERE value IN (");
				Hashtable entseen = new Hashtable();
				bool hasEnts = false;
				for (int i = 0; i < statements.Count; i++) {
					Statement s = (Statement)statements[i];
					for (int j = 0; j < 4; j++) {
						Entity ent = s.GetComponent(j) as Entity;
						if (ent == null || ent.Uri == null) continue;
						if (entityCache.ContainsKey(ent.Uri)) continue;
						if (entseen.ContainsKey(ent.Uri)) continue;
						
						if (hasEnts)
							cmd.Append(" , ");
						EscapedAppend(cmd, ent.Uri);
						hasEnts = true;
						entseen[ent.Uri] = ent.Uri;
					}
				}
				if (hasEnts) {
					cmd.Append(");");
					if (Debug) Console.Error.WriteLine(cmd.ToString());
					using (IDataReader reader = RunReader(cmd.ToString())) {
						while (reader.Read()) {
							int id = reader.GetInt32(0);
							string uri = AsString(reader[1]);
							entityCache[uri] = id;
						}
					}
				}
				
				
				// Prefetch the IDs of all literals mentioned in statements.
				cmd.Length = 0;
				cmd.Append("SELECT id, hash FROM ");
				cmd.Append(table);
				cmd.Append("_literals WHERE hash IN (");
				bool hasLiterals = false;
				Hashtable litseen = new Hashtable();
				for (int i = 0; i < statements.Count; i++) {
					Statement s = (Statement)statements[i];
					Literal lit = s.Object as Literal;
					if (lit == null) continue;
					if (literalCache.ContainsKey(lit)) continue;
					
					string hash = GetLiteralHash(lit);
					if (litseen.ContainsKey(hash)) continue;
					
					if (hasLiterals)
						cmd.Append(" , ");
					cmd.Append('"');
					cmd.Append(hash);
					cmd.Append('"');
					hasLiterals = true;
					litseen[hash] = lit;
				}
				if (hasLiterals) {
					cmd.Append(");");
					using (IDataReader reader = RunReader(cmd.ToString())) {
						while (reader.Read()) {
							int id = reader.GetInt32(0);
							string hash = AsString(reader[1]);
							Literal lit = (Literal)litseen[hash];
							literalCache[lit] = id;
						}
					}
				}
				
				StringBuilder entityInsertions = new StringBuilder();
				StringBuilder literalInsertions = new StringBuilder();
				
				cmd = new StringBuilder();
				if (insertCombined)
					cmd.Append(INSERT_INTO_STATEMENTS_VALUES);

				for (int i = 0; i < statements.Count; i++) {
					Statement statement = (Statement)statements[i];
				
					int subj = GetResourceIdBuffer(statement.Subject, true, literalInsertions, entityInsertions, insertCombined);
					int pred = GetResourceIdBuffer(statement.Predicate, true,  literalInsertions, entityInsertions, insertCombined);
					int objtype = ObjectType(statement.Object);
					int obj = GetResourceIdBuffer(statement.Object, true, literalInsertions, entityInsertions, insertCombined);
					int meta = GetResourceIdBuffer(statement.Meta, true, literalInsertions, entityInsertions, insertCombined);
					
					if (!insertCombined)
						cmd.Append(INSERT_INTO_STATEMENTS_VALUES);
					
					cmd.Append('(');
					cmd.Append(subj);
					cmd.Append(',');
					cmd.Append(pred);
					cmd.Append(',');
					cmd.Append(objtype);
					cmd.Append(',');
					cmd.Append(obj);
					cmd.Append(',');
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
			
			} finally {
				// Clear the array and reuse it.
				statements.Clear();
				addStatementBuffer = statements;
				entityCache.Clear();
				literalCache.Clear();
			}
		}
		
		public override void Remove(Statement template) {
			Init();
			RunAddBuffer();

			System.Text.StringBuilder cmd = new System.Text.StringBuilder("DELETE FROM ");
			cmd.Append(table);
			cmd.Append("_statements ");
			if (!WhereClause(template, cmd)) return;
			cmd.Append(';');
			
			RunCommand(cmd.ToString());
			
			statementsRemoved = true;
			metaEntities = null;
		}
		
		public override Entity[] GetEntities() {
			return GetAllEntities(fourcols);
		}
			
		public override Entity[] GetPredicates() {
			return GetAllEntities(predcol);
		}
		
		public override Entity[] GetMetas() {
			return GetAllEntities(metacol);
		}

		private Entity[] GetAllEntities(string[] cols) {
			RunAddBuffer();
			ArrayList ret = new ArrayList();
			Hashtable seen = new Hashtable();
			foreach (string col in cols) {
				using (IDataReader reader = RunReader("SELECT " + col + ", value FROM " + table + "_statements LEFT JOIN " + table + "_entities ON " + col + "=id " + (col == "object" ? " WHERE objecttype=0" : "") + " GROUP BY " + col + ";")) {
					while (reader.Read()) {
						int id = reader.GetInt32(0);
						if (id == 1 && col == "meta") continue; // don't return DefaultMeta in meta column.
						
						if (seen.ContainsKey(id)) continue;
						seen[id] = seen;
						
						string uri = AsString(reader[1]);
						ret.Add(MakeEntity(id, uri, null));
					}
				}
			}
			return (Entity[])ret.ToArray(typeof(Entity));;
		}
		
		private bool WhereItem(string col, Resource r, System.Text.StringBuilder cmd, bool and) {
			if (and) cmd.Append(" and ");
			
			if (r is MultiRes) {
				cmd.Append('(');
				cmd.Append(col);
				cmd.Append(" IN (");
				if (!AppendMultiRes((MultiRes)r, cmd)) return false;
				cmd.Append(" ))");
			} else {
				int id = GetResourceId(r, false);
				if (id == 0) return false;
				cmd.Append('(');
				cmd.Append(col);
				cmd.Append('=');
				cmd.Append(id);
				cmd.Append(')');
			}
			
			return true;
		}
		
		private bool AppendMultiRes(MultiRes r, StringBuilder cmd) {
			bool first = true;
			for (int i = 0; i < r.items.Length; i++) {
				int id = GetResourceId(r.items[i], false);
				if (id == 0) continue;
				if (!first) cmd.Append(','); first = false;
				cmd.Append(id);
			}
			if (first) return false; // none are in the store
			return true;
		}
		
		private bool WhereClause(Statement template, System.Text.StringBuilder cmd) {
			bool ww;
			return WhereClause(template.Subject, template.Predicate, template.Object, template.Meta, cmd, out ww);
		}

		private bool WhereClause(Resource templateSubject, Resource templatePredicate, Resource templateObject, Resource templateMeta, System.Text.StringBuilder cmd, out bool wroteWhere) {
			if (templateSubject == null && templatePredicate == null && templateObject == null && templateMeta == null) {
				wroteWhere = false;
				return true;
			}
			
			wroteWhere = true;
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
			if (r is long) return (int)(long)r;
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
		
		private static void AppendComma(StringBuilder builder, string text, bool comma) {
			if (comma)
				builder.Append(',');
			builder.Append(text);
		}
		
		internal struct SelectColumnFilter {
			public bool SubjectId, PredicateId, ObjectId, MetaId;
			public bool SubjectUri, PredicateUri, ObjectData, MetaUri;
		}
	
		private static void SelectFilterColumns(SelectColumnFilter filter, StringBuilder cmd) {
			bool f = true;
			
			if (filter.SubjectId) { cmd.Append("q.subject"); f = false; }
			if (filter.PredicateId) { AppendComma(cmd, "q.predicate", !f); f = false; }
			if (filter.ObjectId) { AppendComma(cmd, "q.object", !f); f = false; }
			if (filter.MetaId) { AppendComma(cmd, "q.meta", !f); f = false; }
			if (filter.SubjectUri) { AppendComma(cmd, "suri.value", !f); f = false; }
			if (filter.PredicateUri) { AppendComma(cmd, "puri.value", !f); f = false; }
			if (filter.ObjectData) { AppendComma(cmd, "q.objecttype, ouri.value, lit.value, lit.language, lit.datatype", !f); f = false; }
			if (filter.MetaUri) { AppendComma(cmd, "muri.value", !f); f = false; }
		}
		
		private void SelectFilterTables(SelectColumnFilter filter, StringBuilder cmd) {
			if (filter.SubjectUri) {
				cmd.Append(" LEFT JOIN ");
				cmd.Append(table);
				cmd.Append("_entities AS suri ON q.subject = suri.id");
			}
			if (filter.PredicateUri) {
				cmd.Append(" LEFT JOIN ");
				cmd.Append(table);
				cmd.Append("_entities AS puri ON q.predicate = puri.id");
			}
			if (filter.ObjectData) {
				cmd.Append(" LEFT JOIN ");
				cmd.Append(table);
				cmd.Append("_entities AS ouri ON q.object = ouri.id");

				cmd.Append(" LEFT JOIN ");
				cmd.Append(table);
				cmd.Append("_literals AS lit ON q.object=lit.id");
			}
			if (filter.MetaUri) {
				cmd.Append(" LEFT JOIN ");
				cmd.Append(table);
				cmd.Append("_entities AS muri ON q.meta = muri.id");
			}
		}

		public override void Select(SelectFilter filter, StatementSink result) {
			if (result == null) throw new ArgumentNullException();
			foreach (Entity[] s in SplitArray(filter.Subjects))
			foreach (Entity[] p in SplitArray(filter.Predicates))
			foreach (Resource[] o in SplitArray(filter.Objects))
			foreach (Entity[] m in SplitArray(filter.Metas))
			{
				Select(
					ToMultiRes(s),
					ToMultiRes(p),
					ToMultiRes(o),
					ToMultiRes(m),
					filter.LiteralFilters,
					result,
					filter.Limit); // hmm, repeated
			}
		}
		
		Resource[][] SplitArray(Resource[] e) {
			int lim = 1000;
			if (e == null || e.Length <= lim) {
				if (e is Entity[])
					return new Entity[][] { (Entity[])e };
				else
					return new Resource[][] { e };
			}
			int overflow = e.Length % lim;
			int n = (e.Length / lim) + ((overflow != 0) ? 1 : 0);
			Resource[][] ret;
			if (e is Entity[]) ret = new Entity[n][]; else ret = new Resource[n][];
			for (int i = 0; i < n; i++) {
				int c = lim;
				if (i == n-1 && overflow != 0) c = overflow;
				if (e is Entity[]) ret[i] = new Entity[c]; else ret[i] = new Resource[c];
				Array.Copy(e, i*lim, ret[i], 0, c);
			}
			return ret;
		}
		
		Resource ToMultiRes(Resource[] r) {
			if (r == null || r.Length == 0) return null;
			if (r.Length == 1) return r[0];
			return new MultiRes(r);
		}
		
		private class MultiRes : Resource {
			public MultiRes(Resource[] a) { items = a; }
			public Resource[] items;
			public override string Uri { get { return null; } }
			public bool ContainsLiterals() {
				foreach (Resource r in items)
					if (r is Literal)
						return true;
				return false;
			}
		}
		
		void CacheMultiObjects(Hashtable entMap, Resource obj) {
			if (!(obj is MultiRes)) return;
			foreach (Resource r in ((MultiRes)obj).items)
				entMap[GetResourceId(r, false)] = r;
		}
		
		public override void Select(Statement template, StatementSink result) {
			if (result == null) throw new ArgumentNullException();
			Select(template.Subject, template.Predicate, template.Object, template.Meta, null, result, 0);
		}

		private void Select(Resource templateSubject, Resource templatePredicate, Resource templateObject, Resource templateMeta, LiteralFilter[] litFilters, StatementSink result, int limit) {
			if (result == null) throw new ArgumentNullException();
	
			lock (syncroot) {
			
			Init();
			RunAddBuffer();
			
			// Don't select on columns that we already know from the template.
			// But grab the URIs and literal values for MultiRes selection.
			SelectColumnFilter columns = new SelectColumnFilter();
			columns.SubjectId = (templateSubject == null) || templateSubject is MultiRes;
			columns.PredicateId = (templatePredicate == null) || templatePredicate is MultiRes;
			columns.ObjectId = (templateObject == null) || templateObject is MultiRes;
			columns.MetaId = (templateMeta == null) || templateMeta is MultiRes;
			columns.SubjectUri = templateSubject == null;
			columns.PredicateUri = templatePredicate == null;
			columns.ObjectData = templateObject == null || (templateObject is MultiRes && ((MultiRes)templateObject).ContainsLiterals());
			columns.MetaUri = templateMeta == null;
			
			// Meta URIs tend to be repeated a lot, so we don't
			// want to ever select them from the database.
			// This preloads them, although it makes the first
			// select quite slow.
			if (templateMeta == null && SupportsSubquery) {
				LoadMetaEntities();
				columns.MetaUri = false;
			}
			
			// Have to select something
			if (!columns.SubjectId && !columns.PredicateId && !columns.ObjectId && !columns.MetaId)
				columns.SubjectId = true;
				
			// SQLite has a problem with LEFT JOIN: When a condition is made on the
			// first table in the ON clause (q.objecttype=0/1), when it fails,
			// it excludes the row from the first table, whereas it should only
			// exclude the results of the join.
						
			System.Text.StringBuilder cmd = new System.Text.StringBuilder("SELECT ");
			if (!SupportsNoDuplicates)
				cmd.Append("DISTINCT ");
			SelectFilterColumns(columns, cmd);
			cmd.Append(" FROM ");
			cmd.Append(table);
			cmd.Append("_statements AS q");
			SelectFilterTables(columns, cmd);
			cmd.Append(' ');
			
			bool wroteWhere;
			if (!WhereClause(templateSubject, templatePredicate, templateObject, templateMeta, cmd, out wroteWhere)) return;
			
			// Transform literal filters into SQL.
			if (litFilters != null) {
				foreach (LiteralFilter f in litFilters) {
					string s = FilterToSQL(f, "lit.value");
					if (s != null) {
						if (!wroteWhere) { cmd.Append(" WHERE "); wroteWhere = true; }
						else { cmd.Append(" AND "); }
						cmd.Append(' ');
						cmd.Append(s);
					}
				}
			}
			
			if (limit >= 1) {
				cmd.Append(" LIMIT ");
				cmd.Append(limit);
			}

			cmd.Append(';');
			
			if (Debug) {
				string cmd2 = cmd.ToString();
				//if (cmd2.Length > 80) cmd2 = cmd2.Substring(0, 80);
				Console.Error.WriteLine(cmd2);
			}
			
			Hashtable entMap = new Hashtable();
			
			// Be sure if a MultiRes is involved we hash the
			// ids of the entities so we can return them
			// without creating new ones.
			CacheMultiObjects(entMap, templateSubject);
			CacheMultiObjects(entMap, templatePredicate);
			CacheMultiObjects(entMap, templateObject);
			CacheMultiObjects(entMap, templateMeta);
			
			using (IDataReader reader = RunReader(cmd.ToString())) {
				while (reader.Read()) {
					int col = 0;
					int sid = -1, pid = -1, ot = -1, oid = -1, mid = -1;
					string suri = null, puri = null, ouri = null, muri = null;
					string lv = null, ll = null, ld = null;
					
					if (columns.SubjectId) { sid = reader.GetInt32(col++); }
					if (columns.PredicateId) { pid = reader.GetInt32(col++); }
					if (columns.ObjectId) { oid = reader.GetInt32(col++); }
					if (columns.MetaId) { mid = reader.GetInt32(col++); }
					
					if (columns.SubjectUri) { suri = AsString(reader[col++]); }
					if (columns.PredicateUri) { puri = AsString(reader[col++]); }
					if (columns.ObjectData) { ot = reader.GetInt32(col++); ouri = AsString(reader[col++]); lv = AsString(reader[col++]); ll = AsString(reader[col++]); ld = AsString(reader[col++]);}
					if (columns.MetaUri) { muri = AsString(reader[col++]); }
					
					Entity subject = GetSelectedEntity(sid, suri, templateSubject, columns.SubjectId, columns.SubjectUri, entMap);
					Entity predicate = GetSelectedEntity(pid, puri, templatePredicate, columns.PredicateId, columns.PredicateUri, entMap);
					Resource objec = GetSelectedResource(oid, ot, ouri, lv, ll, ld, templateObject, columns.ObjectId, columns.ObjectData, entMap);
					Entity meta = GetSelectedEntity(mid, muri, templateMeta, columns.MetaId, columns.MetaUri, templateMeta != null ? entMap : metaEntities);

					if (litFilters != null && !LiteralFilter.MatchesFilters(objec, litFilters, this))
						continue;
						
					bool ret = result.Add(new Statement(subject, predicate, objec, meta));
					if (!ret) break;
				}
			}
			
			} // lock
		}

		Entity GetSelectedEntity(int id, string uri, Resource given, bool idSelected, bool uriSelected, Hashtable entMap) {
			if (!idSelected) return (Entity)given;
			if (!uriSelected) {
				Entity ent = (Entity)entMap[id];
				if (ent != null)
					return ent; // had a URI so was precached, or was otherwise precached
				else // didn't have a URI
					return MakeEntity(id, null, entMap);
			}
			return MakeEntity(id, uri, entMap);
		}
		
		Resource GetSelectedResource(int id, int type, string uri, string lv, string ll, string ld, Resource given, bool idSelected, bool uriSelected, Hashtable entMap) {
			if (!idSelected) return (Resource)given;
			if (!uriSelected) return (Resource)entMap[id];
			if (type == 0)
				return MakeEntity(id, uri, entMap);
			else
				return new Literal(lv, ll, ld);
		}

		private string FilterToSQL(LiteralFilter filter, string col) {
			if (filter is SemWeb.Filters.StringCompareFilter) {
				SemWeb.Filters.StringCompareFilter f = (SemWeb.Filters.StringCompareFilter)filter;
				return col + FilterOpToSQL(f.Type) + Escape(f.Pattern, true);
			}
			if (filter is SemWeb.Filters.StringContainsFilter) {
				SemWeb.Filters.StringContainsFilter f = (SemWeb.Filters.StringContainsFilter)filter;
				return col + " LIKE " + quote + "%" + Escape(f.Pattern, false).Replace("%", "\\%") + "%" + quote;
			}
			if (filter is SemWeb.Filters.StringStartsWithFilter) {
				SemWeb.Filters.StringStartsWithFilter f = (SemWeb.Filters.StringStartsWithFilter)filter;
				return col + " LIKE " + quote + Escape(f.Pattern, false).Replace("%", "\\%") + "%" + quote;
			}
			if (filter is SemWeb.Filters.NumericCompareFilter) {
				SemWeb.Filters.NumericCompareFilter f = (SemWeb.Filters.NumericCompareFilter)filter;
				return col + FilterOpToSQL(f.Type) + f.Number;
			}
			return null;
		}
		
		private string FilterOpToSQL(LiteralFilter.CompType op) {
			switch (op) {
			case LiteralFilter.CompType.LT: return " < ";
			case LiteralFilter.CompType.LE: return " <= ";
			case LiteralFilter.CompType.NE: return " <> ";
			case LiteralFilter.CompType.EQ: return " = ";
			case LiteralFilter.CompType.GT: return " > ";
			case LiteralFilter.CompType.GE: return " >= ";
			default: throw new ArgumentException(op.ToString());
			}			
		}
		
		private void LoadMetaEntities() {
			if (metaEntities != null) return;
			metaEntities = new Hashtable();
			// this misses meta entities that are anonymous, but that's ok
			using (IDataReader reader = RunReader("select id, value from " + table + "_entities where id in (select distinct meta from " + table + "_statements)")) {
				while (reader.Read()) {
					int id = reader.GetInt32(0);
					string uri = reader.GetString(1);
					metaEntities[id] = MakeEntity(id, uri, null);
				}
			}
		}
		
		private string Escape(string str, bool quotes) {
			if (str == null) return "NULL";
			StringBuilder b = new StringBuilder();
			EscapedAppend(b, str, quotes);
			return b.ToString();
		}
		
		protected void EscapedAppend(StringBuilder b, string str) {
			EscapedAppend(b, str, true);
		}

		protected virtual char GetQuoteChar() {
			return '\"';
		}
		protected virtual void EscapedAppend(StringBuilder b, string str, bool quotes) {
			if (quotes) b.Append(quote);
			for (int i = 0; i < str.Length; i++) {
				char c = str[i];
				switch (c) {
					case '\n': b.Append("\\n"); break;
					case '\\':
					case '\"':
					case '*':
						b.Append('\\');
						b.Append(c);
						break;
					default:
						b.Append(c);
						break;
				}
			}
			if (quotes) b.Append(quote);
		}
		
		/*internal static void Escape(StringBuilder b) {
			b.Replace("\\", "\\\\");
			b.Replace("\"", "\\\"");
			b.Replace("\n", "\\n");
			b.Replace("%", "\\%");
			b.Replace("*", "\\*");
		}*/

		public override void Import(StatementSource source) {
			if (source == null) throw new ArgumentNullException();
			if (isImporting) throw new InvalidOperationException("Store is already importing.");
			
			Init();
			RunAddBuffer();
			
			cachedNextId = -1;
			addStatementBuffer = new StatementList();
			
			BeginTransaction();
			
			try {
				isImporting = true;
				base.Import(source);
			} finally {
				RunAddBuffer();
				EndTransaction();
				
				addStatementBuffer = null;
				isImporting = false;

				entityCache.Clear();
				literalCache.Clear();
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
				cmd.Append('=');
				cmd.Append(id);
				if (!WhereItem(col, a, cmd, false)) return;
				cmd.Append(';');
				RunCommand(cmd.ToString());
			}

			metaEntities = null;
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
			cmd.Append(' ');
			
			if (!WhereClause(find, cmd))
				return;
			
			RunCommand(cmd.ToString());
			metaEntities = null;
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
			} catch (FormatException) {
				return def;
			}
		}
		
		private string RunScalarString(string sql) {
			object ret = RunScalar(sql);
			if (ret == null) return null;
			if (ret is string) return (string)ret;
			if (ret is byte[]) return System.Text.Encoding.UTF8.GetString((byte[])ret);
			throw new FormatException("SQL store returned a literal value as " + ret);
		}
		
		public override void Close() {
			if (statementsRemoved) {
				RunCommand("DELETE FROM " + table + "_literals where (select count(*) from " + table + "_statements where object=id) = 0 and id > 0");
				RunCommand("DELETE FROM " + table + "_entities where (select count(*) from " + table + "_statements where subject=id) = 0 and (select count(*) from " + table + "_statements where predicate=id) = 0 and (select count(*) from " + table + "_statements where object=id) = 0 and (select count(*) from " + table + "_statements where meta=id) = 0 ;");
			}
		}

		protected virtual void CreateTable() {
			foreach (string cmd in GetCreateTableCommands(table)) {
				try {
					RunCommand(cmd);
				} catch (Exception e) {
					if (Debug) Console.Error.WriteLine(e);
				}
			}
		}
		
		protected virtual void CreateIndexes() {
			foreach (string cmd in GetCreateIndexCommands(table)) {
				try {
					RunCommand(cmd);
				} catch (Exception e) {
					if (Debug) Console.Error.WriteLine(e);
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
				"(id INT NOT NULL, value BLOB NOT NULL, language TEXT, datatype TEXT, hash BINARY(28), PRIMARY KEY(id));",
				
				"CREATE TABLE " + table + "_entities" +
				"(id INT NOT NULL, value BLOB NOT NULL, PRIMARY KEY(id));"
				};
		}
		
		internal static string[] GetCreateIndexCommands(string table) {
			return new string[] {
				"CREATE UNIQUE INDEX subject_full_index ON " + table + "_statements(subject, predicate, object, meta, objecttype);",
				"CREATE INDEX predicate_index ON " + table + "_statements(predicate);",
				"CREATE INDEX object_index ON " + table + "_statements(object);",
				"CREATE INDEX meta_index ON " + table + "_statements(meta);",
			
				"CREATE UNIQUE INDEX literal_index ON " + table + "_literals(hash);",
				"CREATE UNIQUE INDEX entity_index ON " + table + "_entities(value(255));"
				};
		}
	}
	
}
