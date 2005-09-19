using System;
using System.Collections;
using System.IO;

using SemWeb;
using SemWeb.Stores;
using SemWeb.Util;

namespace SemWeb.Query {
	public class QueryException : ApplicationException {
		public QueryException(string message) : base(message) {
		}
			
		public QueryException(string message, Exception cause) : base(message, cause) {
		}
	}
	
	public class QueryEngine {
		// Setup information
	
		ArrayList setupVariables = new ArrayList();
		ArrayList setupVariablesDistinct = new ArrayList();
		ArrayList setupValueFilters = new ArrayList();
		ArrayList setupStatements = new ArrayList();
		ArrayList setupOptionalStatements = new ArrayList();
		
		int start = -1;
		int limit = -1;
		
		// Query model information
		
		bool init = false;
		object sync = new object();
		Variable[] variables;
		Entity[] variableEntities;
		QueryStatement[][] statements;
		
		// contains functional and inverse functional properties
		ResSet fps = new ResSet(),
		          ifps = new ResSet();
		
		private struct Variable {
			public Entity Entity;
			public ValueFilter[] Filters;
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
			public QueryResult(QueryEngine q) {
				Bindings = new ResSet[q.variables.Length];
			}
			private QueryResult(int x) {
				Bindings = new ResSet[x];
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
				QueryResult r = new QueryResult(Bindings.Length);
				for (int i = 0; i < Bindings.Length; i++)
					if (Bindings[i] != null)
						r.Bindings[i] = Bindings[i].Clone();
				return r;
			}
		}
		
		class BindingSet {
			public ArrayList Results = new ArrayList();
			public QueryResult Union;
			
			public BindingSet(QueryEngine q) {
				Union = new QueryResult(q);
			}
		}
					
		public void Select(Entity entity) {
			if (entity.Uri == null) throw new QueryException("Anonymous nodes are automatically considered variables.");
			if (setupVariables.Contains(entity)) return;
			setupVariables.Add(entity);
		}

		public void MakeDistinct(Entity a, Entity b) {
			SetupVariablesDistinct d = new SetupVariablesDistinct();
			d.a = a;
			d.b = b;
			setupVariablesDistinct.Add(d);
		}
		
		public void AddValueFilter(Entity entity, ValueFilter filter) {
			SetupValueFilter d = new SetupValueFilter();
			d.a = entity;
			d.b = filter;
			setupValueFilters.Add(d);
		}
		
		public void AddFilter(Statement filter) {
			setupStatements.Add(filter);
		}

		public void AddOptionalFilter(Statement filter) {
			setupOptionalStatements.Add(filter);
		}
		
		public int ReturnStart { get { return start; } set { start = value; } }
		
		public int ReturnLimit { get { return limit; } set { limit = value; } }
		
		private class SetupVariablesDistinct {
			public Entity a, b;
		}
		private class SetupValueFilter {
			public Entity a;
			public ValueFilter b;
		}
		
