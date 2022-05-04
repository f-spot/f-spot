//
// IUndoAction.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena
{
	public interface IUndoAction
	{
		void Undo ();
		void Redo ();
		void Merge (IUndoAction action);
		bool CanMerge (IUndoAction action);
	}
}
