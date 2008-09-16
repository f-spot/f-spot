//
// Mono.Facebook.FacebookSession.cs:
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
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;

namespace Mono.Facebook
{
	public class FacebookSession
	{
		Util util;
		SessionInfo session_info;
		string auth_token;

		internal Util Util
		{
			get { return util; }
		}

		internal string SessionKey
		{
			get { return session_info.SessionKey; }
		}

		// use this for plain sessions
		public FacebookSession (string api_key, string shared_secret)
		{
			util = new Util (api_key, shared_secret);
		}

		// use this if you want to re-start an infinite session
		public FacebookSession (string api_key, SessionInfo session_info)
			: this (api_key, session_info.Secret)
		{
			this.session_info = session_info;
		}

		public Uri CreateToken ()
		{
			XmlDocument doc = util.GetResponse ("facebook.auth.createToken");
			auth_token = doc.InnerText;

			return new Uri (string.Format ("http://www.facebook.com/login.php?api_key={0}&v=1.0&auth_token={1}", util.ApiKey, auth_token));
		}

		public Uri GetUriForInfiniteToken()
		{
			return new Uri(string.Format("http://www.facebook.com/code_gen.php?v=1.0&api_key={0}", util.ApiKey));
		}

		public SessionInfo GetSession ()
		{
			return GetSessionFromToken(auth_token);
		}

		public SessionInfo GetSessionFromToken(string auth_token)
		{
			this.session_info = util.GetResponse<SessionInfo>("facebook.auth.getSession",
					FacebookParam.Create("auth_token", auth_token));
			this.util.SharedSecret = session_info.Secret;

			this.auth_token = string.Empty;

			return session_info;
		}

		public Album[] GetAlbums ()
		{
			AlbumsResponse rsp = util.GetResponse<AlbumsResponse> ("facebook.photos.getAlbums",
				FacebookParam.Create ("uid", session_info.UId),
				FacebookParam.Create ("session_key", session_info.SessionKey),
				FacebookParam.Create ("call_id", DateTime.Now.Ticks));

			foreach (Album album in rsp.Albums)
				album.Session = this;

			return rsp.Albums;
		}

		public Album CreateAlbum (string name, string description, string location)
		{
			// create parameter list
			List<FacebookParam> param_list = new List<FacebookParam> ();
			param_list.Add (FacebookParam.Create ("session_key", session_info.SessionKey));
			param_list.Add (FacebookParam.Create ("call_id", DateTime.Now.Ticks));
			param_list.Add (FacebookParam.Create ("name", name));

			if (description != null && description != string.Empty)
				param_list.Add (FacebookParam.Create ("description", description));

			if (location != null && location != string.Empty)
				param_list.Add (FacebookParam.Create ("location", location));

			// create the albums
			Album new_album = util.GetResponse<Album> ("facebook.photos.createAlbum", param_list.ToArray ());
			new_album.Session = this;

			// return
			return new_album;
		}

		public Group[] GetGroups ()
		{
			return this.GetGroups (session_info.UId, null);
		}

		public Group[] GetGroups (long? uid, long[] gids)
		{
			List<FacebookParam> param_list = new List<FacebookParam>();
			param_list.Add (FacebookParam.Create ("session_key", session_info.SessionKey));
			param_list.Add (FacebookParam.Create ("call_id", DateTime.Now.Ticks));
			if (uid != null)
				param_list.Add (FacebookParam.Create ("uid", uid));

			if (gids != null)
				param_list.Add (FacebookParam.Create ("gids", gids));

			GroupsResponse rsp = util.GetResponse<GroupsResponse>("facebook.groups.get", param_list.ToArray ());

			foreach (Group gr in rsp.Groups)
				gr.Session = this;

			return rsp.Groups;
		}

		public Event[] GetEvents ()
		{
			return GetEvents (session_info.UId, null, 0, 0, null);
		}

