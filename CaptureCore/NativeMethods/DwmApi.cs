using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace CaptureCore.NativeMethods;

public static partial class DwmApi
{
	public enum DWMWINDOWATTRIBUTE : uint
	{
		NCRenderingEnabled = 1,
		NCRenderingPolicy,
		TransitionsForceDisabled,
		AllowNCPaint,
		CaptionButtonBounds,
		NonClientRtlLayout,
		ForceIconicRepresentation,
		Flip3DPolicy,
		ExtendedFrameBounds,
		HasIconicBitmap,
		DisallowPeek,
		ExcludedFromPeek,
		Cloak,
		Cloaked,
		FreezeRepresentation
	}
	
	[LibraryImport("dwmapi.dll")]
	public static partial int DwmGetWindowAttribute(IntPtr hWnd,
		DWMWINDOWATTRIBUTE dwAttribute,
		IntPtr pvAttribute,
		int cbAttribute);
	
	[LibraryImport("dwmapi.dll")]
	public static partial int DwmGetWindowAttribute(IntPtr hWnd,
		DWMWINDOWATTRIBUTE dwAttribute,
		[MarshalAs(UnmanagedType.Bool)] out bool pvAttribute,
		int cbAttribute);
}