//
// Mono.Google.NoCheckCertificatePolicy
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) Copyright 2006 Novell, Inc. (http://www.novell.com)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Mono.Google {
	public class NoCheckCertificatePolicy : ICertificatePolicy {
		public bool CheckValidationResult (ServicePoint a, X509Certificate b, WebRequest c, int d)
		{
			return true;
		}
	}
}
