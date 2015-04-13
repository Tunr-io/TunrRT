using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TunrLibrary.Models;

namespace TunrRT.Models
{
    public class ChangeSetDetails
    {
        public enum ChangeType
        {
            Create = 0,
            Update = 1,
            Delete = 2
        }
        public Guid ChangeSetId { get; set; }

        public List<KeyValuePair<ChangeType, Song>> Changes { get; set; }

        public DateTimeOffset LastModifiedTime { get; set; }
    }
}
