using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;

using Mod.Lib;

using UnityEngine;
//using UnityEngine.InputSystem;

namespace Mod
{
	public class KeyConfigData
	{
		ConfigEntry<string> m_ConfigEntry;
		Key m_Key;

		static (int, Key)[] s_KeyCodeDic;

		

		public static KeyConfigData Create(ConfigFile config, Key defaultKey, string name, string description, string tag = "General" ) {
			var str = defaultKey.ToString();

			var ret = new KeyConfigData();
			ret.m_ConfigEntry = config.Bind(tag, name, str, description);
			str = ret.m_ConfigEntry.Value;

			if (s_KeyCodeDic == null) {
				var keyCodes = System.Enum.GetValues(typeof(Key));
				s_KeyCodeDic = new (int, Key)[keyCodes.Length];
				for ( int i = 0; i < keyCodes.Length; ++i) {
					var key = (Key)keyCodes.GetValue(i);
					var keyName = key.ToString();
					s_KeyCodeDic[i] = (keyName.GetHashCode(), key);
				}
			}

			var valHash = str.GetHashCode();
			for ( int i = 0; i < s_KeyCodeDic.Length; ++i) {
				if (s_KeyCodeDic[i].Item1 != valHash)
					continue;
				ret.m_Key = s_KeyCodeDic[i].Item2;
				break;
			}

			return ret;
		}

		public void SetKey( Key code ) {
			m_ConfigEntry.Value = code.ToString();
			m_Key = code;
		}

		public bool IsPress() {
			return ModInput.Instance.GetKey(m_Key);
		}

	}

	public abstract class ModConfigBase
	{
		public virtual void Initialize(BepInEx.Configuration.ConfigFile config) { }

	}
}
