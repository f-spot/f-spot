/*
 * Widgets.MetadataDisplay.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 * 	Ruben Vermeersch <ruben@savanne.be>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Gtk;

using Mono.Unix;

using FSpot.Core;
using FSpot.Extensions;
using FSpot.Imaging;

namespace FSpot.Widgets {
	public class MetadataDisplayPage : SidebarPage {
		public MetadataDisplayPage() : base(new MetadataDisplayWidget(),
											Catalog.GetString ("Metadata"),
											"gtk-info") {
			(SidebarWidget as MetadataDisplayWidget).Page = this;
		}

		protected override void AddedToSidebar ()
		{
			MetadataDisplayWidget widget = SidebarWidget as MetadataDisplayWidget;
			(Sidebar as Sidebar).SelectionChanged += widget.HandleSelectionChanged;
			(Sidebar as Sidebar).SelectionItemsChanged += widget.HandleSelectionItemsChanged;
		}
	}

	public class MetadataDisplayWidget : ScrolledWindow {
		Delay update_delay;

		/* 	This VBox only contains exif-data,
			so it is seperated from other information */
		VBox metadata_vbox;

		VBox main_vbox;
		Label metadata_message;
		State display;

		private MetadataDisplayPage page;
		public MetadataDisplayPage Page {
			set { page = value; }
			get { return page; }
		}

		// stores list of the expanded expanders
		List<string> open_list;

		ListStore extended_metadata;

		bool up_to_date = false;

		enum State {
			metadata,
			message
		};

		public MetadataDisplayWidget ()
		{
			main_vbox = new VBox ();
			main_vbox.Spacing = 6;

			metadata_message = new Label (String.Empty);
			metadata_message.UseMarkup = true;
			metadata_message.LineWrap = true;
			metadata_vbox = new VBox ();
			metadata_vbox.Spacing = 6;

			main_vbox.PackStart (metadata_vbox, false, false, 0);
			AddWithViewport (metadata_message);
			((Viewport) Child).ShadowType = ShadowType.None;
			BorderWidth = 3;

			display = State.message;
			ExposeEvent += HandleExposeEvent;

			open_list = new List<string> ();

			// Create Expander and TreeView for
			// extended metadata
			TreeView tree_view = new TreeView ();
			tree_view.HeadersVisible = false;
			tree_view.RulesHint = true;

			TreeViewColumn col = new TreeViewColumn ();
			col.Sizing = TreeViewColumnSizing.Autosize;
			CellRenderer colr = new CellRendererText ();
			col.PackStart (colr, false);

			col.AddAttribute (colr, "markup", 0);

			tree_view.AppendColumn (col);

			extended_metadata = new ListStore (typeof(string));
			tree_view.Model = extended_metadata;

			Expander expander = new Expander (String.Format("<span weight=\"bold\"><small>{0}</small></span>", Catalog.GetString ("Extended Metadata")));
			expander.UseMarkup = true;
			expander.Add (tree_view);
			expander.Expanded = true;

			main_vbox.PackStart (expander, false, false, 6);
			expander.ShowAll ();

			update_delay = new Delay (Update);
			update_delay.Start ();
		}

		private IBrowsableItem photo;
		public IBrowsableItem Photo {
			get { return photo; }
			set {
				photo = value;

				if (!Visible) {
					up_to_date = false;
				} else {
					update_delay.Start ();
				}
			}
		}

		private void HandleExposeEvent (object sender, ExposeEventArgs args)
		{
			if (!up_to_date) {
				update_delay.Start ();
			}
		}

		internal void HandleSelectionChanged (IBrowsableCollection collection) {
            // Don't show metadata when multiple photos are selected.
			Photo = (collection != null && collection.Count == 1) ? collection [0] : null;
		}

		internal void HandleSelectionItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args) {
			if (!args.Changes.MetadataChanged)
				return;

			if (!Visible) {
				up_to_date = false;
            } else {
				update_delay.Start ();
            }
		}

        private bool Visible {
            get {
                return (Page.Sidebar as Sidebar).IsActive (Page);
            }
        }

		private ListStore AddExpander (string name, int pos)
		{
			TreeView tree_view = new TreeView ();
			tree_view.HeadersVisible = false;
			tree_view.RulesHint = true;

			TreeViewColumn col = new TreeViewColumn ();
			col.Sizing = TreeViewColumnSizing.Autosize;
			CellRenderer colr = new CellRendererText ();
			col.PackStart (colr, false);

			col.AddAttribute (colr, "markup", 0);

			tree_view.AppendColumn (col);

			ListStore model = new ListStore (typeof(string));
			tree_view.Model = model;

			Expander expander = new Expander (String.Format ("<span weight=\"bold\"><small>{0}</small></span>", name));
			expander.UseMarkup = true;
			expander.Add (tree_view);
			expander.Expanded = true;

			metadata_vbox.PackStart (expander, false, false, 6);
			metadata_vbox.ReorderChild (expander, pos);

			if (open_list.Contains (name))
				expander.Expanded = true;

			expander.Activated += HandleExpanderActivated;

			expander.ShowAll ();

			return model;
		}

		public void HandleExpanderActivated (object sender, EventArgs e)
		{
			Expander expander = (Expander) sender;
			if (expander.Expanded)
				open_list.Add (expander.Label);
			else
				open_list.Remove (expander.Label);
		}

		private bool Update ()
		{
			bool empty = true;
			int index_of_expander = 0;
			bool missing = false;
			System.Exception error = null;

			/*
            // FIXME: The stuff below needs to be ported to Taglib#.
            TreeIter iter;
			ListStore model;
			string name;

			up_to_date = true;

			int i = 0;

			// Write Exif-Data
			if (exif_info != null) {
				foreach (ExifContent content in exif_info.GetContents ()) {
					ExifEntry [] entries = content.GetEntries ();

					i++;

					if (entries.Length < 1)
						continue;

					empty = false;

					name = ExifUtil.GetIfdNameExtended ((Ifd)i - 1);

					if (index_of_expander >= metadata_vbox.Children.Length)
						model = AddExpander (name, index_of_expander);
					else {
						Expander expander = (Expander)metadata_vbox.Children[index_of_expander];
						if (expander.Label == name)
							model = (ListStore)((TreeView)expander.Child).Model;
						else {
							model = AddExpander (name, index_of_expander);
						}
					}

					model.GetIterFirst(out iter);

					foreach (ExifEntry entry in entries) {
						string s;

						if (entry.Title != null)
							s = String.Format ("{0}\n\t<small>{1}</small>", entry.Title, entry.Value);
						else
							s = String.Format ("Unknown Tag ID={0}\n\t<small>{1}</small>", entry.Tag.ToString (), entry.Value);

						if (model.IterIsValid(iter)) {
							model.SetValue (iter, 0, s);
							model.IterNext(ref iter);
						} else
							model.AppendValues (s);
					}

					// remove rows, that are not used
					while (model.IterIsValid(iter)) {
						model.Remove (ref iter);
					}

					index_of_expander++;
				}
			}


			// Write Extended Metadata
			if (photo != null) {
				MetadataStore store = new MetadataStore ();
				try {
					using (var img = ImageFile.Create (photo.DefaultVersion.Uri)) {
						if (img is SemWeb.StatementSource) {
							StatementSource source = (StatementSource)img;
							source.Select (store);
						}
					}
				} catch (System.IO.FileNotFoundException) {
					missing = true;
				} catch (System.Exception e){
					// Sometimes we don't get the right exception, check for the file
					if (!System.IO.File.Exists (photo.DefaultVersion.Uri.LocalPath)) {
						missing = true;
					} else {
						// if the file is there but we still got an exception display it.
						error = e;
					}
				}

				model = extended_metadata;
				model.GetIterFirst(out iter);

				if (store.StatementCount > 0) {
					empty = false;


					foreach (Statement stmt in store) {
						// Skip anonymous subjects because they are
						// probably part of a collection
						if (stmt.Subject.Uri == null && store.SelectSubjects (null, stmt.Subject).Length > 0)
							continue;

						string title;
						string value;
						string s;

						Description.GetDescription (store, stmt, out title, out value);

						if (value == null)
						{
							MemoryStore substore = store.Select (new Statement ((Entity)stmt.Object, null, null, null)).Load();
							StringBuilder collection = new StringBuilder ();
							collection.Append (title);
							WriteCollection (substore, collection);
							if (model.IterIsValid(iter))
							{
								model.SetValue (iter, 0, collection.ToString ());
								model.IterNext(ref iter);
							} else
								model.AppendValues (collection.ToString ());
						} else {
							s = String.Format ("{0}\n\t<small>{1}</small>", title, value);
							if (model.IterIsValid(iter))
							{
								model.SetValue (iter, 0, s);
								model.IterNext(ref iter);
							} else
								model.AppendValues (s);
						}
					}

				} else {
					// clear Extended Metadata
					String s = String.Format ("<small>{0}</small>", Catalog.GetString ("No Extended Metadata Available"));
					if (model.IterIsValid(iter))
					{
						model.SetValue (iter, 0, s);
						model.IterNext(ref iter);
					} else
						model.AppendValues (s);
				}

				// remove rows, that are not used
				while (model.IterIsValid(iter)) {
					model.Remove (ref iter);
				}
			}*/

			if (empty) {
				string msg;
				if (photo == null) {
				     msg = Catalog.GetString ("No active photo");
				} else if (missing) {
					msg = String.Format (Catalog.GetString ("The photo \"{0}\" does not exist"), photo.DefaultVersion.Uri);
				} else {
				     msg = Catalog.GetString ("No metadata available");

					if (error != null) {
						msg = String.Format ("<i>{0}</i>", error);
					}
				}

				metadata_message.Markup = "<span weight=\"bold\">" + msg + "</span>";

				if (display == State.metadata) {
					// Child is a Viewport, (AddWithViewport in ctor)
					((Viewport)Child).Remove (main_vbox);
					((Viewport)Child).Add (metadata_message);
					display = State.message;
					metadata_message.Show ();
				}
			} else {
				// remove Expanders, that are not used
				while (index_of_expander < metadata_vbox.Children.Length)
					metadata_vbox.Remove (metadata_vbox.Children[index_of_expander]);

				if (display == State.message) {
					// Child is a Viewport, (AddWithViewport in ctor)
					((Viewport)Child).Remove (metadata_message);
					((Viewport)Child).Add (main_vbox);
					display = State.metadata;
					main_vbox.ShowAll ();
				}
			}

			return false;
		}

        /*
		private void WriteCollection (MemoryStore substore, StringBuilder collection)
		{
			string type = null;

			foreach (Statement stmt in substore) {
				if (stmt.Predicate.Uri == MetadataStore.Namespaces.Resolve ("rdf:type")) {
					string prefix;
					MetadataStore.Namespaces.Normalize (stmt.Object.Uri, out prefix, out type);
				}
			}

			foreach (Statement sub in substore) {
				if (sub.Object is SemWeb.Literal) {
					string title;
					string value = ((SemWeb.Literal)sub.Object).Value;

					Description.GetDescription (substore, sub, out title, out value);

					if (type == null)
						collection.AppendFormat ("\n\t<small>{0}: {1}</small>", title, value);
					else
						collection.AppendFormat ("\n\t<small>{0}</small>", value);

				} else {
					if (type == null) {
						MemoryStore substore2 = substore.Select (new Statement ((Entity)sub.Object, null, null, null)).Load();
						if (substore.StatementCount > 0)
							WriteCollection (substore2, collection);
					}
				}
			}
		}*/
	}
}
