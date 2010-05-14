using System;
using System.Collections;
using System.IO;

using SemWeb;
using SemWeb.Filters;
using SemWeb.Stores;
using SemWeb.Util;

namespace SemWeb.Query {

	public class QueryFormatException : ApplicationException {
		public QueryFormatException(string message) : base(message) { }
		public QueryFormatException(string message, Exception cause) : base(message, cause) { }
	}

	public class QueryExecutionException : ApplicationException {
		public QueryExecutionException(string message) : base(message) { }
		public QueryExecutionException(string message, Exception cause) : base(message, cause) { }
	}
	
	public abstract class RdfFunction {
		public abstract string Uri { get; }
		public abstract Resource Evaluate(Resource[] args);	
	
	}

	public abstract class Query {
		int start = 0;
		int limit = -1;
		Entity queryMeta = null;
		
		public int ReturnStart { get { return start; } set { start = value; if (start < 0) start = 0; } }
		
		public int ReturnLimit { get { return limit; } set { limit = value; } }
		
		public Entity QueryMeta { get { return queryMeta; } set { queryMeta = value; } }
		
		public virtual void Run(SelectableSource source, TextWriter output) {
			Run(source, new SparqlXmlQuerySink(output));
		}

		public abstract void Run(SelectableSource source, QueryResultSink resultsink);

		public abstract string GetExplanation();
	}

	public class GraphMatch : Query {
		// Setup information
	
		ArrayList setupVariablesDistinct = new ArrayList();
		ArrayList setupValueFilters = new ArrayList();
		ArrayList setupStatements = new ArrayList();
		ArrayList setupOptionalStatements = new ArrayList();
		
		// Query model information
		
		bool init = false;
		object sync = new object();
		Variable[] variables;
		SemWeb.Variable[] variableEntities;
		QueryStatement[][] statements;
		ArrayList novariablestatements = new ArrayList();
		
		// contains functional and inverse functional properties
		ResSet fps = new ResSet(),
		          ifps = new ResSet();
		
		private struct Variable {
			public SemWeb.Variable Entity;
			public LiteralFilter[] Filters;
		}
		
		private struct VarOrAnchor {
			public bool IsVariable;
			public int VarIndex;
			public Resource Anchor;
			public Resource[] ArrayOfAnchor;
			
			public override string ToString() {
				if (IsVariable)
					return "?" + VarIndex;
				else
					return Anchor.ToString();
			}
			
			public Resource[] GetValues(QueryResult union, bool entities) {
				if (!IsVariable) {
					if (entities)
						return new Entity[] { (Entity)Anchor };
					else
						return ArrayOfAnchor;
				} else {
					if (union.Bindings[VarIndex] == null) return null;
					Resource[] res = union.Bindings[VarIndex].ToArray();
					if (!entities) return res;
					
					ArrayList ret = new ArrayList();
					foreach (Resource r in res)
						if (r is Entity)
							ret.Add(r);
					return (Entity[])ret.ToArray(typeof(Entity));
				}
			}
		}
		
		private class QueryStatement { // class because of use with IComparer
			public bool Optional;
			public VarOrAnchor 
				Subject,
				Predicate,
				Object;
			
			public int NumVars() {
				return (Subject.IsVariable ? 1 : 0)
					+ (Predicate.IsVariable ? 1 : 0)
					+ (Object.IsVariable ? 1 : 0);
			}
			
			public override string ToString() {
				return Subject + " " + Predicate + " " + Object;
			}
		}
		
		class QueryResult {
			public ResSet[] Bindings;
			public bool[] StatementMatched;
			
