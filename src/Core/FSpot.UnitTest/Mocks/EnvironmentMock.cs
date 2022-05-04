//
// EnvironmentMock.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
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
