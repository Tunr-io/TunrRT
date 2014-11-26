using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TunrRT.DataModel;
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
	public sealed partial class LibraryControl : UserControl
	{
		public LibraryControl()
		{
			this.InitializeComponent();
		}

		private void Header_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var sourceObject = ((FrameworkElement)sender).DataContext as LibraryList;
			(DataContext as DataSource).SelectList(sourceObject);
		}
	}
}
