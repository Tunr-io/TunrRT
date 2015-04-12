using System;
using Windows.UI.Xaml.Data;

namespace TunrRT.Converters
{
    public class ToDoubleDigitConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
            return ((int)value).ToString("D2");
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
