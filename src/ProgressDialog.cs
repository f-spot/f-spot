using GLib;
using Gtk;
using GtkSharp;
using System;

public class ProgressDialog : Dialog {

	// We show the dialog after a certain number of milliseconds after the first update.
	// This way if an operation is quick enough we don't bother the user with a useless dialog.

	const int SHOW_TIMEOUT_MSEC = 1000;

	private uint show_dialog_timeout_id;

	private bool HandleShowDialogTimeout ()
	{
		ShowAll ();
		show_dialog_timeout_id = 0;

		return false;
	}

	private void HandleDestroyEvent (object me, DestroyEventArgs args)
	{
		if (show_dialog_timeout_id != 0) {
			Source.Remove (show_dialog_timeout_id);
			show_dialog_timeout_id = 0;
		}
	}


	private bool cancelled;

	private void HandleResponse (object me, ResponseArgs args)
	{
		cancelled = true;
	}


	public enum CancelButtonType {
		Cancel,
		Stop
	};

	private CancelButtonType cancel_button_type;
	private int total_count;

	private ProgressBar progress_bar;
	private Label message_label;

	private DateTime start_time;

	public ProgressDialog (string title, CancelButtonType cancel_button_type, int total_count, Gtk.Window parent_window)
	{
		Title = title;
		this.cancel_button_type = cancel_button_type;
		this.total_count = total_count;
		this.TransientFor = parent_window;

		HasSeparator = false;
		BorderWidth = 6;
		SetDefaultSize (300, -1);

		message_label = new Label ("");
		VBox.PackStart (message_label, true, true, 12);

		progress_bar = new ProgressBar ();
		VBox.PackStart (progress_bar, true, true, 6);

		DestroyEvent += new DestroyEventHandler (HandleDestroyEvent);

		switch (cancel_button_type) {
		case CancelButtonType.Cancel:
			AddButton ("Cancel", (int) ResponseType.Cancel);
			break;
		case CancelButtonType.Stop:
			AddButton ("Stop", (int) ResponseType.Cancel);
			break;
		}

		Response += new ResponseHandler (HandleResponse);
	}

	private int current_count;

	// Return true if the operation was cancelled by the user.
	public bool Update (string message)
	{
#if USE_TIMEOUT			// FIXME something is borked, maybe GTK# bug?
		if (current_count == 0)
			show_dialog_timeout_id = GLib.Timeout.Add ((uint) SHOW_TIMEOUT_MSEC,
								   new GLib.TimeoutHandler (HandleShowDialogTimeout));
#endif

		current_count ++;

		message_label.Text = message;
		progress_bar.Text = String.Format ("{0} of {1}", current_count, total_count);
		progress_bar.Fraction = (double) current_count / total_count;

		ShowAll ();

		while (Application.EventsPending ())
			Application.RunIteration ();

		return cancelled;
	}
}
