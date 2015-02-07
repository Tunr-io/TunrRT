using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary.Models;
using TunrRT.DataModel.Models;

namespace TunrRT.DataModel
{
	class SampleTitleLibraryList : LibraryListSample
	{
		public SampleTitleLibraryList() {
			this.ListName = "tracks";
			this.FilterSong = new Song();
			this.FilteredPropertyName = "Title";
			this.Results = new List<Song>() {
				new Song() {TagTrack=1, TagTitle="Best Song Ever"},
				new Song() {TagTrack=2, TagTitle="Worst Song Ever"},
				new Song() {TagTrack=3, TagTitle="Amazingest Song Ever"},
				new Song() {TagTrack=4, TagTitle="Fastest Song Ever"},
				new Song() {TagTrack=5, TagTitle="Slowest Song Ever"},
				new Song() {TagTrack=6, TagTitle="Ugliest Song Ever"}
			};
		}
	}
}
