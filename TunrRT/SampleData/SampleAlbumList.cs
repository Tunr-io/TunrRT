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
    public class SampleAlbumList : AlbumList
    {
        public SampleAlbumList() : base(null, "Artist", null, null)
        {
            for (int i = 0; i < 30; i++)
            {
                Albums.Add(new Song()
                {
                    TagAlbum = "Some Album " + i
                });
            }
        }

        public SampleAlbumList(DataSource dataSource, string listName, PropertyInfo targetProperty, List<SongFilter> filters)
            : base(dataSource, listName, targetProperty, filters)
        {
        }
        protected override void Update()
        {
            return;
        }
    }
}
