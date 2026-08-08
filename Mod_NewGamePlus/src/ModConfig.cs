using BepInEx.Configuration;
using UnityEngine;

namespace Mod
{
	/// <summary>
	/// Modコンフィグ用.
	/// </summary>
	public class ModConfig : ModConfigBase
	{
		public ConfigEntry<KeyCode> NewGamePlusKey { get; private set; }

		public override void Initialize(ConfigFile config)
		{
			NewGamePlusKey = config.Bind("Keybinds", "NewGamePlusKey", KeyCode.F1, "ロード画面で選択したセーブデータからNewGamePlusを開始するキー");
		}
	}
}
