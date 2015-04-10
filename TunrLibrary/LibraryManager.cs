using Lex.Db;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TunrLibrary.Models;
using Windows.Foundation;

namespace TunrLibrary
{
    public static class LibraryManager
    {
        /// <summary>
        /// Reference to the LexDb instance
        /// </summary>
        private static readonly DbInstance LexDb;

        /// <summary>
        /// Reference to the LexDb table for storing songs
        /// </summary>
        public static DbTable<Song> Songs { get { return _songs ?? (_songs = LexDb.Table<Song>()); } }
        static DbTable<Song> _songs;

        /// <summary>
        /// Reference to the LexDb table for storing playlists
        /// </summary>
        public static DbTable<Playlist> Playlists { get { return _playlists ?? (_playlists = LexDb.Table<Playlist>()); } }
        static DbTable<Playlist> _playlists;

        /// <summary>
        /// Reference to the LexDb table for storing playlist items
        /// </summary>
        public static DbTable<PlaylistItem> PlaylistItems { get { return _playlistItems ?? (_playlistItems = LexDb.Table<PlaylistItem>()); } }
        static DbTable<PlaylistItem> _playlistItems;

        /// <summary>
        /// Concurrent dictionary storing songs in-memory by Guid to be accessed quickly
        /// </summary>
        public static ConcurrentDictionary<Guid, Song> LibraryCache { get; set; }

        /// <summary>
        /// Event handler triggered whenever the library has been updated.
        /// </summary>
        public static event EventHandler LibraryUpdate;
        public static void OnLibraryUpdate()
        {
            EventHandler handler = LibraryUpdate;
            if (null != handler) handler(null, EventArgs.Empty);
        }

        /// <summary>
        /// Static constructor for LibraryManager. Initializes database and in-memory cache.
        /// </summary>
        static LibraryManager()
        {
            // Initialize library cache
            LibraryCache = new ConcurrentDictionary<Guid, Song>();

            // Initialize LexDb
            LexDb = new DbInstance("TunrData");

            // Map Db to objects
            LexDb.Map<Song>()
                .Automap(s => s.SongId)
                .WithIndex("TagFirstPerformer", s => s.TagFirstPerformer)
                .WithIndex("TagAlbum", s => s.TagAlbum, StringComparer.OrdinalIgnoreCase)
                .WithIndex("TagTitle", s => s.TagTitle, StringComparer.OrdinalIgnoreCase)
                .WithIndex("TagYear", s => s.TagYear);
            LexDb.Map<Playlist>()
                .Automap(p => p.PlaylistId);
            LexDb.Map<PlaylistItem>()
                .Automap(p => p.PlaylistItemId)
                .WithIndex("PlaylistFK", p => p.PlaylistFK);
            LexDb.Initialize();

            // Populate the in-memory library cache
            var songs = Songs.LoadAll();
            foreach (var song in songs)
            {
                LibraryCache.AddOrUpdate(song.SongId, song, (key, old) => song);
            }
        }

        /// <summary>
		/// Adds the list of songs to the database, or updates them if they already exist.
        /// Also updates the in-memory cache.
		/// </summary>
		/// <param name="songs">The list of songs to add or update</param>
		/// <returns></returns>
		public static async Task AddOrUpdateSongs(ICollection<Song> songs)
        {
            await Songs.SaveAsync(songs);
            foreach (var song in songs)
            {
                LibraryCache.AddOrUpdate(song.SongId, song, (key, old) => song);
            }
            // Trigger library update event
            OnLibraryUpdate();
        }

        /// <summary>
		/// Fetch songs that match the target filter song.
		/// Any properties that are not null in the target filter will be used to find matching results.
		/// </summary>
		/// <param name="targetFilter">The song containing the properties that will be used to filter the collection</param>
		/// <returns></returns>
		public static Task<List<Song>> FetchMatchingSongs(List<SongFilter> filters)
        {
            var tcs = new TaskCompletionSource<List<Song>>();
            List<Song> result = null;

            var asyncAction = Windows.System.Threading.ThreadPool.RunAsync((workItem) =>
            {
                // TODO: Make this cleaner / not hard-coded to certain properties...
                var library = LibraryCache.Values;
                IEnumerable<Song> query = library;
                for (int i = 0; i < filters.Count; i++)
                {
                    var prop = filters[i].Property;
                    var value = filters[i].Value;
                    query = query.Where(s => {
                        var sv = prop.GetValue(s);
                        return sv != null ? sv.Equals(value) : false;
                    });
                }
                result = query.ToList();
            },Windows.System.Threading.WorkItemPriority.Normal);

            asyncAction.Completed = delegate
            {
                switch (asyncAction.Status)
                {
                    case AsyncStatus.Completed:
                        tcs.SetResult(result);
                        break;
                }
            };

            return tcs.Task;
        }

        /// <summary>
		/// Adds the specified song to the end of a playlist
		/// </summary>
		/// <param name="song">Song to add</param>
		/// <returns></returns>
		public static async Task<PlaylistItem> AddSongToPlaylistAsync(Song song)
        {
            var lastItem = PlaylistItems.IndexQueryByKey("PlaylistFK", Guid.Empty).ToList().OrderByDescending(p => p.Order).Take(1).FirstOrDefault();
            int order = 0;
            if (lastItem != null)
            {
                order = lastItem.Order + 1;
            }
            var newPlaylistItem = new PlaylistItem() { PlaylistFK = Guid.Empty, PlaylistItemId = Guid.NewGuid(), Order = order, SongFK = song.SongId };
            await PlaylistItems.SaveAsync(newPlaylistItem);
            return newPlaylistItem;
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

        /// <summary>
        /// Fetches a playlist item by Id
        /// </summary>
        /// <param name="guid">Guid of the playlist item</param>
        /// <returns></returns>
        public static PlaylistItem FetchPlaylistItem(Guid guid)
        {
            return PlaylistItems.LoadByKey(guid);
        }

        /// <summary>
        /// Fetches a playlist item by Id
        /// </summary>
        /// <param name="guid">Guid of the playlist item</param>
        /// <returns></returns>
        public static async Task<PlaylistItem> FetchPlaylistItemAsync(Guid guid)
        {
            return await PlaylistItems.LoadByKeyAsync(guid);
        }

        /// <summary>
        /// Fetches playlist items by playlist id
        /// </summary>
        /// <param name="guid">Guid of the playlist</param>
        /// <returns></returns>
        public static List<PlaylistItem> FetchPlaylistItems(Guid guid)
        {
            return PlaylistItems.IndexQueryByKey("PlaylistFK", guid).ToList();
        }

        /// <summary>
        /// Fetches playlist items by playlist id
        /// </summary>
        /// <param name="guid">Guid of the playlist</param>
        /// <returns></returns>
        public static async Task<List<PlaylistItem>> FetchPlaylistItemsAsync(Guid guid)
        {
            return await PlaylistItems.IndexQueryByKey("PlaylistFK", guid).ToListAsync();
        }

        /// <summary>
        /// Fetches a list of songs in the playlist with given id
        /// </summary>
        /// <param name="guid">Playlist id</param>
        /// <returns></returns>
        public static async Task<List<Song>> FetchPlaylistSongs(Guid guid)
        {
            var playlistItems = await PlaylistItems.IndexQueryByKey("PlaylistFK", guid).ToListAsync();
            return PlaylistItems.Select(i => i.Song).ToList();
        }
    }
}
