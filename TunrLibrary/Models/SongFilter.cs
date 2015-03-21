using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TunrLibrary.Models
{
	public class SongFilter
	{
		public PropertyInfo Property { get; set; }
		public object Value { get; set; }
	}
}
