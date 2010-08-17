using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;
using Gtk;
using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using Hyena;
using FSpot.UI.Dialog;
using Gnome.Keyring;
using SmugMugNet;

namespace FSpot.Exporters.SmugMug
{
	public class SmugMugAddAlbum {
		[GtkBeans.Builder.Object] Gtk.Dialog dialog;
		[GtkBeans.Builder.Object] Gtk.Entry title_entry;
		[GtkBeans.Builder.Object] Gtk.CheckButton public_check;
		[GtkBeans.Builder.Object] Gtk.ComboBox category_combo;

		[GtkBeans.Builder.Object] Gtk.Button add_button;

		private string dialog_name = "smugmug_add_album_dialog";
		private GtkBeans.Builder builder;
		private SmugMugExport export;
		private SmugMugApi smugmug;
		private string title;
		private ListStore category_store;

		public SmugMugAddAlbum (SmugMugExport export, SmugMugApi smugmug)
		{
			builder = new GtkBeans.Builder (null, "smugmug_add_album_dialog.ui", null);
			builder.Autoconnect (this);

			this.export = export;
			this.smugmug = smugmug;

			this.category_store = new ListStore (typeof(int), typeof(string));
			CellRendererText display_cell = new CellRendererText();
			category_combo.PackStart (display_cell, true);
			category_combo.SetCellDataFunc (display_cell, new CellLayoutDataFunc (CategoryDataFunc));
			this.category_combo.Model = category_store;
			PopulateCategoryCombo ();

			Dialog.Response += HandleAddResponse;

			title_entry.Changed += HandleChanged;
			HandleChanged (null, null);
		}

		private void HandleChanged (object sender, EventArgs args)
		{
			title = title_entry.Text;

			if (title == String.Empty)
				add_button.Sensitive = false;
			else
				add_button.Sensitive = true;
		}

		[GLib.ConnectBefore]
		protected void HandleAddResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				smugmug.CreateAlbum (title, CurrentCategoryId, public_check.Active);
				export.HandleAlbumAdded (title);
			}
			Dialog.Destroy ();
		}

		void CategoryDataFunc (CellLayout layout, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			string name = (string)model.GetValue (iter, 1);
			(renderer as CellRendererText).Text = name;
		}

		protected void PopulateCategoryCombo ()
		{
			SmugMugNet.Category[] categories = smugmug.GetCategories ();

			foreach (SmugMugNet.Category category in categories) {
				category_store.AppendValues (category.CategoryID, category.Title);
			}

			category_combo.Active = 0;

			category_combo.ShowAll ();
		}

		protected int CurrentCategoryId
		{
			get {
				TreeIter current;
				category_combo.GetActiveIter (out current);
				return (int)category_combo.Model.GetValue (current, 0);
			}
		}

		private Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = new Gtk.Dialog (builder.GetRawObject (dialog_name));

				return dialog;
			}
		}
	}
}
