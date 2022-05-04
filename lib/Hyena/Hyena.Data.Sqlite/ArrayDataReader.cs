//
// ArrayDataReader.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Hyena.Data.Sqlite
{
	class ArrayDataReader : IDataReader
	{
		string sql;
		int rows;
		int row = -1;
		int max_got_row = -1;
		List<object[]> data = new List<object[]> ();

		internal ArrayDataReader (IDataReader reader, string sql)
		{
			if (!reader.Read ())
				return;

			FieldCount = reader.FieldCount;
			FieldNames = reader.FieldNames;

			do {
				var vals = new object[FieldCount];
				for (int i = 0; i < FieldCount; i++) {
					vals[i] = reader[i];
				}

				data.Add (vals);
				rows++;
			} while (reader.Read ());

			this.sql = sql;
		}

		public void Dispose ()
		{
			if (rows > 1 && max_got_row < (rows - 1) && Log.Debugging) {
				Log.WarningFormat ("Disposing ArrayDataReader that has {0} rows but we only read {1} of them\n{2}", rows, row, sql);
			}
			row = -1;
			max_got_row = -1;
		}

		public int FieldCount { get; private set; }
		public string[] FieldNames { get; private set; }

		public bool Read ()
		{
			row++;
			max_got_row++;
			return row < rows;
		}

		public object this[int i] {
			get {
				max_got_row = Math.Max (i, max_got_row);
				return data[row][i];
			}
		}

		public object this[string columnName] {
			get { return this[GetColumnIndex (columnName)]; }
		}

		public T Get<T> (int i)
		{
			return (T)Get (i, typeof (T));
		}

		public object Get (int i, Type asType)
		{
			return QueryReader.GetAs (this[i], asType);
		}

		public T Get<T> (string columnName)
		{
			return Get<T> (GetColumnIndex (columnName));
		}

		int GetColumnIndex (string columnName)
		{
			return Array.IndexOf (FieldNames, columnName);
		}
	}
}
