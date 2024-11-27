using System.Collections;
using System.Windows;
using System.Windows.Media;

namespace FaceMosaic;

public class GlyphRunDrawer
{
	static readonly Dictionary<GlyphTypeface, GlyphInfo[]> GlyphInfos = new ();
	
	readonly GlyphRun glyphRun;
	
	readonly GlyphInfo[] glyphInfoTable;
	readonly double baseLine;
	ushort[] glyphIndexes = new ushort[1024];
	double[] advanceWidths = new double[1024];
	readonly ListWrapper<ushort> glyphIndexesList;
	readonly ListWrapper<double> advanceWidthsList;
	
	
	public GlyphRunDrawer(GlyphTypeface typeface, double fontSize, float dpi)
	{
		if (!GlyphInfos.TryGetValue(typeface, out var glyphInfo))
		{
			var characterToGlyphMap = typeface.CharacterToGlyphMap;
			var advanceWidthsDictionary = typeface.AdvanceWidths;
			
			glyphInfoTable = new GlyphInfo[char.MaxValue];
			GlyphInfos.Add(typeface, glyphInfoTable);
			foreach (var kvp in characterToGlyphMap)
			{
				var c = (char)kvp.Key;
				var glyphIndex = kvp.Value;
				var width = advanceWidthsDictionary[glyphIndex] * fontSize;
				var height = typeface.AdvanceHeights[glyphIndex] * fontSize;
				var info = new GlyphInfo(glyphIndex, width, height);
				glyphInfoTable[c] = info;
			}
		}
		else
		{
			glyphInfoTable = glyphInfo;
		}

		baseLine = Math.Round(typeface.Baseline * fontSize);
		glyphIndexesList = new ListWrapper<ushort>(glyphIndexes, 1);
		advanceWidthsList = new ListWrapper<double>(advanceWidths, 1);
		
		glyphRun = new GlyphRun(typeface, 0, false, fontSize, dpi,
			glyphIndexesList, new Point(0, 0), advanceWidthsList,
			null, null, null, null, null, null);
	}

	public void DrawText(DrawingContext context, string text, Point point, Brush brush)
	{
		if (text.Length == 0) return;
		
		EnsureArraySize(text.Length);
		for (var i = 0; i < text.Length; ++i)
		{
			var c = text[i];
			var info = glyphInfoTable[c];
			glyphIndexes[i] = info.Index;
			advanceWidths[i] = info.Width;
		}
		glyphIndexesList.SetSize(text.Length);
		advanceWidthsList.SetSize(text.Length);
		
		context.PushTransform(new TranslateTransform(point.X, point.Y + baseLine));
		context.DrawGlyphRun(brush, glyphRun);
		context.Pop();
	}

	public Size CalculateTextSize(string text)
	{
		var width = 0.0;
		var maxHeight = 0.0;
    
		foreach (var c in text)
		{
			var info = glyphInfoTable[c];
			width += info.Width;
			maxHeight = Math.Max(maxHeight, info.Height);
		}
		
		return new Size(width, maxHeight);
	}

	void EnsureArraySize(int length)
	{
		if (length <= glyphInfoTable.Length) return;
		
		var newLength = Math.Max(glyphIndexes.Length * 2, length);
		glyphIndexes = new ushort[newLength];
		advanceWidths = new double[newLength];
		glyphIndexesList.SetArray(glyphIndexes);
		advanceWidthsList.SetArray(advanceWidths);
	}
	
	struct GlyphInfo(ushort glyphIndex, double width, double height)
	{
		public readonly ushort Index = glyphIndex;
		public readonly double Width = width;
		public readonly double Height = height;
	}

	sealed class ListWrapper<T>(T[] array, int size) : IList<T>
	{
		T[] array = array;

		public int IndexOf(T item) => throw new NotImplementedException();

		public void Insert(int index, T item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public T this[int index]
		{
			get => array[index];
			set => throw new NotImplementedException();
		}

		public bool Remove(T item) => throw new NotImplementedException();

		public int Count { get; private set; } = size;
		public bool IsReadOnly { get; private set; } = true;

		public void Add(T item)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(T item) => throw new NotImplementedException();

		public void CopyTo(T[] destArray, int arrayIndex)
		{
			Array.Copy(this.array, 0, destArray, arrayIndex, Count);
		}

		public void SetArray(T[] newArray)
		{
			array = newArray;
		}

		public void SetSize(int size)
		{
			Count = size;
		}

		public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}