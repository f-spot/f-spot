//
// RatingMenuItem.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Mike Gemuende <mike@gemuende.de>
//
// Copyright (C) 2007 Novell, Inc.
// Copyright (C) 2010 Mike Gemuende
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Widgets
{
    public class RatingMenuItem : Hyena.Widgets.RatingMenuItem
    {
        RatingEntry entry;

        public RatingMenuItem () : base (new RatingEntry ())
        {
            entry = RatingEntry as RatingEntry;
        }

        public RatingMenuItem (object parent) : this ()
        {
            if (parent is FullScreenView) {
                entry.Value = (int)(parent as FullScreenView).View.Item.Current.Rating;

            } else if (App.Instance.Organizer.Selection.Count == 1) {
                entry.Value = (int)App.Instance.Organizer.Selection[0].Rating;
            }
        }

        protected RatingMenuItem (IntPtr raw) : base (raw)
        {
        }
    }
}