			public QueryResult(GraphMatch q) {
				Bindings = new ResSet[q.variables.Length];
				StatementMatched = new bool[q.statements.Length];
			}
			private QueryResult(int x, int y) {
				Bindings = new ResSet[x];
				StatementMatched = new bool[y];
			}
			public void Add(QueryStatement qs, Statement bs) {
				if (qs.Subject.IsVariable) Add(qs.Subject.VarIndex, bs.Subject);
				if (qs.Predicate.IsVariable) Add(qs.Predicate.VarIndex, bs.Predicate);
				if (qs.Object.IsVariable) Add(qs.Object.VarIndex, bs.Object);
			}
			void Add(int varIndex, Resource binding) {
				if (Bindings[varIndex] == null) Bindings[varIndex] = new ResSet();
				Bindings[varIndex].Add(binding);
			}
			public void Clear(QueryStatement qs) {
				if (qs.Subject.IsVariable && Bindings[qs.Subject.VarIndex] != null) Bindings[qs.Subject.VarIndex].Clear();
				if (qs.Predicate.IsVariable && Bindings[qs.Predicate.VarIndex] != null) Bindings[qs.Predicate.VarIndex].Clear();
				if (qs.Object.IsVariable && Bindings[qs.Object.VarIndex] != null) Bindings[qs.Object.VarIndex].Clear();
			}
			public void Set(QueryStatement qs, Statement bs) {
				if (qs.Subject.IsVariable) Set(qs.Subject.VarIndex, bs.Subject);
				if (qs.Predicate.IsVariable) Set(qs.Predicate.VarIndex, bs.Predicate);
				if (qs.Object.IsVariable) Set(qs.Object.VarIndex, bs.Object);
			}
			void Set(int varIndex, Resource binding) {
				if (Bindings[varIndex] == null) Bindings[varIndex] = new ResSet();
				else Bindings[varIndex].Clear();
				Bindings[varIndex].Add(binding);
			}
			public QueryResult Clone() {
				QueryResult r = new QueryResult(Bindings.Length, StatementMatched.Length);
				for (int i = 0; i < Bindings.Length; i++)
					if (Bindings[i] != null)
						r.Bindings[i] = Bindings[i].Clone();
				for (int i = 0; i < StatementMatched.Length; i++)
					r.StatementMatched[i] = StatementMatched[i];
				return r;
			}
		}
		
		class BindingSet {
			public ArrayList Results = new ArrayList();
			public QueryResult Union;
			
			public BindingSet(GraphMatch q) {
				Union = new QueryResult(q);
			}
		}
		
		public void MakeDistinct(BNode a, BNode b) {
			SetupVariablesDistinct d = new SetupVariablesDistinct();
			d.a = a;
			d.b = b;
			setupVariablesDistinct.Add(d);
		}
		
		public void AddValueFilter(Entity entity, LiteralFilter filter) {
			SetupValueFilter d = new SetupValueFilter();
			d.a = entity;
			d.b = filter;
			setupValueFilters.Add(d);
		}
		
		public void AddEdge(Statement filter) {
			setupStatements.Add(filter);
		}

		public void AddOptionalEdge(Statement filter) {
			setupOptionalStatements.Add(filter);
		}
		
		private class SetupVariablesDistinct {
			public Entity a, b;
		}
		private class SetupValueFilter {
			public Entity a;
			public LiteralFilter b;
		}
		
		private void CheckInit() {
			lock (sync) {
				if (!init) {
					Init();
					init = true;
				}
			}
		}

		private static Entity qLimit = "http://purl.oclc.org/NET/rsquary/returnLimit";
		private static Entity qStart = "http://purl.oclc.org/NET/rsquary/returnStart";
		private static Entity qDistinctFrom = "http://purl.oclc.org/NET/rsquary/distinctFrom";
		private static Entity qOptional = "http://purl.oclc.org/NET/rsquary/optional";
		
		public GraphMatch() {
		}
		
		public GraphMatch(RdfReader query) :
			this(new MemoryStore(query),
				query.BaseUri == null ? null : new Entity(query.BaseUri)) {
		}

		public GraphMatch(Store queryModel) : this(queryModel, null) {
		}
		
