using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Win32;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.System.WinRT.Direct3D11;
using WinRT;
using static Windows.Win32.PInvoke;

namespace CaptureCore.Helpers;

public static partial class Direct3D11Helper
{
	public partial class NullHandle() : SafeHandle(IntPtr.Zero, false)
	{
		public override bool IsInvalid => true;

		protected override bool ReleaseHandle() => true;
	}
	
	static unsafe ID3D11Device CreateD3DDevice(D3D_DRIVER_TYPE driverType, D3D11_CREATE_DEVICE_FLAG flags)
	{
		D3D11CreateDevice(null, driverType, new NullHandle(), flags, null, D3D11_SDK_VERSION, out var device, null, out _);
		return device;
	}
	
	public static ID3D11Device CreateD3DDevice()
	{
		ID3D11Device? d3dDevice = null;
		var flags = D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT;
		try
		{
			d3dDevice = CreateD3DDevice(D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, flags);
		}
		catch (Exception e)
		{
			if (e.HResult != (int)DXGI_USAGE.DXGI_USAGE_RENDER_TARGET_OUTPUT)
			{
				throw;
			}
		}

		return d3dDevice ?? CreateD3DDevice(D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_WARP, flags);
	}

	public static IDirect3DDevice CreateDevice(ID3D11Device d3dDevice)
	{
		return CreateDirect3DDeviceFromD3D11Device(d3dDevice);
	}

	public static IDirect3DDevice CreateDirect3DDeviceFromD3D11Device(ID3D11Device d3dDevice)
	{
		var dxgiDevice = d3dDevice.As<IDXGIDevice>();
		CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out var raw);
		var rawPtr = Marshal.GetIUnknownForObject(raw);
		var result = MarshalInterface<IDirect3DDevice>.FromAbi(rawPtr);
		Marshal.Release(rawPtr);
		return result;
	}
	
	public static ID3D11Texture2D CreateD3D11Texture2D(IDirect3DSurface surface)
	{
		surface.As<IDirect3DDxgiInterfaceAccess>()
			.GetInterface(typeof(ID3D11Texture2D).GUID, out var d3dSurface);
		var obj = Marshal.GetObjectForIUnknown(d3dSurface);
		return obj.As<ID3D11Texture2D>();
	}
	
}