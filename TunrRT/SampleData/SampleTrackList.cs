using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary;
using TunrLibrary.Models;
using TunrRT.Models;

namespace TunrRT.SampleData
{
    public class SampleTrackList : TrackList
    {
        public SampleTrackList() : base(null, "Album", null, null)
        {
            for (int i = 0; i < 30; i++)
            {
                Tracks.Add(new Song()
                {
                    TagTitle = "Some Song " + i,
                    TagTrack = i
                });
            }
        }

        public SampleTrackList(DataSource dataSource, string listName, PropertyInfo targetProperty, List<SongFilter> filters)
            : base(dataSource, listName, targetProperty, filters)
        {
        }
        protected override void Update()
        {
            return;
        }
    }
}
