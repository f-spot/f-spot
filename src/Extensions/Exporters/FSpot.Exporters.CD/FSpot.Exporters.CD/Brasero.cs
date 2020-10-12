// Brasero.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2012 SUSE LINUX Products GmbH, Nuernberg, Germany.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// Borrowed parts from banshee.
// src/Backends/Banshee.Gnome/Banshee.GnomeBackend/Brasero.cs

using System.IO;
using System.Diagnostics;

using Hyena;

namespace FSpot.Exporters.CD
{
	public class Brasero : IDiscBurner
	{
		private string brasero_exec;
		public Brasero ()
		{
			brasero_exec = Paths.FindProgramInPath ("brasero");
			if (null == brasero_exec)
				throw new FileNotFoundException ("brasero");
		}

		/// <summary>
		/// Start the process to burn the media to disc
		/// </summary>
		public void Run ()
		{
			// -n, --ncb         Burn the contents of the burn:// URI
			// --immediately     Start burning immediately.
			// It seems like in past version --immediately was part of -n
			// Unfortunately, brasero (3.2) doesn't seem to clean up burn://
			// after a successful burn.
			ProcessStartInfo psi = new ProcessStartInfo (brasero_exec, "-n --immediately");
			Process.Start (psi);
		}
	}
}

