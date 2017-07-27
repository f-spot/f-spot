//
// ViewModeCondition.cs
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
		delegate void ViewModeChangedHandler ();
		private static event ViewModeChangedHandler ViewModeChanged;

		static ViewMode mode = ViewMode.Unknown;
		public static ViewMode Mode {
			get { return mode; }
			set {
				mode = value;
				ViewModeChanged?.Invoke ();
			}
		}

		public ViewModeCondition ()
		{
			ViewModeChanged += NotifyChanged;
		}

		public override bool Evaluate (NodeElement conditionNode)
		{
			string val = conditionNode.GetAttribute ("mode");
			if (val.Length > 0) {
				foreach (string mode in val.Split(',')) {
					if (mode == "single" && Mode == ViewMode.Single) {
						return true;
					}
					if (mode == "library" && Mode == ViewMode.Library) {
						return true;
					}
				}
			}
			return false;
		}
	}
}
