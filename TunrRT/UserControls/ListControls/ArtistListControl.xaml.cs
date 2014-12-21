﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TunrLibrary.Models;
using TunrRT.DataModel;
using TunrRT.DataModel.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace TunrRT.UserControls
{
	public sealed partial class ArtistListControl : UserControl
	{
		public ArtistListControl()
		{
			this.InitializeComponent();
		}

		private void ListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var song = e.ClickedItem as Song;
			(DataContext as LibraryList).SelectSong(song);
		}
	}

	public class ArtistListConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			List<Song> songs = value as List<Song>;
			return songs.GroupBy(a => a.Artist).Select(a => a.First()).OrderBy(a => a.Artist).GroupBy(a => a.Artist.ToUpper()[0]);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
