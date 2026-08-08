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
	[HarmonyPatch]
	public class BepinExPatch
	{
		[HarmonyPatch(typeof(FateBonusPanel), nameof(FateBonusPanel.OnPanelOpen))]
		[HarmonyPrefix]
		private static bool Prefix_OnPanelOpen(FateBonusPanel __instance) {
			if (NewGamePlusParam.State == eNGPState.None || NewGamePlusParam.OldSaveData == null)
				return true;

			// pressCancel フラグを true にしてパネルのコルーチンを終わらせる
			var field = typeof(FateBonusPanel).GetField("_pressCancel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (field != null) {
				field.SetValue(__instance, true);
			}

			NewGamePlusParam.Apply(PlayerStatManagerData.Instance);
			NewGamePlusParam.Clear();
			return false; // 元の OnPanelOpen() をスキップ
		}


	}
}