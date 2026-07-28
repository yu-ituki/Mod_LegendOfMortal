using System;

using UnityEngine;

namespace Mod
{
	public class ModText
	{
		[Serializable]
		public class Row
		{
			public string id;
			public string text_JP;
			public string text_EN;
			public string text_CN;
			public string text_ZHTW;
			public string text_KR;
			public string text_IT;
			public string text_ES;
			public string text_DE;
			public string text_FR;
			public string text_RU;


			[NonSerialized]
			public eTextID textID;
		}

		private (int, eTextID)[] m_TextIDs;
		
		private eLanguage m_Lang;
		private Row[] m_Rows;

		public void Setup( string jsonPath )
		{
			Array textIDs = Enum.GetValues( typeof( eTextID ) );
			m_TextIDs = new (int, eTextID)[ textIDs.Length ];
			for (int j = 0; j < textIDs.Length; j++)
			{
				eTextID id = (eTextID)textIDs.GetValue( j );
				m_TextIDs[ j ] = (id.ToString().GetHashCode(), id);
			}
			
			var jsonFullPath = CommonUtil.GetResourcePath(jsonPath);
			var json = System.IO.File.ReadAllText(jsonFullPath, System.Text.Encoding.UTF8);
			var dats = Newtonsoft.Json.JsonConvert.DeserializeObject<Row[]>(json);
			m_Rows = new Row[dats.Length];
			for ( int i = 0; i < dats.Length; ++i ) {
				var index = System.Array.FindIndex(m_TextIDs, v => v.Item1 == dats[i].id.GetHashCode());
				dats[i].textID = m_TextIDs[index].Item2;
				m_Rows[(int)dats[i].textID] = dats[i];
			}
		}


		public string GetText( eTextID id )
		{
			string ret = null;
			Row row = m_Rows[ (int)id ];
			switch ( GetLanguageCode() )
			{
			case eLanguage.JP: ret = row.text_JP; break;
			case eLanguage.ZH_CN: ret = row.text_CN; break;
			case eLanguage.ZH_TW: ret = row.text_ZHTW; break;
			case eLanguage.KO: ret = row.text_KR; break;
			case eLanguage.ES: ret = row.text_ES; break;
			case eLanguage.DE: ret = row.text_DE; break;
			case eLanguage.IT: ret = row.text_IT; break;
			case eLanguage.FR: ret = row.text_FR; break;
			case eLanguage.RU: ret = row.text_RU; break;
			default: ret = row.text_EN; break;
			};
			if (string.IsNullOrEmpty(ret))
				ret = row.text_EN;
			return ret;
		}

		public void SetLanguageCode( eLanguage lang ) {
			m_Lang = lang;
		}

		public eLanguage GetLanguageCode()
		{
			return m_Lang;
		}

		public Row[] GetRows()
		{
			return m_Rows;
		}
	}

}