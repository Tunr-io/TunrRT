using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using TunrRT.Models;

namespace TunrRT.Controls
{
    public class LibraryListTemplateSelector : DataTemplateSelector
	{
		public DataTemplate ArtistListTemplate { get; set; }
		public DataTemplate AlbumListTemplate { get; set; }
		public DataTemplate TrackListTemplate { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			var list = item as LibraryList;
			if (list == null) return ArtistListTemplate;
			switch (list.GetType().Name)
			{
				case "ArtistList":
					return ArtistListTemplate;
                case "AlbumList":
                    return AlbumListTemplate;
                case "TrackList":
                    return TrackListTemplate;
                default:
                    return TrackListTemplate;
			}
		}
		protected override DataTemplate SelectTemplateCore(object item)
		{
			return ArtistListTemplate;
		}
	}
}
