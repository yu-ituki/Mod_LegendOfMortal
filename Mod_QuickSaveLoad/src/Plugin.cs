using BepInEx;
using BepInEx.Unity.Mono;
using HarmonyLib;
using Mortal.Core;
using System;
using System.Collections;
using UnityEngine;
using Fungus;
using System.Reflection;

namespace Mod
{
	[BepInPlugin(ModInfo.c_ModFullName, ModInfo.c_ModName, ModInfo.c_ModVersion)]
	public class Plugin : BaseUnityPlugin
	{
		private const string QUICK_SAVE_SLOT = "quicksave";

		public static Plugin Instance { get; private set; }

		public ModConfig ModConfig { get => MyModManager.Instance.GetConfig() as ModConfig; }

		// --- 通知表示用の変数 ---
		private string m_NotificationText = "";
		private bool m_IsShowingNotification = false;
		private Coroutine m_NotificationCoroutine;

		// --- 安全ロード用静的ガード（シーンを跨いでも絶対に破棄されない） ---
		private static bool s_IsLoadingLocked = false;

		private void Awake() {
			if (Instance != null) {
				Destroy(gameObject);
				return;
			}
			Instance = this;
			DontDestroyOnLoad(gameObject); // シーン遷移でコルーチンとガードが蒸発するのを防止

			MyModManager.Instance.Initialize<ModConfig>(this, this.Logger, ModInfo.c_ModFullName, ModInfo.c_ModName, ModInfo.c_ModVersion);
			MyModManager.Instance.RegisterOnBootAction(OnBoot);
		}

		void Unload() {
			MyModManager.Instance?.Terminate();
			MyModManager.DeleteInstance();
		}

		void OnBoot() {
			DebugUtil.LogWarning("Mortal QuickSaveLoad Mod Booted!");
		}

		public void Update() {
			if (SaveSystem.Instance == null || ModConfig == null)
				return;

			// ゲーム本体がロード中、またはクイックロード中は連打を完全無視
			if (s_IsLoadingLocked)
				return;

			var sc = SceneController.Instance;
			if (sc != null && (sc.IsPrepare || sc.IsLoading))
				return;

			if (Input.GetKeyDown(ModConfig.QuickSaveKey.Value))
				QuickSave();

			if (Input.GetKeyDown(ModConfig.QuickLoadKey.Value))
				ExecuteSafeLoad(QUICK_SAVE_SLOT);
		}

		public bool QuickSave() {
			var sc = SceneController.Instance;
			if (sc == null || sc.IsPrepare || sc.IsLoading || s_IsLoadingLocked) {
				return false;
			}

			string currentScene = sc.CurrentScene;
			if (currentScene == "Title" ||
				currentScene == "Battle" ||
				currentScene == "Combat" ||
				currentScene == "GameOver" ||
				currentScene == "End" ||
				currentScene == "DemoEnd") {
				ShowNotification($"[QuickSave] 現在のシーン('{currentScene}')ではセーブできません。");
				return false;
			}

			if (currentScene == "Story") {
				var dialog = Fungus.MenuDialog.ActiveMenuDialog;
				if (dialog == null || !dialog.gameObject.activeInHierarchy) {
					ShowNotification("[QuickSave] 会話中のためセーブできません。");
					return false;
				}

				bool isCustomMenu = dialog.GetComponent<Mortal.Core.CustomMenuDialog>() != null;
				bool hasBreakOptions = dialog.GetComponentInChildren<Mortal.Core.BreakOptionButton>() != null;
				if (!isCustomMenu && !hasBreakOptions) {
					ShowNotification("[QuickSave] 会話中のためセーブできません。");
					return false;
				}
			}

			try {
				SaveSystem.Instance.SaveGameData(QUICK_SAVE_SLOT);
				DebugUtil.Log($"[QuickSave] スロット '{QUICK_SAVE_SLOT}' にクイックセーブしました。");
				ShowNotification("finish quick save", 2.0f);
				return true;
			} catch (Exception ex) {
				ShowNotification($"[QuickSave] クイックセーブ失敗: {ex.Message}");
				return false;
			}
		}

		public bool QuickLoad() {
			return ExecuteSafeLoad(QUICK_SAVE_SLOT);
		}

		private bool ExecuteSafeLoad(string slot) {
			if (SaveSystem.Instance == null || s_IsLoadingLocked)
				return false;

			var sc = SceneController.Instance;
			if (sc != null && (sc.IsPrepare || sc.IsLoading))
				return false;

			StartCoroutine(SafeLoadCoroutine(slot));
			return true;
		}

