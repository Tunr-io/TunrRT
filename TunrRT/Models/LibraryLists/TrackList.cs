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
    public class TrackList : LibraryList
    {
        /// <summary>
        /// A SORTED list of unique artists (through song objects) matching filters.
        /// </summary>
        public ObservableCollection<Song> Tracks { get; set; }

        public TrackList(DataSource dataSource, string listName, PropertyInfo targetProperty, List<SongFilter> filters)
            : base(dataSource, listName, targetProperty, filters) {
            Tracks = new ObservableCollection<Song>();
        }

        /// <summary>
        /// Updates the album list to the current data.
        /// </summary>
        protected override async void Update()
        {
            var tracks = await GetResults();

            foreach (var track in tracks)
            {
                Comparison<Song> trackComparison = (x, y) => {
                    return (int)(x.TagTrack - y.TagTrack);
                };
                var trackComparer = Comparer<Song>.Create(trackComparison);
                var index = Array.BinarySearch<Song>(Tracks.ToArray(), track, trackComparer);
                if (index < 0)
                {
                    Tracks.Insert(~index, track);
                }
            }
        }
    }
}
