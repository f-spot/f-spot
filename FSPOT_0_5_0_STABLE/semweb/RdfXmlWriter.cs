using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;

using SemWeb;

namespace SemWeb {
	public class RdfXmlWriter : RdfWriter {
		XmlWriter writer;
		NamespaceManager ns = new NamespaceManager();
		
		XmlDocument doc;
		bool initialized = false;
		
		Hashtable nodeMap = new Hashtable();
		
		long anonCounter = 0;
		Hashtable anonAlloc = new Hashtable();
		Hashtable nameAlloc = new Hashtable();
		Hashtable nodeReferences = new Hashtable();
		ArrayList predicateNodes = new ArrayList();
		
		static Entity rdftype = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
		
		public RdfXmlWriter(XmlDocument dest) { doc = dest; }
		
		public RdfXmlWriter(string file) : this(GetWriter(file)) { }

		public RdfXmlWriter(TextWriter writer) : this(NewWriter(writer)) { }
		
		private static XmlWriter NewWriter(TextWriter writer) {
			XmlTextWriter ret = new XmlTextWriter(writer);
			ret.Formatting = Formatting.Indented;
			ret.Indentation = 1;
			ret.IndentChar = '\t';
			ret.Namespaces = true;
			return ret;
		}
		
		public RdfXmlWriter(XmlWriter writer) {
			this.writer = writer;
		}
		
		private void Start() {
			if (initialized) return;
			initialized = true;
			
			if (doc == null) doc = new XmlDocument();
			
			doc.AppendChild(doc.CreateXmlDeclaration("1.0", null, null));
			
			string rdfprefix = ns.GetPrefix(NS.RDF);
			if (rdfprefix == null) {
				if (ns.GetNamespace("rdf") == null) {
					rdfprefix = "rdf";
					ns.AddNamespace(NS.RDF, "rdf");
				}
			}
			
			XmlElement root = doc.CreateElement(rdfprefix + ":RDF", NS.RDF);
			foreach (string prefix in ns.GetPrefixes())
				root.SetAttribute("xmlns:" + prefix, ns.GetNamespace(prefix));
			
			if (BaseUri != null)
				root.SetAttribute("xml:base", BaseUri);

			doc.AppendChild(root);
		}
		
		public override NamespaceManager Namespaces { get { return ns; } }
		
		char[] normalizechars = { '#', '/' };
		
		private void Normalize(string uri, out string prefix, out string localname) {
			if (uri == "")
				throw new InvalidOperationException("The empty URI cannot be used as an element node.");
				
			if (BaseUri == null && uri.StartsWith("#")) {
				// This isn't quite right, but it prevents dieing
				// for something not uncommon in N3.  The hash
				// gets lost.
				prefix = "";
				localname = uri.Substring(1);
				return;
			}
		
			if (ns.Normalize(uri, out prefix, out localname))
				return;
				
			// No namespace prefix was registered, so come up with something.
			
			int last = uri.LastIndexOfAny(normalizechars);
			if (last <= 0)
				throw new InvalidOperationException("No namespace was registered and no prefix could be automatically generated for <" + uri + ">");
				
			int prev = uri.LastIndexOfAny(normalizechars, last-1);
			if (prev <= 0)
				throw new InvalidOperationException("No namespace was registered and no prefix could be automatically generated for <" + uri + ">");
			
			string n = uri.Substring(0, last+1);
			localname = uri.Substring(last+1);
			
			// TODO: Make sure the local name (here and anywhere in this
			// class) is a valid XML name.
			if (Namespaces.GetPrefix(n) != null) {
				prefix = Namespaces.GetPrefix(n);
				return;
			}
			
			prefix = uri.Substring(prev+1, last-prev-1);

			if (prefix == "xmlns")
				prefix = "";

			
			// Remove all non-xmlable (letter) characters.
			StringBuilder newprefix = new StringBuilder();
			foreach (char c in prefix)
				if (char.IsLetter(c))
					newprefix.Append(c);
			prefix = newprefix.ToString();
			
			if (prefix.Length == 0) {
				// There were no letters in the prefix!
				prefix = "ns";
			}
			
			if (Namespaces.GetNamespace(prefix) == null) {
				doc.DocumentElement.SetAttribute("xmlns:" + prefix, n);
				Namespaces.AddNamespace(n, prefix);
				return;
			}
			
			int ctr = 1;
			while (true) {
				if (Namespaces.GetNamespace(prefix + ctr) == null) {
					prefix += ctr;
					doc.DocumentElement.SetAttribute("xmlns:" + prefix, n);
					Namespaces.AddNamespace(n, prefix);
					return;
				}
				ctr++;
			}
		}
		
		private void SetAttribute(XmlElement element, string nsuri, string prefix, string localname, string val) {
			XmlAttribute attr = doc.CreateAttribute(prefix, localname, nsuri);
			attr.Value = val;
			element.SetAttributeNode(attr);
		}
		
