// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

using FSpot.Core;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Utils;

using Hyena;

namespace FSpot
{
	public sealed class UfrawLaunch
	{
		public UfrawLaunch (string executable, string arguments, string developedPath, string rawPath)
		{
			Executable = executable ?? string.Empty;
			Arguments = arguments ?? string.Empty;
			DevelopedPath = developedPath ?? string.Empty;
			RawPath = rawPath ?? string.Empty;
		}

		public string Executable { get; }
		public string Arguments { get; }
		public string DevelopedPath { get; }
		public string RawPath { get; }
	}

	// Builds the UFRaw command and records the JPEG it writes. The caller starts
	// the process. The developed path is a real file path, including a '#' in the name.
	public static class UfrawDevelop
	{
		public const string QualityKey = Preferences.ExtensionKey + "DevelopInUfraw/JpegQuality";
		public const string ArgumentsKey = Preferences.ExtensionKey + "DevelopInUfraw/Arguments";
		public const string BatchArgumentsKey = Preferences.ExtensionKey + "DevelopInUfraw/BatchArguments";
		public const int DefaultQuality = 98;

		public static bool Develop (Photo photo, bool batch, int quality, string extraArguments, string baseDirectory, Func<SafeUri, bool> isRaw, Func<UfrawLaunch, int> launch)
		{
			if (photo == null || isRaw == null || launch == null)
				return false;
			PhotoVersion raw = photo.GetVersion (Photo.OriginalVersionId);
			if (raw?.Uri == null || !isRaw (raw.Uri))
				return false;

			if (quality < 1 || quality > 100)
				quality = DefaultQuality;

			string name = VersionName (photo);
			string developedPath = DevelopedJpegPath (raw.Uri, photo.Name, name);
			string conf = ConfPath (batch, baseDirectory, raw.Uri.AbsolutePath);
			string arguments = BuildArguments (extraArguments, quality, conf, developedPath, raw.Uri.AbsolutePath);
			// A cancelled or missing ufraw must not adopt a JPEG left by an earlier develop.
			int exitCode = launch (new UfrawLaunch (batch ? "ufraw-batch" : "ufraw", arguments, developedPath, raw.Uri.AbsolutePath));
			if (exitCode != 0 || !File.Exists (developedPath))
				return false;

			MoveSidecar (developedPath, raw.Uri.AbsolutePath, baseDirectory);
			SafeUri developed = SafeUri.FromPath (developedPath);
			photo.DefaultVersionId = photo.AddVersion (developed.GetBaseUri (), developed.GetFilename (), name, true);
			photo.Changes.DataChanged = true;
			return true;
		}

		public static string DevelopedJpegPath (SafeUri original, string photoName, string versionName)
		{
			string directory = Path.GetDirectoryName (original.AbsolutePath) ?? string.Empty;
			string file = Path.GetFileNameWithoutExtension (photoName) + " (" + versionName + ").jpg";
			return Path.Combine (directory, file);
		}

		static string VersionName (Photo photo)
		{
			for (int i = 1; ; i++) {
				string name = i <= 1 ? Strings.DevelopedInUFRaw : string.Format (Strings.DevelopedInUFRawX, i);
				if (!photo.VersionNameExists (name))
					return name;
			}
		}

		static string ConfPath (bool batch, string baseDirectory, string rawPath)
		{
			if (batch) {
				if (string.IsNullOrEmpty (baseDirectory))
					return null;
				string saved = Path.Combine (baseDirectory, "batch.ufraw");
				return File.Exists (saved) ? saved : null;
			}
			string beside = Path.ChangeExtension (rawPath, ".ufraw");
			return File.Exists (beside) ? beside : null;
		}

		static string BuildArguments (string extra, int quality, string conf, string developedPath, string rawPath)
		{
			var builder = new StringBuilder ();
			if (!string.IsNullOrWhiteSpace (extra))
				builder.Append (extra.Trim ()).Append (' ');
			builder.Append ("--overwrite --create-id=also --compression=").Append (quality).Append (" --out-type=jpeg");
			if (!string.IsNullOrEmpty (conf))
				builder.Append (" --conf=").Append (Quote (conf));
			builder.Append (" --output=").Append (Quote (developedPath));
			builder.Append (' ').Append (Quote (rawPath));
			return builder.ToString ();
		}

		static string Quote (string value)
		{
			value ??= string.Empty;
			// CreateProcess treats a backslash as literal unless it escapes a quote.
			// Doubling every one makes Windows look for a path that does not exist.
			if (Path.DirectorySeparatorChar == '\\')
				return QuoteForWindows (value);
			return "\"" + value.Replace ("\\", "\\\\").Replace ("\"", "\\\"") + "\"";
		}

		internal static string QuoteForWindows (string value)
		{
			value ??= string.Empty;
			var builder = new StringBuilder (value.Length + 2);
			builder.Append ('"');
			int slashes = 0;
			foreach (char c in value) {
				if (c == '\\') {
					slashes++;
					continue;
				}
				if (c == '"') {
					builder.Append ('\\', (slashes * 2) + 1);
					builder.Append ('"');
					slashes = 0;
					continue;
				}
				if (slashes > 0) {
					builder.Append ('\\', slashes);
					slashes = 0;
				}
				builder.Append (c);
			}
			if (slashes > 0)
				builder.Append ('\\', slashes * 2);
			builder.Append ('"');
			return builder.ToString ();
		}

		static void MoveSidecar (string developedPath, string rawPath, string baseDirectory)
		{
			string developedSidecar = Path.ChangeExtension (developedPath, ".ufraw");
			if (!File.Exists (developedSidecar))
				return;
			if (!string.IsNullOrEmpty (baseDirectory)) {
				Directory.CreateDirectory (baseDirectory);
				string batchPath = Path.Combine (baseDirectory, "batch.ufraw");
				if (File.Exists (batchPath))
					File.Delete (batchPath);
				File.Copy (developedSidecar, batchPath);
			}
			string rawSidecar = Path.ChangeExtension (rawPath, ".ufraw");
			if (!string.Equals (developedSidecar, rawSidecar, StringComparison.Ordinal)) {
				if (File.Exists (rawSidecar))
					File.Delete (rawSidecar);
				File.Move (developedSidecar, rawSidecar);
			}
		}
	}
}
