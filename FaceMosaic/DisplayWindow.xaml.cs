using System.Windows;
using System.Windows.Interop;
using CaptureCore.NativeMethods;

namespace FaceMosaic;

public partial class DisplayWindow : Window
{
	public DisplayWindow()
	{
		InitializeComponent();
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		var hwnd = new WindowInteropHelper(this).Handle;
		const int gwlStyle = (int)User32.GWL.GWL_STYLE;
		var style = User32.GetWindowLongPtr(hwnd, gwlStyle);
		style &= (~(int)User32.WindowStyles.WS_SYSMENU);
		User32.SetWindowLongPtr(hwnd, gwlStyle, (int)style);
	}
}