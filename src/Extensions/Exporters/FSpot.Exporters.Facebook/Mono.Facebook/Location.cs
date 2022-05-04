//
// Mono.Facebook.Location.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Serialization;

namespace Mono.Facebook
{
	public class Location
	{
		[XmlElement ("street")]
		public string Street;

		[XmlElement ("city")]
		public string City;

		[XmlElement ("state")]
		public string State;

		[XmlElement ("country")]
		public string Country;

		[XmlElement ("zip")]
		public string Zip;
	}
}