		private XmlElement GetNode(Entity entity, string type, XmlElement context) {
			string uri = entity.Uri;
		
			if (nodeMap.ContainsKey(entity)) {
				XmlElement ret = (XmlElement)nodeMap[entity];
				if (type == null) return ret;
				
				// Check if we have to add new type information to the existing node.
				if (ret.NamespaceURI + ret.LocalName == NS.RDF + "Description") {
					// Replace the untyped node with a typed node, copying in
					// all of the children of the old node.
					string prefix, localname;
					Normalize(type, out prefix, out localname);
					XmlElement newnode = doc.CreateElement(prefix + ":" + localname, ns.GetNamespace(prefix));
					
					foreach (XmlNode childnode in ret) {
						newnode.AppendChild(childnode.Clone());
					}
					
					ret.ParentNode.ReplaceChild(newnode, ret);
					nodeMap[entity] = newnode;
					return newnode;
				} else {
					// The node is already typed, so just add a type predicate.
					XmlElement prednode = CreatePredicate(ret, NS.RDF + "type");
					SetAttribute(prednode, NS.RDF, ns.GetPrefix(NS.RDF), "resource", type);
					return ret;
				}
			}
			
			Start();			
			
			XmlElement node;
			if (type == null) {
				node = doc.CreateElement(ns.GetPrefix(NS.RDF) + ":Description", NS.RDF);
			} else {
				string prefix, localname;
				Normalize(type, out prefix, out localname);
				node = doc.CreateElement(prefix + ":" + localname, ns.GetNamespace(prefix));
			}
			
			if (uri != null) {
				string fragment;
				if (!Relativize(uri, out fragment))
					SetAttribute(node, NS.RDF, ns.GetPrefix(NS.RDF), "about", uri);
				else if (fragment.Length == 0)
					SetAttribute(node, NS.RDF, ns.GetPrefix(NS.RDF), "about", "");
				else
					SetAttribute(node, NS.RDF, ns.GetPrefix(NS.RDF), "ID", fragment.Substring(1)); // chop off hash
			} else {
				// The nodeID attribute will be set the first time the node is referenced,
				// in case it's never referenced so we don't need to put a nodeID on it.
			}
			
			if (context == null)
				doc.DocumentElement.AppendChild(node);
			else
				context.AppendChild(node);
			
			nodeMap[entity] = node;
			return node;
		}
		
		private XmlElement CreatePredicate(XmlElement subject, Entity predicate) {
			if (predicate.Uri == null)
				throw new InvalidOperationException("Predicates cannot be blank nodes.");
			
			string prefix, localname;
			Normalize(predicate.Uri, out prefix, out localname);
			XmlElement pred = doc.CreateElement(prefix + ":" + localname, ns.GetNamespace(prefix));
			subject.AppendChild(pred);
			predicateNodes.Add(pred);
			return pred;
		}
		
		public override void Add(Statement statement) {
			if (statement.AnyNull) throw new ArgumentNullException();
		
			XmlElement subjnode;
			
			bool hastype = statement.Predicate == rdftype && statement.Object.Uri != null;
			subjnode = GetNode(statement.Subject, hastype ? statement.Object.Uri : null, null);
			if (hastype) return;

			XmlElement prednode = CreatePredicate(subjnode, statement.Predicate);
			
			if (!(statement.Object is Literal)) {
				if (nodeMap.ContainsKey(statement.Object)) {
					if (statement.Object.Uri != null) {
						string uri = statement.Object.Uri, fragment;
						if (Relativize(statement.Object.Uri, out fragment))
							uri = fragment;
						SetAttribute(prednode, NS.RDF, ns.GetPrefix(NS.RDF), "resource", uri);
					} else {
						SetAttribute(prednode, NS.RDF, ns.GetPrefix(NS.RDF), "nodeID", GetBNodeRef((BNode)statement.Object));
						
						// If this is the first reference to the bnode, put its nodeID on it, since we
						// delayed setting that attribute until we needed it.
						SetAttribute((XmlElement)nodeMap[statement.Object], NS.RDF, ns.GetPrefix(NS.RDF), "nodeID", GetBNodeRef((BNode)statement.Object));
					}

					// Track at most one reference to this entity as a statement object
					if (nodeReferences.ContainsKey(nodeMap[statement.Object]))
						nodeReferences[nodeMap[statement.Object]] = null;
					else
						nodeReferences[nodeMap[statement.Object]] = prednode;
				} else {
					GetNode((Entity)statement.Object, null, prednode);
				}
			} else {
				Literal literal = (Literal)statement.Object;
				if (literal.DataType != null && literal.DataType == "http://www.w3.org/1999/02/22-rdf-syntax-ns#XMLLiteral") {
					prednode.InnerXml = literal.Value;
					SetAttribute(prednode, NS.RDF, ns.GetPrefix(NS.RDF), "parseType", "Literal");
				} else {
					prednode.InnerText = literal.Value;
					if (literal.Language != null)
						prednode.SetAttribute("xml:lang", literal.Language);
					if (literal.DataType != null)
						SetAttribute(prednode, NS.RDF, ns.GetPrefix(NS.RDF), "datatype", literal.DataType);
				}
			}
		}
		
