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
				new Song() {TagTrack=1, TagAlbum="Best Album"},
				new Song() {TagTrack=2, TagAlbum="Worst Album"},
				new Song() {TagTrack=3, TagAlbum="Pretty Mediocre Album"},
				new Song() {TagTrack=4, TagAlbum="Heavenly Album"}
			};
		}
	}
}
