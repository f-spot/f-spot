//
// FSpotTabbloExport.ApplicationCentricCertificatePolicy
//
// Authors:
//	Wojciech Dzierzanowski (wojciech.dzierzanowski@gmail.com)
//
// (C) Copyright 2008 Wojciech Dzierzanowski
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;

using FSpot.Utils;

namespace FSpotTabbloExport {

	class ApplicationCentricCertificatePolicy : ICertificatePolicy {

		protected enum Decision {
			DontTrust,
			TrustOnce,
			TrustAlways
		};

		private Dictionary<string, int> cert_hashes;

		private static readonly IsolatedStorageFile isolated_store =
				IsolatedStorageFile.GetUserStoreForAssembly ();

		private const string StoreName = "cert_hashes";


		public bool CheckValidationResult (ServicePoint service_point,
		                                   X509Certificate certificate,
		                                   WebRequest request,
						   int problem)
		{
			Log.DebugFormat ("Checking validation result for {0}: problem={1}", request.RequestUri, problem);

			if (0 == problem) {
				return true;
			}

			// Only try to deal with the problem if it is a trust
			// failure.
			if (-2146762486 != problem) {
				return false;
			}

			LoadCertificates ();

			string hash = certificate.GetCertHashString ();
			Log.DebugFormat ("Certificate hash: " + hash);

			int stored_problem = 0;
			if (cert_hashes.TryGetValue (hash, out stored_problem)
					&& problem == stored_problem) {
				Log.DebugFormat ("We already trust this site");
				return true;
			}

			Decision decision = GetDecision (certificate, request);
			Log.DebugFormat ("Decision: " + decision);

			switch (decision) {
			case Decision.DontTrust:
				return false;
			case Decision.TrustOnce:
				return true;
			case Decision.TrustAlways:
				SaveCertificate (hash, problem);
				return true;
			default:
				Debug.Assert (false, "Unknown decision");
				return false;
			}
		}


		protected virtual Decision GetDecision (
				X509Certificate certificate,
				WebRequest request)
		{
			Decision decision = Decision.DontTrust;
			Log.DebugFormat ("Making the default decision: " + decision);
			return decision;
		}


		private void LoadCertificates ()
		{
			using (IsolatedStorageFileStream isol_stream =
					new IsolatedStorageFileStream (
							StoreName,
							FileMode.OpenOrCreate,
							FileAccess.Read,
			                                isolated_store)) {
				try {
					BinaryFormatter formatter =
							new BinaryFormatter ();
					cert_hashes = (Dictionary<string, int>)
							formatter.Deserialize (
								isol_stream);
				} catch (SerializationException e) {
					// FIXME: handle
					Log.Exception (e);
				}
			}

			if (null == cert_hashes) {
				cert_hashes = new Dictionary<string,int> ();
			}
		}


		private void SaveCertificate (string hash, int problem)
		{
			cert_hashes.Add (hash, problem);

			using (IsolatedStorageFileStream isolated_stream =
					new IsolatedStorageFileStream (
							StoreName,
							FileMode.OpenOrCreate,
							FileAccess.Write,
			                                isolated_store)) {
				try {
					BinaryFormatter formatter =
							new BinaryFormatter ();
					formatter.Serialize (isolated_stream,
							cert_hashes);
				} catch (SerializationException e) {
					// FIXME: handle
					Log.Exception (e);
				}
			}
		}
	}
}
