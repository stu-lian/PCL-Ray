using System;
using System.Collections.Generic;
using System.Numerics;

namespace PCL;

internal sealed class MavenComparableVersion : IComparable<MavenComparableVersion>
{
	private interface IItem
	{
		int Type { get; }

		bool IsNull { get; }

		int CompareTo(IItem other);
	}

	private sealed class NumericItem : IItem
	{
		public static readonly NumericItem Zero = new NumericItem("0");

		private readonly BigInteger _value;

		public int Type => 0;

		public bool IsNull => _value.IsZero;

		public NumericItem(string value)
		{
			_value = BigInteger.Parse(_StripLeadingZeroes(value));
		}

		public int CompareTo(IItem other)
		{
			if (other == null)
			{
				return (!_value.IsZero) ? 1 : 0;
			}
			switch (other.Type)
			{
			case 0:
				return _value.CompareTo(((NumericItem)other)._value);
			case 1:
			case 2:
			case 3:
				return 1;
			default:
				throw new InvalidOperationException("Invalid Maven version item");
			}
		}
	}

	private sealed class StringItem : IItem
	{
		private static readonly string[] Qualifiers = new string[7] { "alpha", "beta", "milestone", "rc", "snapshot", "", "sp" };

		private const string ReleaseIndex = "5";

		private readonly string _value;

		public int Type => 1;

		public bool IsNull => _value.Length == 0;

		public StringItem(string value, bool followedByDigit)
		{
			if (followedByDigit && value.Length == 1)
			{
				value = value switch
				{
					"a" => "alpha", 
					"b" => "beta", 
					"m" => "milestone", 
					_ => value, 
				};
			}
			_value = ((value == "cr") ? "rc" : value);
		}

		private static string _ComparableQualifier(string qualifier)
		{
			bool flag;
			switch (qualifier)
			{
			case "ga":
			case "final":
			case "release":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				return "5";
			}
			int num = Array.IndexOf(Qualifiers, qualifier);
			if (num >= 0)
			{
				return num.ToString();
			}
			return Qualifiers.Length + "-" + qualifier;
		}

		public int CompareTo(IItem other)
		{
			if (other == null)
			{
				return string.CompareOrdinal(_ComparableQualifier(_value), "5");
			}
			return other.Type switch
			{
				0 => -1, 
				1 => string.CompareOrdinal(_ComparableQualifier(_value), _ComparableQualifier(((StringItem)other)._value)), 
				2 => -other.CompareTo(this), 
				3 => _CompareCombination((CombinationItem)other), 
				_ => throw new InvalidOperationException("Invalid Maven version item"), 
			};
		}

		private int _CompareCombination(CombinationItem other)
		{
			int num = CompareTo(other.StringPart);
			if (num != 0)
			{
				return num;
			}
			if (CompareTo(null) != 0)
			{
				return -1;
			}
			return -other.DigitPart.CompareTo(null);
		}
	}

	private sealed class CombinationItem : IItem
	{
		public StringItem StringPart { get; }

		public IItem DigitPart { get; }

		public int Type => 3;

		public bool IsNull => false;

		public CombinationItem(string value)
		{
			int i;
			for (i = 0; i < value.Length && !char.IsDigit(value[i]); i++)
			{
			}
			StringPart = new StringItem(value.Substring(0, i), followedByDigit: true);
			DigitPart = _ParseItem(combination: false, digit: true, value.Substring(i));
		}

		public int CompareTo(IItem other)
		{
			if (other == null)
			{
				int num = StringPart.CompareTo(null);
				if (num != 0)
				{
					return num;
				}
				return DigitPart.CompareTo(null);
			}
			return other.Type switch
			{
				0 => -1, 
				1 => _CompareString((StringItem)other), 
				2 => -other.CompareTo(this), 
				3 => _CompareCombination((CombinationItem)other), 
				_ => throw new InvalidOperationException("Invalid Maven version item"), 
			};
		}

		private int _CompareString(StringItem other)
		{
			int num = StringPart.CompareTo(other);
			if (num != 0)
			{
				return num;
			}
			if (StringPart.CompareTo(null) != 0)
			{
				return 1;
			}
			return DigitPart.CompareTo(null);
		}

		private int _CompareCombination(CombinationItem other)
		{
			int num = StringPart.CompareTo(other.StringPart);
			if (num != 0)
			{
				return num;
			}
			return DigitPart.CompareTo(other.DigitPart);
		}
	}

	private sealed class ListItem : List<IItem>, IItem
	{
		public int Type => 2;

		public bool IsNull => Count == 0;

		public void Normalize()
		{
			for (int num = Count - 1; num >= 0; num--)
			{
				if (base[num].IsNull)
				{
					if (num == Count - 1 || base[num + 1].Type == 1)
					{
						RemoveAt(num);
					}
					else
					{
						ListItem listItem = base[num + 1] as ListItem;
						bool flag = listItem != null && listItem.Count > 0;
						if (flag)
						{
							int type = listItem[0].Type;
							bool flag2 = ((type == 1 || type == 3) ? true : false);
							flag = flag2;
						}
						if (flag)
						{
							RemoveAt(num);
						}
					}
				}
			}
			if (Count == 1 && base[0] is ListItem collection)
			{
				Clear();
				AddRange(collection);
			}
		}

