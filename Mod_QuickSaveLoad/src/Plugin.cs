using BepInEx;
using BepInEx.Unity.Mono;
using HarmonyLib;
using Mortal.Core;
using UnityEngine;

namespace Mod
{
    [BepInPlugin(ModInfo.c_ModFullName, ModInfo.c_ModName, ModInfo.c_ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string QUICK_SAVE_SLOT = "quicksave";

        public static Plugin Instance { get; private set; }

        public ModConfig ModConfig { get => MyModManager.Instance.GetConfig() as ModConfig; }

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
            if (SaveSystem.Instance == null)
                return;

            if (Input.GetKeyDown(KeyCode.F5))
                QuickSave();

            if (Input.GetKeyDown(KeyCode.F9))
                QuickLoad();
        }

        /// <summary>
        /// クイックセーブの実行処理（内部で安全性をチェックして判定・弾く）
        /// </summary>
        /// <returns>セーブが正常に実行された場合は true、弾かれたまたは失敗した場合は false</returns>
        public bool QuickSave()
        {
            var sc = SceneController.Instance;
            if (sc == null)
            {
                DebugUtil.LogWarning("[QuickSave] SceneController が存在しないためセーブできません。");
                return false;
            }

            // 1. ロード中・準備中はセーブ不可
            if (sc.IsPrepare || sc.IsLoading)
            {
                DebugUtil.LogWarning("[QuickSave] シーン準備中またはロード中のためセーブできません。");
                return false;
            }

            // 2. シーンによる制限（会話中・タイトル画面・バトル中などはセーブ不可）
            string currentScene = sc.CurrentScene;
            if (currentScene == "Title" ||
                currentScene == "Story" ||
                currentScene == "Battle" ||
                currentScene == "Combat" ||
                currentScene == "GameOver" ||
                currentScene == "End" ||
                currentScene == "DemoEnd")
            {
                DebugUtil.LogWarning($"[QuickSave] 現在のシーン('{currentScene}')ではセーブできません。");
                return false;
            }

            try
            {
                SaveSystem.Instance.SaveGameData(QUICK_SAVE_SLOT);
                DebugUtil.Log($"[QuickSave] スロット '{QUICK_SAVE_SLOT}' にクイックセーブしました。");
                return true;
            }
            catch (System.Exception ex)
            {
                DebugUtil.LogError($"[QuickSave] クイックセーブ失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// クイックロードの実行処理（内部で安全性をチェックして判定・弾く）
        /// </summary>
        /// <returns>ロードが正常に開始された場合は true、弾かれたまたは失敗した場合は false</returns>
        public bool QuickLoad()
        {
            var saveData = SaveSystem.Instance.GetSaveData(QUICK_SAVE_SLOT);
            if (saveData == null)
            {
                DebugUtil.LogWarning($"[QuickSave] スロット '{QUICK_SAVE_SLOT}' のデータが存在しません。");
                return false;
            }

            var sc = SceneController.Instance;
            if (sc == null)
            {
                DebugUtil.LogWarning("[QuickSave] SceneController が存在しないためロードできません。");
                return false;
            }

            // シーン準備中・ロード中の連打や割込み防止ガード
            if (sc.IsPrepare || sc.IsLoading)
            {
                DebugUtil.LogWarning("[QuickSave] シーン準備中またはロード中のためロードをキャンセルしました。");
                return false;
            }

            try
            {
                // 1. スロットのセット
                SaveSystem.Instance.SetSlot(QUICK_SAVE_SLOT);

                // 2. BGMの停止（ロード時の自然な音切り替えのため）
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.StopMusic();
                }

                // 3. セーブデータの読み込み
                SaveSystem.Instance.LoadGameData();

                // 4. 周回データ/全般データの保存更新
                SaveSystem.Instance.SaveUniverseData();

                // 5. ミッション/イベントフラグ状態の更新
                if (MissionManagerData.Instance != null)
                {
                    MissionManagerData.Instance.UpdateCheckMissions();
                }

                // 6. 画面・シーンの読み込み＆描画切替
                sc.LoadCurrentScene();

                DebugUtil.Log($"[QuickSave] スロット '{QUICK_SAVE_SLOT}' からクイックロードし、画面を切り替えました。");
                return true;
            }
            catch (System.Exception ex)
            {
                DebugUtil.LogError($"[QuickSave] クイックロード失敗: {ex.Message}");
                return false;
            }
        }
    }
}