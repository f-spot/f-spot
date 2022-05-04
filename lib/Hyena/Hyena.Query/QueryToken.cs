//
// QueryToken.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Query
{
	public enum TokenID
	{
		Unknown,
		OpenParen,
		CloseParen,
		Not,
		Or,
		And,
		Range,
		Term
	}

	public class QueryToken
	{
		TokenID id;
		int line;
		int column;
		string term;

		public QueryToken ()
		{
		}

		public QueryToken (string term)
		{
			id = TokenID.Term;
			this.term = term;
		}

		public QueryToken (TokenID id)
		{
			this.id = id;
		}

		public QueryToken (TokenID id, int line, int column)
		{
			this.id = id;
			this.line = line;
			this.column = column;
		}

		public TokenID ID {
			get { return id; }
			set { id = value; }
		}

		public int Line {
			get { return line; }
			set { line = value; }
		}

		public int Column {
			get { return column; }
			set { column = value; }
		}

		public string Term {
			get { return term; }
			set { term = value; }
		}
	}
}
