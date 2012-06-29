// Brasero.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2012 SUSE LINUX Products GmbH, Nuernberg, Germany.
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

