using System.Collections.Generic;

namespace PCL.Core.UI;

public delegate void MsgBoxHandler(string message, string caption, ICollection<MsgBoxButtonInfo> buttons, MsgBoxTheme theme, bool block, ref int result);
