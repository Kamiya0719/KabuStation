using System;
using System.Net.Http;

namespace CSharp_sample
{
	public class GenerateToken
	{
		public static string GetToken(bool isTest = false, bool isGetFile = true)
		{
			if (!isTest && isGetFile) {
				// ファイル読み込みを行って、一番下の行のDateTimeが同じ日付だったらスキップ
				string[] info = CsvControll.GetTokenInfo();
				if(info.Length == 2) {
					DateTime dDate = DateTime.Parse(info[1]);
					// 日付が今日でないやつはアウト なんとなく7時でも切り替えるようにしよう
					if (Common.SameD(DateTime.Now, dDate) && !(DateTime.Now.Hour >= 7 && dDate.Hour < 7)) return info[0];
				}
			}
			Common.DebugInfo("GetToken", isTest);
			string Token = RequestBasic.RequestToken().token;
			if (!isTest && Token != "" && Token != null) CsvControll.SaveTokenInfo(new string[2] { Token, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") });
			return Token;
		}
	}
}
