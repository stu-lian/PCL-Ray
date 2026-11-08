using System;

namespace PCL.Core.Minecraft.IdentityModel;

public class IdentityModelConfigurationException : IdentityModelException
{
	public IdentityModelConfigurationException(string message, Exception? innerException = null)
		: base(message, innerException)
	{
	}
}
