using System;
using System.Collections;

namespace SemWeb.Reasoning {
	public class InferenceStore : Store {
		
		Store store;
		ReasoningEngine engine;
		
		public InferenceStore(Store store, ReasoningEngine engine) {
			this.store = store;
			this.engine = engine;
		}
		
		public Store Source { get { return store; } }
		public ReasoningEngine Engine { get { return engine; } }
		
		public override int StatementCount { get { return store.StatementCount; } }

		public override void Clear() { store.Clear(); }
		
		public override Entity[] GetAllEntities() {
			return store.GetAllEntities();
		}
		
		public override Entity[] GetAllPredicates() {
			return store.GetAllPredicates();
		}
		
		public override void Add(Statement statement) {
			store.Add(statement);
		}		
		
		public override void Remove(Statement statement) {
			store.Remove(statement);
		}

		public override void Import(StatementSource source) {
			store.Import(source);
		}
		
		public override void Select(Statement template, SelectPartialFilter partialFilter, StatementSink result) {
			// If the template is a full statement (has a subject, predicate,
			// and object), use the specialized routine to check if the statement
			// is asserted in the source.
			if (!template.AnyNull) {
				if (Contains(template))
					result.Add(template);
				return;
			}
			
			// For each matching statement, find further entailments that match the template.
			result = new ReasoningStatementSink(template, result, this);
			Source.Select(template, partialFilter, result);
			
			// Do specialized querying based on the template.
			Engine.Select(template, result, Source);
		}
		
		public override void Select(Statement[] templates, SelectPartialFilter partialFilter, StatementSink result) {
			throw new NotImplementedException();
		}

		public override bool Contains(Statement statement) {
			if (statement.AnyNull)
				throw new ArgumentNullException();

			if (Source.Contains(statement))
				return true;
			
			return Engine.IsAsserted(statement, Source);
		}
		
		public override void Replace(Entity a, Entity b) {
			store.Replace(a, b);
		}

		public override void Replace(Statement find, Statement replacement) {
			store.Replace(find, replacement);
		}
		
		public override Entity[] FindEntities(Statement[] filters) {
			return store.FindEntities(filters);
		}
		
		private class ReasoningStatementSink : StatementSink {
			Statement template;
			StatementSink store;
			InferenceStore inference;
			
			public ReasoningStatementSink(Statement template, StatementSink store, InferenceStore inference) { this.template = template; this.store = store; this.inference = inference; }
			
			public bool Add(Statement statement) {
				inference.Engine.SelectFilter(ref statement, inference.Source);
				store.Add(statement);
				inference.Engine.FindEntailments(statement, template, store, inference.Source);
				return true;
			}
		}
	}
	
	public abstract class ReasoningEngine {
		public virtual bool IsAsserted(Statement statement, Store source) {
			return false;
		}
		
		public virtual void FindEntailments(Statement statement, Statement template, StatementSink result, Store source) {
		}

		public virtual void Select(Statement statement, StatementSink result, Store source) {
		}
		
		public virtual void SelectFilter(ref Statement statement, Store source) {
		}
	}
	
