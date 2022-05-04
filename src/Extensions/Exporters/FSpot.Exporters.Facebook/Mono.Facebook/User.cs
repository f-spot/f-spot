//
// Mono.Facebook.User.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//	George Talusan (george@convolve.ca)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
