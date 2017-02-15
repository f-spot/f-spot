//
// FindBar.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007 Gabriel Burt
// Copyright (C) 2010 Daniel Köb
// Copyright (C) 2007-2008 Stephane Delcroix
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


using System.Text;

using Gtk;

namespace FSpot.Widgets
{
	public class LogicEntryCompletion : EntryCompletion
	{
		readonly Entry entry;

		public bool Completing { get; private set; }

		public LogicEntryCompletion (Entry entry, TreeModel tree_model)
		{
			this.entry = entry;

			Model = new DependentListStore (tree_model);

			InlineCompletion = false;
			MinimumKeyLength = 1;
			TextColumn = 1;
			PopupSetWidth = false;
			MatchFunc = LogicEntryCompletionMatchFunc;
			MatchSelected += HandleMatchSelected;

			// Insert these when appropriate..
			//InsertActionText (0, "or");
			//InsertActionText (1, "and");
			// HandleAction...
		}

		[GLib.ConnectBefore]
		void HandleMatchSelected (object sender, MatchSelectedArgs args)
		{
			string name = args.Model.GetValue (args.Iter, TextColumn) as string;
			//Log.DebugFormat ("match selected..{0}", name);

			int pos = entry.Position;
			string updated_text = completion_logic.ReplaceKey (entry.Text, name, ref pos);

			Completing = true;
			entry.Text = updated_text;
			entry.Position = pos;
			Completing = false;

			args.RetVal = true;
			//Log.Debug ("done w/ match selected");
		}

		readonly CompletionLogic completion_logic = new CompletionLogic ();
		public bool LogicEntryCompletionMatchFunc (EntryCompletion completion, string key, TreeIter iter)
		{
			if (Completing)
				return false;

			key = key == null ? null : key.Normalize (NormalizationForm.FormC);
			string name = completion.Model.GetValue (iter, completion.TextColumn) as string;
			int pos = entry.Position - 1;
			return completion_logic.MatchFunc (name, key, pos);
		}
	}
}
