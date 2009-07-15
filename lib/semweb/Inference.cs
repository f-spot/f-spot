using System;
using System.Collections;
using SemWeb;

namespace SemWeb.Inference {
	
	public class Rule {
		public readonly Statement[] Antecedent;
		public readonly Statement[] Consequent;
	
		public Rule(Statement[] antecedent, Statement[] consequent) {
			Antecedent = antecedent;
			Consequent = consequent;
		}
		
		public override string ToString() {
			string ret = "{";
			foreach (Statement s in Antecedent)
				ret += " " + s.ToString();
			ret += " } => {";
			foreach (Statement s in Consequent)
				ret += " " + s.ToString();
			ret += " }";
			return ret;
		}
	}
	
	public class ProofStep {
		public readonly Rule Rule;
		public readonly IDictionary Substitutions;
		
		public ProofStep(Rule rule, IDictionary substitutions) {
			Rule = rule;
			Substitutions = substitutions;
		}
	}
	
	public class Proof {
		public readonly Statement[] Proved;
		public readonly ProofStep[] Steps;
		
		public Proof(Statement[] proved, ProofStep[] steps) {
			Proved = proved;
			Steps = steps;
		}
	}
}
