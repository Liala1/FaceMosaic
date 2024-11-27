using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Win32.Foundation;
using CaptureCore;
using CaptureCore.Helpers;
using Compunet.YoloSharp;
using Compunet.YoloSharp.Data;
using FaceMosaic.Enums;
using FaceMosaic.Extensions;
using FaceMosaic.Helpers;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;
using Window = System.Windows.Window;

namespace FaceMosaic;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	public Settings Settings { get; }
	HWND hwnd;
	
	readonly GraphicsCaptureApplication capture;
	readonly DisplayWindow displayWindow = new();

	ObservableCollection<Process>? processes;

	readonly AutoResetEvent frameAvailableEvent = new (false);
	readonly CancellationTokenSource cancellationTokenSource = new ();
	Task processingTask;
	readonly ConcurrentQueue<(TimeSpan timestamp, CaptureImage<Bgra32> captureImage)> frameQueue = new();
	TimeSpan prevTimestamp = TimeSpan.Zero;

	readonly YoloPredictor predictor = null!;
	readonly MatPool matPool = new (3);
	WriteableBitmap? writeableBitmap;
	
	public MainWindow()
	{
		Settings = Settings.Load();
		Configuration.Default.PreferContiguousImageBuffers = true;
		
		capture = new GraphicsCaptureApplication(OnFrameArrived);
		processingTask = Task.Run(() => ProcessFrameAsync(cancellationTokenSource.Token));

		try
		{
			var sessionOptions = new SessionOptions
			{
				EnableCpuMemArena = true,
				EnableMemoryPattern = true,
				GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
				ExecutionMode = ExecutionMode.ORT_PARALLEL,
				IntraOpNumThreads = 4,
				InterOpNumThreads = 4,
			};

			var options = new YoloPredictorOptions
			{
				SessionOptions = sessionOptions,
				UseDml = true,
				UseCuda = false,
			};
			
			predictor = new YoloPredictor("yolov11n-face.onnx", options);
		}
		catch (Exception e)
		{
			var text = e.Message + "\n" + e.StackTrace + "\n" + e.InnerException?.Message;
			MessageBox.Show(e.Message);
			File.WriteAllText("log.txt", text);
		}
		
		InitializeComponent();
	}

	
	void Window_Loaded(object sender, RoutedEventArgs e)
	{
		var interopWindow = new WindowInteropHelper(this);
		hwnd = (HWND)interopWindow.Handle;
		
		InitializeWindowList();
	}

	void Window_Closed(object? sender, EventArgs e)
	{
		displayWindow.Close();
		Settings.Save();
	}

	async void PickerButton_Click(object sender, RoutedEventArgs e)
	{
		capture.StopCapture();
		WindowComboBox.SelectedIndex = -1;
		await StartPickerCaptureAsync();
	}

	void UpdateButton_Click(object sender, RoutedEventArgs e)
	{
		InitializeWindowList();
	}

	void WindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var comboBox = (ComboBox)sender;
		var process = (Process)comboBox.SelectedItem;

		if (process == null) return;

		capture.StopCapture();
		try
		{
			StartHwndCapture((HWND)process.MainWindowHandle);
		}
		catch (Exception)
		{
			processes?.Remove(process);
			comboBox.SelectedIndex = -1;
		}
	}
	
	void OnChangedCrop(object sender, TextChangedEventArgs e)
	{
		var left = CropLeft.Value;
		var top = CropTop.Value;
		var right = CropRight.Value;
		var bottom = CropBottom.Value;

		capture.UpdateCropSize(left, top, right, bottom);
	}
	
	void InitializeWindowList()
	{
		if (ApiInformation.IsApiContractPresent(typeof(UniversalApiContract).FullName, 8))
		{
			var processedWithWindows = Process.GetProcesses()
				.Where(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle) && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle));
			processes = new ObservableCollection<Process>(processedWithWindows);
			WindowComboBox.ItemsSource = processes;
		}
		else
		{
			WindowComboBox.IsEnabled = false;
		}
	}

	async Task StartPickerCaptureAsync()
	{
		var picker = new GraphicsCapturePicker();

		picker.SetWindow(hwnd);
		var item = await picker.PickSingleItemAsync();

		if (item != null)
		{
			displayWindow.Show();
			capture.StartCapture(item);
		}
	}

	void StartHwndCapture(HWND targetHwnd)
	{
		var item = CaptureHelper.CreateItemForWindow(targetHwnd);
		if (item != null)
		{
			displayWindow.Show();
			capture.StartCapture(item);
		}
	}
	
	unsafe WriteableBitmap CopyMatToWriteableBitmap(Mat mat)
	{
		if (writeableBitmap == null || DifferentImageSizes(writeableBitmap, mat))
		{
			var presentationSource = PresentationSource.FromVisual(this);
			var m = presentationSource!.CompositionTarget!.TransformToDevice;
			
			writeableBitmap = new WriteableBitmap(mat.Width, mat.Height,
				m.M11, m.M22,
				PixelFormats.Bgra32, null);
		}

		writeableBitmap.Lock();
		try
		{
			var dest = new Span<Bgra32>(writeableBitmap.BackBuffer.ToPointer(), mat.Width * mat.Height);
			var src = new Span<Bgra32>(mat.DataPointer, mat.Width * mat.Height);
			src.CopyTo(dest);
			writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, mat.Width, mat.Height));
		}
		finally
		{
			writeableBitmap.Unlock();
		}
		return writeableBitmap;
	}
	
	static bool DifferentImageSizes(WriteableBitmap bitmap, Mat mat)
	{
		const double to = 0.5;
		return Math.Abs(bitmap.Width - mat.Width) > to || Math.Abs(bitmap.Height - mat.Height) > to;
	}

	static unsafe void CopyImage(Mat mat, Image<Bgra32> image)
	{
		image.DangerousTryGetSinglePixelMemory(out var memory);
		var dest = new Span<Bgra32>(mat.DataPointer, mat.Width * mat.Height);
		memory.Span.CopyTo(dest);
	}

	static void SafeInflateRect(ref Rect rect, int width, int height, Mat frame)
	{
		rect.Inflate(width, height);
		rect.X = Math.Max(0, rect.X);
		rect.Y = Math.Max(0, rect.Y);
		rect.Width = Math.Min(rect.Width, frame.Width - rect.X);
		rect.Height = Math.Min(rect.Height, frame.Height - rect.Y);
	}
	
	void ApplyPixelate(Mat frame, Rect rect)
	{
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}
		
		var faceSize = Math.Min(rect.Width, rect.Height);
		var blockSize = Math.Max(1, faceSize / Math.Max(1, Settings.PixelateDivisions));

		using var faceRegion = new Mat(frame, rect);
		using var small = new Mat();
		
		Cv2.Resize(faceRegion, small, new Size(rect.Width / blockSize, rect.Height / blockSize), 0, 0, InterpolationFlags.Nearest);
		Cv2.Resize(small, faceRegion, faceRegion.Size(), 0, 0, InterpolationFlags.Nearest);
	}

	void ApplyBlur(Mat frame, Rect rect)
	{
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			return;
		}

		var m = frame[rect];
		if (Settings.BlurStrength > 0)
		{
			Cv2.Blur(m, m, new Size(Settings.BlurStrength, Settings.BlurStrength));
		}
	}

	async Task ProcessFrameAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				await frameAvailableEvent.WaitOneAsync(cancellationToken);
				await ProcessFramesInQueueAsync();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}

	async Task ProcessFramesInQueueAsync()
	{
		while (frameQueue.TryDequeue(out var frame))
		{
			var image = frame.captureImage.Image!;
			var timestamp = frame.timestamp;
			
			var result = await predictor.DetectAsync(image);
			var buffer = PrepareMatForImage(image);

			if (buffer?.Mat == null) continue;

			CopyImage(buffer.Mat, image);
			ProcessDetections(result, buffer.Mat);

			_ = Dispatcher.InvokeAsync(() =>
			{
				UpdateDisplayWindow(timestamp, buffer.Mat, result);
				frame.captureImage.Used = false;
				matPool.Return(buffer);
			});
		}
	}

	MatBuffer? PrepareMatForImage(Image<Bgra32> image)
	{
		var buffer = matPool.Rent();
		if (buffer == null) return null;

		var mat = buffer.Mat;
		if (mat != null && mat.Width == image.Width && mat.Height == image.Height)
		{
			return buffer;
		}
		mat?.Dispose();
		mat = new Mat(image.Height, image.Width, MatType.CV_8UC4);
		buffer.Mat = mat;
		return buffer;
	}

	void ProcessDetections(YoloResult<Detection> result, Mat mat)
	{
		foreach (var detect in result)
		{
			if (detect.Name.Name != "face") continue;
			
			var rect = new Rect(detect.Bounds.X, detect.Bounds.Y, detect.Bounds.Width, detect.Bounds.Height);
			SafeInflateRect(ref rect, Settings.Inflate, Settings.Inflate, mat);

			if (Settings.MaskingType == MaskingType.Pixelate)
			{
				ApplyPixelate(mat, rect);
			}
			else
			{
				ApplyBlur(mat, rect);
			}
		}
	}
	
	void UpdateDisplayWindow(TimeSpan timestamp, Mat mat, YoloResult<Detection> result)
	{
		if (prevTimestamp < timestamp)
		{
			displayWindow.Overlay.SetYoloResult(result);
			displayWindow.Overlay.InvalidateVisual();
            
			var bitmap = CopyMatToWriteableBitmap(mat);
			
			UpdateWindowSize(bitmap, mat);
			prevTimestamp = timestamp;
		}
	}
	
	void UpdateWindowSize(WriteableBitmap bitmap, Mat mat)
	{
		var addWidth = displayWindow.Width - displayWindow.Image.ActualWidth;
		var addHeight = displayWindow.Height - displayWindow.Image.ActualHeight;
    
		displayWindow.Image.Source = bitmap;
		displayWindow.Width = mat.Width + addWidth;
		displayWindow.Height = mat.Height + addHeight;
	}
	
	void OnFrameArrived(TimeSpan timestamp, CaptureImage<Bgra32> image)
	{
		frameQueue.Enqueue((timestamp, image));

		if (frameQueue.Count > 2)
		{
			Console.WriteLine($"Skip {DateTime.Now:hh:mm:ss.fff}");
			frameQueue.TryDequeue(out var queue);
			queue.captureImage.Used = false;
		}
		frameAvailableEvent.Set();
	}
	
	void StopCaptureButton_OnClick(object sender, RoutedEventArgs e)
	{
		capture.StopCapture();
		WindowComboBox.SelectedIndex = -1;
		displayWindow.Hide();
	}
}