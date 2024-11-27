using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Win32;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi.Common;
using CaptureCore.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace CaptureCore;

public class GraphicsCapture : IDisposable
{
	readonly Direct3D11CaptureFramePool framePool;
	readonly GraphicsCaptureSession session;
	SizeInt32 lastSize;
	
	readonly IDirect3DDevice device;
	readonly ID3D11Device d3dDevice;
	ID3D11Texture2D? buffer;
	
	readonly Action<TimeSpan, CaptureImage<Bgra32>> frameCallback;
	readonly CaptureImage<Bgra32>[] captureImages = new CaptureImage<Bgra32>[4];
	readonly TimeSpan frameInterval;
	DateTime lastFrameTime;
	int cropLeft, cropTop, cropRight, cropBottom;
	
	public GraphicsCapture(GraphicsCaptureItem captureItem, Action<TimeSpan, CaptureImage<Bgra32>> action)
	{
		frameCallback = action;
		frameInterval = TimeSpan.FromSeconds(1.0 / 60);
		lastFrameTime = DateTime.MinValue;
		for (var i = 0; i < captureImages.Length; ++i)
		{
			captureImages[i] = new CaptureImage<Bgra32>
			{
				Id = i
			};
		}

		var itemSize = captureItem.Size;
		d3dDevice = Direct3D11Helper.CreateD3DDevice();
		device = Direct3D11Helper.CreateDevice(d3dDevice);
		
		framePool = Direct3D11CaptureFramePool.Create(
			device,
			DirectXPixelFormat.B8G8R8A8UIntNormalized,
			2,
			itemSize);

		session = framePool.CreateCaptureSession(captureItem);
		lastSize = itemSize;

		framePool.FrameArrived += OnFrameArrived;
	}
	
	public void Dispose()
	{
		session.Dispose();
		framePool.Dispose();
	}

	public void UpdateCropSize(int left, int top, int right, int bottom)
	{
		cropLeft = left;
		cropTop = top;
		cropRight = right;
		cropBottom = bottom;
	}

	public void StartCapture()
	{
		session.IsCursorCaptureEnabled = false;
		session.StartCapture();
	}

	void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
	{
		using var frame = sender.TryGetNextFrame();
		if (DateTime.Now - lastFrameTime < frameInterval)
		{
			return;
		}

		var bitmap = Direct3D11Helper.CreateD3D11Texture2D(frame.Surface);
		var image = ConvertToImage(bitmap);
		if (image == null)
		{
			return;
		}
		
		frameCallback.Invoke(frame.SystemRelativeTime, image);

		if (frame.ContentSize != lastSize)
		{
			lastSize = frame.ContentSize;
			framePool.Recreate(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, lastSize);
		}
		lastFrameTime = DateTime.Now;
	}

	unsafe CaptureImage<Bgra32>? ConvertToImage(ID3D11Texture2D texture)
	{
		var image = GetUnusedImage();
		if (image == null)
		{
			return null;
		}
		image.Used = true;
		
		SetupStagingTexture(texture, out var stagingDesc);

		d3dDevice.GetImmediateContext(out var context);

		var region2 = new D3D11_BOX()
		{
			left = (uint)cropLeft,
			top = (uint)cropTop,
			right = (uint)(stagingDesc.Width - cropRight),
			bottom = (uint)(stagingDesc.Height - cropBottom),
			back = 1,
		};
		
		context.CopySubresourceRegion(buffer, 0, 0, 0, 0, texture, 0, region2);
		
		D3D11_MAPPED_SUBRESOURCE subResource = default;
		context.Map(buffer, default, D3D11_MAP.D3D11_MAP_READ, default, &subResource);

		var subResourceWidth = (int)(region2.right - region2.left);
		var subResourceHeight = (int)(region2.bottom - region2.top);
		var reqWidth = Math.Max(1, subResourceWidth);
		var reqHeight = Math.Max(1, subResourceHeight);
		if (image.Image == null || image.Image.Width != reqWidth || image.Image.Height != reqHeight)
		{
			image.Image?.Dispose();
			image.Image = new Image<Bgra32>(reqWidth, reqHeight);
		}
		
		var rowSize = (int)subResource.RowPitch / 4;
		var ptr = new IntPtr(subResource.pData);
		
		Parallel.For(0, image.Image.Height, y =>
		{
			var span = new Span<Bgra32>(ptr.ToPointer(), rowSize * (int)(region2.bottom - region2.top));
			var memory = image.Image.DangerousGetPixelRowMemory(y);
			span.Slice(rowSize * y, image.Image.Width).CopyTo(memory.Span);
		});
		
		context.Unmap(buffer, 0);
		Marshal.ReleaseComObject(context);
		
		return image;
	}

	void SetupStagingTexture(ID3D11Texture2D texture, out D3D11_TEXTURE2D_DESC stagingDesc)
	{
		texture.GetDesc(out var desc);
		stagingDesc = new D3D11_TEXTURE2D_DESC
		{
			Width = desc.Width,
			Height = desc.Height,
			MipLevels = 1,
			ArraySize = 1,
			Format = desc.Format,
			Usage = D3D11_USAGE.D3D11_USAGE_STAGING,
			BindFlags = 0,
			SampleDesc = new DXGI_SAMPLE_DESC()
			{
				Count = 1,
			},
			CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
		};

		if (buffer == null)
		{
			d3dDevice.CreateTexture2D(stagingDesc, default, out buffer);
		}
		else if (!IsBufferSizeValid(stagingDesc))
		{
			Marshal.ReleaseComObject(buffer);
			buffer = null;
			d3dDevice.CreateTexture2D(stagingDesc, default, out buffer);
		}
	}

	bool IsBufferSizeValid(D3D11_TEXTURE2D_DESC stagingDesc)
	{
		buffer.GetDesc(out var bufferDesc);
		return stagingDesc.Width == bufferDesc.Width && stagingDesc.Height == bufferDesc.Height;
	}

	CaptureImage<Bgra32>? GetUnusedImage()
	{
		return captureImages.FirstOrDefault(captureImage => !captureImage.Used);
	}
}