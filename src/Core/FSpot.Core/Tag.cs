//
// Tag.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2008 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Gdk;

using FSpot.Settings;
using FSpot.Utils;

namespace FSpot.Core
{
	public class Tag : DbItem, IComparable<Tag>, IDisposable
	{
		public string Name { get; set; }
		public int Popularity { get; set; }
		public int SortPriority { get; set; }
		// Icon.  If ThemeIconName is not null, then we save the name of the icon instead
		// of the actual icon data.
		public string ThemeIconName { get; set; }
		public bool IconWasCleared { get; set; }
		bool disposed;

		Category category;
		public Category Category {
			get { return category; }
			set {
				if (Category != null)
					Category.RemoveChild (this);

				category = value;
				if (category != null)
					category.AddChild (this);
			}
		}

		Pixbuf icon;
		public Pixbuf Icon {
			get {
				if (icon == null && ThemeIconName != null) {
					cached_icon_size = IconSize.Hidden;
					icon = GtkUtil.TryLoadIcon (FSpot.Settings.Global.IconTheme, ThemeIconName, 48, (Gtk.IconLookupFlags)0);
				}
				return icon;
			}
			set {
				ThemeIconName = null;
				if (icon != null)
					icon.Dispose ();
				icon = value;
				cached_icon_size = IconSize.Hidden;
				IconWasCleared = value == null;
			}
		}

		static IconSize tag_icon_size = IconSize.Large;
		public static IconSize TagIconSize {
			get { return tag_icon_size; }
			set { tag_icon_size = value; }
		}

		Pixbuf cached_icon;
		IconSize cached_icon_size = IconSize.Hidden;

		// We can use a SizedIcon everywhere we were using an Icon
		public Pixbuf SizedIcon {
			get {
				if (tag_icon_size == IconSize.Hidden) //Hidden
					return null;
				if (tag_icon_size == cached_icon_size)
					return cached_icon;
				if (ThemeIconName != null) { //Theme icon
					if (cached_icon != null)
						cached_icon.Dispose ();
					try {
						cached_icon = GtkUtil.TryLoadIcon (FSpot.Settings.Global.IconTheme, ThemeIconName, (int)tag_icon_size, (Gtk.IconLookupFlags)0);

						if (Math.Max (cached_icon.Width, cached_icon.Height) <= (int)tag_icon_size)
							return cached_icon;
					} catch (Exception) {
						Console.WriteLine ("missing theme icon: {0}", ThemeIconName);
						return null;
					}
				}
				if (Icon == null)
					return null;

				if (Math.Max (Icon.Width, Icon.Height) >= (int)tag_icon_size) { //Don't upscale
					if (cached_icon != null)
						cached_icon.Dispose ();
					cached_icon = Icon.ScaleSimple ((int)tag_icon_size, (int)tag_icon_size, InterpType.Bilinear);
					cached_icon_size = tag_icon_size;
					return cached_icon;
				}
				return Icon;
			}
		}

		// FIXME: Why does this ctor take one of its derived classes as a parameter?
		// You are not supposed to invoke these constructors outside of the TagStore class.
		public Tag (Category category, uint id, string name) : base (id)
		{
			Category = category;
			Name = name;
			Popularity = 0;
			IconWasCleared = false;
		}

		// IComparer
		public int CompareTo (Tag tag)
		{
			if (tag == null)
				throw new ArgumentNullException ("tag");

			if (Category == tag.Category) {
				if (SortPriority == tag.SortPriority)
					return Name.CompareTo (tag.Name);
				return SortPriority - tag.SortPriority;
			}
			return Category.CompareTo (tag.Category);
		}

		public bool IsAncestorOf (Tag tag)
		{
			if (tag == null)
				throw new ArgumentNullException ("tag");

			for (Category parent = tag.Category; parent != null; parent = parent.Category) {
				if (parent == this)
					return true;
			}

			return false;
		}

		public void Dispose()
		{
			Dispose(true);
			System.GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing) {
				// free managed resources
				if (icon != null) {
					icon.Dispose ();
					icon = null;
				}
				if (cached_icon != null) {
					cached_icon.Dispose ();
					cached_icon = null;
				}
			}
			// free unmanaged resources
		}
	}
}
