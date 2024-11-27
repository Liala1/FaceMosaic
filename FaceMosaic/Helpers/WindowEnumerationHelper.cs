using System.Runtime.InteropServices;
using CaptureCore.NativeMethods;

namespace FaceMosaic.Helpers;

public static class WindowEnumerationHelper
{
	public static bool IsWindowValidForCapture(IntPtr hwnd)
	{
		if (hwnd.ToInt64() == 0) return false;

		if (hwnd == User32.GetShellWindow()) return false;

		if (!User32.IsWindowVisible(hwnd)) return false;

		if (User32.GetAncestor(hwnd, User32.GetAncestorFlags.GetRoot) != hwnd) return false;

		var style = (User32.WindowStyles)(uint)User32.GetWindowLongPtr(hwnd, (int)User32.GWL.GWL_STYLE).ToInt64();
		if (style.HasFlag(User32.WindowStyles.WS_DISABLED)) return false;

		var hrTemp = DwmApi.DwmGetWindowAttribute(hwnd, DwmApi.DWMWINDOWATTRIBUTE.Cloak, out var cloaked, Marshal.SizeOf<bool>());
		return hrTemp != 0 || !cloaked;
	}
}