		private GraphMatch(Store queryModel, Entity queryNode) {
			// Find the query options
			if (queryNode != null) {
				ReturnStart = GetIntOption(queryModel, queryNode, qStart);
				ReturnLimit = GetIntOption(queryModel, queryNode, qLimit);
			}

			// Search the query for 'distinct' predicates between variables.
			foreach (Statement s in queryModel.Select(new Statement(null, qDistinctFrom, null))) {
				if (!(s.Object is BNode)) continue;
				MakeDistinct((BNode)s.Subject, (BNode)s.Object);
			}
			
			// Add all statements except the query predicates and value filters into a
			// new store with just the statements relevant to the search.
			foreach (Statement s in queryModel.Select(Statement.All)) {
				if (IsQueryPredicate(s.Predicate)) continue;
				
				/*if (s.Predicate.Uri != null && extraValueFilters != null && extraValueFilters.Contains(s.Predicate.Uri)) {
					ValueFilterFactory f = (ValueFilterFactory)extraValueFilters[s.Predicate.Uri];
					AddValueFilter(s.Subject, f.GetValueFilter(s.Predicate.Uri, s.Object));
					continue;
				} else {
					ValueFilter f = ValueFilter.GetValueFilter(s.Predicate, s.Object);
					if (f != null) {
						AddValueFilter(s.Subject, f);
						continue;
					}
				}*/
				
				if (s.Meta == Statement.DefaultMeta)
					AddEdge(s);
				else if (queryNode != null && queryModel.Contains(new Statement(queryNode, qOptional, s.Meta)))
					AddOptionalEdge(s);
			}
		}
		
		private int GetIntOption(Store queryModel, Entity query, Entity predicate) {
			Resource[] rr = queryModel.SelectObjects(query, predicate);
			if (rr.Length == 0) return -1;
			Resource r = rr[0];
			if (r == null || !(r is Literal)) return -1;
			try {
				return int.Parse(((Literal)r).Value);
			} catch (Exception) {
				return -1;
			}
		}		

		private bool IsQueryPredicate(Entity e) {
			if (e == qDistinctFrom) return true;
			if (e == qLimit) return true;
			if (e == qStart) return true;
			if (e == qOptional) return true;
			return false;
		}
		
		public override string GetExplanation() {
			CheckInit();
			string ret = "Query:\n";
			foreach (Statement s in novariablestatements)
				ret += " Check: " + s + "\n";
			foreach (QueryStatement[] sgroup in statements) {
				ret += " ";
				if (sgroup.Length != 1)
					ret += "{";
				foreach (QueryStatement s in sgroup) {
					ret += s.ToString();
					if (s.Optional) ret += " (Optional)";
					if (sgroup.Length != 1)
						ret += " & ";
				}
				if (sgroup.Length != 1)
					ret += "}";
				ret += "\n";
			}
			return ret;
		}
		
		public override void Run(SelectableSource targetModel, QueryResultSink result) {
			CheckInit();
			
			foreach (Statement s in novariablestatements)
				if (!targetModel.Contains(s))
					return;
			
			VariableBinding[] finalbindings = new VariableBinding[variables.Length];
			for (int i = 0; i < variables.Length; i++)
				finalbindings[i].Variable = variableEntities[i];
			
			result.Init(finalbindings, true, false);
			
			Debug("Begnning Query");
			
			BindingSet bindings = new BindingSet(this);
			for (int group = 0; group < statements.Length; group++) {
				bool ret = Query(group, bindings, targetModel);
				if (!ret) {
					// A false return value indicates the query
					// certainly failed -- a non-optional statement
					// failed to match at all.
					result.Finished();
					return;
				}
			}

			int ctr = -1;
			foreach (QueryResult r in bindings.Results) {
				Permutation permutation = new Permutation(r.Bindings);
				do {
					ctr++;
					if (ctr < ReturnStart) continue;
					for (int i = 0; i < variables.Length; i++)
						finalbindings[i].Target = permutation[i];
					result.Add(finalbindings);
					if (ReturnLimit != -1 && ctr == ReturnStart+ReturnLimit) break;	
				} while (permutation.Next());
				if (ReturnLimit != -1 && ctr == ReturnStart+ReturnLimit) break;	
			}

			
			result.Finished();
		}
		
		class Permutation {
			public int[] index;
			public Resource[][] values;
			
			public Resource this[int i] {
				get {
					return values[i][index[i]];
				}
			}
			
			public Permutation(ResSet[] bindings) {
				index = new int[bindings.Length];
				values = new Resource[bindings.Length][];
				for (int i = 0; i < bindings.Length; i++) {
					values[i] = new Resource[bindings[i] == null ? 1 : bindings[i].Count];
					if (bindings[i] != null) {
						int ctr = 0;
						foreach (Resource r in bindings[i])
							values[i][ctr++] = r;
					}
				}
			}
			public bool Next() {
				for (int i = 0; i < index.Length; i++) {
					index[i]++;
					if (index[i] < values[i].Length) break;
					
					index[i] = 0;
					if (i == index.Length-1) return false;
				}
				return true;
			}
		}
		
