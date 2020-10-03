//
// PhotoSelectionCondition.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
		public PhotoSelectionCondition ()
		{
			App.Instance.Organizer.Selection.Changed += (collection) => { NotifyChanged (); };
		}

		public override bool Evaluate (NodeElement conditionNode)
		{
			int count = App.Instance.Organizer.Selection.Count;
			string val = conditionNode.GetAttribute ("selection");
			if (val.Length > 0) {
				foreach (string selection in val.Split (',')) {
					if (selection == "multiple" && count > 1) {
						return true;
					}
					if (selection == "single" && count == 1) {
						return true;
					}
				}
			}

			return false;
		}
	}
}
