/*
 * Mono.Google.Picasa.GDataApi.cs
 *
 * Author(s):
 *   Stephane Delcroix  <stephane@delcroix.org>
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 * Check Picasa Web Albums Data Api at http://code.google.com/apis/picasaweb/gdata.html
 *
 */


namespace Mono.Google.Picasa {
	class GDataApi {
		private const string feed = "http://picasaweb.google.com/data/feed/api/";
		private const string entry = "http://picasaweb.google.com/data/entry/api/";
		const string gallery = "user/{userid}?kind=album";
		const string album_by_id = "user/{userid}/albumid/{aid}?kind=photo";
		//const string album_by_name = entry + "user/{userid}/album/{aname}?kind=photo";
		const string picture_by_id = "user/{userid}/albumid/{aid}/photoid/{pid}";
		const string post_url = feed + "user/{userid}";
		const string post_picture = feed + "user/{userid}/albumid/{aid}";
		const string date_format = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.000Z'";

		public static string GetGalleryFeed (string user)
		{
			return feed + gallery.Replace ("{userid}", user);
		}

		public static string GetGalleryEntry (string user)
		{
			return entry + gallery.Replace ("{userid}", user);
		}

		public static string GetAlbumFeedById (string user, string aid)
		{
			return feed + album_by_id.Replace ("{userid}", user).Replace ("{aid}", aid);
		}

		public static string GetAlbumEntryById (string user, string aid)
		{
			return entry + album_by_id.Replace ("{userid}", user).Replace ("{aid}", aid);
		}

		//public static string GetAlbumByName (string user, string aname)
		//{
		//	return album_by_name.Replace ("{userid}", user).Replace ("{aname}", aname);
		//}

		public static string GetPictureEntry (string user, string aid, string pid)
		{
			return entry + picture_by_id.Replace ("{userid}", user).Replace ("{aid}", aid).Replace ("{pid}", pid);
		}

		public static string GetPictureFeed (string user, string aid, string pid)
		{
			return feed + picture_by_id.Replace ("{userid}", user).Replace ("{aid}", aid).Replace ("{pid}", pid);
		}

		public static string GetPostURL (string user)
		{
			return post_url.Replace ("{userid}", user);
		}

		public static string GetURLForUpload (string user, string aid)
		{
			return post_picture.Replace ("{userid}", user).Replace ("{aid}", aid);
		}

		public static string DateFormat {
			get { return date_format; }
		}
	}
}
