//
// Mono.Facebook.FacebookSession.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//	George Talusan (george@convolve.ca)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;

namespace Mono.Facebook
{
	public class FacebookSession
	{
		Util util;
		SessionInfo session_info;
		string auth_token;

		internal string SessionKey {
			get { return session_info.session_key; }
		}

		internal Util Util
		{
			get { return util; }
		}

		// use this for plain sessions
		public FacebookSession (string api_key, string shared_secret)
		{
			util = new Util (api_key, shared_secret);
		}

		// use this if you want to re-start an infinite session
		public FacebookSession (string api_key, SessionInfo session_info)
			: this (api_key, session_info.secret)
		{
			this.session_info = session_info;
		}

		public Uri CreateToken ()
		{
			XmlDocument doc = util.GetResponse ("facebook.auth.createToken");
			auth_token = doc.InnerText;

			return new Uri (string.Format ("http://www.facebook.com/login.php?api_key={0}&v=1.0&auth_token={1}", util.ApiKey, auth_token));
		}

		public Uri GetGrantUri (string permission)
		{
			return new Uri(string.Format("http://www.facebook.com/authorize.php?api_key={0}&v=1.0&ext_perm={1}", util.ApiKey, permission));
		}

		public bool HasAppPermission(string permission)
		{
			return util.GetBoolResponse("facebook.users.hasAppPermission",
				FacebookParam.Create ("call_id", DateTime.Now.Ticks),
				FacebookParam.Create ("session_key", session_info.session_key),
				FacebookParam.Create ("ext_perm", permission));
		}

		public bool RevokeAppPermission(string permission)
		{
			return util.GetBoolResponse
				("facebook.auth.revokeExtendedPermission",
					FacebookParam.Create ("call_id", DateTime.Now.Ticks),
					FacebookParam.Create ("session_key", session_info.session_key),
					FacebookParam.Create ("perm", permission));
		}

		public SessionInfo GetSession ()
		{
			return GetSessionFromToken(auth_token);
		}

		public SessionInfo GetSessionFromToken(string auth_token)
		{
			this.session_info = util.GetResponse<SessionInfo>("facebook.auth.getSession",
					FacebookParam.Create("auth_token", auth_token));
			this.util.SharedSecret = session_info.secret;

			this.auth_token = string.Empty;

			return session_info;
		}

		public Album[] GetAlbums ()
		{
			try {
				var rsp = util.GetResponse<AlbumsResponse> ("facebook.photos.getAlbums",
					FacebookParam.Create ("uid", session_info.uid),
					FacebookParam.Create ("session_key", session_info.session_key),
					FacebookParam.Create ("call_id", DateTime.Now.Ticks));

				var profile_rsp = util.GetResponse<AlbumsResponse> ("facebook.photos.getAlbums",
					FacebookParam.Create ("uid", session_info.uid),
					FacebookParam.Create ("session_key", session_info.session_key),
					FacebookParam.Create ("call_id", DateTime.Now.Ticks),
					FacebookParam.Create ("aid", -3));

				// Filters out Profile pictures album, can't upload there.
				return rsp.albums.Where ((a) => a.aid != profile_rsp.album [0].aid).ToArray ();
			} catch (FormatException) {
				return new Album[0];
			}
		}

		public Album CreateAlbum (string name, string description, string location)
		{
			// create parameter list
			List<FacebookParam> param_list = new List<FacebookParam> ();
			param_list.Add (FacebookParam.Create ("session_key", session_info.session_key));
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
			return this.GetGroups (session_info.uid, null);
		}

		public Group[] GetGroups (long? uid, long[] gids)
		{
			List<FacebookParam> param_list = new List<FacebookParam>();
			param_list.Add (FacebookParam.Create ("session_key", session_info.session_key));
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
			return GetEvents (session_info.uid, null, 0, 0, null);
		}

		public Event[] GetEvents (long? uid, long[] eids, long start_time, long end_time, string rsvp_status)
		{
			List<FacebookParam> param_list = new List<FacebookParam>();
			param_list.Add (FacebookParam.Create ("session_key", session_info.session_key));
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
			param_list.Add (FacebookParam.Create ("session_key", session_info.session_key));
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
			return new Me (session_info.uid, this);
		}

		public Notifications GetNotifications ()
		{
			Notifications notifications = util.GetResponse<Notifications>("facebook.notifications.get",
				FacebookParam.Create ("uid", session_info.uid),
				FacebookParam.Create ("session_key", session_info.session_key),
				FacebookParam.Create ("call_id", DateTime.Now.Ticks));

			foreach (Friend f in notifications.FriendRequests)
				f.Session = this;

			return notifications;
		}

		public Friend[] GetFriends ()
		{
			FriendsResponse response = Util.GetResponse<FriendsResponse>("facebook.friends.get",
					FacebookParam.Create ("session_key", session_info.session_key),
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
				   FacebookParam.Create ("session_key", session_info.session_key),
				   FacebookParam.Create ("call_id", DateTime.Now.Ticks),
				   FacebookParam.Create ("uids1", uid1),
				   FacebookParam.Create ("uids2", uid2));

			return response.friend_infos[0].AreFriends;
		}

		public FriendInfo[] AreFriends (long[] uids1, long[] uids2)
		{
			List<FacebookParam> param_list = new List<FacebookParam> ();
			param_list.Add (FacebookParam.Create ("session_key", session_info.session_key));
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
				FacebookParam.Create ("session_key", session_info.session_key),
				FacebookParam.Create ("call_id", DateTime.Now.Ticks),
				FacebookParam.Create ("query", sql_query));

			return doc;
		}
	}
}
