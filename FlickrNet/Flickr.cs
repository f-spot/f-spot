using System;
using System.Net;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Serialization;
using System.Text;
using System.Collections;
using System.Collections.Specialized;

namespace FlickrNet
{
	/// <summary>
	/// The main Flickr class.
	/// </summary>
	/// <remarks>
	/// Create an instance of this class and then call its methods to perform methods on Flickr.
	/// </remarks>
	/// <example>
	/// <code>FlickrNet.Flickr flickr = new FlickrNet.Flickr();
	/// User user = flickr.PeopleFindByEmail("cal@iamcal.com");
	/// Console.WriteLine("User Id is " + u.UserId);</code>
	/// </example>
	public class Flickr
	{
		/// <summary>
		/// </summary>
		public delegate void UploadProgressHandler(object sender, UploadProgressEventArgs e);

		/// <summary>
		/// UploadProgressHandler is fired during a synchronous upload process to signify that 
		/// a segment of uploading has been completed. This is approximately 50 bytes. The total
		/// uploaded is recorded in the <see cref="UploadProgressEventArgs"/> class.
		/// </summary>
		public event UploadProgressHandler OnUploadProgress;

		#region [ Private Variables ]
		private const string _baseUrl = "http://www.flickr.com/services/rest/";
		private const string _uploadUrl = "http://www.flickr.com/services/upload/";
		private const string _authUrl = "http://www.flickr.com/tools/auth.gne";
		private string _apiKey;
		private string _apiToken;
		private string _sharedSecret;
		private int _timeout = 30000;
		private const string UserAgent = "Mozilla/4.0 FlickrNet API (compatible; MSIE 6.0; Windows NT 5.1)";
		private string _lastRequest;
		private string _lastResponse;

		private WebProxy _proxy = WebProxy.GetDefaultProxy();

		// Static serializers
		private static XmlSerializer _responseSerializer = new XmlSerializer(typeof(FlickrNet.Response));
		private static XmlSerializer _uploaderSerializer = new XmlSerializer(typeof(FlickrNet.Uploader));

		#endregion

		#region [ Public Properties ]
		/// <summary>
		/// Get or set the API Key to be used by all calls. API key is mandatory for all 
		/// calls to Flickr.
		/// </summary>
		public string ApiKey 
		{ 
			get { return _apiKey; } 
			set { _apiKey = (value==null||value.Length==0?null:value); }
		}

		/// <summary>
		/// API shared secret is required for all calls that require signing, which includes
		/// all methods that require authentication, as well as the actual flickr.auth.* calls.
		/// </summary>
		public string ApiSecret
		{
			get { return _sharedSecret; }
			set { _sharedSecret = (value==null||value.Length==0?null:value); }
		}

		/// <summary>
		/// The API token is required for all calls that require authentication.
		/// A <see cref="FlickrException"/> will be raised by Flickr if the API token is
		/// not set when required.
		/// </summary>
		/// <remarks>
		/// It should be noted that some methods will work without the API token, but
		/// will return different results if used with them (such as group pool requests, 
		/// and results which include private pictures the authenticated user is allowed to see
		/// (their own, or others).
		/// </remarks>
		public string ApiToken 
		{
			get { return _apiToken; }
			set { _apiToken = (value==null||value.Length==0?null:value); }
		}

		/// <summary>
		/// All GET calls to Flickr are cached by the Flickr.Net API. Set the <see cref="CacheTimeout"/>
		/// to determine how long these calls should be cached (make this as long as possible!)
		/// </summary>
		public TimeSpan CacheTimeout
		{
			get { return Cache.CacheTimeout; }
			set { Cache.CacheTimeout = value; }
		}

		/// <summary>
		/// <see cref="CacheSizeLimit"/> is the cache file size in bytes for downloaded
		/// pictures. The default is 50MB (or 50 * 1024 * 1025 in bytes).
		/// </summary>
		public long CacheSizeLimit
		{
			get { return Cache.CacheSizeLimit; }
			set { Cache.CacheSizeLimit = value; }
		}

		/// <summary>
		/// Internal timeout for all web requests in milliseconds. Defaults to 30 seconds.
		/// </summary>
		public int HttpTimeout
		{
			get { return _timeout; } 
			set { _timeout = value; }
		}

		/// <summary>
		/// Checks to see if a shared secret and an api token are stored in the object.
		/// Does not check if these values are valid values.
		/// </summary>
		public bool IsAuthenticated
		{
			get { return (_sharedSecret != null && _apiToken != null); }
		}

		/// <summary>
		/// Returns the raw XML returned from the last response.
		/// Only set it the response was not returned from cache.
		/// </summary>
		public string LastResponse
		{
			get { return _lastResponse; }
		}

		/// <summary>
		/// Returns the last URL requested. Includes API signing.
		/// </summary>
		public string LastRequest
		{
			get { return _lastRequest; }
		}

		/// <summary>
		/// You can set the <see cref="WebProxy"/> or alter its properties.
		/// It defaults to your internet explorer proxy settings.
		/// </summary>
		public WebProxy Proxy { get { return _proxy; } set { _proxy = value; } }
		#endregion

		#region [ Cache Methods ]
		/// <summary>
		/// Forces the Cache to save to disk.
		/// </summary>
		public void ForceCacheSave()
		{
			Cache.ForceSave();
		}

		/// <summary>
		/// Clears the cache completely.
		/// </summary>
		public void FlushCache()
		{
			Cache.FlushCache();
		}

		/// <summary>
		/// Clears the cache for a particular URL.
		/// </summary>
		/// <param name="url">The URL to remove from the cache.</param>
		/// <remarks>
		/// The URL can either be an image URL for a downloaded picture, or
		/// a request URL (see <see cref="LastRequest"/> for getting the last URL).
		/// </remarks>
		public void FlushCache(string url)
		{
			Cache.FlushCache(url);
		}

		/// <summary>
		/// Provides static access to the list of cached photos.
		/// </summary>
		/// <returns>An array of <see cref="PictureCacheItem"/> objects.</returns>
		public PictureCacheItem[] GetCachePictures()
		{
			lock(Cache.DownloadCache.SyncRoot)
			{
				PictureCacheItem[] cache = new PictureCacheItem[Cache.DownloadCache.Count];
				for(int i = 0; i< Cache.DownloadCache.Count; i++)
				{
					if( Cache.DownloadCache.GetByIndex(i) is PictureCacheItem )
						cache[i] = (PictureCacheItem)Cache.DownloadCache.GetByIndex(i);
				}
				return cache;
			}
		}
		#endregion

		#region [ Constructors ]

		/// <summary>
		/// Constructor loads configuration settings from app.config or web.config file if they exist.
		/// </summary>
		public Flickr()
		{
			FlickrConfigurationSettings settings = FlickrConfigurationManager.Settings;
			if( settings == null ) return;

			if( settings.CacheSize != 0 ) CacheSizeLimit = settings.CacheSize;
			if( settings.CacheTimeout != TimeSpan.MinValue ) CacheTimeout = settings.CacheTimeout;
			ApiKey = settings.ApiKey;
			ApiToken = settings.ApiToken;
			ApiSecret = settings.SharedSecret;

			if( settings.IsProxyDefined )
			{
				Proxy.Address = new Uri("http://" + settings.ProxyIPAddress + ":" + settings.ProxyPort);
				if( settings.ProxyUsername != null && settings.ProxyUsername.Length > 0 )
				{
					NetworkCredential creds = new NetworkCredential();
					creds.UserName = settings.ProxyUsername;
					creds.Password = settings.ProxyPassword;
					creds.Domain = settings.ProxyDomain;
					Proxy.Credentials = creds;
				}
			}
		}

		/// <summary>
		/// Create a new instance of the <see cref="Flickr"/> class with no authentication.
		/// </summary>
		/// <param name="apiKey">Your Flickr API Key.</param>
		public Flickr(string apiKey) : this(apiKey, "", "")
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="Flickr"/> class with an API key and a Shared Secret.
		/// This is only useful really useful for calling the Auth functions as all other
		/// authenticationed methods also require the API Token.
		/// </summary>
		/// <param name="apiKey">Your Flickr API Key.</param>
		/// <param name="sharedSecret">Your Flickr Shared Secret.</param>
		public Flickr(string apiKey, string sharedSecret) : this(apiKey, sharedSecret, "")
		{
		}

		/// <summary>
		/// Create a new instance of the <see cref="Flickr"/> class with the email address and password given
		/// </summary>
		/// <param name="apiKey">Your Flickr API Key</param>
		/// <param name="sharedSecret">Your FLickr Shared Secret.</param>
		/// <param name="token">The token for the user who has been authenticated.</param>
		public Flickr(string apiKey, string sharedSecret, string token) : this()
		{
			_apiKey = apiKey;
			_sharedSecret = sharedSecret;
			_apiToken = token;
		}
		#endregion

		#region [ Private Methods ]
		/// <summary>
		/// A private method which performs the actual HTTP web request if
		/// the details are not found within the cache.
		/// </summary>
		/// <param name="url">The URL to download.</param>
		/// <returns>A <see cref="FlickrNet.Response"/> object.</returns>
		private string DoGetResponse(string url)
		{
			HttpWebRequest req = null;
			HttpWebResponse res = null;

			// Initialise the web request
			req = (HttpWebRequest)HttpWebRequest.Create(url);
			req.Method = "POST";
			req.ContentLength = 0;
			req.UserAgent = UserAgent;
			req.Proxy = Proxy;
			req.Timeout = HttpTimeout;
			req.KeepAlive = false;

			try
			{
				// Get response from the internet
				res = (HttpWebResponse)req.GetResponse();
			}
			catch(WebException ex)
			{
				if( ex.Status == WebExceptionStatus.ProtocolError )
				{
					HttpWebResponse res2 = (HttpWebResponse)ex.Response;
					if( res2 != null )
					{
						throw new FlickrException((int)res2.StatusCode, res2.StatusDescription);
					}
				}
				throw new FlickrException(9999, ex.Message);
			}

			string responseString = string.Empty;

			using (StreamReader sr = new StreamReader(res.GetResponseStream()))
			{
				responseString = sr.ReadToEnd();
			}

			return responseString;
		}

