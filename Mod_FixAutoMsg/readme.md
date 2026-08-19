# Mod_AutoAdvance (活俠傳 / Legend of Mortal)

会話メッセージの自動送り待機時間を調整できるようにする BepInEx Mod です。

## 概要
* ゲーム中に専用の設定ウィンドウを開き、スライダーを操作して直感的に自動文字送りスピードを変更できます。
* スライダーの変更内容はリアルタイムに反映され、コンフィグファイルへ自動で保存されます。
* 有志日本語化Mod環境およびバニラ環境の双方に対応しています。

---

## 使い方 (操作方法)

Mod導入後、ゲーム中のオート挙動が自動的に置き換わります。  
  
F1（デフォルト設定）で設定ウィンドウが開き、「1文字あたりの待機秒数」を調整可能です。  

---

## コンフィグ（設定変更）

初回ゲーム起動時、以下のパスに設定ファイルが自動生成されます。  
  
`ゲームフォルダ/BepInEx/config/com.yu-ituki.mortal.fix-auto-msg.cfg`

ゲーム内のGUIからだけでなく、テキストエディタ（メモ帳など）で開いて直接数値を設定することも可能です。

```ini
[AutoMessage]

## 表示完了後の読了待機時間（1文字あたりの秒数）
# Setting type: Single
# Default value: 0.2
SecondsPerChar = 0.2

[General]

## 設定ウィンドウを開閉するキー
# Setting type: KeyCode
# Default value: F1
ToggleMenuKey = F1