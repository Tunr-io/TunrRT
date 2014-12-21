using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunrLibrary.Models
{
	[Table("Playlists")]
	public class Playlist
	{
		[PrimaryKey, Unique]
		public Guid PlaylistId { get; set; }
		public string Name { get; set; }
	}
}
