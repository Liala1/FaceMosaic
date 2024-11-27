using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT.Graphics.Capture;
using WinRT;
using WinRT.Interop;

namespace CaptureCore.Helpers;

public static class CaptureHelper
{
	static readonly Guid GraphicsCaptureItemGuid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

	public static void SetWindow(this GraphicsCapturePicker picker, HWND hwnd)
	{
		InitializeWithWindow.Initialize(picker, hwnd);
	}

	public static GraphicsCaptureItem? CreateItemForWindow(HWND hwnd)
	{
		unsafe
		{
			var guid = GraphicsCaptureItemGuid;
			var guidPointer = (Guid*)Unsafe.AsPointer(ref guid);
			
			var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
			interop.CreateForWindow(hwnd, guidPointer, out var itemPointer);
			var item = MarshalInterface<GraphicsCaptureItem>.FromAbi(itemPointer);
			Marshal.Release(itemPointer);
			return item;
		}
	}
}