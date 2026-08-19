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

		// GUI用変数 (基準解像度 1920x1080 に基づく座標とサイズ)
		private const float BaseWidth = 1920f;
		private const float BaseHeight = 1080f;
		private bool _showConfigWindow = false;
		private Rect _windowRect = new Rect(50, 50, 420, 220);

		private void Awake() {
			Instance = this;
			MyModManager.Instance.Initialize<ModConfig>(this, Logger, ModInfo.c_ModFullName, ModInfo.c_ModName, ModInfo.c_ModVersion);
			MyModManager.Instance.RegisterOnBootAction(OnBoot);
		}

		private void Update() {
			if (ModConfig != null && Input.GetKeyDown(ModConfig.ToggleMenuKey.Value)) {
				_showConfigWindow = !_showConfigWindow;
			}
		}

		private void OnGUI() {
			if (!_showConfigWindow || ModConfig == null) return;

			// 元の GUI.matrix を退避
			Matrix4x4 originalMatrix = GUI.matrix;

			// 解像度に応じたスケーリング行列を計算・適用
			float scaleX = Screen.width / BaseWidth;
			float scaleY = Screen.height / BaseHeight;
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleX, scaleY, 1.0f));

			// スケーリングされた仮想座標系でウィンドウを描画
			_windowRect = GUI.Window(9999, _windowRect, DrawConfigWindow, "Fix Auto Msg Speed");

			// GUI.matrix を元に戻す
			GUI.matrix = originalMatrix;
		}

		private void DrawConfigWindow(int windowId) {
			// 解像度スケールに合わせたフォント・コントロールサイズの設定
			GUIStyle titleStyle = new GUIStyle(GUI.skin.label) {
				fontSize = 20,
				fontStyle = FontStyle.Bold
			};

			GUIStyle valueStyle = new GUIStyle(GUI.skin.label) {
				fontSize = 18,
				alignment = TextAnchor.MiddleLeft
			};

			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) {
				fontSize = 18
			};

			GUILayout.Space(15);

			float currentVal = ModConfig.AutoSecondsPerChar.Value;
			GUILayout.Label($"1文字あたりの秒数: {currentVal:F3}s", valueStyle);

			GUILayout.Space(10);

			// スライダーの描画（高さを少し広げて操作性を向上）
			float newVal = GUILayout.HorizontalSlider(currentVal, 0.005f, 0.7f, GUILayout.Height(30));

			if (Math.Abs(newVal - currentVal) > 0.0001f) {
				ModConfig.AutoSecondsPerChar.Value = (float)Math.Round(newVal, 3);
				Config.Save();
			}

			GUILayout.Space(15);

			if (GUILayout.Button("Close", buttonStyle, GUILayout.Height(36))) {
				_showConfigWindow = false;
			}

			// ウィンドウのドラッグ領域設定
			GUI.DragWindow(new Rect(0, 0, 10000, 30));
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
			while (continueButton != null && !continueButton.gameObject.activeInHierarchy) {
				yield return null;
			}

			float waitTime = CalculateWaitSeconds(text);
			yield return new WaitForSecondsRealtime(waitTime);

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
			while (true) {
				bool moved = false;
				try { moved = original.MoveNext(); } catch { yield break; }
				if (!moved) break;
				yield return original.Current;
			}

			if (writerMb == null) yield break;

			var textComp = writerMb.GetComponentInChildren<Text>(true);
			float waitTime = CalculateWaitSeconds(textComp != null ? textComp.text : null);
			yield return new WaitForSecondsRealtime(waitTime);

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