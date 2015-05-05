using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunrLibrary.Models
{
	public class Song
	{
		/// <summary>
		/// Unique identifier for this song.
		/// </summary>
		public Guid SongId { get; set; }

		/// <summary>
		/// MD5 hash of the first few KB of the file. Used to prevent duplicates.
		/// </summary>
		public string FileMd5Hash { get; set; }

        /// <summary>
		/// MD5 hash of the pure audio. Used to prevent duplicates.
		/// </summary>
		public string AudioMd5Hash { get; set; }

        /// <summary>
        /// Full name of the file originally uploaded.
        /// </summary>
        public string FileName { get; set; }

		/// <summary>
		/// Type of this file - 'mp3', 'flac', etc.
		/// </summary>
		public string FileType { get; set; }

		/// <summary>
		/// Size of the file in bytes.
		/// </summary>
		public long? FileSize { get; set; }

		/// <summary>
		/// Number of audio channels.
		/// </summary>
		public int? AudioChannels { get; set; }

		/// <summary>
		/// Bitrate of the audio.
		/// </summary>
		public double? AudioBitrate { get; set; }

		/// <summary>
		/// Sample rate of the audio.
		/// </summary>
		public int? AudioSampleRate { get; set; }

		/// <summary>
		/// Duration of the song in seconds.
		/// </summary>
		public double? Duration { get; set; }

		/// <summary>
		/// Tag: Track title.
		/// </summary>
		public string TagTitle { get; set; }

		/// <summary>
		/// Tag: Track album.
		/// </summary>
		public string TagAlbum { get; set; }

		/// <summary>
		/// Tag: List of track performers.
		/// </summary>
		public List<string> TagPerformers { get; set; }

		/// <summary>
		/// The first performer listed for this song.
		/// </summary>
		public string TagFirstPerformer
		{
			get
			{
                if (TagPerformers == null)
                {
                    return null;
                }
                if (TagPerformers.Count == 0)
                {
                    return "Unknown";
                }
				return TagPerformers.FirstOrDefault();
			}
			set
			{
				if (TagPerformers == null)
				{
					TagPerformers = new List<string>() {
						value
					};
					return;
				}
				if (TagPerformers.Contains(value))
				{
					TagPerformers.Remove(value);
				}
				TagPerformers.Insert(0, value);
			}
		}

		/// <summary>
		/// Tag: List of track album artists.
		/// </summary>
		public List<string> TagAlbumArtists { get; set; }

		/// <summary>
		/// Tag: List of track composers.
		/// </summary>
		public List<string> TagComposers { get; set; }

		/// <summary>
		/// Tag: List of track genres.
		/// </summary>
		public List<string> TagGenres { get; set; }

		/// <summary>
		/// Tag: Track year.
		/// </summary>
		public int? TagYear { get; set; }

		/// <summary>
		/// Tag: Track number.
		/// </summary>
		public int? TagTrack { get; set; }

		/// <summary>
		/// Tag: Total album track count.
		/// </summary>
		public int? TagTrackCount { get; set; }

		/// <summary>
		/// Tag: Track disc number.
		/// </summary>
		public int? TagDisc { get; set; }

		/// <summary>
		/// Tag: Total album disc count.
		/// </summary>
		public int? TagDiscCount { get; set; }

		/// <summary>
		/// Tag: Track comment.
		/// </summary>
		public string TagComment { get; set; }

		/// <summary>
		/// Tag: Track lyrics.
		/// </summary>
		public string TagLyrics { get; set; }

		/// <summary>
		/// Tag: Track conductor.
		/// </summary>
		public string TagConductor { get; set; }

		/// <summary>
		/// Tag: Track BPM.
		/// </summary>
		public int? TagBeatsPerMinute { get; set; }

		/// <summary>
		/// Tag: Track grouping.
		/// </summary>
		public string TagGrouping { get; set; }

		/// <summary>
		/// Tag: Track copyright.
		/// </summary>
		public string TagCopyright { get; set; }
	}
}
