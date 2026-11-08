using System.Collections.Generic;

namespace PCL.Core.Logging;

public static class LogLevelExtensions
{
	extension(LogLevel level)
	{
		public string PrintName()
		{
			return _LevelNameMap[level];
		}

		public ActionLevel DefaultActionLevel()
		{
			return (ActionLevel)((int)level % 100);
		}

		public LogLevel RealLevel()
		{
			if (level < (LogLevel)400)
			{
				if (level < (LogLevel)200)
				{
					if (level < (LogLevel)100)
					{
						return LogLevel.Trace;
					}
					return LogLevel.Debug;
				}
				if (level < (LogLevel)300)
				{
					return LogLevel.Info;
				}
				return LogLevel.Warning;
			}
			if (level < (LogLevel)500)
			{
				return LogLevel.Error;
			}
			return LogLevel.Fatal;
		}

		public int Header()
		{
			return (int)level / 100 * 100;
		}
	}

	private static readonly Dictionary<LogLevel, string> _LevelNameMap = new Dictionary<LogLevel, string>
	{
		[LogLevel.Trace] = "TRA",
		[LogLevel.Debug] = "DBG",
		[LogLevel.Info] = "INFO",
		[LogLevel.Warning] = "WARN",
		[LogLevel.Error] = "ERR!",
		[LogLevel.Fatal] = "FTL!"
	};
}
