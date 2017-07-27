//
// FolderQueryWidget.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
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

using System.Text;
using System.Collections.Generic;
using System.Linq;

using Gtk;
using Hyena;

using FSpot.Query;

namespace FSpot
{
	public class FolderQueryWidget : HBox
	{
        readonly PhotoQuery query;
        FolderSet folder_set;

		public FolderQueryWidget (PhotoQuery query)
		{
			folder_set = new FolderSet ();
			this.query = query;

			query.SetCondition (folder_set);

			Drag.DestSet (this, DestDefaults.All,
			              folder_query_widget_source_table,
			              Gdk.DragAction.Copy | Gdk.DragAction.Move);
		}

		void UpdateGui ()
		{
			while (Children.Length != 0)
				Remove (Children[0]);

			int length = folder_set.Folders.Count ();

			if (length == 0) {
				Hide ();
				return;
			}

			if (length < 4) {

				foreach (var uri in folder_set.Folders) {
					var image = new Image ("gtk-directory", IconSize.Button);
					image.TooltipText = uri.ToString ();
					PackStart (image);
				}

				TooltipText = string.Empty;

			} else {

				var label = new Label (string.Format ("<i>{0}x</i>", length));
				label.UseMarkup = true;
				PackStart (label);

				var image = new Image ("gtk-directory", IconSize.Button);
				PackStart (image);

				var builder = new StringBuilder ();
				foreach (var uri in folder_set.Folders) {
					if (builder.Length > 0)
						builder.AppendLine ();

					builder.Append (uri.ToString ());
				}

				TooltipText = builder.ToString ();
			}

			ShowAll ();
		}

		public void SetFolders (IEnumerable<SafeUri> uris)
		{
			folder_set.Folders = uris;

			UpdateGui ();
		}

		public void Clear ()
		{
			folder_set.Folders = null;
		}

		public bool Empty {
			get { return folder_set.Folders == null || !folder_set.Folders.Any(); }
		}

		static TargetEntry [] folder_query_widget_source_table = {
				DragDropTargets.UriQueryEntry
		};

		protected override void OnDragDataReceived (Gdk.DragContext context, int x, int y, SelectionData selection_data, uint info, uint time_)
		{
			base.OnDragDataReceived (context, x, y, selection_data, info, time_);

			SetFolders (selection_data.GetUriListData ());
			query.RequestReload ();
		}
	}
}
