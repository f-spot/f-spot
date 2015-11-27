//
// ListViewTestModule.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Collections.Generic;
using Gtk;

using Hyena.Data;
using Hyena.Collections;
using Hyena.Gui;
using Hyena.Gui.Canvas;
using Hyena.Data.Gui;

using Selection = Hyena.Collections.Selection;

namespace Hyena.Data.Gui.Tests
{
    [TestModule ("List View")]
    public class ListViewTestModule : Window
    {
        private View view;
        private Model model;

        public ListViewTestModule () : base ("ListView")
        {
            WindowPosition = WindowPosition.Center;
            SetDefaultSize (800, 600);

            ScrolledWindow scroll = new ScrolledWindow ();
            scroll.HscrollbarPolicy = PolicyType.Automatic;
            scroll.VscrollbarPolicy = PolicyType.Automatic;

            view = new View ();
            model = new Model ();

            scroll.Add (view);
            Add (scroll);
            ShowAll ();

            view.SetModel (model);
        }

        private class View : ListView<ModelItem>
        {
            public View ()
            {
                ColumnController = new ColumnController ();
                ColumnController.AddRange (
                    new Column (String.Empty, new ColumnCellCheckBox ("F", true), 1),
                    new Column ("Apples", new ColumnCellText ("B", true), 1),
                    new Column ("Pears", new ColumnCellText ("C", true), 1),
                    new Column ("How Hot", new ColumnCellRating ("G", true), 1),
                    new Column ("Peaches", new ColumnCellText ("D", true), 1),
                    new Column ("Doodle", new ColumnCellDoodle ("E", true), 1),
                    new Column ("GUIDs!OMG", new ColumnCellText ("A", true), 1)
                );
            }
        }
    }

    [TestModule ("Grid View")]
    public class GridViewTestModule : Window
    {
        private View view;
        private Model model;

        public GridViewTestModule () : base ("GridView")
        {
            WindowPosition = WindowPosition.Center;
            SetDefaultSize (800, 600);

            view = new View ();
            model = new Model ();

            /*var hbox = new HBox () { Spacing = 6 };

            var add_margin_control = new System.Action<string, Func<double>, System.Action<double>> ((type, get, set) => {
                var spin = new SpinButton (0, 20, 1);
                spin.Value = get ();
                spin.ValueChanged += (o, a) => { set (spin.Value); };
                hbox.PackStart (new Label (type + " Margin:"), false, false, 0);
                hbox.PackStart (spin, false, false, 0);
            });

            add_margin_control ("", () => view.Box.Margin.Left, v => view.Box.Margin = new Thickness (v));*/

            var scroll = new ScrolledWindow () {
                HscrollbarPolicy = PolicyType.Automatic,
                VscrollbarPolicy = PolicyType.Automatic
            };
            scroll.Add (view);

            var vbox = new VBox () { Spacing = 12 };
            //vbox.PackStart (hbox, true, true, 0);
            vbox.PackStart (scroll, true, true, 0);

            Add (vbox);
            ShowAll ();

            view.SetModel (model);
        }

        private class View : ListView<ModelItem>
        {
            public View ()
            {
                ViewLayout = new DataViewLayoutGrid () {
                    ChildAllocator = () => {
                        return new StackPanel () {
                            Orientation = Hyena.Gui.Canvas.Orientation.Vertical,
                            Width = 400,
                            Spacing = 15,
                            //Margin = new Thickness (10),
                            Theme = Theme,
                            Children = {
                                new Slider (),
                                new ColumnCellCheckBox ("F", true),
                                new TextBlock () { Binder = new ObjectBinder () { Property = "A" } },
                                new TextBlock () { Binder = new ObjectBinder () { Property = "B" } },
                                //new ColumnCellText ("B", true),
                                //new ColumnCellText ("C", true),
                                new ColumnCellRating ("G", true),
                                //new ColumnCellText ("D", true),
                                new ColumnCellDoodle ("E", true),
                                //new ColumnCellText ("A", true)
                            }
                        };
                        //return new ColumnCellRating ("G", true);
                    },
                    View = this
                };
            }
        }
    }

    internal class Model : IListModel<ModelItem>
    {
        private List<ModelItem> store = new List<ModelItem> ();
        private Selection selection = new Selection ();

        public event EventHandler Cleared;
        public event EventHandler Reloaded;

        public Model ()
        {
            Random random = new Random (0);
            for (int i = 0; i < 1000; i++) {
                store.Add (new ModelItem (i, random));
            }
        }

        public void Clear ()
        {
        }

        public void Reload ()
        {
        }

        public object GetItem (int index)
        {
            return this[index];
        }

        public int Count {
            get { return store.Count; }
        }

        public bool CanReorder {
            get { return false; }
        }

        public ModelItem this[int index] {
            get { return store[index]; }
        }

        public Selection Selection {
            get { return selection; }
        }
    }

    internal class ModelItem
    {
        public ModelItem (int i, Random rand)
        {
            a = Guid.NewGuid ().ToString ();
            b = rand.Next (0, 255);
            c = rand.NextDouble ();
            d = String.Format ("Item {0}", i);
            e = new List<Gdk.Point> ();
            f = rand.Next (0, 1) == 1;
            g = rand.Next (0, 5);
        }

        string a; public string A { get { return a; } }
        int b;    public int    B { get { return b; } }
        double c; public double C { get { return c; } }
        string d; public string D { get { return d; } }
        List<Gdk.Point> e; public List<Gdk.Point> E { get { return e; } }
        bool f; public bool F { get { return f; } set { f = value; } }
        int g; public int G { get { return g; } set { g = value; } }
    }

    internal class ColumnCellDoodle : ColumnCell, IInteractiveCell
    {
        private Random random = new Random ();
        private bool red = false;

        public ColumnCellDoodle (string property, bool expand) : base (property, expand)
        {
        }

        public override void Render (CellContext context, double cellWidth, double cellHeight)
        {
            red = !red;
            Cairo.Context cr = context.Context;
            cr.Rectangle (0, 0, cellWidth, cellHeight);
            cr.Color = CairoExtensions.RgbaToColor (red ? 0xff000099 : 0x00000099);
            cr.Fill ();

            List<Gdk.Point> points = Points;
            for (int i = 0, n = points.Count; i < n; i++) {
                if (i == 0) {
                    cr.MoveTo (points[i].X, points[i].Y);
                } else {
                    cr.LineTo (points[i].X, points[i].Y);
                }
            }

            cr.Color = CairoExtensions.RgbToColor ((uint)random.Next (0xffffff));
            cr.LineWidth = 1;
            cr.Stroke ();
        }

        private object last_pressed_bound;

        public bool ButtonEvent (int x, int y, bool pressed, Gdk.EventButton evnt)
        {
            if (!pressed) {
                last_pressed_bound = null;
                return false;
            }

            last_pressed_bound = BoundObject;
            Points.Add (new Gdk.Point (x, y));
            return true;
        }

        public bool MotionEvent (int x, int y, Gdk.EventMotion evnt)
        {
            if (last_pressed_bound == BoundObject) {
                Points.Add (new Gdk.Point (x, y));
                return true;
            }

            return false;
        }

        public bool PointerLeaveEvent ()
        {
            last_pressed_bound = null;
            return true;
        }

        private List<Gdk.Point> Points {
            get { return (List<Gdk.Point>)BoundObject; }
        }
    }
}
