using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections;
using System.Text.RegularExpressions;

namespace FlickrNet
{
	/// <summary>
	/// Contains details of image held with the Flickr.Net cache.
	/// </summary>
	[Serializable]
	public class PictureCacheItem
	{
		#region [ Internal Variables ]
		internal string url;
		internal DateTime creationTime;
		internal string filename;
		internal long fileSize;
		#endregion

		#region [ Public Properties ]
		/// <summary>
		/// The URL of the original image on Flickr.
		/// </summary>
		public string Url { get { return url; } }
		/// <summary>
		/// The <see cref="DateTime"/> that the cache item was created.
		/// </summary>
		public DateTime CreationTime { get { return creationTime; } }
		/// <summary>
		/// The filesize in bytes of the image.
		/// </summary>
		public long FileSize { get { return fileSize; } }
		/// <summary>
		/// The Flickr photo id of the image.
		/// </summary>
		public string PhotoId
		{
			get 
			{
				if( url == null ) 
					return null;
				else
					return Regex.Match(url, "(?<=/)[0-9]+").Value;
			}
		}
		#endregion

	}

	/// <summary>
	/// Internal Cache class
	/// </summary>
	internal sealed class Cache
	{
		[Serializable]
		internal struct ResponseCacheItem
		{
			private string url;
			private string response;
			private DateTime creationTime;

			public string Url { get { return url; } set { url = value; } }
			public string Response { get { return response; } set { response = value; } }
			public DateTime CreationTime { get { return creationTime; } set { creationTime = value; } }
		}

		private Cache()
		{
		}

		static Cache()
		{
		
			LoadCache();

			_timer = new System.Timers.Timer();
			_timer.Elapsed += new System.Timers.ElapsedEventHandler(_timer_Elapsed);
			_timer.Interval = 10 * 60 * 1000; // 10 minutes
			_timer.Start();
		}

		private static System.Timers.Timer _timer = null;

		internal static void DeleteOldest()
		{
			if( DownloadCache.Count == 0 ) return;

			PictureCacheItem itm = (PictureCacheItem)DownloadCache.GetByIndex(0);
			System.Diagnostics.Debug.WriteLine("Delete url: " + itm.url);
			CacheSize -= itm.fileSize;
			if( File.Exists(itm.filename) ) File.Delete(itm.filename);
			
			DownloadCache.RemoveAt(0);

			return;
		}

		internal static long CacheSizeLimit
		{
			get 
			{
				if( CacheSettings.ContainsKey("SizeLimit") )
					return (long)CacheSettings["SizeLimit"];
				else
					return 50 * 1024 * 1024;
			}
			set 
			{ 
				if( CacheSettings.ContainsKey("SizeLimit") )
					CacheSettings["SizeLimit"] = value;
				else
					CacheSettings.Add("SizeLimit", value);
			}
		}

		internal static long CacheSize
		{
			get 
			{
				if( CacheSettings.ContainsKey("CurrentSize") )
					return (long)CacheSettings["CurrentSize"];
				else
					return 0;
			}
			set 
			{ 
				if( CacheSettings.ContainsKey("CurrentSize") )
					CacheSettings["CurrentSize"] = value;
				else
					CacheSettings.Add("CurrentSize", value);
			}
		}


		// Default cache timeout is 1 hour
		private static TimeSpan _cachetimeout = new TimeSpan(0, 1, 0, 0, 0);

		public static TimeSpan CacheTimeout
		{
			get { return _cachetimeout; }
			set { _cachetimeout = value; }
		}
		
		private static object _lockObject = new object();

		internal static SortedList ResponseCache = new SortedList();
		internal static SortedList DownloadCache = new SortedList();
		private static Hashtable CacheSettings = new Hashtable();

		private static void SaveCache()
		{
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

			System.IO.Stream stream;

			lock(ResponseCache.SyncRoot)
			{
				stream = Utils.GetApplicationDataWriteStream("responseCache.bin");
				try
				{
					formatter.Serialize(stream, ResponseCache);
				}
				finally
				{
					stream.Close();
				}
			}

			lock(DownloadCache.SyncRoot)
			{
				stream = Utils.GetApplicationDataWriteStream("downloadCache.bin");
				try
				{
					formatter.Serialize(stream, DownloadCache);
				}
				finally
				{
					stream.Close();
				}
			}

			lock(CacheSettings.SyncRoot)
			{
				stream = Utils.GetApplicationDataWriteStream("cacheSettings.bin");
				try
				{
					formatter.Serialize(stream, CacheSettings);
				}
				finally
				{
					stream.Close();
				}
			}
		}

		private static void LoadCache()
		{
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

			System.IO.Stream stream;

			stream = Utils.GetApplicationDataReadStream("cacheSettings.bin");
			if( stream == null )
			{
				CacheSettings = new Hashtable();
				return;
			}
			try
			{
				CacheSettings = (Hashtable)formatter.Deserialize(stream);
			}
			catch
			{
				CacheSettings = new Hashtable();
			}
			finally
			{
				stream.Close();
			}

			stream = Utils.GetApplicationDataReadStream("responseCache.bin");
			if( stream == null )
			{
				ResponseCache = new SortedList();
				return;
			}
			try
			{
				ResponseCache = (SortedList)formatter.Deserialize(stream);
			}
			catch
			{
				ResponseCache = new SortedList();
			}
			finally
			{
				stream.Close();
			}

			stream = Utils.GetApplicationDataReadStream("downloadCache.bin");
			if( stream == null )
			{
				DownloadCache = new SortedList();
				return;
			}
			try
			{
				DownloadCache = (SortedList)formatter.Deserialize(stream);
			}
			catch
			{
				DownloadCache = new SortedList();
			}
			finally
			{
				stream.Close();
			}

			return;
		}

		private static void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			ProcessSave();
		}

		private static void ProcessSave()
		{
//			foreach(Object o in ResponseCache.Values)
//			{
//				ResponseCacheItem item = (ResponseCacheItem)o;
//				if( item.creationTime + CacheTimeout > DateTime.Now )
//				{
//					ResponseCache.Remove(item.url);
//				}
//			}
//
//			foreach(Object o in DownloadCache.Values)
//			{
//				PictureCacheItem item = (PictureCacheItem)o;
//				if( item.creationTime + CacheTimeout > DateTime.Now )
//				{
//					DownloadCache.Remove(item.url);
//				}
//			}

			SaveCache();
		}

		internal static void ForceSave()
		{
			ProcessSave();
		}

		internal static void FlushCache(string url)
		{
			if( ResponseCache.ContainsKey(url) )
				ResponseCache.Remove(url);
			if( DownloadCache.ContainsKey(url) )
			{

				PictureCacheItem itm = (PictureCacheItem)DownloadCache[url];
				Utils.DeleteISFFile(itm.filename);
				DownloadCache.Remove(url);
			}
		}

		internal static void FlushCache()
		{
			ResponseCache.Clear();
			foreach(Object o in DownloadCache.Values)
			{
				PictureCacheItem itm = (PictureCacheItem)o;
				Utils.DeleteISFFile(itm.filename);
			}
			DownloadCache.Clear();
			CacheSize = 0;
			ForceSave();
		}

	}
}
