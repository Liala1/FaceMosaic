using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using FaceMosaic.Enums;

namespace FaceMosaic;

public partial class Settings : ObservableObject
{
	[ObservableProperty]
	int cropLeft;
	
	[ObservableProperty]
	int cropTop;
	
	[ObservableProperty]
	int cropRight;
	
	[ObservableProperty]
	int cropBottom;
	
	[ObservableProperty]
	MaskingType maskingType;
	
	[ObservableProperty]
	int inflate;

	[ObservableProperty]
	int pixelateDivisions = 4;
	
	[ObservableProperty]
	int blurStrength = 30;

	[ObservableProperty]
	bool plotting;
	
	[ObservableProperty]
	string yoloModelPath = "yolov11n-face.onnx";
	
	public static Settings Current { get; private set; } = null!;

	public static Settings Load()
	{
		try
		{
			Current = JsonSerializer.Deserialize<Settings>(File.ReadAllText("settings.json"))!;
			return Current;
		}
		catch (Exception)
		{
			Current = new Settings();
			return Current;
		}
	}

	public void Save()
	{
		var json = JsonSerializer.Serialize(this);
		File.WriteAllText("settings.json", json);
	}
}