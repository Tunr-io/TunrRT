﻿using System;
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
    public class ArtistList : LibraryList
    {
        /// <summary>
        /// A SORTED list of unique artists (through song objects) matching filters.
        /// </summary>
        public ObservableCollection<Song> Artists { get; set; }

        public ArtistList(DataSource dataSource, string listName, PropertyInfo targetProperty, List<SongFilter> filters)
            : base(dataSource, listName, targetProperty, filters) {
            Artists = new ObservableCollection<Song>();
        }

        /// <summary>
        /// Updates the artist list to the current data.
        /// </summary>
        protected override async void Update()
        {
            var songs = await GetResults();
            var artists = songs.GroupBy(s => s.TagFirstPerformer).Select(a => a.First());

            foreach (var artist in artists)
            {
                Comparison<Song> artistComparison = (x, y) => x.TagFirstPerformer.CompareTo(y.TagFirstPerformer);
                var artistComparer = Comparer<Song>.Create(artistComparison);
                var index = Array.BinarySearch<Song>(Artists.ToArray(), artist, artistComparer);
                if (index < 0)
                {
                    Artists.Insert(~index, artist);
                }
            }
        }
    }
}
