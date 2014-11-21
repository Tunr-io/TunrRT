using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using TunrRT.DataModel.Models;

namespace TunrRT.DataModel
{
	/// <summary>
	/// This class exists to manage a filtered list of library items.
	/// </summary>
	class LibraryList
	{
		private readonly DataSource DataSource;
		public PropertyInfo FilteredProperty { get; set; }
		public Song FilterSong { get; set; }
		LibraryList(DataSource dataSource)
		{
			this.DataSource = dataSource;
		}
		private List<Song> _Results;
		public List<Song> Results
		{
			get
			{
				if (_Results == null) {
					_Results = this.DataSource.QueryFilteredSongs(FilterSong);
				}
				return _Results;
			}
		}
	}
}
