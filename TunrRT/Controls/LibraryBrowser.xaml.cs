﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class LibraryBrowser : UserControl
    {
        public LibraryBrowser()
        {
            this.InitializeComponent();
        }

        private void ListBackStack_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedList = (e.ClickedItem as LibraryList);
            (DataContext as DataSource).GoBackTo(clickedList);
        }
    }
}
