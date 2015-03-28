using System.Collections.Generic;
using System.Linq;
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

            PlaylistItems.Clear();
            for (int i = 0; i < 20; i++)
            {
                PlaylistItems.Add(new PlaylistItem()
                {
                    Song = new Song()
                    {
                        TagTitle = "Test Track " + i,
                        TagFirstPerformer = "Test Artist " + i
                    }
                });
            }
        }
    }
}
