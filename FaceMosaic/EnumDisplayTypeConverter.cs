using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace FaceMosaic;

public class EnumDisplayTypeConverter(Type type) : EnumConverter(type)
{
	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == typeof(string) && value != null)
		{
			var field = value.GetType().GetField(value.ToString()!);
			if (field != null)
			{
				var attribute = field.GetCustomAttribute<DisplayAttribute>(false);
				return attribute == null ? value.ToString() : attribute.GetName();
			}
		}

		return base.ConvertTo(context, culture, value, destinationType);
	}
}