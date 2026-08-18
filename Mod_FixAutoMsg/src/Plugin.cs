using BepInEx;
using BepInEx.Unity.Mono;

using HarmonyLib;

using Mortal.Core;

using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;

namespace Mod
{
	[BepInPlugin(ModInfo.c_ModFullName, ModInfo.c_ModName, ModInfo.c_ModVersion)]
	public class Plugin : BaseUnityPlugin
	{
		public static Plugin Instance { get; private set; }
		public ModConfig ModConfig => MyModManager.Instance.GetConfig() as ModConfig;
		public static bool IsJpModLoaded { get; private set; }

		private static readonly Regex TagRegex = new Regex("<[^>]*?>", RegexOptions.Compiled);
		private static Coroutine _currentAutoRoutine;

		// キャッシュ用
		private static Type _fungusSayDialogType;
		private static Type _fungusWriterType;
		private static FieldInfo _continueButtonField;

		private void Awake() {
			Instance = this;
			MyModManager.Instance.Initialize<ModConfig>(this, Logger, ModInfo.c_ModFullName, ModInfo.c_ModName, ModInfo.c_ModVersion);
			MyModManager.Instance.RegisterOnBootAction(OnBoot);
		}

		private void OnBoot() {
			CacheReflectionInfo();

			IsJpModLoaded = CheckJpModLoaded();
			var harmony = new HarmonyLib.Harmony(ModInfo.c_ModFullName);

			if (IsJpModLoaded) {
				PatchForJpMod(harmony);
			} else {
				PatchForVanilla(harmony);
			}
		}

		private static void CacheReflectionInfo() {
			_fungusSayDialogType = AccessTools.TypeByName("Fungus.SayDialog");
			_fungusWriterType = AccessTools.TypeByName("Fungus.Writer");

			if (_fungusSayDialogType != null) {
				_continueButtonField = AccessTools.Field(_fungusSayDialogType, "continueButton");
			}
		}

		private static bool CheckJpModLoaded() {
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
				string name = asm.GetName().Name;
				if (name.IndexOf("LOM_JP", StringComparison.OrdinalIgnoreCase) >= 0 ||
					name.IndexOf("RubyPrototype", StringComparison.OrdinalIgnoreCase) >= 0) {
					return true;
				}
			}
			return false;
		}

		#region 日本語Mod環境 (SayDialog フック)
		private static void PatchForJpMod(HarmonyLib.Harmony harmony) {
			if (_fungusSayDialogType == null) return;

			var doSayMethod = AccessTools.Method(_fungusSayDialogType, "DoSay");
			if (doSayMethod != null) {
				harmony.Patch(doSayMethod, postfix: new HarmonyMethod(typeof(Plugin), nameof(OnDoSayPostfix_Jp)));
			}
		}

		private static void OnDoSayPostfix_Jp(object __instance, string text) {
			if (__instance == null || Instance == null) return;

			var btn = GetContinueButton(__instance);
			if (btn != null) {
				RestartAutoRoutine(AutoAdvanceRoutine_Jp(btn, text));
			}
		}

		private static IEnumerator AutoAdvanceRoutine_Jp(Button continueButton, string text) {
			// 1. タイピング描画完了（ボタンがアクティブになる）を待機
			while (continueButton != null && !continueButton.gameObject.activeInHierarchy) {
				yield return null;
			}

			// 2. 読了待機
			float waitTime = CalculateWaitSeconds(text);
			yield return new WaitForSecondsRealtime(waitTime);

			// 3. 次へ送る
			TryClickButton(continueButton);
			_currentAutoRoutine = null;
		}
		#endregion

		#region バニラ/通常環境 (Writer フック)
		private static void PatchForVanilla(HarmonyLib.Harmony harmony) {
			if (_fungusWriterType == null) return;

			var writeMethod = AccessTools.Method(_fungusWriterType, "Write");
			if (writeMethod != null) {
				harmony.Patch(writeMethod, postfix: new HarmonyMethod(typeof(Plugin), nameof(OnWritePostfix_Vanilla)));
			}
		}

		private static void OnWritePostfix_Vanilla(ref IEnumerator __result, object __instance) {
			if (__result == null || Instance == null) return;

			if (__instance is MonoBehaviour mb) {
				__result = RunWriterThenWait_Vanilla(__result, mb);
			}
		}

		private static IEnumerator RunWriterThenWait_Vanilla(IEnumerator original, MonoBehaviour writerMb) {
			// 1. 元のタイピング処理を完走させる
			while (true) {
				bool moved = false;
				try { moved = original.MoveNext(); } catch { yield break; }
				if (!moved) break;
				yield return original.Current;
			}

			if (writerMb == null) yield break;

			// 2. 読了待機
			var textComp = writerMb.GetComponentInChildren<Text>(true);
			float waitTime = CalculateWaitSeconds(textComp != null ? textComp.text : null);
			yield return new WaitForSecondsRealtime(waitTime);

			// 3. 次へ送る
			if (_fungusSayDialogType != null) {
				var sayDialog = writerMb.GetComponentInParent(_fungusSayDialogType);
				if (sayDialog != null) {
					TryClickButton(GetContinueButton(sayDialog));
				}
			}
		}
		#endregion

		#region 共通ヘルパー
		private static void RestartAutoRoutine(IEnumerator routine) {
			if (Instance == null) return;

			if (_currentAutoRoutine != null) {
				Instance.StopCoroutine(_currentAutoRoutine);
				_currentAutoRoutine = null;
			}

			_currentAutoRoutine = Instance.StartCoroutine(routine);
		}

		private static float CalculateWaitSeconds(string rawText) {
			int cleanLen = StripTags(rawText).Length;
			float perChar = Instance?.ModConfig?.AutoSecondsPerChar?.Value ?? 0.04f;
			return cleanLen * perChar;
		}

		private static string StripTags(string input) {
			if (string.IsNullOrEmpty(input)) return string.Empty;
			string result = TagRegex.Replace(input, string.Empty);
			return result.Replace("\r", "").Replace("\n", "");
		}

		private static Button GetContinueButton(object sayDialogInstance) {
			if (sayDialogInstance == null) return null;

			if (_continueButtonField != null) {
				return _continueButtonField.GetValue(sayDialogInstance) as Button;
			}
			return AccessTools.Field(sayDialogInstance.GetType(), "continueButton")?.GetValue(sayDialogInstance) as Button;
		}

		private static void TryClickButton(Button btn) {
			if (btn != null && btn.gameObject.activeInHierarchy) {
				btn.onClick.Invoke();
			}
		}
		#endregion
	}
}