//
// ViewModeCondition.cs
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
	public enum ViewMode
	{
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
		static event ViewModeChangedHandler ViewModeChanged;

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
				foreach (string mode in val.Split (',')) {
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
