using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ServerManager.Views;

public sealed class HexBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string color = value as string ?? parameter as string ?? "#07101c";
		try
		{
			return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
		}
		catch
		{
			return new SolidColorBrush((Color)ColorConverter.ConvertFromString(parameter as string ?? "#07101c"));
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
