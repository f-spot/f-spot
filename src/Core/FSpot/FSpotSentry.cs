//
// FSpotSentry.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FSpot.Database;
using FSpot.Resources.Lang;
using FSpot.Settings;

using Sentry;
using Sentry.Protocol;

namespace FSpot
{
	// Shared Sentry options for every desktop client. Reporting follows the
	// preference, which defaults on. FSPOT_SENTRY=0 forces it off. Tests call
	// Configure and never Init, so CI cannot ship events to the project DSN.
	public static class FSpotSentry
	{
		public const string GtkClient = "gtk";
		public const string AvaloniaClient = "avalonia";
		public const string KillSwitchVariable = "FSPOT_SENTRY";

		public const string EnvironmentName =
#if DEBUG
			"development";
#else
			"production";
#endif

		const string ClientTag = "client";
		const string Dsn =
			"https://6cdc2b33312541c5ac9c3a61e1c960d0@o4509369388761088.ingest.us.sentry.io/4509568228851712";

		static TagStore catalog;

		// The default reads the catalog at send time. Tests may replace this
		// delegate; the clients call UseTagCatalog and leave it in place.
		public static Func<IEnumerable<string>> TagNames = ReadCatalogTags;

		public static void UseTagCatalog (TagStore tags)
		{
			catalog = tags;
		}

		static IEnumerable<string> ReadCatalogTags ()
		{
			TagStore tags = catalog;
			if (tags == null)
				return Array.Empty<string> ();
			try {
				return tags.Names ();
			} catch (Exception ex) {
				Console.Error.WriteLine (ex);
				return Array.Empty<string> ();
			}
		}

		public static bool IsKillSwitchOff (string value)
		{
			if (string.IsNullOrWhiteSpace (value))
				return false;
			return value == "0"
				|| value.Equals ("false", StringComparison.OrdinalIgnoreCase)
				|| value.Equals ("off", StringComparison.OrdinalIgnoreCase)
				|| value.Equals ("no", StringComparison.OrdinalIgnoreCase);
		}

		public static string ReportingDescription ()
			=> IsRequested () ? Strings.CrashReportsStayOffUntilEnabled : Strings.CrashReportsAreOff;

		public static bool IsRequested ()
		{
			if (IsKillSwitchOff (Environment.GetEnvironmentVariable (KillSwitchVariable)))
				return false;
			try {
				return LibraryPreferences.SendCrashReports;
			} catch (Exception ex) {
				Console.Error.WriteLine (ex);
				return false;
			}
		}

		public static IDisposable TryInit (string client)
		{
			if (!IsRequested ())
				return Disabled.Instance;
			return SentrySdk.Init (o => Configure (o, client));
		}

		public static void Apply (string client)
		{
			if (IsRequested ()) {
				if (!SentrySdk.IsEnabled)
					SentrySdk.Init (o => Configure (o, client));
				return;
			}

			if (SentrySdk.IsEnabled)
				SentrySdk.Close ();
		}

		public static void Configure (SentryOptions options, string client)
		{
			if (options == null)
				throw new ArgumentNullException (nameof (options));
			if (string.IsNullOrEmpty (client))
				throw new ArgumentException ("client is required", nameof (client));

			options.Dsn = Dsn;
			options.Release = $"f-spot@{FSpotConfiguration.Version}";
			options.Environment = EnvironmentName;
			options.IsGlobalModeEnabled = true;
			options.SendDefaultPii = false;
			options.DefaultTags[ClientTag] = client;
			options.SetBeforeSend ((evt, _) => ScrubEvent (evt));
			options.SetBeforeBreadcrumb ((crumb, _) => ScrubBreadcrumb (crumb));
		}

		public static string Scrub (string text)
		{
			if (string.IsNullOrEmpty (text))
				return text;

			string home = FSpotConfiguration.HomeDirectory;
			if (!string.IsNullOrEmpty (home)) {
				text = ReplacePath (text, home);
				string slashed = home.Replace ('\\', '/');
				if (!string.Equals (slashed, home, StringComparison.Ordinal))
					text = ReplacePath (text, slashed);
				string backed = home.Replace ('/', '\\');
				if (!string.Equals (backed, home, StringComparison.Ordinal))
					text = ReplacePath (text, backed);
			}

			IEnumerable<string> tags;
			try {
				tags = TagNames?.Invoke () ?? Array.Empty<string> ();
			} catch (Exception ex) {
				Console.Error.WriteLine (ex);
				tags = Array.Empty<string> ();
			}

			foreach (string tag in tags.Where (name => !string.IsNullOrWhiteSpace (name)).OrderByDescending (name => name.Length))
				text = ReplaceOrdinal (text, tag, "[tag]");

			return ScrubPaths (text);
		}

