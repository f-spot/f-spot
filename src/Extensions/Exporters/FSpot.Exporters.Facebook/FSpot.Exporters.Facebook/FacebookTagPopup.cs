//
// FacebookTagPopup.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Collections.Generic;

using FSpot.Widgets;

using Mono.Facebook;

namespace FSpot.Exporters.Facebook
{
	internal class FacebookTagPopup : BuilderWindow
	{
		Dictionary<long, User> _friends;

		public FacebookTagPopup (Dictionary<long, User> friends) : base (Assembly.GetExecutingAssembly (), "FacebookExport.ui", "facebook_tag_popup")
		{
			Friends = friends;
		}

		public Dictionary<long, User> Friends
		{
			get { return _friends; }
			set { _friends = value; }
		}
	}
}
