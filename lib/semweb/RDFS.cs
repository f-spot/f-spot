using System;
using System.Collections;

using SemWeb;
using SemWeb.Stores;
using SemWeb.Util;

namespace SemWeb.Inference {

	public class RDFS : SelectableSource, SupportsPersistableBNodes, IDisposable {
		static readonly Entity type = NS.RDF + "type";
		static readonly Entity subClassOf = NS.RDFS + "subClassOf";
		static readonly Entity subPropertyOf = NS.RDFS + "subPropertyOf";
		static readonly Entity domain = NS.RDFS + "domain";
		static readonly Entity range = NS.RDFS + "range";
	
		// Each of these hashtables relates an entity
		// to a ResSet of other entities, including itself.
		Hashtable superclasses = new Hashtable();
		Hashtable subclasses = new Hashtable();
		Hashtable superprops = new Hashtable();
		Hashtable subprops = new Hashtable();
		
		// The hashtables relate a property to a ResSet of
		// its domain and range, and from a type to a ResSet
		// of properties it is the domain or range of.
		Hashtable domains = new Hashtable();
		Hashtable ranges = new Hashtable();
		Hashtable domainof = new Hashtable();
		Hashtable rangeof = new Hashtable();
		
		SelectableSource data;
		
		StatementSink schemasink;
		
		public RDFS(SelectableSource data) {
			this.data = data;
			schemasink = new SchemaSink(this);
		}
		
		public RDFS(StatementSource schema, SelectableSource data)
		: this(data) {
			LoadSchema(schema);
		}
		
		void IDisposable.Dispose() {
			if (data is IDisposable)
				((IDisposable)data).Dispose();
		}
		
		public StatementSink Schema { get { return schemasink; } }
		
		string SupportsPersistableBNodes.GetStoreGuid() { if (data is SupportsPersistableBNodes) return ((SupportsPersistableBNodes)data).GetStoreGuid(); return null; }
		
		string SupportsPersistableBNodes.GetNodeId(BNode node) { if (data is SupportsPersistableBNodes) return ((SupportsPersistableBNodes)data).GetNodeId(node); return null; }
		
		BNode SupportsPersistableBNodes.GetNodeFromId(string persistentId) { if (data is SupportsPersistableBNodes) return ((SupportsPersistableBNodes)data).GetNodeFromId(persistentId); return null; }

		class SchemaSink : StatementSink {
			RDFS rdfs;
			public SchemaSink(RDFS parent) { rdfs = parent; }
			bool StatementSink.Add(Statement s) { rdfs.Add(s); return true; }
		}
		
		void Add(Statement schemastatement) {
			if (schemastatement.Predicate == subClassOf && schemastatement.Object is Entity)
				AddRelation(schemastatement.Subject, (Entity)schemastatement.Object, superclasses, subclasses, true);
			if (schemastatement.Predicate == subPropertyOf && schemastatement.Object is Entity)
				AddRelation(schemastatement.Subject, (Entity)schemastatement.Object, superprops, subprops, true);
			if (schemastatement.Predicate == domain && schemastatement.Object is Entity)
				AddRelation(schemastatement.Subject, (Entity)schemastatement.Object, domains, domainof, false);
			if (schemastatement.Predicate == range && schemastatement.Object is Entity)
				AddRelation(schemastatement.Subject, (Entity)schemastatement.Object, ranges, rangeof, false);
		}
		
		void AddRelation(Entity a, Entity b, Hashtable supers, Hashtable subs, bool incself) {
			AddRelation(a, b, supers, incself);
			AddRelation(b, a, subs, incself);
		}
		
		void AddRelation(Entity a, Entity b, Hashtable h, bool incself) {
			ResSet r = (ResSet)h[a];
			if (r == null) {
				r = new ResSet();
				h[a] = r;
				if (incself) r.Add(a);
			}
			r.Add(b);
		}
		
		public void LoadSchema(StatementSource source) {
			if (source is SelectableSource) {
				((SelectableSource)source).Select(
					new SelectFilter(
					null,
					new Entity[] { subClassOf, subPropertyOf, domain, range },
					null, null), Schema);
			} else {
				source.Select(Schema);
			}
		}
		
		public bool Distinct { get { return false; } }
		
		public void Select(StatementSink sink) { data.Select(sink); }
		
		public bool Contains(Statement template) {
			return Store.DefaultContains(this, template);
		}
		
		public void Select(Statement template, StatementSink sink) {
			if (template.Predicate == null) {
				data.Select(template, sink);
				return;
			}
			
			Select(new SelectFilter(template), sink);
		}
		
