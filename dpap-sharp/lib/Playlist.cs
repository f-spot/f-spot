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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DPAP {

    public delegate void PlaylistTrackHandler (object o, int index, Track track);

    public class Playlist {

        private static int nextid = 1;
        
        private int id;
        private string name = String.Empty;
        private List<Track> tracks = new List<Track> ();
        private List<int> containerIds = new List<int> ();

        public event PlaylistTrackHandler TrackAdded;
        public event PlaylistTrackHandler TrackRemoved;
        public event EventHandler NameChanged;

        public Track this[int index] {
            get {
                if (tracks.Count > index)
                    return tracks[index];
                else
                    return null;
            }
            set { tracks[index] = value; }
        }
        
        public IList<Track> Tracks {
            get { return new ReadOnlyCollection<Track> (tracks); }
        }

        internal int Id {
            get { return id; }
            set { id = value; }
        }

        public string Name {
            get { return name; }
            set {
                name = value;
                if (NameChanged != null)
                    NameChanged (this, new EventArgs ());
            }
        }

        internal Playlist () {
            id = nextid++;
        }
		
        public Playlist (string name) : this () {
            this.name = name;
        }

        public void InsertTrack (int index, Track track) {
            InsertTrack (index, track, tracks.Count + 1);
        }

        internal void InsertTrack (int index, Track track, int id) {
            tracks.Insert (index, track);
            containerIds.Insert (index, id);

            if (TrackAdded != null)
                TrackAdded (this, index, track);
        }

        public void Clear () {
            tracks.Clear ();
        }

        public void AddTrack (Track track) {
            AddTrack (track, tracks.Count + 1);
        }
        
        internal void AddTrack (Track track, int id) {
            tracks.Add (track);
            containerIds.Add (id);

            if (TrackAdded != null)
                TrackAdded (this, tracks.Count - 1, track);
        }

        public void RemoveAt (int index) {
            Track track = (Track) tracks[index];
            tracks.RemoveAt (index);
            containerIds.RemoveAt (index);
            
            if (TrackRemoved != null)
                TrackRemoved (this, index, track);
        }

        public bool RemoveTrack (Track track) {
            int index;
            bool ret = false;
            
            while ((index = IndexOf (track)) >= 0) {
                ret = true;
                RemoveAt (index);
            }

            return ret;
        }

        public int IndexOf (Track track) {
            return tracks.IndexOf (track);
        }

        internal int GetContainerId (int index) {
            return (int) containerIds[index];
        }

        internal ContentNode ToTracksNode (int[] deletedIds) {
            ArrayList trackNodes = new ArrayList ();

            for (int i = 0; i < tracks.Count; i++) {
                Track track = tracks[i] as Track;
                trackNodes.Add (track.ToPlaylistNode ((int) containerIds[i]));
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

            if (deletedNodes != null)
                children.Add (new ContentNode ("dmap.deletedidlisting", deletedNodes));
            
            
            return new ContentNode ("daap.playlistsongs", children);
        }

        internal ContentNode ToNode (bool basePlaylist) {

            ArrayList nodes = new ArrayList ();

            nodes.Add (new ContentNode ("dmap.itemid", id));
            nodes.Add (new ContentNode ("dmap.persistentid", (long) id));
            nodes.Add (new ContentNode ("dmap.itemname", name));
            nodes.Add (new ContentNode ("dmap.itemcount", tracks.Count));
            if (basePlaylist)
                nodes.Add (new ContentNode ("daap.baseplaylist", (byte) 1));
            
            return new ContentNode ("dmap.listingitem", nodes);
        }

        internal static Playlist FromNode (ContentNode node) {
            Playlist pl = new Playlist ();

            foreach (ContentNode child in (ContentNode[]) node.Value) {
                switch (child.Name) {
                case  "daap.baseplaylist":
                    return null;
                case "dmap.itemid":
                    pl.Id = (int) child.Value;
                    break;
                case "dmap.itemname":
                    pl.Name = (string) child.Value;
                    break;
                default:
                    break;
                }
            }

            return pl;
        }

        internal void Update (Playlist pl) {
            if (pl.Name == name)
                return;

            Name = pl.Name;
        }

        internal int LookupIndexByContainerId (int id) {
            return containerIds.IndexOf (id);
        }

		public int getId() {
			return Id;
		}
    }

}
