/*
 * Simple upload based on the api at tcpdumps of the fotki web interfaces
 * 
 */
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Net;
using FSpot;

public class FotkiRemote {
	// This is the up
	public static string UploadBaseUrl = "http://public.fotki.com/";
	public static string AuthUrl = "http://login.fotki.com/";
	
	private CookieContainer cookies;
	
	string login;
	string passwd;

	public void Upload (Photo photo, string gallery)
	{
		if (login == null || passwd == null)
			throw new Exception ("Must Login First");

		string path = string.Format ("/{0}/{1}/", login, gallery);
		
		FormClient client = new FormClient (cookies);
		client.SuppressCookiePath = true;
		client.Add ("cmd", "uploadns1");
		client.Add ("start", System.Web.HttpUtility.UrlEncode (path));
		client.Add ("photo", new FileInfo (photo.DefaultVersionUri.LocalPath));
		client.Add ("desc", photo.Description);
		if (photo.Tags != null) {
			StringBuilder taglist = new StringBuilder ();

			foreach (Tag t in photo.Tags) {
				taglist.Append (t.Name + " ");
			}
			
			client.Add ("keywords", taglist.ToString ());
		}

		string upload_url = UploadBaseUrl + path + "?";

		Stream response = client.Submit (upload_url).GetResponseStream ();
		StreamReader reader = new StreamReader (response, Encoding.UTF8);

		Console.WriteLine (reader.ReadToEnd ());
	}

	public void Login (string login, string passwd)
	{
		FormClient client = new FormClient (cookies);
		client.SuppressCookiePath = true;
		client.Add ("finish", String.Empty);
		client.Add ("check_cookies", String.Empty);
		client.Add ("login", login);
		client.Add ("password", passwd);

		Stream response = client.Submit (AuthUrl).GetResponseStream ();
		StreamReader reader = new StreamReader (response, Encoding.UTF8);
		
		Console.WriteLine (reader.ReadToEnd ());

		this.login = login;
		this.passwd = passwd;
	}

	public FotkiRemote ()
	{
	       //FIXME this api is lame
		cookies = new CookieContainer ();
		cookies.Add (new Cookie ("c", "1"));
	}
}
