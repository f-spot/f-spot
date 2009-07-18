// ServerInfo.cs
//
// Authors:
//   Andrzej Wytyczak-Partyka <iapart@gmail.com>
//   James Willcox <snorp@snorp.net>
//
// Copyright (C) 2008 Andrzej Wytyczak-Partyka
// Copyright (C) 2005  James Willcox <snorp@snorp.net>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//


using System;
using System.Text;
using System.Net;

namespace DPAP {

    public enum AuthenticationMethod : byte {
        None,
        UserAndPassword,
        Password,
    }
            
    internal class ServerInfo {

        private string name;
        private AuthenticationMethod auth_method;
        private bool supports_update;
        
        public string Name {
            get { return name; }
            set { name = value; }
        }

        public AuthenticationMethod AuthenticationMethod {
            get { return auth_method; }
            set { auth_method = value; }
        }

        public bool SupportsUpdate {
            get { return supports_update; }
            set { supports_update = value; }
        }

        internal static ServerInfo FromNode (ContentNode node) {
            ServerInfo info = new ServerInfo ();

            if (node.Name != "dmap.serverinforesponse")
                return null;

            foreach (ContentNode child in (node.Value as ContentNode [])) {
                switch (child.Name) {
                case "dmap.itemname":
                    info.Name = (string) child.Value;
                    break;
                case "dmap.authenticationmethod":
                    info.AuthenticationMethod = (AuthenticationMethod) child.Value;
                    break;
                case "dmap.supportsupdate":
                    info.SupportsUpdate = (byte) child.Value == 1;
                    break;
                }
            }

            return info;
        }

        internal ContentNode ToNode (int dbCount) {
            return new ContentNode ("dmap.serverinforesponse",
                                    new ContentNode ("dmap.status", 200),
                                    new ContentNode ("dmap.protocolversion", new Version (2, 0, 0)),
                                    new ContentNode ("dpap.protocolversion", new Version (1, 0, 1)),			                        
                                    new ContentNode ("dmap.itemname", "photos"),
                                    new ContentNode ("dmap.loginrequired", (byte) 1),
			                     //    new ContentNode ("dmap.authenticationmethod", (byte) auth_method),
			                        new ContentNode ("dmap.timeoutinterval", (int) Server.DefaultTimeout.TotalSeconds),
			                        new ContentNode ("dmap.supportsautologout", (byte) 1),
                                 //   new ContentNode ("dmap.supportsupdate", (byte) 1),
                                 //   new ContentNode ("dmap.supportspersistentids", (byte) 1),
                                 //   new ContentNode ("dmap.supportsextensions", (byte) 1),
                                 //   new ContentNode ("dmap.supportsbrowse", (byte) 1),
                                 //   new ContentNode ("dmap.supportsquery", (byte) 1),
                                    new ContentNode ("dmap.supportsindex", (byte) 1),
                                 //   new ContentNode ("dmap.supportsresolve", (byte) 0),
                                    new ContentNode ("dmap.databasescount", dbCount));
        }

    }
}
