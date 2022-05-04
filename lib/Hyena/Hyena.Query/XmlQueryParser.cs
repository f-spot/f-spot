//
// XmlQueryParser.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Xml;

namespace Hyena.Query
{
	public class XmlQueryParser : QueryParser
	{
		string str;
		QueryFieldSet field_set;

		public static QueryNode Parse (string input, QueryFieldSet fieldSet)
		{
			return new XmlQueryParser (input).BuildTree (fieldSet);
		}

		public XmlQueryParser () : base () { }

		public XmlQueryParser (string str)
		{
			this.str = str;
		}

		public override QueryNode BuildTree (QueryFieldSet fieldSet)
		{
			field_set = fieldSet;
			var doc = new XmlDocument ();
			try {
				doc.LoadXml (str);
				var request = doc.FirstChild as XmlElement;
				if (request == null || request.Name != "request")
					throw new Exception ("Invalid request");

				var query = request.FirstChild as XmlElement;
				if (query == null || query.Name != "query" || query.GetAttribute ("banshee-version") != "1")
					throw new Exception ("Invalid query");

				QueryNode node = Parse (query.FirstChild as XmlElement, null);
				return node?.Trim ();
			} catch (Exception) {
			}
			return null;
		}

		QueryNode Parse (XmlElement node, QueryListNode parent)
		{
			if (node == null)
				return null;

			QueryListNode list = null;
			//Console.WriteLine ("Parsing node: {0}", node.Name);
			switch (node.Name.ToLower ()) {
			case "and":
				list = new QueryListNode (Keyword.And);
				break;
			case "or":
				list = new QueryListNode (Keyword.Or);
				break;
			case "not":
				list = new QueryListNode (Keyword.Not);
				break;
			default:
				var term = new QueryTermNode ();

				// Get the field (if any) that this term applies to
				if (node["field"] != null)
					term.Field = field_set[node["field"].GetAttribute ("name")];

				// Get the value
				term.Value = QueryValue.CreateFromXml (node, term.Field);

				// Get the operator from the term's name
				term.Operator = term.Value.OperatorSet[node.Name];


				if (parent != null) {
					parent.AddChild (term);
				}

				return term;
			}

			if (list != null) {
				if (parent != null)
					parent.AddChild (list);

				// Recursively parse the children of a QueryListNode
				foreach (XmlNode child in node.ChildNodes) {
					Parse (child as XmlElement, list);
				}
			}

			return list;
		}

		public override void Reset ()
		{
		}
	}
}