		/// <summary>
		/// Download a picture (or anything else actually).
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		private Stream DoDownloadPicture(string url)
		{
			HttpWebRequest req = null;
			HttpWebResponse res = null;

			try
			{
				req = (HttpWebRequest)HttpWebRequest.Create(url);
				req.UserAgent = UserAgent;
				req.Proxy = Proxy;
				req.Timeout = HttpTimeout;
				req.KeepAlive = false;
				res = (HttpWebResponse)req.GetResponse();
			}
			catch(WebException ex)
			{
				if( ex.Status == WebExceptionStatus.ProtocolError )
				{
					HttpWebResponse res2 = (HttpWebResponse)ex.Response;
					if( res2 != null )
					{
						throw new FlickrException((int)res2.StatusCode, res2.StatusDescription);
					}
				}
				else if( ex.Status == WebExceptionStatus.Timeout )
				{
					throw new FlickrException(301, "Request time-out");
				}
				throw new FlickrException(9999, "Picture download failed (" + ex.Message + ")");
			}

			return res.GetResponseStream();
		}
		#endregion

		#region [ GetResponse methods ]
		private Response GetResponseNoCache(NameValueCollection parameters)
		{
			return GetResponse(parameters, TimeSpan.MinValue);
		}

		private Response GetResponseAlwaysCache(NameValueCollection parameters)
		{
			return GetResponse(parameters, TimeSpan.MaxValue);
		}

		private Response GetResponseCache(NameValueCollection parameters)
		{
			return GetResponse(parameters, Cache.CacheTimeout);
		}

		private Response GetResponse(NameValueCollection parameters, TimeSpan cacheTimeout)
		{
			// Calulate URL
			string url = _baseUrl + "?";
			string hash = _sharedSecret;
			
			parameters["api_key"] = _apiKey;

			if( _apiToken != null )
			{
				parameters["auth_token"] = _apiToken;
			}

			string[] keys = parameters.AllKeys;
			Array.Sort(keys);

			foreach(string key in keys)
			{
				url += key + "=" + Utils.UrlEncode(parameters[key]) + "&";
				hash += key + parameters[key];
			}

			if( _sharedSecret != null && _sharedSecret.Length > 0 ) url += "&api_sig=" + Md5Hash(hash);

			_lastRequest = url;
			_lastResponse = string.Empty;

			// If not set to cache then just get live response
			if( cacheTimeout == TimeSpan.MinValue )
			{
				_lastResponse = DoGetResponse(url);
				return Deserialize(_lastResponse);
			}

			lock(Cache.ResponseCache.SyncRoot)
			{
				if( cacheTimeout == TimeSpan.MaxValue && Cache.ResponseCache.ContainsKey(url) )
				{
					Cache.ResponseCacheItem cacheItem = (Cache.ResponseCacheItem)Cache.ResponseCache[url];
					_lastResponse = cacheItem.Response;
					return Deserialize(_lastResponse);
				}

				if( Cache.ResponseCache.ContainsKey(url) )
				{
					Cache.ResponseCacheItem cacheItem = (Cache.ResponseCacheItem)Cache.ResponseCache[url];
					if( cacheItem.CreationTime.Add(cacheTimeout) < DateTime.Now )
					{
						Cache.ResponseCache.Remove(url);
					}
					else
					{
						_lastResponse = cacheItem.Response;
						return Deserialize(_lastResponse);
					}
				}

				_lastResponse = DoGetResponse(url);

				Cache.ResponseCacheItem resCache = new Cache.ResponseCacheItem();
				resCache.Response = _lastResponse;
				resCache.Url = url;
				resCache.CreationTime = DateTime.Now;

				FlickrNet.Response response = Deserialize(_lastResponse);

				if( response.Status == ResponseStatus.OK )
				{
					Cache.ResponseCache.Add(url, resCache);
				}

				return response;
			}
		}

		/// <summary>
		/// Converts the response string (in XML) into the <see cref="Response"/> object.
		/// </summary>
		/// <param name="responseString">The response from Flickr.</param>
		/// <returns>A <see cref="Response"/> object containing the details of the </returns>
		private static Response Deserialize(string responseString)
		{
			XmlSerializer serializer = _responseSerializer;
			try
			{
				// Deserialise the web response into the Flickr response object
				StringReader responseReader = new StringReader(responseString);
				FlickrNet.Response response = (FlickrNet.Response)serializer.Deserialize(responseReader);
				responseReader.Close();

				return response;
			}
			catch(InvalidOperationException ex)
			{
				// Serialization error occurred!
				throw new FlickrException(9998, "Invalid response received (" + ex.Message + ")");
			}
		}

		#endregion

		private string Md5Hash(string unhashed)
		{
			System.Security.Cryptography.MD5CryptoServiceProvider csp = new System.Security.Cryptography.MD5CryptoServiceProvider();
			byte[] bytes = System.Text.Encoding.Default.GetBytes(unhashed);
			byte[] hashedBytes = csp.ComputeHash(bytes, 0, bytes.Length);
			return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
		}

		#region [ DownloadPicture ]
		/// <summary>
		/// Downloads the picture from a internet and transfers it to a stream object.
		/// </summary>
		/// <param name="url">The url of the image to download.</param>
		/// <returns>A <see cref="Stream"/> object containing the downloaded picture.</returns>
		/// <remarks>
		/// The method checks the download cache first to see if the picture has already 
		/// been downloaded and if so returns the cached image. Otherwise it goes to the internet for the actual 
		/// image.
		/// </remarks>
		public System.IO.Stream DownloadPicture(string url)
		{
			lock(Cache.DownloadCache.SyncRoot)
			{
				if( Cache.DownloadCache.ContainsKey(url) )
				{
					PictureCacheItem cacheItem = (PictureCacheItem)Cache.DownloadCache[url];
					return Utils.GetApplicationDataReadStream(cacheItem.filename);
				}

				PictureCacheItem picCache = new PictureCacheItem();
				picCache.filename = Guid.NewGuid().ToString();
				Stream read = DoDownloadPicture(url);
				Stream write = Utils.GetApplicationDataWriteStream(picCache.filename);
				int b = read.ReadByte();
				long fileSize = 0;
				while( b != -1 )
				{
					fileSize++;
					write.WriteByte((byte)b);
					b = read.ReadByte();
				}
				read.Close();
				write.Close();

				while( Cache.CacheSize + fileSize > Cache.CacheSizeLimit )
				{
					Cache.DeleteOldest();
				}

				Cache.CacheSize += fileSize;

				picCache.url = url;
				picCache.creationTime = DateTime.Now;
				picCache.fileSize = fileSize;

				Cache.DownloadCache.Add(url, picCache);

				Cache.ForceSave();
				return Utils.GetApplicationDataReadStream(picCache.filename);
			}
		}
		#endregion

		#region [ Auth ]
		/// <summary>
		/// Retrieve a temporary FROB from the Flickr service, to be used in redirecting the
		/// user to the Flickr web site for authentication. Only required for desktop authentication.
		/// </summary>
		/// <remarks>
		/// Pass the FROB to the <see cref="AuthCalcUrl"/> method to calculate the url.
		/// </remarks>
		/// <example>
		/// <code>
		/// string frob = flickr.AuthGetFrob();
		/// string url = flickr.AuthCalcUrl(frob, AuthLevel.Read);
		/// 
		/// // redirect the user to the url above and then wait till they have authenticated and return to the app.
		/// 
		/// Auth auth = flickr.AuthGetToken(frob);
		/// 
		/// // then store the auth.Token for later use.
		/// string token = auth.Token;
		/// </code>
		/// </example>
		/// <returns>The FROB.</returns>
		public string AuthGetFrob()
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.auth.getFrob");
			parameters.Add("api_key", _apiKey);
			
