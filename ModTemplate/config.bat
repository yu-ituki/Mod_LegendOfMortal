::---------------.
:: 設定項目.
::---------------.
:: ゲームインストールフォルダ.
set GAME_PATH="D:\game\steam\steamapps\common\LegendOfMortal"
set GAME_DLL_PATH="%GAME_PATH:"=%\Mortal_Data"
set GAME_BEPINEX_PATH="%GAME_PATH:"=%\BepInEx"
:: Lib元フォルダ.
set LIB_SRC_PATH="%~dp0/../Libs"
:: Libコピー先フォルダ.
set LIB_DEST_PATH="%~dp0/src/Lib"

::---------------.
:: DLL参照用にシンボリックリンクを貼る.
::---------------.
if not exist %~dp0game_dlls\ (
    mklink /D %~dp0game_dlls\ %GAME_DLL_PATH% 
)
if not exist %~dp0game_bepinex\ (
    mklink /D %~dp0game_bepinex\ %GAME_BEPINEX_PATH% 
)

::---------------.
:: Lib参照用にシンボリックリンクを貼る.
::---------------.
if not exist %LIB_DEST_PATH% (
    mklink /D %LIB_DEST_PATH% %LIB_SRC_PATH% 
)