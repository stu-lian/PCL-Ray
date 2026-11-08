using System;

namespace PCL.Core.Logging;

public delegate void LogHandler(LogLevel level, string msg, string? module = null, Exception? ex = null);
