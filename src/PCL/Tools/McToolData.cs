using System.Collections.Generic;

namespace PCL;

public readonly record struct McDye(string Key, string Chinese, int ColorId, uint Rgb)
{
	public string Hex => $"#{Rgb & 0xFFFFFF:X6}";
}

public readonly record struct McPattern(string Key, string Chinese, string LegacyCode, bool Emblem, string UnlockHint)
{
	public string Namespace => "minecraft:" + Key;
}

public readonly record struct McEntity(string Key, string Chinese, bool IsBabyTag);

public readonly record struct TrimPattern(string Key, string Chinese, string Source, string CopyBlock, string Since);

public readonly record struct TrimMaterial(string Key, string Chinese, string[] Palette);

public static class McToolData
{
	public static readonly McDye[] Dyes =
	{
		new("white", "白色", 0, 0xF9FFFE), new("orange", "橙色", 1, 0xF9801D),
		new("magenta", "品红色", 2, 0xC74EBD), new("light_blue", "淡蓝色", 3, 0x3AB3DA),
		new("yellow", "黄色", 4, 0xFED83D), new("lime", "黄绿色", 5, 0x80C71F),
		new("pink", "粉红色", 6, 0xF38BAA), new("gray", "灰色", 7, 0x474F52),
		new("light_gray", "淡灰色", 8, 0x9D9D97), new("cyan", "青色", 9, 0x169C9C),
		new("purple", "紫色", 10, 0x8932B8), new("blue", "蓝色", 11, 0x3C44AA),
		new("brown", "棕色", 12, 0x835432), new("green", "绿色", 13, 0x5E7C16),
		new("red", "红色", 14, 0xB02E26), new("black", "黑色", 15, 0x1D1D21)
	};

	public static readonly McPattern[] Patterns =
	{
		new("square_bottom_left", "左下方块", "bl", false, ""),
		new("square_bottom_right", "右下方块", "br", false, ""),
		new("square_top_left", "左上方块", "tl", false, ""),
		new("square_top_right", "右上方块", "tr", false, ""),
		new("stripe_bottom", "底部条纹", "bs", false, ""),
		new("stripe_top", "顶部条纹", "ts", false, ""),
		new("stripe_left", "左侧条纹", "ls", false, ""),
		new("stripe_right", "右侧条纹", "rs", false, ""),
		new("stripe_center", "中间竖条纹", "cs", false, ""),
		new("stripe_middle", "中间横条纹", "ms", false, ""),
		new("stripe_downright", "右斜条纹", "drs", false, ""),
		new("stripe_downleft", "左斜条纹", "dls", false, ""),
		new("small_stripes", "密集条纹", "ss", false, ""),
		new("cross", "交叉十字", "cr", false, ""),
		new("straight_cross", "正十字", "sc", false, ""),
		new("triangle_bottom", "底部三角", "bt", false, ""),
		new("triangle_top", "顶部三角", "tt", false, ""),
		new("triangles_bottom", "底部锯齿", "bts", false, ""),
		new("triangles_top", "顶部锯齿", "tts", false, ""),
		new("diagonal_left", "左对角分割", "ld", false, ""),
		new("diagonal_right", "右对角分割", "rud", false, ""),
		new("diagonal_up_left", "左上对角分割", "lud", false, ""),
		new("diagonal_up_right", "右上对角分割", "rd", false, ""),
		new("circle", "圆形", "mc", false, ""),
		new("rhombus", "菱形", "mr", false, ""),
		new("half_vertical", "左半旗帜", "vh", false, ""),
		new("half_vertical_right", "右半旗帜", "vhr", false, ""),
		new("half_horizontal", "上半旗帜", "hh", false, ""),
		new("half_horizontal_bottom", "下半旗帜", "hhb", false, ""),
		new("border", "边框", "bo", false, ""),
		new("gradient", "向下渐变", "gra", false, ""),
		new("gradient_up", "向上渐变", "gru", false, ""),
		new("bricks", "砖纹", "bri", true, "纸 + 砖块合成"),
		new("curly_border", "波纹边框", "cbo", true, "纸 + 藤蔓合成"),
		new("creeper", "苦力怕盾徽", "cre", true, "纸 + 苦力怕头颅合成"),
		new("skull", "骷髅盾徽", "sku", true, "纸 + 凋灵骷髅头颅合成"),
		new("flower", "花朵盾徽", "flo", true, "纸 + 滨菊合成"),
		new("mojang", "Mojang 盾徽", "moj", true, "纸 + 附魔金苹果合成"),
		new("globe", "地球盾徽", "glb", true, "流浪商人交易"),
		new("piglin", "猪灵盾徽", "pig", true, "堡垒遗迹箱子"),
		new("flow", "流动盾徽", "flw", true, "试炼密室不祥宝库"),
		new("guster", "旋风盾徽", "gus", true, "试炼密室宝库")
	};

