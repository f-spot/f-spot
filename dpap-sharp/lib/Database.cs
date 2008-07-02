/*
 * daap-sharp
 * Copyright (C) 2005  James Willcox <snorp@snorp.net>
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Net;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace DPAP {

    public delegate void TrackHandler (object o, TrackArgs args);

    public class TrackArgs : EventArgs {
        private Track track;

        public Track Track {
            get { return track; }
        }
        
        public TrackArgs (Track track) {
            this.track = track;
        }
    }
        
    public delegate void PlaylistHandler (object o, PlaylistArgs args);

    public class PlaylistArgs : EventArgs {
        private Playlist pl;

        public Playlist Playlist {
            get { return pl; }
        }
        
        public PlaylistArgs (Playlist pl) {
            this.pl = pl;
        }
    }

    public class Database : ICloneable {

        private const int ChunkLength = 8192;
		
        private const string TrackQuery = "meta=dpap.aspectratio,dmap.itemid,dmap.itemname,dpap.imagefilename," +
			"dpap.imagefilesize,dpap.creationdate,dpap.imagepixelwidth," +
			"dpap.imagepixelheight,dpap.imageformat,dpap.imagerating," +
			"dpap.imagecomments,dpap.imagelargefilesize&type=photo"; 
			

        private static int nextid = 1;
        private Client client;
        private int id;
        private long persistentId;
        private string name;

        private List<Track> tracks = new List<Track> ();
        private List<Playlist> playlists = new List<Playlist> ();
        private Playlist basePlaylist = new Playlist ();
        private int nextTrackId = 1;

        public event TrackHandler TrackAdded;
        public event TrackHandler TrackRemoved;
        public event PlaylistHandler PlaylistAdded;
        public event PlaylistHandler PlaylistRemoved;

        public int Id {
            get { return id; }
        }

        public string Name {
            get { return name; }
            set {
                name = value;
                basePlaylist.Name = value;
            }
        }
        
        public IList<Track> Tracks {
            get {
                return new ReadOnlyCollection<Track> (tracks);
            }
        }
        
        public int TrackCount {
            get { return tracks.Count; }
        }

        public Track TrackAt(int index)
        {
            return tracks[index] as Track;
        }

        public IList<Playlist> Playlists {
            get {
                return new ReadOnlyCollection<Playlist> (playlists);
            }
        }

        internal Client Client {
            get { return client; }
        }

        private Database () {
            this.id = nextid++;
        }

        public Database (string name) : this () {
            this.Name = name;
        }

        internal Database (Client client, ContentNode dbNode) : this () {
            this.client = client;

            Parse (dbNode);
        }

        private void Parse (ContentNode dbNode) {
            foreach (ContentNode item in (ContentNode[]) dbNode.Value) {

                switch (item.Name) {
                case "dmap.itemid":
                    id = (int) item.Value;
                    break;
                case "dmap.persistentid":
                    persistentId = (long) item.Value;
                    break;
                case "dmap.itemname":
                    name = (string) item.Value;
                    break;
                default:
                    break;
                }
            }
        }

        public Track LookupTrackById (int id) {
            foreach (Track track in tracks) {
                if (track.Id == id)
                    return track;
            }

            return null;
        }

        public Playlist LookupPlaylistById (int id) {
            if (id == basePlaylist.Id)
                return basePlaylist;

            foreach (Playlist pl in playlists) {
                if (pl.Id == id)
                    return pl;
            }

            return null;
        }

        internal ContentNode ToTracksNode (string[] fields, int[] deletedIds) {

            ArrayList trackNodes = new ArrayList ();
            foreach (Track track in tracks) {
                trackNodes.Add (track.ToNode (fields));
            }

            ArrayList deletedNodes = null;

            if (deletedIds.Length > 0) {
                deletedNodes = new ArrayList ();
                
                foreach (int id in deletedIds) {
                    deletedNodes.Add (new ContentNode ("dmap.itemid", id));
                }
            }

            ArrayList children = new ArrayList ();
            children.Add (new ContentNode ("dmap.status", 200));
            children.Add (new ContentNode ("dmap.updatetype", deletedNodes == null ? (byte) 0 : (byte) 1));
            children.Add (new ContentNode ("dmap.specifiedtotalcount", tracks.Count));
            children.Add (new ContentNode ("dmap.returnedcount", tracks.Count));
            children.Add (new ContentNode ("dmap.listing", trackNodes));

            if (deletedNodes != null) {
                children.Add (new ContentNode ("dmap.deletedidlisting", deletedNodes));
            }
            
            return new ContentNode ("daap.databasesongs", children);
        }

        internal ContentNode ToPlaylistsNode () {
            ArrayList nodes = new ArrayList ();

            nodes.Add (basePlaylist.ToNode (true));
            
            foreach (Playlist pl in playlists) {
                nodes.Add (pl.ToNode (false));
            }

            return new ContentNode ("daap.databaseplaylists",
                                    new ContentNode ("dmap.status", 200),
                                    new ContentNode ("dmap.updatetype", (byte) 0),
                                    new ContentNode ("dmap.specifiedtotalcount", nodes.Count),
                                    new ContentNode ("dmap.returnedcount", nodes.Count),
                                    new ContentNode ("dmap.listing", nodes));
        }

        internal ContentNode ToDatabaseNode () {
            return new ContentNode ("dmap.listingitem",
                                    new ContentNode ("dmap.itemid", id),
                                    new ContentNode ("dmap.persistentid", (long) id),
                                    new ContentNode ("dmap.itemname", name),
                                    new ContentNode ("dmap.itemcount", tracks.Count),
                                    new ContentNode ("dmap.containercount", playlists.Count + 1));
        }

        public void Clear () {
            if (client != null)
                throw new InvalidOperationException ("cannot clear client databases");

            ClearPlaylists ();
            ClearTracks ();
        }

        private void ClearPlaylists () {
            foreach (Playlist pl in new List<Playlist> (playlists)) {
                RemovePlaylist (pl);
            }
        }

        private void ClearTracks () {
            foreach (Track track in new List<Track> (tracks)) {
                RemoveTrack (track);
            }
        }

        private bool IsUpdateResponse (ContentNode node) {
            return node.Name == "dmap.updateresponse";
        }

        private void RefreshPlaylists (string revquery) {
            byte[] playlistsData;

            try {
                playlistsData = client.Fetcher.Fetch (String.Format ("/databases/{0}/containers", id, revquery));
            } catch (WebException) {
                return;
            }
            
            ContentNode playlistsNode = ContentParser.Parse (client.Bag, playlistsData);

            if (IsUpdateResponse (playlistsNode))
                return;

            // handle playlist additions/changes
            ArrayList plids = new ArrayList ();
            
            foreach (ContentNode playlistNode in (ContentNode[]) playlistsNode.GetChild ("dmap.listing").Value) {
                Playlist pl = Playlist.FromNode (playlistNode);

                if (pl != null) {
                    plids.Add (pl.Id);
                    Playlist existing = LookupPlaylistById (pl.Id);

                    if (existing == null) {
                        AddPlaylist (pl);
                    } else {
                        existing.Update (pl);
                    }
                }
            }

            // delete playlists that no longer exist
            foreach (Playlist pl in new List<Playlist> (playlists)) {
                if (!plids.Contains (pl.Id)) {
                    RemovePlaylist (pl);
                }
            }

            plids = null;

            // add/remove tracks in the playlists
            foreach (Playlist pl in playlists) {
                byte[] playlistTracksData = client.Fetcher.Fetch (String.Format ("/databases/{0}/containers/{1}/items",
                                                                                id, pl.Id), revquery);
                ContentNode playlistTracksNode = ContentParser.Parse (client.Bag, playlistTracksData);

                if (IsUpdateResponse (playlistTracksNode))
                    return;

                if ((byte) playlistTracksNode.GetChild ("dmap.updatetype").Value == 1) {

                    // handle playlist track deletions
                    ContentNode deleteList = playlistTracksNode.GetChild ("dmap.deletedidlisting");

                    if (deleteList != null) {
                        foreach (ContentNode deleted in (ContentNode[]) deleteList.Value) {
                            int index = pl.LookupIndexByContainerId ((int) deleted.Value);

                            if (index < 0)
                                continue;

                            pl.RemoveAt (index);
                        }
                    }
                }

                // add new tracks, or reorder existing ones

                int plindex = 0;
                foreach (ContentNode plTrackNode in (ContentNode[]) playlistTracksNode.GetChild ("dmap.listing").Value) {
                    Track pltrack = null;
                    int containerId = 0;
                    Track.FromPlaylistNode (this, plTrackNode, out pltrack, out containerId);

                    if (pl[plindex] != null && pl.GetContainerId (plindex) != containerId) {
                        pl.RemoveAt (plindex);
                        pl.InsertTrack (plindex, pltrack, containerId);
                    } else if (pl[plindex] == null) {
                        pl.InsertTrack (plindex, pltrack, containerId);
                    }

                    plindex++;
                }
            }
        }

        private void RefreshTracks (string revquery) {
            byte[] tracksData = client.Fetcher.Fetch (String.Format ("/databases/{0}/items", id),
                                                     TrackQuery + "&" + revquery);
            ContentNode tracksNode = ContentParser.Parse (client.Bag, tracksData);

            if (IsUpdateResponse (tracksNode))
                return;

            // handle track additions/changes
            foreach (ContentNode trackNode in (ContentNode[]) tracksNode.GetChild ("dmap.listing").Value) {
                Track track = Track.FromNode (trackNode);
                Track existing = LookupTrackById (track.Id);

                if (existing == null)
                    AddTrack (track);
                else
                    existing.Update (track);
            }

            if ((byte) tracksNode.GetChild ("dmap.updatetype").Value == 1) {

                // handle track deletions
                ContentNode deleteList = tracksNode.GetChild ("dmap.deletedidlisting");

                if (deleteList != null) {
                    foreach (ContentNode deleted in (ContentNode[]) deleteList.Value) {
                        Track track = LookupTrackById ((int) deleted.Value);

                        if (track != null)
                            RemoveTrack (track);
                    }
                }
            }
        }

        internal void Refresh (int newrev) {
            if (client == null)
                throw new InvalidOperationException ("cannot refresh server databases");

            string revquery = null;

            if (client.Revision != 0)
                revquery = String.Format ("revision-number={0}&delta={1}", newrev, newrev - client.Revision);

            RefreshTracks ("");
            RefreshPlaylists ("");
        }

        private HttpWebResponse FetchTrack (Track track, long offset) {
            return client.Fetcher.FetchFile (String.Format ("/databases/{0}/items/{1}.{2}", id, track.Id, track.Format),
                                             offset);
        }

        public Stream StreamTrack (Track track, out long length) {
            return StreamTrack (track, -1, out length);
        }
        
        public Stream StreamTrack (Track track, long offset, out long length) {
            HttpWebResponse response = FetchTrack (track, offset);
            length = response.ContentLength;
            return response.GetResponseStream ();
        }

        public void DownloadTrack (Track track, string dest) {

            BinaryWriter writer = new BinaryWriter (File.Open (dest, FileMode.Create));

            try {
                long len;
                using (BinaryReader reader = new BinaryReader (StreamTrack (track, out len))) {
                    int count = 0;
                    byte[] buf = new byte[ChunkLength];
                    
                    do {
                        count = reader.Read (buf, 0, ChunkLength);
                        writer.Write (buf, 0, count);
                    } while (count != 0);
                }
            } finally {
                writer.Close ();
            }
        }

        public void AddTrack (Track track) {
            if (track.Id == 0)
                track.SetId (nextTrackId++);
            
            tracks.Add (track);
            basePlaylist.AddTrack (track);

            if (TrackAdded != null)
                TrackAdded (this, new TrackArgs (track));
        }

        public void RemoveTrack (Track track) {
            tracks.Remove (track);
            basePlaylist.RemoveTrack (track);

            foreach (Playlist pl in playlists) {
                pl.RemoveTrack (track);
            }

            if (TrackRemoved != null)
                TrackRemoved (this, new TrackArgs (track));
        }

        public void AddPlaylist (Playlist pl) {
            playlists.Add (pl);

            if (PlaylistAdded != null)
                PlaylistAdded (this, new PlaylistArgs (pl));
        }

        public void RemovePlaylist (Playlist pl) {
            playlists.Remove (pl);

            if (PlaylistRemoved != null)
                PlaylistRemoved (this, new PlaylistArgs (pl));
        }

        private Playlist ClonePlaylist (Database db, Playlist pl) {
            Playlist clonePl = new Playlist (pl.Name);
            clonePl.Id = pl.Id;

            IList<Track> pltracks = pl.Tracks;
            for (int i = 0; i < pltracks.Count; i++) {
                clonePl.AddTrack (db.LookupTrackById (pltracks[i].Id), pl.GetContainerId (i));
            }

            return clonePl;
        }

        public object Clone () {
            Database db = new Database (this.name);
            db.id = id;
            db.persistentId = persistentId;

            List<Track> cloneTracks = new List<Track> ();
            foreach (Track track in tracks) {
                cloneTracks.Add ((Track) track.Clone ());
            }

            db.tracks = cloneTracks;

            List<Playlist> clonePlaylists = new List<Playlist> ();
            foreach (Playlist pl in playlists) {
                clonePlaylists.Add (ClonePlaylist (db, pl));
            }

            db.playlists = clonePlaylists;
            db.basePlaylist = ClonePlaylist (db, basePlaylist);
            return db;
        }
    }
}
