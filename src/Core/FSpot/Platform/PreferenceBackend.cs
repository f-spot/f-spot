//
// PreferenceBackend.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2019 Stephen Shaw
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
using System.IO;

using Newtonsoft.Json.Linq;

using FSpot.Settings;

namespace FSpot.Platform
{
	public class PreferenceBackend
	{
		const string SettingsRoot = "FSpotSettings";

		static readonly object sync_handler = new object ();

		static readonly JObject client;

		public string SettingsFile { get; }

		public PreferenceBackend (string location)// = null)
		{
			if (string.IsNullOrWhiteSpace (location))
				SettingsFile = Path.Combine (Global.BaseDirectory, Global.SettingsName);
			else
				SettingsFile = location;
		}

		JObject Client {
			get {
				lock (sync_handler) {
					return client ?? LoadSettings ();
				}
			}
		}

		JObject LoadSettings ()
		{
			if (!File.Exists (SettingsFile)) {
				var empty = new JObject {
					[SettingsRoot] = new JObject ()
				};
				File.WriteAllText (SettingsFile, empty.ToString ());
			}

			var settingsFile = File.ReadAllText (SettingsFile);
			var o = JObject.Parse (settingsFile);
			return (JObject)o[SettingsRoot];
		}

		public object Get (string key)
		{
			try {
				var result = Client[key].ToString ();
				return result;
			} catch (Exception) {
				throw new NoSuchKeyException (key);
			}
		}

		internal T Get<T> (string key)
		{
			T value = default;
			try {
				value = (T)Get (key);
			} catch (NoSuchKeyException) {
			} catch (InvalidCastException) {
			}
			return value;
		}

		public void Set (string key, object o)
		{
			Client[SettingsRoot][key] = (JObject)o;
		}
	}
}
