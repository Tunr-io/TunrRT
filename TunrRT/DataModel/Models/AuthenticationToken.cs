using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunrRT.DataModel.Models
{
	class AuthenticationToken
	{
		public Guid Id { get; set; }
		public string access_token { get; set; }
		public string token_type { get; set; }
		public int expires_in { get; set; }
		public string userName { get; set; }
		public string DisplayName { get; set; }
	}
}
