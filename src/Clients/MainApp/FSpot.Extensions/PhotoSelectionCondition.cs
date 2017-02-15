//
// PhotoSelectionCondition.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
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
			App.Instance.Organizer.Selection.Changed += (collection) => { NotifyChanged (); };
		}

		public override bool Evaluate (NodeElement conditionNode)
		{
			int count = App.Instance.Organizer.Selection.Count;
			string val = conditionNode.GetAttribute ("selection");
			if (val.Length > 0) {
				foreach (string selection in val.Split(',')) {
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
