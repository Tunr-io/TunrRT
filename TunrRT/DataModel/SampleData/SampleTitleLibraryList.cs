using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
				new Song() {TrackNumber=1, Title="Best Song Ever"},
				new Song() {TrackNumber=2, Title="Worst Song Ever"},
				new Song() {TrackNumber=3, Title="Amazingest Song Ever"},
				new Song() {TrackNumber=4, Title="Fastest Song Ever"},
				new Song() {TrackNumber=5, Title="Slowest Song Ever"},
				new Song() {TrackNumber=6, Title="Ugliest Song Ever"}
			};
		}
	}
}