		private void Debug(string message) {
			//Console.Error.WriteLine(message);
		}
		
		class BindingEnumerator {
			IEnumerator[] loops = new IEnumerator[3];
			int loop = 0;
			
			public BindingEnumerator(QueryStatement qs, QueryResult bindings) {
				loops[0] = GetBindings(qs.Subject, bindings);
				loops[1] = GetBindings(qs.Predicate, bindings);
				loops[2] = GetBindings(qs.Object, bindings);
			}
			
			public bool MoveNext(out Entity s, out Entity p, out Resource o) {
				while (true) {
					bool b = loops[loop].MoveNext();
					if (!b) {
						 if (loop == 0) { s = null; p = null; o = null; return false; }
						 loops[loop].Reset();
						 loop--;
						 continue;
					}
					
					if (loop <= 1) {
						object obj = loops[loop].Current;
						if (obj != null && !(obj is Entity)) continue;
					}
					
					if (loop < 2) { loop++; continue; }
					
					s = loops[0].Current as Entity; 
					p = loops[1].Current as Entity; 
					o = loops[2].Current as Resource;
					return true; 
				}
			}
		}

		private bool Query(int groupindex, BindingSet bindings, SelectableSource targetModel) {
			QueryStatement[] group = statements[groupindex];
			
			QueryStatement qs = group[0];
			
			int numMultiplyBound = IsMultiplyBound(qs.Subject, bindings)
				+ IsMultiplyBound(qs.Predicate, bindings)
				+ IsMultiplyBound(qs.Object, bindings);
			
			if (numMultiplyBound >= 1) {
				// If there is one or more multiply-bound variable,
				// then we need to iterate through the permutations
				// of the variables in the statement.
				
				Debug(qs.ToString() + " Something Multiply Bound");
				
				MemoryStore matches = new MemoryStore();
				targetModel.Select(
					new SelectFilter(
						(Entity[])qs.Subject.GetValues(bindings.Union, true),
						(Entity[])qs.Predicate.GetValues(bindings.Union, true),
						qs.Object.GetValues(bindings.Union, false),
						QueryMeta == null ? null : new Entity[] { QueryMeta }
						),
					new ClearMetaDupCheck(matches));
				
				Debug("\t" + matches.StatementCount + " Matches");
				
				if (matches.StatementCount == 0) {
					// This statement doesn't match any of
					// the existing bindings.  If this was
					// optional, preserve the bindings.
					return qs.Optional;
				}
				
				// We need to preserve the pairings of
				// the multiply bound variable with the matching
				// statements.
				
				ArrayList newbindings = new ArrayList();
				
				if (!qs.Optional) bindings.Union.Clear(qs);
				
				foreach (QueryResult binding in bindings.Results) {
					// Break apart the permutations in this binding.
					BindingEnumerator enumer2 = new BindingEnumerator(qs, binding);
					Entity s, p;
					Resource o;
					while (enumer2.MoveNext(out s, out p, out o)) {
						// Get the matching statements from the union query
						Statement bs = new Statement(s, p, o);
						MemoryStore innermatches = matches.Select(bs).Load();
						
						// If no matches, the binding didn't match the filter.
						if (innermatches.StatementCount == 0) {
							if (qs.Optional) {
								// Preserve the binding.
								QueryResult bc = binding.Clone();
								bc.Set(qs, bs);
								newbindings.Add(bc);
								continue;
							} else {
								// Toss out the binding.
								continue;
							}
						}
						
						for (int si = 0; si < innermatches.StatementCount; si++) {
							Statement m = innermatches[si];
							if (!MatchesFilters(m, qs, targetModel)) {
								if (qs.Optional) {
									QueryResult bc = binding.Clone();
									bc.Set(qs, bs);
									newbindings.Add(bc);
								}
								continue;
							}
							bindings.Union.Add(qs, m);
							
							QueryResult r = binding.Clone();
							r.Set(qs, m);
							r.StatementMatched[groupindex] = true;
							newbindings.Add(r);
						}
					}
				}
				
				bindings.Results = newbindings;
				
			} else {
				// There are no multiply bound variables, but if
				// there are more than two unbound variables,
				// we need to be sure to preserve the pairings
				// of the matching values.
			
				int numUnbound = IsUnbound(qs.Subject, bindings)
					+ IsUnbound(qs.Predicate, bindings)
					+ IsUnbound(qs.Object, bindings);
					
				bool sunbound = IsUnbound(qs.Subject, bindings) == 1;
				bool punbound = IsUnbound(qs.Predicate, bindings) == 1;
				bool ounbound = IsUnbound(qs.Object, bindings) == 1;
				
				Statement s = GetStatement(qs, bindings);
				
				// If we couldn't get a statement out of this,
				// then if this was not an optional filter,
				// fail.  If this was optional, don't change
				// the bindings any. 
				if (s == StatementFailed) return qs.Optional;
				
				if (numUnbound == 0) {
					Debug(qs.ToString() + " All bound");
					
					// All variables are singly bound already.
					// We can just test if the statement exists.
					if (targetModel.Contains(s)) {
						// Mark each binding that it matched this statement.
						foreach (QueryResult r in bindings.Results)
							r.StatementMatched[groupindex] = true;
					} else {
						return qs.Optional;
					}
				
				} else if (numUnbound == 1) {
					Debug(qs.ToString() + " 1 Unbound");
				
					// There is just one unbound variable.  The others
					// are not multiply bound, so they must be uniquely
					// bound (but they may not be bound in all results).
					// Run a combined select to find all possible values
					// of the unbound variable at once, and set these to
					// be the values of the variable for matching results.
					
					ResSet values = new ResSet();
					MemoryStore ms = new MemoryStore();
					targetModel.Select(s, ms);
					for (int si = 0; si < ms.StatementCount; si++) {
						Statement match = ms[si];
						if (!MatchesFilters(match, qs, targetModel)) continue;
						if (sunbound) values.Add(match.Subject);
						if (punbound) values.Add(match.Predicate);
						if (ounbound) values.Add(match.Object);
					}
					
					Debug("\t" + values.Count + " matches");
					
					if (values.Count == 0)
						return qs.Optional;
						
					int varIndex = -1;
					if (sunbound) varIndex = qs.Subject.VarIndex;
					if (punbound) varIndex = qs.Predicate.VarIndex;
					if (ounbound) varIndex = qs.Object.VarIndex;
					
					if (bindings.Results.Count == 0)
						bindings.Results.Add(new QueryResult(this));
					
					bindings.Union.Bindings[varIndex] = new ResSet();
					foreach (Resource r in values)
						bindings.Union.Bindings[varIndex].Add(r);
						
					foreach (QueryResult r in bindings.Results) {
						// Check that the bound variables are bound in this result.
						// If it is bound, it will be bound to the correct resource,
						// but it might not be bound at all if an optional statement
						// failed to match -- in which case, don't modify the binding.
						if (qs.Subject.IsVariable && !sunbound && r.Bindings[qs.Subject.VarIndex] == null) continue;
						if (qs.Predicate.IsVariable && !punbound && r.Bindings[qs.Predicate.VarIndex] == null) continue;
						if (qs.Object.IsVariable && !ounbound && r.Bindings[qs.Object.VarIndex] == null) continue;
					
						r.Bindings[varIndex] = values;
						r.StatementMatched[groupindex] = true;
					}
					
				} else {
					// There are two or more unbound variables, the
					// third variable being uniquely bound, if bound.
					// Keep track of the pairing of unbound variables.
					
					if (numUnbound == 3)
						throw new QueryExecutionException("Query would select all statements in the store.");
					
					Debug(qs.ToString() + " 2 or 3 Unbound");
				
					if (bindings.Results.Count == 0)
						bindings.Results.Add(new QueryResult(this));
						
					ArrayList newbindings = new ArrayList();
					MemoryStore ms = new MemoryStore();
					targetModel.Select(s, ms);
					for (int si = 0; si < ms.StatementCount; si++) {
						Statement match = ms[si];
						if (!MatchesFilters(match, qs, targetModel)) continue;
						bindings.Union.Add(qs, match);
						foreach (QueryResult r in bindings.Results) {
							if (numUnbound == 2) {
								// Check that the bound variable is bound in this result.
								// If it is bound, it will be bound to the correct resource,
								// but it might not be bound at all if an optional statement
								// failed to match -- in which case, preserve the binding if
								// this was an optional statement.
								bool matches = true;
								if (qs.Subject.IsVariable && !sunbound && r.Bindings[qs.Subject.VarIndex] == null) matches = false;
								if (qs.Predicate.IsVariable && !punbound && r.Bindings[qs.Predicate.VarIndex] == null) matches = false;
								if (qs.Object.IsVariable && !ounbound && r.Bindings[qs.Object.VarIndex] == null) matches = false;
								if (!matches) {
									if (qs.Optional)
										newbindings.Add(r);
									continue;
								}
							}
						
							QueryResult r2 = r.Clone();
							r2.Add(qs, match);
							r2.StatementMatched[groupindex] = true;
							newbindings.Add(r2);
						}
					}
					if (newbindings.Count == 0)
						return qs.Optional; // don't clear out bindings if this was optional and it failed
					bindings.Results = newbindings;
				}
			}
			
			return true;
		}
		
