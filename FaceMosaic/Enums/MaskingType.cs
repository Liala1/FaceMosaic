using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FaceMosaic.Enums;

[TypeConverter(typeof(EnumDisplayTypeConverter))]
public enum MaskingType
{
	[Display(Name = "モザイク")]
	Pixelate,
	[Display(Name = "ぼかし")]
	Blur,
}
