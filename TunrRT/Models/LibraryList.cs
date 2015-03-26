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
using Windows.ApplicationModel.Core;
using System.Collections.ObjectModel;

namespace TunrRT.Models
{
	/// <summary>
	/// This class exists to manage a filtered list of library items.
	/// </summary>
	public abstract class LibraryList : INotifyPropertyChanged
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
        /// Whether or not this library list should be visible
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return _IsVisible;
            }
            set
            {
                _IsVisible = value;
                OnPropertyChanged("IsVisible");
            }
        }
        private bool _IsVisible = true;

		/// <summary>
		/// The list of filters being applied to this list
		/// </summary>
		public List<SongFilter> Filters { get; set; }
        
		public LibraryList(DataSource dataSource, string listName, PropertyInfo targetProperty, List<SongFilter> filters)
		{
			this.DataSource = dataSource;
            this.ListName = listName;
            this.TargetProperty = targetProperty;
            this.Filters = filters;
            LibraryManager.LibraryUpdate += LibraryManager_LibraryUpdate;
            Update();
		}

        /// <summary>
        /// Event triggered whenever a library update occurs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void LibraryManager_LibraryUpdate(object sender, EventArgs e)
        {
            Update();
        }

        /// <summary>
        /// Fetches all songs from the database that match list's filters
        /// </summary>
        /// <returns></returns>
        public async Task<List<Song>> GetResults()
        {
            return await LibraryManager.FetchMatchingSongs(Filters);
        }

        /// <summary>
        /// Run to trigger update of list data
        /// </summary>
        protected abstract void Update();

		/// <summary>
		/// Selects an option from this filter list for further filtering by the datasource
		/// </summary>
		/// <param name="s">The target song that the user has selected</param>
		public void SelectSong(Song s) {
			DataSource.SelectFilter(s, TargetProperty);
		}
	}
}
