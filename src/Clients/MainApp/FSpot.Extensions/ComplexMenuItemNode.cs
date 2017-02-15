//
// ComplexMenuItemNode.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2008 Stephane Delcroix
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

using System;

using Mono.Addins;

using Hyena.Widgets;

namespace FSpot.Extensions
{
	[ExtensionNode ("ComplexMenuItem")]
	public class ComplexMenuItemNode : MenuNode
	{
		[NodeAttribute]
		protected string widget_type;

		[NodeAttribute]
		protected string command_type;

		ICommand cmd;

		public override Gtk.MenuItem GetMenuItem (object parent)
		{
			ComplexMenuItem item = Activator.CreateInstance (Type.GetType (widget_type), parent) as ComplexMenuItem;
			cmd = (ICommand) Addin.CreateInstance (command_type);

            if (item != null)
                item.Activated += OnActivated;
            return item;
        }

        void OnActivated (object o, EventArgs e)
        {
            if (cmd != null)
                cmd.Run (o, e);
        }
	}
}
