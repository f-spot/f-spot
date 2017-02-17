//
// MockExtensions.cs
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

using System;
using Moq.Language.Flow;
using System.Collections;

namespace FSpot.FileSystem.UnitTest.Mocks
{
	public static class MockExtensions
	{
		// Inspired by http://haacked.com/archive/2010/11/24/moq-sequences-revisited.aspx/
		// licensed under MIT license.
		public static void ReturnsInOrder<T, TResult>(this ISetup<T, TResult> setup, params object[] results)
			where T : class
		{
			var queue = new Queue (results);
			setup.Returns (() => {
				var result = queue.Dequeue ();
				var exception = result as Exception;
				if (exception != null) {
					throw exception;
				}
				return (TResult)result;
			});
		}
	}
}
