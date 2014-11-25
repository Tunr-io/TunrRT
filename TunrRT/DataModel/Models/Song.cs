using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunrRT.DataModel.Models
{
	[Table("Songs")]
	public class Song 
	{
		[PrimaryKey,Unique]
		public Guid? SongId { get; set; }
		public Guid? OwnerId { get; set; }
		public string SongMD5 { get; set; }
		public string Title { get; set; }
		public string Artist { get; set; }
		public string Album { get; set; }
		public int? TrackNumber { get; set; }
		public int? DiscNumber { get; set; }
		public int? Year { get; set; }
		public string Genre { get; set; }
		public double? Length { get; set; }

		public Song Clone()
		{
			return new Song()
			{
				SongId = SongId,
				OwnerId = OwnerId,
				SongMD5 = SongMD5,
				Title = Title,
				Artist = Artist,
				Album = Album,
				TrackNumber = TrackNumber,
				DiscNumber = DiscNumber,
				Year = Year,
				Genre = Genre,
				Length = Length
			};
		}
	}
}
