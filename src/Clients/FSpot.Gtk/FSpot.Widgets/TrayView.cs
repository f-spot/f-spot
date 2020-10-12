//
// TrayView.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2007 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

namespace FSpot.Widgets
{
    /// <summary>
    ///    This class implements a simply tray widget which which shows a collection of photos
    ///    and does not react to user interaction.
    /// </summary>
    public class TrayView : CollectionGridView
	{
		public TrayView (System.IntPtr raw) : base (raw) {}

        public TrayView (IBrowsableCollection collection) : base (collection)
        {
            MaxColumns = 1;
        }
    }
}