		private IEnumerator SafeLoadCoroutine(string slot) {
			// ロード開始直後に即時ロック
			s_IsLoadingLocked = true;
			DebugUtil.Log("[QuickSave] SafeLoad: cleanup and start load pipeline...");

			// 1. 会話・ダイアログ・Flowchart の安全破棄
			CleanupBeforeLoad();
			yield return null;

			// 2. 公式ロードシーケンスの実行（例外が起きても finally で確実にロック解除）
			try {
				SaveSystem.Instance.SetSlot(slot);

				if (SoundManager.Instance != null)
					SoundManager.Instance.StopMusic();

				// ゲーム本体のセーブデータ読み込みと更新
				SaveSystem.Instance.LoadGameData();
				SaveSystem.Instance.SaveUniverseData();

				if (MissionManagerData.Instance != null)
					MissionManagerData.Instance.UpdateCheckMissions();

				// シーン再ロードを発火
				var sc = SceneController.Instance;
				if (sc != null)
					sc.LoadCurrentScene();

				DebugUtil.Log($"[QuickSave] SafeLoad: slot '{slot}' loaded.");
				ShowNotification("finish quick load", 2.0f);
			} catch (Exception ex) {
				DebugUtil.LogWarning($"[QuickSave] SafeLoad failed: {ex.Message}");
				ShowNotification($"[QuickSave] クイックロード失敗: {ex.Message}");
			}

			// 3. シーンの切り替え完了待機（IsPrepare / IsLoading が終わるまで確実に待つ）
			yield return new WaitForSecondsRealtime(0.1f);

			float timer = 0f;
			while (timer < 30f) {
				var sc = SceneController.Instance;
				if (sc != null && !sc.IsPrepare && !sc.IsLoading) {
					break;
				}
				timer += Time.unscaledDeltaTime;
				yield return null;
			}

			// ロード完了後の安定待ちインターバル（連打対策）
			yield return new WaitForSecondsRealtime(0.5f);

			// ガード解除
			s_IsLoadingLocked = false;
			DebugUtil.Log("[QuickSave] SafeLoad finished, lock released.");
		}

		private void CleanupBeforeLoad() {
			try {
				var say = Fungus.SayDialog.ActiveSayDialog;
				if (say != null) {
					try { say.StopAllCoroutines(); } catch { }
					try { say.gameObject.SetActive(false); } catch { }
				}

				var menu = Fungus.MenuDialog.ActiveMenuDialog;
				if (menu != null) {
					try { menu.StopAllCoroutines(); } catch { }
					try { menu.gameObject.SetActive(false); } catch { }
				}

				var flowcharts = GameObject.FindObjectsOfType<Fungus.Flowchart>();
				foreach (var fc in flowcharts) {
					try {
						var mi = fc.GetType().GetMethod("StopAllBlocks", BindingFlags.Public | BindingFlags.Instance);
						if (mi != null) {
							mi.Invoke(fc, null);
						} else {
							fc.StopAllCoroutines();
						}
					} catch { }
					try { fc.gameObject.SetActive(false); } catch { }
				}

				Time.timeScale = 1f;
			} catch { }
		}

		// ==========================================
		// UI通知表示用メソッド
		// ==========================================

		private void ShowNotification(string text, float duration = 1.0f) {
			if (m_NotificationCoroutine != null) {
				StopCoroutine(m_NotificationCoroutine);
			}
			m_NotificationCoroutine = StartCoroutine(Coroutine_ShowNotification(text, duration));
		}

		private IEnumerator Coroutine_ShowNotification(string text, float duration) {
			m_NotificationText = text;
			m_IsShowingNotification = true;

			yield return new WaitForSecondsRealtime(duration);

			m_IsShowingNotification = false;
			m_NotificationText = "";
			m_NotificationCoroutine = null;
		}

		private void OnGUI() {
			if (!m_IsShowingNotification || string.IsNullOrEmpty(m_NotificationText))
				return;

			GUIStyle style = new GUIStyle(GUI.skin.label) {
				fontSize = 20,
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = Color.white }
			};

			float width = 220f;
			float height = 45f;
			float x = (Screen.width - width) / 2f;
			float y = 35f;

			GUI.Box(new Rect(x, y, width, height), "");
			GUI.Label(new Rect(x, y, width, height), m_NotificationText, style);
		}
	}
}