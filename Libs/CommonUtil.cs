using System.IO;
using System.IO.Compression;
using BepInEx;
using HarmonyLib;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Net.Security;

namespace Mod
{
	public class ModPatchInfo {
		public System.Type m_TargetType;
		public string m_Regex;
		public MethodInfo m_Prefix;
		public MethodInfo m_Postfix;

		HarmonyMethod m_PatchPrefix;
		HarmonyMethod m_PatchPostfix;
		MethodInfo m_MethodInfo;
		

		public void Patch( Harmony harmony ) {
			if (m_MethodInfo != null)
				return;
			var methodInfos = m_TargetType.GetMethods((System.Reflection.BindingFlags)~(0));
			m_MethodInfo = System.Array.Find(methodInfos, v => Regex.IsMatch(v.Name, m_Regex));
			//	DebugUtil.LogError($"{method?.Name} :  {m_Regex} ");
			if (m_MethodInfo == null)
				return;
			m_PatchPrefix = m_Prefix != null ? new HarmonyMethod(m_Prefix) : null;
			m_PatchPostfix = m_Postfix != null ? new HarmonyMethod(m_Postfix) : null;
			harmony.Patch(m_MethodInfo, m_PatchPrefix, m_PatchPostfix, null, null, null);
		}

		public void Unpatch( Harmony harmony ) {
			if (m_MethodInfo == null)
				return;
			if ( m_PatchPostfix != null )
				harmony.Unpatch(m_MethodInfo, m_PatchPostfix.method);
			if (m_PatchPrefix != null)
				harmony.Unpatch(m_MethodInfo, m_PatchPrefix.method);
			m_MethodInfo = null;
			m_PatchPostfix = null;
			m_PatchPrefix = null;
		}
	}


	public class TmpFile : System.IDisposable
	{
		string m_TmpPath;

		public static TmpFile Create(string basePath) {
			if (!System.IO.File.Exists(basePath))
				throw new System.Exception($"[TmpFile] file not found : {basePath}");
			TmpFile ret = new TmpFile();

			var ext = System.IO.Path.GetExtension(basePath);
			ret.m_TmpPath = basePath.Replace($".{ext}", "");
			ret.m_TmpPath = $"{ret.m_TmpPath}_tmp.{ext}";

			if (System.IO.File.Exists(ret.m_TmpPath))
				System.IO.File.Delete(ret.m_TmpPath);
			System.IO.File.Copy(basePath, ret.m_TmpPath);

			return ret;
		}

		public void Delete() {
			if (System.IO.File.Exists(m_TmpPath))
				System.IO.File.Delete(m_TmpPath);
		}

		public void Dispose() {
			Delete();
		}

		public string GetPath() {
			return m_TmpPath;
		}

	}

	public class CommonUtil
	{

		private static string m_ResourcePathBase;


		public static void Initialize( PluginInfo info )
		{
			m_ResourcePathBase = Path.GetDirectoryName( info.Location ) + "/resource/";
		}

		public static string GetResourcePath( string resName )
		{
			return m_ResourcePathBase + resName;
		}

	
		public static Sprite LoadSprite( string resName ) {
			var tex = LoadTexture(resName);
			return Sprite.Create( tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
		}

		public static void UnloadSprite( ref Sprite sprite ) {
			var tex = sprite?.texture;
			UnloadTexture(ref tex);
			sprite = null;
		}


		public static Texture2D LoadTexture( string resName ) {
			try {
				var path = GetResourcePath(resName);
				var bytes = System.IO.File.ReadAllBytes(path);
				var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				if ( !tex.LoadImage(bytes) ) {
					UnloadTexture(ref tex);
					return null;
				}
				tex.Apply();
				return tex;
			} catch ( System.Exception e ) {
				DebugUtil.LogError(e);
			}
			return null;
		}
		public static void UnloadTexture(ref Texture2D tex) {
			if ( tex != null )
				Texture2D.Destroy(tex);
			tex = null;
		}

		public static bool GetMouseDown(int button) {
			return UnityEngine.Input.GetMouseButtonDown(button);
		}
		public static bool GetMouseUp(int button) {
			return UnityEngine.Input.GetMouseButtonUp(button);
		}
		public static bool GetMouse(int button) {
			return UnityEngine.Input.GetMouseButton(button);
		}

		public static bool GetKeyDown( UnityEngine.KeyCode keyCode ) {
			return UnityEngine.Input.GetKeyDown(keyCode);
		}

		public static bool GetKeyUp(UnityEngine.KeyCode keyCode) {
			return UnityEngine.Input.GetKeyUp(keyCode);
		}

		public static bool GetKey(UnityEngine.KeyCode keyCode) {
			return UnityEngine.Input.GetKey(keyCode);
		}
		public static bool GetKeyAny() {
			return UnityEngine.Input.anyKey;
		}
		public static bool GetKeyAnyDown() {
			return UnityEngine.Input.anyKeyDown;
		}

		public static void CopyDirectory(string sourceDir, string destinationDir) {
			// コピー先のディレクトリが存在しない場合は作成
			if (!Directory.Exists(destinationDir)) {
				Directory.CreateDirectory(destinationDir);
			}

			// ファイルをコピー
			foreach (string file in Directory.GetFiles(sourceDir)) {
				string fileName = Path.GetFileName(file);
				string destFile = Path.Combine(destinationDir, fileName);
				File.Copy(file, destFile, true); // 上書きオプションをtrueに設定
			}

			// サブディレクトリを再帰的にコピー
			foreach (string subDir in Directory.GetDirectories(sourceDir)) {
				string subDirName = Path.GetFileName(subDir);
				string destSubDir = Path.Combine(destinationDir, subDirName);
				CopyDirectory(subDir, destSubDir);
			}
		}



		public static void ApplyHarmonyPatches(Harmony harmony, ModPatchInfo[] infos) {
			foreach (var itr in infos)
				itr.Patch(harmony);
		}


		const float c_Epsilon = 0.0001f;

		public static bool IsEqual( float a, float b ) {
			return Mathf.Abs(a - b) <= c_Epsilon;
		}

		public static bool IsEqual(Vector3 a, Vector3 b) {
			return Mathf.Abs(Vector3.Distance(a,b)) <= c_Epsilon;
		}

	

		public static MethodInfo ToMethodInfo( System.Action act ) => act.Method;
		public static MethodInfo ToMethodInfo<T>(System.Action<T> act) => act.Method;
		public static MethodInfo ToMethodInfo<T,T2>(System.Action<T,T2> act) => act.Method;
		public static MethodInfo ToMethodInfo<T,T2,T3>(System.Action<T,T2,T3> act) => act.Method;
		public static MethodInfo ToMethodInfo<T,T2,T3,T4>(System.Action<T,T2,T3,T4> act) => act.Method;

		public static MethodInfo ToMethodInfo(System.Func<bool> act) => act.Method;
		public static MethodInfo ToMethodInfo<T>(System.Func<T,bool> act) => act.Method;
		public static MethodInfo ToMethodInfo<T,T2>(System.Func<T, T2, bool> act) => act.Method;
		public static MethodInfo ToMethodInfo<T,T2,T3>(System.Func<T, T2, T3, bool> act) => act.Method;
		public static MethodInfo ToMethodInfo<T,T2,T3,T4>(System.Func<T, T2, T3, T4, bool> act) => act.Method;


	}
}
