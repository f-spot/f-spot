/*
 * FSpot.Extensions.ViewModeCondition.cs
 *
 * Author(s)
 * 	Ruben Vermeersch  <ruben@savanne.be>
 *
 * This is free software. See COPYING for details.
 *
 */

using Mono.Addins;

namespace FSpot.Extensions
{
	public enum ViewMode {
		Unknown,
		Single,
		Library
	}

	// Defines a view mode condition, which determines which view mode is used.
	//
	// There are two valid values for the "mode" attribute, which 
	// should be added to the Condition tag.
	//   - single: Single view mode.
	//   - library: Full F-Spot mode.
	//
	// This class contains a very nasty hack using a static initialization method
	// to keep track of the current view mode. This is (unfortunately) needed
	// because there is no way to get hold of a reference to the current window.
	public class ViewModeCondition : ConditionType
	{
		private static event ViewModeChangedHandler ViewModeChanged;
		private delegate void ViewModeChangedHandler ();

		private static ViewMode mode = ViewMode.Unknown;
		public static ViewMode Mode {
			get { return mode; }
			set {
				mode = value;

				if (ViewModeChanged != null)
					ViewModeChanged ();
			}
		}

		public ViewModeCondition ()
		{
			ViewModeChanged += delegate { NotifyChanged (); };
		}

		public override bool Evaluate (NodeElement conditionNode)
		{
			string val = conditionNode.GetAttribute ("mode");
			if (val.Length > 0) {
				foreach (string mode in val.Split(',')) {
					if (mode == "single" && Mode == ViewMode.Single) {
						return true;
					} else if (mode == "library" && Mode == ViewMode.Library) {
						return true;
					}
				}
			}
			return false;
		}
	}
}
