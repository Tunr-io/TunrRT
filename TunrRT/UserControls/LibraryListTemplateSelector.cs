﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunrRT.DataModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TunrRT.UserControls
{
	class LibraryListTemplateSelector : DataTemplateSelector
	{
		public DataTemplate ArtistListTemplate { get; set; }
		public DataTemplate AlbumListTemplate { get; set; }
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			var list = item as LibraryList;
			if (list == null) return ArtistListTemplate;
			switch (list.FilteredPropertyName.ToLower())
			{
				case "artist":
					return ArtistListTemplate;
				case "album":
					return AlbumListTemplate;
			}
			return ArtistListTemplate;
			
		}
		protected override DataTemplate SelectTemplateCore(object item)
		{
			return ArtistListTemplate;
		}
	}
}
