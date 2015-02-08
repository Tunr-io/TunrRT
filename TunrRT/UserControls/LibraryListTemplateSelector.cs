using System;
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
		public DataTemplate TitleListTemplate { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			var list = item as LibraryList;
			if (list == null) return ArtistListTemplate;
			switch (list.FilteredPropertyName.ToLower())
			{
				case "tagfirstperformer":
					return ArtistListTemplate;
				case "tagalbum":
					return AlbumListTemplate;
				case "tagtitle":
					return TitleListTemplate;
			}
			return TitleListTemplate;
			
		}
		protected override DataTemplate SelectTemplateCore(object item)
		{
			return ArtistListTemplate;
		}
	}
}
