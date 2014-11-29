using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunrLibrary.Models
{
	[Table("PlaylistItems")]
	class PlaylistItem
	{
		[PrimaryKey, Unique]
		public Guid PlaylistItemId { get; set; }
		public Guid SongId { get; set; }
		public int Order { get; set; }
	}
}
