
using System;

namespace DPAP {

    public class AuthenticationException : ApplicationException {

        public AuthenticationException (string msg) : base (msg) {
        }
    }
}