	public static readonly (string Key, string Chinese)[] ArmorPieces =
	{
		("helmet", "头盔"), ("chestplate", "胸甲"), ("leggings", "护腿"), ("boots", "靴子")
	};

	public static readonly (string Key, string Chinese, string Note)[] ArmorMaterials =
	{
		("leather", "皮革", ""), ("golden", "黄金", ""), ("chainmail", "锁链", ""),
		("iron", "铁", ""), ("diamond", "钻石", ""), ("netherite", "下界合金", ""),
		("copper", "铜", "1.21.9+")
	};

	public static readonly TrimPattern[] TrimPatterns =
	{
		new("sentry", "坚守", "掠夺者前哨站的箱子", "圆石", "1.19.4"),
		new("vex", "恼鬼", "林地府邸的箱子", "圆石", "1.19.4"),
		new("coast", "海岸", "沉船的箱子", "圆石", "1.19.4"),
		new("dune", "沙丘", "沙漠神殿的箱子", "砂岩", "1.19.4"),
		new("eye", "眼眸", "要塞的图书馆与祭坛箱子", "末地石", "1.19.4"),
		new("wayfinder", "向导", "掠夺路径的箱子", "陶瓦", "1.20"),
		new("raiser", "牧民", "掠夺路径的箱子", "陶瓦", "1.20"),
		new("shaper", "塑造", "沙漠水井周边的考古刷子", "陶瓦", "1.20"),
		new("host", "雇主", "沙漠神殿的考古刷子", "陶瓦", "1.20"),
		new("rib", "肋骨", "下界要塞的箱子", "下界岩", "1.19.4"),
		new("silence", "幽静（极稀有）", "远古城市的箱子", "深板岩圆石", "1.19.4"),
		new("ward", "监守", "远古城市的箱子", "深板岩圆石", "1.19.4"),
		new("snout", "猪鼻", "堡垒遗迹的箱子", "黑石", "1.19.4"),
		new("spire", "尖塔", "末地城的箱子", "紫珀块", "1.19.4"),
		new("tide", "潮汐", "海底遗迹的远古守卫者掉落", "海晶石", "1.19.4"),
		new("wild", "荒野", "丛林神庙的箱子", "苔石", "1.19.4"),
		new("flow", "涡流", "试炼密室的宝库", "旋风棒", "1.21"),
		new("bolt", "镶铆", "试炼密室的不祥宝库", "铜块（含涂蜡的）", "1.21")
	};

	public static readonly TrimMaterial[] TrimMaterials =
	{
		new("amethyst", "紫水晶", new[] { "紫水晶碎片" }),
		new("copper", "铜", new[] { "铜锭" }),
		new("diamond", "钻石", new[] { "钻石" }),
		new("emerald", "绿宝石", new[] { "绿宝石" }),
		new("gold", "金", new[] { "金锭" }),
		new("iron", "铁", new[] { "铁锭" }),
		new("lapis", "青金石", new[] { "青金石" }),
		new("netherite", "下界合金", new[] { "下界合金锭" }),
		new("quartz", "石英", new[] { "下界石英" }),
		new("redstone", "红石", new[] { "红石粉" }),
		new("resin", "树脂", new[] { "树脂砖" })
	};

