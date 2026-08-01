using BepInEx.Configuration;
using UnityEngine;

namespace Mod
{
	public class ModConfig : ModConfigBase
	{
		public ConfigEntry<KeyCode> QuickSaveKey { get; private set; }
		public ConfigEntry<KeyCode> QuickLoadKey { get; private set; }

		public override void Initialize(ConfigFile config)
		{
			QuickSaveKey = config.Bind("Keybinds", "QuickSaveKey", KeyCode.F6, "Quick saveに割り当てるキー");
			QuickLoadKey = config.Bind("Keybinds", "QuickLoadKey", KeyCode.F9, "Quick loadに割り当てるキー");
		}
	}
}