		// The old regex stopped at the first space, so "/Volumes/Photo Disk/…"
		// leaked "Disk/…". A path keeps going through spaces while each next
		// word still contains a separator or a dot, and stops before a word
		// such as "now".
		static string ScrubPaths (string text)
		{
			var builder = new System.Text.StringBuilder ();
			int index = 0;
			while (index < text.Length) {
				int start = FindPathStart (text, index);
				if (start < 0) {
					builder.Append (text, index, text.Length - index);
					break;
				}

				builder.Append (text, index, start - index);
				builder.Append ("[path]");
				index = ConsumePath (text, start);
			}

			return builder.ToString ();
		}

		static int FindPathStart (string text, int from)
		{
			for (int index = from; index < text.Length; index++) {
				if (IsPathStart (text, index))
					return index;
			}

			return -1;
		}

		static bool IsPathStart (string text, int index)
		{
			if (StartsWithFileUri (text, index) || StartsWithUnc (text, index) || StartsWithDrive (text, index))
				return true;
			if (text[index] == '/' && (index == 0 || char.IsWhiteSpace (text[index - 1])))
				return true;
			if (text[index] == '~' && index + 1 < text.Length && (text[index + 1] == '/' || text[index + 1] == '\\') && IsBoundary (text, index))
				return true;
			return IsHfsPath (text, index);
		}

		static bool StartsWithFileUri (string text, int index)
			=> IsBoundary (text, index) && StartsWithOrdinalIgnore (text, index, "file://");

		static bool StartsWithUnc (string text, int index)
		{
			if (index + 1 >= text.Length)
				return false;
			if (text[index] == '\\' && text[index + 1] == '\\')
				return true;
			return text[index] == '/' && text[index + 1] == '/'
				&& (index == 0 || char.IsWhiteSpace (text[index - 1]));
		}

		static bool StartsWithDrive (string text, int index)
		{
			if (!IsBoundary (text, index) || index + 2 >= text.Length || !char.IsLetter (text[index]) || text[index + 1] != ':')
				return false;
			char slash = text[index + 2];
			return slash == '\\' || slash == '/';
		}

		// osascript errors look like "Macintosh HD:Users:alice:…": a volume
		// of one or two words, then at least two colons. A clock ("12:30:00")
		// and an http URL are not volumes.
		static bool IsHfsPath (string text, int index)
		{
			if (!IsBoundary (text, index) || index >= text.Length || !IsVolumeChar (text[index]))
				return false;

			int cursor = EndOfVolumeWord (text, index);
			if (cursor < text.Length && text[cursor] == ' ') {
				int afterSpace = cursor + 1;
				if (afterSpace < text.Length && IsVolumeChar (text[afterSpace])) {
					int secondWord = EndOfVolumeWord (text, afterSpace);
					if (secondWord < text.Length && text[secondWord] == ':')
						cursor = secondWord;
				}
			}

			if (cursor >= text.Length || text[cursor] != ':')
				return false;

			int end = cursor;
			while (end < text.Length && !char.IsWhiteSpace (text[end]))
				end++;

			int colons = 0;
			bool letter = false;
			for (int scan = index; scan < end; scan++) {
				if (text[scan] == ':' && scan + 2 < end && text[scan + 1] == '/' && text[scan + 2] == '/')
					return false;
				if (text[scan] == ':')
					colons++;
				else if (char.IsLetter (text[scan]))
					letter = true;
			}

			return colons >= 2 && letter;
		}

		static int ConsumePath (string text, int start)
		{
			int end = start;
			while (end < text.Length && !char.IsWhiteSpace (text[end]))
				end++;

			while (end < text.Length && char.IsWhiteSpace (text[end])) {
				int token = end;
				while (token < text.Length && char.IsWhiteSpace (text[token]))
					token++;
				if (token >= text.Length || !LooksLikePathContinuation (text, token))
					break;

				end = token;
				while (end < text.Length && !char.IsWhiteSpace (text[end]))
					end++;
			}

			return end;
		}

		static bool LooksLikePathContinuation (string text, int start)
		{
			for (int index = start; index < text.Length && !char.IsWhiteSpace (text[index]); index++) {
				char c = text[index];
				if (c == '/' || c == '\\' || c == '.')
					return true;
			}

			return false;
		}

		static int EndOfVolumeWord (string text, int index)
		{
			while (index < text.Length && IsVolumeChar (text[index]))
				index++;
			return index;
		}

		static bool IsVolumeChar (char c)
			=> char.IsLetterOrDigit (c) || c == '-' || c == '_' || c == '\'';

		static bool IsBoundary (string text, int index)
			=> index == 0 || !char.IsLetterOrDigit (text[index - 1]);

		static bool StartsWithOrdinalIgnore (string text, int index, string prefix)
		{
			if (index + prefix.Length > text.Length)
				return false;
			return string.Compare (text, index, prefix, 0, prefix.Length, StringComparison.OrdinalIgnoreCase) == 0;
		}

