/*
 * Simple upload based on the api at 
 * http://www.flickr.com/services/api/upload.api.html
 */
using System;
using System.IO;
using System.Text;

public class FlickrRemote {
	// This is the uo
	public static string UploadUrl = "http://www.flickr.com/tools/uploader_go.gne";
	public static string AuthUrl = "http://www.flickr.com/tools/auth.gne";
	
	string email;
	string passwd;
	string username;
	int limit;
	int used;
	bool pro;

	public bool ExportTags;
	public FSpot.ProgressItem Progress;

	public FlickrRemote ()
	{
		//FIXME this api is lame
	}
	
	public bool Pro {
		get {
			return pro;
		}
	}

	public string Upload (Photo photo)
	{
		if (email == null || passwd == null)
			throw new Exception ("Must Login First");

		// FIXME flickr needs rotation

		FormClient client = new FormClient ();
		client.Add ("email", email);
		client.Add ("password", passwd);
		client.Add ("photo", new FileInfo (photo.DefaultVersionPath));
		if (photo.Description != null) {
			client.Add ("description", photo.Description);
		}
		if (ExportTags && photo.Tags != null) {
			StringBuilder taglist = new StringBuilder ();

			foreach (Tag t in photo.Tags) {
				taglist.Append (t.Name + " ");
			}
			
			client.Add ("tags", taglist.ToString ());
		}

		string error_verbose;
		int error_value;
		try {
			Stream response = client.Submit (UploadUrl, this.Progress).GetResponseStream ();

			System.Xml.XmlDocument doc = new System.Xml.XmlDocument ();
			doc.Load (response);
		
			System.Xml.XmlNode node = doc.SelectSingleNode ("//uploader/status");
			string status = node.ChildNodes [0].Value;
			if (status == "ok") {
				node = node.NextSibling;
				string photoid = node.ChildNodes [0].Value;

				System.Console.WriteLine ("Successful upload: photoid={0}", photoid);
				return photoid;
			} else {
				node = node.NextSibling;
				error_value = int.Parse (node.ChildNodes [0].Value);
				
				System.Console.WriteLine ("Got Error {0} while uploading", error_value);

				node = node.NextSibling;
				error_verbose = node.ChildNodes [0].Value;
			}
		} catch (Exception e) {
			throw new System.Exception ("Error parsing flickr response", e);
		}
		throw new System.Exception (error_verbose);
	}

	public bool Login (string email, string passwd)
	{
		this.email = email;
		this.passwd = passwd;

		FormClient client = new FormClient ();
		client.Add ("email", email);
		client.Add ("password", passwd);

		try {
			Stream response = client.Submit (AuthUrl, this.Progress).GetResponseStream ();
		
			System.Xml.XmlDocument doc = new System.Xml.XmlDocument ();
			doc.Load (response);

			System.Xml.XmlNode node = doc.SelectSingleNode ("//user/username");
			this.username = node.ChildNodes [0].Value;

			node = doc.SelectSingleNode ("//user/status/pro");
			this.pro = (int.Parse (node.ChildNodes [0].Value) == 1);

			node = doc.SelectSingleNode ("//user/transfer");
			foreach (System.Xml.XmlNode child in node.ChildNodes) {
				switch (child.Name) {
				case "limit":
					this.limit = int.Parse (child.ChildNodes [0].Value);
					break;
				case "used":
					this.limit = int.Parse (child.ChildNodes [0].Value);
					break;
				}
			}
			System.Console.WriteLine ("User {0} successfully logged in", this.username);
			return true;
		} catch (Exception e) {
			System.Console.WriteLine (e);
			return false;
		}

	}
}
