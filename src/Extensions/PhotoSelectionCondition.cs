/*
 * FSpot.Extensions.PhotoSelectionCondition.cs
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
	// Defines a selection condition, which determines the number of photos 
	// selected.
	//
	// There are two valid values for the "selection" attribute, which 
	// should be added to the Condition tag.
	//   - single: One photo is selected
	//   - multiple: Multiple photos are selected
	public class PhotoSelectionCondition : ConditionType
	{
		public PhotoSelectionCondition()
		{
			if (MainWindow.Toplevel != null)
				MainWindow.Toplevel.Selection.Changed += delegate { NotifyChanged ();};
		}

		public override bool Evaluate (NodeElement conditionNode)
		{
			int count = MainWindow.Toplevel.Selection.Count;
			string val = conditionNode.GetAttribute ("selection");
			if (val.Length > 0) {
				foreach (string selection in val.Split(',')) {
					if (selection == "multiple" && count > 1) {
						return true;
					} else if (selection == "single" && count == 1) {
						return true;
					}
				}
			}
			return false;
		}
	}
}
