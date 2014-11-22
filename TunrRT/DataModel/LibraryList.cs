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
	public class LibraryList
	{
		/// <summary>
		/// The data source this library list is querying from
		/// </summary>
		private readonly DataSource DataSource;

		/// <summary>
		/// The name of this list - displayed in the browser
		/// </summary>
		public string ListName { get; set; }

		/// <summary>
		/// The property name of the song on which this list is filtering
		/// </summary>
		public string FilteredPropertyName { get; set; }

		/// <summary>
		/// The song object containing all of the properties this list is filtering by
		/// </summary>
		public Song FilterSong { get; set; }

		public LibraryList(DataSource dataSource)
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
