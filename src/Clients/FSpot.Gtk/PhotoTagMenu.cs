//
// PhotoTagMenu.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2004-2006 Novell, Inc.
// Copyright (C) 2004, 2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot;
using FSpot.Core;

using Gtk;

using Hyena;

public class PhotoTagMenu : Menu
{
	public delegate void TagSelectedHandler (Tag t);
	public event TagSelectedHandler TagSelected;

	public PhotoTagMenu ()
	{
	}

	protected PhotoTagMenu (IntPtr raw) : base (raw) {}

	public void Populate (List<Photo> photos) {
		var dict = new Dictionary<uint, Tag> ();
		if (photos != null) {
			foreach (var p in photos) {
				foreach (Tag t in p.Tags) {
					if (!dict.ContainsKey (t.Id)) {
						dict.Add (t.Id, t);
					}
				}
			}
		}

		foreach (Widget w in Children) {
			w.Destroy ();
		}

		if (dict.Count == 0) {
			/* Fixme this should really set parent menu
			   items insensitve */
			MenuItem item = new MenuItem (Mono.Unix.Catalog.GetString ("(No Tags)"));
			this.Append (item);
			item.Sensitive = false;
			item.ShowAll ();
			return;
		}

		foreach (Tag t in dict.Values) {
			MenuItem item = new TagMenu.TagMenuItem (t);
			Append (item);
			item.ShowAll ();
			item.Activated += HandleActivate;
		}

	}

	void HandleActivate (object obj, EventArgs args)
	{
		if (TagSelected != null) {
			TagMenu.TagMenuItem t = obj as TagMenu.TagMenuItem;
			if (t != null)
				TagSelected (t.Value);
			else
				Log.Debug ("Item was not a TagMenuItem");
		}
	}
}
