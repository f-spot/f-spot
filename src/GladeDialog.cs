namespace FSpot {
	public class GladeDialog {
		protected string dialog_name;
		protected Glade.XML xml;
		private Gtk.Dialog dialog;
		
		protected GladeDialog ()
		{

		}

		public GladeDialog (string name)
		{
			CreateDialog (name);
		}

		protected void CreateDialog (string name)
		{
			this.dialog_name = name;		
			xml = new Glade.XML (null, "f-spot.glade", name, "f-spot");
			xml.Autoconnect (this);
		}

		public Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = (Gtk.Dialog) xml.GetWidget (dialog_name);
				
				return dialog;
			}
		}
	}
}
