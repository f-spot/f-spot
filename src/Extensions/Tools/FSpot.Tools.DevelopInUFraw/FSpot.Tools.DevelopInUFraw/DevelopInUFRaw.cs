/*
 * DevelopInUFraw.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.IO;

using Mono.Unix;

using Hyena;
using FSpot;
using FSpot.Core;
using FSpot.Utils;
using FSpot.Extensions;
using FSpot.Imaging;
using FSpot.UI.Dialog;

namespace FSpot.Tools.DevelopInUFraw
{
	// GUI Version
	public class DevelopInUFRaw : AbstractDevelopInUFRaw {
		public DevelopInUFRaw() : base("ufraw")
		{
		}

		public override void Run (object o, EventArgs e)
		{
			Log.Information ("Executing DevelopInUFRaw extension");

			foreach (Photo p in App.Instance.Organizer.SelectedPhotos ()) {
				DevelopPhoto (p);
			}
		}
	}
}
