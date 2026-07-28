using JetBrains.Annotations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
//using UnityEngine.InputSystem;


namespace Mod.Lib
{
	public class ModKey
	{
		const float c_Time_RepeatStart = 0.5f;
		const float c_Time_RepeatInterval = 0.1f;

		public enum eType
		{
			Key,
			Mouse,
		}

		public enum eState
		{
			None,
			Push,
			Press,
			Release,
		}

		eState m_State;
		eType m_Type;
		Key m_Key;
		int m_MouseButtonIndex;

		float m_PressTime;
		bool m_IsRepeat;
		float m_RepeatTime;
		bool m_IsPush;
		bool m_IsTrgRepeat;

	
		public ModKey(Key code) {
			m_Key = code;
			m_Type = eType.Key;
		}

		public ModKey(int mouseButtonIndex) {
			m_MouseButtonIndex = mouseButtonIndex;
			m_Type = eType.Mouse;
		}

		
		public void Update(float delta) {
			bool isPrevPush = m_IsPush;
			switch (m_Type) {
				case eType.Key:
					//	m_IsPush = Keyboard.current[m_Key].IsPressed();
					m_IsPush = Input.GetKey(m_Key.ToKeyCode());
					break;
				case eType.Mouse:
					switch ( m_MouseButtonIndex) {
					//case 0: m_IsPush = Mouse.current.leftButton.IsPressed(); break;
					//case 1: m_IsPush = Mouse.current.rightButton.IsPressed(); break;
					//case 2: m_IsPush = Mouse.current.middleButton.IsPressed(); break;
					case 0: m_IsPush = Input.GetMouseButton(0); break;
					case 1: m_IsPush = Input.GetMouseButton(1); break;
					case 2: m_IsPush = Input.GetMouseButtonDown(2); break;
					}
					break;
			}

			if (m_IsPush) {
				if (isPrevPush)
					m_State = eState.Push;
				else
					m_State = eState.Press;
			} else {
				if (isPrevPush)
					m_State = eState.Release;
				else
					m_State = eState.None;
			}

			if (m_IsPush)
				m_PressTime += delta;
			else
				m_PressTime = 0;


			if (m_IsRepeat) {
				m_IsTrgRepeat = false;
				if (m_PressTime <= 0.0f) {
					m_IsRepeat = false;
					m_RepeatTime = 0.0f;
				} else {
					m_RepeatTime += delta;
					if (m_RepeatTime > c_Time_RepeatInterval) {
						m_RepeatTime = 0.0f;
						m_IsTrgRepeat = true;
					}
				}
			} else {
				if (m_PressTime > c_Time_RepeatStart) {
					m_IsRepeat = true;
					m_RepeatTime = 0.0f;
				}
			}
		}

		public bool IsTriggerRepeat() {
			return m_IsTrgRepeat || m_State == eState.Press;
		}

		public eState GetState() {
			return m_State;
		}

		public eType GetKeyType() {
			return m_Type;
		}

		public bool IsEqual(int mouseButton) {
			if (m_Type != eType.Mouse)
				return false;
			if (m_MouseButtonIndex != mouseButton)
				return false;
			return true;
		}

		public bool IsEqual(Key key) {
			if (m_Type != eType.Key)
				return false;
			if (m_Key != key)
				return false;
			return true;
		}
	}



	/// <summary>
	/// キー入力.
	/// </summary>
	[DefaultExecutionOrder(-100)]
	class ModInput : MonoSingleton<ModInput>
	{
		// リピートキー入力のためだけに作成.

		List<ModKey> m_Keys = new List<ModKey>();

		
		void Awake() {
			m_Keys = new List<ModKey>();
		}


		void  Update() {
			float delta = Time.deltaTime;
			foreach ( var itr in m_Keys ) {
				itr.Update(delta);
			}
		}


		ModKey _GetOrCreateKey(int mouseButton) {
			foreach (var item in m_Keys) {
				if (!item.IsEqual(mouseButton))
					continue;
				return item;
			}
			ModKey ret = new ModKey(mouseButton);
			m_Keys.Add(ret);
			return ret;
		}

		ModKey _GetOrCreateKey(Key key) {
			foreach (var item in m_Keys) {
				if (!item.IsEqual(key))
					continue;
				return item;
			}
			ModKey ret = new ModKey(key);
			m_Keys.Add(ret);
			return ret;
		}


		public bool GetMouseDown(int button) { return _GetOrCreateKey(button).GetState() == ModKey.eState.Press; }
		public bool GetMouseUp(int button) { return _GetOrCreateKey(button).GetState() == ModKey.eState.Release; }
		public bool GetMouse(int button) { return _GetOrCreateKey(button).GetState() == ModKey.eState.Push; }
		public bool GetMouseRepeat(int button) { return _GetOrCreateKey(button).IsTriggerRepeat(); }

		public bool GetKeyDown(Key keyCode) { return _GetOrCreateKey(keyCode).GetState() == ModKey.eState.Press; }

		public bool GetKeyUp(Key keyCode) { return _GetOrCreateKey(keyCode).GetState() == ModKey.eState.Release; }

		public bool GetKey(Key keyCode) { return _GetOrCreateKey(keyCode).GetState() == ModKey.eState.Push; }
		
		public bool GetKeyRepeat(Key keyCode) { return _GetOrCreateKey(keyCode).IsTriggerRepeat(); }

		public bool GetKeyAny() {
			return Input.anyKey;
		}
		public bool GetKeyAnyDown() {
			return Input.anyKeyDown;
		}

	}
}
