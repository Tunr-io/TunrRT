using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary.Models;
using TunrRT.DataModel.Models;

namespace TunrRT.DataModel
{
	class SampleArtistLibraryList : LibraryListSample
	{
		public SampleArtistLibraryList() {
			this.ListName = "music";
			this.FilterSong = new Song();
			this.FilteredPropertyName = "Artist";
			this.Results = new List<Song>() {
				new Song() {Artist="Test Artist 1"},
				new Song() {Artist="Test Artist 2"},
				new Song() {Artist="Test Artist 3"},
				new Song() {Artist="Best Artist 1"},
				new Song() {Artist="Best Artist 2"},
				new Song() {Artist="Best Artist 3"}
			};
		}
	}
}
