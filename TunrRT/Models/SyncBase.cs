using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary.Models;

namespace TunrRT.Models
{
    public class SyncBase
    {
        public Guid? LastSyncId { get; set; }
        public List<Song> Library { get; set; }
    }
}