		private string GetBNodeRef(BNode node) {
			if (node.LocalName != null &&
				(nameAlloc[node.LocalName] == null || (BNode)nameAlloc[node.LocalName] == node)
				&& !node.LocalName.StartsWith("bnode")) {
				nameAlloc[node.LocalName] = node; // ensure two different nodes with the same local name don't clash
				return node.LocalName;
			} else if (anonAlloc[node] != null) {
				return (string)anonAlloc[node];
			} else {
				string id = "bnode" + (anonCounter++);
				anonAlloc[node] = id;
				return id;
			}
		}
		
		public override void Close() {
			Start(); // make sure the document node was written

			// For any node that was referenced by exactly one predicate,
			// move the node into that predicate, provided the subject
			// isn't itself!
			foreach (DictionaryEntry e in nodeReferences) {
				if (e.Value == null) continue; // referenced by more than one predicate
				XmlElement node = (XmlElement)e.Key;
				XmlElement predicate = (XmlElement)e.Value;
				if (node.ParentNode != node.OwnerDocument.DocumentElement) continue; // already referenced somewhere
				if (predicate.ParentNode == node) continue; // can't insert node as child of itself
				node.ParentNode.RemoveChild(node);
				predicate.AppendChild(node);
				predicate.RemoveAttribute("resource", NS.RDF); // it's on the lower node
				predicate.RemoveAttribute("nodeID", NS.RDF); // it's on the lower node
				node.RemoveAttribute("nodeID", NS.RDF); // not needed anymore
			}
			
			// Predicates that have rdf:Description nodes 1) with only literal
			// properties (with no language/datatype/parsetype) can be
			// condensed by putting the literals onto the predicate as
			// attributes, 2) with no literal attributes but resource
			// objects can be condensed by using parseType=Resource.
			foreach (XmlElement pred in predicateNodes) {
				// Is this a property with a resource as object?
				if (!(pred.FirstChild is XmlElement)) continue; // literal value
				if (pred.Attributes.Count > 0) continue; // parseType=Literal already
				
				// Make sure this resource is not typed
				XmlElement obj = (XmlElement)pred.FirstChild;
				if (obj.NamespaceURI + obj.LocalName != NS.RDF + "Description") continue; // untyped
				
				// And make sure it has no attributes already but an rdf:about
				if (obj.Attributes.Count > 1) continue; // at most a rdf:about attribute
				if (obj.Attributes.Count == 1 && obj.Attributes[0].NamespaceURI+obj.Attributes[0].LocalName != NS.RDF+"about") continue;
				
				// See if all its predicates are literal with no attributes.
				bool allSimpleLits = true;
				foreach (XmlElement opred in obj.ChildNodes) {
					if (opred.FirstChild is XmlElement)
						allSimpleLits = false;
					if (opred.Attributes.Count > 0)
						allSimpleLits = false;
				}
						
				if (allSimpleLits) {
					// Condense by moving all of obj's elements to attributes of the predicate,
					// and turning a rdf:about into a rdf:resource, and then remove obj completely.
					if (obj.Attributes.Count == 1)
						SetAttribute(pred, NS.RDF, ns.GetPrefix(NS.RDF), "resource", obj.Attributes[0].Value);
					foreach (XmlElement opred in obj.ChildNodes)
						SetAttribute(pred, opred.NamespaceURI, ns.GetPrefix(opred.NamespaceURI), opred.LocalName, opred.InnerText);
					pred.RemoveChild(obj);
					
					if (pred.ChildNodes.Count == 0) pred.IsEmpty = true;

				} else if (obj.Attributes.Count == 0) { // no rdf:about
					// Condense this node using parseType=Resource
					pred.RemoveChild(obj);
					foreach (XmlElement opred in obj.ChildNodes)
						pred.AppendChild(opred.Clone());
					SetAttribute(pred, NS.RDF, ns.GetPrefix(NS.RDF), "parseType", "Resource");
				}
			}

			base.Close();
			
			if (writer != null) {
				doc.WriteTo(writer);
				//writer.Close();
			}
		}
		
		bool Relativize(string uri, out string fragment) {
			fragment = null;
			if (BaseUri == null) return false;
			if (!uri.StartsWith(BaseUri) || uri.Length < BaseUri.Length) return false;
			string rel = uri.Substring(BaseUri.Length);
			if (rel == "") { fragment = ""; return true; }
			if (rel[0] == '#') { fragment = rel; return true; }
			return false;
		}
		
	}

}
