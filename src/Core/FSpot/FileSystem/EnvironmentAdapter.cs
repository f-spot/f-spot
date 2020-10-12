//
// EnvironmentAdapter.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.FileSystem
{
	class EnvironmentAdapter : IEnvironment
	{
		#region IEnvironment implementation

		public string GetEnvironmentVariable (string variable)
		{
			return Environment.GetEnvironmentVariable (variable);
		}

		public string UserName {
			get {
				return Environment.UserName;
			}
		}

		#endregion
	}
}
