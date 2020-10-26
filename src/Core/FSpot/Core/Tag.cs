//
// Tag.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2013-2019 Stephen Shaw
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
			get => category;
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
					icon = GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, ThemeIconName, 48, (Gtk.IconLookupFlags)0);
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

		public static IconSize TagIconSize { get; set; } = IconSize.Large;

		Pixbuf cached_icon;
		IconSize cached_icon_size = IconSize.Hidden;

		// We can use a SizedIcon everywhere we were using an Icon
		public Pixbuf SizedIcon {
			get {
				if (TagIconSize == IconSize.Hidden) //Hidden
					return null;
				if (TagIconSize == cached_icon_size)
					return cached_icon;
				if (ThemeIconName != null) { //Theme icon
					if (cached_icon != null)
						cached_icon.Dispose ();
					try {
						cached_icon = GtkUtil.TryLoadIcon (FSpotConfiguration.IconTheme, ThemeIconName, (int)TagIconSize, (Gtk.IconLookupFlags)0);

						if (Math.Max (cached_icon.Width, cached_icon.Height) <= (int)TagIconSize)
							return cached_icon;
					} catch (Exception) {
						Console.WriteLine ($"missing theme icon: {ThemeIconName}");
						return null;
					}
				}
				if (Icon == null)
					return null;

				if (Math.Max (Icon.Width, Icon.Height) >= (int)TagIconSize) { //Don't upscale
					if (cached_icon != null)
						cached_icon.Dispose ();
					cached_icon = Icon.ScaleSimple ((int)TagIconSize, (int)TagIconSize, InterpType.Bilinear);
					cached_icon_size = TagIconSize;
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
				throw new ArgumentNullException (nameof (tag));

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
				throw new ArgumentNullException (nameof (tag));

			for (Category parent = tag.Category; parent != null; parent = parent.Category) {
				if (parent == this)
					return true;
			}

			return false;
		}

		public void Dispose ()
		{
			Dispose (true);
			System.GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
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
