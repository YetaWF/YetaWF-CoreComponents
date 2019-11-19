/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Net.NetworkInformation;

namespace YetaWF.Core.ConsoleStartup.Support {

    /// <summary>
    /// Class implementing standard support functions for console applications.
    /// </summary>
    public static class Support {

        /// <summary>
        /// Tests whether the application can access the Internet.
        /// </summary>
        /// <returns>Returns true if Internet connectivity is available, false otherwise.</returns>
        public static bool IsConnectedToInternet() {
            try {
                Ping myPing = new Ping();
                String host = "8.8.8.8";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                return (reply.Status == IPStatus.Success);
            } catch { }
            return false;
        }
    }
}
