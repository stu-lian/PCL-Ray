namespace PCL.Core.Minecraft.Profile.Authentication.Utils.Models;

public record XboxAuthenticate<T>
{
	public required T Properties { get; set; }

	public string RelyingParty { get; set; }

	public required string TokenType { get; set; }
}