		public static SentryEvent ScrubEvent (SentryEvent evt)
		{
			if (evt == null)
				return null;
			if (evt.Message != null) {
				evt.Message = new SentryMessage {
					Message = Scrub (evt.Message.Message),
					Formatted = Scrub (evt.Message.Formatted)
				};
			}

			if (evt.SentryExceptions != null) {
				foreach (SentryException exception in evt.SentryExceptions) {
					exception.Value = Scrub (exception.Value);
					ScrubStack (exception.Stacktrace);
				}
			}

			if (evt.SentryThreads != null) {
				foreach (SentryThread thread in evt.SentryThreads)
					ScrubStack (thread.Stacktrace);
			}

			// Snapshot first. SetExtra mutates the map the Serilog sink fills
			// with {Path} and {File}, and walking it live would skip entries.
			if (evt.Extra != null) {
				foreach (KeyValuePair<string, object> pair in evt.Extra.ToArray ())
					evt.SetExtra (pair.Key, ScrubStructured (pair.Key, pair.Value));
			}

			if (evt.Contexts != null) {
				foreach (string key in evt.Contexts.Keys.ToArray ())
					evt.Contexts[key] = ScrubStructured (key, evt.Contexts[key]);
			}

			return evt;
		}

		public static Breadcrumb ScrubBreadcrumb (Breadcrumb crumb)
		{
			if (crumb == null)
				return null;
			return new Breadcrumb (Scrub (crumb.Message), crumb.Type, ScrubBreadcrumbData (crumb.Data), crumb.Category, crumb.Level);
		}

		static void ScrubStack (SentryStackTrace stack)
		{
			if (stack == null || stack.Frames == null)
				return;
			foreach (SentryStackFrame frame in stack.Frames) {
				if (frame == null)
					continue;
				frame.FileName = Scrub (frame.FileName);
				frame.AbsolutePath = Scrub (frame.AbsolutePath);
				frame.Module = Scrub (frame.Module);
			}
		}

		static IReadOnlyDictionary<string, string> ScrubBreadcrumbData (IReadOnlyDictionary<string, string> data)
		{
			if (data == null || data.Count == 0)
				return data;

			var copy = new Dictionary<string, string> (data.Count);
			foreach (KeyValuePair<string, string> pair in data)
				copy[pair.Key] = IsPathKey (pair.Key) ? "[path]" : Scrub (pair.Value);
			return copy;
		}

		static object ScrubStructured (string key, object value)
		{
			if (IsPathKey (key))
				return "[path]";
			if (value is string text)
				return Scrub (text);
			if (value is IDictionary<string, object> children) {
				foreach (string child in children.Keys.ToArray ())
					children[child] = ScrubStructured (child, children[child]);
				return children;
			}

			return value;
		}

		// "profile" contains the letters of "file" but is not a path field.
		// Split camel case and separators, then match a whole word.
		static bool IsPathKey (string key)
		{
			if (string.IsNullOrEmpty (key))
				return false;

			string spaced = System.Text.RegularExpressions.Regex.Replace (key, "([a-z0-9])([A-Z])", "$1 $2");
			spaced = System.Text.RegularExpressions.Regex.Replace (spaced, "[^A-Za-z0-9]+", " ");
			foreach (string part in spaced.Split (new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
				if (part.Equals ("path", StringComparison.OrdinalIgnoreCase)
					|| part.Equals ("file", StringComparison.OrdinalIgnoreCase)
					|| part.Equals ("filename", StringComparison.OrdinalIgnoreCase)
					|| part.Equals ("uri", StringComparison.OrdinalIgnoreCase)
					|| part.Equals ("url", StringComparison.OrdinalIgnoreCase)
					|| part.Equals ("folder", StringComparison.OrdinalIgnoreCase)
					|| part.Equals ("directory", StringComparison.OrdinalIgnoreCase))
					return true;
			}

			string flat = System.Text.RegularExpressions.Regex.Replace (key, "[^A-Za-z0-9]", string.Empty);
			return flat.Equals ("filepath", StringComparison.OrdinalIgnoreCase)
				|| flat.Equals ("fileuri", StringComparison.OrdinalIgnoreCase)
				|| flat.Equals ("folderpath", StringComparison.OrdinalIgnoreCase);
		}

		static string ReplacePath (string text, string path)
			=> ReplaceOrdinal (text, path, "~");

		static string ReplaceOrdinal (string text, string oldValue, string newValue)
		{
			if (string.IsNullOrEmpty (text) || string.IsNullOrEmpty (oldValue))
				return text;
			int index = text.IndexOf (oldValue, StringComparison.Ordinal);
			if (index < 0)
				return text;
			var builder = new System.Text.StringBuilder ();
			int start = 0;
			while (index >= 0) {
				builder.Append (text, start, index - start);
				builder.Append (newValue);
				start = index + oldValue.Length;
				index = text.IndexOf (oldValue, start, StringComparison.Ordinal);
			}
			builder.Append (text, start, text.Length - start);
			return builder.ToString ();
		}

		sealed class Disabled : IDisposable
		{
			public static readonly Disabled Instance = new Disabled ();
			public void Dispose () { }
		}
	}
}
