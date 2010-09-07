/*
 * TrayView.cs
 *
 * Author(s):
 *  Larry Ewing <lewing@novell.com>
 *  Mike Gemuende <mike@gemuende.de>
 *
 * Copyright (C) 2004 Novell, Inc.
 * Copyright (C) 2010 Mike Gemuende
 *
 * This is free software. See COPYING for details.
 */

using Gdk;
using Gtk;

using FSpot.Core;


namespace FSpot.Widgets
{
    /// <summary>
    ///    This class implements a simply tray widget which which shows a collection of photos
    ///    and does not react to user interaction.
    /// </summary>
    public class TrayView : CollectionGridView {

#region Constructors

        public TrayView (System.IntPtr raw) : base (raw) {}

        public TrayView (IBrowsableCollection collection) : base (collection)
        {
            MaxColumns = 1;
        }

#endregion

    }
}
