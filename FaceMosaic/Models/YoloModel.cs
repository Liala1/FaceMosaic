namespace FaceMosaic.Models;

public class YoloModel(string modelName, string displayName)
{
	public string ModelName { get; set; } = modelName;
	public string DisplayName { get; set; } = displayName;
}