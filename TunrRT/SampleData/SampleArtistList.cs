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
    public class SampleArtistList : ArtistList
    {
        public SampleArtistList() : base(null, "Music", null, null)
        {
            for (int i = 0; i < 30; i++)
            {
                Artists.Add(new Song()
                {
                    TagPerformers = new List<string>() { "Some Artist " + i }
                });
            }
        }

        public SampleArtistList(DataSource dataSource, string listName, PropertyInfo targetProperty, List<SongFilter> filters)
            : base(dataSource, listName, targetProperty, filters)
        {
            Artists.Add(new Song()
            {
                TagPerformers = new List<string>(){ "Some Artist" }
            });
        }
        protected override void Update()
        {
            return;
        }
    }
}
