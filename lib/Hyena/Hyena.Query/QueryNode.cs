//
// QueryNode.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Hyena.Query
{
	public enum QueryNodeSearchMethod
	{
		DepthFirst,
		BreadthFirst
	}

	public abstract class QueryNode
	{
		QueryListNode parent;
		int source_column;
		int source_line;

		public QueryNode ()
		{
		}

		public QueryNode (QueryListNode parent)
		{
			Parent = parent;
			Parent.AddChild (this);
		}

		protected void PrintIndent (int depth)
		{
			Console.Write (string.Empty.PadLeft (depth * 2, ' '));
		}

		public void Dump ()
		{
			Dump (0);
		}

		internal virtual void Dump (int depth)
		{
			PrintIndent (depth);
			Console.WriteLine (this);
		}

		public abstract QueryNode Trim ();

		public string ToUserQuery ()
		{
			var sb = new StringBuilder ();
			AppendUserQuery (sb);
			return sb.ToString ();
		}

		public abstract void AppendUserQuery (StringBuilder sb);

		public string ToXml (QueryFieldSet fieldSet)
		{
			return ToXml (fieldSet, false);
		}

		public virtual string ToXml (QueryFieldSet fieldSet, bool pretty)
		{
			var doc = new XmlDocument ();

			XmlElement request = doc.CreateElement ("request");
			doc.AppendChild (request);

			XmlElement query = doc.CreateElement ("query");
			query.SetAttribute ("banshee-version", "1");
			request.AppendChild (query);

			AppendXml (doc, query, fieldSet);

			if (!pretty) {
				return doc.OuterXml;
			}

			using (var sw = new StringWriter ()) {
				using (var xtw = new XmlTextWriter (sw)) {
					xtw.Formatting = System.Xml.Formatting.Indented;
					xtw.Indentation = 2;
					doc.WriteTo (xtw);
					return sw.ToString ();
				}
			}
		}

		public IEnumerable<T> SearchForValues<T> () where T : QueryValue
		{
			return SearchForValues<T> (QueryNodeSearchMethod.DepthFirst);
		}

		public IEnumerable<T> SearchForValues<T> (QueryNodeSearchMethod method) where T : QueryValue
		{
			if (method == QueryNodeSearchMethod.DepthFirst) {
				return SearchForValuesByDepth<T> (this);
			} else {
				return SearchForValuesByBreadth<T> ();
			}
		}

		static IEnumerable<T> SearchForValuesByDepth<T> (QueryNode node) where T : QueryValue
		{
			var list = node as QueryListNode;
			if (list != null) {
				foreach (QueryNode child in list.Children) {
					foreach (T item in SearchForValuesByDepth<T> (child)) {
						yield return item;
					}
				}
			} else {
				var term = node as QueryTermNode;
				if (term != null) {
					var value = term.Value as T;
					if (value != null) {
						yield return value;
					}
				}
			}
		}

		IEnumerable<T> SearchForValuesByBreadth<T> () where T : QueryValue
		{
			var queue = new Queue<QueryNode> ();
			queue.Enqueue (this);
			do {
				QueryNode node = queue.Dequeue ();
				var list = node as QueryListNode;
				if (list != null) {
					foreach (QueryNode child in list.Children) {
						queue.Enqueue (child);
					}
				} else {
					var term = node as QueryTermNode;
					if (term != null) {
						var value = term.Value as T;
						if (value != null) {
							yield return value;
						}
					}
				}
			} while (queue.Count > 0);
		}

		public IEnumerable<QueryField> GetFields ()
		{
			foreach (QueryTermNode term in GetTerms ())
				yield return term.Field;
		}

		public IEnumerable<QueryTermNode> GetTerms ()
		{
			var queue = new Queue<QueryNode> ();
			queue.Enqueue (this);
			do {
				QueryNode node = queue.Dequeue ();
				var list = node as QueryListNode;
				if (list != null) {
					foreach (QueryNode child in list.Children) {
						queue.Enqueue (child);
					}
				} else {
					var term = node as QueryTermNode;
					if (term != null) {
						yield return term;
					}
				}
			} while (queue.Count > 0);
		}

		public override string ToString ()
		{
			return ToUserQuery ();
		}

		public abstract void AppendXml (XmlDocument doc, XmlNode parent, QueryFieldSet fieldSet);

		public virtual string ToSql (QueryFieldSet fieldSet)
		{
			var sb = new StringBuilder ();
			AppendSql (sb, fieldSet);
			return sb.ToString ();
		}

		public abstract void AppendSql (StringBuilder sb, QueryFieldSet fieldSet);

		public QueryListNode Parent {
			get { return parent; }
			set { parent = value; }
		}

		public int SourceColumn {
			get { return source_column; }
			set { source_column = value; }
		}

		public int SourceLine {
			get { return source_line; }
			set { source_line = value; }
		}
	}
}
