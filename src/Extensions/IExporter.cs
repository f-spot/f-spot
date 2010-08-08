/*
 * IExporter.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using Mono.Addins;
using System;

using FSpot.Core;

namespace FSpot.Extensions
{
	public interface IExporter
	{
		void Run (IBrowsableCollection selection);
	}
}
