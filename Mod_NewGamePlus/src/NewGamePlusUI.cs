using Mod;

using Mortal.Core;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NewGamePlusUI : MonoBehaviour
{
	private Rect m_WindowRect;
	private bool m_IsOpen;
	private string m_Slot = string.Empty;
	private eCarryOverOptions m_Options = eCarryOverOptions.All;
	private string m_StatusText = string.Empty;
	private float m_StatusExpire;

	// 背面UIの操作（レイキャスト）を遮断するための動的uGUIキャンバス
	private GameObject m_BlockerObject;

	private void Update() {
		var config = Plugin.Instance.ModConfig;
		if (SaveSystem.Instance == null || config == null)
			return;

		if (Input.GetKeyDown(config.NewGamePlusKey.Value)) {
			if (!IsLoadScreenActive()) {
				ShowStatus("ロード画面でのみ使用できます。");
				return;
			}

			string currentSlot = GetSelectedSlot();
			if (string.IsNullOrEmpty(currentSlot) || SaveSystem.Instance.GetSaveData(currentSlot) == null) {
				ShowStatus("有効なセーブデータを選択してください。");
				return;
			}

			m_Slot = currentSlot;
			OpenWindow();
		}

		if (m_IsOpen && Input.GetKeyDown(KeyCode.Escape))
			CloseWindow();
	}

	private void OpenWindow() {
		m_IsOpen = true;
		CreateBlocker();

		if (EventSystem.current != null)
			EventSystem.current.sendNavigationEvents = false;
	}

	private void CloseWindow() {
		m_IsOpen = false;
		DestroyBlocker();

		if (EventSystem.current != null)
			EventSystem.current.sendNavigationEvents = true;
	}

	// 背面UIのクリック/ホバーを物理的に吸い取る透明なuGUIパネルを作成
	private void CreateBlocker() {
		if (m_BlockerObject != null)
			return;

		m_BlockerObject = new GameObject("NGPlus_UIBlocker");
		var canvas = m_BlockerObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 9999; // 画面の最前面に配置

		m_BlockerObject.AddComponent<GraphicRaycaster>();

		var imageObj = new GameObject("Image");
		imageObj.transform.SetParent(m_BlockerObject.transform, false);

		var image = imageObj.AddComponent<Image>();
		image.color = new Color(0f, 0f, 0f, 0.4f); // 必要に応じて半透明黒（背景の暗転効果）
		image.raycastTarget = true; // レイキャストをここで吸収

		var rectTransform = image.rectTransform;
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.sizeDelta = Vector2.zero;
	}

	private void DestroyBlocker() {
		if (m_BlockerObject != null) {
			Destroy(m_BlockerObject);
			m_BlockerObject = null;
		}
	}

	private void OnDisable() =>
		DestroyBlocker();

	private void OnDestroy() =>
		DestroyBlocker();

	private void OnGUI() {
		if (!IsLoadScreenActive())
			return;

		float nativeW = Display.main.systemWidth;
		float nativeH = Display.main.systemHeight;
		float scaleX = nativeW / 1920f;
		float scaleY = nativeH / 1080f;
		float scale = Mathf.Min(scaleX, scaleY);

		// 1. 下部案内ラベル
		if (!m_IsOpen) {
			string targetSlot = GetSelectedSlot();
			if (!string.IsNullOrEmpty(targetSlot))
				DrawLabel(
					new Rect(nativeW / 2f - 350f * scaleX, nativeH - 80f * scaleY, 700f * scaleX, 45f * scaleY),
					$"{Plugin.Instance.ModConfig.NewGamePlusKey.Value}: 選択中({targetSlot})から NewGamePlus を開始",
					Mathf.RoundToInt(20f * scale)
				);
		}

		// 2. ステータス通知
		if (m_StatusExpire > Time.realtimeSinceStartup)
			DrawLabel(
				new Rect(nativeW / 2f - 300f * scaleX, 40f * scaleY, 600f * scaleX, 50f * scaleY),
				m_StatusText,
				Mathf.RoundToInt(22f * scale)
			);

		// 3. 設定ダイアログ
		if (m_IsOpen) {
			float winW = 600f * scaleX;
			float winH = 460f * scaleY; // トグル項目が1つ増えたため高さを拡張(420f -> 460f)
			float winX = (nativeW - winW) / 2f;
			float winY = (nativeH - winH) / 2f;

			m_WindowRect = new Rect(winX, winY, winW, winH);

			var winStyle = new GUIStyle(GUI.skin.window) { fontSize = Mathf.RoundToInt(22f * scale) };
			m_WindowRect = GUI.ModalWindow(123456, m_WindowRect, id => DrawWindow(id, scale, scaleX, scaleY), "NewGamePlus", winStyle);

			GUI.FocusWindow(123456);
			GUI.BringWindowToFront(123456);

			if (Event.current.isMouse || Event.current.isKey)
				Event.current.Use();
		}
	}

	private void DrawWindow(int id, float scale, float scaleX, float scaleY) {
		var lStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20f * scale) };
		var bStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20f * scale) };

		GUILayout.BeginVertical(GUI.skin.box);
		GUILayout.Space(10f * scaleY);
		GUILayout.Label($"引き継ぎ元: {m_Slot}", lStyle);
		GUILayout.Space(15f * scaleY);

		DrawCustomToggle(eCarryOverOptions.Stats, "プレイヤーステータスを引き継ぐ", scale, scaleY);
		GUILayout.Space(10f * scaleY);
		DrawCustomToggle(eCarryOverOptions.Personality, "性格（性情・處世・修養・道德）を引き継ぐ", scale, scaleY);
		GUILayout.Space(10f * scaleY);
		DrawCustomToggle(eCarryOverOptions.Affection, "好感度を引き継ぐ", scale, scaleY);
		GUILayout.Space(10f * scaleY);
		DrawCustomToggle(eCarryOverOptions.SectAssets, "門派資産を引き継ぐ", scale, scaleY);

		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("開始", bStyle, GUILayout.Height(50f * scaleY)))
			StartNewGamePlus();
		if (GUILayout.Button("キャンセル", bStyle, GUILayout.Height(50f * scaleY)))
			CloseWindow();
		GUILayout.EndHorizontal();
		GUILayout.Space(10f * scaleY);
		GUILayout.EndVertical();

		GUI.DragWindow();
	}

	private void DrawCustomToggle(eCarryOverOptions flag, string text, float scale, float scaleY) {
		bool isOptionActive = (m_Options & flag) != 0;
		string prefix = isOptionActive ? "[✓] " : "[   ] ";

		var btnStyle = new GUIStyle(GUI.skin.label) {
			fontSize = Mathf.RoundToInt(20f * scale),
			normal = { textColor = isOptionActive ? Color.white : Color.gray }
		};

		if (GUILayout.Button(prefix + text, btnStyle, GUILayout.Height(36f * scaleY)))
			m_Options ^= flag;
	}

	private string GetSelectedSlot() {
		if (EventSystem.current != null) {
			GameObject selectedObj = EventSystem.current.currentSelectedGameObject;
			if (selectedObj != null) {
				var slotComponent = selectedObj.GetComponentInParent<ILoadGameSlot>();
				if (slotComponent != null && !string.IsNullOrEmpty(slotComponent.Slot))
					return slotComponent.Slot;
			}
		}
		return SaveSystem.Instance?.CurrentSlot ?? string.Empty;
	}

	private void StartNewGamePlus() {
		if (m_Options == eCarryOverOptions.None) {
			ShowStatus("1つ以上選択してください。");
			return;
		}

		string fromSlot = m_Slot;
		NewGamePlusParam.OldSaveData = SaveSystem.Instance.GetSaveData(fromSlot);

		string newSlot = GetNewSlotName();
		SaveSystem.Instance.SetSlot(newSlot);

		NewGamePlusParam.State = eNGPState.Request;
		NewGamePlusParam.Options = m_Options;

		SoundManager.Instance?.StopMusic();
		SaveSystem.Instance.NewGameData();
		SaveSystem.Instance.SaveUniverseData();
		MissionManagerData.Instance?.UpdateCheckMissions();
		SceneController.Instance?.LoadStory();

		ShowStatus("NewGamePlusを開始しました。");
		CloseWindow();
	}

	private string GetNewSlotName() {
		return "temp_ngp";
		/*
		int index = 1;
		while (true) {
			string slotName = index.ToString("D3");
			if (SaveSystem.Instance.GetSaveData(slotName) == null)
				return slotName;
			index++;
		}
		*/
	}

	private void ShowStatus(string msg) {
		m_StatusText = msg;
		m_StatusExpire = Time.realtimeSinceStartup + 2f;
	}

	private bool IsLoadScreenActive() =>
		LoadGamePanel.Instance != null && LoadGamePanel.Instance.gameObject.activeInHierarchy;

	private void DrawLabel(Rect r, string t, int fontSize) {
		GUI.Box(r, "");
		GUI.Label(r, t, new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
	}
}