//
// TagSelectionWidgetTests.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2017 Stephen Shaw
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
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.IO;
using FSpot.Core;
using FSpot.Database;
//using Gnome;
using Hyena;
using NUnit.Framework;

namespace FSpot.Widgets.Tests
{
	[TestFixture]
	public class TagSelectionWidgetTests
	{
		#if false
			TagSelectionWidget selection_widget;

			void OnSelectionChanged ()
			{
				Log.Debug ("Selection changed:");

				foreach (Tag t in selection_widget.TagSelection)
					Log.DebugFormat ("\t{0}", t.Name);
			}

			Test ()
			{
				const string path = "/tmp/TagSelectionTest.db";

				try {
					File.Delete (path);
				} catch {}

				Db db = new Db (path, true);

				Category people_category = db.Tags.CreateCategory (null, "People");
				db.Tags.CreateTag (people_category, "Anna");
				db.Tags.CreateTag (people_category, "Ettore");
				db.Tags.CreateTag (people_category, "Miggy");
				db.Tags.CreateTag (people_category, "Nat");

				Category places_category = db.Tags.CreateCategory (null, "Places");
				db.Tags.CreateTag (places_category, "Milan");
				db.Tags.CreateTag (places_category, "Boston");

				Category exotic_category = db.Tags.CreateCategory (places_category, "Exotic");
				db.Tags.CreateTag (exotic_category, "Bengalore");
				db.Tags.CreateTag (exotic_category, "Manila");
				db.Tags.CreateTag (exotic_category, "Tokyo");

				selection_widget = new TagSelectionWidget (db.Tags);
				selection_widget.SelectionChanged += new SelectionChangedHandler (OnSelectionChanged);

				Window window = new Window (WindowType.Toplevel);
				window.SetDefaultSize (400, 200);
				ScrolledWindow scrolled = new ScrolledWindow (null, null);
				scrolled.SetPolicy (PolicyType.Automatic, PolicyType.Automatic);
				scrolled.Add (selection_widget);
				window.Add (scrolled);

				window.ShowAll ();
			}

			static void Main (string [] args)
			{
				Program program = new Program ("TagSelectionWidgetTest", "0.0", Modules.UI, args);

				Test test = new Test ();

				program.Run ();
			}
	#endif
	}
}
