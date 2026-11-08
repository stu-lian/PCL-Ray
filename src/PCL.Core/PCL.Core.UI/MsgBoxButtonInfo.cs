using System;

namespace PCL.Core.UI;

public record MsgBoxButtonInfo(string Context, int Value = 0, Action? OnClick = null);
