using BepInEx;
using HarmonyLib;
using Mortal.Core;
using UnityEngine;
using BepInEx.Unity.Mono;

namespace Mod
{
    [BepInPlugin(ModInfo.c_ModFullName, ModInfo.c_ModName, ModInfo.c_ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
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
            DebugUtil.LogWarning("Mortal hoge Mod Booted!");
        }

        public void Update() {
            if (SaveSystem.Instance == null)
                return;

        }
	}
}