using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CaptureCore;

public class CaptureImage<T> where T : unmanaged, IPixel<T>
{
	public Image<T>? Image { get; internal set; }
	public bool Used { get; set; }
	public int Id { get; set; }
}