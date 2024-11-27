using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace CaptureCore.NativeMethods;

public static partial class User32
{
	public enum GetAncestorFlags
	{
		/// <summary>
		/// Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
		/// </summary>
		GetParent = 1,

		/// <summary>
		/// Retrieves the root window by walking the chain of parent windows.
		/// </summary>
		GetRoot = 2,

		/// <summary>
		/// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
		/// </summary>
		GetRootOwner = 3
	}
	
	public enum GWL
	{
		GWL_WNDPROC = -4,
		GWL_HINSTANCE = -6,
		GWL_HWNDPARENT = -8,
		GWL_STYLE = -16,
		GWL_EXSTYLE = -20,
		GWL_USERDATA = -21,
		GWL_ID = -12
	}

	[Flags]
	public enum WindowStyles : uint
	{
		WS_BORDER = 0x800000,
		WS_CAPTION = 0xc00000,
		WS_CHILD = 0x40000000,
		WS_CLIPCHILDREN = 0x2000000,
		WS_CLIPSIBLINGS = 0x4000000,
		WS_DISABLED = 0x8000000,
		WS_DLGFRAME = 0x400000,
		WS_GROUP = 0x20000,
		WS_HSCROLL = 0x100000,
		WS_MAXIMIZE = 0x1000000,
		WS_MAXIMIZEBOX = 0x10000,
		WS_MINIMIZE = 0x20000000,
		WS_MINIMIZEBOX = 0x20000,
		WS_OVERLAPPED = 0x0,
		WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_SIZEFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
		WS_POPUP = 0x80000000u,
		WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
		WS_SIZEFRAME = 0x40000,
		WS_SYSMENU = 0x80000,
		WS_TABSTOP = 0x10000,
		WS_VISIBLE = 0x10000000,
		WS_VSCROLL = 0x200000
	}

	public enum WindowStylesEx : uint
	{
		WS_EX_ACCEPTFILES = 0x00000010,
		WS_EX_APPWINDOW = 0x00040000,
		WS_EX_CLIENTEDGE = 0x00000200,
		WS_EX_COMPOSITED = 0x02000000,
		WS_EX_CONTEXTHELP = 0x00000400,
		WS_EX_CONTROLPARENT = 0x00010000,
		WS_EX_DLGMODALFRAME = 0x00000001,
		WS_EX_LAYERED = 0x00080000,
		WS_EX_LAYOUTRTL = 0x00400000,
		WS_EX_LEFT = 0x00000000,
		WS_EX_LEFTSCROLLBAR = 0x00004000,
		WS_EX_LTRREADING = 0x00000000,
		WS_EX_MDICHILD = 0x00000040,
		WS_EX_NOACTIVATE = 0x08000000,
		WS_EX_NOINHERITLAYOUT = 0x00100000,
		WS_EX_NOPARENTNOTIFY = 0x00000004,
		WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
		WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
		WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
		WS_EX_RIGHT = 0x00001000,
		WS_EX_RIGHTSCROLLBAR = 0x00000000,
		WS_EX_RTLREADING = 0x00002000,
		WS_EX_STATICEDGE = 0x00020000,
		WS_EX_TOOLWINDOW = 0x00000080,
		WS_EX_TOPMOST = 0x00000008,
		WS_EX_TRANSPARENT = 0x00000020,
		WS_EX_WINDOWEDGE = 0x00000100,
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int left, top, right, bottom;

		public int Width
		{
			get => right - left;
			set => right = value + left;
		}

		public int Height
		{
			get => bottom - top;
			set => bottom = value + top;
		}
	}

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool GetClientRect(nint hWnd, out RECT lpRect);
	
	[LibraryImport("user32.dll")]
	public static partial IntPtr GetShellWindow();

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool IsWindowVisible(IntPtr hWnd);

	[LibraryImport("user32.dll")]
	public static partial IntPtr GetAncestor(IntPtr hWnd, GetAncestorFlags flags);

	[LibraryImport("user32.dll", EntryPoint = "GetWindowLong")]
	private static partial IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

	[LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
	
	public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
	{
		return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLongPtr32(hWnd, nIndex);
	}
	
	[LibraryImport("user32.dll", EntryPoint = "SetWindowLong")]
	private static partial IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, int dwNewLong);
	
	[LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, int dwNewLong);

	public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, int dwNewLong)
	{
		return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
	}

	[LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
	public static partial IntPtr FindWindow(string? lpClassName, string? lpWindowName);
}