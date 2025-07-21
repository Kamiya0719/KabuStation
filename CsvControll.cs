using System;
using System.Collections.Generic;
using System.IO;

namespace CSharp_sample
{

	class CsvControll
	{
		private enum FILE_TYPE
		{
			/* 外部から取得 */
			Pro500, // 今季のプロ500銘柄一覧(銘柄コードのみ？)
			Pro500All, // プロ500全部
			JapanRaw, // Webで仕入れてきた日経平均データの過去生データ
			SplitDate, // Webから取得した分割・合併銘柄日時情報
			OldDataRaw, // Excelで作った2000銘柄の過去生データ(巨大)
			BuyConditions, // 購入決定のための51条件一覧(とりあえず固定)

			/* コード情報 */
			Code, // 銘柄ごとの各値段一覧データ

			/* 毎日取得 */
			Basic, // 汎用情報(トークン/当日最大JScore)
			BaseJScore, // ベース日経平均スコア
			JScoreIkichis, // 当日の安値を見て当日暫定日経平均スコアを決定する閾値
			CodeDaily, // 銘柄各種情報
			CodeResOrder, // 注文一覧情報
			JapanCond, // 日経平均の2000日付分の全51条件に対するそれぞれのTF情報(検証用一時情報)
			RankingInfo, // 詳細ランキングに関する情報
			SpInfo, // Sp系情報
			DayMemo, // 毎日のチェック用メモ
			Board, // リクエストしたBoard情報を一時保存

			/* デバッグ用？過去データ */
			CodeResOrderOld, // 注文一覧情報 過去分
			CodeDailyOld, // 銘柄各種情報 過去分
			RankingInfoOld, // 詳細ランキングに関する情報 過去分

			/* ログ系 */
			Log, // ログ今日分
			ErrorLog, // エラーログ今日分
			SymbolLog, // シンボルログ
			LogOld, // ログ過去分
			ErrorLogOld, // エラーログ過去分

