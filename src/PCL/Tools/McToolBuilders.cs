using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PCL;

public sealed class SummonOptions
{
	public string Entity = "zombie";
	public string X = "~";
	public string Y = "~";
	public string Z = "~";
	public string Name = "";
	public bool NameVisible;
	public bool Glowing;
	public bool Invulnerable;
	public bool NoAi;
	public bool Silent;
	public int FireSeconds;
	public double Health;
	public bool Baby;
	public Dictionary<string, string> Equipment = new Dictionary<string, string>();
	public bool PassengerEnabled;
	public string PassengerEntity = "chicken";
	public string PassengerName = "";
	public bool PassengerGlowing;
	public bool PassengerNoAi;
}

public sealed class BannerPattern
{
	public string Key = "stripe_top";
	public string Color = "white";
}

public sealed class FireworkExplosion
{
	public string Shape = "small_ball";
	public List<uint> Colors = new List<uint>();
	public List<uint> FadeColors = new List<uint>();
	public bool Trail;
	public bool Twinkle;
}

public sealed class FireworkOptions
{
	public int Flight = 2;
	public List<FireworkExplosion> Explosions = new List<FireworkExplosion>();
}

public sealed class BeaconResult
{
	public List<string> Layers = new List<string>();
	public int[] Color = { 255, 255, 255 };
	public double Delta;
	public bool Exact;
}

public static class McToolBuilders
{
	private static readonly string[] EquipmentSlots = { "mainhand", "offhand", "head", "chest", "legs", "feet" };
	private static readonly Dictionary<string, string> SlotNames = new Dictionary<string, string>
	{
		{ "mainhand", "主手" }, { "offhand", "副手" }, { "head", "头部" },
		{ "chest", "胸部" }, { "legs", "腿部" }, { "feet", "脚部" }
	};

