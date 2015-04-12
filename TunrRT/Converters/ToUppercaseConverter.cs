using System;
using Windows.UI.Xaml.Data;

namespace TunrRT.Converters
{
    public class ToUppercaseConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (value as string).ToUpper();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
