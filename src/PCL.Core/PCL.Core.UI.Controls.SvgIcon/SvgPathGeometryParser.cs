using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace PCL.Core.UI.Controls.SvgIcon;

internal static class SvgPathGeometryParser
{
	private enum TokenKind
	{
		Command,
		Number
	}

	private readonly record struct Token(TokenKind Kind, char Command, double Number);

	private sealed class Parser(string data)
	{
		private readonly PathGeometry _geometry = new PathGeometry();

		private readonly List<Token> _tokens = _Tokenize(data);

		private Point _current;

		private PathFigure? _figure;

		private bool _figureOpen;

		private Point _figureStart;

		private int _index;

		private char _lastCommand;

		private Point? _lastCubicControl;

		private Point? _lastQuadraticControl;

		public Geometry Parse()
		{
			char c = '\0';
			while (_index < _tokens.Count)
			{
				if (_PeekCommand(out var command))
				{
					c = command;
					_index++;
				}
				else if (c == '\0')
				{
					throw new FormatException("SVG path data must start with a command.");
				}
				_ExecuteCommand(c);
			}
			return _geometry;
		}

		private void _ExecuteCommand(char command)
		{
			switch (command)
			{
			case 'M':
			case 'm':
				_MoveTo(command == 'm');
				break;
			case 'L':
			case 'l':
				_LineTo(command == 'l');
				break;
			case 'H':
			case 'h':
				_HorizontalLineTo(command == 'h');
				break;
			case 'V':
			case 'v':
				_VerticalLineTo(command == 'v');
				break;
			case 'C':
			case 'c':
				_CubicBezierTo(command == 'c');
				break;
			case 'S':
			case 's':
				_SmoothCubicBezierTo(command == 's');
				break;
			case 'Q':
			case 'q':
				_QuadraticBezierTo(command == 'q');
				break;
			case 'T':
			case 't':
				_SmoothQuadraticBezierTo(command == 't');
				break;
			case 'A':
			case 'a':
				_ArcTo(command == 'a');
				break;
			case 'Z':
			case 'z':
				_CloseFigure();
				break;
			default:
				throw new FormatException($"Unsupported SVG path command: {command}");
			}
		}

		private void _MoveTo(bool relative)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			if (_HasNumber())
			{
				Point point = _ReadPoint(relative);
				_BeginFigure(point);
				_SetLastCommand('M');
				while (_HasNumber())
				{
					_AddLine(_ReadPoint(relative));
				}
			}
		}