			FlickrNet.Response response = GetResponseNoCache(parameters);
			if( response.Status == ResponseStatus.OK )
			{
				return response.AllElements[0].InnerText;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Calculates the URL to redirect the user to Flickr web site for
		/// authentication. Used by desktop application. 
		/// See <see cref="AuthGetFrob"/> for example code.
		/// </summary>
		/// <param name="frob">The FROB to be used for authentication.</param>
		/// <param name="authLevel">The <see cref="AuthLevel"/> stating the maximum authentication level your application requires.</param>
		/// <returns>The url to redirect the user to.</returns>
		public string AuthCalcUrl(string frob, AuthLevel authLevel)
		{
			if( _sharedSecret == null ) throw new FlickrException(0, "AuthGetToken requires signing. Please supply api key and secret.");

			string hash = _sharedSecret + "api_key" + _apiKey + "frob" + frob + "perms" + authLevel.ToString().ToLower();
			hash = Md5Hash(hash);
			string url = "http://www.flickr.com/services/auth/?api_key=" + _apiKey + "&perms=" + authLevel.ToString().ToLower() + "&frob=" + frob;
			url += "&api_sig=" + hash;

			return url;
		}

		/// <summary>
		/// Calculates the URL to redirect the user to Flickr web site for
		/// auehtntication. Used by Web applications. 
		/// See <see cref="AuthGetFrob"/> for example code.
		/// </summary>
		/// <remarks>
		/// The Flickr web site provides 'tiny urls' that can be used in place
		/// of this URL when you specify your return url in the API key page.
		/// It is recommended that you use these instead as they do not include
		/// your API or shared secret.
		/// </remarks>
		/// <param name="authLevel">The <see cref="AuthLevel"/> stating the maximum authentication level your application requires.</param>
		/// <returns>The url to redirect the user to.</returns>
		public string AuthCalcWebUrl(AuthLevel authLevel)
		{
			if( _sharedSecret == null ) throw new FlickrException(0, "AuthGetToken requires signing. Please supply api key and secret.");

			string hash = _sharedSecret + "api_key" + _apiKey + "perms" + authLevel.ToString().ToLower();
			hash = Md5Hash(hash);
			string url = "http://www.flickr.com/services/auth/?api_key=" + _apiKey + "&perms=" + authLevel.ToString().ToLower();
			url += "&api_sig=" + hash;

			return url;
		}

		/// <summary>
		/// After the user has authenticated your application on the flickr web site call this 
		/// method with the FROB (either stored from <see cref="AuthGetFrob"/> or returned in the URL
		/// from the Flickr web site) to get the users token.
		/// </summary>
		/// <param name="frob">The string containing the FROB.</param>
		/// <returns>A <see cref="Auth"/> object containing user and token details.</returns>
		public Auth AuthGetToken(string frob)
		{
			if( _sharedSecret == null ) throw new FlickrException(0, "AuthGetToken requires signing. Please supply api key and secret.");

			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.auth.getToken");
			parameters.Add("api_key", _apiKey);
			parameters.Add("frob", frob);

			FlickrNet.Response response = GetResponseNoCache(parameters);
			if( response.Status == ResponseStatus.OK )
			{
				Auth auth = new Auth(response.AllElements[0]);
				return auth;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}

		/// <summary>
		/// Checks a authentication token with the flickr service to make
		/// sure it is still valid.
		/// </summary>
		/// <param name="token">The authentication token to check.</param>
		/// <returns>The <see cref="Auth"/> object detailing the user for the token.</returns>
		public Auth AuthCheckToken(string token)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.auth.checkToken");
			parameters.Add("api_key", _apiKey);
			parameters.Add("auth_token", token);

			FlickrNet.Response response = GetResponseNoCache(parameters);
			if( response.Status == ResponseStatus.OK )
			{
				Auth auth = new Auth(response.AllElements[0]);
				return auth;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}
		#endregion

		#region [ UploadPicture ]
		/// <summary>
		/// Uploads a file to Flickr.
		/// </summary>
		/// <param name="filename">The filename of the file to open.</param>
		/// <returns>The id of the photo on a successful upload.</returns>
		/// <exception cref="FlickrException">Thrown when Flickr returns an error. see http://www.flickr.com/services/api/upload.api.html for more details.</exception>
		/// <remarks>Other exceptions may be thrown, see <see cref="FileStream"/> constructors for more details.</remarks>
		public string UploadPicture(string filename)
		{
			return UploadPicture(filename, null, null, null);
		}

		/// <summary>
		/// Uploads a file to Flickr.
		/// </summary>
		/// <param name="filename">The filename of the file to open.</param>
		/// <param name="title">The title of the photograph.</param>
		/// <returns>The id of the photo on a successful upload.</returns>
		/// <exception cref="FlickrException">Thrown when Flickr returns an error. see http://www.flickr.com/services/api/upload.api.html for more details.</exception>
		/// <remarks>Other exceptions may be thrown, see <see cref="FileStream"/> constructors for more details.</remarks>
		public string UploadPicture(string filename, string title)
		{
			return UploadPicture(filename, null, null, null);
		}

		/// <summary>
		/// Uploads a file to Flickr.
		/// </summary>
		/// <param name="filename">The filename of the file to open.</param>
		/// <param name="title">The title of the photograph.</param>
		/// <param name="description">The description of the photograph.</param>
		/// <returns>The id of the photo on a successful upload.</returns>
		/// <exception cref="FlickrException">Thrown when Flickr returns an error. see http://www.flickr.com/services/api/upload.api.html for more details.</exception>
		/// <remarks>Other exceptions may be thrown, see <see cref="FileStream"/> constructors for more details.</remarks>
		public string UploadPicture(string filename, string title, string description)
		{
			return UploadPicture(filename, null, null, null);
		}

		/// <summary>
		/// Uploads a file to Flickr.
		/// </summary>
		/// <param name="filename">The filename of the file to open.</param>
		/// <param name="title">The title of the photograph.</param>
		/// <param name="description">The description of the photograph.</param>
		/// <param name="tags">A comma seperated list of the tags to assign to the photograph.</param>
		/// <returns>The id of the photo on a successful upload.</returns>
		/// <exception cref="FlickrException">Thrown when Flickr returns an error. see http://www.flickr.com/services/api/upload.api.html for more details.</exception>
		/// <remarks>Other exceptions may be thrown, see <see cref="FileStream"/> constructors for more details.</remarks>
		public string UploadPicture(string filename, string title, string description, string tags)
		{
			Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			return UploadPicture(filename, stream, title, description, tags, -1, -1, -1);
		}

		/// <summary>
		/// Uploads a file to Flickr.
		/// </summary>
		/// <param name="filename">The filename of the file to open.</param>
		/// <param name="title">The title of the photograph.</param>
		/// <param name="description">The description of the photograph.</param>
		/// <param name="tags">A comma seperated list of the tags to assign to the photograph.</param>
		/// <param name="isPublic">True if the photograph should be public and false if it should be private.</param>
		/// <param name="isFriend">True if the photograph should be marked as viewable by friends contacts.</param>
		/// <param name="isFamily">True if the photograph should be marked as viewable by family contacts.</param>
		/// <returns>The id of the photo on a successful upload.</returns>
		/// <exception cref="FlickrException">Thrown when Flickr returns an error. see http://www.flickr.com/services/api/upload.api.html for more details.</exception>
		/// <remarks>Other exceptions may be thrown, see <see cref="FileStream"/> constructors for more details.</remarks>
		public string UploadPicture(string filename, string title, string description, string tags, bool isPublic, bool isFamily, bool isFriend)
		{
			Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			return UploadPicture(filename, stream, title, description, tags, isPublic?1:0, isFamily?1:0, isFriend?1:0);
		}

		/// <summary>
		/// Private method that does all the uploading work.
		/// </summary>
		/// <param name="filename">The filename of the file to be uploaded.</param>
		/// <param name="stream">The <see cref="Stream"/> object containing the pphoto to be uploaded.</param>
		/// <param name="title">The title of the photo (optional).</param>
		/// <param name="description">The description of the photograph (optional).</param>
		/// <param name="tags">The tags for the photograph (optional).</param>
		/// <param name="isPublic">0 for private, 1 for public.</param>
		/// <param name="isFamily">1 if family, 0 is not.</param>
		/// <param name="isFriend">1 if friend, 0 if not.</param>
		/// <returns>The id of the photograph after successful uploading.</returns>
		private string UploadPicture(string filename, Stream stream, string title, string description, string tags, int isPublic, int isFamily, int isFriend)
		{
			/*
			 * 
			 * Modified UploadPicture code taken from the Flickr.Net library
			 * URL: http://workspaces.gotdotnet.com/flickrdotnet
			 * It is used under the terms of the Common Public License 1.0
			 * URL: http://www.opensource.org/licenses/cpl.php
			 * 
			 * */

			string boundary = "FLICKR_MIME_" + DateTime.Now.ToString("yyyyMMddhhmmss");

			HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(_uploadUrl);
			req.UserAgent = "Mozilla/4.0 FlickrNet API (compatible; MSIE 6.0; Windows NT 5.1)";
			req.Method = "POST";
			req.Proxy = Proxy;
			req.Referer = "http://www.flickr.com";
			req.ContentType = "multipart/form-data; boundary=\"" + boundary + "\"";

			StringBuilder sb = new StringBuilder();

			NameValueCollection parameters = new NameValueCollection();
		
			if( title != null && title.Length > 0 )
			{
				parameters.Add("title", title);
			}
			if( description != null && description.Length > 0 )
			{
				parameters.Add("description", description);
			}
			if( tags != null && tags.Length > 0 )
			{
				parameters.Add("tags", tags);
			}
			if( isPublic >= 0 )
			{
				parameters.Add("is_public", isPublic.ToString());
			}
			if( isFriend >= 0 )
			{
				parameters.Add("is_friend", isFriend.ToString());
			}
			if( isFamily >= 0 )
			{
				parameters.Add("is_family", isFamily.ToString());
			}

			parameters.Add("api_key", _apiKey);
			parameters.Add("auth_token", _apiToken);

			string[] keys = parameters.AllKeys;
			string hash = _sharedSecret;
			Array.Sort(keys);

			foreach(string key in keys)
			{
				hash += key + parameters[key];

				sb.Append("--" + boundary + "\r\n");
				sb.Append("Content-Disposition: form-data; name=\"" + key + "\"\r\n");
				sb.Append("\r\n");
				sb.Append(parameters[key] + "\r\n");
			}

			sb.Append("--" + boundary + "\r\n");
			sb.Append("Content-Disposition: form-data; name=\"api_sig\"\r\n");
			sb.Append("\r\n");
			sb.Append(Md5Hash(hash) + "\r\n");

			// Photo
			sb.Append("--" + boundary + "\r\n");
			sb.Append("Content-Disposition: form-data; name=\"photo\"; filename=\"" + filename + "\"\r\n");
			sb.Append("Content-Type: image/jpeg\r\n");
			sb.Append("\r\n");

			UTF8Encoding encoding = new UTF8Encoding();

			byte[] postContents = encoding.GetBytes(sb.ToString());
			
			byte[] photoContents = new byte[stream.Length];
			stream.Read(photoContents, 0, photoContents.Length);
			stream.Close();

			byte[] postFooter = encoding.GetBytes("\r\n--" + boundary + "--\r\n");

			byte[] dataBuffer = new byte[postContents.Length + photoContents.Length + postFooter.Length];
			Buffer.BlockCopy(postContents, 0, dataBuffer, 0, postContents.Length);
			Buffer.BlockCopy(photoContents, 0, dataBuffer, postContents.Length, photoContents.Length);
			Buffer.BlockCopy(postFooter, 0, dataBuffer, postContents.Length + photoContents.Length, postFooter.Length);

			req.ContentLength = dataBuffer.Length;

			Stream resStream = req.GetRequestStream();

			int j = 1;
			int uploadBit = Math.Min(dataBuffer.Length / 1024, 50*1024);
			int uploadSoFar = 0;

			for(int i = 0; i < dataBuffer.Length; i=i+uploadBit)
			{
				int toUpload = Math.Min(uploadBit, dataBuffer.Length - i);
				uploadSoFar += toUpload;

				resStream.Write(dataBuffer, i, toUpload);

				if( (OnUploadProgress != null) && ((j++) % 50 == 0 || uploadSoFar == dataBuffer.Length) )
				{
					OnUploadProgress(this, new UploadProgressEventArgs(i+toUpload, uploadSoFar == dataBuffer.Length));
				}
			}
			resStream.Close();

			HttpWebResponse res = (HttpWebResponse)req.GetResponse();

			XmlSerializer serializer = _uploaderSerializer;

			StreamReader sr = new StreamReader(res.GetResponseStream());
			string s= sr.ReadToEnd();
			sr.Close();

			StringReader str = new StringReader(s);

			FlickrNet.Uploader uploader = (FlickrNet.Uploader)serializer.Deserialize(str);
			
			if( uploader.Status == ResponseStatus.OK )
			{
				return uploader.PhotoId;
			}
			else
			{
				throw new FlickrException(uploader.Code, uploader.Message);
			}
		}
		#endregion

		#region [ Blogs ]
		/// <summary>
		/// Gets a list of blogs that have been set up by the user.
		/// Requires authentication.
		/// </summary>
		/// <returns>A <see cref="Blogs"/> object containing the list of blogs.</returns>
		/// <remarks></remarks>
		public Blogs BlogGetList()
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.blogs.getList");
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Blogs;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Posts a photo already uploaded to a blog.
		/// Requires authentication.
		/// </summary>
		/// <param name="blogId">The Id of the blog to post the photo too.</param>
		/// <param name="photoId">The Id of the photograph to post.</param>
		/// <param name="title">The title of the blog post.</param>
		/// <param name="description">The body of the blog post.</param>
		/// <returns>True if the operation is successful.</returns>
		public bool BlogPostPhoto(int blogId, int photoId, string title, string description)
		{
			return BlogPostPhoto(blogId, photoId, title, description, null);
		}

		/// <summary>
		/// Posts a photo already uploaded to a blog.
		/// Requires authentication.
		/// </summary>
		/// <param name="blogId">The Id of the blog to post the photo too.</param>
		/// <param name="photoId">The Id of the photograph to post.</param>
		/// <param name="title">The title of the blog post.</param>
		/// <param name="description">The body of the blog post.</param>
		/// <param name="blogPassword">The password of the blog if it is not already stored in flickr.</param>
		/// <returns>True if the operation is successful.</returns>
		public bool BlogPostPhoto(int blogId, int photoId, string title, string description, string blogPassword)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.blogs.postPhoto");
			parameters.Add("title", Utils.UrlEncode(title));
			parameters.Add("description", Utils.UrlEncode(description));
			if( blogPassword != null ) parameters.Add("blog_password", blogPassword);
			FlickrNet.Response response = GetResponseCache(parameters);
			
			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}
		#endregion

		#region [ Contacts ]
		/// <summary>
		/// Gets a list of contacts for the logged in user.
		/// Requires authentication.
		/// </summary>
		/// <returns>An instance of the <see cref="Contacts"/> class containing the list of contacts.</returns>
		public Contacts ContactsGetList()
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.contacts.getList");
			parameters.Add("api_key", _apiKey);
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Contacts;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Gets a list of the given users contact, or those that are publically avaiable.
		/// </summary>
		/// <param name="userId">The Id of the user who's contacts you want to return.</param>
		/// <returns>An instance of the <see cref="Contacts"/> class containing the list of contacts.</returns>
		public Contacts ContactsGetPublicList(string userId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.contacts.getPublicList");
			parameters.Add("user_id", userId);
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Contacts;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}
		#endregion

		#region [ Favorites ]
		/// <summary>
		/// Adds a photo to the logged in favourites.
		/// Requires authentication.
		/// </summary>
		/// <param name="photoId">The id of the photograph to add.</param>
		/// <returns>True if the operation is successful.</returns>
		public bool FavoritesAdd(string photoId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.favorites.add");
			parameters.Add("photo_id", photoId);
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Removes a photograph from the logged in users favourites.
		/// Requires authentication.
		/// </summary>
		/// <param name="photoId">The id of the photograph to remove.</param>
		/// <returns>True if the operation is successful.</returns>
		public bool FavoritesRemove(string photoId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.favorites.remove");
			parameters.Add("photo_id", photoId);
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Get a list of the currently logger in users favourites.
		/// Requires authentication.
		/// </summary>
		/// <returns><see cref="Photos"/> instance containing a collection of <see cref="Photo"/> objects.</returns>
		public Photos FavoritesGetList()
		{
			return FavoritesGetList(null, 0, 0);
		}

		/// <summary>
		/// Get a list of the currently logger in users favourites.
		/// Requires authentication.
		/// </summary>
		/// <param name="perPage">Number of photos to include per page.</param>
		/// <param name="page">The page to download this time.</param>
		/// <returns><see cref="Photos"/> instance containing a collection of <see cref="Photo"/> objects.</returns>
		public Photos FavoritesGetList(int perPage, int page)
		{
			return FavoritesGetList(null, perPage, page);
		}

		/// <summary>
		/// Get a list of favourites for the specified user.
		/// </summary>
		/// <param name="userId">The user id of the user whose favourites you wish to retrieve.</param>
		/// <returns><see cref="Photos"/> instance containing a collection of <see cref="Photo"/> objects.</returns>
		public Photos FavoritesGetList(string userId)
		{
			return FavoritesGetList(userId, 0, 0);
		}

		/// <summary>
		/// Get a list of favourites for the specified user.
		/// </summary>
		/// <param name="userId">The user id of the user whose favourites you wish to retrieve.</param>
		/// <param name="perPage">Number of photos to include per page.</param>
		/// <param name="page">The page to download this time.</param>
		/// <returns><see cref="Photos"/> instance containing a collection of <see cref="Photo"/> objects.</returns>
		public Photos FavoritesGetList(string userId, int perPage, int page)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.favorites.getList");
			if( userId != null ) parameters.Add("user_id", userId);
			if( perPage > 0 ) parameters.Add("per_page", perPage.ToString());
			if( page > 0 ) parameters.Add("page", page.ToString());
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photos;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Gets the public favourites for a specified user.
		/// </summary>
		/// <remarks>This function difers from <see cref="Flickr.FavoritesGetList"/> in that the user id 
		/// is not optional.</remarks>
		/// <param name="userId">The is of the user whose favourites you wish to return.</param>
		/// <returns>A <see cref="Photos"/> object containing a collection of <see cref="Photo"/> objects.</returns>
		public Photos FavoritesGetPublicList(string userId)
		{
			return FavoritesGetPublicList(userId, 0, 0);
		}
			
		/// <summary>
		/// Gets the public favourites for a specified user.
		/// </summary>
		/// <remarks>This function difers from <see cref="Flickr.FavoritesGetList"/> in that the user id 
		/// is not optional.</remarks>
		/// <param name="userId">The is of the user whose favourites you wish to return.</param>
		/// <param name="perPage">The number of photos to return per page.</param>
		/// <param name="page">The specific page to return.</param>
		/// <returns>A <see cref="Photos"/> object containing a collection of <see cref="Photo"/> objects.</returns>
		public Photos FavoritesGetPublicList(string userId, int perPage, int page)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.favorites.getPublicList");
			parameters.Add("user_id", userId);
			if( perPage > 0 ) parameters.Add("per_page", perPage.ToString());
			if( page > 0 ) parameters.Add("page", page.ToString());
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photos;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}
		#endregion

		#region [ Groups ]
		/// <summary>
		/// Returns the top <see cref="Category"/> with a list of sub-categories and groups. 
		/// (The top category does not have any groups in it but others may).
		/// </summary>
		/// <returns>A <see cref="Category"/> instance.</returns>
		public Category GroupsBrowse()
		{
			return GroupsBrowse(0);
		}
		
		/// <summary>
		/// Returns the <see cref="Category"/> specified by the category id with a list of sub-categories and groups. 
		/// </summary>
		/// <param name="catId"></param>
		/// <returns>A <see cref="Category"/> instance.</returns>
		public Category GroupsBrowse(long catId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.groups.browse");
			parameters.Add("cat_id", catId.ToString());
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Category;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Returns a list of currently active groups.
		/// </summary>
		/// <returns>An <see cref="ActiveGroups"/> instance.</returns>
		public ActiveGroups GroupsGetActiveList()
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.groups.getActiveList");
			parameters.Add("api_key", _apiKey);
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.ActiveGroups;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Returns a <see cref="GroupInfo"/> object containing details about a group.
		/// </summary>
		/// <param name="groupId">The id of the group to return.</param>
		/// <returns>The <see cref="GroupInfo"/> specified by the group id.</returns>
		public GroupInfo GroupsGetInfo(string groupId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.groups.getInfo");
			parameters.Add("api_key", _apiKey);
			parameters.Add("group_id", groupId);
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.GroupInfo;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}
		#endregion

		#region [ Group Pool ]
		/// <summary>
		/// Adds a photo to a pool you have permission to add photos to.
		/// </summary>
		/// <param name="photoId">The id of one of your photos to be added.</param>
		/// <param name="groupId">The id of a group you are a member of.</param>
		/// <returns>True on a successful addition.</returns>
		public bool GroupPoolAdd(string photoId, string groupId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.groups.pools.add");
			parameters.Add("photo_id", photoId);
			parameters.Add("group_id", groupId);
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Gets the context for a photo from within a group. This provides the
		/// id and thumbnail url for the next and previous photos in the group.
		/// </summary>
		/// <param name="photoId">The Photo ID for the photo you want the context for.</param>
		/// <param name="groupId">The group ID for the group you want the context to be relevant to.</param>
		/// <returns>The <see cref="Context"/> of the photo in the group.</returns>
		public Context GroupPoolGetContext(string photoId, string groupId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.groups.pools.getContext");
			parameters.Add("photo_id", photoId);
			parameters.Add("group_id", groupId);
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				Context context = new Context();
				context.Count = response.ContextCount.Count;
				context.NextPhoto = response.ContextNextPhoto;
				context.PreviousPhoto = response.ContextPrevPhoto;
				return context;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Remove a picture from a group.
		/// </summary>
		/// <param name="photoId">The id of one of your pictures you wish to remove.</param>
		/// <param name="groupId">The id of the group to remove the picture from.</param>
		/// <returns>True if the photo is successfully removed.</returns>
		public bool GroupPoolRemove(string photoId, string groupId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.groups.pools.remove");
			parameters.Add("photo_id", photoId);
			parameters.Add("group_id", groupId);
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Gets a list of 
		/// </summary>
		/// <returns></returns>
		public PoolGroups GroupPoolGetGroups()
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.groups.pools.getGroups");
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.PoolGroups;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Gets a list of photos for a given group.
		/// </summary>
		/// <param name="groupId">The group ID for the group.</param>
		/// <returns>A <see cref="Photos"/> object containing the list of photos.</returns>
		public Photos GroupPoolGetPhotos(string groupId)
		{
			return GroupPoolGetPhotos(groupId, null, 0, 0);
		}

		/// <summary>
		/// Gets a list of photos for a given group.
		/// </summary>
		/// <param name="groupId">The group ID for the group.</param>
		/// <param name="tags">Space seperated list of tags that photos returned must have.</param>
		/// <returns>A <see cref="Photos"/> object containing the list of photos.</returns>
		public Photos GroupPoolGetPhotos(string groupId, string tags)
		{
			return GroupPoolGetPhotos(groupId, tags, 0, 0);
		}

		/// <summary>
		/// Gets a list of photos for a given group.
		/// </summary>
		/// <param name="groupId">The group ID for the group.</param>
		/// <param name="perPage">The number of photos per page.</param>
		/// <param name="page">The page to return.</param>
		/// <returns>A <see cref="Photos"/> object containing the list of photos.</returns>
		public Photos GroupPoolGetPhotos(string groupId, int perPage, int page)
		{
			return GroupPoolGetPhotos(groupId, null, perPage, page);
		}

		/// <summary>
		/// Gets a list of photos for a given group.
		/// </summary>
		/// <param name="groupId">The group ID for the group.</param>
		/// <param name="tags">Space seperated list of tags that photos returned must have.</param>
		/// <param name="perPage">The number of photos per page.</param>
		/// <param name="page">The page to return.</param>
		/// <returns>A <see cref="Photos"/> object containing the list of photos.</returns>
		public Photos GroupPoolGetPhotos(string groupId, string tags, int perPage, int page)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.groups.pools.getPhotos");
			parameters.Add("group_id", groupId);
			if( tags != null )parameters.Add("tags", tags);
			if( perPage > 0 ) parameters.Add("per_page", perPage.ToString());
			if( page > 0 ) parameters.Add("page", page.ToString());
			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photos;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}
		#endregion

		#region [ Notes ]
		/// <summary>
		/// Add a note to a picture.
		/// </summary>
		/// <param name="photoId">The photo id to add the note to.</param>
		/// <param name="noteX">The X co-ordinate of the upper left corner of the note.</param>
		/// <param name="noteY">The Y co-ordinate of the upper left corner of the note.</param>
		/// <param name="noteWidth">The width of the note.</param>
		/// <param name="noteHeight">The height of the note.</param>
		/// <param name="noteText">The text in the note.</param>
		/// <returns></returns>
		public string NotesAdd(string photoId, int noteX, int noteY, int noteWidth, int noteHeight, string noteText)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.notes.add");
			parameters.Add("photo_id", photoId);
			parameters.Add("note_x", noteX.ToString());
			parameters.Add("note_y", noteY.ToString());
			parameters.Add("note_w", noteWidth.ToString());
			parameters.Add("note_h", noteHeight.ToString());
			parameters.Add("note_text", noteText);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				foreach(XmlElement element in response.AllElements)
				{
					return element.Attributes["id", ""].Value;
				}
				return string.Empty;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Edit and update a note.
		/// </summary>
		/// <param name="noteId">The ID of the note to update.</param>
		/// <param name="noteX">The X co-ordinate of the upper left corner of the note.</param>
		/// <param name="noteY">The Y co-ordinate of the upper left corner of the note.</param>
		/// <param name="noteWidth">The width of the note.</param>
		/// <param name="noteHeight">The height of the note.</param>
		/// <param name="noteText">The new text in the note.</param>
		public void NotesEdit(string noteId, int noteX, int noteY, int noteWidth, int noteHeight, string noteText)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.notes.edit");
			parameters.Add("note_id", noteId);
			parameters.Add("note_x", noteX.ToString());
			parameters.Add("note_y", noteY.ToString());
			parameters.Add("note_w", noteWidth.ToString());
			parameters.Add("note_h", noteHeight.ToString());
			parameters.Add("note_text", noteText);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Delete an existing note.
		/// </summary>
		/// <param name="noteId">The ID of the note.</param>
		public void NotesDelete(string noteId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.notes.delete");
			parameters.Add("note_id", noteId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}
		#endregion

		#region [ People ]
		/// <summary>
		/// Used to fid a flickr users details by specifying their email address.
		/// </summary>
		/// <param name="emailAddress">The email address to search on.</param>
		/// <returns>The <see cref="User"/> object containing the matching details.</returns>
		/// <exception cref="FlickrException">A FlickrException is raised if the email address is not found.</exception>
		public User PeopleFindByEmail(string emailAddress)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.people.findByEmail");
			parameters.Add("api_key", _apiKey);
			parameters.Add("find_email", emailAddress);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.User;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Returns a <see cref="User"/> object matching the screen name.
		/// </summary>
		/// <param name="username">The screen name or username of the user.</param>
		/// <returns>A <see cref="User"/> class containing the userId and username of the user.</returns>
		/// <exception cref="FlickrException">A FlickrException is raised if the email address is not found.</exception>
		public User PeopleFindByUsername(string username)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.people.findByUsername");
			parameters.Add("api_key", _apiKey);
			parameters.Add("username", username);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.User;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Gets the <see cref="Person"/> object for the given user id.
		/// </summary>
		/// <param name="userId">The user id to find.</param>
		/// <returns>The <see cref="Person"/> object containing the users details.</returns>
		public Person PeopleGetInfo(string userId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.people.getInfo");
			parameters.Add("api_key", _apiKey);
			parameters.Add("user_id", userId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Person;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Get a list of people online in Flickr Live. Obsolete now.
		/// </summary>
		/// <returns>An instance of the <see cref="Online"/> class.</returns>
		public Online PeopleGetOnlineList()
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.peopl.getOnlineList");
			parameters.Add("api_key", _apiKey);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.OnlineUsers;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Get a list of public groups for a user.
		/// </summary>
		/// <param name="userId">The user id to get groups for.</param>
		/// <returns>An instance of the <see cref="PoolGroups"/> class.</returns>
		public PoolGroups PeopleGetPublicGroups(string userId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.people.getPublicGroups");
			parameters.Add("api_key", _apiKey);
			parameters.Add("user_id", userId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.PoolGroups;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Gets a users public photos. Excludes private photos.
		/// </summary>
		/// <param name="userId">The user id of the user.</param>
		/// <returns>The collection of photos contained within a <see cref="Photo"/> object.</returns>
		public Photos PeopleGetPublicPhotos(string userId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.people.getPublicPhotos");
			parameters.Add("api_key", _apiKey);
			parameters.Add("user_id", userId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photos;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}
		#endregion

		#region [ Photos ]
		/// <summary>
		/// Add a selection of tags to a photo.
		/// </summary>
		/// <param name="photoId">The photo id of the photo.</param>
		/// <param name="tags">An array of strings containing the tags.</param>
		/// <returns>True if the tags are added successfully.</returns>
		public void PhotosAddTags(string photoId, string[] tags)
		{	
			string s = string.Join(",", tags);
			PhotosAddTags(photoId, s);
		}

		/// <summary>
		/// Add a selection of tags to a photo.
		/// </summary>
		/// <param name="photoId">The photo id of the photo.</param>
		/// <param name="tags">An string of comma delimited tags.</param>
		/// <returns>True if the tags are added successfully.</returns>
		public void PhotosAddTags(string photoId, string tags)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.addTags");
			parameters.Add("photo_id", photoId);
			parameters.Add("tags", tags);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Get all the contexts (group, set and photostream 'next' and 'previous'
		/// pictures) for a photo.
		/// </summary>
		/// <param name="photoId">The photo id of the photo to get the contexts for.</param>
		/// <returns>An instance of the <see cref="AllContexts"/> class.</returns>
		public AllContexts PhotosGetAllContexts(string photoId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getAllContexts");
			parameters.Add("photo_id", photoId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				AllContexts contexts = new AllContexts(response.AllElements[0]);
				return contexts;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}

		/// <summary>
		/// Gets the most recent 10 photos from your contacts.
		/// </summary>
		/// <returns>An instance of the <see cref="Photo"/> class containing the photos.</returns>
		public Photos PhotosGetContactsPhotos()
		{
			return PhotosGetContactsPhotos(0, false, false, false);
		}

		/// <summary>
		/// Gets the most recent photos from your contacts.
		/// </summary>
		/// <param name="count">The number of photos to return, from between 10 and 50.</param>
		/// <returns>An instance of the <see cref="Photo"/> class containing the photos.</returns>
		public Photos PhotosGetContactsPhotos(long count)
		{
			return PhotosGetContactsPhotos(count, false, false, false);
		}

		/// <summary>
		/// Gets your contacts most recent photos.
		/// </summary>
		/// <param name="count">The number of photos to return, from between 10 and 50.</param>
		/// <param name="justFriends">If true only returns photos from contacts marked as
		/// 'friends'.</param>
		/// <param name="singlePhoto">If true only returns a single photo for each of your contacts.
		/// Ignores the count if this is true.</param>
		/// <param name="includeSelf">If true includes yourself in the group of people to 
		/// return photos for.</param>
		/// <returns>An instance of the <see cref="Photo"/> class containing the photos.</returns>
		public Photos PhotosGetContactsPhotos(long count, bool justFriends, bool singlePhoto, bool includeSelf)
		{
			if( (count < 10 || count > 50) && !singlePhoto )
			{
				throw new ArgumentOutOfRangeException("count", count, "Count must be between 10 and 50.");
			}
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getContactsPhotos");
			if( count > 0 && !singlePhoto ) parameters.Add("count", count.ToString());
			if( justFriends ) parameters.Add("just_friends", "1");
			if( singlePhoto ) parameters.Add("single_photo", "1");
			if( includeSelf ) parameters.Add("include_self", "1");

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photos;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}


		public Photos PhotosGetContactsPublicPhotos(string userId)
		{
			return PhotosGetContactsPublicPhotos(userId, 0, false, false, false);
		}

		public Photos PhotosGetContactsPublicPhotos(string userId, long count)
		{
			return PhotosGetContactsPublicPhotos(userId, count, false, false, false);
		}

		public Photos PhotosGetContactsPublicPhotos(string userId, long count, bool justFriends, bool singlePhoto, bool includeSelf)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getContactsPublicPhotos");
			parameters.Add("api_key", _apiKey);
			parameters.Add("user_id", userId);
			if( count > 0 ) parameters.Add("count", count.ToString());
			if( justFriends ) parameters.Add("just_friends", "1");
			if( singlePhoto ) parameters.Add("single_photo", "1");
			if( includeSelf ) parameters.Add("include_self", "1");

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photos;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public Context PhotosGetContext(string photoId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getContext");
			parameters.Add("photo_id", photoId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				Context c = new Context();
				c.Count = response.ContextCount.Count;
				c.NextPhoto = response.ContextNextPhoto;
				c.PreviousPhoto = response.ContextPrevPhoto;

				return c;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Returns count of photos between each pair of dates in the list.
		/// </summary>
		/// <remarks>If you pass in DateA, DateB and DateC it returns
		/// a list of the number of photos between DateA and DateB,
		/// followed by the number between DateB and DateC. 
		/// More parameters means more sets.</remarks>
		/// <param name="dates">Array of <see cref="DateTime"/> objects.</param>
		/// <returns><see cref="PhotoCounts"/> class instance.</returns>
		public PhotoCounts PhotosGetCounts(DateTime[] dates)
		{
			string s = "";
			foreach(DateTime d in dates)
			{
				s += Utils.DateToUnixTimestamp(d) + ",";
			}
			if( s.Length > 0 ) s = s.Substring(0, s.Length - 1);

			return PhotosGetCounts(s);
		}

		public PhotoCounts PhotosGetCounts(string dates)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getContactsPhotos");
			parameters.Add("dates", dates);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.PhotoCounts;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Gets the EXIF data for a given Photo ID.
		/// </summary>
		/// <param name="photoId">The Photo ID of the photo to return the EXIF data for.</param>
		/// <returns>An instance of the <see cref="ExifPhoto"/> class containing the EXIF data.</returns>
		public ExifPhoto PhotosGetExif(string photoId)
		{
			return PhotosGetExif(photoId, null);
		}

		public ExifPhoto PhotosGetExif(string photoId, string secret)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getExif");
			parameters.Add("photo_id", photoId);
			if( secret != null ) parameters.Add("secret", secret);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				ExifPhoto e = new ExifPhoto(response.PhotoInfo.PhotoId,
					response.PhotoInfo.Secret,
					response.PhotoInfo.Server,
					response.PhotoInfo.ExifTagCollection);

				return e;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public PhotoInfo PhotosGetInfo(string photoId)
		{
			return PhotosGetInfo(photoId, null);
		}
		
		public PhotoInfo PhotosGetInfo(string photoId, string secret)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getInfo");
			parameters.Add("photo_id", photoId);
			if( secret != null ) parameters.Add("secret", secret);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.PhotoInfo;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public PhotoPermissions PhotosGetPerms(string photoId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getExif");
			parameters.Add("photo_id", photoId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.PhotoPermissions;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public Photos PhotosGetRecent()
		{
			return PhotosGetRecent(0, 0);
		}

		public Photos PhotosGetRecent(long perPage, long page)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getRecent");
			parameters.Add("api_key", _apiKey);
			if( perPage > 0 ) parameters.Add("per_page", perPage.ToString());
			if( page > 0 ) parameters.Add("page", page.ToString());

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photos;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public Sizes PhotosGetSizes(string photoId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getSizes");
			parameters.Add("photo_id", photoId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Sizes;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public Photos PhotosGetUntagged()
		{
			return PhotosGetUntagged(0, 0);
		}

		public Photos PhotosGetUntagged(int perPage, int page)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getUntagged");
			if( perPage > 0 ) parameters.Add("per_page", perPage.ToString());
			if( page > 0 ) parameters.Add("page", page.ToString());

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photos;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Gets a list of photos not in sets. Defaults to include all extra fields.
		/// </summary>
		/// <returns><see cref="Photos"/> instance containing list of photos.</returns>
		public Photos PhotosGetNotInSet()
		{
			return PhotosGetNotInSet(0, 0, PhotoSearchExtras.All);
		}

		/// <summary>
		/// Gets a specific page of the list of photos which are not in sets.
		/// Defaults to include all extra fields.
		/// </summary>
		/// <param name="page">The page number to return.</param>
		/// <returns><see cref="Photos"/> instance containing list of photos.</returns>
		public Photos PhotosGetNotInSet(int page)
		{
			return PhotosGetNotInSet(0, page, PhotoSearchExtras.All);
		}

		/// <summary>
		/// Gets a specific page of the list of photos which are not in sets.
		/// Defaults to include all extra fields.
		/// </summary>
		/// <param name="perPage">Number of photos per page.</param>
		/// <param name="page">The page number to return.</param>
		/// <returns><see cref="Photos"/> instance containing list of photos.</returns>
		public Photos PhotosGetNotInSet(int perPage, int page)
		{
			return PhotosGetNotInSet(perPage, page, PhotoSearchExtras.All);
		}

		/// <summary>
		/// Gets a list of a users photos which are not in a set.
		/// </summary>
		/// <param name="perPage">Number of photos per page.</param>
		/// <param name="page">The page number to return.</param>
		/// <param name="extras"><see cref="PhotoSearchExtras"/> enumeration.</param>
		/// <returns><see cref="Photos"/> instance containing list of photos.</returns>
		public Photos PhotosGetNotInSet(int perPage, int page, PhotoSearchExtras extras)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.getNotInSet");
			if( perPage > 0 ) parameters.Add("per_page", perPage.ToString());
			if( page > 0 ) parameters.Add("page", page.ToString());
			if( extras != PhotoSearchExtras.None )
			{
				string val = "";
				if( (extras & PhotoSearchExtras.DateTaken) == PhotoSearchExtras.DateTaken )
					val += "date_taken,";
				if( (extras & PhotoSearchExtras.DateUploaded) == PhotoSearchExtras.DateUploaded )
					val += "date_upload,";
				if( (extras & PhotoSearchExtras.IconServer) == PhotoSearchExtras.IconServer )
					val += "icon_server,";
				if( (extras & PhotoSearchExtras.License) == PhotoSearchExtras.License )
					val += "license,";
				if( (extras & PhotoSearchExtras.OwnerName) == PhotoSearchExtras.OwnerName )
					val += "owner_name,";
				if( (extras & PhotoSearchExtras.OriginalFormat) == PhotoSearchExtras.OriginalFormat )
					val += "original_format";
				parameters.Add("extras", val);
			}

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photos;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Gets a list of all current licenses.
		/// </summary>
		/// <returns><see cref="Licenses"/> instance.</returns>
		public Licenses PhotosLicensesGetInfo()
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.licenses.getInfo");
			parameters.Add("api_key", _apiKey);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Licenses;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Remove an existing tag.
		/// </summary>
		/// <param name="tagId">The id of the tag, as returned by <see cref="Flickr.PhotosGetInfo"/> or similar method.</param>
		/// <returns>True if the tag was removed.</returns>
		public bool PhotosRemoveTag(long tagId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.removeTag");
			parameters.Add("tag_id", tagId.ToString());

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		// PhotoSearch - text versions
		public Photos PhotosSearchText(string userId, string text)
		{
			return PhotosSearch(userId, "", 0, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearchText(string userId, string text, PhotoSearchExtras extras)
		{
			return PhotosSearch(userId, "", 0, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, extras);
		}

		public Photos PhotosSearchText(string userId, string text, int license)
		{
			return PhotosSearch(userId, "", 0, text, DateTime.MinValue, DateTime.MinValue, license, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearchText(string userId, string text, int license, PhotoSearchExtras extras)
		{
			return PhotosSearch(userId, "", 0, text, DateTime.MinValue, DateTime.MinValue, license, 0, 0, extras);
		}

		public Photos PhotosSearchText(string text, PhotoSearchExtras extras)
		{
			return PhotosSearch(null, "", 0, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, extras);
		}

		public Photos PhotosSearchText(string text)
		{
			return PhotosSearch(null, "", 0, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearchText(string text, int license)
		{
			return PhotosSearch(null, "", 0, text, DateTime.MinValue, DateTime.MinValue, license, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearchText(string text, int license, PhotoSearchExtras extras)
		{
			return PhotosSearch(null, "", 0, text, DateTime.MinValue, DateTime.MinValue, license, 0, 0, extras);
		}

		// PhotoSearch - tag array versions
		public Photos PhotosSearch(string[] tags, PhotoSearchExtras extras)
		{
			return PhotosSearch(null, tags, 0, "", DateTime.MinValue, DateTime.MinValue, 0, 0, 0, extras);
		}

		public Photos PhotosSearch(string[] tags)
		{
			return PhotosSearch(null, tags, 0, "", DateTime.MinValue, DateTime.MinValue, 0, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string[] tags, int license, PhotoSearchExtras extras)
		{
			return PhotosSearch(null, tags, 0, "", DateTime.MinValue, DateTime.MinValue, license, 0, 0, extras);
		}

		public Photos PhotosSearch(string[] tags, int license)
		{
			return PhotosSearch(null, tags, 0, "", DateTime.MinValue, DateTime.MinValue, license, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string[] tags, TagMode tagMode, string text, int perPage, int page)
		{
			return PhotosSearch(null, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, perPage, page, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string[] tags, TagMode tagMode, string text, int perPage, int page, PhotoSearchExtras extras)
		{
			return PhotosSearch(null, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, perPage, page, extras);
		}

		public Photos PhotosSearch(string[] tags, TagMode tagMode, string text)
		{
			return PhotosSearch(null, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string[] tags, TagMode tagMode, string text, PhotoSearchExtras extras)
		{
			return PhotosSearch(null, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, extras);
		}

		public Photos PhotosSearch(string userId, string[] tags)
		{
			return PhotosSearch(userId, tags, 0, "", DateTime.MinValue, DateTime.MinValue, 0, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string userId, string[] tags, PhotoSearchExtras extras)
		{
			return PhotosSearch(userId, tags, 0, "", DateTime.MinValue, DateTime.MinValue, 0, 0, 0, extras);
		}

		public Photos PhotosSearch(string userId, string[] tags, int license)
		{
			return PhotosSearch(userId, tags, 0, "", DateTime.MinValue, DateTime.MinValue, license, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string userId, string[] tags, int license, PhotoSearchExtras extras)
		{
			return PhotosSearch(userId, tags, 0, "", DateTime.MinValue, DateTime.MinValue, license, 0, 0, extras);
		}

		public Photos PhotosSearch(string userId, string[] tags, TagMode tagMode, string text, int perPage, int page)
		{
			return PhotosSearch(userId, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, perPage, page, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string userId, string[] tags, TagMode tagMode, string text, int perPage, int page, PhotoSearchExtras extras)
		{
			return PhotosSearch(userId, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, perPage, page, extras);
		}

		public Photos PhotosSearch(string userId, string[] tags, TagMode tagMode, string text)
		{
			return PhotosSearch(userId, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string userId, string[] tags, TagMode tagMode, string text, PhotoSearchExtras extras)
		{
			return PhotosSearch(userId, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, extras);
		}

		public Photos PhotosSearch(string userId, string[] tags, TagMode tagMode, string text, DateTime minUploadDate, DateTime maxUploadDate, int license, int perPage, int page, PhotoSearchExtras extras)
		{
			return PhotosSearch(userId, String.Join(",", tags), tagMode, text, minUploadDate, maxUploadDate, license, perPage, page, extras);
		}

		public Photos PhotosSearch(string userId, string[] tags, TagMode tagMode, string text, DateTime minUploadDate, DateTime maxUploadDate, int license, int perPage, int page)
		{
			return PhotosSearch(userId, String.Join(",", tags), tagMode, text, minUploadDate, maxUploadDate, license, perPage, page, PhotoSearchExtras.All);
		}

		// PhotoSearch - tags versions
		public Photos PhotosSearch(string tags, int license, PhotoSearchExtras extras)
		{
			return PhotosSearch(null, tags, 0, "", DateTime.MinValue, DateTime.MinValue, license, 0, 0, extras);
		}

		public Photos PhotosSearch(string tags, int license)
		{
			return PhotosSearch(null, tags, 0, "", DateTime.MinValue, DateTime.MinValue, license, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string tags, TagMode tagMode, string text, int perPage, int page)
		{
			return PhotosSearch(null, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, perPage, page, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string tags, TagMode tagMode, string text, int perPage, int page, PhotoSearchExtras extras)
		{
			return PhotosSearch(null, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, perPage, page, extras);
		}

		public Photos PhotosSearch(string tags, TagMode tagMode, string text)
		{
			return PhotosSearch(null, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string tags, TagMode tagMode, string text, PhotoSearchExtras extras)
		{
			return PhotosSearch(null, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, extras);
		}

		public Photos PhotosSearch(string userId, string tags)
		{
			return PhotosSearch(userId, tags, 0, "", DateTime.MinValue, DateTime.MinValue, 0, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string userId, string tags, PhotoSearchExtras extras)
		{
			return PhotosSearch(userId, tags, 0, "", DateTime.MinValue, DateTime.MinValue, 0, 0, 0, extras);
		}

		public Photos PhotosSearch(string userId, string tags, int license)
		{
			return PhotosSearch(userId, tags, 0, "", DateTime.MinValue, DateTime.MinValue, license, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string userId, string tags, int license, PhotoSearchExtras extras)
		{
			return PhotosSearch(userId, tags, 0, "", DateTime.MinValue, DateTime.MinValue, license, 0, 0, extras);
		}

		public Photos PhotosSearch(string userId, string tags, TagMode tagMode, string text, int perPage, int page)
		{
			return PhotosSearch(userId, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, perPage, page, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string userId, string tags, TagMode tagMode, string text, int perPage, int page, PhotoSearchExtras extras)
		{
			return PhotosSearch(userId, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, perPage, page, extras);
		}

		public Photos PhotosSearch(string userId, string tags, TagMode tagMode, string text)
		{
			return PhotosSearch(userId, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, PhotoSearchExtras.All);
		}

		public Photos PhotosSearch(string userId, string tags, TagMode tagMode, string text, PhotoSearchExtras extras)
		{
			return PhotosSearch(userId, tags, tagMode, text, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, extras);
		}

		// Actual PhotoSearch function
		public Photos PhotosSearch(string userId, string tags, TagMode tagMode, string text, DateTime minUploadDate, DateTime maxUploadDate, int license, int perPage, int page, PhotoSearchExtras extras)
		{
			PhotoSearchOptions options = new PhotoSearchOptions();
			options.UserId = userId;
			options.Tags = tags;
			options.TagMode = tagMode;
			options.Text = text;
			options.MinUploadDate = minUploadDate;
			options.MaxUploadDate = maxUploadDate;
			options.License = license;
			options.PerPage = perPage;
			options.Page = page;
			options.Extras = extras;

			return PhotoSearch(options);
		}

		public Photos PhotoSearch(PhotoSearchOptions options)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.search");
			if( options.UserId != null ) parameters.Add("user_id", options.UserId);
			if( options.Tags != null ) 
			{
				parameters.Add("tags", options.Tags);
				parameters.Add("tag_mode", (options.TagMode==TagMode.AllTags?"all":"any"));
			}
			if( options.MinUploadDate != DateTime.MinValue ) parameters.Add("min_upload_date", Utils.DateToUnixTimestamp(options.MinUploadDate).ToString());
			if( options.MaxUploadDate != DateTime.MinValue ) parameters.Add("max_upload_date", Utils.DateToUnixTimestamp(options.MaxUploadDate).ToString());
			if( options.License != 0 ) parameters.Add("license", options.License.ToString("d"));
			if( options.PerPage != 0 ) parameters.Add("per_page", options.PerPage.ToString());
			if( options.Page != 0 ) parameters.Add("page", options.Page.ToString());
			if( options.Extras != PhotoSearchExtras.None )
			{
				string val = "";
				if( (options.Extras & PhotoSearchExtras.DateTaken) == PhotoSearchExtras.DateTaken )
					val += "date_taken,";
				if( (options.Extras & PhotoSearchExtras.DateUploaded) == PhotoSearchExtras.DateUploaded )
					val += "date_upload,";
				if( (options.Extras & PhotoSearchExtras.IconServer) == PhotoSearchExtras.IconServer )
					val += "icon_server,";
				if( (options.Extras & PhotoSearchExtras.License) == PhotoSearchExtras.License )
					val += "license,";
				if( (options.Extras & PhotoSearchExtras.OwnerName) == PhotoSearchExtras.OwnerName )
					val += "owner_name";
				if( (options.Extras & PhotoSearchExtras.OriginalFormat) == PhotoSearchExtras.OriginalFormat )
					val += "original_format";
				parameters.Add("extras", val);
			}
			if( options.SortOrder != PhotoSearchSortOrder.None )
				parameters.Add("sort", options.SortOrderString);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photos;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public bool PhotosSetDates(string photoId, DateTime dateTaken, DateGranularity granularity)
		{
			return PhotosSetDates(photoId, DateTime.MinValue, dateTaken, granularity);
		}
		
		public bool PhotosSetDates(string photoId, DateTime datePosted)
		{
			return PhotosSetDates(photoId, datePosted, DateTime.MinValue, 0);
		}
		
		public bool PhotosSetDates(string photoId, DateTime datePosted, DateTime dateTaken, DateGranularity granularity)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.setDates");
			parameters.Add("photo_id", photoId);
			if( datePosted != DateTime.MinValue ) parameters.Add("date_posted", datePosted.ToString("yyyy-MM-dd hh:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo));
			if( dateTaken != DateTime.MinValue ) 
			{
				parameters.Add("date_taken", dateTaken.ToString("yyyy-MM-dd hh:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo));
				parameters.Add("date_taken_granularity", granularity.ToString("d"));
			}

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}

		/// <summary>
		/// Sets the title and description of the photograph.
		/// </summary>
		/// <param name="photoId">The numerical photoId of the photograph.</param>
		/// <param name="title">The new title of the photograph.</param>
		/// <param name="description">The new description of the photograph.</param>
		/// <returns>True when the operation is successful.</returns>
		/// <exception cref="FlickrException">Thrown when the photo id cannot be found.</exception>
		public bool PhotosSetMeta(string photoId, string title, string description)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.setMeta");
			parameters.Add("photo_id", photoId);
			parameters.Add("title", Utils.UrlEncode(title));
			parameters.Add("description", Utils.UrlEncode(description));

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}

		public bool PhotosSetPerms(string photoId, int isPublic, int isFriend, int isFamily, PermissionComment permComment, PermissionAddMeta permAddMeta)
		{
			return PhotosSetPerms(photoId, (isPublic==1), (isFriend==1), (isFamily==1), permComment, permAddMeta);
		}

		public bool PhotosSetPerms(string photoId, bool isPublic, bool isFriend, bool isFamily, PermissionComment permComment, PermissionAddMeta permAddMeta)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.setPerms");
			parameters.Add("photo_id", photoId);
			parameters.Add("is_public", (isPublic?"1":"0"));
			parameters.Add("is_friend", (isFriend?"1":"0"));
			parameters.Add("is_family", (isFamily?"1":"0"));
			parameters.Add("perm_comment", permComment.ToString("d"));
			parameters.Add("perm_addmeta", permAddMeta.ToString("d"));

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}

		public bool PhotosSetTags(string photoId, string[] tags)
		{
			string s = string.Join(",", tags);
			return PhotosSetTags(photoId, s);
		}
			
		public bool PhotosSetTags(string photoId, string tags)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.setTags");
			parameters.Add("photo_id", photoId);
			parameters.Add("tags", tags);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}
		#endregion

		#region [ Photosets ]
		public void PhotosetsAddPhoto(string photosetId, string photoId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photosets.addPhoto");
			parameters.Add("photoset_id", photosetId);
			parameters.Add("photo_id", photoId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public Photoset PhotosetsCreate(string title, string primaryPhotoId)
		{
			return PhotosetsCreate(title, null, primaryPhotoId);
		}

		public Photoset PhotosetsCreate(string title, string description, string primaryPhotoId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photosets.create");
			parameters.Add("title", Utils.UrlEncode(title));
			parameters.Add("primary_photo_id", primaryPhotoId);
			parameters.Add("description", Utils.UrlEncode(description));

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photoset;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}

		public bool PhotosetsDelete(string photosetId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photosets.delete");
			parameters.Add("photoset_id", photosetId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}

		public bool PhotosetsEditMeta(string photosetId, string title, string description)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photosets.editMeta");
			parameters.Add("photoset_id", photosetId);
			parameters.Add("title", Utils.UrlEncode(title));
			parameters.Add("description", Utils.UrlEncode(description));

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}

		public bool PhotosetsEditPhotos(string photosetId, string primaryPhotoId, string[] photoIds)
		{
			return PhotosetsEditPhotos(photosetId, primaryPhotoId, string.Join(",", photoIds));
		}

		public bool PhotosetsEditPhotos(string photosetId, string primaryPhotoId, string photoIds)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photosets.editPhotos");
			parameters.Add("photoset_id", photosetId);
			parameters.Add("primary_photo_id", primaryPhotoId);
			parameters.Add("photo_ids", photoIds);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}

		/// <summary>
		/// Gets the context of the specified photo within the photoset.
		/// </summary>
		/// <param name="photoId">The photo id of the photo in the set.</param>
		/// <param name="photosetId">The id of the set.</param>
		/// <returns><see cref="Context"/> of the specified photo.</returns>
		public Context PhotosetsGetContext(string photoId, string photosetId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photosets.getContext");
			parameters.Add("photo_id", photoId);
			parameters.Add("photoset_id", photosetId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				Context c = new Context();
				c.Count = response.ContextCount.Count;
				c.NextPhoto = response.ContextNextPhoto;
				c.PreviousPhoto = response.ContextPrevPhoto;

				return c;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public Photoset PhotosetsGetInfo(string photosetId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photosets.getInfo");
			parameters.Add("photoset_id", photosetId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photoset;
			}
			else
			{
				throw new FlickrException(response.Error);
			}

		}

		public Photosets PhotosetsGetList()
		{
			return PhotosetsGetList(null);
		}

		public Photosets PhotosetsGetList(string userId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photosets.getList");
			if( userId != null ) parameters.Add("user_id", userId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photosets;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public Photoset PhotosetsGetPhotos(string photosetId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photosets.getPhotos");
			parameters.Add("photoset_id", photosetId);
			parameters.Add("extras", "owner");

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Photoset;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public bool PhotosetsOrderSets(string[] photosetIds)
		{
			return PhotosetsOrderSets(string.Join(",", photosetIds));
		}

		public bool PhotosetsOrderSets(string photosetIds)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photosets.orderSets");
			parameters.Add("photosetIds", photosetIds);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return true;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public void PhotosetsDeletePhoto(string photosetId, string photoId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photosets.deletePhoto");
			parameters.Add("photoset_id", photosetId);
			parameters.Add("photo_id", photoId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}
		#endregion

		#region [ Tags ]
		public PhotoInfo TagsGetListPhoto(string photoId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.notes.add");
			parameters.Add("api_key", _apiKey);
			parameters.Add("photo_id", photoId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.PhotoInfo;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public WhoInfo TagsGetListUser()
		{
			return TagsGetListUser(null);
		}

		public WhoInfo TagsGetListUser(string userId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.tags.getListUser");
			if( userId != null ) parameters.Add("user_id", userId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Who;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public WhoInfo TagsGetListUserPopular()
		{
			return TagsGetListUserPopular(null, 0);
		}
			
		public WhoInfo TagsGetListUserPopular(int count)
		{
			return TagsGetListUserPopular(null, count);
		}
			
		public WhoInfo TagsGetListUserPopular(string userId)
		{
			return TagsGetListUserPopular(userId, 0);
		}
			
		public WhoInfo TagsGetListUserPopular(string userId, long count)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.photos.notes.add");
			if( userId != null ) parameters.Add("user_id", userId);
			if( count > 0 ) parameters.Add("count", count.ToString());

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Who;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public PhotoInfoTags TagsGetRelated(string tag)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.tags.getRelated");
			parameters.Add("api_key", _apiKey);
			parameters.Add("tag", tag);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.Tags;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		#endregion

		#region [ Tests ]
		/// <summary>
		/// Runs the flickr.test.echo method and returned an array of <see cref="XmlElement"/> items.
		/// </summary>
		/// <param name="echoParameter">The parameter to pass to the method.</param>
		/// <param name="echoValue">The value to pass to the method with the parameter.</param>
		/// <returns>An array of <see cref="XmlElement"/> items.</returns>
		/// <remarks>
		/// The APi Key has been removed from the returned array and will not be shown.
		/// </remarks>
		/// <example>
		/// <code>
		/// XmlElement[] elements = flickr.TestEcho("&amp;param=value");
		/// foreach(XmlElement element in elements)
		/// {
		///		if( element.Name = "method" )
		///			Console.WriteLine("Method = " + element.InnerXml);
		///		if( element.Name = "param" )
		///			Console.WriteLine("Param = " + element.InnerXml);
		/// }
		/// </code>
		/// </example>
		public XmlElement[] TestEcho(string echoParameter, string echoValue)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.test.echo");
			parameters.Add("api_key", _apiKey);
			if( echoParameter != null && echoParameter.Length > 0 )
			{
				parameters.Add(echoParameter, echoValue);
			}

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				// Remove the api_key element from the array.
				XmlElement[] elements = new XmlElement[response.AllElements.Length - 1];
				int c = 0;
				foreach(XmlElement element in response.AllElements)
				{
					if(element.Name != "api_key" )
						elements[c++] = element;
				}
				return elements;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		/// <summary>
		/// Test the logged in state of the current Filckr object.
		/// </summary>
		/// <returns>The <see cref="User"/> object containing the username and userid of the current user.</returns>
		public User TestLogin()
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.test.login");

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.User;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}
		#endregion

		#region [ Urls ]
		public GroupInfo UrlsGetGroup(string groupId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.urls.getGroup");
			parameters.Add("api_key", _apiKey);
			parameters.Add("group_id", groupId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.GroupInfo;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public User UrlsGetUserPhotos(string userId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.urls.getUserPhotos");
			parameters.Add("api_key", _apiKey);
			parameters.Add("user_id", userId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.User;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}
		
		public User UrlsGetUserProfile(string userId)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.urls.getUserProfile");
			parameters.Add("api_key", _apiKey);
			parameters.Add("user_id", userId);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.User;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public GroupInfo UrlsLookupGroup(string urlToFind)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.urls.lookupGroup");
			parameters.Add("api_key", _apiKey);
			parameters.Add("url", urlToFind);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.GroupInfo;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}

		public User UrlsLookupUser(string urlToFind)
		{
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("method", "flickr.urls.lookupUser");
			parameters.Add("api_key", _apiKey);
			parameters.Add("url", urlToFind);

			FlickrNet.Response response = GetResponseCache(parameters);

			if( response.Status == ResponseStatus.OK )
			{
				return response.User;
			}
			else
			{
				throw new FlickrException(response.Error);
			}
		}
		#endregion

		/// <summary>
		/// No longer supported.
		/// </summary>
		/// <param name="email"></param>
		/// <param name="password"></param>
		/// <returns>False.</returns>
		[Obsolete("Authentication test no longer supported", true)]
		public static bool AuthenticationTest(string email, string password)
		{
			return false;
		}
	}

	/// <summary>
	/// Used to specify where all tags must be matched or any tag to be matched.
	/// </summary>
	public enum TagMode
	{
		/// <summary>
		/// Any tag must match, but not all.
		/// </summary>
		AnyTag,
		/// <summary>
		/// All tags must be found.
		/// </summary>
		AllTags
	}

}
