using Gtk;
using System.Collections.Generic;

namespace FSpot.Widgets
{
    public partial class ImageView : Container
    {
        List<LayoutChild> children = new List<LayoutChild> ();

        #region container

        protected override void OnAdded (Gtk.Widget widget)
        {
            Put (widget, 0, 0);
        }

        protected override void OnRemoved (Gtk.Widget widget)
        {
            LayoutChild child = null;
            foreach (var c in children) {
                if (child.Widget == widget) {
                    child = c;
                    break;
                }
            }
            
            if (child != null) {
                widget.Unparent ();
                children.Remove (child);
            }
        }

        protected override void ForAll (bool include_internals, Gtk.Callback callback)
        {
            foreach (var child in children) {
                callback (child.Widget);
            }
        }

        #endregion


        #region children

        class LayoutChild
        {
            Gtk.Widget widget;
            public Gtk.Widget Widget {
                get { return widget; }
            }

            public int X { get; set; }
            public int Y { get; set; }

            public LayoutChild (Gtk.Widget widget, int x, int y)
            {
                this.widget = widget;
                X = x;
                Y = y;
            }
        }

        LayoutChild GetChild (Gtk.Widget widget)
        {
            foreach (var child in children) {
                if (child.Widget == widget)
                    return child;
            }
            return null;
        }

        #endregion

        #region Public API

        public void Put (Gtk.Widget widget, int x, int y)
        {
            children.Add (new LayoutChild (widget, x, y));
            if (IsRealized)
                widget.ParentWindow = GdkWindow;
            widget.Parent = this;
        }

        public void Move (Gtk.Widget widget, int x, int y)
        {
            LayoutChild child = GetChild (widget);
            if (child == null)
                return;
            
            child.X = x;
            child.Y = y;
            if (Visible && widget.Visible)
                QueueResize ();
        }

        private void OnRealizedChildren ()
        {
            foreach (var child in children) {
                child.Widget.ParentWindow = GdkWindow;
            }
        }

        private void OnMappedChildren ()
        {
            foreach (var child in children) {
                if (child.Widget.Visible && !child.Widget.IsMapped)
                    child.Widget.Map ();
            }
        }

        private void OnSizeRequestedChildren ()
        {
            foreach (var child in children) {
                child.Widget.SizeRequest ();
            }
        }

        private void OnSizeAllocatedChildren ()
        {
            foreach (var child in children) {
                Gtk.Requisition req = child.Widget.ChildRequisition;
                child.Widget.SizeAllocate (new Gdk.Rectangle (child.X, child.Y, req.Width, req.Height));
            }
        }
        
        #endregion
    }
}