		public Event[] GetEvents (long? uid, long[] eids, long start_time, long end_time, string rsvp_status)
		{
			List<FacebookParam> param_list = new List<FacebookParam>();
			param_list.Add (FacebookParam.Create ("session_key", session_info.SessionKey));
			param_list.Add (FacebookParam.Create ("call_id", DateTime.Now.Ticks));
			if (uid != null)
				param_list.Add (FacebookParam.Create ("uid", uid));

			if (eids != null)
				param_list.Add (FacebookParam.Create ("eids", eids));

			param_list.Add (FacebookParam.Create ("start_time", start_time));
			param_list.Add (FacebookParam.Create ("end_time", start_time));
			if (rsvp_status != null)
				param_list.Add (FacebookParam.Create ("rsvp_status", rsvp_status));

			EventsResponse rsp = util.GetResponse<EventsResponse>("facebook.events.get", param_list.ToArray ());

			foreach (Event evt in rsp.Events)
				evt.Session = this;

			return rsp.Events;
		}

		public User GetUserInfo (long uid)
		{
			User[] users = this.GetUserInfo (new long[1] { uid }, User.FIELDS);

			if (users.Length < 1)
				return null;

			return users[0];
		}

		public User[] GetUserInfo (long[] uids, string[] fields)
		{
			List<FacebookParam> param_list = new List<FacebookParam>();
			param_list.Add (FacebookParam.Create ("session_key", session_info.SessionKey));
			param_list.Add (FacebookParam.Create ("call_id", DateTime.Now.Ticks));

			if (uids == null || uids.Length == 0)
				throw new Exception ("uid not provided");

			param_list.Add (FacebookParam.Create ("uids", uids));
			param_list.Add (FacebookParam.Create ("fields", fields));

			UserInfoResponse rsp = util.GetResponse<UserInfoResponse>("facebook.users.getInfo", param_list.ToArray ());
			return rsp.Users;
		}

		public Me GetLoggedInUser ()
		{
			return new Me (session_info.UId, this);
		}

		public Notifications GetNotifications ()
		{
			Notifications notifications = util.GetResponse<Notifications>("facebook.notifications.get",
				FacebookParam.Create ("uid", session_info.UId),
				FacebookParam.Create ("session_key", session_info.SessionKey),
				FacebookParam.Create ("call_id", DateTime.Now.Ticks));

			foreach (Friend f in notifications.FriendRequests)
				f.Session = this;

			return notifications;
		}

		public Friend[] GetFriends ()
		{
			FriendsResponse response = Util.GetResponse<FriendsResponse>("facebook.friends.get",
					FacebookParam.Create ("session_key", SessionKey),
					FacebookParam.Create ("call_id", DateTime.Now.Ticks));

			Friend[] friends = new Friend[response.UIds.Length];

			for (int i = 0; i < friends.Length; i++)
				friends[i] = new Friend (response.UIds[i], this);

			return friends;
		}

		public bool AreFriends (Friend friend1, Friend friend2)
		{
			return AreFriends (friend1.UId, friend2.UId);
		}

		public bool AreFriends (long uid1, long uid2)
		{
			AreFriendsResponse response = Util.GetResponse<AreFriendsResponse>("facebook.friends.areFriends",
				   FacebookParam.Create ("session_key", SessionKey),
				   FacebookParam.Create ("call_id", DateTime.Now.Ticks),
				   FacebookParam.Create ("uids1", uid1),
				   FacebookParam.Create ("uids2", uid2));

			return response.friend_infos[0].AreFriends;
		}

		public FriendInfo[] AreFriends (long[] uids1, long[] uids2)
		{
			List<FacebookParam> param_list = new List<FacebookParam> ();
			param_list.Add (FacebookParam.Create ("session_key", session_info.SessionKey));
			param_list.Add (FacebookParam.Create ("call_id", DateTime.Now.Ticks));

			if (uids1 == null || uids1.Length == 0)
				throw new Exception ("uids1 not provided");

			if (uids2 == null || uids2.Length == 0)
				throw new Exception ("uids2 not provided");

			param_list.Add (FacebookParam.Create ("uids1", uids1));
			param_list.Add (FacebookParam.Create ("uids2", uids2));

			AreFriendsResponse rsp = util.GetResponse<AreFriendsResponse> ("facebook.friends.areFriends", param_list.ToArray ());
			return rsp.friend_infos;
		}

		public XmlDocument Query (string sql_query)
		{
			XmlDocument doc = Util.GetResponse ("facebook.fql.query",
				FacebookParam.Create ("session_key", SessionKey),
				FacebookParam.Create ("call_id", DateTime.Now.Ticks),
				FacebookParam.Create ("query", sql_query));

			return doc;
		}
	}
}
