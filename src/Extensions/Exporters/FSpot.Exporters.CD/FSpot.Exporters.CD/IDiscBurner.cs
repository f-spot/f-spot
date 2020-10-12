//
// IDiscBurner.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2012 SUSE LINUX Products GmbH, Nuernberg, Germany.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

/*
 * Previously there was unmanaged code that called the command brasero -n
 * using:
 * 		[DllImport ("libc")]
 *		extern static int system (string program);
 * essentially forcing CDExport to be tied exclusively to brasero and gnome.
 *
 * The idea with this change is to allow other burners and platforms.  So,
 * not much has changed yet, but ideally it would be cool if the platform
 * be detected and all of this done properly.
 */

namespace FSpot.Exporters.CD
{
	/// <summary>
	/// Provides a generic interface for potentially other burner
	/// and platforms.
	/// </summary>
	public interface IDiscBurner
	{
		void Run ();
	}
}

