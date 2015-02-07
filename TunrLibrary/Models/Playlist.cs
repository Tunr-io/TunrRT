using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TunrLibrary.Models
{
	public class Playlist
	{
		public Guid PlaylistId { get; set; }
		public string Name { get; set; }
		public Guid[] PlaylistItemsFK { get; set; }

		[XmlIgnore]  // to avoid DB Serialization
		public Collection<PlaylistItem> PlaylistItems { get { return _playlistItems ?? (_playlistItems = PlaylistItemsRelation()); } }
		Collection<PlaylistItem> _playlistItems;

		void SyncPlaylistItemsFK()
		{
			PlaylistItemsFK = (from i in _playlistItems select i.PlaylistItemId).ToArray();
		}

		Collection<PlaylistItem> PlaylistItemsRelation()
		{
			var playlistItems = PlaylistItemsFK == null ? Enumerable.Empty<PlaylistItem>() : LibraryManager.PlaylistItems.LoadByKeys(PlaylistItemsFK);
			var result = new ObservableCollection<PlaylistItem>(playlistItems);
			result.CollectionChanged += (s, e) => SyncPlaylistItemsFK();
			return result;
		}

	}
}
