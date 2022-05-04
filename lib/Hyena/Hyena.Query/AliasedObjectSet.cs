//
// AliasedObjectSet.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace Hyena.Query
{
	public interface IAliasedObject
	{
		string Name { get; }
		string[] Aliases { get; }
	}

	public class AliasedObjectSet<T> : IEnumerable<T> where T : IAliasedObject
	{
		protected Dictionary<string, T> map = new Dictionary<string, T> ();
		protected List<string> aliases = new List<string> ();
		protected T[] objects;

		public AliasedObjectSet (params T[] objects)
		{
			this.objects = objects;
			foreach (T obj in objects) {
				map[obj.Name.ToLower ()] = obj;
				foreach (string alias in obj.Aliases) {
					if (!string.IsNullOrEmpty (alias) && alias.IndexOf (" ") == -1) {
						foreach (string sub_alias in alias.Split (',')) {
							string lower_alias = sub_alias.ToLower ();
							map[lower_alias] = obj;
							if (!aliases.Contains (lower_alias)) {
								aliases.Add (lower_alias);
							}
						}
					}
				}
			}
			aliases.Sort (SortByLongest);
		}

		int SortByLongest (string a, string b)
		{
			return b.Length.CompareTo (a.Length);
		}

		public string FindAlias (string input)
		{
			input = input.ToLower ();
			foreach (string alias in aliases) {
				if (input.StartsWith (alias)) {
					return alias;
				}
			}
			return null;
		}

		public T[] Objects {
			get { return objects; }
		}

		public T First {
			get { return objects[0]; }
		}

		public IEnumerator<T> GetEnumerator ()
		{
			foreach (T o in objects) {
				yield return o;
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		public T this[string alias] {
			get {
				if (!string.IsNullOrEmpty (alias) && map.ContainsKey (alias.ToLower ()))
					return map[alias.ToLower ()];
				return default;
			}
		}
	}
}
