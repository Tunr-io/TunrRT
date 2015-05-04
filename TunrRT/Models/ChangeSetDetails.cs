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
        
        public Guid ChangeSetId { get; set; }

        public List<ChangeDetails> Changes { get; set; }
        public bool IsFresh { get; set; }

        public DateTimeOffset LastModifiedTime { get; set; }
    }

    public class ChangeDetails
    {
        public enum ChangeType
        {
            Create = 0,
            Update = 1,
            Delete = 2
        }
        public ChangeType Type { get; set; }
        public Song Song { get; set; }
    }
}
