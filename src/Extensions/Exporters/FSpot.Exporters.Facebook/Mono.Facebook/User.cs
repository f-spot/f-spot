//
// Mono.Facebook.User.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//	George Talusan (george@convolve.ca)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Xml.Serialization;
using Mono.Facebook.Schemas;

namespace Mono.Facebook
{
	public class User : user
	{
		public static readonly string[] FIELDS = { "about_me", "activities", "affiliations", "birthday", "books",
			"current_location", "education_history", "first_name", "hometown_location", "interests", "last_name",
			"movies", "music", "name", "notes_count", "pic", "pic_big", "pic_small", "political", "profile_update_time",
			"quotes", "relationship_status", "religion", "sex", "significant_other_id",
			"status", "timezone", "tv", "uid", "wall_count" };

		[XmlIgnore ()]
		public Uri PicUri
		{
			get { return new Uri (pic); }
		}

		[XmlIgnore ()]
		public Uri PicBigUri
		{
			get { return new Uri (pic_big); }
		}

		[XmlIgnore ()]
		public Uri PicSmallUri
		{
			get { return new Uri (pic_small); }
		}
	}
}
