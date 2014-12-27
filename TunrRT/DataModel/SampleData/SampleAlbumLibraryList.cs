using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary.Models;
using TunrRT.DataModel.Models;

namespace TunrRT.DataModel
{
	class SampleAlbumLibraryList : LibraryListSample
	{
		public SampleAlbumLibraryList()
		{
			this.ListName = "albums";
			this.FilterSong = new Song();
			this.FilteredPropertyName = "Album";
			this.Results = new List<Song>() {
				new Song() {TrackNumber=1, Album="Best Album"},
				new Song() {TrackNumber=2, Album="Worst Album"},
				new Song() {TrackNumber=3, Album="Pretty Mediocre Album"},
				new Song() {TrackNumber=4, Album="Heavenly Album"}
			};
		}
	}
}
