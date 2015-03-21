using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using TunrRT.DataModel.Models;
using TunrLibrary.Models;
using TunrLibrary;
using System.ComponentModel;

namespace TunrRT.DataModel
{
	/// <summary>
	/// This class exists to manage a filtered list of library items.
	/// </summary>
	public class LibraryList : INotifyPropertyChanged
	{
		// Implement INotifyPropertyChanged ...
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}

		/// <summary>
		/// The data source this library list is querying from
		/// </summary>
		private readonly DataSource DataSource;

		/// <summary>
		/// The name of this list
		/// </summary>
		public string ListName { get; set; }

		/// <summary>
		/// The property on which this list is based
		/// </summary>
		public PropertyInfo TargetProperty { get; set; }

		/// <summary>
		/// The list of filters being applied to this list
		/// </summary>
		public List<SongFilter> Filters { get; set; }

		public LibraryList(DataSource dataSource)
		{
			this.DataSource = dataSource;
		}

		/// <summary>
		/// Called to pull the latest list of results from the database for this list
		/// </summary>
		/// <returns></returns>
		public async Task UpdateResults()
		{
			await Task.Run(() =>
			{
				_Results = LibraryManager.FetchMatchingSongs(Filters);
			});
			OnPropertyChanged("Results");
		}

		private List<Song> _Results;
		public List<Song> Results
		{
			get
			{
				if (_Results == null) {
					_Results = new List<Song>();
					UpdateResults();
				}
				return _Results;
			}
		}

		/// <summary>
		/// Selects an option from this filter list for further filtering by the datasource
		/// </summary>
		/// <param name="s">The target song that the user has selected</param>
		public void SelectSong(Song s) {
			DataSource.SelectFilter(s, TargetProperty);
		}
	}
}
