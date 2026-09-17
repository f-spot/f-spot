//
// ResourceLoader.
//  https://github.com/PintaProject/Pinta/blob/dd226b9a3f0f82d44db0c202dacecab375efcf92/Pinta.Resources/ResourceManager.cs
//
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//
// Copyright (c) 2010 Jonathan Pobst
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

using Gdk;

namespace FSpot.Resources
{
	public static class ResourceLoader
	{
		static bool HasResource (Assembly asm, string name)
		{
			string[] resources = asm.GetManifestResourceNames ();

			if (Array.IndexOf (resources, name) > -1)
				return true;
			else
				return false;
		}

		[MethodImpl (MethodImplOptions.NoInlining)]
		public static Pixbuf GetIcon (string name, int size)
		{
			Pixbuf result = null;
			try {
				// First see if it's a built-in gtk icon, like gtk-new.
				// This will also load any icons added by Gtk.IconFactory.AddDefault() .
				string stock = FSpot.Utils.IconNameMap.StockId (name);
				var icon_set = Gtk.Widget.DefaultStyle.LookupIconSet (stock);
				if (icon_set == null && stock != name)
					icon_set = Gtk.Widget.DefaultStyle.LookupIconSet (name);
				if (icon_set != null && FSpot.Utils.GtkIconSizeMap.TryMapPixels (size, out int gtkSize)) {
					result = icon_set.RenderIcon (Gtk.Widget.DefaultStyle, Gtk.Widget.DefaultDirection,
						Gtk.StateType.Normal, (Gtk.IconSize)gtkSize, null, null);
				}
				// Otherwise, get it from our embedded resources.
				if (result == null) {
					var image = $"{name}-{size}.png";
					if (HasResource (ResourceAssembly.Instance, image))
						result = new Pixbuf (ResourceAssembly.Instance, image);
				}

				//Maybe we can find the icon in the resource of a different assembly (e.g. Plugin)
				if (result == null) {
					foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
						if (HasResource (asm, name))
							result = new Pixbuf (asm, name);
					}
				}
			} catch (Exception ex) {
				Console.Error.WriteLine (ex.Message);
			}

			// Ensure that we don't crash if an icon is missing for some reason.
			if (result == null) {
				try {
					// Try to return gtk's default missing image
					if (name != Gtk.Stock.MissingImage) {
						result = GetIcon (Gtk.Stock.MissingImage, size);
					} else {
						// If gtk is missing it's "missing image", we'll create one on the fly
						result = CreateMissingImage (size);
					}
				} catch (Exception ex) {
					Console.Error.WriteLine (ex.Message);
				}
			}
			return result;
		}

		public static Stream GetResourceIconStream (string name)
		{
			return ResourceAssembly.Instance.GetManifestResourceStream (name);
		}

		static Pixbuf CreateMissingImage (int size)
		{
			return FSpot.Utils.GtkUtil.MissingImagePlaceholder (size);
		}
	}
}