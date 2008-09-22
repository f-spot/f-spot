using System;
using System.Collections;
using System.IO;
using System.Text;

using SemWeb;
using SemWeb.Reasoning;

namespace SemWeb.Stores {

	public class KnowledgeModel : Store {
		
		SemWeb.Stores.MultiStore stores;
		Store mainstore;
		
		public KnowledgeModel() {
			stores = new SemWeb.Stores.MultiStore();
			mainstore = stores;
		}
		
		public KnowledgeModel(RdfReader parser) : this() {
			stores.Add(new MemoryStore(parser));
		}

		public SemWeb.Stores.MultiStore Storage { get { return stores; } }
		
		public void Add(Store storage) {
			Storage.Add(storage);
		}
		
		public void AddReasoning(ReasoningEngine engine) {
			mainstore = new InferenceStore(mainstore, engine);
		}
		
		public override Entity[] GetAllEntities() { return stores.GetAllEntities(); }
		
		public override Entity[] GetAllPredicates() { return stores.GetAllPredicates(); }
		
		public override bool Contains(Statement statement) {
			return mainstore.Contains(statement);
		}
		
		public override void Select(Statement template, SelectPartialFilter partialFilter, StatementSink result) {
			mainstore.Select(template, partialFilter, result);
		}
		
		public override void Select(Statement[] templates, SelectPartialFilter partialFilter, StatementSink result) {
			mainstore.Select(templates, partialFilter, result);
		}

		public override int StatementCount { get { return stores.StatementCount; } }

		public override void Clear() { throw new InvalidOperationException(); }
		public override void Add(Statement statement) { throw new InvalidOperationException(); }
		public override void Remove(Statement statement) { throw new InvalidOperationException(); }
		
		public override void Replace(Entity a, Entity b) {
			mainstore.Replace(a, b);
		}
		
		public override void Replace(Statement find, Statement replacement) {
			mainstore.Replace(find, replacement);
		}
		
		public override Entity[] FindEntities(Statement[] filters) {
			return mainstore.FindEntities(filters);
		}
	}

		
}
