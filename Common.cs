using System;
using System.Collections.Generic;

namespace CSharp_sample
{
	class Common
	{

		private static bool IsFirst = true;
		public static void DebugInfo(string tag, params object[] messages)
		{
			string message = tag + " ";
			int idx = 0;
			foreach (object obj in messages) {
				message += ", T" + idx + ":" + FormatObject(obj) + " ";
				idx++;
			}
			Console.WriteLine(message + " #End#");

			CsvControll.SaveDebugInfo(new List<string[]>() { new string[1] { message } }, !IsFirst);
			IsFirst = false;
		}
		private static string FormatObject(object o)
		{
			if (o is null) return "(null)";
			if (o as string == string.Empty) return "(empty)";
			return o.ToString();
		}
		public static double Round(double value, int keta = 0)
		{
			return Math.Round(value, keta, MidpointRounding.AwayFromZero);
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
			if (symbol == "6740") return 16; //todo 売れたら
			foreach (string[] info in CsvControll.GetSpInfo()) {
				if (info[0] == symbol) return Int32.Parse(info[1]);
			}
			CsvControll.ErrorLog("Sp10BuyPrice", symbol, "", "");
			return 0;
		}




		// 営業日休み一覧 2026年になったら順次追加
		private static readonly string[] restDates = new string[] {
			"2025/01/01","2025/01/02","2025/01/03","2025/01/13","2025/02/11","2025/02/23","2025/02/24","2025/03/20",
			"2025/04/29","2025/05/03","2025/05/04","2025/05/05","2025/05/06","2025/07/21","2025/08/11","2025/09/15",
			"2025/09/23","2025/10/13","2025/11/03","2025/11/23","2025/11/24","2025/12/31",
			"2024/01/01","2024/01/02","2024/01/03","2024/01/08","2024/02/12","2024/02/23","2024/03/20","2024/04/29",
			"2024/05/03","2024/05/04","2024/05/06","2024/07/15","2024/08/12","2024/09/16","2024/09/23","2024/10/14",
			"2024/11/04","2024/11/23","2024/12/31",
			"2023/01/01","2023/01/02","2023/01/03","2023/01/09","2023/02/11","2023/02/23","2023/03/21","2023/04/29",
			"2023/05/03","2023/05/04","2023/05/05","2023/07/17","2023/08/11","2023/09/18","2023/09/23","2023/10/09",
			"2023/11/03","2023/11/23","2023/12/31",
			"2022/01/01","2022/01/02","2022/01/03","2022/01/10","2022/02/11","2022/02/23","2022/03/21","2022/04/29",
			"2022/05/03","2022/05/04","2022/05/05","2022/07/18","2022/08/11","2022/09/19","2022/09/23","2022/10/10",
			"2022/11/03","2022/11/23","2022/12/31",
			"2021/01/01","2021/01/02","2021/01/03","2021/01/11","2021/02/11","2021/02/23","2021/03/20","2021/04/29",
			"2021/05/03","2021/05/04","2021/05/05","2021/07/22","2021/07/23","2021/08/09","2021/09/20","2021/09/23",
			"2021/11/03","2021/11/23","2021/12/31",
			"2020/01/01","2020/01/02","2020/01/03","2020/01/13","2020/02/11","2020/02/24","2020/03/20","2020/04/29",
			"2020/05/04","2020/05/05","2020/05/06","2020/07/23","2020/07/24","2020/08/10","2020/09/21","2020/09/22",
			"2020/11/03","2020/11/23","2020/12/31",
			"2019/01/01","2019/01/02","2019/01/03","2019/01/14","2019/02/11","2019/03/21","2019/04/29","2019/04/30",
			"2019/05/01","2019/05/02","2019/05/03","2019/05/06","2019/07/15","2019/08/12","2019/09/16","2019/09/23",
			"2019/10/14","2019/10/22","2019/11/04","2019/12/31",
			"2018/01/01","2018/01/08","2018/02/11","2018/02/12","2018/03/21","2018/04/29","2018/04/30",
			"2018/05/03","2018/05/04","2018/05/05","2018/07/16","2018/08/11","2018/09/17","2018/09/23","2018/09/24",
			"2018/10/08","2018/11/03","2018/11/23","2018/12/23","2018/12/24",
			"2017/01/01","2017/01/02","2017/01/09","2017/02/11","2017/03/20","2017/04/29",
			"2017/05/03","2017/05/04","2017/05/05","2017/07/17","2017/08/11","2017/09/18","2017/09/23",
			"2017/10/09","2017/11/03","2017/11/23","2017/12/23",
		};
		// 2025/1/6を0と考えたときのインデックスを返す。引数が営業日でないときは次の営業日のもの
		public static int GetDateIdx(DateTime setDate)
		{
			int idx = 0;
			bool isNew = false;
			foreach (DateTime date in GetDateList()) {
				if (NewD2(setDate, date)) {
					isNew = true;
				}else{
					if (isNew) return idx;
				}
				idx++;
			}
			// ここまで来るのはエラーやな
			CsvControll.ErrorLog("GetDateIdx", idx.ToString(), setDate.ToString(), "");
			return 0;
		}
		public static DateTime GetDateByIdx(int setIdx)
		{
			int idx = 0;
			foreach (DateTime date in GetDateList()) {
				if (idx == setIdx) return date;
				idx++;
			}
			// ここまで来るのはエラーやな
			CsvControll.ErrorLog("GetDateByIdx", idx.ToString(), setIdx.ToString(), "");
			return DateTime.Parse("2000/01/01");
		}
		// 営業日日時一覧 使いまわしそうなので一時保存
		private static HashSet<DateTime> dateList = new HashSet<DateTime>();
		private static HashSet<DateTime> GetDateList()
		{
			if (dateList.Count == 0) {
				DateTime loopDate = new DateTime(2025, 12, 31);// todo とりあえず2026年まで
				for (int i = 0; i < 365 * (2025 - 2017 + 1); i++) { // とりあえず2017年まで
					string youbi = loopDate.ToString("ddd");
					if (!(youbi == "土" || youbi == "日" || IsRestDate(loopDate))) dateList.Add(loopDate);
					loopDate = loopDate.AddDays(-1);
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
			if (date < 20000101 || date > 20500101) {
				CsvControll.ErrorLog("DateParse", date.ToString(), "", "");
				return DateTime.Parse("2026/03/31");
			}
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
			int proIdx = GetDateIdx(DateTime.Parse(Def.Pro500Day));
			if (proIdx - 5 < GetDateIdx(date) && proIdx >= GetDateIdx(date)) return 0.5;

			if (proIdx - 10 < GetDateIdx(date) && proIdx - 5 >= GetDateIdx(date)) return 0;
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
