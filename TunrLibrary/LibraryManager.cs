using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary.Models;
using System.Reflection;
using Lex.Db;

namespace TunrLibrary
{
	/// <summary>
	/// This class handles the persistant storage of the music library metadata.
	/// </summary>
	public static class LibraryManager
	{
		private static readonly DbInstance LexDb;

		public static DbTable<Song> Songs { get { return _songs ?? (_songs = LexDb.Table<Song>()); } }
		static DbTable<Song> _songs;

		public static DbTable<Playlist> Playlists { get { return _playlists ?? (_playlists = LexDb.Table<Playlist>()); } }
		static DbTable<Playlist> _playlists;

		public static DbTable<PlaylistItem> PlaylistItems { get { return _playlistItems ?? (_playlistItems = LexDb.Table<PlaylistItem>()); } }
		static DbTable<PlaylistItem> _playlistItems;

		/// <summary>
		/// Initialize the connection to the SQLite database, creating tables if they don't exist.
		/// </summary>
		static LibraryManager()
		{
			// Initialize LexDb
			LexDb = new DbInstance("TunrData");
			LexDb.Map<Song>()
				.Automap(s => s.SongId)
				.WithIndex("TagFirstPerformer", s => s.TagFirstPerformer)
				.WithIndex("TagAlbum", s => s.TagAlbum, StringComparer.OrdinalIgnoreCase)
				.WithIndex("TagTitle", s => s.TagTitle, StringComparer.OrdinalIgnoreCase)
				//.WithIndex("TagGenres", s => s.TagGenres)
				.WithIndex("TagYear", s => s.TagYear);
			LexDb.Map<Playlist>()
				.Automap(p => p.PlaylistId);
			LexDb.Map<PlaylistItem>()
				.Automap(p => p.PlaylistItemId)
				.WithIndex("PlaylistFK", p => p.PlaylistFK);

			LexDb.Initialize();
		}

		/// <summary>
		/// Fetch songs that match the target filter song.
		/// Any properties that are not null in the target filter will be used to find matching results.
		/// </summary>
		/// <param name="targetFilter">The song containing the properties that will be used to filter the collection</param>
		/// <returns></returns>
		public static List<Song> FetchMatchingSongs(Song targetFilter)
		{
			// TODO: Make this cleaner / not hard-coded to certain properties...
			var library = Songs.LoadAll();
			var props = targetFilter.GetType().GetRuntimeProperties();
			var nonnull = props.Where(p => p.PropertyType != typeof(Guid) && p.GetValue(targetFilter, null) != null).ToList();
			IEnumerable<Song> query = library;
			for (int i = 0; i < nonnull.Count; i++)
			{
				var prop = nonnull[i];
				query = query.Where(s => prop.GetValue(s) == prop.GetValue(targetFilter));
			}
			return query.ToList();
		}

		/// <summary>
		/// Adds the list of songs to the database, or updates them if they already exist.
		/// </summary>
		/// <param name="songs">The list of songs to add or update</param>
		/// <returns></returns>
		public static async Task AddOrUpdateSongs(List<Song> songs)
		{
			await Songs.SaveAsync(songs);
		}

		/// <summary>
		/// Adds the specified song to the end of a playlist
		/// </summary>
		/// <param name="song">Song to add</param>
		/// <returns></returns>
		public static async Task AddSongToPlaylistAsync(Song song)
		{
			var lastItem = PlaylistItems.IndexQueryByKey("PlaylistFK", Guid.Empty).ToList().OrderByDescending(p => p.Order).Take(1).FirstOrDefault();
			int order = 0;
			if (lastItem != null)
			{
				order = lastItem.Order + 1;
			}
			await PlaylistItems.SaveAsync(new PlaylistItem() { PlaylistFK = Guid.Empty, PlaylistItemId = Guid.NewGuid(), Order = order, SongFK = song.SongId });
		}

		/// <summary>
		/// Clears the specified playlist of any items
		/// </summary>
		/// <param name="playlistId">ID of playlist to clear of items</param>
		/// <returns></returns>
		public static async Task ClearPlaylist(Guid playlistId)
		{
			await PlaylistItems.DeleteAsync(PlaylistItems.IndexQueryByKey("PlaylistFK", playlistId).ToList());
		}

		/// <summary>
		/// Fetches the song associated with the given playlist item
		/// </summary>
		/// <param name="playlistItemId">GUID of playlist item with which to find the song</param>
		/// <returns>Song object associated with playlist item</returns>
		public static async Task<Song> FetchPlaylistItemSong(Guid playlistItemId)
		{
			return (await PlaylistItems.LoadByKeyAsync(playlistItemId)).Song;
		}

		public static async Task<PlaylistItem> FetchPlaylistItem(Guid guid)
		{
			return await PlaylistItems.LoadByKeyAsync(guid);
		}

		public static async Task<List<PlaylistItem>> FetchPlaylistItems(Guid guid)
		{
			return await PlaylistItems.IndexQueryByKey("PlaylistFK", guid).ToListAsync();
		}
	}
}