		public int CompareTo(IItem other)
		{
			if (other == null)
			{
				using (List<IItem>.Enumerator enumerator = GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						int num = enumerator.Current.CompareTo(null);
						if (num != 0)
						{
							return num;
						}
					}
				}
				return 0;
			}
			if (other.Type == 0)
			{
				return -1;
			}
			int type = other.Type;
			if ((type == 1 || type == 3) ? true : false)
			{
				int num2 = ((Count == 0) ? (-other.CompareTo(null)) : base[0].CompareTo(other));
				int num3 = 1;
				while (num2 == 0 && num3 < Count)
				{
					num2 = base[num3].CompareTo(null);
					num3++;
				}
				return num2;
			}
			if (!(other is ListItem listItem))
			{
				throw new InvalidOperationException("Invalid Maven version item");
			}
			int num4 = Math.Max(Count, listItem.Count);
			for (int i = 0; i < num4; i++)
			{
				IItem item = ((i < Count) ? base[i] : null);
				IItem item2 = ((i < listItem.Count) ? listItem[i] : null);
				int num5 = item?.CompareTo(item2) ?? ((item2 != null) ? (-item2.CompareTo(null)) : 0);
				if (num5 != 0)
				{
					return num5;
				}
			}
			return 0;
		}
	}

	private const int MaxVersionLength = 256;

	private readonly ListItem _items = new ListItem();

	public MavenComparableVersion(string version)
	{
		if (version == null)
		{
			throw new ArgumentNullException("version");
		}
		if (version.Length > 256)
		{
			throw new ArgumentException("Maven version is too long", "version");
		}
		_Parse(version.ToLowerInvariant());
	}

	public int CompareTo(MavenComparableVersion? other)
	{
		if (other != null)
		{
			return _items.CompareTo(other._items);
		}
		return 1;
	}

	public static int Compare(string left, string right)
	{
		bool flag = left.Length > 256;
		bool flag2 = right.Length > 256;
		if (flag | flag2)
		{
			if (flag != flag2)
			{
				if (!flag)
				{
					return -1;
				}
				return 1;
			}
			return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
		}
		return new MavenComparableVersion(left).CompareTo(new MavenComparableVersion(right));
	}

	private void _Parse(string version)
	{
		ListItem listItem = _items;
		Stack<ListItem> stack = new Stack<ListItem>();
		stack.Push(listItem);
		bool flag = false;
		bool combination = false;
		int num = 0;
		for (int i = 0; i < version.Length; i++)
		{
			switch (version[i])
			{
			case '.':
			{
				ListItem listItem3 = listItem;
				IItem item;
				if (i != num)
				{
					item = _ParseItem(combination, flag, version.Substring(num, i - num));
				}
				else
				{
					IItem zero = NumericItem.Zero;
					item = zero;
				}
				listItem3.Add(item);
				combination = false;
				num = i + 1;
				break;
			}
			case '-':
				if (i == num)
				{
					listItem.Add(NumericItem.Zero);
				}
				else
				{
					if (!flag && i != version.Length - 1 && char.IsDigit(version[i + 1]))
					{
						combination = true;
						break;
					}
					listItem.Add(_ParseItem(combination, flag, version.Substring(num, i - num)));
				}
				num = i + 1;
				if (listItem.Count > 0)
				{
					ListItem listItem4 = new ListItem();
					listItem.Add(listItem4);
					listItem = listItem4;
					stack.Push(listItem);
				}
				combination = false;
				break;
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				if (!flag && i > num)
				{
					combination = true;
					if (listItem.Count > 0)
					{
						ListItem listItem5 = new ListItem();
						listItem.Add(listItem5);
						listItem = listItem5;
						stack.Push(listItem);
					}
				}
				flag = true;
				break;
			default:
				if (flag && i > num)
				{
					listItem.Add(_ParseItem(combination, digit: true, version.Substring(num, i - num)));
					num = i;
					ListItem listItem2 = new ListItem();
					listItem.Add(listItem2);
					listItem = listItem2;
					stack.Push(listItem);
					combination = false;
				}
				flag = false;
				break;
			}
		}
		if (version.Length > num)
		{
			if (!flag && listItem.Count > 0)
			{
				ListItem listItem6 = new ListItem();
				listItem.Add(listItem6);
				listItem = listItem6;
				stack.Push(listItem);
			}
			listItem.Add(_ParseItem(combination, flag, version.Substring(num)));
		}
		while (stack.Count > 0)
		{
			stack.Pop().Normalize();
		}
	}

	private static IItem _ParseItem(bool combination, bool digit, string value)
	{
		if (combination)
		{
			return new CombinationItem(value.Replace("-", ""));
		}
		if (!digit)
		{
			return new StringItem(value, followedByDigit: false);
		}
		return new NumericItem(value);
	}

	private static string _StripLeadingZeroes(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "0";
		}
		int i;
		for (i = 0; i < value.Length && value[i] == '0'; i++)
		{
		}
		if (i != value.Length)
		{
			return value.Substring(i);
		}
		return "0";
	}
}