	private static string TextComponent(string text) => "{text:" + JsonSerializer.Serialize(text) + "}";
	private static string FormatNumber(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

	public static string BuildSummon(SummonOptions options)
	{
		string entity = string.IsNullOrWhiteSpace(options.Entity) ? "zombie" : options.Entity;
		List<string> tags = new List<string>();
		if (!string.IsNullOrWhiteSpace(options.Name))
		{
			tags.Add("CustomName:" + TextComponent(options.Name));
			if (options.NameVisible) tags.Add("CustomNameVisible:1b");
		}
		if (options.Glowing) tags.Add("Glowing:1b");
		if (options.Invulnerable) tags.Add("Invulnerable:1b");
		if (options.NoAi) tags.Add("NoAI:1b");
		if (options.Silent) tags.Add("Silent:1b");
		if (options.FireSeconds > 0) tags.Add("Fire:" + (options.FireSeconds * 20) + "s");
		if (options.Health > 0)
		{
			tags.Add("attributes:[{id:\"minecraft:max_health\",base:" + FormatNumber(options.Health) + "}]");
			tags.Add("Health:" + options.Health.ToString("0.0", CultureInfo.InvariantCulture) + "f");
		}
		if (options.Baby)
		{
			tags.Add(SupportsBabyTag(entity) ? "IsBaby:1b" : "Age:-1728000");
		}
		List<string> equipment = new List<string>();
		foreach (string slot in EquipmentSlots)
		{
			if (options.Equipment.TryGetValue(slot, out string? item) && !string.IsNullOrWhiteSpace(item))
			{
				equipment.Add(slot + ":{id:\"minecraft:" + item.Trim() + "\",count:1}");
			}
		}
		if (equipment.Count > 0) tags.Add("equipment:{" + string.Join(",", equipment) + "}");
		if (options.PassengerEnabled && !string.IsNullOrWhiteSpace(options.PassengerEntity))
		{
			List<string> passenger = new List<string> { "id:\"minecraft:" + options.PassengerEntity.Trim() + "\"" };
			if (!string.IsNullOrWhiteSpace(options.PassengerName)) passenger.Add("CustomName:" + TextComponent(options.PassengerName));
			if (options.PassengerGlowing) passenger.Add("Glowing:1b");
			if (options.PassengerNoAi) passenger.Add("NoAI:1b");
			tags.Add("Passengers:[{" + string.Join(",", passenger) + "}]");
		}
		string position = Coordinate(options.X) + " " + Coordinate(options.Y) + " " + Coordinate(options.Z);
		string suffix = tags.Count > 0 ? " {" + string.Join(",", tags) + "}" : "";
		return "/summon minecraft:" + entity + " " + position + suffix;
	}

	private static string Coordinate(string value) => string.IsNullOrWhiteSpace(value) ? "~" : value.Trim();

	private static bool SupportsBabyTag(string entity)
	{
		switch (entity)
		{
			case "zombie":
			case "husk":
			case "drowned":
			case "zombie_villager":
			case "zombified_piglin":
			case "piglin":
			case "piglin_brute":
			case "zoglin":
				return true;
			default:
				return false;
		}
	}

	public static string EquipmentSlotName(string slot) => SlotNames.TryGetValue(slot, out string? name) ? name : slot;

	private static string EscapeQuoted(string text) =>
		text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'");

	private static string LegacyName(string name) => "'{\"text\":\"" + EscapeQuoted(name.Trim()) + "\"}'";

	public static string BuildShieldGive(string baseColor, IReadOnlyList<BannerPattern> patterns, string name)
	{
		List<string> components = new List<string>();
		if (!string.IsNullOrWhiteSpace(name))
		{
			components.Add("minecraft:custom_name='" + JsonSerializer.Serialize(new Dictionary<string, string> { ["text"] = name.Trim() }) + "'");
		}
		components.Add("minecraft:base_color=\"" + baseColor + "\"");
		if (patterns.Count > 0)
		{
			string list = string.Join(",", patterns.Select(p => "{pattern:\"minecraft:" + p.Key + "\",color:\"" + p.Color + "\"}"));
			components.Add("minecraft:banner_patterns=[" + list + "]");
		}
		return "/give @p minecraft:shield[" + string.Join(",", components) + "]";
	}

	public static string BuildShieldLegacy(string baseColor, IReadOnlyList<BannerPattern> patterns, string name)
	{
		string display = string.IsNullOrWhiteSpace(name) ? "" : "display:{Name:" + LegacyName(name) + "},";
		string list = string.Join(",", patterns.Select(p => "{Pattern:\"" + McToolData.FindPattern(p.Key).LegacyCode + "\",Color:" + McToolData.FindDye(p.Color).ColorId + "}"));
		string patternTag = patterns.Count > 0 ? ",Patterns:[" + list + "]" : "";
		return "/give @p minecraft:shield{" + display + "BlockEntityTag:{Base:" + McToolData.FindDye(baseColor).ColorId + patternTag + "}} 1";
	}

	private static string IntArray(IReadOnlyList<uint> values) => "[I;" + string.Join(",", values.Select(v => unchecked((int)v).ToString(CultureInfo.InvariantCulture))) + "]";

	private static string ExplosionTag(FireworkExplosion explosion)
	{
		StringBuilder builder = new StringBuilder();
		builder.Append("{shape:\"").Append(explosion.Shape).Append("\"");
		builder.Append(",colors:").Append(IntArray(explosion.Colors.Count > 0 ? explosion.Colors : new List<uint> { 0 }));
		if (explosion.FadeColors.Count > 0) builder.Append(",fade_colors:").Append(IntArray(explosion.FadeColors));
		if (explosion.Trail) builder.Append(",has_trail:true");
		if (explosion.Twinkle) builder.Append(",has_twinkle:true");
		builder.Append('}');
		return builder.ToString();
	}

	public static string BuildFireworkRocket(FireworkOptions options)
	{
		string explosions = options.Explosions.Count == 0
			? "explosions:[]"
			: "explosions:[" + string.Join(",", options.Explosions.Select(ExplosionTag)) + "]";
		return "/give @s minecraft:firework_rocket[minecraft:fireworks={flight_duration:" + options.Flight + "," + explosions + "}] 1";
	}

	public static string BuildFireworkStar(FireworkExplosion explosion) =>
		"/give @s minecraft:firework_star[minecraft:firework_explosion=" + ExplosionTag(explosion) + "] 1";

	public static List<string> FireworkCraftingSteps(FireworkOptions options)
	{
		List<string> steps = new List<string>();
		for (int index = 0; index < options.Explosions.Count; index++)
		{
			FireworkExplosion explosion = options.Explosions[index];
			List<string> ingredients = new List<string> { "火药×1" };
			foreach (uint color in explosion.Colors)
			{
				McDye dye = McToolData.Dyes.FirstOrDefault(d => d.Rgb == color);
				ingredients.Add((string.IsNullOrEmpty(dye.Key) ? "自定义" : dye.Chinese) + "染料×1");
			}
			string? shapeItem = ShapeItem(explosion.Shape);
			if (!string.IsNullOrEmpty(shapeItem)) ingredients.Add(shapeItem + "×1");
			if (explosion.Trail) ingredients.Add("钻石×1（拖曳）");
			if (explosion.Twinkle) ingredients.Add("荧石粉×1（闪烁）");
			int slots = 1 + explosion.Colors.Count + (string.IsNullOrEmpty(shapeItem) ? 0 : 1) + (explosion.Trail ? 1 : 0) + (explosion.Twinkle ? 1 : 0);
			steps.Add(slots > 9
				? $"烟火之星 {index + 1}：这套配料占 {slots} 格，超过工作台的 9 格，只能用上面的指令获取"
				: $"烟火之星 {index + 1}：{string.Join(" + ", ingredients)}");
			if (explosion.FadeColors.Count > 0)
			{
				string fade = string.Join(" + ", explosion.FadeColors.Select(c => (McToolData.Dyes.FirstOrDefault(d => d.Rgb == c).Chinese ?? "自定义") + "染料×1"));
				steps.Add($"烟火之星 {index + 1} 淡化：再合成一次并加入 {fade}");
			}
		}
		int total = 1 + options.Flight + options.Explosions.Count;
		string rocket = $"烟花火箭：纸×1 + 火药×{options.Flight}（飞行 {options.Flight}） + 烟火之星×{options.Explosions.Count}";
		steps.Add(total > 9
			? $"{rocket} 共 {total} 格，超过 9 格——飞行 {options.Flight} 时最多放 {8 - options.Flight} 颗星，再多只能用指令"
			: rocket);
		return steps;
	}

	private static string? ShapeItem(string shape) => shape switch
	{
		"large_ball" => "火焰弹",
		"star" => "金粒",
		"creeper" => "任意头颅",
		"burst" => "羽毛",
		_ => null
	};

	public static string ShapeName(string shape) => shape switch
	{
		"small_ball" => "小球",
		"large_ball" => "大球",
		"star" => "星形",
		"creeper" => "苦力怕",
		"burst" => "喷发",
		_ => shape
	};

	public static BeaconResult SolveBeacon(int[] target, int maxLayers, IReadOnlyList<string> palette)
	{
		BeaconResult best = new BeaconResult { Color = new[] { 255, 255, 255 }, Delta = Distance(new[] { 255, 255, 255 }, target) };
		best.Exact = best.Delta == 0;
		List<string> current = new List<string>();
		void Walk(int remaining)
		{
			if (remaining == 0) return;
			foreach (string key in palette)
			{
				current.Add(key);
				int[] color = Simulate(current);
				double delta = Distance(color, target);
				if (delta < best.Delta || (delta == best.Delta && current.Count < best.Layers.Count))
				{
					best = new BeaconResult { Layers = new List<string>(current), Color = color, Delta = delta, Exact = delta == 0 };
				}
				if (current.Count < maxLayers) Walk(remaining - 1);
				current.RemoveAt(current.Count - 1);
			}
		}
		for (int length = 1; length <= maxLayers; length++) Walk(length);
		return best;
	}

	public static int[] Simulate(IReadOnlyList<string> layers)
	{
		double[]? color = null;
		foreach (string key in layers)
		{
			McDye dye = McToolData.FindDye(key);
			int[] rgb = { (int)(dye.Rgb >> 16) & 255, (int)(dye.Rgb >> 8) & 255, (int)dye.Rgb & 255 };
			if (color == null) color = new double[] { rgb[0], rgb[1], rgb[2] };
			else color = new[] { (color[0] + rgb[0]) / 2.0, (color[1] + rgb[1]) / 2.0, (color[2] + rgb[2]) / 2.0 };
		}
		if (color == null) return new[] { 255, 255, 255 };
		return new[] { (int)Math.Round(color[0]), (int)Math.Round(color[1]), (int)Math.Round(color[2]) };
	}

	private static double Distance(int[] left, int[] right)
	{
		double sum = 0;
		for (int index = 0; index < 3; index++)
		{
			double diff = left[index] - right[index];
			sum += diff * diff;
		}
		return Math.Sqrt(sum);
	}

	public static string ColorHex(int[] color) => "#" + color[0].ToString("X2") + color[1].ToString("X2") + color[2].ToString("X2");

	public static string BuildTrimGive(string piece, string pattern, string material, string armorMaterial, string target)
	{
		string selector = string.IsNullOrWhiteSpace(target) ? "@p" : target.Trim();
		string patternId = pattern.Contains(':') ? pattern : "minecraft:" + pattern;
		string materialId = material.Contains(':') ? material : "minecraft:" + material;
		return "/give " + selector + " minecraft:" + armorMaterial + "_" + piece +
			"[minecraft:trim={pattern:\"" + patternId + "\",material:\"" + materialId + "\"}] 1";
	}

	public sealed class TitleSegment
	{
		public string Text = "";
		public string Color = "white";
		public bool Bold;
		public bool Italic;
	}

	public static readonly (string Key, string Chinese, string Hex)[] TitleColors =
	{
		("white", "白色", "#FFFFFF"), ("gray", "灰色", "#AAAAAA"), ("dark_gray", "深灰", "#555555"),
		("black", "黑色", "#000000"), ("red", "红色", "#FF5555"), ("dark_red", "深红", "#AA0000"),
		("gold", "金色", "#FFAA00"), ("yellow", "黄色", "#FFFF55"), ("green", "绿色", "#55FF55"),
		("dark_green", "深绿", "#00AA00"), ("aqua", "青色", "#55FFFF"), ("dark_aqua", "深青", "#00AAAA"),
		("blue", "蓝色", "#5555FF"), ("dark_blue", "深蓝", "#0000AA"),
		("light_purple", "淡紫", "#FF55FF"), ("dark_purple", "深紫", "#AA00AA")
	};

	public static string TitleColorHex(string key)
	{
		foreach ((string k, string _, string hex) in TitleColors)
		{
			if (k == key) return hex;
		}
		return "#FFFFFF";
	}

	public static string SegmentJson(TitleSegment segment)
	{
		StringBuilder builder = new StringBuilder();
		builder.Append("{\"text\":").Append(JsonSerializer.Serialize(segment.Text));
		if (!string.IsNullOrEmpty(segment.Color) && segment.Color != "white") builder.Append(",\"color\":\"").Append(segment.Color).Append('"');
		if (segment.Bold) builder.Append(",\"bold\":true");
		if (segment.Italic) builder.Append(",\"italic\":true");
		builder.Append('}');
		return builder.ToString();
	}

	public static List<string> BuildTitleCommands(string target, TitleSegment title, TitleSegment subtitle, TitleSegment actionBar, int fadeIn, int stay, int fadeOut)
	{
		string selector = string.IsNullOrWhiteSpace(target) ? "@a" : target.Trim();
		List<string> commands = new List<string>();
		// times 只在用户改了默认值时生成，避免只填标题时也多出一条无用指令。
		if (fadeIn != 10 || stay != 70 || fadeOut != 20)
		{
			commands.Add("/title " + selector + " times " + fadeIn + " " + stay + " " + fadeOut);
		}
		// 主标题与副标题在 Java 版中必须分别用 title / subtitle 两条子命令设置，无法合并。
		if (!string.IsNullOrWhiteSpace(subtitle.Text)) commands.Add("/title " + selector + " subtitle " + SegmentJson(subtitle));
		if (!string.IsNullOrWhiteSpace(title.Text)) commands.Add("/title " + selector + " title " + SegmentJson(title));
		if (!string.IsNullOrWhiteSpace(actionBar.Text)) commands.Add("/title " + selector + " actionbar " + SegmentJson(actionBar));
		return commands;
	}
}
