using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary.Models;
using TunrRT.DataModel.Models;

namespace TunrRT.DataModel
{
	class DataSourceSampleData : DataSource
	{
		public DataSourceSampleData()
		{
			BrowseLists = new ObservableCollection<LibraryList>()
			{
				new LibraryListSample() {
					FilteredPropertyName="Artist",
					ListName="Music",
					FilterSong=new Song(),
					Results=new List<Song>() {
						new Song() {Artist="Test Artist 1"},
						new Song() {Artist="Test Artist 2"},
						new Song() {Artist="Test Artist 3"}
					}
				}
			};
		}
	}
}
