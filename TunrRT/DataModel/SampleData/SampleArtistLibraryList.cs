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
			this.FilteredPropertyName = "TagPerformers";
			this.Results = new List<Song>() {
				new Song() {TagPerformers=new List<string>() {"Test Artist 1"}},
				new Song() {TagPerformers=new List<string>() {"Test Artist 2"}},
				new Song() {TagPerformers=new List<string>() {"Test Artist 3"}},
				new Song() {TagPerformers=new List<string>() {"Test Artist 1"}},
				new Song() {TagPerformers=new List<string>() {"Test Artist 2"}},
				new Song() {TagPerformers=new List<string>() {"Test Artist 3"}},
			};
		}
	}
}
