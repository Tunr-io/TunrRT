using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary.Models;
using TunrRT.DataModel.Models;

namespace TunrRT.DataModel
{
	class LibraryListSample : LibraryList
	{
		public LibraryListSample() : base(null)
		{

		}
		public List<Song> Results { get; set; }
	}
}
