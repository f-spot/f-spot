//
// ListView_Model.cs
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
using System.Reflection;

using Gtk;

namespace Hyena.Data.Gui
{
    public partial class ListView<T> : ListViewBase
    {
        #pragma warning disable 0067
        public event EventHandler ModelChanged;
        public event EventHandler ModelReloaded;
        #pragma warning restore 0067

        public void SetModel (IListModel<T> model)
        {
            SetModel (model, 0.0);
        }

        public virtual void SetModel (IListModel<T> value, double vpos)
        {
            if (model == value) {
                return;
            }

            if (model != null) {
                model.Cleared -= OnModelClearedHandler;
                model.Reloaded -= OnModelReloadedHandler;
            }

            model = value;

            if (model != null) {
                model.Cleared += OnModelClearedHandler;
                model.Reloaded += OnModelReloadedHandler;
                selection_proxy.Selection = model.Selection;
                IsEverReorderable = model.CanReorder;
            }

            if (ViewLayout != null) {
                ViewLayout.Model = Model;
            }

            ISortable sortable = model as ISortable;
            if (sortable != null && ColumnController != null) {
                ISortableColumn sort_column = ColumnController.SortColumn ?? ColumnController.DefaultSortColumn;
                if (sort_column != null) {
                    if (sortable.Sort (sort_column)) {
                        model.Reload ();
                    }
                    RecalculateColumnSizes ();
                    RegenerateColumnCache ();
                    InvalidateHeader ();
                    IsReorderable = sortable.SortColumn == null || sortable.SortColumn.SortType == SortType.None;
                }
            }

            RefreshViewForModel (vpos);

            var handler = ModelChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void RefreshViewForModel (double? vpos)
        {
            if (Model == null) {
                UpdateAdjustments ();
                QueueDraw ();
                return;
            }

            if (ViewLayout != null) {
                ViewLayout.ModelUpdated ();
            }

            UpdateAdjustments ();

            if (vpos != null) {
                ScrollToY ((double) vpos);
            } else if (Model.Count <= ItemsInView) {
                // If our view fits all rows at once, make sure we're scrolled to the top
                ScrollToY (0.0);
            } else if (vadjustment != null) {
                ScrollToY (vadjustment.Value);
            }

            if (Parent is ScrolledWindow) {
                Parent.QueueDraw ();
            }
        }

        private void OnModelClearedHandler (object o, EventArgs args)
        {
            OnModelCleared ();
        }

        private void OnModelReloadedHandler (object o, EventArgs args)
        {
            OnModelReloaded ();

            var handler = ModelReloaded;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void OnColumnControllerUpdatedHandler (object o, EventArgs args)
        {
            OnColumnControllerUpdated ();
        }

        protected virtual void OnModelCleared ()
        {
            RefreshViewForModel (null);
        }

        protected virtual void OnModelReloaded ()
        {
            RefreshViewForModel (null);
        }

        private IListModel<T> model;
        public virtual IListModel<T> Model {
            get { return model; }
        }

        private string row_opaque_property_name = "Sensitive";
        private PropertyInfo row_opaque_property_info;
        bool row_opaque_property_invalid = false;

        public string RowOpaquePropertyName {
            get { return row_opaque_property_name; }
            set {
                if (value == row_opaque_property_name) {
                    return;
                }

                row_opaque_property_name = value;
                row_opaque_property_info = null;
                row_opaque_property_invalid = false;

                InvalidateList ();
            }
        }

        private bool IsRowOpaque (object item)
        {
            if (item == null || row_opaque_property_invalid) {
                return true;
            }

            if (row_opaque_property_info == null || row_opaque_property_info.ReflectedType != item.GetType ()) {
                row_opaque_property_info = item.GetType ().GetProperty (row_opaque_property_name);
                if (row_opaque_property_info == null || row_opaque_property_info.PropertyType != typeof (bool)) {
                    row_opaque_property_info = null;
                    row_opaque_property_invalid = true;
                    return true;
                }
            }

            return (bool)row_opaque_property_info.GetValue (item, null);
        }

        private string row_bold_property_name = "IsBold";
        private PropertyInfo row_bold_property_info;
        bool row_bold_property_invalid = false;

        public string RowBoldPropertyName {
            get { return row_bold_property_name; }
            set {
                if (value == row_bold_property_name) {
                    return;
                }

                row_bold_property_name = value;
                row_bold_property_info = null;
                row_bold_property_invalid = false;

                InvalidateList ();
            }
        }

        private bool IsRowBold (object item)
        {
            if (item == null || row_bold_property_invalid) {
                return false;
            }

            if (row_bold_property_info == null || row_bold_property_info.ReflectedType != item.GetType ()) {
                row_bold_property_info = item.GetType ().GetProperty (row_bold_property_name);
                if (row_bold_property_info == null || row_bold_property_info.PropertyType != typeof (bool)) {
                    row_bold_property_info = null;
                    row_bold_property_invalid = true;
                    return false;
                }
            }

            return (bool)row_bold_property_info.GetValue (item, null);
        }
    }
}
