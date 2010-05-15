using FSpot;
using System.Collections.Generic;

public abstract class ImportBackend {
	// Prepare for importing; returns the items that should be imported
	// If it returns an empty list, you should not call Step(), Cancel() or Finish() until you call Prepare() again.
	public abstract List<IBrowsableItem> Prepare ();

	// Import one picture.  Returns false when done; then you have to call Finish().
	public abstract bool Step (out StepStatusInfo import_info);

	// Cancel importing.
	public abstract void Cancel ();

	// Complete importing (needs to be called).
	public abstract void Finish ();

	// The import roll. Should be set at Prepare () and removed at Cancel ()
	protected Roll roll;
}