	public class RDFSReasoning : ReasoningEngine {
		public static readonly Entity rdfType = new Entity("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
		public static readonly Entity rdfProperty = new Entity("http://www.w3.org/1999/02/22-rdf-syntax-ns#Property");
		public static readonly Entity rdfsSubClassOf = new Entity("http://www.w3.org/2000/01/rdf-schema#subClassOf");
		public static readonly Entity rdfsSubPropertyOf = new Entity("http://www.w3.org/2000/01/rdf-schema#subPropertyOf");
		public static readonly Entity rdfsDomain = new Entity("http://www.w3.org/2000/01/rdf-schema#domain");
		public static readonly Entity rdfsRange = new Entity("http://www.w3.org/2000/01/rdf-schema#range");
		public static readonly Entity rdfsResource = new Entity("http://www.w3.org/2000/01/rdf-schema#Resource");
		public static readonly Entity rdfsClass = new Entity("http://www.w3.org/2000/01/rdf-schema#Class");
		public static readonly Entity rdfsLiteral = new Entity("http://www.w3.org/2000/01/rdf-schema#Literal");
		
		Hashtable closures = new Hashtable();
		
		private class PutInArraySink : StatementSink {
			int spo;
			ArrayList sink;
			
			public PutInArraySink(int spo, ArrayList sink) {
				this.spo = spo; this.sink = sink;
			}

			public bool Add(Statement statement) {
				object obj = null;
				if (spo == 0) obj = statement.Subject;
				else if (spo == 1) obj = statement.Predicate;
				else if (spo == 2) obj = statement.Object;
				if (obj is Entity && !sink.Contains(obj))
					sink.Add(obj);
				return true;
			}
		}
		
		public ArrayList getClosure(Entity type, Store source, Entity relation, bool inverse) {
			if (type.Uri == null) return new ArrayList();
			
			Hashtable closure = (Hashtable)closures[relation];
			if (closure == null) {
				closure = new Hashtable();
				closures[relation] = closure;
			}
			
			ArrayList items = (ArrayList)closure[type.Uri];
			if (items != null) return items;
			
			items = new ArrayList();
			
			OWLReasoning.TransitiveSelect(type, type, relation, inverse, source, new PutInArraySink(inverse ? 0 : 2, items));
			
			// Everything is a subClassOf rdfs:Resource
			if (relation == rdfsSubClassOf && !inverse && !items.Contains(rdfsResource))
				items.Add(rdfsResource);			
			
			closure[type.Uri] = items;
			return items;
		}
		
		public ArrayList getSuperTypes(Entity type, Store source) {
			return getClosure(type, source, rdfsSubClassOf, false);
		}
		public IList getSubTypes(Entity type, Store source) {
			if (type.Uri == rdfsResource)
				return source.SelectSubjects(rdfType, rdfsClass);
			return getClosure(type, source, rdfsSubClassOf, true);
		}
		public ArrayList getSuperProperties(Entity type, Store source) {
			return getClosure(type, source, rdfsSubPropertyOf, false);
		}
		public ArrayList getSubProperties(Entity type, Store source) {
			return getClosure(type, source, rdfsSubPropertyOf, true);
		}
		
		public override bool IsAsserted(Statement statement, Store source) {
			if (statement.Predicate == null || statement.Predicate.Uri == null) return false;
			
			// X rdf:type Y
			if (statement.Predicate.Uri == rdfType && statement.Object is Entity) {
				Entity typeentity = rdfType;
				
				// Check: Z rdf:subClassOf Y for all Z s.t. X rdf:type Z
				foreach (Resource type in source.SelectObjects(statement.Subject, typeentity)) {
					if (type is Entity && getSuperTypes((Entity)type, source).Contains(statement.Object))
						return true;
				}
					
				// Check: Z rdf:rdfType Y for all Z s.t. X rdf:subPropertyOf Z
				foreach (Resource type in source.SelectObjects(statement.Subject, rdfsSubPropertyOf)) {
					if (!(type is Entity)) continue;
					Statement st = new Statement((Entity)type, typeentity, statement.Object);
					if (source.Contains(st)) return true;
					if (IsAsserted(st, source)) return true;
				}
			}
			
			// X rdfs:subClassOf Y
			// Check if Y is in the supertypes array.
			if (statement.Predicate.Uri == rdfsSubClassOf)
				return getSuperTypes(statement.Subject, source).Contains(statement.Object);

			// X rdfs:subPropertyOf Y
			// Check if Y is in the superproperties array.
			if (statement.Predicate.Uri == rdfsSubPropertyOf)
				return getSuperProperties(statement.Subject, source).Contains(statement.Object);
			
			// X rdfs:domain/range Y
			// Check if Y is a subtype of any Z s.t. for any Q (X subPropertyOf Q) domain/range Z.
			if ((statement.Predicate.Uri == rdfsDomain || statement.Predicate.Uri == rdfsRange) && statement.Object is Entity) {
				ArrayList supertypes = getSuperTypes((Entity)statement.Object, source);
				ArrayList preds = getSuperProperties(statement.Subject, source);
				preds.Add(statement.Predicate);
				foreach (Entity predicate in preds)
					foreach (Resource obj in source.SelectObjects(predicate, statement.Predicate))
						if (supertypes.Contains(obj))
							return true;
			}
			
			return false;
		}
		
		public override void Select(Statement template, StatementSink result, Store source) {
			if (template.Predicate == null || template.Predicate.Uri == null) return;
			
			// X P Y
			// Run a select for all Q s.t. Q subPropertyOf Y.
			foreach (Entity predicate in getSubProperties(template.Predicate, source)) {
				source.Select(new Statement(template.Subject, predicate, template.Object), result);
			}

			// X rdfs:domain/range Y
			if ((template.Predicate.Uri == rdfsDomain || template.Predicate.Uri == rdfsRange) && template.Object != null && template.Object is Entity) {
				foreach (Entity type in getSuperTypes((Entity)template.Object, source))
					source.Select(new Statement(template.Subject, template.Predicate, type), result);
			}
			
			// X rdf:type rdf:Property
			// Return everything that is in the predicate position of a statement
			if (template.Predicate.Uri == rdfType && template.Object != null && template.Object.Uri != null && template.Object.Uri == rdfProperty) {
				foreach (Entity predicate in source.GetAllPredicates())
					result.Add(new Statement(predicate, template.Predicate, template.Object));
			}
		}

		public override void FindEntailments(Statement statement, Statement template, StatementSink result, Store source) {
			// X rdfs:subClassOf/subPropertyOf Y
			// These are transitive, so add X p Z for all Z s.t. Y p Z.
			if ((statement.Predicate.Uri == rdfsSubClassOf || statement.Predicate.Uri == rdfsSubPropertyOf) && statement.Object is Entity) {
				if (template.Object == null) {
					// Find forward transitive entailments.
					OWLReasoning.TransitiveSelect(statement.Subject, (Entity)statement.Object, statement.Predicate, false, source, result);
				} else if (template.Subject == null) {
					// Find inverse transitive entailments.
					OWLReasoning.TransitiveSelect((Entity)statement.Object, statement.Subject, statement.Predicate, true, source, result);
				}
			}
			
			// X P Y
			// Add X Q Y for all Q s.t. P rdfs:subPropertyof Q
			foreach (Entity predicate in getSuperProperties(statement.Predicate, source))
				result.Add(new Statement(statement.Subject, predicate, statement.Object));

			// X rdfs:domain/range Y
			// If the template is empty on the object, add all object subtypes.
			// If the template is empty on the subject, all all subject subproperties.
			if ((statement.Predicate.Uri == rdfsDomain || statement.Predicate.Uri == rdfsRange) && statement.Object is Entity) {
				if (template.Object == null)
					foreach (Entity type in getSubTypes((Entity)statement.Object, source))
						result.Add(new Statement(statement.Subject, statement.Predicate, type));
				if (template.Subject == null)
					foreach (Entity type in getSubProperties(statement.Subject, source))
						result.Add(new Statement(type, statement.Predicate, statement.Object));
			}
		}
		
		public override void SelectFilter(ref Statement statement, Store source) {
			// When a literal without a datatype is selected, attempt to determine
			// the data type from the rdfs:range of the predicate.
			Literal lit = statement.Object as Literal;
			if (lit != null && lit.DataType == null) {
				ArrayList predrange = new ArrayList();
				predrange.Add(statement.Predicate);
				predrange.AddRange(getSuperProperties(statement.Predicate, source));
				foreach (Entity predicate in predrange) {
					string newtype = null;
					foreach (Resource range in source.SelectObjects(predicate, rdfsRange)) {
						if (range.Uri != null && range.Uri != rdfsLiteral) {
							if (newtype == null)
								newtype = range.Uri;
							else // Multiple types match -- not sure which this value falls in
								return;
						}							
					}
					if (newtype != null) {
						statement = new Statement(statement.Subject, statement.Predicate, new Literal(lit.Value, lit.Language, newtype), statement.Meta);
						return;
					}
				}
			}
		}
	}	
	
