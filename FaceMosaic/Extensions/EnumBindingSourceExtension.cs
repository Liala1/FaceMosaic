using System.Windows.Markup;

namespace FaceMosaic.Extensions;

public class EnumBindingSourceExtension : MarkupExtension
{
	readonly Type enumType;

	public EnumBindingSourceExtension(Type enumType)
	{
		if (!enumType.IsEnum)
		{
			throw new ArgumentException($"{enumType} is not enum.");
		}

		this.enumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
	}

	public override object ProvideValue(IServiceProvider serviceProvider) => 
		Enum.GetValues(enumType);
}