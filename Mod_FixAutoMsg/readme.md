### README.md

```markdown
# Mod_QuickSaveLoad (活俠傳 / Legend of Mortal)

ゲーム内に「クイックセーブ」および「クイックロード」の機能を追加する BepInEx Mod です。

## 概要
* 指定したキーを押すことで、いつでもクイックセーブ / クイックロードを実行できます。
* キー設定はコンフィグファイルから自由に変更可能です。

---

## 使い方 (操作方法)

* **[F6]**: クイックセーブを実行（デフォルト）
* **[F9]**: クイックロードを実行（デフォルト）

---

## コンフィグ（設定変更）

初回ゲーム起動時、以下のパスに設定ファイルが自動生成されます。

`ゲームフォルダ/BepInEx/config/Mod_QuickSaveLoad.cfg`

テキストエディタ（メモ帳など）で開き、割り当てるキーを変更することができます。

```ini
[General]

## Quick saveに割り当てるキー
# Setting type: KeyCode
# Default value: F6
QuickSaveKey = F6

## Quick loadに割り当てるキー
# Setting type: KeyCode
# Default value: F9
QuickLoadKey = F9

```

### 設定可能なキー（例）

* ファンクションキー: `F1` ～ `F12`
* アルファベット: `A` ～ `Z`
* 数字: `Alpha0` ～ `Alpha9` （テンキーは `Keypad0` ～ `Keypad9`）
* その他: `Space`, `Return` (Enter), `Tab`, `LeftShift` など

※使用可能なキー名は [Unity KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html) の表記に準拠しています。

---

## インストール方法

1. [BepInEx](https://github.com/BepInEx/BepInEx) をゲームに導入します。
2. ビルドした `Mod_QuickSaveLoad.dll` を `ゲームフォルダ/BepInEx/plugins/` フォルダ内に配置します。
3. ゲームを起動します。

```

```
