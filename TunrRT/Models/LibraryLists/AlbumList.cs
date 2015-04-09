using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary;
using TunrLibrary.Models;

namespace TunrRT.Models
{
    public class AlbumList : LibraryList
    {
        /// <summary>
        /// A SORTED list of unique artists (through song objects) matching filters.
        /// </summary>
        public ObservableCollection<Song> Albums { get; set; }

        public AlbumList(DataSource dataSource, string listName, PropertyInfo targetProperty, List<SongFilter> filters)
            : base(dataSource, listName, targetProperty, filters) {
            Albums = new ObservableCollection<Song>();
        }

        /// <summary>
        /// Updates the album list to the current data.
        /// </summary>
        protected override async void Update()
        {
            var songs = await GetResults();
            var albums = songs.GroupBy(s => s.TagAlbum).Select(a => a.First());

            foreach (var album in albums)
            {
                Comparison<Song> albumComparison = (x, y) => {
                    var xAlbum = x.TagAlbum == null ? "Unknown" : x.TagAlbum;
                    var yAlbum = y.TagAlbum == null ? "Unknown" : y.TagAlbum;
                    return xAlbum.CompareTo(yAlbum);
                };
                var albumComparer = Comparer<Song>.Create(albumComparison);
                var index = Array.BinarySearch<Song>(Albums.ToArray(), album, albumComparer);
                if (index < 0)
                {
                    Albums.Insert(~index, album);
                }
            }
        }
    }
}
