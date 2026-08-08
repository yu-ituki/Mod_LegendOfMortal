using Fungus;
using Mortal.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;


namespace Mod
{
 // 引き継ぎ項目.
    [Flags]
    public enum eCarryOverOptions
    {
        None = 0,
        Stats = 1 << 0,
        Affection = 1 << 1,
        SectAssets = 1 << 2,
        All = Stats | Affection | SectAssets
    }

	public enum eNGPState {
		None,
		Request,
	//	ReserveApply,
	//	Apply,
	}

	class Const
	{
		// 引き継ぎから除外するステータス（リセット対象）のブラックリスト
		public static readonly HashSet<GameStatType> c_IgnoredStats = new HashSet<GameStatType>
		{
			GameStatType.稱號,             // 称号
			GameStatType.愛人,             // 恋人フラグ
			GameStatType.變心,             // 心変わりフラグ
			GameStatType.娘化,             // TS/特殊フラグ
			GameStatType.門派規模,         // 門派の規模
			GameStatType.門派名聲,         // 門派の名声
			GameStatType.門派人數,         // 人数
			GameStatType.門派貢獻,         // 門派貢献
			GameStatType.個人貢獻度,       // 個人貢献度
			GameStatType.行動次數,         // 行動回数
			GameStatType.額外行動次數_1,   // 追加行動回数1
			GameStatType.額外行動次數_2,   // 追加行動回数2
			GameStatType.額外行動次數_3,   // 追加行動回数3
		};
	}

    // NG+実行状態.
    internal static class NewGamePlusParam
    {

        public static eNGPState State { get; set; }
        public static eCarryOverOptions Options { get; set; }
        public static GameSave OldSaveData { get; set; }

        public static void Clear()
        {
            State = eNGPState.None;
            Options = eCarryOverOptions.All;
            OldSaveData = null;
        }

        public static bool IsOptionSet(eCarryOverOptions option) =>
            (Options & option) != 0;



		public static void Apply(PlayerStatManagerData playerStat) {
			var oldSave = NewGamePlusParam.OldSaveData;
			if (oldSave == null) return;

			bool isCarryStats = NewGamePlusParam.IsOptionSet(eCarryOverOptions.Stats);
			bool isCarrySect = NewGamePlusParam.IsOptionSet(eCarryOverOptions.SectAssets);

			if (isCarryStats || isCarrySect) {
				foreach (var stat in oldSave.Stats) {
					if (!OBB.Framework.Utils.EnumUtils.TryParseByStringValue(stat.Key, out GameStatType statType) )
						continue;

					// ブラックリストに入っているステータスはリセット（引き継ぎをスキップ）
					if (Const.c_IgnoredStats.Contains(statType))
						continue;

					if (statType == GameStatType.門派資產) {
						if (isCarrySect) {
							playerStat.Stats.Set(statType, stat.Value);
						}
					}
					else if (isCarryStats) {
						playerStat.Stats.Set(statType, stat.Value);
					}
				}
			}

			// 好感度の引き継ぎ
			if (NewGamePlusParam.IsOptionSet(eCarryOverOptions.Affection)) {
				foreach (var rel in oldSave.Relationships) {
					RelationshipStatType relType;
					if (!OBB.Framework.Utils.EnumUtils.TryParseByStringValue(rel.Key, out relType) &&
						!Enum.TryParse(rel.Key, ignoreCase: true, out relType)) {
						continue;
					}

					var target = playerStat.Relationships.Get(relType);
					if (target != null) {
						target.SetValue(rel.Value);
						target.SetActive(rel.Active);
					}
				}
			}
		}
	}
}
