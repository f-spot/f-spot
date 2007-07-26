/*
 * FSpot.Extensions.ICommand.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;

namespace FSpot.Extensions
{
	public interface ICommand
	{
		void Run (object o, EventArgs e);
	}
}
