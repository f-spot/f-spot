//
// ListView.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Hyena.Gui.Canvas;

namespace Hyena.Data.Gui
{
    public partial class ListView<T> : ListViewBase, IListView<T>
    {
        private CanvasManager manager;
        
        protected ListView (IntPtr ptr) : base (ptr)
        {
        }

        public ListView ()
        {
            column_layout = new Pango.Layout (PangoContext);
            CanFocus = true;
            selection_proxy.Changed += delegate { InvalidateList (); };

            HasTooltip = true;
            QueryTooltip += OnQueryTooltip;
            DirectionChanged += (o, a) => SetDirection ();
            manager = new CanvasManager (this);
        }

        private void OnQueryTooltip (object o, Gtk.QueryTooltipArgs args)
        {
            if (!args.KeyboardTooltip) {
                if (ViewLayout != null) {
                    var pt = new Point (args.X - list_interaction_alloc.X, args.Y - list_interaction_alloc.Y);
                    var child = ViewLayout.FindChildAtPoint (pt);
                    if (child != null) {
                        string markup;
                        Rect area;
                        pt.Offset (ViewLayout.ActualAllocation.Point);
                        if (child.GetTooltipMarkupAt (pt, out markup, out area)) {
                            area.Offset (-ViewLayout.ActualAllocation.X, -ViewLayout.ActualAllocation.Y);
                            area.Offset (list_interaction_alloc.X, list_interaction_alloc.Y);
                            args.Tooltip.Markup = markup;
                            args.Tooltip.TipArea = (Gdk.Rectangle)area;
                            /*if (!area.Contains (args.X, args.Y)) {
                                Log.WarningFormat ("Tooltip rect {0} does not contain tooltip point {1},{2} -- this will cause excessive requerying", area, args.X, args.Y);
                            }*/
                            args.RetVal = true;
                        }
                    }
                } else if (cell_context != null && cell_context.Layout != null) {
                    ITooltipCell cell;
                    Column column;
                    int row_index;

                    if (GetEventCell<ITooltipCell> (args.X, args.Y, out cell, out column, out row_index)) {
                        CachedColumn cached_column = GetCachedColumnForColumn (column);

                        string markup = cell.GetTooltipMarkup (cell_context, cached_column.Width);
                        if (!String.IsNullOrEmpty (markup)) {
                            Gdk.Rectangle rect = new Gdk.Rectangle ();
                            rect.X = list_interaction_alloc.X + cached_column.X1;

                            // get the y of the event in list coords
                            rect.Y = args.Y - list_interaction_alloc.Y;

                            // get the top of the cell pointed to by list_y
                            rect.Y -= VadjustmentValue % ChildSize.Height;
                            rect.Y -= rect.Y % ChildSize.Height;

                            // convert back to widget coords
                            rect.Y += list_interaction_alloc.Y;

                            // TODO is this right even if the list is wide enough to scroll horizontally?
                            rect.Width = cached_column.Width;

                            // TODO not right - could be smaller if at the top/bottom and only partially showing
                            rect.Height = ChildSize.Height;

                            /*if (!rect.Contains (args.X, args.Y)) {
                                Log.WarningFormat ("ListView tooltip rect {0} does not contain tooltip point {1},{2} -- this will cause excessive requerying", rect, args.X, args.Y);
                            }*/

                            args.Tooltip.Markup = markup;
                            args.Tooltip.TipArea = rect;
                            args.RetVal = true;
                        }
                    }
                }
            }

            // Work around ref counting SIGSEGV, see http://bugzilla.gnome.org/show_bug.cgi?id=478519#c9
            if (args.Tooltip != null) {
                args.Tooltip.Dispose ();
            }
        }
    }
}