		static Resource[] ResourceArrayNull = new Resource[] { null };
		
		private static IEnumerator GetBindings(VarOrAnchor e, QueryResult bindings) {
			if (!e.IsVariable) {
				if (e.ArrayOfAnchor == null)
					e.ArrayOfAnchor = new Resource[] { e.Anchor };
				return e.ArrayOfAnchor.GetEnumerator();
			}
			if (bindings.Bindings[e.VarIndex] == null) return ResourceArrayNull.GetEnumerator();
			return bindings.Bindings[e.VarIndex].Items.GetEnumerator();
		}
		private int IsMultiplyBound(VarOrAnchor e, BindingSet bindings) {
			if (!e.IsVariable) return 0;
			if (bindings.Union.Bindings[e.VarIndex] == null) return 0;
			if (bindings.Union.Bindings[e.VarIndex].Items.Count == 1) return 0;
			return 1;
		}
		private int IsUnbound(VarOrAnchor e, BindingSet bindings) {
			if (!e.IsVariable) return 0;
			if (bindings.Union.Bindings[e.VarIndex] == null) return 1;
			return 0;
		}
		
		private Resource GetUniqueBinding(VarOrAnchor e, BindingSet bindings) {
			if (!e.IsVariable) return e.Anchor;
			if (bindings.Union.Bindings[e.VarIndex] == null || bindings.Union.Bindings[e.VarIndex].Count == 0) return null;
			if (bindings.Union.Bindings[e.VarIndex].Count > 1) throw new Exception();
			foreach (Resource r in bindings.Union.Bindings[e.VarIndex].Items)
				return r;
			throw new Exception();
		}
		
