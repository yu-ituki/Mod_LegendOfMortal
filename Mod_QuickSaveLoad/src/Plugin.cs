using BepInEx;
using BepInEx.Unity.Mono;
using HarmonyLib;
using Mortal.Core;
using System;
using System.Collections;
using UnityEngine;
using Fungus;


namespace Mod
{
	[BepInPlugin(ModInfo.c_ModFullName, ModInfo.c_ModName, ModInfo.c_ModVersion)]
	public class Plugin : BaseUnityPlugin
	{
		private const string QUICK_SAVE_SLOT = "quicksave";
		private static string[] c_SaveEnableDialogNames = new string[] {
			"TalkMenuDialog",
			"SectionFree01_MenuDialog",
		};

		public static Plugin Instance { get; private set; }

		public ModConfig ModConfig { get => MyModManager.Instance.GetConfig() as ModConfig; }

		// --- 通知表示用の変数 ---
		private string m_NotificationText = "";
		private bool m_IsShowingNotification = false;
		private Coroutine m_NotificationCoroutine;



		private void Awake() {
			Instance = this;
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

			if (Input.GetKeyDown(ModConfig.QuickSaveKey.Value))
				QuickSave();

			if (Input.GetKeyDown(ModConfig.QuickLoadKey.Value))
				QuickLoad();
		}
		
		/// <summary>
		/// クイックセーブの実行処理（内部で安全性をチェックして判定・弾く）
		/// </summary>
		/// <returns>セーブが正常に実行された場合は true、弾かれたまたは失敗した場合は false</returns>
		public bool QuickSave() {
			var sc = SceneController.Instance;
			if (sc == null) {
				DebugUtil.LogWarning("[QuickSave] SceneController が存在しないためセーブできません。");
				return false;
			}

			// 1. ロード中・準備中はセーブ不可
			if (sc.IsPrepare || sc.IsLoading) {
				DebugUtil.LogWarning("[QuickSave] シーン準備中またはロード中のためセーブできません。");
				return false;
			}

			// 2. シーンによる制限（会話中・タイトル画面・バトル中などはセーブ不可）
			string currentScene = sc.CurrentScene;
			if (currentScene == "Title" ||
				//currentScene == "Story" ||
				currentScene == "Battle" ||
				currentScene == "Combat" ||
				currentScene == "GameOver" ||
				currentScene == "End" ||
				currentScene == "DemoEnd") 
			{
				ShowNotification($"[QuickSave] 現在のシーン('{currentScene}')ではセーブできません。");
				return false;
			}
				
			// Story状態でも特定メニュー（SectionFree01_MenuDialog）のときだけ許可する
			if (currentScene == "Story") {
				var dialog = Fungus.MenuDialog.ActiveMenuDialog;

				// 判定ロジック:
				// 1. dialog が null、またはアクティブでない場合は不可
				// 2. 名前が "SectionFree01_MenuDialog" で始まらない場合は不可
				DebugUtil.LogWarning($"!!!!!!!!!!!!!!{dialog.name}");

				bool isActiveDialog = dialog != null && dialog.gameObject.activeInHierarchy;
				bool isTargetDialog = isActiveDialog && System.Array.Exists(c_SaveEnableDialogNames, (v) => dialog.name.StartsWith(v));
				if (!isTargetDialog) {
					ShowNotification("[QuickSave] 会話中のためセーブできません。");
					return false;
				}
			}
			try {
				SaveSystem.Instance.SaveGameData(QUICK_SAVE_SLOT);
				DebugUtil.Log($"[QuickSave] スロット '{QUICK_SAVE_SLOT}' にクイックセーブしました。");

				// --- 成功時の通知コルーチン開始 ---
				ShowNotification("finish quick save", 2.0f);

				return true;
			} catch (System.Exception ex) {
				ShowNotification($"[QuickSave] クイックセーブ失敗: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// クイックロードの実行処理（内部で安全性をチェックして判定・弾く）
		/// </summary>
		/// <returns>ロードが正常に開始された場合は true、弾かれたまたは失敗した場合は false</returns>
		public bool QuickLoad() {
			var saveData = SaveSystem.Instance.GetSaveData(QUICK_SAVE_SLOT);
			if (saveData == null) {
				ShowNotification($"[QuickSave] スロット '{QUICK_SAVE_SLOT}' のデータが存在しません。");
				return false;
			}

			var sc = SceneController.Instance;
			if (sc == null) {
				ShowNotification($"[QuickSave] SceneController が存在しないためロードできません。");
				return false;
			}

			// シーン準備中・ロード中の連打や割込み防止ガード
			if (sc.IsPrepare || sc.IsLoading) {
				ShowNotification($"[QuickSave] シーン準備中またはロード中のためロードをキャンセルしました。");
				return false;
			}

			try {
				// 1. スロットのセット
				SaveSystem.Instance.SetSlot(QUICK_SAVE_SLOT);

				// 2. BGMの停止（ロード時の自然な音切り替えのため）
				if (SoundManager.Instance != null) {
					SoundManager.Instance.StopMusic();
				}

				// 3. セーブデータの読み込み
				SaveSystem.Instance.LoadGameData();

				// 4. 周回データ/全般データの保存更新
				SaveSystem.Instance.SaveUniverseData();

				// 5. ミッション/イベントフラグ状態の更新
				if (MissionManagerData.Instance != null) {
					MissionManagerData.Instance.UpdateCheckMissions();
				}

				// 6. 画面・シーンの読み込み＆描画切替
				sc.LoadCurrentScene();

				DebugUtil.Log($"[QuickSave] スロット '{QUICK_SAVE_SLOT}' からクイックロードし、画面を切り替えました。");

				// --- 成功時の通知コルーチン開始 ---
				ShowNotification("finish quick load", 2.0f);

				return true;
			} catch (System.Exception ex) {
				ShowNotification($"[QuickSave] クイックロード失敗: {ex.Message}");
				return false;
			}
		}

		// ==========================================
		// UI通知表示用メソッド・コルーチン
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

			yield return new WaitForSeconds(duration);

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

			// 画面の中央上部に幅 220px, 高さ 45px の枠を表示
			float width = 220f;
			float height = 45f;
			float x = (Screen.width - width) / 2f;
			float y = 35f;

			GUI.Box(new Rect(x, y, width, height), "");
			GUI.Label(new Rect(x, y, width, height), m_NotificationText, style);
		}

	}
}