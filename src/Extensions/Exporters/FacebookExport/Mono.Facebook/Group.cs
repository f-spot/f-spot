//
// Mono.Facebook.Group.cs:
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
	public class Group : group, SessionWrapper
	{
        [XmlIgnore]
		public FacebookSession Session { get; set; }

		[XmlIgnore ()]
		public Uri Picture
		{
			get { return new Uri (pic); }
		}

		[XmlIgnore ()]
		public Uri PictureBig
		{
			get { return new Uri (pic_big); }
		}

		[XmlIgnore ()]
		public Uri PictureSmall
		{
			get { return new Uri (pic_small); }
		}

		[XmlIgnore ()]
		public Uri WebSite
		{
			get
			{
				if (website == string.Empty)
					return null;

				return new Uri (website);
			}
		}

		public GroupMemberList GetMembers ()
		{
			GroupMembersResponse rsp = Session.Util.GetResponse<GroupMembersResponse>("facebook.groups.getMembers",
				FacebookParam.Create ("gid", gid),
				FacebookParam.Create ("call_id", DateTime.Now.Ticks),
				FacebookParam.Create ("session_key", Session.SessionKey));

			GroupMemberList members = new GroupMemberList ();

			members.Members = new Friend [rsp.Members.UIds.Length];
			for (int i = 0; i < members.Members.Length; i++)
				members.Members [i] = new Friend (rsp.Members.UIds [i], this.Session);

			members.Admins = new Friend [rsp.Admins.UIds.Length];
			for (int i = 0; i < members.Admins.Length; i++)
				members.Admins [i] = new Friend (rsp.Admins.UIds [i], this.Session);

			members.NotReplied = new Friend [rsp.NotReplied.UIds.Length];
			for (int i = 0; i < members.NotReplied.Length; i++)
				members.NotReplied [i] = new Friend (rsp.NotReplied.UIds [i], this.Session);

			members.Officers = new Friend [rsp.Officers.UIds.Length];
			for (int i = 0; i < members.Officers.Length; i++)
				members.Officers [i] = new Friend (rsp.Officers.UIds [i], this.Session);

			return members;
		}
	}

	public class GroupMemberList
	{
		public Friend[] Members;

		public Friend[] Admins;

		public Friend[] Officers;

		public Friend[] NotReplied;
	}
}