		public void Query(Store targetModel, QueryResultSink result) {
			lock (sync) {
				if (!init) {
					Init();
					init = true;
				}
			}
			
			result.Init(variableEntities);
			
			BindingSet bindings = new BindingSet(this);
			foreach (QueryStatement[] group in statements) {
				bool ret = Query(group, bindings, targetModel);
				if (!ret) {
					// A false return value indicates the query
					// certainly failed.
					result.Finished();
					return;
				}
			}

			VariableBinding[] finalbindings = new VariableBinding[variables.Length];
			for (int i = 0; i < variables.Length; i++)
				finalbindings[i].Variable = variableEntities[i];
			
			int ctr = -1;
			foreach (QueryResult r in bindings.Results) {
				Permutation permutation = new Permutation(r.Bindings);
				do {
					ctr++;
					if (ctr < start) continue;
					for (int i = 0; i < variables.Length; i++)
						finalbindings[i].Target = permutation[i];
					result.Add(finalbindings);
					if (ctr == start+limit) break;	
				} while (permutation.Next());
				if (ctr == start+limit) break;	
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

		private bool Query(QueryStatement[] group, BindingSet bindings, Store targetModel) {
			if (group.Length == 1) {
				QueryStatement qs = group[0];
				
				int numMultiplyBound = IsMultiplyBound(qs.Subject, bindings)
					+ IsMultiplyBound(qs.Predicate, bindings)
					+ IsMultiplyBound(qs.Object, bindings);
				
				if (numMultiplyBound >= 1) {
					// If there is one or more multiply-bound variable,
					// then we need to iterate through the permutations
					// of the variables in the statement.
					
					Debug(qs.ToString() + " Something Multiply Bound");
					
					Entity s, p;
					Resource o;
					
					ArrayList templates = new ArrayList();
					BindingEnumerator enumer = new BindingEnumerator(qs, bindings.Union);
					while (enumer.MoveNext(out s, out p, out o))
						templates.Add(new Statement(s, p, o));
					
					Debug("\t" + templates.Count + " Templates");
					
					// But we still need to preserve the pairings of
					// the multiply bound variable with the matching
					// statements.
					
					MemoryStore matches1 = targetModel.Select((Statement[])templates.ToArray(typeof(Statement)));
					
					Debug("\t" + matches1.StatementCount + " Matches");
					
					// The memory store that we get back from a select
					// won't do indexing, but indexing will help.
					MemoryStore matches = new MemoryStore();
					matches.Import(matches1);

					ArrayList newbindings = new ArrayList();
					
					if (!qs.Optional) bindings.Union.Clear(qs);
					
					foreach (QueryResult binding in bindings.Results) {
						// Break apart the permutations in this binding.
						BindingEnumerator enumer2 = new BindingEnumerator(qs, binding);
						while (enumer2.MoveNext(out s, out p, out o)) {
							// Get the matching statements from the union query
							Statement bs = new Statement(s, p, o);
							MemoryStore innermatches = matches.Select(bs);
							
							// If no matches, the binding didn't match the filter.
							if (innermatches.StatementCount == 0) {
								if (qs.Optional) {
									// Preserve the binding.
									newbindings.Add(binding);
								} else {
									// Toss out the binding.
									continue;
								}
							}
							
							foreach (Statement m in innermatches) {
								if (!MatchesFilters(m, qs, targetModel)) {
									if (qs.Optional) newbindings.Add(binding);
									continue;
								}
								bindings.Union.Add(qs, m);
								
								QueryResult r = binding.Clone();
								r.Set(qs, m);
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
						if (!targetModel.Contains(s)
							&& !qs.Optional)
							return false;
					
					} else if (numUnbound == 1) {
						Debug(qs.ToString() + " 1 Unbound");
					
						// There is just one unbound variable.  The others
						// are not multiply bound, so they must be uniquely
						// bound (but they may not be bound in all results).
						// Run a combined select to find all possible values
						// of the unbound variable at once, and set these to
						// be the values of the variable for matching results.
						
						ResSet values = new ResSet();
						foreach (Statement match in targetModel.Select(s)) {
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
						}
						
					} else {
						// There are two or more unbound variables, the
						// third variable being uniquely bound, if bound.
						// Keep track of the pairing of unbound variables.
						
						Debug(qs.ToString() + " 2 or 3 Unbound");
					
						if (bindings.Results.Count == 0)
							bindings.Results.Add(new QueryResult(this));
							
						ArrayList newbindings = new ArrayList();
						foreach (Statement match in targetModel.Select(s)) {
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
								newbindings.Add(r2);
							}
						}
						if (qs.Optional && newbindings.Count == 0)
							return true; // don't clear out bindings
						bindings.Results = newbindings;
					}
				}
			
			} else {
				// There is more than one statement in the group.
				// These are never optional.

				Debug("Statement group.");
		
				VarOrAnchor var = new VarOrAnchor();
				foreach (QueryStatement qs in group) {
					if (qs.Subject.IsVariable && bindings.Union.Bindings[qs.Subject.VarIndex] == null) var = qs.Subject;
					if (qs.Predicate.IsVariable && bindings.Union.Bindings[qs.Predicate.VarIndex] == null) var = qs.Predicate;
					if (qs.Object.IsVariable && bindings.Union.Bindings[qs.Object.VarIndex] == null) var = qs.Object;
					break;
				}
				// The variable should be unbound so far.
				if (bindings.Union.Bindings[var.VarIndex] != null)
					throw new Exception();
				
				ArrayList findstatements = new ArrayList();
				foreach (QueryStatement qs in group) {
					Statement s = GetStatement(qs, bindings);
					if (s == StatementFailed) return false;
					findstatements.Add(s);
				}
				
				ResSet values = new ResSet();
				foreach (Entity r in targetModel.FindEntities((Statement[])findstatements.ToArray(typeof(Statement)))) {
					if (!MatchesFilters(r, var, targetModel)) continue;
					values.Add(r);
				}
				if (values.Count == 0) return false;
				
				bindings.Union.Bindings[var.VarIndex] = values;
				
				if (bindings.Results.Count == 0)
					bindings.Results.Add(new QueryResult(this));
			
				foreach (QueryResult result in bindings.Results)
					result.Bindings[var.VarIndex] = values;
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
			return new Statement((Entity)s, (Entity)p, o);
		}
		
		bool MatchesFilters(Statement s, QueryStatement q, Store targetModel) {
			return MatchesFilters(s.Subject, q.Subject, targetModel)
				&& MatchesFilters(s.Predicate, q.Predicate, targetModel)
				&& MatchesFilters(s.Object, q.Object, targetModel);
		}
		
		bool MatchesFilters(Resource e, VarOrAnchor var, Store targetModel) {
			if (!var.IsVariable) return true;
			foreach (ValueFilter f in variables[var.VarIndex].Filters) {
				if (!f.Filter(e, targetModel)) return false;
			}
			return true;
		}
		
		private void Init() {
			// Any anonymous nodes in the graph are SelectFirst nodes
			foreach (Statement s in setupStatements) {
				InitAnonVariable(s.Subject);
				InitAnonVariable(s.Predicate);
				InitAnonVariable(s.Object);
			}
			foreach (Statement s in setupOptionalStatements) {
				InitAnonVariable(s.Subject);
				InitAnonVariable(s.Predicate);
				InitAnonVariable(s.Object);
			}
		
			// Set up the variables array.
			variables = new Variable[setupVariables.Count];
			variableEntities = new Entity[variables.Length];
			Hashtable varIndex = new Hashtable();
			for (int i = 0; i < variables.Length; i++) {
				variables[i].Entity = (Entity)setupVariables[i];
				variableEntities[i] = variables[i].Entity;
				varIndex[variables[i].Entity] = i;
				
				ArrayList filters = new ArrayList();
				foreach (SetupValueFilter filter in setupValueFilters) {
					if (filter.a == variables[i].Entity)
						filters.Add(filter.b);
				}
				
				variables[i].Filters = (ValueFilter[])filters.ToArray(typeof(ValueFilter));
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
		
		private void InitAnonVariable(Resource r) {
			if (r is Entity && r.Uri == null)
				setupVariables.Add(r);
		}
		
		private void InitSetStatement(Statement st, ArrayList statements, Hashtable varIndex, bool optional) {
			QueryStatement qs = new QueryStatement();
			
			InitSetStatement(st.Subject, ref qs.Subject, varIndex);
			InitSetStatement(st.Predicate, ref qs.Predicate, varIndex);
			InitSetStatement(st.Object, ref qs.Object, varIndex);
			
			qs.Optional = optional;
			
			// If this statement has no variables, just drop it.
			if (!qs.Subject.IsVariable && !qs.Predicate.IsVariable && !qs.Object.IsVariable)
				return;

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
				
				// next is equal to what we have.  we can either
				// group it with an existing statement, or make
				// a new group out of it.
				
				bool grouped = false;
				foreach (ArrayList g in considerations) {
					// We can group next in g, if the only
					// unset variables in next are the only
					// unset variables in the statements in g.
					// AND if nothing else is a variable,
					// because of having multiply-bound variables
					// with FindEntities.  And none may be optional.
					QueryStatement s = (QueryStatement)g[0];
					int su = InitBuildNodeGetUniqueUnsetVar(s, setVars);
					int nu = InitBuildNodeGetUniqueUnsetVar(next, setVars);
					if (su == nu && su != -1 && s.NumVars() == 1 && next.NumVars() == 1
						&& !s.Optional && !next.Optional) {
						g.Add(next);
						grouped = true;
						break;
					}
				}
				if (grouped) continue;
				
				// we didn't group it with anything existing,
				// so make a new group
				
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
		
		int InitBuildNodeGetUniqueUnsetVar(QueryStatement s, Hashtable setVars) {
			int ret = -1;
			if (s.Subject.IsVariable && !setVars.ContainsKey(s.Subject.VarIndex))
				ret = s.Subject.VarIndex;
			if (s.Predicate.IsVariable && !setVars.ContainsKey(s.Predicate.VarIndex)) {
				if (ret != -1) return -1;
				ret = s.Predicate.VarIndex;
			}
			if (s.Object.IsVariable && !setVars.ContainsKey(s.Object.VarIndex)) {
				if (ret != -1) return -1;
				ret = s.Object.VarIndex;
			}
			return ret;
		}
	}
	
	public abstract class QueryResultSink {
		public virtual void Init(Entity[] variables) {
		}
		
		public abstract bool Add(VariableBinding[] result);

		public virtual void Finished() {
		}
	}
	
	public struct VariableBinding {
		Entity v;
		Resource t;
		
		internal VariableBinding(Entity variable, Resource target) {
			v = variable;
			t = target;
		}
		
		public Entity Variable { get { return v; } internal set { v = value; } }
		public Resource Target { get { return t; } internal set { t = value; } }
	}
}	

