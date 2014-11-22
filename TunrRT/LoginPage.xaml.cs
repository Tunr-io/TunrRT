﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
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
			var app = (App.Current) as App;
			try {
				await app.DataSource.SetCredentialsAsync(TextBoxEmail.Text, TextBoxPassword.Password);
				Frame.Navigate(typeof(HubPage));
				Frame.BackStack.Clear();
				return;
			}
			catch (Exception)
			{}
			MessageDialog messageDialog = new MessageDialog("Could not log you in. Make sure your credentials are correct, then try again.", "Authentication failure");
			await messageDialog.ShowAsync();
		}
	}
}