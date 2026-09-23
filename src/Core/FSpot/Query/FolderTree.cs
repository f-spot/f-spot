// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FSpot.Utils;

using Hyena;

namespace FSpot.Query
{
	public readonly struct FolderCount
	{
		public FolderCount (SafeUri uri, int count)
		{
			Uri = uri ?? throw new ArgumentNullException (nameof (uri));
			Count = count;
		}

		public SafeUri Uri { get; }
		public int Count { get; }
	}

	public sealed class FolderTreeNode
	{
		public FolderTreeNode (string name, SafeUri uri, int count, IReadOnlyList<FolderTreeNode> children)
		{
			Name = name ?? throw new ArgumentNullException (nameof (name));
			Uri = uri ?? throw new ArgumentNullException (nameof (uri));
			Count = count;
			Children = children ?? throw new ArgumentNullException (nameof (children));
		}

		public string Name { get; }
		public SafeUri Uri { get; }
		public int Count { get; }
		public IReadOnlyList<FolderTreeNode> Children { get; }
	}

	public static class FolderTree
	{
		public static IReadOnlyList<FolderTreeNode> Build (IEnumerable<FolderCount> folders)
		{
			if (folders == null)
				throw new ArgumentNullException (nameof (folders));

			var roots = new Dictionary<string, Mutable> (StringComparer.Ordinal);
			foreach (var folder in folders.OrderBy (item => item.Uri.AbsoluteUri, StringComparer.Ordinal)) {
				string[] segments = Segments (folder.Uri);
				if (segments.Length == 0)
					continue;

				if (!roots.TryGetValue (segments[0], out var node)) {
					node = new Mutable (segments[0], new SafeUri (segments[0] + ":///", true));
					roots[segments[0]] = node;
				}

				var current = node;
				for (int i = 1; i < segments.Length; i++) {
					current = current.Child (segments[i]);
				}

				current.DirectCount += folder.Count;
			}

			return roots.Values
				.SelectMany (root => root.Freeze ().Children)
				.Where (node => node.Count > 0)
				.ToList ();
		}

		static string[] Segments (SafeUri uri)
		{
			string text = uri.AbsoluteUri;
			int index = text.IndexOf ("://", StringComparison.Ordinal);
			string path = index >= 0 ? text.Substring (index + 3) : text;
			if (path.Length == 0 || path[0] != '/')
				path = "/" + path;
			string[] parts = path.TrimEnd ('/').Split ('/');
			parts[0] = uri.Scheme;
			return parts;
		}

		sealed class Mutable
		{
			readonly Dictionary<string, Mutable> children = new Dictionary<string, Mutable> (StringComparer.Ordinal);

			public Mutable (string rawSegment, SafeUri uri)
			{
				RawSegment = rawSegment;
				Uri = uri;
			}

			public string RawSegment { get; }
			public SafeUri Uri { get; }
			public int DirectCount;

			public Mutable Child (string rawSegment)
			{
				if (!children.TryGetValue (rawSegment, out var child)) {
					child = new Mutable (rawSegment, Uri.Append (rawSegment + "/"));
					children[rawSegment] = child;
				}

				return child;
			}

			public FolderTreeNode Freeze ()
			{
				var frozen = children.Values
					.OrderBy (child => child.RawSegment, StringComparer.Ordinal)
					.Select (child => child.Freeze ())
					.Where (child => child.Count > 0)
					.ToList ();
				int count = DirectCount + frozen.Sum (child => child.Count);
				return new FolderTreeNode (System.Uri.UnescapeDataString (RawSegment), Uri, count, frozen);
			}
		}
	}
}
