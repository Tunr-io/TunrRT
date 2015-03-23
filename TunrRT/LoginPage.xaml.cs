using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace TunrRT
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class LoginPage : Page
	{
		public LoginPage()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Invoked when this page is about to be displayed in a Frame.
		/// </summary>
		/// <param name="e">Event data that describes how this page was reached.
		/// This parameter is typically used to configure the page.</param>
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
            
		}

		private async void AppBarLogin_Click(object sender, RoutedEventArgs e)
		{
			StatusBar status = StatusBar.GetForCurrentView();
			status.ForegroundColor = Colors.Black;
			status.ProgressIndicator.Text = "Logging in...";
			await status.ProgressIndicator.ShowAsync();
			TextBoxEmail.IsEnabled = false;
			TextBoxPassword.IsEnabled = false;
            var result = await (this.DataContext as DataSource).Authenticate(TextBoxEmail.Text, TextBoxPassword.Password);
            if (result)
            {
                await status.ProgressIndicator.HideAsync();
                TextBoxEmail.IsEnabled = true;
                TextBoxPassword.IsEnabled = true;
                Frame.Navigate(typeof(MainPage));
                Frame.BackStack.Clear();
                return;
            }
            await status.ProgressIndicator.HideAsync();
			TextBoxEmail.IsEnabled = true;
			TextBoxPassword.IsEnabled = true;
			MessageDialog messageDialog = new MessageDialog("Could not log you in. Make sure your credentials are correct, then try again.", "Authentication failure");
			await messageDialog.ShowAsync();
		}

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxEmail.Focus(FocusState.Programmatic);
        }
    }
}
