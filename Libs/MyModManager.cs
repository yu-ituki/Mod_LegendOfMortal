using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using Lean.Localization;
using Mortal.Core;
using BepInEx.Unity.Mono;

namespace Mod
{
	[HarmonyPatch]
	/// <summary>
	/// Mod管理クラス.
	/// </summary>
	public class MyModManager : Singleton<MyModManager>
	{
		public enum eState {
			None,
			Initializing,
			Idle,
		}

		BaseUnityPlugin m_Plugin;
		string m_ModName;
		string m_ModFullName;
		string m_ModVersion;
		Harmony m_Harmony;

		eState m_State;
		ModConfigBase m_Config;

		System.Action m_OnBoot;


		/// <summary>
		/// 初期化.
		/// </summary>
		/// <typeparam name="ModConfigType"></typeparam>
		/// <param name="plugin"></param>
		/// <param name="logger"></param>
		/// <param name="modFullName"></param>
		/// <param name="modName"></param>
		/// <param name="modVersion"></param>
		public void Initialize<ModConfigType>(
            BaseUnityPlugin plugin,
            BepInEx.Logging.ManualLogSource logger, 
            string modFullName, 
            string modName, 
            string modVersion
        ) 
            where ModConfigType : ModConfigBase, new()
        {
			m_State = eState.Initializing;
			m_Plugin = plugin;
			m_ModName = modName;
			m_ModFullName = modFullName;
			m_ModVersion = modVersion;

			DebugUtil.Initialize(logger);
			CommonUtil.Initialize(plugin.Info);
			
			m_Config = new ModConfigType();
			m_Config.Initialize(plugin.Config);

			m_Harmony = new Harmony(modFullName);
			m_Harmony.PatchAll();

			RegisterOnBootAction(
				() => {
					eLanguage lang = _CalcCurrentLanguage();
					ModTextManager.Instance.Initialize(lang);
				}
			);
		}

		eLanguage _CalcCurrentLanguage() {
			string currentLang = LeanLocalization.CurrentLanguage;
			if (string.IsNullOrEmpty(currentLang))
			{
				return eLanguage.EN;
			}

			// LeanLocalization で設定されている言語名またはコードで分岐
			switch ( currentLang.ToLower() ) {
				case "japanese":
				case "ja":
					return eLanguage.JP; //日本語.
				case "english":
				case "en":
					return eLanguage.EN; //英語.
				case "chinese (simplified)":
				case "chinese_simplified":
				case "zh-cn":
					return eLanguage.ZH_CN; //中国語(簡体字).
				case "chinese (traditional)":
				case "chinese_traditional":
				case "zh-tw":
					return eLanguage.ZH_TW; //中国語(繁体字).
				case "korean":
				case "ko":
					return eLanguage.KO; //韓国語.
				case "italian":
				case "it":
					return eLanguage.IT; //イタリア語.
				case "spanish":
				case "es":
					return eLanguage.ES; //スペイン語.
				case "german":
				case "de":
					return eLanguage.DE; //ドイツ語.
				case "french":
				case "fr":
					return eLanguage.FR; //フランス語.
				case "portuguese":
				case "pt":
					return eLanguage.PT; //ポルトガル語.
				case "russian":
				case "ru":
					return eLanguage.RU; //ロシア語.
				default:
					return eLanguage.EN;
			}
		}

		/// <summary>
		/// 終了処理.
		/// </summary>
		public void Terminate() {
			m_Harmony?.UnpatchSelf();
			m_Harmony = null;
		}

		/// <summary>
		/// コンフィグ取得.
		/// </summary>
		/// <returns></returns>
		public ModConfigBase GetConfig() {
			return m_Config;
		}

		/// <summary>
		/// HarmonyPatch動的追加.
		/// </summary>
		/// <param name="info"></param>
		public void AddPatch( ModPatchInfo info ) {
			info.Patch(m_Harmony);
		}

		/// <summary>
		/// HarmonyPatch動的削除.
		/// </summary>
		/// <param name="info"></param>
		public void RemovePatch(ModPatchInfo info ) {
			info.Unpatch(m_Harmony);
		}

		/// <summary>
		/// ゲーム開始時コールバック登録.
		/// </summary>
		/// <param name="callback"></param>
		public void RegisterOnBootAction(System.Action callback) {
			m_OnBoot += callback;
		}


		// 以下コールバック群....
		//------
		[HarmonyPatch(typeof(SaveSystem), "LoadUniverseData")]
		[HarmonyPostfix]
		static void PostFix_OnLoadComplete() {
			if (Instance.m_State == eState.Idle)
				return;
			Instance.m_State = eState.Idle;
			Instance.m_OnBoot?.Invoke();
		}
	}
}
