/*
 * FSpot.Widgets.DateEdit.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * Copyright (c) 2008 Novell, Inc.
 *
 * This is free software. See COPYING for details.
 */

using System;
using Gtk;

namespace FSpot.Widgets
{
	public class DateEdit : Bin
	{
		public event EventHandler DateChanged;
		public event EventHandler TimeChanged;

		public bool ShowTime {get; set;}
		public bool ShowOffset {get; set;}
		public DateTimeOffset DateTimeOffset {get; set;}

		ComboBox combo;

		public DateEdit () : DateEdit (DateTime.Now)
		{
		}

		public DateEdit (DateTime datetime) : DateEdit (new DateTimeOffset (datetime))
		{
		}

		public DateEdit (DateTimeOffset datetimeoffset)
		{
			DateTimeOffset = datetimeoffset;
			combo = new ComboBox ();
			Add (combo);
		}
	}
}
