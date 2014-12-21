using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary.Models;
using System.Reflection;

namespace TunrLibrary
{
	/// <summary>
	/// This class handles the persistant storage of the music library metadata.
	/// </summary>
	public static class LibraryManager
	{
		private static readonly SQLiteAsyncConnection SqlLiteConnection;

		/// <summary>
		/// Initialize the connection to the SQLite database, creating tables if they don't exist.
		/// </summary>
		static LibraryManager()
		{
			// Initialize the SQLite DB
			SqlLiteConnection = new SQLiteAsyncConnection("Tunr.db");
			SqlLiteConnection.CreateTableAsync<Song>();
			SqlLiteConnection.CreateTableAsync<Playlist>();
			SqlLiteConnection.CreateTableAsync<PlaylistItem>();
		}

		/// <summary>
		/// Finds a song in the database by its GUID
		/// </summary>
		/// <param name="id">GUID of the song to find</param>
		/// <returns>Single Song object or null if not found</returns>
		public static async Task<Song> FetchSongByIdAsync(Guid id)
		{
			return await SqlLiteConnection.Table<Song>().Where(s => s.SongId == id).FirstOrDefaultAsync();
		}

		/// <summary>
		/// Fetch songs that match the target filter song.
		/// Any properties that are not null in the target filter will be used to find matching results.
		/// </summary>
		/// <param name="targetFilter">The song containing the properties that will be used to filter the collection</param>
		/// <returns></returns>
		public static async Task<List<Song>> FetchMatchingSongs(Song targetFilter)
		{
			var props = targetFilter.GetType().GetRuntimeProperties();
			var nonnull = props.Where(p => p.GetValue(targetFilter, null) != null).ToList();

			string sqlQuery = "SELECT * FROM Songs ";
			if (nonnull.Count > 0)
			{
				sqlQuery += " WHERE";
			}
			for (int i = 0; i < nonnull.Count; i++)
			{
				var prop = nonnull[i];
				if (i > 0)
				{
					sqlQuery += " AND";
				}
				sqlQuery += " " + prop.Name + " = ? ";
			}
			var sqlParams = nonnull.Select(p => p.GetValue(targetFilter, null)).ToArray<object>();

			var matches = await SqlLiteConnection.QueryAsync<Song>(sqlQuery, sqlParams);
			return matches;
		}

		/// <summary>
		/// Adds the list of songs to the database, or updates them if they already exist.
		/// </summary>
		/// <param name="songs">The list of songs to add or update</param>
		/// <returns></returns>
		public static async Task AddOrUpdateSongs(List<Song> songs)
		{
			await SqlLiteConnection.InsertOrReplaceAllAsync(songs);
		}

		/// <summary>
		/// Adds the specified song to a playlist
		/// </summary>
		/// <param name="song">Song to add</param>
		/// <returns></returns>
		public static async Task AddSongToPlaylistAsync(Song song)
		{
			await SqlLiteConnection.InsertAsync(new PlaylistItem() { PlaylistItemId = Guid.NewGuid(), Order = 0, SongId = (Guid)song.SongId });
		}

		/// <summary>
		/// Retrieves a list of playlist items from the specified playlist
		/// </summary>
		/// <param name="playlistId">GUID of playlist to retrieve</param>
		/// <returns>A list of playlist items</returns>
		public static async Task<List<PlaylistItem>> FetchPlaylistItems(Guid playlistId)
		{
			return await SqlLiteConnection.Table<PlaylistItem>().Where(p => p.PlaylistId == playlistId).OrderBy(p => p.Order).ToListAsync();
		}

		/// <summary>
		/// Fetches the song associated with the given playlist iten
		/// </summary>
		/// <param name="playlistItemId">GUID of playlist item with which to find the song</param>
		/// <returns>Song object associated with playlist item</returns>
		public static async Task<Song> FetchPlaylistItemSong(Guid playlistItemId)
		{
			// HACK : this should be a join query, but I'm lazy right now
			var playlistItem = await SqlLiteConnection.Table<PlaylistItem>().Where(p => p.PlaylistItemId == playlistItemId).FirstOrDefaultAsync();
			var song = await SqlLiteConnection.Table<Song>().Where(s => s.SongId == playlistItem.SongId).FirstOrDefaultAsync();
			return song;
		}
	}
}
