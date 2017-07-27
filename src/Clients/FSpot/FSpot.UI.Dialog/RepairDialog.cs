//
// RepairDialog.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2006 Larry Ewing
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

using Gtk;

using FSpot.Widgets;
using FSpot.Core;

namespace FSpot. UI.Dialog
{
	public class RepairDialog : BuilderDialog
	{
		[GtkBeans.Builder.Object] ScrolledWindow view_scrolled;

		readonly IBrowsableCollection source;
		readonly PhotoList missing;

		public RepairDialog (IBrowsableCollection collection) : base ("RepairDialog.ui", "repair_dialog")
		{
			source = collection;
			missing = new PhotoList ();

			FindMissing ();
			TrayView view = new TrayView (missing);
			view_scrolled.Add (view);

			ShowAll ();
		}

		public void FindMissing ()
		{
			int i;
			missing.Clear ();

			for (i = 0; i < source.Count; i++) {
				IPhoto item = source [i];
				string path = item.DefaultVersion.Uri.LocalPath;
				if (! File.Exists (path) || (new FileInfo (path).Length == 0))
					missing.Add (item);
			}
		}
	}
}
