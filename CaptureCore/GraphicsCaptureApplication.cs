using Windows.Graphics.Capture;
using SixLabors.ImageSharp.PixelFormats;

namespace CaptureCore;

public class GraphicsCaptureApplication(Action<TimeSpan, CaptureImage<Bgra32>> callback) : IDisposable
{
	GraphicsCapture? capture;

	int cropLeft, cropTop, cropRight, cropBottom;

	public void Dispose()
	{
		StopCapture();
	}

	public void StartCapture(GraphicsCaptureItem item)
	{
		StopCapture();
		capture = new GraphicsCapture(item, callback);
		
		UpdateCropSize(cropLeft, cropTop, cropRight, cropBottom);
		capture.StartCapture();
	}

	public void StopCapture()
	{
		capture?.Dispose();
	}

	public void UpdateCropSize(int left, int top, int right, int bottom)
	{
		cropLeft = left;
		cropTop = top;
		cropRight = right;
		cropBottom = bottom;
		capture?.UpdateCropSize(left, top, right, bottom);
	}
}