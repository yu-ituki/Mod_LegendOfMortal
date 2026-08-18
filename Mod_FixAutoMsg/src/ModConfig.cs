using BepInEx.Configuration;

using UnityEngine;

namespace Mod
{
	public class ModConfig : ModConfigBase
	{
		public ConfigEntry<float> AutoSecondsPerChar { get; private set; }

		public override void Initialize(ConfigFile config) {
			AutoSecondsPerChar = config.Bind("AutoMessage", "SecondsPerChar", 0.08f, "表示完了後の読了待機時間（1文字あたりの秒数）");
		}
	}
}