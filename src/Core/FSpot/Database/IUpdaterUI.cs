//
// IUpdaterUI.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Database
{
	public interface IUpdaterUI {
		void Show ();
		void Pulse ();
		void Destroy ();
	}
}