		public void Select(SelectFilter filter, StatementSink sink) {
			if (filter.Predicates == null || filter.LiteralFilters != null) {
				data.Select(filter, sink);
				return;
			}
			
			ResSet remainingPredicates = new ResSet();
			
			Entity[] subjects = filter.Subjects;
			Entity[] predicates = filter.Predicates;
			Resource[] objects = filter.Objects;
			Entity[] metas = filter.Metas;
			
			foreach (Entity p in predicates) {
				if (p == type) {
					if (objects != null) {
						// Do the subjects have any of the types listed in the objects,
						// or what things have those types?
						
						// Expand objects by the subclass closure of the objects
						data.Select(new SelectFilter(subjects, new Entity[] { p }, GetClosure(objects, subclasses), metas), sink);
						
						// Process domains and ranges.
						ResSet dom = new ResSet(), ran = new ResSet();
						Hashtable domPropToType = new Hashtable();
						Hashtable ranPropToType = new Hashtable();
						foreach (Entity e in objects) {
							Entity[] dc = GetClosure((ResSet)domainof[e], subprops);
							if (dc != null)
							foreach (Entity c in dc) {
								dom.Add(c);
								AddRelation(c, e, domPropToType, false);
							}
							
							dc = GetClosure((ResSet)rangeof[e], subprops);
							if (dc != null)
							foreach (Entity c in dc) {
								ran.Add(c);
								AddRelation(c, e, ranPropToType, false);
							}
						}
						
						// If it's in the domain of any of these properties,
						// we know its type.
						if (subjects != null) {
							if (dom.Count > 0) data.Select(new SelectFilter(subjects, dom.ToEntityArray(), null, metas), new ExpandDomRan(0, domPropToType, sink));
							if (ran.Count > 0) data.Select(new SelectFilter(null, ran.ToEntityArray(), subjects, metas), new ExpandDomRan(1, ranPropToType, sink));
						}
						
					} else if (subjects != null) {
						// What types do these subjects have?
						
						// Expand the resulting types by the closure of their superclasses
						data.Select(new SelectFilter(subjects, new Entity[] { p }, objects, metas), new Expand(superclasses, sink));
						
						// Use domains and ranges to get type info
						data.Select(new SelectFilter(subjects, null, null, metas), new Expand3(0, domains, superclasses, sink));
						data.Select(new SelectFilter(null, null, subjects, metas), new Expand3(1, ranges, superclasses, sink));

					} else {
						// What has type what?  We won't answer that question.
						data.Select(filter, sink);
					}

				} else if ((p == subClassOf || p == subPropertyOf)
					&& (metas == null || metas[0] == Statement.DefaultMeta)) {
					
					Hashtable supers = (p == subClassOf) ? superclasses : superprops;
					Hashtable subs = (p == subClassOf) ? subclasses : subprops;
					
					if (subjects != null && objects != null) {
						// Expand objects by the subs closure of the objects.
						data.Select(new SelectFilter(subjects, new Entity[] { p }, GetClosure(objects, subs), metas), sink);
					} else if (subjects != null) {
						// get all of the supers of all of the subjects
						foreach (Entity s in subjects)
							foreach (Entity o in GetClosure(new Entity[] { s }, supers))
								sink.Add(new Statement(s, p, o));
					} else if (objects != null) {
						// get all of the subs of all of the objects
						foreach (Resource o in objects) {
							if (o is Literal) continue;
							foreach (Entity s in GetClosure(new Entity[] { (Entity)o }, subs))
								sink.Add(new Statement(s, p, (Entity)o));
						}
					} else {
						// What is a subclass/property of what?  We won't answer that.
						data.Select(filter, sink);
					}
				} else {
					remainingPredicates.Add(p);
				}
			}
			
			if (remainingPredicates.Count > 0) {
				// Also query the subproperties of any property
				// being queried, but remember which subproperties
				// came from which superproperties so we can map them
				// back to the properties actually queried.  The closures
				// contain the queried properties themselves too.
				ResSet qprops = new ResSet();
				Hashtable propfrom = new Hashtable();
				foreach (Entity p in remainingPredicates) { 
					foreach (Entity sp in GetClosure(new Entity[] { p }, subprops)) {
						AddRelation(sp, p, propfrom, false);
						qprops.Add(sp);
					}
				}
				
				//data.Select(subjects, qprops.ToEntityArray(), objects, metas, new LiteralDTMap(ranges, new PredMap(propfrom, sink)));
				
				SelectFilter sf = new SelectFilter(subjects, qprops.ToEntityArray(), objects, metas);
				sf.LiteralFilters = filter.LiteralFilters;
				sf.Limit = filter.Limit;
				
				data.Select(sf, new PredMap(propfrom, sink));
			}
		}
		
