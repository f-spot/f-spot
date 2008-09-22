/*
 * FSpot.Tag
 * 
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Gdk;
using FSpot.Utils;

namespace FSpot
{
	public class Tag : DbItem, IComparable, IDisposable {
		private string name;
		public string Name {
			set {
				name = value;
			}
			get {
				return name;
			}
		}
	
		private Category category;
		public Category Category {
			set {
				if (Category != null)
					Category.RemoveChild (this);
	
				category = value;
				if (category != null)
					category.AddChild (this);
			}
			get {
				return category;
			}
		}
	
		private int sort_priority;
		public int SortPriority {
			set { sort_priority = value; }
			get { return sort_priority; }
		}
	
		private int popularity = 0;
		public int Popularity {
			get { return popularity; }
			set { popularity = value; }
		}

		// Icon.  If theme_icon_name is not null, then we save the name of the icon instead
		// of the actual icon data.
	
		private string theme_icon_name;
		public string ThemeIconName {
			set {
				theme_icon_name = value;
				cached_icon_size = IconSize.Hidden;
				icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, theme_icon_name, 48, (Gtk.IconLookupFlags)0);
			}
			get { return theme_icon_name; }
		}
	
		private Pixbuf icon;
		public Pixbuf Icon {
			set {
				theme_icon_name = null;
				if (icon != null)
					icon.Dispose ();
				icon = value;
				cached_icon_size = IconSize.Hidden;
			}
			get { return icon; }
		}
	
		public enum IconSize {
			Hidden = 0,
			Small = 16,
			Medium = 24,
			Large = 48
		};
	
		private static IconSize tag_icon_size = IconSize.Large;
		public static IconSize TagIconSize {
			get { return tag_icon_size; }
			set { tag_icon_size = value; }
		}
	
		private Pixbuf cached_icon;
		private IconSize cached_icon_size = IconSize.Hidden;
	
		// We can use a SizedIcon everywhere we were using an Icon
		public Pixbuf SizedIcon {
			get {
				if (tag_icon_size == IconSize.Hidden) //Hidden
					return null;
				if (tag_icon_size == cached_icon_size)
					return cached_icon;
				if (theme_icon_name != null) { //Theme icon
					if (cached_icon != null)
						cached_icon.Dispose ();
					cached_icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, theme_icon_name, (int) tag_icon_size, (Gtk.IconLookupFlags)0);
	
					if (Math.Max (cached_icon.Width, cached_icon.Height) <= (int) tag_icon_size) 
						return cached_icon;
				}
				if (Math.Max (icon.Width, icon.Height) >= (int) tag_icon_size) { //Don't upscale
					if (cached_icon != null)
						cached_icon.Dispose ();
					cached_icon = icon.ScaleSimple ((int) tag_icon_size, (int) tag_icon_size, InterpType.Bilinear);
					cached_icon_size = tag_icon_size;
					return cached_icon;
				}
				else
					return icon;
			}	
		}
	
	
		// You are not supposed to invoke these constructors outside of the TagStore class.
		public Tag (Category category, uint id, string name)
			: base (id)
		{
			Category = category;
			Name = name;
		}
	
	
		// IComparer.
		public int CompareTo (object obj)
		{
			Tag tag = obj as Tag;
	
			if (Category == tag.Category) {
				if (SortPriority == tag.SortPriority)
					return Name.CompareTo (tag.Name);
				else
					return SortPriority - tag.SortPriority;
			} else {
				return Category.CompareTo (tag.Category);
			}
		}
		
		public bool IsAncestorOf (Tag tag)
		{
			for (Category parent = tag.Category; parent != null; parent = parent.Category) {
				if (parent == this)
					return true;
			}
	
			return false;
		}

		public void Dispose ()
		{
			if (icon != null)
				icon.Dispose ();
			if (cached_icon != null)
				cached_icon.Dispose ();
			if (category != null)
				category.Dispose ();
			System.GC.SuppressFinalize (this);
		}

		~Tag ()
		{
			Log.DebugFormat ("Finalizer called on {0}. Should be Disposed", GetType ());		
			if (icon != null)
				icon.Dispose ();
			if (cached_icon != null)
				cached_icon.Dispose ();
			if (category != null)
				category.Dispose ();
		}
	}
}
