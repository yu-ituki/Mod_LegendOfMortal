# Mod_LegendOfMortal

活侠伝（Legend of Mortal）のMod群です。  

神ゲーなのでみんな布教用に100本くらい買ってください。  

ゲームリンク  
[https://store.steampowered.com/app/1859910](https://store.steampowered.com/app/1859910)  

# 導入方法  

まずは日本語化Modをインストールしてください。  
もしくは自分で同じBepInExをダウンロードしてきてください。   
そうしたらダウンロードしたBepInExをゲームのインストールフォルダ直下に展開してください。  
展開するとMortal.exeがあるフォルダにBepInExフォルダやwinhttp.dllなどが置かれるはずです。  
（winhttp.dllはたまにウイルスチェッカーに引っかかるっぽいので何かあったら気にせず除外設定してください）  
その後、BepInEx/plugins フォルダ以下に[releases](https://github.com/yu-ituki/Mod_LegendOfMortal/releases)に上がっているzipを展開してください。  
基本的にはそれだけで動作します。

# 各Modのビルド手順

## 開発環境

* Windows 10/11
* Visual Studio 2022
* .NET Framework 4.8

## ビルド手順

※事前に前述の導入方法に記載されているBepInExを入れておいてください。  
（Vortex拡張機能を使用した場合は自動的にBepInExが入った状態になります）  
1. config.bat を編集し、自身のゲームインストールフォルダのパスを記載してください。  
2. config.bat を叩いてください。  
3. VisualStudio2022等でslnを開いてビルドしてください。  
  
基本的にはこれだけでビルドが出来て、ビルド後は自動的にBepInEx/pluginsフォルダにModがコピーされます。  
  
# 新規Modの作成簡易ガイド  
  
Mod作成用のテンプレートを用意しました。  
  
このプロジェクトをCloneするかダウンロードしてきて、ModTemplateフォルダとLibsフォルダのみを使用します。  

詳細な手順やテンプレートは[このページ](https://github.com/yu-ituki/Mod_LegendOfMortal/tree/main/ModTemplate) に記載してあります。  

その他不明点があれば[こちら](https://x.com/ituki_yu)に質問してきてください。  

あと良さげなModが出来たら自分も使いたいのでください。  

# ライセンスと免責

MITライセンスです。  

あと何も責任は取りません。  