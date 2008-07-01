
using System;

namespace DPAP {

    internal class Utility {

        private static DateTime epoch = new DateTime (1970, 1, 1).ToLocalTime ();
        
        public static DateTime ToDateTime (int time) {
            return epoch.AddSeconds (time);
        }

        public static int FromDateTime (DateTime time) {
            return (int) time.Subtract (epoch).TotalSeconds;
        }
    }
}
