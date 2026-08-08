using BepInEx;
using BepInEx.Unity.Mono;
using HarmonyLib;
using Mortal.Core;
using Mortal.Story;
using MoonSharp.Interpreter;
using System;
using System.Collections;
using UnityEngine;
using Fungus;



namespace Mod
{
	// 本体.
	[BepInPlugin(ModInfo.c_ModFullName, ModInfo.c_ModName, ModInfo.c_ModVersion)]
	public class Plugin : BaseUnityPlugin
	{
		public static Plugin Instance { get; private set; }
		public ModConfig ModConfig => MyModManager.Instance.GetConfig() as ModConfig;


		// エントリポイント.
		private void Awake() {
			Instance = this;
			MyModManager.Instance.Initialize<ModConfig>(this, this.Logger, ModInfo.c_ModFullName, ModInfo.c_ModName, ModInfo.c_ModVersion);
			gameObject.AddComponent<NewGamePlusUI>();
		}



		void Unload() {
			MyModManager.Instance?.Terminate();
			MyModManager.DeleteInstance();
		}

	}
}