		Statement StatementFailed = new Statement(null, null, null);
		
		private Statement GetStatement(QueryStatement sq, BindingSet bindings) {
			Resource s = GetUniqueBinding(sq.Subject, bindings);
			Resource p = GetUniqueBinding(sq.Predicate, bindings);
			Resource o = GetUniqueBinding(sq.Object, bindings);
			if (s is Literal || p is Literal) return StatementFailed;
			return new Statement((Entity)s, (Entity)p, o, QueryMeta);
		}
		
		bool MatchesFilters(Statement s, QueryStatement q, SelectableSource targetModel) {
			return MatchesFilters(s.Subject, q.Subject, targetModel)
				&& MatchesFilters(s.Predicate, q.Predicate, targetModel)
				&& MatchesFilters(s.Object, q.Object, targetModel);
		}
		
		bool MatchesFilters(Resource e, VarOrAnchor var, SelectableSource targetModel) {
			if (!var.IsVariable) return true;
			/*foreach (ValueFilter f in variables[var.VarIndex].Filters) {
				if (!f.Filter(e, targetModel)) return false;
			}*/
			return true;
		}
		
		class ClearMetaDupCheck : StatementSink {
			MemoryStore m;
			public ClearMetaDupCheck(MemoryStore m) { this.m = m; }
			public bool Add(Statement s) {
				// remove meta information
				s = new Statement(s.Subject, s.Predicate, s.Object);
				if (!m.Contains(s))
					m.Add(s);
				return true;
			}
		}
		
