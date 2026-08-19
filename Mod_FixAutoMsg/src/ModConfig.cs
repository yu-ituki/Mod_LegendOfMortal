using BepInEx.Configuration;
using UnityEngine;

namespace Mod
{
	public class ModConfig : ModConfigBase
	{
		public ConfigEntry<float> AutoSecondsPerChar { get; private set; }
		public ConfigEntry<KeyCode> ToggleMenuKey { get; private set; }

		public override void Initialize(ConfigFile config) {
			AutoSecondsPerChar = config.Bind(
				"AutoMessage", 
				"SecondsPerChar", 
				0.2f, 
				"表示完了後の読了待機時間（1文字あたりの秒数）"
			);

			ToggleMenuKey = config.Bind(
				"General", 
				"ToggleMenuKey", 
				KeyCode.F1, 
				"設定ウィンドウを開閉するキー"
			);
		}
	}
}