			/* 検証用一時情報 */
			BuyCode, // 購入可否情報(検証用一時情報)
			AllCodeList, // Excelに渡すようのデータ(色々使う？)
			CodeDispInfo, // 現在の銘柄情報確認用
			ResponseSymbol, // 銘柄情報レスポンス検証用保存
			DebugInfo, // DebugInfoで出力したやつをとりあえず保存しておく
			Cond51All, // 全ての51判定情報を一時保存しておく
			BenefitAll, // 全ての購入時利益情報を一時保存しておく
		}
		private enum FOLDER_TYPE { Import, Code, EveryDay, Old, Log, Debug, } // DayMemo		
																			  // ファイルタイプごとのフォルダ名
		private static readonly Dictionary<FILE_TYPE, FOLDER_TYPE> FolderTypes = new Dictionary<FILE_TYPE, FOLDER_TYPE>() {
			/* 外部から取得 */
			{FILE_TYPE.Pro500, FOLDER_TYPE.Import }, // 今季のプロ500銘柄一覧(銘柄コードのみ？)
			{FILE_TYPE.Pro500All, FOLDER_TYPE.Import }, // プロ500全部
			{FILE_TYPE.JapanRaw, FOLDER_TYPE.Import }, // Webで仕入れてきた日経平均データの過去生データ
			{FILE_TYPE.SplitDate, FOLDER_TYPE.Import }, // Webから取得した分割・合併銘柄日時情報
			{FILE_TYPE.OldDataRaw, FOLDER_TYPE.Import }, // Excelで作った2000銘柄の過去生データ(巨大)
			{FILE_TYPE.BuyConditions, FOLDER_TYPE.Import }, // 購入決定のための51条件一覧(とりあえず固定)
			/* コード情報 */
			{FILE_TYPE.Code, FOLDER_TYPE.Code }, // 銘柄ごとの各値段一覧データ
			/* 毎日取得 */
			{FILE_TYPE.Basic, FOLDER_TYPE.EveryDay }, // 汎用情報(トークン/当日最大JScore/推定登録中銘柄情報)
			{FILE_TYPE.BaseJScore, FOLDER_TYPE.EveryDay }, // 日経平均スコア
			{FILE_TYPE.JScoreIkichis, FOLDER_TYPE.EveryDay }, // 当日の安値を見て当日暫定日経平均スコアを決定する閾値
			{FILE_TYPE.CodeDaily, FOLDER_TYPE.EveryDay }, // 銘柄各種情報
			{FILE_TYPE.CodeResOrder, FOLDER_TYPE.EveryDay }, // 注文一覧情報		
			{FILE_TYPE.JapanCond, FOLDER_TYPE.EveryDay }, // 日経平均の2000日付分の全51条件に対するそれぞれのTF情報
			{FILE_TYPE.RankingInfo, FOLDER_TYPE.EveryDay }, // 詳細ランキング情報
			{FILE_TYPE.SpInfo, FOLDER_TYPE.EveryDay }, // Sp系情報
			{FILE_TYPE.Board, FOLDER_TYPE.EveryDay }, // リクエストしたBoard情報を一時保存			
			//{FILE_TYPE.DayMemo, FOLDER_TYPE.EveryDay }, // 毎日のチェック用メモ
			/* デバッグ用？過去データ */
			{FILE_TYPE.CodeResOrderOld, FOLDER_TYPE.Old }, // 注文一覧情報 過去分
			{FILE_TYPE.CodeDailyOld, FOLDER_TYPE.Old }, // 銘柄各種情報 過去分
			{FILE_TYPE.RankingInfoOld, FOLDER_TYPE.Old }, // 詳細ランキングに関する情報 過去分			
			/* ログ系 */
			{FILE_TYPE.Log, FOLDER_TYPE.Log }, // ログ今日分(タグごとにファイル分ける？)
			{FILE_TYPE.ErrorLog, FOLDER_TYPE.Log }, // エラーログ今日分(タグごとにファイル分ける？)
			{FILE_TYPE.SymbolLog, FOLDER_TYPE.Log }, // シンボルログ今日分(タグごとにファイル分ける？)
			{FILE_TYPE.LogOld, FOLDER_TYPE.Log }, // ログ過去分
			{FILE_TYPE.ErrorLogOld, FOLDER_TYPE.Log }, // エラーログ過去分
			/* 検証用一時情報 */
			{FILE_TYPE.BuyCode, FOLDER_TYPE.Debug }, // 購入可否情報(検証用一時情報)
			{FILE_TYPE.AllCodeList, FOLDER_TYPE.Debug }, // 日経平均の2000日付分の全51条件に対するそれぞれのTF情報(検証用一時情報)		
			{FILE_TYPE.CodeDispInfo, FOLDER_TYPE.Debug }, // 銘柄各種情報表示
			{FILE_TYPE.ResponseSymbol, FOLDER_TYPE.Debug }, // 銘柄各種情報表示
			{FILE_TYPE.DebugInfo, FOLDER_TYPE.Debug }, // デバッグ情報一時保存
			{FILE_TYPE.Cond51All, FOLDER_TYPE.Debug }, // 全ての51判定情報を一時保存しておく
			{FILE_TYPE.BenefitAll, FOLDER_TYPE.Debug }, // 全ての購入時利益情報を一時保存しておく		
		};

