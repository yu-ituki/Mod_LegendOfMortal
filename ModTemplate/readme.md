# Readme
『活俠傳 (Legend of Mortal)』の Mod開発用の汎用雛形（ModTemplate）です。  
様々なサポート機能を有しています。  

# 解説
このテンプレートは下記を有しています。
* 基本となる最小限のプロジェクト、ソース群（エントリポイントや雛形など）
* Mod用の簡易的なローカライズ機能（全言語対応用・LeanLocalization連動）
* 簡易的にゲームインストールフォルダとDLL参照できる仕組み
* ゲーム本体のBepInExフォルダに成果物を自動コピーする仕組み
* ゲーム中の基本的なライフサイクルの管理（SaveSystemフックによる起動検知）
* 動的HarmonyPatch登録の簡易化機能
* BepInExコンフィグ関連の簡易的なサポート機能

## 使い方
1. このフォルダをコピーしてください。
2. このリポジトリに存在するLibsフォルダもコピーしてください。
   1. ライブラリ的なソースコードを別フォルダ（./../Libs）に括りだしています。 
3. 「ModTemplate.sln」を自身のMod名にリネームしてください。
4. ModInfo.cs の「c_ModFullName」および「c_ModName」を自身のMod名に変更してください。
5. config.bat に自身の環境情報（`GAME_PATH` など）を記載してください。
6. 準備完了です。これでビルドできるようになっているはずです。

## 動作環境
* .Net Framework 4.8で動作しています。    
* Visual Studio 2022 と .Net Framework 4.8を入れてもらえればとりあえず動くと思います。  
* このプロジェクトにはUnityは必要ありませんが、プレハブやScriptableObject、AssetBundleなどを生成する場合は必要です。  
  * C#のみでも、とりあえず new GameObject() して AddComponentしたり、 Texture2D.LoadRawTextureData() なりを活用すれば   
    もしかしたらUnityが必要ないかもしれませんが、リソースを新規追加するんだったら有ったほうが良いです。   
* あとはゲーム本体（活俠傳）を買ってインストールすれば動作環境が完成です。唐門の弟子になって一緒に泣きましょう。

## Libsについて
* 各Modで完全に共通使用出来そうなソース群です。 
* このフォルダの1つ上にフォルダが存在しています。 
* config.batを叩くことで、Libsとプロジェクトのsrc/Lib以下にシンボリックリンクが張られ参照されます。 
* Libsを一緒に落としてきて頂き、プロジェクトフォルダと同列のディレクトリに置いて頂ければ、  
  テンプレートをコピペしてもとりあえずビルドできるようになっています。  

## 基本となる最小限のプロジェクト、ソース群
* Mod構築用の最小限のプロジェクトやソースが入っています。
* Mortalの主要DLL（Mortal.Core / Mortal.Story / Mortal.Free 等）にあらかじめ参照が通っています。
* Plugin.cs がエントリポイントです。
* ModConfig.cs にMod用コンフィグ（BepInExコンフィグ形式）を書きます。
* BepInExロガーを使用して、DebugUtil.Log()、LogError() などを用意しました。 Debug.Log() 的に使用できます。
  * BepInExログを閲覧するにはBepInExコンソールを開く必要があります。
  * ゲームインストールフォルダ/BepInEx/config/BepInEx.cfg の [Logging.Console] にて、Enabled = true でゲーム起動時に開きます。

## ゲーム中の基本的なライフサイクルの管理
* MyModManager.cs がライブラリ群の初期化やゲーム内の基本的なタイミングのコールバックを管理しています。  
* Plugin.cs で初期化＆コールバック登録をしています。  
* RegisterOnBootAction() でゲーム開始時のコールバックが登録できます。
  * SaveSystem.LoadUniverseData の呼び出し直後（ゲーム基本データのロード完了時）に実行されるコールバックです[cite: 2, 23]。
  * ほぼすべてのデータが揃った状態のコールバックになります。
  * ゲーム中の様々なデータにアクセスしたい初期化処理はここで書いてください。

## Mod用の簡易的なローカライズ機能（全言語対応用）
* エクセルを使用した簡易的なローカライズ機能です
* エクセルで定義したIDとC#のenumを同期させる仕組みが入っています
  * 最低限エクセルさえあれば誰でも叩けるということで、VBAで作られています
  * data/resource/tables/mod_texts.xlsm にテーブル定義＆VBAが入っています
* エクセル上のボタンを叩くとsrc/TextID.cs および mod_texts.json が出力されます
* src/Lib/ModTextManager.cs で読み込んでいます。
* ModTextManager.Instance.GetText( eTextID ) で、ゲーム内の言語（LeanLocalization）を自動識別して文字列を返却します。
* 文中ユーザーデータ埋め込みに対応しています。文中の[0]～[8]と ModTextManager.Instance.SetUserData() が対応していて、indexに応じて置き換わります。

## 簡易的にゲームインストールフォルダとDLL参照できる仕組み
* config.batに書かれたインストールフォルダのDLLを参照しに行きます
* 仕組み的にはconfig.batにてシンボリックリンクを貼り、csprojで参照しているだけです
* DLL参照を増やす場合は直に参照を増やしてもいいですし、csprojを直にいじって、適当にコピペして増やしても大丈夫です

## ゲーム本体のBepInEx/pluginsフォルダに成果物を自動コピーする仕組み
* config.batに書かれたインストールフォルダに下記をコピーします
  * ビルド後のDLL
  * data/resource フォルダごと全部
* これでビルド -> 起動するだけでゲーム本体で即座に動作確認が可能です
* コピーされたファイル群はそのまま配信データ（ZIP等）として使用可能です

## 動的HarmonyPatch登録の簡易化機能
* ローカルファンクションの登録や、特定タイミングでのみ登録しておきたい際などに、動的にPatchを当てやすいサポート機能を用意しています。
* 下記のように書くと、登録、登録解除が比較的かんたんに出来ます。
```csharp
// SaveSystemのSaveGameDataにパッチを当てる例.
static void _OnSaveGameData() {

}

var info = new ModPatchInfo() {
  m_TargetType = typeof(Mortal.Core.SaveSystem),
  m_Regex = "SaveGameData",
  m_Prefix = CommonUtil.ToMethodInfo( _OnSaveGameData )
};

// 当てる.
MyModManager.Instance.AddPatch(info);

// 外す.
MyModManager.Instance.RemovePatch(info);