		private void _LineTo(bool relative)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			while (_HasNumber())
			{
				_AddLine(_ReadPoint(relative));
			}
			_SetLastCommand('L');
		}

		private void _HorizontalLineTo(bool relative)
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			while (_HasNumber())
			{
				double num = _ReadNumber();
				_AddLine(new Point(relative ? (_current.X + num) : num, _current.Y));
			}
			_SetLastCommand('H');
		}

		private void _VerticalLineTo(bool relative)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			while (_HasNumber())
			{
				double num = _ReadNumber();
				Point current = _current;
				current.Y = (relative ? (_current.Y + num) : num);
				_AddLine(current);
			}
			_SetLastCommand('V');
		}

		private void _CubicBezierTo(bool relative)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			while (_HasNumber())
			{
				Point point = _ReadPoint(relative);
				Point val = _ReadPoint(relative);
				Point val2 = _ReadPoint(relative);
				_EnsureFigure();
				_figure.Segments.Add(new BezierSegment(point, val, val2, isStroked: true));
				_current = val2;
				_lastCubicControl = val;
				_lastQuadraticControl = null;
				_lastCommand = 'C';
			}
		}

		private void _SmoothCubicBezierTo(bool relative)
		{
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			Point point;
			Point val;
			Point val2;
			Point val3;
			for (; _HasNumber(); point = val, val2 = _ReadPoint(relative), val3 = _ReadPoint(relative), _EnsureFigure(), _figure.Segments.Add(new BezierSegment(point, val2, val3, isStroked: true)), _current = val3, _lastCubicControl = val2, _lastQuadraticControl = null, _lastCommand = 'S')
			{
				char lastCommand = _lastCommand;
				if ((lastCommand == 'C' || lastCommand == 'S') ? true : false)
				{
					Point? lastCubicControl = _lastCubicControl;
					if (lastCubicControl.HasValue)
					{
						val = _Reflect(_lastCubicControl.Value, _current);
						continue;
					}
				}
				val = _current;
			}
		}

		private void _QuadraticBezierTo(bool relative)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			while (_HasNumber())
			{
				Point val = _ReadPoint(relative);
				Point val2 = _ReadPoint(relative);
				_EnsureFigure();
				_figure.Segments.Add(new QuadraticBezierSegment(val, val2, isStroked: true));
				_current = val2;
				_lastQuadraticControl = val;
				_lastCubicControl = null;
				_lastCommand = 'Q';
			}
		}

		private void _SmoothQuadraticBezierTo(bool relative)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			Point val2;
			Point val;
			Point val3;
			for (; _HasNumber(); val2 = val, val3 = _ReadPoint(relative), _EnsureFigure(), _figure.Segments.Add(new QuadraticBezierSegment(val2, val3, isStroked: true)), _current = val3, _lastQuadraticControl = val2, _lastCubicControl = null, _lastCommand = 'T')
			{
				char lastCommand = _lastCommand;
				if ((lastCommand == 'Q' || lastCommand == 'T') ? true : false)
				{
					Point? lastQuadraticControl = _lastQuadraticControl;
					if (lastQuadraticControl.HasValue)
					{
						val = _Reflect(_lastQuadraticControl.Value, _current);
						continue;
					}
				}
				val = _current;
			}
		}

		private void _ArcTo(bool relative)
		{
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			while (_HasNumber())
			{
				double num = Math.Abs(_ReadNumber());
				double num2 = Math.Abs(_ReadNumber());
				double rotationAngle = _ReadNumber();
				bool isLargeArc = Math.Abs(_ReadNumber()) > 0.0;
				bool flag = Math.Abs(_ReadNumber()) > 0.0;
				Point val = _ReadPoint(relative);
				_EnsureFigure();
				if (num <= 0.0 || num2 <= 0.0)
				{
					_figure.Segments.Add(new LineSegment(val, isStroked: true));
				}
				else
				{
					_figure.Segments.Add(new ArcSegment(val, new Size(num, num2), rotationAngle, isLargeArc, flag ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, isStroked: true));
				}
				_current = val;
				_lastCubicControl = null;
				_lastQuadraticControl = null;
				_lastCommand = 'A';
			}
		}

		private void _CloseFigure()
		{
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			if (_figureOpen && _figure != null)
			{
				_figure.IsClosed = true;
				_current = _figureStart;
				_figureOpen = false;
			}
			_SetLastCommand('Z');
		}

		private void _BeginFigure(Point point)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			_figure = new PathFigure
			{
				StartPoint = point,
				IsClosed = false,
				IsFilled = true
			};
			_geometry.Figures.Add(_figure);
			_current = point;
			_figureStart = point;
			_figureOpen = true;
			_lastCubicControl = null;
			_lastQuadraticControl = null;
		}

		private void _EnsureFigure()
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			if (!_figureOpen || _figure == null)
			{
				_BeginFigure(_current);
			}
		}

		private void _AddLine(Point point)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			_EnsureFigure();
			_figure.Segments.Add(new LineSegment(point, isStroked: true));
			_current = point;
			_lastCubicControl = null;
			_lastQuadraticControl = null;
			_lastCommand = 'L';
		}

		private Point _ReadPoint(bool relative)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			double num = _ReadNumber();
			double num2 = _ReadNumber();
			if (!relative)
			{
				return new Point(num, num2);
			}
			return new Point(_current.X + num, _current.Y + num2);
		}

		private double _ReadNumber()
		{
			if (!_HasNumber())
			{
				throw new FormatException("Expected number in SVG path data.");
			}
			return _tokens[_index++].Number;
		}

		private bool _HasNumber()
		{
			if (_index < _tokens.Count)
			{
				return _tokens[_index].Kind == TokenKind.Number;
			}
			return false;
		}

		private bool _PeekCommand(out char command)
		{
			if (_index < _tokens.Count && _tokens[_index].Kind == TokenKind.Command)
			{
				command = _tokens[_index].Command;
				return true;
			}
			command = '\0';
			return false;
		}

		private void _SetLastCommand(char command)
		{
			_lastCommand = command;
			if ((command != 'C' && command != 'S') || 1 == 0)
			{
				_lastCubicControl = null;
			}
			if ((command != 'Q' && command != 'T') || 1 == 0)
			{
				_lastQuadraticControl = null;
			}
		}

		private static Point _Reflect(Point point, Point around)
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			return new Point(around.X * 2.0 - point.X, around.Y * 2.0 - point.Y);
		}
	}

	public static Geometry Parse(string data)
	{
		return new Parser(data).Parse();
	}

	private static List<Token> _Tokenize(string data)
	{
		List<Token> list = new List<Token>();
		int num = 0;
		while (num < data.Length)
		{
			char c = data[num];
			if (char.IsWhiteSpace(c) || c == ',')
			{
				num++;
				continue;
			}
			if (_IsCommand(c))
			{
				list.Add(new Token(TokenKind.Command, c, 0.0));
				num++;
				continue;
			}
			int num2 = num;
			if ((c == '+' || c == '-') ? true : false)
			{
				num++;
			}
			bool flag = false;
			while (num < data.Length && char.IsDigit(data[num]))
			{
				num++;
				flag = true;
			}
			if (num < data.Length && data[num] == '.')
			{
				num++;
				while (num < data.Length && char.IsDigit(data[num]))
				{
					num++;
					flag = true;
				}
			}
			if (!flag)
			{
				throw new FormatException($"Invalid SVG path number near index {num2}.");
			}
			bool flag2 = num < data.Length;
			if (flag2)
			{
				char c2 = data[num];
				bool flag3 = ((c2 == 'E' || c2 == 'e') ? true : false);
				flag2 = flag3;
			}
			if (flag2)
			{
				int num3 = num;
				num++;
				flag2 = num < data.Length;
				if (flag2)
				{
					char c2 = data[num];
					bool flag3 = ((c2 == '+' || c2 == '-') ? true : false);
					flag2 = flag3;
				}
				if (flag2)
				{
					num++;
				}
				bool flag4 = false;
				while (num < data.Length && char.IsDigit(data[num]))
				{
					num++;
					flag4 = true;
				}
				if (!flag4)
				{
					num = num3;
				}
			}
			int num4 = num2;
			string s = data.Substring(num4, num - num4);
			list.Add(new Token(TokenKind.Number, '\0', double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture)));
		}
		return list;
	}

	private static bool _IsCommand(char ch)
	{
		switch (ch)
		{
		case 'A':
		case 'C':
		case 'H':
		case 'L':
		case 'M':
		case 'Q':
		case 'S':
		case 'T':
		case 'V':
		case 'Z':
		case 'a':
		case 'c':
		case 'h':
		case 'l':
		case 'm':
		case 'q':
		case 's':
		case 't':
		case 'v':
		case 'z':
			return true;
		default:
			return false;
		}
	}
}
