using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TunrLibrary.Models
{
	public class PlaylistItem
	{
		public Guid PlaylistItemId { get; set; }
		public Guid PlaylistFK { get; set; }
		public Guid SongFK { get; set; }
		public int Order { get; set; }

		[XmlIgnore] // to avoid DB Serialization
		public Playlist Playlist
		{
			get
			{
				return PlaylistFK == null ? null : _playlist ?? (_playlist = LibraryManager.Playlists.LoadByKey(PlaylistFK));
			}
			set
			{
				_playlist = value;
				SyncPlaylistFK();
			}
		}
		Playlist _playlist;

		void SyncPlaylistFK()
		{
			PlaylistFK = _playlist != null ? _playlist.PlaylistId : Guid.Empty;
		}

		[XmlIgnore] // to avoid DB Serialization
		public Song Song
		{
			get
			{
				return SongFK == null ? null : _song ?? (_song = LibraryManager.Songs.LoadByKey(SongFK));
			}
			set
			{
				_song = value;
				SyncSongFK();
			}
		}
		Song _song;

		void SyncSongFK()
		{
			SongFK = _song != null ? _song.SongId : Guid.Empty;
		}

	}
}
