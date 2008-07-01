using System;
using System.Net;

namespace DPAP {

    public delegate void UserHandler (object o, UserArgs args);

    public class UserArgs : EventArgs {

        private User user;

        public User User {
            get { return user; }
        }
        
        public UserArgs (User user) {
            this.user = user;
        }
    }

    public class User {
        private DateTime loginTime;
        private DateTime lastAction;
        private IPAddress address;
        private string user;

        public DateTime LoginTime {
            get { return loginTime; }
        }

        public DateTime LastActionTime {
            get { return lastAction; }
            internal set { lastAction = value; }
        }

        public IPAddress Address {
            get { return address; }
        }

        public string UserName {
            get { return user; }
        }

        internal User (DateTime loginTime, IPAddress address, string user) {
            this.loginTime = loginTime;
            this.address = address;
            this.user = user;
        }
    }
}
