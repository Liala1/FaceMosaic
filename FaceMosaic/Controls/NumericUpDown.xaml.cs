using System.Windows;
using System.Windows.Controls;

namespace FaceMosaic.Controls;

public partial class NumericUpDown : UserControl
{
	public event TextChangedEventHandler? TextChanged;
	
	public static readonly DependencyProperty ValueProperty =
		DependencyProperty.Register(nameof(Value), typeof(int), typeof(NumericUpDown),
			new PropertyMetadata(0, OnValueChanged));

	public int Value
	{
		get => (int)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public int Increment { get; set; } = 1;
	public int Maximum { get; set; } = 100;
	public int Minimum { get; set; } = 0;

	public NumericUpDown()
	{
		InitializeComponent();
	}

	static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var control = (NumericUpDown)d;
		var value = (int)e.NewValue;

		if (value > control.Maximum)
			control.Value = control.Maximum;
		if (value < control.Minimum)
			control.Value = control.Minimum;
	}

	void ButtonUp_Click(object sender, RoutedEventArgs e)
	{
		if (Value + Increment <= Maximum)
			Value += Increment;
	}

	void ButtonDown_Click(object sender, RoutedEventArgs e)
	{
		if (Value - Increment >= Minimum)
			Value -= Increment;
	}
	
	void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		TextChanged?.Invoke(sender, e);
	}
}