using Windows.System;
using CaptureCore.Helpers;

namespace FaceMosaic;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
	DispatcherQueueController controller = CoreMessagingHelper.CreateDispatcherQueueControllerForCurrentThread();
}

