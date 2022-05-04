//
// Mono.Facebook.PeopleList.cs:
//
// Authors:
//	George Talusan (george@convolve.ca)
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Serialization;

namespace Mono.Facebook
{
	public class PeopleList
	{
		[XmlElement ("uid")]
		public long[] uid_array;

		public long[] UIds
		{
			get { return uid_array ?? new long[0]; }
		}
	}
}