		private void Init() {
			// Get the list of variables, which is the set
			// of anonymous nodes in the statements to match.
			ArrayList setupVariables = new ArrayList();
			
			if (setupStatements.Count == 0)
				throw new QueryFormatException("A query must have at least one non-optional statement.");
			
			foreach (Statement s in setupStatements) {
				InitAnonVariable(s.Subject, setupVariables);
				InitAnonVariable(s.Predicate, setupVariables);
				InitAnonVariable(s.Object, setupVariables);
			}
			foreach (Statement s in setupOptionalStatements) {
				InitAnonVariable(s.Subject, setupVariables);
				InitAnonVariable(s.Predicate, setupVariables);
				InitAnonVariable(s.Object, setupVariables);
			}
		
			// Set up the variables array.
			variables = new Variable[setupVariables.Count];
			variableEntities = new SemWeb.Variable[variables.Length];
			Hashtable varIndex = new Hashtable();
			for (int i = 0; i < variables.Length; i++) {
				variables[i].Entity = (SemWeb.Variable)setupVariables[i];
				variableEntities[i] = variables[i].Entity;
				varIndex[variables[i].Entity] = i;
				
				ArrayList filters = new ArrayList();
				foreach (SetupValueFilter filter in setupValueFilters) {
					if (filter.a == variables[i].Entity)
						filters.Add(filter.b);
				}
				
				//variables[i].Filters = (ValueFilter[])filters.ToArray(typeof(ValueFilter));
			}
			
			// Set up the statements
			ArrayList statements = new ArrayList();
			foreach (Statement st in setupStatements)
				InitSetStatement(st, statements, varIndex, false);
			foreach (Statement st in setupOptionalStatements)
				InitSetStatement(st, statements, varIndex, true);
			
			// Order the statements in the most efficient order
			// for the recursive query.
			Hashtable setVars = new Hashtable();
			ArrayList sgroups = new ArrayList();
			while (statements.Count > 0) {
				QueryStatement[] group = InitBuildNode(statements, setVars);
				sgroups.Add(group);
				foreach (QueryStatement qs in group) {
					if (qs.Subject.IsVariable) setVars[qs.Subject.VarIndex] = setVars;
					if (qs.Predicate.IsVariable) setVars[qs.Predicate.VarIndex] = setVars;
					if (qs.Object.IsVariable) setVars[qs.Object.VarIndex] = setVars;
				}
			}
			
			this.statements = (QueryStatement[][])sgroups.ToArray(typeof(QueryStatement[]));
		}
		
		private void InitAnonVariable(Resource r, ArrayList setupVariables) {
			if (r is SemWeb.Variable)
				setupVariables.Add(r);
		}
		
		private void InitSetStatement(Statement st, ArrayList statements, Hashtable varIndex, bool optional) {
			QueryStatement qs = new QueryStatement();
			
			InitSetStatement(st.Subject, ref qs.Subject, varIndex);
			InitSetStatement(st.Predicate, ref qs.Predicate, varIndex);
			InitSetStatement(st.Object, ref qs.Object, varIndex);
			
			qs.Optional = optional;
			
			// If this statement has no variables, add it to a separate list.
			if (!qs.Subject.IsVariable && !qs.Predicate.IsVariable && !qs.Object.IsVariable)
				novariablestatements.Add(st);
			else
				statements.Add(qs);
		}
		
		private void InitSetStatement(Resource ent, ref VarOrAnchor st, Hashtable varIndex) {
			if (!varIndex.ContainsKey(ent)) {
				st.IsVariable = false;
				st.Anchor = ent;
			} else {
				st.IsVariable = true;
				st.VarIndex = (int)varIndex[ent];
			}
		}
		
		private class QueryStatementComparer : IComparer {
			Hashtable setVars;
			ResSet fps, ifps;
			
			public QueryStatementComparer(Hashtable setVars, ResSet fps, ResSet ifps) {
				this.setVars = setVars;
				this.fps = fps;
				this.ifps = ifps;
			}
		
			int IComparer.Compare(object a, object b) {
				return Compare((QueryStatement)a, (QueryStatement)b);
			}
			