		// ファイルタイプごとのファイル名
		private static readonly Dictionary<FILE_TYPE, string> FileNames = new Dictionary<FILE_TYPE, string>() {
			/* 外部から取得 */
			{FILE_TYPE.Pro500, @"Import\Pro500" }, // 今季のプロ500銘柄一覧(銘柄コードのみ？)
			{FILE_TYPE.Pro500All, @"Import\Pro500All" }, // プロ500全部
			{FILE_TYPE.JapanRaw, @"Import\JapanRaw" }, // Webで仕入れてきた日経平均データの過去生データ
			{FILE_TYPE.SplitDate, @"Import\SplitDate" }, // Webから取得した分割・合併銘柄日時情報
			{FILE_TYPE.OldDataRaw, @"Import\OldDataRaw" }, // Excelで作った2000銘柄の過去生データ(巨大)
			{FILE_TYPE.BuyConditions, @"Import\BuyConditions" }, // 購入決定のための51条件一覧(とりあえず固定)
			/* コード情報 */
			{FILE_TYPE.Code, @"Code\" }, // 銘柄ごとの各値段一覧データ
			/* 毎日取得 */
			{FILE_TYPE.Basic, @"EveryDay\Basic" }, // 汎用情報(トークン/当日最大JScore/推定登録中銘柄情報)
			{FILE_TYPE.BaseJScore, @"EveryDay\BaseJScore" }, // 日経平均スコア
			{FILE_TYPE.JScoreIkichis, @"EveryDay\JScoreIkichis" }, // 当日の安値を見て当日暫定日経平均スコアを決定する閾値
			{FILE_TYPE.CodeDaily, @"EveryDay\CodeDaily" }, // 銘柄各種情報
			{FILE_TYPE.CodeResOrder, @"EveryDay\CodeResOrder" }, // 注文一覧情報		
			{FILE_TYPE.JapanCond, @"EveryDay\JapanCond\" }, // 日経平均の2000日付分の全51条件に対するそれぞれのTF情報
			{FILE_TYPE.RankingInfo, @"EveryDay\RankingInfo" }, // 詳細ランキング情報
			{FILE_TYPE.SpInfo, @"EveryDay\SpInfo" }, // Sp系情報
			{FILE_TYPE.DayMemo, @"DayMemo" }, // 毎日のチェック用メモ
			{FILE_TYPE.Board, @"EveryDay\Board" }, // リクエストしたBoard情報を一時保存			
			/* デバッグ用？過去データ */
			{FILE_TYPE.CodeResOrderOld, @"Old\CodeResOrderOld\" }, // 注文一覧情報 過去分
			{FILE_TYPE.CodeDailyOld, @"Old\CodeDailyOld\" }, // 銘柄各種情報 過去分
			{FILE_TYPE.RankingInfoOld, @"Old\RankingInfoOld\" }, // 詳細ランキングに関する情報 過去分			
			/* ログ系 */
			{FILE_TYPE.Log, @"Log\Log" }, // ログ今日分(タグごとにファイル分ける？)
			{FILE_TYPE.ErrorLog, @"Log\ErrorLog" }, // エラーログ今日分(タグごとにファイル分ける？)
			{FILE_TYPE.SymbolLog, @"Log\SymbolLog\" }, // シンボルログ今日分(タグごとにファイル分ける？)
			{FILE_TYPE.LogOld, @"Log\LogOld\" }, // ログ過去分
			{FILE_TYPE.ErrorLogOld, @"Log\ErrorLogOld\" }, // エラーログ過去分
			/* 検証用一時情報 */
			{FILE_TYPE.BuyCode, @"Debug\BuyCode\" }, // 購入可否情報(検証用一時情報)
			{FILE_TYPE.AllCodeList, @"Debug\AllCodeList\" }, // 日経平均の2000日付分の全51条件に対するそれぞれのTF情報(検証用一時情報)		
			{FILE_TYPE.CodeDispInfo, @"Debug\CodeDispInfo\" }, // 銘柄各種情報表示
			{FILE_TYPE.ResponseSymbol, @"Debug\ResponseSymbol" }, // 銘柄各種情報表示
			{FILE_TYPE.DebugInfo, @"Debug\DebugInfo" }, // デバッグ情報一時保存
			{FILE_TYPE.Cond51All, @"Debug\Cond51All\" }, // 全ての51判定情報を一時保存しておく
			{FILE_TYPE.BenefitAll, @"Debug\BenefitAll\" }, // 全ての購入時利益情報を一時保存しておく		
		};
		public const string DFORM = "yyyy/MM/dd";
		public const string DFILEFORM = "yyyyMMdd";

		// コード一覧を取得(スキップ対象の銘柄は除外)
		private static readonly int[] skipCodes = new int[] {
			1775,2715,3275,4955,6164,8114,9058,9232,

			//2157, 2667, 3778, 3989, 3996, 4169, 4371, 4388, 4485, 4563, 4572, 4583, 4586, 4586, 4592, 4592, 4599, 4833, 4833, 4883,
			//4883, 5759, 5759, 6072, 6579, 6676, 8103, 8946, 9519,
			2157,2217,2585,2934,2938,2998,3697,4125,4169,4258,4260,4263,4270,4371,4373,4374,4376,4377,4388,4413,
			4417,4419,4485,4563,4811,5076,5078,5599,5621,5888,5889,5892,6224,6226,6524,6579,6676,7126,7128,7130,
			7133,7134,7380,7381,8103,9147,9166,9211,9214,9218,9219,9246,9247,9248,9259,9340,9341,9343,9560,9564,
			5588,5830,5832,7383,1973,3254,3738,3857,3990,7342,7518,8732,8890,
			1301,1332,1333,1375,1376,
			9766,7803,1717,3498,3612,9305,
			1417,5038,5132,5253,5356,5592,5831,5834,5838,9023, // データが少ないが消さなくてもいいような気も
		};
		public static List<string> GetCodeList()
		{
			//char[] delimiterChars = { '\\', '.' };
			List<string> list = new List<string>();
			for (int f = 1000; f <= 9000; f += 1000) {
				foreach (string file in Directory.EnumerateFiles(GetFilePath(FILE_TYPE.Code, f.ToString(), false), "*", SearchOption.TopDirectoryOnly)) {
					//string[] words = file.Split(delimiterChars);
					//if (Array.IndexOf(skipCodes, Int32.Parse(words[words.Length - 2])) >= 0) continue;
					//list.Add(words[words.Length - 2]);

					string symbol = Path.GetFileNameWithoutExtension(file);
					if (Array.IndexOf(skipCodes, Int32.Parse(symbol)) >= 0) continue;
					list.Add(symbol);

					//Common.DebugInfo("AAA", f, symbol);
				}
			}
			return list;
		}
		public static void SaveCodeInfo(string code, List<string[]> datas, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.Code, CodeAddName(code), datas, isAddWrite);
		}
		// コードデータを取得(日付,始値,高値,安値,終値)
		public static List<string[]> GetCodeInfo(string symbol)
		{
			// 分割・合併を考慮して各種値段を現在基準へと変換する
			Dictionary<string, Dictionary<DateTime, double>> splitDate = GetSplitDate();
			List<string[]> res = new List<string[]>();
			foreach (string[] info in GetCsvDatas(FILE_TYPE.Code, CodeAddName(symbol))) {
				double ratio = 1.0;
				if (splitDate.ContainsKey(symbol)) {
					foreach (KeyValuePair<DateTime, double> pair in splitDate[symbol]) {
						if (Common.NewD2(DateTime.Parse(info[0]), pair.Key)) ratio /= pair.Value;
					}
				}
				res.Add(new string[] {
					info[0],(Double.Parse(info[1])*ratio).ToString(),(Double.Parse(info[2])*ratio).ToString(),(Double.Parse(info[3])*ratio).ToString(),(Double.Parse(info[4])*ratio).ToString()
				});
			}

			return res;
		}
		private static string CodeAddName(string code)
		{
			string folder = (Int32.Parse(code) - Int32.Parse(code) % 1000).ToString();
			return Path.Combine(folder, code);
		}
		// 日付一覧を取得(代表として1301を用いる)
		public static List<DateTime> GetDateList()
		{
			List<DateTime> list = new List<DateTime>();
			foreach (string[] info in GetCodeInfo(Def.CapitalSymbol)) list.Add(DateTime.Parse(info[0]));
			return list;
		}

		// Excelで作った2000コード過去データ情報を取得
		public static List<string[]> GetOldDataRaw() { return GetCsvDatas(FILE_TYPE.OldDataRaw); }
		// 日経平均過去データを取得
		public static List<string[]> GetJapanRaw() { return GetCsvDatas(FILE_TYPE.JapanRaw); }
		// 51条件一覧を取得
		public static List<string[]> GetConditions()
		{
			if (true) {
				return Condtions.GetNewConditions();
			}
			return GetCsvDatas(FILE_TYPE.BuyConditions);
		}
		// 購入可否情報を取得
		public static List<string[]> GetBuyCode(string code) { return GetCsvDatas(FILE_TYPE.BuyCode, code); }
		// 購入可否情報をセーブ
		public static void SaveBuyCode(string code, List<string[]> datas) { SaveCsvDatas(FILE_TYPE.BuyCode, code, datas); }
		// 日経平均の各日付の値を取得
		public static List<string[]> GetJapanInfo() { return GetCodeInfo(Def.JapanSymbol); }
		// 日経平均 条件に対するTF
		public static void SaveJapanCond(int condIdx, List<string[]> datas, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.JapanCond, condIdx.ToString(), datas, isAddWrite);
		}
		// 日経平均 条件に対するTF
		public static List<string[]> GetJapanCond(int condIdx) { return GetCsvDatas(FILE_TYPE.JapanCond, condIdx.ToString()); }
		// コードの分割・合併日
		public static Dictionary<string, Dictionary<DateTime, double>> GetSplitDate()
		{
			Dictionary<string, Dictionary<DateTime, double>> splitDate = new Dictionary<string, Dictionary<DateTime, double>>();
			foreach (string[] sp in GetCsvDatas(FILE_TYPE.SplitDate)) {
				if (!splitDate.ContainsKey(sp[0])) splitDate[sp[0]] = new Dictionary<DateTime, double>();
				//if(sp[0]=="1852") Common.DebugInfo("GetSplitDate", sp[0], sp[1], sp[2], sp[3]);
				splitDate[sp[0]][DateTime.Parse(sp[1])] = Double.Parse(sp[3]) / Double.Parse(sp[2]);
			}
			return splitDate;
		}

		// 日経平均スコア保存
		public static void SaveBaseJScores(List<string[]> datas, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.BaseJScore, "", datas, isAddWrite);
		}
		// 日経平均スコアを取得
		public static int GetBaseJScore(DateTime setDate)
		{
			foreach (string[] info in GetCsvDatas(FILE_TYPE.BaseJScore)) {
				if (Common.SameD(setDate, DateTime.Parse(info[0]))) return Int32.Parse(info[1]);
			}
			return -99;
		}
		// 指定日・前日・前々日の日付の生日経平均スコアを取得してTrueスコア取得
		public static int GetTrueJScore(DateTime setDate)
		{
			int[] rawScores = new int[3] { -1, -1, -1 };
			for (int i = 0; i < 3; i++) rawScores[i] = GetBaseJScore(Common.GetDateByIdx(Common.GetDateIdx(setDate) - i));
			return Condtions.ConvertTrueJScore(rawScores[0], rawScores[1], rawScores[2]);
		}

		// コードデイリー情報を保存
		public static void SaveCodeDaily(List<string[]> datas, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.CodeDaily, "", datas, isAddWrite);
		}
		// コードデイリー情報を取得
		public static List<string[]> GetCodeDaily() { return GetCsvDatas(FILE_TYPE.CodeDaily); }
		// コードデイリー情報を過去データとして保存
		public static void SaveCodeDailyOld(List<string[]> datas, DateTime date)
		{
			SaveCsvDatas(FILE_TYPE.CodeDailyOld, date.ToString(DFILEFORM), datas, true);
		}
		public static List<string[]> GetCodeDailyOld(DateTime date)
		{
			return GetCsvDatas(FILE_TYPE.CodeDailyOld, date.ToString(DFILEFORM));
		}

		// コードデイリー情報を保存
		public static void SaveCodeResOrder(List<string[]> datas, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.CodeResOrder, "", datas, isAddWrite);
		}
		// コードデイリー情報を取得
		public static List<string[]> GetCodeResOrder() { return GetCsvDatas(FILE_TYPE.CodeResOrder); }
		// コードデイリー情報を過去データとして保存
		public static void SaveCodeResOrderOld(List<string[]> datas, DateTime date)
		{
			SaveCsvDatas(FILE_TYPE.CodeResOrderOld, date.ToString(DFILEFORM), datas, true);
		}
		public static List<string[]> GetCodeResOrderOld(DateTime date)
		{
			return GetCsvDatas(FILE_TYPE.CodeResOrderOld, date.ToString(DFILEFORM));
		}


		// 毎日のチェック用メモ
		public static void SaveDayMemo(List<string[]> datas, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.DayMemo, "", datas, isAddWrite);
		}
		// 毎日のチェック用メモ
		public static List<string[]> GetDayMemo() { return GetCsvDatas(FILE_TYPE.DayMemo); }


		// リクエストしたBoard情報を一時保存
		public static void SaveBoard(List<string[]> datas, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.Board, "", datas, isAddWrite);
		}
		// リクエストしたBoard情報を一時保存
		public static List<string[]> GetBoard() { return GetCsvDatas(FILE_TYPE.Board); }



		// デバッグ情報
		public static void SaveDebugInfo(List<string[]> datas, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.DebugInfo, "", datas, isAddWrite);
		}


		// 全ての51判定情報を一時保存しておく
		public static void SaveCond51All(List<string[]> datas, string symbol, int diffDayIdx, int ratioIdx)
		{
			string addName = Path.Combine(diffDayIdx.ToString(), ratioIdx.ToString(), symbol);
			SaveCsvDatas(FILE_TYPE.Cond51All, addName, datas);
		}
		public static List<string[]> GetCond51All(string symbol, int diffDayIdx, int ratioIdx)
		{
			string addName = Path.Combine(diffDayIdx.ToString(), ratioIdx.ToString(), symbol);
			return GetCsvDatas(FILE_TYPE.Cond51All, addName);
		}

		// 全ての購入時利益情報を一時保存しておく
		public static void SaveBenefitAll(List<string[]> datas, string symbol)
		{
			SaveCsvDatas(FILE_TYPE.BenefitAll, symbol, datas);
		}
		public static List<string[]> GetBenefitAll(string symbol)
		{
			return GetCsvDatas(FILE_TYPE.BenefitAll, symbol);
		}



		// 翌日日経平均スコア仮閾値を保存
		public static void SaveTrueJScoreIkichis(List<string[]> datas, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.JScoreIkichis, "", datas, isAddWrite);
		}
		// 翌日日経平均スコア仮閾値を取得
		public static List<string[]> GetJScoreIkichis() { return GetCsvDatas(FILE_TYPE.JScoreIkichis); }
		// 詳細ランキング
		public static void SaveRankingInfo(List<string[]> datas, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.RankingInfo, "", datas, isAddWrite);
		}
		// 詳細ランキング
		public static List<string[]> GetRankingInfo() { return GetCsvDatas(FILE_TYPE.RankingInfo); }
		// 詳細ランキング Old
		public static void SaveRankingInfoOld(List<string[]> datas, DateTime date, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.RankingInfoOld, date.ToString(DFILEFORM), datas, isAddWrite);
		}
		// 詳細ランキング Old
		public static List<string[]> GetRankingInfoOld(DateTime date) { return GetCsvDatas(FILE_TYPE.RankingInfoOld, date.ToString(DFILEFORM)); }
		public static List<string> GetRankingOldList()
		{
			//char[] delimiterChars = { '\\', '.' };
			List<string> list = new List<string>();
			foreach (string file in Directory.EnumerateFiles(GetFilePath(FILE_TYPE.RankingInfoOld, "", false), "*", SearchOption.TopDirectoryOnly)) {
				//string[] words = file.Split(delimiterChars);
				//list.Add(words[words.Length - 2]);
				list.Add(Path.GetFileNameWithoutExtension(file));
			}
			return list;
		}

		// Sp系情報
		public static void SaveSpInfo(List<string[]> datas, bool isAddWrite = false)
		{
			SaveCsvDatas(FILE_TYPE.SpInfo, "", datas, isAddWrite);
		}
		public static List<string[]> GetSpInfo() { return GetCsvDatas(FILE_TYPE.SpInfo); }


		/**
		 * Basic系開始
		 */

		// トークン情報を保存
		public static void SaveTokenInfo(string[] datas) { SaveBasicInfo(0, datas); }
		// トークン情報を取得
		public static string[] GetTokenInfo() { return GetBasicInfo(0); }
		// 当日Minites情報を保存(とりあえず最大JScoreのみ？)
		public static void SaveMinitesInfo(string[] datas) { SaveBasicInfo(1, datas); }
		// 当日Minites情報を取得
		public static string[] GetMinitesInfo() { return GetBasicInfo(1); }
		// 推定登録中銘柄情報を保存
		public static void SaveRegisterInfo(string[] datas) { SaveBasicInfo(2, datas); }
		// 推定登録中銘柄情報を取得
		public static string[] GetRegisterInfo() { return GetBasicInfo(2); }
		// 今日の購入ベース額を保存
		public static void SaveBuyBasePriceInfo(string[] datas) { SaveBasicInfo(3, datas); }
		// 今日の購入ベース額を取得
		public static string[] GetBuyBasePriceInfo() { return GetBasicInfo(3); }

		/**
		 * Basic系終了
		 */


		// プロ500を取得 とりあえず1行500列
		public static List<string[]> GetPro500() { return GetCsvDatas(FILE_TYPE.Pro500); }
		private static List<string>[] Pro500All = null;
		public static List<string>[] GetPro500All()
		{
			if (Pro500All != null) return Pro500All;
			var raws = GetCsvDatas(FILE_TYPE.Pro500All);
			for (int i = 0; i < raws.Count; i++) {
				string[] info = raws[i];
				if (i == 0) {
					Pro500All = new List<string>[info.Length];
					for (int j = 0; j < info.Length; j++) Pro500All[j] = new List<string>();
				}
				for (int j = 0; j < info.Length; j++) Pro500All[j].Add(info[j]);
			}
			return Pro500All;
		}

		// 全銘柄コード一覧を取得
		public static void SaveAllCodeList(List<string[]> datas) { SaveCsvDatas(FILE_TYPE.AllCodeList, "", datas); }
		// 全銘柄コード一覧を取得
		//public static List<string[]> GetAllCodeList() { return GetCsvDatas(FILE_TYPE.AllCodeList); }

		// 検証用 各種現在銘柄情報表示ファイル
		public static void SaveCodeDispInfo(string symbol, List<string[]> datas) { SaveCsvDatas(FILE_TYPE.CodeDispInfo, symbol, datas); }
		// 検証用 各種現在銘柄情報表示ファイル
		public static List<string[]> GetCodeDispInfo(string symbol) { return GetCsvDatas(FILE_TYPE.CodeDispInfo, symbol); }
		// 検証用 リクエスト保存用
		public static void SaveResponseSymbol(List<string[]> datas, bool isAddWrite) { SaveCsvDatas(FILE_TYPE.ResponseSymbol, "", datas, isAddWrite); }
		// 検証用 リクエスト保存用
		public static List<string[]> GetResponseSymbol() { return GetCsvDatas(FILE_TYPE.ResponseSymbol); }


		// ログ情報を保存
		public static void Log(string tag, string d1, string d2, string d3)
		{
			Common.DebugInfo("Log", tag, d1, d2, d3);
			string[] data = new string[5] { DateTime.Now.ToString(), tag, d1, d2, d3 };
			SaveCsvDatas(FILE_TYPE.Log, "", new List<string[]>() { data }, true);
		}
		// ログ情報を取得
		public static List<string[]> GetLog() { return GetCsvDatas(FILE_TYPE.Log); }
		// ログ情報をリセット
		public static void ResetLog() { SaveCsvDatas(FILE_TYPE.Log, "", new List<string[]>()); }
		// ログ情報を過去データとして保存
		public static void SaveLogOld(DateTime date) { SaveCsvDatas(FILE_TYPE.LogOld, date.ToString(DFILEFORM), GetLog(), true); }
		// エラーログ情報を保存
		public static void ErrorLog(string tag, string d1, string d2, string d3)
		{
			Common.DebugInfo("ErrorLog", tag, d1, d2, d3);
			string[] data = new string[5] { DateTime.Now.ToString(), tag, d1, d2, d3 };
			SaveCsvDatas(FILE_TYPE.ErrorLog, "", new List<string[]>() { data }, true);
		}
		// エラーログ情報を取得
		public static List<string[]> GetErrorLog() { return GetCsvDatas(FILE_TYPE.ErrorLog); }
		// エラーログ情報をリセット
		public static void ResetErrorLog() { SaveCsvDatas(FILE_TYPE.ErrorLog, "", new List<string[]>()); }
		// エラーログ情報を過去データとして保存
		public static void SaveErrorLogOld(DateTime date)
		{
			List<string[]> list = GetErrorLog();
			if (list.Count > 0) SaveCsvDatas(FILE_TYPE.ErrorLogOld, date.ToString(DFILEFORM), GetErrorLog(), true);
		}
		public static List<string[]> GetErrorLogOld(DateTime date) { return GetCsvDatas(FILE_TYPE.ErrorLogOld, date.ToString(DFILEFORM)); }


		private static Dictionary<string, List<string[]>> symbolLogs = new Dictionary<string, List<string[]>>();
		// シンボルログ情報を保存
		public static void SymbolLog(string symbol, string tag, string d1, string d2 = "", string d3 = "")
		{
			if (!symbolLogs.ContainsKey(symbol)) symbolLogs[symbol] = new List<string[]>();
			symbolLogs[symbol].Add(new string[5] { DateTime.Now.ToString(), tag, d1, d2, d3 });
		}
		public static void FlushSymbolLog()
		{
			string dateString = DateTime.Today.ToString(DFILEFORM);
			foreach (KeyValuePair<string, List<string[]>> pair in symbolLogs) {
				CreateFolder(FILE_TYPE.SymbolLog, pair.Key);
				SaveCsvDatas(FILE_TYPE.SymbolLog, Path.Combine(pair.Key, pair.Key + "_" + dateString + "_SymbolLog"), pair.Value, true);
			}
		}


		/// <summary>
		/// 汎用private ///
		/// </summary>

		// 汎用データ取得
		private static List<string[]> GetCsvDatas(FILE_TYPE type, string addName = "")
		{
			List<string[]> datas = new List<string[]>();
			if (File.Exists(GetFilePath(type, addName))) {
				// 第二引数が「true」 → 追加書き込みOK,「false」→ 追加書き込みせず、上書きして書き込む
				using (StreamReader rfile = new StreamReader(GetFilePath(type, addName))) {
					// 行末まで１行ずつ読み込む
					while (rfile.Peek() != -1) datas.Add(rfile.ReadLine().Split(','));
				}
			}
			return datas;
		}

		// 汎用セーブ
		private static void SaveCsvDatas(FILE_TYPE type, string addName, List<string[]> datas, bool isAddWrite = false)
		{
			if (File.Exists(GetFilePath(type, addName))) {
				// 第二引数が「true」 → 追加書き込みOK,「false」→ 追加書き込みせず、上書きして書き込む
				using (StreamWriter file = new StreamWriter(GetFilePath(type, addName), isAddWrite)) {
					foreach (string[] data in datas) file.WriteLine(String.Join(",", data));
				}
			} else {
				string[] contexts = new string[datas.Count];
				for (int i = 0; i < datas.Count; i++) contexts[i] = String.Join(",", datas[i]);
				File.WriteAllLines(GetFilePath(type, addName), contexts);
			}
		}

		// 汎用ファイル削除
		private static void DeleteFile(FILE_TYPE type, string addName)
		{
			if (File.Exists(GetFilePath(type, addName))) File.Delete(GetFilePath(type, addName));
		}

		private static string GetFilePath(FILE_TYPE type, string addName, bool isCsv = true)
		{
			string path;
			if (type == FILE_TYPE.DayMemo) {
				path = Path.Combine(FilePath(), "..", type.ToString(), addName);
			}else		if (type == FILE_TYPE.Code) {
				path = Path.Combine(FilePath(), type.ToString(), addName);
			}else{
				path = Path.Combine(FilePath(), FolderTypes[type].ToString(), type.ToString(), addName);
			}
			return isCsv ? path + ".csv" : path;
		}

		private static void CreateFolder(FILE_TYPE type, string addName)
		{
			string path = GetFilePath(type, addName, false);
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);
		}
		public static void CreateFolders()
		{
			for (int diffDayIdx = 0; diffDayIdx < Condtions.diffDayList.Length; diffDayIdx++) {
				for (int ratioIdx = 0; ratioIdx < Condtions.ratioList.Length; ratioIdx++) {
					string addName = Path.Combine(diffDayIdx.ToString(), ratioIdx.ToString());
					CreateFolder(FILE_TYPE.Cond51All, addName);
				}
			}
		}
		private static string FilePath() { return Secret.FilePath; }
		public static void FileTest()
		{
			Console.WriteLine("GetCurrentDirectory:" + Directory.GetCurrentDirectory());
			Console.WriteLine("FilePath:" + FilePath());
			Console.WriteLine("GetFilePath:" + GetFilePath(FILE_TYPE.Pro500, ""));
			Console.WriteLine("GetFilePath:" + GetFilePath(FILE_TYPE.DayMemo, ""));
			foreach (FILE_TYPE type in Enum.GetValues(typeof(FILE_TYPE))) {
				string add = "";
				bool isOk = false;
				if ((new List<FILE_TYPE>() { FILE_TYPE.AllCodeList, FILE_TYPE.LogOld, FILE_TYPE.SymbolLog }).Contains(type)) {
					continue; // 未使用
				}
				if (type == FILE_TYPE.Code) {
					isOk = GetCodeInfo(Def.CapitalSymbol)[0][0] != "" && GetCodeList().Count > 2000;
				} else if (type == FILE_TYPE.BenefitAll) {
					isOk = GetBenefitAll(Def.CapitalSymbol)[0][0] != "";
				} else if (type == FILE_TYPE.BuyCode) {
					isOk = GetBuyCode(Def.CapitalSymbol)[0][0] != "";
				} else if (type == FILE_TYPE.CodeDispInfo) {
					isOk = GetCodeDispInfo(Def.CapitalSymbol)[0][0] != "";
				} else if (type == FILE_TYPE.Cond51All) {
					isOk = GetCond51All(Def.CapitalSymbol, 0, 0)[0][0] != "";
				} else if (type == FILE_TYPE.JapanCond) {
					isOk = GetJapanCond(189)[0][0] != "";
				} else if (type == FILE_TYPE.ErrorLogOld) {
					isOk = GetErrorLogOld(DateTime.Parse("2025/07/18"))[0][0] != "";
				} else if (type == FILE_TYPE.CodeDailyOld) {
					isOk = GetCodeDailyOld(DateTime.Parse("2025/07/18"))[0][0] != "";
				} else if (type == FILE_TYPE.CodeResOrderOld) {
					isOk = GetCodeResOrderOld(DateTime.Parse("2025/07/18"))[0][0] != "";
				} else if (type == FILE_TYPE.RankingInfoOld) {
					isOk = GetRankingInfoOld(DateTime.Parse("2025/07/17"))[0][0] != "";
				} else { // その他 単体ファイル
					isOk = File.Exists(GetFilePath(type, add));
				}
				Console.WriteLine((isOk ? "Ok:" : "\n\n\n\n!!!!!!!NoExists!!!!!!:") + type.ToString());
			}
		}


		private static string[] GetBasicInfo(int row)
		{
			List<string[]> allDatas = GetCsvDatas(FILE_TYPE.Basic);
			return allDatas.Count >= row + 1 ? allDatas[row] : new string[0];
		}

		private static void SaveBasicInfo(int row, string[] data)
		{
			List<string[]> allDatas = GetCsvDatas(FILE_TYPE.Basic);
			// 足りてなければ埋めておく
			for (int i = allDatas.Count; i <= row; i++) allDatas.Add(new string[0]);
			allDatas[row] = data;
			SaveCsvDatas(FILE_TYPE.Basic, "", allDatas);
		}

	}
}
