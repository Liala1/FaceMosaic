using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Compunet.YoloSharp.Data;

namespace FaceMosaic.Controls;

public class PlottingOverlay : Canvas
{
	readonly Typeface labelFont = new (SystemFonts.MenuFontFamily.Source);
	const double LabelFontSize = 14;

	YoloResult<Detection>? yoloResult;
	readonly List<GlyphRunDrawer> glyphRunDrawers = [];

	public PlottingOverlay()
	{
		for (var i = 0; i < 16; ++i)
		{
			glyphRunDrawers.Add(CreateGlyphRunDrawer());
		}
	}

	GlyphRunDrawer CreateGlyphRunDrawer()
	{
		if (!labelFont.TryGetGlyphTypeface(out var glyphTypeface))
		{
			throw new ArgumentException("GlyphTypeface");
		}
		return new GlyphRunDrawer(glyphTypeface, LabelFontSize, (float)VisualTreeHelper.GetDpi(this).PixelsPerDip);
	}
	
	public void SetYoloResult(YoloResult<Detection>? result)
	{
		yoloResult = result;
	}
	
	protected override void OnRender(DrawingContext drawingContext)
	{
		base.OnRender(drawingContext);
		
		if (yoloResult == null)
		{
			return;
		}
		
		if (!Settings.Current.Plotting)
		{
			return;
		}
		
		EnsureGlyphRunDrawers(yoloResult);
		DrawDetections(drawingContext, yoloResult);
	}

	void DrawDetections(DrawingContext context, YoloResult<Detection> result)
	{
		const int boxThickness = 1;
		var labelPadding = new Point(2, 0);
		var detectId = 0;
		foreach (var detect in result)
		{
			//var color = detect.Name.Name == "face" ? Colors.Red : Colors.Green;
			if (detect.Name.Name != "face") continue;
			var color = Colors.Red;
			var bounds = GetInflatedBounds(detect);
			DrawBoundingBox(context, color, bounds, boxThickness);
			
			var text = $"{detect.Name.Name} ({detect.Confidence:F2})";
			var textSize = glyphRunDrawers[detectId].CalculateTextSize(text);
			var labelPoint = new Point(bounds.X + labelPadding.X, bounds.Y + labelPadding.Y - textSize.Height);
			var glyphRunDrawer = glyphRunDrawers[detectId];

			DrawLabel(context, glyphRunDrawer, color, detect.Confidence, labelPoint, text, textSize, labelPadding);
			
			++detectId;
		}
	}

	static Rect GetInflatedBounds(Detection detect)
	{
		var bounds = new Rect(detect.Bounds.X, detect.Bounds.Y, detect.Bounds.Width, detect.Bounds.Height);
		bounds.Inflate(Settings.Current.Inflate, Settings.Current.Inflate);
		return bounds;
	}

	void DrawBoundingBox(DrawingContext drawingContext, Color color, Rect bounds, int boxThickness)
	{
		var boxPen = new Pen(new SolidColorBrush(color), boxThickness);
		drawingContext.DrawRectangle(null, boxPen, bounds);
	}
	
	void DrawLabel(DrawingContext drawingContext, GlyphRunDrawer glyphRunDrawer, Color color, float confidence, Point labelPoint, string text, Size textSize, Point labelPadding)
	{
		color.A = (byte)(255 * confidence);
		var brush = new SolidColorBrush(color);
		var labelRect = new Rect(
			labelPoint.X - labelPadding.X,
			labelPoint.Y - labelPadding.Y,
			textSize.Width + labelPadding.X * 2,
			textSize.Height + labelPadding.Y * 2);

		drawingContext.DrawRectangle(brush, null, labelRect);
		// TODO: 
		labelPoint.Y -= 4;
		glyphRunDrawer.DrawText(drawingContext, text, labelPoint, Brushes.White);
	}

	void EnsureGlyphRunDrawers(YoloResult<Detection> result)
	{
		if (glyphRunDrawers.Count >= result.Count) return;
		
		var addCount = result.Count - glyphRunDrawers.Count;
		for (var i = 0; i < addCount; ++i)
		{
			glyphRunDrawers.Add(CreateGlyphRunDrawer());
		}
	}
}