//  AbstractLiteral.cs
//
//  Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2007-2009 Novell, Inc.
// Copyright (C) 2007 Gabriel Burt
// Copyright (C) 2007-2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// This has to do with Finding photos based on tags
// http://mail.gnome.org/archives/f-spot-list/2005-November/msg00053.html
// http://bugzilla-attachments.gnome.org/attachment.cgi?id=54566

namespace FSpot.Query
{
	public abstract class AbstractLiteral : Term
	{
	    protected AbstractLiteral (Term parent, Literal after) : base (parent, after)
		{
		}

		public override Term Invert (bool recurse)
		{
			isNegated = !isNegated;
			return this;
		}
	}
}