	public class OWLReasoning : ReasoningEngine {
		public static readonly Entity rdfType = new Entity("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
		public static readonly Entity owlInverseOf = new Entity("http://www.w3.org/2002/07/owl#inverseOf");
		public static readonly Entity owlTransitive = new Entity("http://www.w3.org/2002/07/owl#TransitiveProperty");
		public static readonly Entity owlSymmetric = new Entity("http://www.w3.org/2002/07/owl#SymmetricProperty");
		public static readonly Entity owlFunctional = new Entity("http://www.w3.org/2002/07/owl#FunctionalProperty");
		public static readonly Entity owlInverseFunctional = new Entity("http://www.w3.org/2002/07/owl#InverseFunctionalProperty");
		
		public static void TransitiveSelect(Entity subject, Entity start, Entity predicate, bool inverse, Store source, StatementSink result) {
			new TransitiveFilter(source, result, subject, start, predicate, inverse);
		}
		
		private class TransitiveFilter : StatementSink {
			Store source;
			StatementSink sink;
			Entity subject, predicate;
			bool inverse, checkSymmetric;
			Hashtable seen = new Hashtable();
			ArrayList newobjects = new ArrayList();
			
			public TransitiveFilter(Store source, StatementSink sink, Entity subject, Entity start, Entity predicate, bool inverse) {
				this.source = source; this.sink = sink; this.subject = subject; this.predicate = predicate;
				
				seen[start] = seen;
				newobjects.Add(start);				
				while (newobjects.Count > 0) {
					ArrayList list = (ArrayList)newobjects.Clone();
					newobjects.Clear();
					foreach (Entity e in list) {
						this.inverse = inverse;
						this.checkSymmetric = false;
						source.Select(new Statement(!inverse ? e : null, predicate, inverse ? e : null), this);
						
						this.inverse = !inverse;
						this.checkSymmetric = true;
						source.Select(new Statement(inverse ? e : null, predicate, !inverse ? e : null), this);
					}
				}
			}
			public bool Add(Statement statement) {
				if (checkSymmetric) {
					bool isSym = source.Contains(new Statement(statement.Predicate, rdfType, owlSymmetric));
					if (!isSym) return true;
				}
				
				Resource r = !inverse ? statement.Object : statement.Subject;
				if (statement.Object is Entity && !seen.ContainsKey(r)) {
					newobjects.Add(r);
					sink.Add(new Statement(!inverse ? subject : (Entity)r, predicate, inverse ? subject : (Entity)r));
				}
				seen[r] = seen;
				return true;				
			}
		}
		
		public override void FindEntailments(Statement statement, Statement template, StatementSink result, Store source) {
			// X P Y
			// Where P is a owlTransitive
			if (statement.Object is Entity && source.Contains(new Statement(statement.Predicate, rdfType, owlTransitive))) {
				if (template.Subject == null)
					TransitiveSelect((Entity)statement.Object, statement.Subject, statement.Predicate, true, source, result);
				if (template.Object == null) {
					TransitiveSelect(statement.Subject, (Entity)statement.Object, statement.Predicate, false, source, result);
				}
			}
		}
		
		
		public override void Select(Statement template, StatementSink result, Store source) {
			if (template.Predicate == null) return;
			
			// Run inverse properties and symmetric properties backwards
			if (template.Object == null || template.Object is Entity) {
				// Create a filter that reverses the subject and object.
				InverseFilter filter = null;
				
				// Find all Y Q X where P owl:inverseOf Q
				foreach (Resource inverse in source.SelectObjects(template.Predicate, owlInverseOf)) {
					if (filter == null) filter = new InverseFilter(template.Predicate, result);
					if (inverse is Entity)
						source.Select(new Statement((Entity)template.Object, (Entity)inverse, template.Subject), filter );
				}
					
				// Find all Y Q X where Q owl:inverseOf P
				foreach (Entity inverse in source.SelectSubjects(owlInverseOf, template.Predicate)) {
					if (filter == null) filter = new InverseFilter(template.Predicate, result);
					source.Select(new Statement((Entity)template.Object, inverse, template.Subject), filter );
				}
				
				// If P owl:Symmetric, find all Y P X.
				if (source.Contains(new Statement(template.Predicate, rdfType, owlSymmetric))) {
					if (filter == null) filter = new InverseFilter(template.Predicate, result);
					source.Select(new Statement((Entity)template.Object, template.Predicate, template.Subject), filter );
				}			
			}
		}

		internal class InverseFilter : StatementSink {
			StatementSink sink;
			Entity predicate;
			public InverseFilter(Entity predicate, StatementSink sink) { this.predicate = predicate; this.sink = sink; }
			public bool Add(Statement statement) {
				if (statement.Object is Entity)
					sink.Add(new Statement((Entity)statement.Object, predicate, statement.Subject));
				return true;
			}
		}

	}
}
