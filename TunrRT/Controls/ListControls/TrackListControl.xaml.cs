using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using TunrLibrary.Models;
using TunrRT.Models;
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

namespace TunrRT.Controls
{
	public sealed partial class TrackListControl : UserControl
	{
        public TrackListControl()
		{
			this.InitializeComponent();
		}

		private void ListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var song = e.ClickedItem as Song;
			(DataContext as LibraryList).SelectSong(song);
		}
	}
}