	public static readonly string[] BeaconGlassOrder =
	{
		"white", "orange", "magenta", "light_blue", "yellow", "lime", "pink", "gray",
		"light_gray", "cyan", "purple", "blue", "brown", "green", "red", "black"
	};

	public static readonly McEntity[] HostileEntities =
	{
		new("zombie", "僵尸", true), new("husk", "尸壳", true), new("drowned", "溺尸", true),
		new("skeleton", "骷髅", false), new("stray", "流浪者", false), new("bogged", "沼骸", false),
		new("wither_skeleton", "凋灵骷髅", false), new("creeper", "苦力怕", false),
		new("spider", "蜘蛛", false), new("cave_spider", "洞穴蜘蛛", false),
		new("enderman", "末影人", false), new("slime", "史莱姆", false),
		new("magma_cube", "岩浆怪", false), new("blaze", "烈焰人", false),
		new("ghast", "恶魂", false), new("piglin", "猪灵", true),
		new("piglin_brute", "猪灵蛮兵", true), new("hoglin", "疣猪兽", true),
		new("zoglin", "僵尸疣猪兽", true), new("zombified_piglin", "僵尸猪灵", true),
		new("witch", "女巫", false), new("ravager", "劫掠兽", false),
		new("vex", "恼鬼", false), new("vindicator", "卫道士", false),
		new("evoker", "唤魔者", false), new("illusioner", "幻术师", false),
		new("pillager", "掠夺者", false), new("phantom", "幻翼", false),
		new("guardian", "守卫者", false), new("elder_guardian", "远古守卫者", false),
		new("shulker", "潜影贝", false), new("silverfish", "蠹虫", false),
		new("endermite", "末影螨", false), new("warden", "监守者", false),
		new("breeze", "旋风人", false), new("wither", "凋灵", false),
		new("ender_dragon", "末影龙", false), new("creaking", "嘎枝", false),
		new("giant", "巨人", false), new("zombie_villager", "僵尸村民", true),
		new("armor_stand", "盔甲架", false)
	};

	public static readonly McEntity[] PassiveEntities =
	{
		new("cow", "牛", true), new("pig", "猪", true), new("sheep", "羊", true),
		new("chicken", "鸡", true), new("rabbit", "兔子", true), new("horse", "马", true),
		new("donkey", "驴", true), new("mule", "骡", true), new("llama", "羊驼", true),
		new("trader_llama", "行商羊驼", true), new("cat", "猫", true), new("ocelot", "豹猫", true),
		new("wolf", "狼", true), new("fox", "狐狸", true), new("panda", "熊猫", true),
		new("polar_bear", "北极熊", true), new("turtle", "海龟", true), new("bee", "蜜蜂", true),
		new("strider", "炽足兽", true), new("glow_squid", "发光鱿鱼", true), new("squid", "鱿鱼", true),
		new("salmon", "鲑鱼", false), new("cod", "鳕鱼", false), new("tropical_fish", "热带鱼", false),
		new("pufferfish", "河豚", false), new("dolphin", "海豚", true), new("axolotl", "美西螈", true),
		new("goat", "山羊", true), new("allay", "悦灵", false), new("frog", "青蛙", true),
		new("tadpole", "蝌蚪", true), new("camel", "骆驼", true), new("sniffer", "嗅探兽", true),
		new("armadillo", "犰狳", true), new("happy_ghast", "快乐恶魂", true),
		new("mooshroom", "哞菇", true), new("villager", "村民", true),
		new("wandering_trader", "流浪商人", true), new("snow_golem", "雪傀儡", false),
		new("iron_golem", "铁傀儡", false), new("parrot", "鹦鹉", true), new("bat", "蝙蝠", true),
		new("skeleton_horse", "骷髅马", true), new("zombie_horse", "僵尸马", true)
	};

	public static McDye FindDye(string key)
	{
		foreach (McDye dye in Dyes)
		{
			if (dye.Key == key) return dye;
		}
		return Dyes[0];
	}

	public static McPattern FindPattern(string key)
	{
		foreach (McPattern pattern in Patterns)
		{
			if (pattern.Key == key) return pattern;
		}
		return Patterns[5];
	}
}
