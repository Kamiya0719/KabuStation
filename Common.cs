using System;
using System.Collections.Generic;

namespace CSharp_sample
{
	class Common
	{

		public static void DebugInfo(string tag, params object[] messages)
		{
			string message = tag + " ";
			int idx = 0;
			foreach (object obj in messages) {
				message += ", T" + idx + ":" + FormatObject(obj) + " ";
				idx++;
			}
			Console.WriteLine(message + " #End#");
			CsvControll.SaveDebugInfo(new List<string[]>() { new string[1] { message } }, true);
		}
		private static string FormatObject(object o)
		{
			if (o is null) return "(null)";
			if (o as string == string.Empty) return "(empty)";
			return o.ToString();
		}




		// プロ500
		private static List<string> Pro500List = new List<string>();
		public static bool Pro500(string symbol)
		{
			if (Pro500List.Count == 0) foreach (string[] data in CsvControll.GetPro500()) { Pro500List.Add(data[0]); }
			return Pro500List.Contains(symbol);
		}

		public static bool Sp10(string symbol)
		{
			return Def.SpBuyInfo.ContainsKey(symbol);
		}

		public static int Sp10BuyPrice(string symbol)
		{
			foreach (string[] info in CsvControll.GetSpInfo()) {
				if (info[0] == symbol) return Int32.Parse(info[1]);
			}
			CsvControll.ErrorLog("Sp10BuyPrice", symbol, "", "");
			return 0;
		}




		// 営業日休み一覧 2026年になったら順次追加
		private static readonly string[] restDates = new string[] {
			"2025/01/01","2025/01/02","2025/01/03","2025/01/13","2025/02/11","2025/02/23","2025/02/24",
			"2025/03/20","2025/04/29","2025/05/03","2025/05/04","2025/05/05","2025/05/06","2025/07/21",
			"2025/08/11","2025/09/15","2025/09/23","2025/10/13","2025/11/03","2025/11/23","2025/11/24",
			"2025/12/31",
		};
		// 営業日日時一覧 使いまわしそうなので一時保存
		private static List<string> dateList = new List<string>();
		// 2025/1/6を0と考えたときのインデックスを返す。引数が営業日でないときは次の営業日のもの
		public static int GetDateIdx(DateTime setDate)
		{
			int idx = 0;
			foreach (string dateString in GetDateList()) {
				//Console.WriteLine("Gdi:{0}:{1}", idx, dateString);
				if (Common.NewD2(setDate, DateTime.Parse(dateString))) return idx;
				idx++;
			}
			// ここまで来るのはエラーやな
			CsvControll.ErrorLog("GetDateIdx", idx.ToString(), setDate.ToString(), "");
			return 0;
		}
		public static DateTime GetDateByIdx(int setIdx)
		{
			int idx = 0;
			foreach (string dateString in GetDateList()) {
				//Console.WriteLine("Gdi:{0}:{1}", idx, dateString);
				if (idx == setIdx) return DateTime.Parse(dateString);
				idx++;
			}
			// ここまで来るのはエラーやな
			CsvControll.ErrorLog("GetDateByIdx", idx.ToString(), setIdx.ToString(), "");
			return DateTime.Parse("2000/01/01");
		}
		private static List<string> GetDateList()
		{
			if (dateList.Count == 0) {
				DateTime loopDate = new DateTime(2025, 1, 1);
				for (int i = 0; i < 730; i++) { // todo とりあえず2026年まで
					string youbi = loopDate.ToString("ddd");
					string dateString = loopDate.ToString(CsvControll.DFORM);
					loopDate = loopDate.AddDays(1);
					if (youbi == "土" || youbi == "日" || IsRestDate(DateTime.Parse(dateString))) continue;
					dateList.Add(dateString);
				}
			}
			return dateList;
		}
		/** 祝日 */
		public static bool IsRestDate(DateTime date)
		{
			return Array.IndexOf(restDates, date.ToString(CsvControll.DFORM)) >= 0;
		}
		/** 同日判定 */
		public static bool SameD(DateTime d1, DateTime d2)
		{
			return d1.ToString(CsvControll.DFILEFORM) == d2.ToString(CsvControll.DFILEFORM);
		}
		/** d2のほうが新しい(つまり大きい)か同じ */
		public static bool NewD2(DateTime d1, DateTime d2)
		{
			return (d1 - d2).TotalDays <= 0;
		}
		/** d2のほうが新しい(つまり大きい)か同じ 秒単位での比較 */
		public static bool NewD2Second(DateTime d1, DateTime d2)
		{
			return DateTime.Compare(d1, d2) <= 0;
		}

		/** yyyyMMdd をDateTimeにする */
		public static DateTime DateParse(int date)
		{
			return DateTime.ParseExact(date.ToString(), CsvControll.DFILEFORM, null);
		}



		/** 
		 * 日付による購入倍率 1 or 0.5 or 0 決算日7営業日前=>0も追加かな？ 
		 */
		public static double DateBuyRatioOne(DateTime date, DateTime fisDate)
		{
			double ratio = DateBuyRatioAll(date);
			if (ratio == 0 || IsInFisDate(date, fisDate, 7, 5)) return 0; // 決算日の7営業日前-5営業日後
			if (ratio == 0.5 || IsInFisDate(date, fisDate, 10, 5)) return 0.5; // 決算日の10営業日前-5営業日後
			return 1;
		}
		/** 
		 * 日付による購入倍率 全体用
		 */
		public static double DateBuyRatioAll(DateTime date)
		{
			if ((date.Month == 3 && date.Day >= 18) // 3/18-4/3
				|| (date.Month == 4 && date.Day <= 3)
				|| (date.Month == 12 && date.Day >= 19) // 12/19-1/5
				|| (date.Month == 1 && date.Day <= 5)) {
				return 0;
			}
			if ((date.Month == 3 && date.Day >= 9) || (date.Month == 12 && date.Day >= 14)) return 0.5;
			return 1;
		}

		/** 妥協した理想売りする日付 決算日7営業日前も */
		public static bool IsHalfSellDate(DateTime date, DateTime fisDate)
		{
			// トランプモード中は常にHalf
			if (Def.TranpMode) return true;

			return (date.Month == 3 && date.Day >= 9) // 3/9-3/31
				|| (date.Month == 12 && date.Day >= 14) // 12/14-12/31
				|| IsInFisDate(date, fisDate, 7, 2); // 決算日の7営業日前-2営業日後
		}
		/** 損切する日付 決算日3営業日前も追加かな？ */
		public static bool IsLossCutDate(DateTime date, DateTime fisDate)
		{
			return (date.Month == 3 && date.Day >= 23) // 3/23-3/31
				|| (date.Month == 12 && date.Day >= 24) // 12/24-12/31
				|| IsInFisDate(date, fisDate, 3, 2); // 決算日の3営業日前-2営業日後
		}

		/** 指定日が決算日の 〇営業日前/〇日後 以内であるかどうか */
		private static bool IsInFisDate(DateTime date, DateTime fisDate, int before, int after)
		{
			return GetDateIdx(fisDate) - before <= GetDateIdx(date)
				&& GetDateIdx(fisDate) + after >= GetDateIdx(date);
		}


	}
}
