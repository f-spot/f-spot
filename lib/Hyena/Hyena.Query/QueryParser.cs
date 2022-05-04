//
// QueryParser.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;

namespace Hyena.Query
{
	public abstract class QueryParser
	{
		protected StreamReader reader;

		public QueryParser ()
		{
			Reset ();
		}

		public QueryParser (string inputQuery) : this (new MemoryStream (Encoding.UTF8.GetBytes (inputQuery)))
		{
		}

		public QueryParser (Stream stream) : this (new StreamReader (stream))
		{
		}

		public QueryParser (StreamReader reader) : this ()
		{
			InputReader = reader;
		}

		public abstract QueryNode BuildTree (QueryFieldSet fieldSet);
		public abstract void Reset ();

		public StreamReader InputReader {
			get { return reader; }
			set { reader = value; }
		}
	}
}
