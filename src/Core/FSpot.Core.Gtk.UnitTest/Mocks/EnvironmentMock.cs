//
// EnvironmentMock.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.FileSystem;
using Moq;

namespace Mocks
{
	public class EnvironmentMock
	{
		readonly Mock<IEnvironment> mock;

		public IEnvironment Object {
			get {
				return mock.Object;
			}
		}

		public static IEnvironment Create (string user)
		{
			var environmentMock = new Mock<IEnvironment> ();
			environmentMock.Setup (e => e.UserName).Returns (user);
			return environmentMock.Object;
		}

		public EnvironmentMock (string variable, string value)
		{
			mock = new Mock<IEnvironment> ();
			SetVariable (variable, value);
		}

		public void SetVariable (string variable, string value)
		{
			mock.Setup (m => m.GetEnvironmentVariable (variable)).Returns (value);
		}
	}
}
