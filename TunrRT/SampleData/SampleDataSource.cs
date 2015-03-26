using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary.Models;
using TunrRT.Models;

namespace TunrRT.SampleData
{
    public class SampleDataSource : DataSource
    {
        public SampleDataSource() : base()
        {
            BrowseLists.Clear();
            var artists = new ArtistList(this, "Sample", LibraryFilterTreeProperties.FirstOrDefault(), new List<TunrLibrary.SongFilter>());
            for (int i = 0 ;i < 30; i++)
            {
                artists.Artists.Add(new Song()
                {
                    TagPerformers = new List<string>() { "Test Artist " + i }
                });
            }
            BrowseLists.Add(artists);
        }
    }
}
