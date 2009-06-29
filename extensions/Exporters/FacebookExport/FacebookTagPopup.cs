/*
 * FacebookTagPopup.cs
 *
 * Authors:
 *   George Talusan <george@convolve.ca>
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (C) 2007 George Talusan
 * Copyright (c) 2008-2009 Novell, Inc.
 *
 * This is free software. See COPYING for details.
 */

using System.Reflection;
using System.Collections.Generic;
using FSpot.Widgets;
using Mono.Facebook;

namespace FSpot.Exporter.Facebook
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
