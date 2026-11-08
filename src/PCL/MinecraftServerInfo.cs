namespace PCL;

public class MinecraftServerInfo
{
	public string Name { get; set; }

	public string Address { get; set; }

	public int Port { get; set; } = 25565;

	public ServerStatus Status { get; set; }

	public int PlayerCount { get; set; }

	public int MaxPlayers { get; set; }

	public string Description { get; set; } = "";

	public string Version { get; set; } = "";

	public int Ping { get; set; }

	public string Icon { get; set; } = "";
}