		static Entity[] GetClosure(ResSet starts, Hashtable table) {
			if (starts == null) return null;
			return GetClosure(starts.ToArray(), table);
		}
		
		static Entity[] GetClosure(Resource[] starts, Hashtable table) {
			ResSet ret = new ResSet();
			ResSet toadd = new ResSet(starts);
			while (toadd.Count > 0) {
				ResSet newadd = new ResSet();
				
				foreach (Resource e in toadd) {
					if (!(e is Entity)) continue;
					if (ret.Contains(e)) continue;
					ret.Add(e);
					if (table.ContainsKey(e))
						newadd.AddRange((ResSet)table[e]);
				}
				
				toadd.Clear();
				toadd.AddRange(newadd);
			}
			return ret.ToEntityArray();
		}
		
		class Expand : StatementSink {
			Hashtable table;
			StatementSink sink;
			public Expand(Hashtable t, StatementSink s) { table = t; sink = s; }
			public bool Add(Statement s) {
				foreach (Entity e in RDFS.GetClosure(new Resource[] { s.Object }, table))
					if (!sink.Add(new Statement(s.Subject, s.Predicate, e, s.Meta)))
						return false;
				return true;
			}
		}

		class ExpandDomRan : StatementSink {
			int domran;
			Hashtable map;
			StatementSink sink;
			public ExpandDomRan(int dr, Hashtable propToType, StatementSink s) {
				if (s == null) throw new ArgumentNullException();
				domran = dr;
				map = propToType;
				sink = s; 
			}
			public bool Add(Statement s) {
				if (s.AnyNull) throw new ArgumentNullException();
				if (domran == 1 && !(s.Object is Entity)) return true;
				if (!map.ContainsKey(s.Predicate)) return true; // shouldn't really happen
				foreach (Entity e in (ResSet)map[s.Predicate]) {
					Statement s1 = new Statement(
						domran == 0 ? s.Subject : (Entity)s.Object,
						type,
						e,
						s.Meta);
					if (!sink.Add(s1))
						return false;
				}
				return true;
			}
		}

		class Expand3 : StatementSink {
			int domran;
			Hashtable table;
			Hashtable superclasses;
			StatementSink sink;
			public Expand3(int dr, Hashtable t, Hashtable sc, StatementSink s) { domran = dr; table = t; superclasses = sc; sink = s; }
			public bool Add(Statement s) {
				if (domran == 1 && !(s.Object is Entity)) return true;
				ResSet rs = (ResSet)table[s.Predicate];
				if (rs == null) return true;
				foreach (Entity e in RDFS.GetClosure(rs, superclasses)) {
					Statement s1 = new Statement(
						domran == 0 ? s.Subject : (Entity)s.Object,
						type,
						e,
						s.Meta);
					if (!sink.Add(s1))
						return false;
				}
				return true;
			}
		}
		
		class PredMap : StatementSink {
			Hashtable table;
			StatementSink sink;
			public PredMap(Hashtable t, StatementSink s) { table = t; sink = s; }
			public bool Add(Statement s) {
				if (table[s.Predicate] == null) {
					return sink.Add(s);
				} else {
					foreach (Entity e in (ResSet)table[s.Predicate])
						if (!sink.Add(new Statement(s.Subject, e, s.Object, s.Meta)))
							return false;
				}
				return true;
			}
		}

		class LiteralDTMap : StatementSink {
			Hashtable ranges;
			StatementSink sink;
			public LiteralDTMap(Hashtable t, StatementSink s) { ranges = t; sink = s; }
			public bool Add(Statement s) {
				ranges.ToString(); // avoid warning about not using variable
				if (s.Object is Literal && ((Literal)s.Object).DataType == null) {
					// TODO: Look at the superproperty closure of the predicate
					// and apply the first range found to the literal.  While
					// more than one range may apply, we can only assign one.
					// It would be best to assign the most specific, but we
					// don't have that info.  And, don't ever assign the rdfs:Literal
					// or rdfs:Resource classes as the data type -- and there may be
					// others -- that are consistent but just not data types.
					// Also, assign the most specific data type if we have
					// the class relations among them.
					return sink.Add(s);
				} else {
					return sink.Add(s);
				}
			}
		}
	}

}