			public int Compare(QueryStatement a, QueryStatement b) {
				int optional = a.Optional.CompareTo(b.Optional);
				if (optional != 0) return optional;
				
				int numvars = NumVars(a).CompareTo(NumVars(b));
				if (numvars != 0) return numvars;
				
				int complexity = Complexity(a).CompareTo(Complexity(b));
				return complexity;
			}
			
			private int NumVars(QueryStatement s) {
				int ret = 0;
				if (s.Subject.IsVariable && !setVars.ContainsKey(s.Subject.VarIndex))
					ret++;
				if (s.Predicate.IsVariable && !setVars.ContainsKey(s.Predicate.VarIndex))
					ret++;
				if (s.Object.IsVariable && !setVars.ContainsKey(s.Object.VarIndex))
					ret++;
				return ret;
			}
			
			private int Complexity(QueryStatement s) {
				if (s.Predicate.IsVariable) return 2;
				if ((!s.Subject.IsVariable || setVars.ContainsKey(s.Subject.VarIndex))
					&& fps.Contains(s.Predicate.Anchor))
					return 0;
				if ((!s.Object.IsVariable || setVars.ContainsKey(s.Object.VarIndex))
					&& ifps.Contains(s.Predicate.Anchor))
					return 0;
				return 1;
			}
		}
		
		private QueryStatement[] InitBuildNode(ArrayList statements, Hashtable setVars) {
			// Get the best statements to consider
			// Because we can consider statements in groups, we need
			// a list of lists.
			QueryStatementComparer comparer = new QueryStatementComparer(setVars, fps, ifps);
			ArrayList considerations = new ArrayList();
			for (int i = 0; i < statements.Count; i++) {
				QueryStatement next = (QueryStatement)statements[i];
				int comp = 1;
				if (considerations.Count > 0) {
					QueryStatement curcomp = (QueryStatement) ((ArrayList)considerations[0])[0];
					comp = comparer.Compare(curcomp, next);
				}
				
				if (comp < 0) // next is worse than current
					continue;
				
				if (comp > 0) // clear out worse possibilities
					considerations.Clear();
				
				ArrayList group = new ArrayList();
				group.Add(next);
				considerations.Add(group);
			}
			
			// Pick the group with the most number of statements.
			ArrayList bestgroup = null;
			foreach (ArrayList g in considerations) {
				if (bestgroup == null || bestgroup.Count < g.Count)
					bestgroup = g;
			}
			
			foreach (QueryStatement qs in bestgroup)
				statements.Remove(qs);
			
			return (QueryStatement[])bestgroup.ToArray(typeof(QueryStatement));
		}

	}
	
	public abstract class QueryResultSink {
		public virtual void Init(VariableBinding[] variables, bool distinct, bool ordered) {
		}
		
		public abstract bool Add(VariableBinding[] result);

		public virtual void Finished() {
		}
		
		public virtual void AddComments(string comments) {
		}
	}
	
	internal class QueryResultBufferSink : QueryResultSink {
		public ArrayList Bindings = new ArrayList();
		public override bool Add(VariableBinding[] result) {
			Bindings.Add(result.Clone());
			return true;
		}
	}

	public struct VariableBinding {
		Variable v;
		Resource t;
		
		public VariableBinding(Variable variable, Resource target) {
			v = variable;
			t = target;
		}
		
		public Variable Variable { get { return v; } set { v = value; } }
		public string Name { get { return v.LocalName; } }
		public Resource Target { get { return t; } set { t = value; } }

		public static Statement Substitute(VariableBinding[] variables, Statement template) {
			// This may throw an InvalidCastException if a variable binds
			// to a literal but was used as the subject, predicate, or meta
			// of the template.
			foreach (VariableBinding v in variables) {
				if (v.Variable == template.Subject) template = new Statement((Entity)v.Target, template.Predicate, template.Object, template.Meta);
				if (v.Variable == template.Predicate) template = new Statement(template.Subject, (Entity)v.Target, template.Object, template.Meta);
				if (v.Variable == template.Object) template = new Statement(template.Subject, template.Predicate, v.Target, template.Meta);
				if (v.Variable == template.Meta) template = new Statement(template.Subject, template.Predicate, template.Object, (Entity)v.Target);
			}
			return template;
		}
	}
}

