﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TunrRT.UserControls
{
	class LibraryListTemplateSelector : DataTemplateSelector
	{
		public DataTemplate ArtistListTemplate { get; set; }
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			return ArtistListTemplate;
		}
		protected override DataTemplate SelectTemplateCore(object item)
		{
			return ArtistListTemplate;
		}
	}
}