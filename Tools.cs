using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharp_sample
{
	class Tools
	{

		// 代表となる銘柄 最新まであることが必須 プロ500にいなければならない
		public const string CapitalSymbol = "1417";

		/*
		1.「全コード取得用20250204 - コピー」をポチって作成
		2.Sheet1をコピーしてImport\OldDataRaw.csvとして保存
		3.SetOldData();
		4.https://jp.investing.com/indices/japan-ni225-historical-data で日経平均過去データをダウンロードしてImport\JapanRaw.csvとして保存
		5.SetJapanOldData();
		6.DataChecker();

		 */

		// 2000コードの一覧にかぶりがないものを追加
		public static void AddNewPro500()
		{
			List<string> codeList = CsvControll.GetCodeList();
			List<string> newCodes = new List<string>();
			foreach (string[] pro500 in CsvControll.GetPro500()) {
				string symbol = pro500[0];
				if (!codeList.Contains(symbol)) newCodes.Add(symbol);
			}
			foreach (string symbol in newCodes) {
				CsvControll.Log("AddNewPro500", symbol, "", "");
				//CsvControll.SaveCodeInfo(symbol, new List<string[]>() { });
			}
		}

		// 2000コードの一覧セット(Excelで情報入手できない銘柄も含む)
		public static void SaveAllCodeList()
		{
			List<string> codeList = CsvControll.GetCodeList();
			// 文字列順に並べる とりあえず4桁オンリーだしまあええやろ
			codeList.Sort();
			List<string[]> saveDatas = new List<string[]>();
			foreach (string symbol in codeList) saveDatas.Add(new string[1] { symbol });
			CsvControll.SaveAllCodeList(saveDatas);
		}

		// excel側のデータから過去データcsvの作成 nowCodeListのうちoldにない銘柄についてはファイル削除
		public static void SetOldData()
		{
			//List<string> nowCodeList = CsvControll.GetCodeList();

			Dictionary<string, List<string[]>> list = new Dictionary<string, List<string[]>>();
			int raw = 0;
			List<string> codeList = new List<string>();
			foreach (string[] values in CsvControll.GetOldDataRaw()) {
				int i = 0;
				string[] oneData = new string[5];
				foreach (string value in values) {
					// iに応じて銘柄コードが必要やな + インデックス(0-4)
					if (raw == 0) {
						// 銘柄コード
						if (i % 5 == 1) {
							list[value] = new List<string[]>();
							codeList.Add(value);
						}
					} else {
						string code = codeList[i / 5];
						if (i % 5 == 0) oneData = new string[5];
						oneData[i % 5] = value;
						// 日付が必要
						if (i % 5 == 4 && oneData[0] != "") list[code].Add(oneData);
					}
					i++;
				}
				raw++;
			}

			Common.DebugInfo("SetOldDataCount", list.Count);

			List<DateTime> dateList = new List<DateTime>();
			foreach (KeyValuePair<string, List<string[]>> pair in list) {
				if (pair.Key != Tools.CapitalSymbol) continue;
				for (int i = pair.Value.Count - 1; i >= 0; i--) dateList.Add(DateTime.Parse(pair.Value[i][0]));
			}

			foreach (KeyValuePair<string, List<string[]>> pair in list) {
				Dictionary<DateTime, string[]> writeDatas = new Dictionary<DateTime, string[]>();
				string beforeEndPrice = ""; // 前日の終値
				for (int i = pair.Value.Count - 1; i >= 0; i--) {
					// data[0]が日付,data[1-4]がデータ
					string[] data = pair.Value[i];
					// 日付はフォーマットする
					data[0] = DateTime.Parse(data[0]).ToString(CsvControll.DFORM);
					// データがなければ前日の終値をつっこむ
					for (int j = 1; j <= 4; j++) {
						if (data[j] == "" || data[j] == null) data[j] = beforeEndPrice;
					}
					writeDatas[DateTime.Parse(data[0])] = data;
					beforeEndPrice = data[4];
				}

				bool isStart = false;
				string[] lastData = null;
				List<string[]> trueWriteDatas = new List<string[]>();
				foreach (DateTime date in dateList) {
					if (!isStart && writeDatas.ContainsKey(date)) isStart = true;
					if (!isStart) continue;

					if (writeDatas.ContainsKey(date)) {
						trueWriteDatas.Add(writeDatas[date]);
						lastData = writeDatas[date];
					} else {
						trueWriteDatas.Add(new string[5]{
							date.ToString(CsvControll.DFORM),lastData[1],lastData[2],lastData[3],lastData[4]
						});
					}
				}
				CsvControll.SaveCodeInfo(pair.Key, trueWriteDatas);
			}

			// oldになかったやつ削除
			//foreach (string symbol in nowCodeList) {
			//	if (!list.ContainsKey(symbol)) CsvControll.DeleteCodeInfo(symbol);
			//}
		}

		// 日経平均過去データをフォーマットして101に保存
		public static void SetJapanOldData()
		{
			List<DateTime> dateList = CsvControll.GetDateList();

			Dictionary<DateTime, string[]> writeDatas = new Dictionary<DateTime, string[]>();
			// 日経平均について、各日付ごとに二日後に4％減ってれば1,8%なら2,それ以外は0とする
			List<string[]> japanInfo = CsvControll.GetJapanRaw();
			//	日付け,終値,始値,高値,安値,出来高,変化率
			// "2024-12-12","39,849.14","39,849.97","40,091.55","39,827.59","1.21B","1.21%"
			for (int i = japanInfo.Count - 1; i >= 0; i--) {
				string[] info = japanInfo[i];
				DateTime date = DateTime.ParseExact(info[0].Replace("\"", ""), "yyyy-MM-dd", null);
				if (!dateList.Contains(date)) continue;
				writeDatas[date] = new string[5] {
					date.ToString(CsvControll.DFORM),
					(info[3]+info[4]).Replace("\"", ""),
					(info[5]+info[6]).Replace("\"", ""),
					(info[7]+info[8]).Replace("\"", ""),
					(info[1]+info[2]).Replace("\"", ""),
				};
			}
			string[] lastData = null;
			List<string[]> trueWriteDatas = new List<string[]>();
			foreach (DateTime date in dateList) {
				if (writeDatas.ContainsKey(date)) {
					trueWriteDatas.Add(writeDatas[date]);
					lastData = writeDatas[date];
				} else {
					trueWriteDatas.Add(new string[5]{
						date.ToString(CsvControll.DFORM),lastData[1],lastData[2],lastData[3],lastData[4]
					});
				}
			}
			CsvControll.SaveCodeInfo("101", trueWriteDatas);
		}

		// エラーデータチェック あとSkip対象の選別
		public static void DataChecker()
		{
			List<DateTime> dateList = CsvControll.GetDateList();
			List<string> codeList = CsvControll.GetCodeList();
			codeList.Add("101");
			foreach (string code in codeList) {
				if (!Common.Pro500(code)) continue;
				List<string[]> codeInfo = CsvControll.GetCodeInfo(code);
				int codeStart = -1;
				string firstDate = codeInfo[0][0];
				double beforePrice = 0;
				for (int i = 0; i < dateList.Count; i++) {
					DateTime date = dateList[i];
					if (date.ToString(CsvControll.DFORM) == firstDate) codeStart = i;
					if (codeStart < 0) { continue; }// 1301ほど古い日付がないやつ用のスキップ

					string[] info = codeInfo[i - codeStart];
					if (date.ToString(CsvControll.DFORM) != info[0]) {
						// エラー1 代表の1301の日付一覧と異なる
						CsvControll.ErrorLog("DataChecker1", code, date.ToString(CsvControll.DFORM), info[0]);
						if (code != "101") break;
					}
					for (int j = 1; j <= 4; j++) {
						if (Double.Parse(info[j]) <= 5) {
							// エラー2 いくらなんでも5円以下は存在しないはず
							CsvControll.ErrorLog("DataChecker2", code, i.ToString(), info[j]);
							//return;
						}
					}
					if (beforePrice > 0 && (beforePrice >= Double.Parse(info[4]) * 1.6 || beforePrice * 1.6 <= Double.Parse(info[4]))) {
						// エラー3 1.3倍はありえるんか？ => skipCodeにしてしまう
						CsvControll.ErrorLog("DataChecker3", code, info[0], info[4]);
						break;
					}
					beforePrice = Double.Parse(info[4]);
				}
				if (codeStart == -1 || codeStart >= 1000) {
					// エラー4 データがなさすぎる
					CsvControll.ErrorLog("DataChecker4", code, codeStart.ToString(), codeStart.ToString());
				}
			}

			CsvControll.Log("DataCheckerEnd", codeList.Count.ToString(), dateList.Count.ToString(), "");
		}

		// 過去用の日経平均スコアを雑に作成
		public static void SaveJapanScoreMulti(DateTime startDate, DateTime endDate)
		{
			int idx = Common.GetDateIdx(startDate);
			for (int i = 0; i <= 30; i++) {
				if (Common.NewD2(Common.GetDateByIdx(idx + i), endDate)) {
					Condtions.SaveJapanBaseScoreOneDay(Common.GetDateByIdx(idx + i));
				}
			}
		}

		public static void EnableBuy()
		{
			DateTime setDate = DateTime.Today;

			List<string[]> conditions = CsvControll.GetConditions();
			List<string> codeList = CsvControll.GetCodeList();
			string res = "";
			foreach (string symbol in codeList) {
				if (Common.Pro500(symbol) && Condtions.IsCondOk(setDate, CsvControll.GetCodeInfo(symbol), conditions)) {
					res += symbol + ", ";
				}
			}
			Common.DebugInfo("EnableBuy", res);
		}

		// 明日購入対象となったやつを表示
		public static void IsBuyList()
		{
			string res = "";
			int num = 0;
			foreach (KeyValuePair<string, CodeDaily> pair in MinitesExec.GetCodeDailys()) {
				if (pair.Value.IsBuy()) { res += pair.Key + ", "; num++; }
			}
			Common.DebugInfo("IsBuyList", res, num, 1, 1);
		}

		// 各種コードデータを可視化する
		public static void CodeDataChecker()
		{
			List<DateTime> dateList = CsvControll.GetDateList();
			List<string> codeList = CsvControll.GetCodeList();
			Dictionary<string, CodeDaily> codeDailys = MinitesExec.GetCodeDailys();
			Dictionary<string, CodeResOrder> codeResOrders = MinitesExec.GetCodeResOrders();

			foreach (KeyValuePair<string, CodeDaily> pair in codeDailys) {
				string symbol = pair.Key;
				CodeDaily codeDaily = pair.Value;

				List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
				string[] lastData = codeInfo[codeInfo.Count - 1];
				string lastLastEndPrice = codeInfo[codeInfo.Count - 2][4];

				List<string[]> saveDatas = new List<string[]>();

				// codeDaily情報
				saveDatas.Add(new string[13]{
					"Code","Ex","Yobine","Unit","LastEndPrice",
					"StartHave","IdealPrice","IsBuy","IsLossCut",
					"BuyPrice","LossSellPrice","BuyNeed","SellOrderNeed",
				});
				saveDatas.Add(codeDaily.GetSaveInfo());

				// 各種値段
				ResponseBoard resB = RequestBasic.RequestBoard(Int32.Parse(symbol), 1);
				saveDatas.Add(new string[7] { "StartPrice", "HighPrice", "LowPrice", "EndPrice", "LastEndPrice", "BeforeLoss", "Volume" });
				saveDatas.Add(new string[7] {
					lastData[1],lastData[2],lastData[3],lastData[4],lastLastEndPrice,
					(Double.Parse(lastData[4]) / Double.Parse(lastLastEndPrice) - 1).ToString(),resB.TradingVolume.ToString(),
				});

				// 板気配
				saveDatas.Add(new string[3] { "Board", "Price", "Num", });
				SellBuy[][] list = new SellBuy[][]{
					new SellBuy[10] {
						resB.Sell1,resB.Sell2,resB.Sell3,resB.Sell4,resB.Sell5,resB.Sell6,resB.Sell7,resB.Sell8,resB.Sell9,resB.Sell10,
					},
					new SellBuy[10]{
						resB.Buy1,resB.Buy2,resB.Buy3,resB.Buy4,resB.Buy5,resB.Buy6,resB.Buy7,resB.Buy8,resB.Buy9,resB.Buy10,
					},
				};
				for (int type = 0; type <= 1; type++) {
					for (int i = 0; i < 10; i++) {
						// 売10→1, 買1→10
						int index = type == 0 ? 9 - i : i;
						SellBuy sellBuy = list[type][index];
						saveDatas.Add(new string[3] { (type == 0 ? "Sell" : "Buy") + (index + 1), sellBuy.Price.ToString(), sellBuy.Qty.ToString(), });
					}
				}

				// 注文
				saveDatas.Add(new string[9] { "ID", "Code", "1:Se_2:Bu", "Recept", "OrderNum", "Price", "StartCum", "State", "Cum", });
				foreach (KeyValuePair<string, CodeResOrder> pair2 in codeResOrders) {
					CodeResOrder order = pair2.Value;
					if (order.Symbol == symbol && order.OrderQty > 0) {
						saveDatas.Add(order.GetSaveInfo());
					}
				}

				// 銘柄ごとにファイル作成
				CsvControll.SaveCodeDispInfo(symbol, saveDatas);
			}

			//Common.DebugDebugInfo("ERRORAll:{0}:EndR\n", 1);
		}

		// 決算期などのコード情報を雑に仕入れて保存
		public static void GetAllResponseSymbol()
		{
			List<string> codeList = CsvControll.GetCodeList();
			foreach (string symbol in codeList) {
				if (8886 >= Int32.Parse(symbol)) continue;
				ResponseSymbol resS = RequestBasic.RequestSymbol(Int32.Parse(symbol), 1);
				List<string[]> saveList = new List<string[]>();
				saveList.Add(new string[]{
					resS.Symbol.ToString(), //0 銘柄コード
					resS.SymbolName.ToString(), //1 銘柄名
					resS.DisplayName.ToString(), //2 銘柄略称※株式・先物・オプション銘柄の場合のみ
					resS.Exchange.ToString(), //3 市場コード※株式・先物・オプション銘柄の場合のみ
					resS.ExchangeName.ToString(), //4 市場名称※株式・先物・オプション銘柄の場合のみ
					resS.BisCategory.ToString(), //5 業種コード名※株式銘柄の場合のみ
					resS.TotalMarketValue.ToString(), //6 時価総額※株式銘柄の場合のみ 追加情報出力フラグ：falseの場合、null
					resS.TotalStocks.ToString(), //7 発行済み株式数（千株）※株式銘柄の場合のみ追加情報出力フラグ：falseの場合、null
					resS.TradingUnit.ToString(), //8 売買単位※株式・先物・オプション銘柄の場合のみ
					resS.FiscalYearEndBasic.ToString(), //9 決算期日※株式銘柄の場合のみ 追加情報出力フラグ：falseの場合、null
					resS.PriceRangeGroup.ToString(), //10 呼値グループ※株式・先物・オプション銘柄の場合のみ※各呼値コードが対応する商品は以下となります。
					resS.KCMarginBuy.ToString(), //11 一般信用買建フラグ※trueのとき、一般信用(長期) または一般信用(デイトレ) が買建可能※株式銘柄の場合のみ
					resS.KCMarginSell.ToString(), //12 一般信用売建フラグ※trueのとき、一般信用(長期) または一般信用(デイトレ) が売建可能※株式銘柄の場合のみ
					resS.MarginBuy.ToString(), //13 制度信用買建フラグ※trueのとき制度信用買建可能※株式銘柄の場合のみ
					resS.MarginSell.ToString(), //14 制度信用売建フラグ※trueのとき制度信用売建可能※株式銘柄の場合のみ
					resS.UpperLimit.ToString(), //15 値幅上限※株式・先物・オプション銘柄の場合のみ
					resS.LowerLimit.ToString(), //16 値幅下限※株式・先物・オプション銘柄の場合のみ
				});
				CsvControll.SaveResponseSymbol(saveList, true);
			}


		}

		// 危険な月日を計算 1日単位3日単位5日単位
		public static void CheckDateBenefitLoss()
		{
			int FisNum = 3; // 決算日との差分(決算日の●日前) 営業日基準
			int FisNum2 = 4; // ratioの差分(●日前との終値の比率) 営業日基準
			int Num = 5;

			// 決算日も
			List<string> codeList = CsvControll.GetCodeList();
			Dictionary<string, string[]> responseSymbols = new Dictionary<string, string[]>();
			foreach (string[] res in CsvControll.GetResponseSymbol()) responseSymbols[res[0]] = res;

			List<int> dateList = new List<int>();
			foreach (string[] info in CsvControll.GetCodeInfo(codeList[0])) {
				DateTime date = DateTime.Parse(info[0]);
				int key = date.Month * 100 + date.Day;
				if (key != 229 && key != 102 && key != 103 && key != 811 && !dateList.Contains(key)) dateList.Add(key);
			}
			dateList.Sort();

			// 1.コードごとに、各日付ごと(日付229を取り除く)に7年(7つ)を格納し、並び変え、1番上と下を取り除いて、それ以外の5個で平均をとる2000*365
			Dictionary<int, List<double>> avList = new Dictionary<int, List<double>>();
			List<double> fisList = new List<double>();
			foreach (string symbol in codeList) {
				List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
				if (!responseSymbols.ContainsKey(symbol)) continue;
				if (responseSymbols[symbol][9] == "0") { Common.DebugInfo("CheckDateBenefitLossError", symbol); responseSymbols[symbol][9] = "20250123"; }

				DateTime fis = DateTime.ParseExact(responseSymbols[symbol][9], CsvControll.DFILEFORM, null);
				int fisKey = fis.Month * 100 + fis.Day;
				int fisKeyIdx = -99;
				for (int idx = 0; idx < dateList.Count; idx++) {
					if (fisKey == dateList[idx]) fisKeyIdx = idx;
				}
				int fisKeyTrue = dateList[(fisKeyIdx - FisNum) % dateList.Count];


				Dictionary<int, List<double>> list = new Dictionary<int, List<double>>();
				bool isFisBefore = false;
				for (int i = 0; i < codeInfo.Count - FisNum2; i++) {
					string[] info = codeInfo[i];
					DateTime date = DateTime.Parse(info[0]);
					double ratio = Double.Parse(info[4]) / Double.Parse(codeInfo[i + 1][4]) - 1;
					int key = date.Month * 100 + date.Day;
					if (key == 229) continue;
					if (!list.ContainsKey(key)) list[key] = new List<double>();
					list[key].Add(ratio);

					// 
					if (IsOverDateKey1(fisKeyTrue, key)) isFisBefore = true;
					if (isFisBefore && IsOverDateKey1(key, fisKeyTrue)) {
						isFisBefore = false;
						fisList.Add(Double.Parse(info[4]) / Double.Parse(codeInfo[i + FisNum2][4]) - 1);
					}
				}
				foreach (KeyValuePair<int, List<double>> pair in list) {
					if (pair.Value.Count <= 3) continue;
					pair.Value.Sort();
					double av = 0;
					for (int i = 1; i < pair.Value.Count - 1; i++) {
						av += pair.Value[i] / (pair.Value.Count - 2);
					}
					if (!avList.ContainsKey(pair.Key)) avList[pair.Key] = new List<double>();
					avList[pair.Key].Add(av);
				}
			}

			// 2.日付ごとにまとめ、値段でソート、上30個下30個を除外して、平均の平均をとる 365
			// 4.値段でソート　確認
			// 5.3で作ったやつで前後1日含めて平均をとる 363 値段でソート　確認
			Dictionary<int, double> avAvList = new Dictionary<int, double>();
			foreach (KeyValuePair<int, List<double>> pair in avList) {
				pair.Value.Sort();
				double av = 0;
				for (int i = 30; i < pair.Value.Count - 30; i++) {
					av += pair.Value[i] / (pair.Value.Count - 60);
				}
				avAvList[pair.Key] = av;
			}

			// 一日ごとの確認
			//foreach (KeyValuePair<int, double> pair in avAvList.OrderBy((x) => x.Value)) Common.DebugInfo("CheckDateBenefitLoss1", pair.Key, pair.Value);

			Dictionary<int, double> avAvListMulti = new Dictionary<int, double>();
			for (int i = 0; i < dateList.Count - Num; i++) {
				double avAvAv = 0;
				for (int j = 0; j < Num; j++) {
					if (!avAvList.ContainsKey(dateList[i + j])) Common.DebugInfo("KeyError", dateList[i + j]);
					avAvAv += avAvList[dateList[i + j]] / Num;
				}
				avAvListMulti[dateList[i]] = avAvAv;
			}
			foreach (KeyValuePair<int, double> pair in avAvListMulti.OrderBy((x) => x.Value)) Common.DebugInfo("CheckDateBenefitLoss2", pair.Key, pair.Value * 100);

			// 決算期系
			fisList.Sort();
			double fisAv = 0;
			for (int i = 10; i < fisList.Count - 10; i++) {
				fisAv += fisList[i] / (fisList.Count - 20);
			}
			Common.DebugInfo("CheckDateBenefitLossFis", fisAv * 100, fisList.Count);

		}
		// key1の方が後(あるいは同じ)かどうか ただし1月は12月より後とする
		private static bool IsOverDateKey1(int key1, int key2)
		{
			if (key1 % 100 == 1 && key2 % 100 == 12) return true;
			if (key1 % 100 == 12 && key2 % 100 == 1) return false;
			return key1 >= key2;
		}

		// 購入対象となるものがどんな感じか複数日付でチェック マージンは無視
		public static void BuyCheck()
		{
			for (int i = 0; i < 10; i++) {
				DateTime setDate = Common.GetDateByIdx(Common.GetDateIdx(DateTime.Parse("2025/04/30")) - i);
				List<string[]> conditions = CsvControll.GetConditions();
				foreach (string symbol in CsvControll.GetCodeList()) {
					if (Common.Pro500(symbol) && Condtions.IsCondOk(setDate, CsvControll.GetCodeInfo(symbol), conditions)) {
						Common.DebugInfo("BuyCheck", setDate.ToString(CsvControll.DFORM), symbol);
					}
				}
			}
		}

		// 日経平均が下がりすぎのラインを調査
		public static void CheckJapanInfo()
		{
			string symbol = "101";
			List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
			for (int i = 2; i < codeInfo.Count - 1; i++) {
				DateTime date = DateTime.Parse(codeInfo[i][0]);
				double ratio = Double.Parse(codeInfo[i][4]) / Double.Parse(codeInfo[i - 1][4]);
				double ratio2 = Double.Parse(codeInfo[i - 1][4]) / Double.Parse(codeInfo[i - 2][4]);
				double ratio3 = Double.Parse(codeInfo[i + 1][4]) / Double.Parse(codeInfo[i][4]);


				if (ratio <= 0.96) {
					// 過去を遡って自身より低いやつが何個前にあるのかチェック
					int before = 0; int before2 = 0; int before3 = 0;
					for (int j = i - 1; j >= 0; j--) {
						if (Double.Parse(codeInfo[j][4]) <= Double.Parse(codeInfo[i][4])) { before = j - i; break; }
					}
					for (int j = i - 2; j >= 0; j--) {
						if (Double.Parse(codeInfo[j][4]) <= Double.Parse(codeInfo[i - 1][4])) { before2 = j - (i - 1); break; }
					}
					for (int j = i; j >= 0; j--) {
						if (Double.Parse(codeInfo[j][4]) <= Double.Parse(codeInfo[i + 1][4])) { before3 = j - (i + 1); break; }
					}
					Common.DebugInfo("CheckJapanInfo", date.ToString(CsvControll.DFORM), ratio2, ratio, ratio3, before2, before, before3);
				}
			}

		}



		// 低値コード探し
		public static  double IsLowPriceCheck(string symbol)
		{
			//if (!Common.Sp10(symbol)) return (false, 0);

			List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
			int lastIdx = codeInfo.Count - 1;
			List<double> lowList = new List<double>();
			List<double> highList = new List<double>();
			for (int i = lastIdx; i >= 0; i--) {
				string[] info = codeInfo[i];
				highList.Add(Double.Parse(info[2]));
				lowList.Add(Double.Parse(info[3]));
			}
			// 基準値 この値段なら現在まあ購入可能であるだろう
			double basePrice = GetRank(lowList, 5, 3) + 1;

			bool isOk = true;
			int[,] lowDef = new int[3, 3] { { 90, 20, 3 }, { 85, 60, 6 }, { 80, 120, 8 } };
			int[,] highDef = new int[4, 3] { { 110, 5, 3 }, { 130, 20, 3 }, { 150, 60, 6 }, { 180, 120, 8 } };
			for (int i = 0; i < lowDef.GetLength(0); i++) {
				double r = GetRank(lowList, lowDef[i, 1], lowDef[i, 2]);
				if (basePrice * lowDef[i, 0] / 100 >= r && basePrice - 10 >= r) isOk = false;
			}
			for (int i = 0; i < highDef.GetLength(0); i++) {
				double r = GetRank(highList, highDef[i, 1], highDef[i, 2], false);
				if (basePrice * highDef[i, 0] / 100 <= r && basePrice + 10 <= r) isOk = false;
			}
			if (basePrice + 1 > GetRank(highList, 5, 3, false)) isOk = false;

			return  isOk ? basePrice : 0;
		}


		// 低値コード探し
		public static void LowPriceCheck()
		{
			List<string> list = new List<string>();
			foreach (string symbol in CsvControll.GetCodeList()) {
				if (symbol == "9432" || symbol == "3071") continue;
				List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
				// 低価格で安定していて、4円の差分がある
				bool isOk = true;
				for (int i = codeInfo.Count - 1; i >= codeInfo.Count - 100; i--) {
					string[] info = codeInfo[i];
					if (Double.Parse(info[2]) >= 200) {
						isOk = false;
						break;
					}
				}
				if (isOk) list.Add(symbol);
			}

			string allLog = "";
			// 
			foreach (string symbol in list) {
				List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
				int lastIdx = codeInfo.Count - 1;

				// まず4現在値を定義する？直近のlow平均とhigh平均を求める
				// lowを並べて、18,16,16,17,16 下から3番？
				List<double> lowList = new List<double>();
				List<double> highList = new List<double>();
				for (int i = lastIdx; i >= 0; i--) {
					string[] info = codeInfo[i];
					highList.Add(Double.Parse(info[2]));
					lowList.Add(Double.Parse(info[3]));
				}
				// 基準値 この値段なら現在まあ購入可能であるだろう
				double basePrice = GetRank(lowList, 5, 3) + 1;

				string logText = symbol + ", " + Common.Pro500(symbol) + ", " + basePrice + ", ";

				bool isOk = true;
				int[,] lowDef = new int[3, 3] { { 90, 20, 3 }, { 85, 60, 6 }, { 80, 120, 8 } };
				int[,] highDef = new int[4, 3] { { 110, 5, 3 }, { 130, 20, 3 }, { 150, 60, 6 }, { 180, 120, 8 } };
				for (int i = 0; i < lowDef.GetLength(0); i++) {
					double r = GetRank(lowList, lowDef[i, 1], lowDef[i, 2]);
					logText += "Low" + i + ":" + r;
					if (basePrice * lowDef[i, 0] / 100 >= r && basePrice - 10 >= r) {
						isOk = false;
						logText += ":False";
					}
					logText += ", ";
				}
				for (int i = 0; i < highDef.GetLength(0); i++) {
					double r = GetRank(highList, highDef[i, 1], highDef[i, 2], false);
					logText += "High" + i + ":" + r;
					if (basePrice * highDef[i, 0] / 100 <= r && basePrice + 10 <= r) {
						isOk = false;
						logText += ":False";
					}
					logText += ", ";
				}
				if (basePrice + 1 > GetRank(highList, 5, 3, false)) isOk = false;

				logText += isOk;
				Common.DebugInfo("LPC", logText);
				int num = isOk && !Common.Pro500(symbol) ? (int)basePrice : 0;
				allLog += "{ \"" + symbol + "\", " + num + " },\n";
			}
			Common.DebugInfo("LPCLast", allLog);
		}
		// 上からnum分とってrank番目に大きなものを返す
		private static double GetRank(List<double> list, int num, int rank, bool isAct = true)
		{
			List<double> numList = new List<double>();
			for (int i = 0; i < num; i++) numList.Add(list[i]);
			numList.Sort();
			if (!isAct) numList.Reverse();
			return numList[rank - 1];
		}


		// 関数とかを簡易テストしとこ
		public static void TestExec()
		{

			//RequestBasic.RequestSendOrder(3656, 1, false, codeDaily.SellOrderNeed(), codeDaily.SellPrice(true), 0);
			//RequestBasic.RequestSendOrder(3656, 1, true, 100, 121, 0);

			List<string[]> logOld =  CsvControll.GetErrorLogOld(DateTime.Today);
			string raw = logOld[10][3];

			raw = "[{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250415A02N99113743\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-15T17:39:08.73984+09:00\",\"Symbol\":\"9076\",\"SymbolName\":\"セイノーホールディングス\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":2338.0,\"OrderQty\":0.0,\"CumQty\":0.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250430,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250415A02N99113743\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-15T17:39:08.73984+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250418,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":2,\"ID\":\"20250415B02N99113744\",\"RecType\":4,\"ExchangeID\":null,\"State\":1,\"TransactTime\":\"2025-04-15T17:39:08.73984+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250418,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":3,\"ID\":\"20250415G02N99386046\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-16T06:06:23.900939+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250418,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250415B02N99386047\",\"RecType\":4,\"ExchangeID\":\"A4110000001343\",\"State\":3,\"TransactTime\":\"2025-04-16T08:00:15.116214+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250418,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":7,\"ID\":\"20250416F02N00477800\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-16T15:45:05.02229+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250418,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":8,\"ID\":\"20250416G02N00866334\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-17T06:06:23.430674+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250421,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":11,\"ID\":\"20250416B02N00866335\",\"RecType\":4,\"ExchangeID\":\"A4110B00001300\",\"State\":3,\"TransactTime\":\"2025-04-17T08:00:13.717901+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250421,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":12,\"ID\":\"20250417F02N01930727\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-17T15:45:04.90347+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250421,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":13,\"ID\":\"20250417G02N02298793\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-18T06:06:27.220828+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250422,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":16,\"ID\":\"20250417B02N02298794\",\"RecType\":4,\"ExchangeID\":\"A4110600001151\",\"State\":3,\"TransactTime\":\"2025-04-18T08:00:13.226237+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250422,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":17,\"ID\":\"20250418F02N03376162\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-18T15:45:04.733096+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250422,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":18,\"ID\":\"20250418G02N03639022\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-19T06:06:18.043815+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250423,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":21,\"ID\":\"20250418B02N03639023\",\"RecType\":4,\"ExchangeID\":\"A4110600001068\",\"State\":3,\"TransactTime\":\"2025-04-21T08:00:12.634435+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250423,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":22,\"ID\":\"20250421F02N04870181\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-21T15:45:05.142868+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250423,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":23,\"ID\":\"20250421G02N05210427\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-22T06:06:20.627154+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250424,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":26,\"ID\":\"20250421B02N05210428\",\"RecType\":4,\"ExchangeID\":\"A4110400001341\",\"State\":3,\"TransactTime\":\"2025-04-22T08:00:16.121055+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250424,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":27,\"ID\":\"20250422F02N06277777\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-22T15:45:05.427349+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250424,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":28,\"ID\":\"20250422G02N06642752\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-23T06:06:21.869733+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250425,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":31,\"ID\":\"20250422B02N06642753\",\"RecType\":4,\"ExchangeID\":\"A4110000001139\",\"State\":3,\"TransactTime\":\"2025-04-23T08:00:14.135657+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250425,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":32,\"ID\":\"20250423F02N07790971\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-23T15:45:04.420686+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250425,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":33,\"ID\":\"20250423G02N08198090\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-24T06:06:25.515188+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250428,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":36,\"ID\":\"20250423B02N08198091\",\"RecType\":4,\"ExchangeID\":\"A4110B00001520\",\"State\":3,\"TransactTime\":\"2025-04-24T08:00:16.637021+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250428,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":37,\"ID\":\"20250424F02N09350620\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-24T15:45:05.176885+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250428,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":38,\"ID\":\"20250424G02N09740269\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T06:06:25.924962+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":41,\"ID\":\"20250424B02N09740270\",\"RecType\":4,\"ExchangeID\":\"A4110500001422\",\"State\":3,\"TransactTime\":\"2025-04-25T08:00:16.66101+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":42,\"ID\":\"20250425F02N10967554\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T15:45:05.271828+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":43,\"ID\":\"20250425G02N11290710\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-26T06:06:20.174501+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":46,\"ID\":\"20250425B02N11290711\",\"RecType\":4,\"ExchangeID\":\"A4110400000874\",\"State\":3,\"TransactTime\":\"2025-04-28T08:00:10.746031+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":47,\"ID\":\"20250428F02N12513883\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T15:45:05.41717+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":48,\"ID\":\"20250428G02N12829795\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-29T06:06:14.150696+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":51,\"ID\":\"20250428B02N12829796\",\"RecType\":4,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-30T00:30:15.250007+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":52,\"ID\":\"20250429D02N13290802\",\"RecType\":6,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-30T00:30:15.203206+09:00\",\"OrdType\":null,\"Price\":null,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250423A02N08068728\",\"State\":3,\"OrderState\":3,\"OrdType\":1,\"RecvTime\":\"2025-04-24T00:33:58.360136+09:00\",\"Symbol\":\"6869\",\"SymbolName\":\"シスメックス\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":2785.0,\"OrderQty\":300.0,\"CumQty\":0.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250501,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250423A02N08068728\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-24T00:33:58.360136+09:00\",\"OrdType\":1,\"Price\":2785.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250428,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":2,\"ID\":\"20250423B02N08068729\",\"RecType\":4,\"ExchangeID\":null,\"State\":1,\"TransactTime\":\"2025-04-24T00:33:58.360136+09:00\",\"OrdType\":1,\"Price\":2785.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250428,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":3,\"ID\":\"20250423G02N08198102\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-24T06:06:25.56199+09:00\",\"OrdType\":1,\"Price\":2785.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250428,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250423B02N08198103\",\"RecType\":4,\"ExchangeID\":\"A4110200001507\",\"State\":3,\"TransactTime\":\"2025-04-24T08:00:16.64494+09:00\",\"OrdType\":1,\"Price\":2785.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250428,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":7,\"ID\":\"20250424F02N09350622\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-24T15:45:05.176885+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250428,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":8,\"ID\":\"20250424G02N09740273\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T06:06:25.956163+09:00\",\"OrdType\":1,\"Price\":2785.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":11,\"ID\":\"20250424B02N09740274\",\"RecType\":4,\"ExchangeID\":\"A4110600001397\",\"State\":3,\"TransactTime\":\"2025-04-25T08:00:16.663565+09:00\",\"OrdType\":1,\"Price\":2785.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":12,\"ID\":\"20250425F02N10967555\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T15:45:05.271828+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":13,\"ID\":\"20250425G02N11290714\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-26T06:06:20.205702+09:00\",\"OrdType\":1,\"Price\":2785.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":16,\"ID\":\"20250425B02N11290715\",\"RecType\":4,\"ExchangeID\":\"A4110000000872\",\"State\":3,\"TransactTime\":\"2025-04-28T08:00:10.752448+09:00\",\"OrdType\":1,\"Price\":2785.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":17,\"ID\":\"20250428F02N12513884\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T15:45:05.41717+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":18,\"ID\":\"20250428G02N12829799\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-29T06:06:14.181897+09:00\",\"OrdType\":1,\"Price\":2785.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":21,\"ID\":\"20250428B02N12829800\",\"RecType\":4,\"ExchangeID\":\"A4110400000848\",\"State\":3,\"TransactTime\":\"2025-04-30T08:00:10.106859+09:00\",\"OrdType\":1,\"Price\":2785.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250424A02N09478313\",\"State\":3,\"OrderState\":3,\"OrdType\":1,\"RecvTime\":\"2025-04-24T17:46:11.684456+09:00\",\"Symbol\":\"3591\",\"SymbolName\":\"ワコールホールディングス\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":5118.0,\"OrderQty\":200.0,\"CumQty\":0.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250430,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250424A02N09478313\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-24T17:46:11.684456+09:00\",\"OrdType\":1,\"Price\":5118.0,\"Qty\":200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":2,\"ID\":\"20250424B02N09478314\",\"RecType\":4,\"ExchangeID\":null,\"State\":1,\"TransactTime\":\"2025-04-24T17:46:11.684456+09:00\",\"OrdType\":1,\"Price\":5118.0,\"Qty\":200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":3,\"ID\":\"20250424G02N09740277\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T06:06:25.956163+09:00\",\"OrdType\":1,\"Price\":5118.0,\"Qty\":200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250424B02N09740278\",\"RecType\":4,\"ExchangeID\":\"A4110600001402\",\"State\":3,\"TransactTime\":\"2025-04-25T08:00:16.668346+09:00\",\"OrdType\":1,\"Price\":5118.0,\"Qty\":200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":7,\"ID\":\"20250425F02N10967556\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T15:45:05.271828+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":8,\"ID\":\"20250425G02N11290718\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-26T06:06:20.221302+09:00\",\"OrdType\":1,\"Price\":5118.0,\"Qty\":200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":11,\"ID\":\"20250425B02N11290719\",\"RecType\":4,\"ExchangeID\":\"A4110000000878\",\"State\":3,\"TransactTime\":\"2025-04-28T08:00:10.75733+09:00\",\"OrdType\":1,\"Price\":5118.0,\"Qty\":200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":12,\"ID\":\"20250428F02N12513885\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T15:45:05.41717+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":13,\"ID\":\"20250428G02N12829803\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-29T06:06:14.181897+09:00\",\"OrdType\":1,\"Price\":5118.0,\"Qty\":200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":16,\"ID\":\"20250428B02N12829804\",\"RecType\":4,\"ExchangeID\":\"A4110100000855\",\"State\":3,\"TransactTime\":\"2025-04-30T08:00:10.133453+09:00\",\"OrdType\":1,\"Price\":5118.0,\"Qty\":200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250424A02N09478318\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-24T17:46:12.012064+09:00\",\"Symbol\":\"3656\",\"SymbolName\":\"ＫＬａｂ\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":122.0,\"OrderQty\":2400.0,\"CumQty\":2400.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250502,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250424A02N09478318\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-24T17:46:12.012064+09:00\",\"OrdType\":1,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":2,\"ID\":\"20250424B02N09478319\",\"RecType\":4,\"ExchangeID\":null,\"State\":1,\"TransactTime\":\"2025-04-24T17:46:12.012064+09:00\",\"OrdType\":1,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":3,\"ID\":\"20250424G02N09740281\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T06:06:25.971763+09:00\",\"OrdType\":1,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250424B02N09740282\",\"RecType\":4,\"ExchangeID\":\"A4110800001416\",\"State\":3,\"TransactTime\":\"2025-04-25T08:00:16.67074+09:00\",\"OrdType\":1,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":7,\"ID\":\"20250425E02N10178565\",\"RecType\":8,\"ExchangeID\":\"85\",\"State\":3,\"TransactTime\":\"2025-04-25T09:01:07.606754+09:00\",\"OrdType\":0,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":\"E2025042500FVQ\",\"ExecutionDay\":\"2025-04-25T09:01:07.606754+09:00\",\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250424A02N09478323\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-24T17:46:12.339673+09:00\",\"Symbol\":\"4829\",\"SymbolName\":\"日本エンタープライズ\",\"Exchange\":1,\"ExchangeName\":\"東証ス\",\"Price\":108.0,\"OrderQty\":2200.0,\"CumQty\":2200.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250502,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250424A02N09478323\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-24T17:46:12.339673+09:00\",\"OrdType\":1,\"Price\":108.0,\"Qty\":2200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":2,\"ID\":\"20250424B02N09478324\",\"RecType\":4,\"ExchangeID\":null,\"State\":1,\"TransactTime\":\"2025-04-24T17:46:12.339673+09:00\",\"OrdType\":1,\"Price\":108.0,\"Qty\":2200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":3,\"ID\":\"20250424G02N09740285\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T06:06:25.987364+09:00\",\"OrdType\":1,\"Price\":108.0,\"Qty\":2200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250424B02N09740286\",\"RecType\":4,\"ExchangeID\":\"A4110000001397\",\"State\":3,\"TransactTime\":\"2025-04-25T08:00:16.674498+09:00\",\"OrdType\":1,\"Price\":108.0,\"Qty\":2200.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":7,\"ID\":\"20250425E02N10390533\",\"RecType\":8,\"ExchangeID\":\"65\",\"State\":3,\"TransactTime\":\"2025-04-25T09:52:43.92622+09:00\",\"OrdType\":0,\"Price\":108.0,\"Qty\":100.0,\"ExecutionID\":\"E2025042501L3R\",\"ExecutionDay\":\"2025-04-25T09:52:43.92622+09:00\",\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":8,\"ID\":\"20250425E02N10705569\",\"RecType\":8,\"ExchangeID\":\"104\",\"State\":3,\"TransactTime\":\"2025-04-25T13:07:03.546471+09:00\",\"OrdType\":0,\"Price\":108.0,\"Qty\":100.0,\"ExecutionID\":\"E20250425033IW\",\"ExecutionDay\":\"2025-04-25T13:07:03.546471+09:00\",\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":9,\"ID\":\"20250425E02N10705700\",\"RecType\":8,\"ExchangeID\":\"106\",\"State\":3,\"TransactTime\":\"2025-04-25T13:08:21.404734+09:00\",\"OrdType\":0,\"Price\":108.0,\"Qty\":2000.0,\"ExecutionID\":\"E202504250347U\",\"ExecutionDay\":\"2025-04-25T13:08:21.404734+09:00\",\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250424A02N09478328\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-24T17:46:12.651681+09:00\",\"Symbol\":\"7256\",\"SymbolName\":\"河西工業\",\"Exchange\":1,\"ExchangeName\":\"東証ス\",\"Price\":89.0,\"OrderQty\":100.0,\"CumQty\":100.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250502,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250424A02N09478328\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-24T17:46:12.651681+09:00\",\"OrdType\":1,\"Price\":89.0,\"Qty\":100.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":2,\"ID\":\"20250424B02N09478329\",\"RecType\":4,\"ExchangeID\":null,\"State\":1,\"TransactTime\":\"2025-04-24T17:46:12.651681+09:00\",\"OrdType\":1,\"Price\":89.0,\"Qty\":100.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":3,\"ID\":\"20250424G02N09740289\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T06:06:26.002964+09:00\",\"OrdType\":1,\"Price\":89.0,\"Qty\":100.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250424B02N09740290\",\"RecType\":4,\"ExchangeID\":\"A4110300001460\",\"State\":3,\"TransactTime\":\"2025-04-25T08:00:16.678051+09:00\",\"OrdType\":1,\"Price\":89.0,\"Qty\":100.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":7,\"ID\":\"20250425E02N10882733\",\"RecType\":8,\"ExchangeID\":\"234\",\"State\":3,\"TransactTime\":\"2025-04-25T15:13:23.978301+09:00\",\"OrdType\":0,\"Price\":89.0,\"Qty\":100.0,\"ExecutionID\":\"E20250425041CO\",\"ExecutionDay\":\"2025-04-25T15:13:23.978301+09:00\",\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250424A02N09478333\",\"State\":3,\"OrderState\":3,\"OrdType\":1,\"RecvTime\":\"2025-04-24T17:46:12.979289+09:00\",\"Symbol\":\"8086\",\"SymbolName\":\"ニプロ\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":1359.0,\"OrderQty\":700.0,\"CumQty\":0.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250502,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250424A02N09478333\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-24T17:46:12.979289+09:00\",\"OrdType\":1,\"Price\":1359.0,\"Qty\":700.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":2,\"ID\":\"20250424B02N09478334\",\"RecType\":4,\"ExchangeID\":null,\"State\":1,\"TransactTime\":\"2025-04-24T17:46:12.979289+09:00\",\"OrdType\":1,\"Price\":1359.0,\"Qty\":700.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":3,\"ID\":\"20250424G02N09740297\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T06:06:26.018564+09:00\",\"OrdType\":1,\"Price\":1359.0,\"Qty\":700.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250424B02N09740298\",\"RecType\":4,\"ExchangeID\":\"A4110400001300\",\"State\":3,\"TransactTime\":\"2025-04-25T08:00:16.678633+09:00\",\"OrdType\":1,\"Price\":1359.0,\"Qty\":700.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":7,\"ID\":\"20250425F02N10967557\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T15:45:05.271828+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":8,\"ID\":\"20250425G02N11290722\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-26T06:06:20.221302+09:00\",\"OrdType\":1,\"Price\":1359.0,\"Qty\":700.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":11,\"ID\":\"20250425B02N11290723\",\"RecType\":4,\"ExchangeID\":\"A4110400000891\",\"State\":3,\"TransactTime\":\"2025-04-28T08:00:10.76208+09:00\",\"OrdType\":1,\"Price\":1359.0,\"Qty\":700.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":12,\"ID\":\"20250428F02N12513886\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T15:45:05.41717+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":13,\"ID\":\"20250428G02N12829807\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-29T06:06:14.213098+09:00\",\"OrdType\":1,\"Price\":1359.0,\"Qty\":700.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":16,\"ID\":\"20250428B02N12829808\",\"RecType\":4,\"ExchangeID\":\"A4110100000874\",\"State\":3,\"TransactTime\":\"2025-04-30T08:00:10.150946+09:00\",\"OrdType\":1,\"Price\":1359.0,\"Qty\":700.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":2,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250425A02N10195952\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-25T09:05:26.269948+09:00\",\"Symbol\":\"6538\",\"SymbolName\":\"ディスラプターズ\",\"Exchange\":1,\"ExchangeName\":\"東証ス\",\"Price\":0.0,\"OrderQty\":2000.0,\"CumQty\":2000.0,\"Side\":\"2\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250425,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250425A02N10195952\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T09:05:26.269948+09:00\",\"OrdType\":1,\"Price\":0.0,\"Qty\":2000.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250425B02N10195953\",\"RecType\":4,\"ExchangeID\":\"A4110600004062\",\"State\":3,\"TransactTime\":\"2025-04-25T09:05:26.238261+09:00\",\"OrdType\":1,\"Price\":0.0,\"Qty\":2000.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250425E02N10195954\",\"RecType\":8,\"ExchangeID\":\"11\",\"State\":3,\"TransactTime\":\"2025-04-25T09:05:26.238353+09:00\",\"OrdType\":0,\"Price\":153.0,\"Qty\":900.0,\"ExecutionID\":\"E2025042500LX5\",\"ExecutionDay\":\"2025-04-25T09:05:26.238353+09:00\",\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250425E02N10195955\",\"RecType\":8,\"ExchangeID\":\"16\",\"State\":3,\"TransactTime\":\"2025-04-25T09:05:26.238353+09:00\",\"OrdType\":0,\"Price\":154.0,\"Qty\":1100.0,\"ExecutionID\":\"E2025042500LX6\",\"ExecutionDay\":\"2025-04-25T09:05:26.238353+09:00\",\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":2,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250425A02N10226301\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-25T09:06:23.476615+09:00\",\"Symbol\":\"4591\",\"SymbolName\":\"リボミック\",\"Exchange\":1,\"ExchangeName\":\"東証グ\",\"Price\":0.0,\"OrderQty\":3300.0,\"CumQty\":3300.0,\"Side\":\"2\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250425,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250425A02N10226301\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T09:06:23.476615+09:00\",\"OrdType\":1,\"Price\":0.0,\"Qty\":3300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250425B02N10226302\",\"RecType\":4,\"ExchangeID\":\"A4110B00004187\",\"State\":3,\"TransactTime\":\"2025-04-25T09:06:23.442793+09:00\",\"OrdType\":1,\"Price\":0.0,\"Qty\":3300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250425E02N10226303\",\"RecType\":8,\"ExchangeID\":\"86\",\"State\":3,\"TransactTime\":\"2025-04-25T09:06:23.442884+09:00\",\"OrdType\":0,\"Price\":94.0,\"Qty\":3300.0,\"ExecutionID\":\"E2025042500NNU\",\"ExecutionDay\":\"2025-04-25T09:06:23.442884+09:00\",\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":2,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250425A02N10549990\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-25T11:30:16.520863+09:00\",\"Symbol\":\"7256\",\"SymbolName\":\"河西工業\",\"Exchange\":1,\"ExchangeName\":\"東証ス\",\"Price\":89.0,\"OrderQty\":3300.0,\"CumQty\":3300.0,\"Side\":\"2\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250425,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250425A02N10549990\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T11:30:16.520863+09:00\",\"OrdType\":1,\"Price\":89.0,\"Qty\":3300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250425B02N10549991\",\"RecType\":4,\"ExchangeID\":\"A4110B00010079\",\"State\":3,\"TransactTime\":\"2025-04-25T12:05:08.530012+09:00\",\"OrdType\":1,\"Price\":89.0,\"Qty\":3300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250425E02N10621651\",\"RecType\":8,\"ExchangeID\":\"186\",\"State\":3,\"TransactTime\":\"2025-04-25T12:30:00.075263+09:00\",\"OrdType\":0,\"Price\":89.0,\"Qty\":3300.0,\"ExecutionID\":\"E2025042502JPS\",\"ExecutionDay\":\"2025-04-25T12:30:00.075263+09:00\",\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":2,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250425A02N10897378\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-25T15:21:17.445062+09:00\",\"Symbol\":\"8198\",\"SymbolName\":\"マックスバリュ東海\",\"Exchange\":1,\"ExchangeName\":\"東証ス\",\"Price\":3040.0,\"OrderQty\":300.0,\"CumQty\":300.0,\"Side\":\"2\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250425,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250425A02N10897378\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T15:21:17.445062+09:00\",\"OrdType\":1,\"Price\":3040.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250425B02N10897379\",\"RecType\":4,\"ExchangeID\":\"A4110000015312\",\"State\":3,\"TransactTime\":\"2025-04-25T15:21:17.421986+09:00\",\"OrdType\":1,\"Price\":3040.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250425E02N10897380\",\"RecType\":8,\"ExchangeID\":\"72\",\"State\":3,\"TransactTime\":\"2025-04-25T15:21:17.422081+09:00\",\"OrdType\":0,\"Price\":3035.0,\"Qty\":300.0,\"ExecutionID\":\"E202504250440G\",\"ExecutionDay\":\"2025-04-25T15:21:17.422081+09:00\",\"DelivDay\":20250430,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250425A02N11096205\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-25T17:44:42.048488+09:00\",\"Symbol\":\"7256\",\"SymbolName\":\"河西工業\",\"Exchange\":1,\"ExchangeName\":\"東証ス\",\"Price\":90.0,\"OrderQty\":3300.0,\"CumQty\":3300.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250507,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250425A02N11096205\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T17:44:42.048488+09:00\",\"OrdType\":1,\"Price\":90.0,\"Qty\":3300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":2,\"ID\":\"20250425B02N11096206\",\"RecType\":4,\"ExchangeID\":null,\"State\":1,\"TransactTime\":\"2025-04-25T17:44:42.048488+09:00\",\"OrdType\":1,\"Price\":90.0,\"Qty\":3300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":3,\"ID\":\"20250425G02N11290726\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-26T06:06:20.236902+09:00\",\"OrdType\":1,\"Price\":90.0,\"Qty\":3300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250425B02N11290727\",\"RecType\":4,\"ExchangeID\":\"A4110800000900\",\"State\":3,\"TransactTime\":\"2025-04-28T08:00:10.766798+09:00\",\"OrdType\":1,\"Price\":90.0,\"Qty\":3300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":7,\"ID\":\"20250428E02N11829995\",\"RecType\":8,\"ExchangeID\":\"55\",\"State\":3,\"TransactTime\":\"2025-04-28T09:04:24.163867+09:00\",\"OrdType\":0,\"Price\":90.0,\"Qty\":3300.0,\"ExecutionID\":\"E2025042800L6M\",\"ExecutionDay\":\"2025-04-28T09:04:24.163867+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250425A02N11096210\",\"State\":3,\"OrderState\":3,\"OrdType\":1,\"RecvTime\":\"2025-04-25T17:44:42.391696+09:00\",\"Symbol\":\"8198\",\"SymbolName\":\"マックスバリュ東海\",\"Exchange\":1,\"ExchangeName\":\"東証ス\",\"Price\":3160.0,\"OrderQty\":300.0,\"CumQty\":0.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250507,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250425A02N11096210\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-25T17:44:42.391696+09:00\",\"OrdType\":1,\"Price\":3160.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":2,\"ID\":\"20250425B02N11096211\",\"RecType\":4,\"ExchangeID\":null,\"State\":1,\"TransactTime\":\"2025-04-25T17:44:42.391696+09:00\",\"OrdType\":1,\"Price\":3160.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":3,\"ID\":\"20250425G02N11290730\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-26T06:06:20.268103+09:00\",\"OrdType\":1,\"Price\":3160.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250425B02N11290731\",\"RecType\":4,\"ExchangeID\":\"A4110900000910\",\"State\":3,\"TransactTime\":\"2025-04-28T08:00:10.772107+09:00\",\"OrdType\":1,\"Price\":3160.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":7,\"ID\":\"20250428F02N12513887\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T15:45:05.41717+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":8,\"ID\":\"20250428G02N12829811\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-29T06:06:14.213098+09:00\",\"OrdType\":1,\"Price\":3160.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":11,\"ID\":\"20250428B02N12829812\",\"RecType\":4,\"ExchangeID\":\"A4110400000910\",\"State\":3,\"TransactTime\":\"2025-04-30T08:00:10.160133+09:00\",\"OrdType\":1,\"Price\":3160.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250427A02N11655501\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-28T02:10:25.628529+09:00\",\"Symbol\":\"4591\",\"SymbolName\":\"リボミック\",\"Exchange\":1,\"ExchangeName\":\"東証グ\",\"Price\":95.0,\"OrderQty\":3300.0,\"CumQty\":3300.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250507,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250427A02N11655501\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T02:10:25.628529+09:00\",\"OrdType\":1,\"Price\":95.0,\"Qty\":3300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250427B02N11655502\",\"RecType\":4,\"ExchangeID\":\"A4110200001499\",\"State\":3,\"TransactTime\":\"2025-04-28T08:00:16.686889+09:00\",\"OrdType\":1,\"Price\":95.0,\"Qty\":3300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250428E02N11887216\",\"RecType\":8,\"ExchangeID\":\"138\",\"State\":3,\"TransactTime\":\"2025-04-28T09:14:11.893204+09:00\",\"OrdType\":0,\"Price\":95.0,\"Qty\":200.0,\"ExecutionID\":\"E2025042800VME\",\"ExecutionDay\":\"2025-04-28T09:14:11.893204+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250428E02N11887217\",\"RecType\":8,\"ExchangeID\":\"154\",\"State\":3,\"TransactTime\":\"2025-04-28T09:14:12.285032+09:00\",\"OrdType\":0,\"Price\":95.0,\"Qty\":200.0,\"ExecutionID\":\"E2025042800VMK\",\"ExecutionDay\":\"2025-04-28T09:14:12.285032+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":7,\"ID\":\"20250428E02N11887219\",\"RecType\":8,\"ExchangeID\":\"162\",\"State\":3,\"TransactTime\":\"2025-04-28T09:14:12.803762+09:00\",\"OrdType\":0,\"Price\":95.0,\"Qty\":200.0,\"ExecutionID\":\"E2025042800VMO\",\"ExecutionDay\":\"2025-04-28T09:14:12.803762+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":8,\"ID\":\"20250428E02N11887226\",\"RecType\":8,\"ExchangeID\":\"173\",\"State\":3,\"TransactTime\":\"2025-04-28T09:14:15.458954+09:00\",\"OrdType\":0,\"Price\":95.0,\"Qty\":800.0,\"ExecutionID\":\"E2025042800VNV\",\"ExecutionDay\":\"2025-04-28T09:14:15.458954+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":9,\"ID\":\"20250428E02N11887237\",\"RecType\":8,\"ExchangeID\":\"177\",\"State\":3,\"TransactTime\":\"2025-04-28T09:14:19.375941+09:00\",\"OrdType\":0,\"Price\":95.0,\"Qty\":1900.0,\"ExecutionID\":\"E2025042800VQ6\",\"ExecutionDay\":\"2025-04-28T09:14:19.375941+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250427A02N11655506\",\"State\":3,\"OrderState\":3,\"OrdType\":1,\"RecvTime\":\"2025-04-28T02:10:25.971737+09:00\",\"Symbol\":\"6538\",\"SymbolName\":\"ディスラプターズ\",\"Exchange\":1,\"ExchangeName\":\"東証ス\",\"Price\":154.0,\"OrderQty\":2000.0,\"CumQty\":0.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250507,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250427A02N11655506\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T02:10:25.971737+09:00\",\"OrdType\":1,\"Price\":154.0,\"Qty\":2000.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250427B02N11655507\",\"RecType\":4,\"ExchangeID\":\"A4110400001427\",\"State\":3,\"TransactTime\":\"2025-04-28T08:00:16.688628+09:00\",\"OrdType\":1,\"Price\":154.0,\"Qty\":2000.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250428F02N12513888\",\"RecType\":7,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T15:45:05.41717+09:00\",\"OrdType\":0,\"Price\":null,\"Qty\":null,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250428G02N12829815\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-29T06:06:14.228698+09:00\",\"OrdType\":1,\"Price\":154.0,\"Qty\":2000.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":9,\"ID\":\"20250428B02N12829816\",\"RecType\":4,\"ExchangeID\":\"A4110100000912\",\"State\":3,\"TransactTime\":\"2025-04-30T08:00:10.173697+09:00\",\"OrdType\":1,\"Price\":154.0,\"Qty\":2000.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":2,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250428A02N11842270\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-28T09:05:19.24921+09:00\",\"Symbol\":\"4586\",\"SymbolName\":\"メドレックス\",\"Exchange\":1,\"ExchangeName\":\"東証グ\",\"Price\":65.0,\"OrderQty\":4600.0,\"CumQty\":4600.0,\"Side\":\"2\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250428,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250428A02N11842270\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T09:05:19.24921+09:00\",\"OrdType\":1,\"Price\":65.0,\"Qty\":4600.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250428B02N11842271\",\"RecType\":4,\"ExchangeID\":\"A4110300003901\",\"State\":3,\"TransactTime\":\"2025-04-28T09:05:19.242244+09:00\",\"OrdType\":1,\"Price\":65.0,\"Qty\":4600.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250428E02N12119240\",\"RecType\":8,\"ExchangeID\":\"991\",\"State\":3,\"TransactTime\":\"2025-04-28T10:37:27.663372+09:00\",\"OrdType\":0,\"Price\":65.0,\"Qty\":4600.0,\"ExecutionID\":\"E20250428022DN\",\"ExecutionDay\":\"2025-04-28T10:37:27.663372+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":2,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250428A02N11842302\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-28T09:05:23.242913+09:00\",\"Symbol\":\"6093\",\"SymbolName\":\"エスクロー・エージェント・ジャパン\",\"Exchange\":1,\"ExchangeName\":\"東証ス\",\"Price\":129.0,\"OrderQty\":2300.0,\"CumQty\":2300.0,\"Side\":\"2\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250428,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250428A02N11842302\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T09:05:23.242913+09:00\",\"OrdType\":1,\"Price\":129.0,\"Qty\":2300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250428B02N11842303\",\"RecType\":4,\"ExchangeID\":\"A4110300003909\",\"State\":3,\"TransactTime\":\"2025-04-28T09:05:23.24512+09:00\",\"OrdType\":1,\"Price\":129.0,\"Qty\":2300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250428E02N11967753\",\"RecType\":8,\"ExchangeID\":\"352\",\"State\":3,\"TransactTime\":\"2025-04-28T09:36:46.915054+09:00\",\"OrdType\":0,\"Price\":129.0,\"Qty\":2300.0,\"ExecutionID\":\"E2025042801DUX\",\"ExecutionDay\":\"2025-04-28T09:36:46.915054+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":2,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250428A02N11842835\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-28T09:07:14.816973+09:00\",\"Symbol\":\"3656\",\"SymbolName\":\"ＫＬａｂ\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":121.0,\"OrderQty\":2400.0,\"CumQty\":2400.0,\"Side\":\"2\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250428,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250428A02N11842835\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T09:07:14.816973+09:00\",\"OrdType\":1,\"Price\":121.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250428B02N11842836\",\"RecType\":4,\"ExchangeID\":\"A4110400004083\",\"State\":3,\"TransactTime\":\"2025-04-28T09:07:14.818273+09:00\",\"OrdType\":1,\"Price\":121.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250428E02N11899503\",\"RecType\":8,\"ExchangeID\":\"352\",\"State\":3,\"TransactTime\":\"2025-04-28T09:19:38.390235+09:00\",\"OrdType\":0,\"Price\":121.0,\"Qty\":2400.0,\"ExecutionID\":\"E20250428012VS\",\"ExecutionDay\":\"2025-04-28T09:19:38.390235+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250428A02N11899887\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-28T09:21:15.382126+09:00\",\"Symbol\":\"3656\",\"SymbolName\":\"ＫＬａｂ\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":122.0,\"OrderQty\":2400.0,\"CumQty\":2400.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250428,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250428A02N11899887\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T09:21:15.382126+09:00\",\"OrdType\":1,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250428B02N11899888\",\"RecType\":4,\"ExchangeID\":\"A4110000005119\",\"State\":3,\"TransactTime\":\"2025-04-28T09:21:15.37785+09:00\",\"OrdType\":1,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250428E02N11936492\",\"RecType\":8,\"ExchangeID\":\"669\",\"State\":3,\"TransactTime\":\"2025-04-28T09:28:50.387195+09:00\",\"OrdType\":0,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":\"E20250428018ZI\",\"ExecutionDay\":\"2025-04-28T09:28:50.387195+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":2,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250428A02N11936564\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-28T09:29:15.094426+09:00\",\"Symbol\":\"3656\",\"SymbolName\":\"ＫＬａｂ\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":121.0,\"OrderQty\":2400.0,\"CumQty\":2400.0,\"Side\":\"2\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250428,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250428A02N11936564\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T09:29:15.094426+09:00\",\"OrdType\":1,\"Price\":121.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250428B02N11936565\",\"RecType\":4,\"ExchangeID\":\"A4110700005573\",\"State\":3,\"TransactTime\":\"2025-04-28T09:29:15.088704+09:00\",\"OrdType\":1,\"Price\":121.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250428E02N12067579\",\"RecType\":8,\"ExchangeID\":\"792\",\"State\":3,\"TransactTime\":\"2025-04-28T10:07:30.976821+09:00\",\"OrdType\":0,\"Price\":121.0,\"Qty\":2400.0,\"ExecutionID\":\"E2025042801SKT\",\"ExecutionDay\":\"2025-04-28T10:07:30.976821+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250428A02N11967823\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-28T09:37:18.441619+09:00\",\"Symbol\":\"6093\",\"SymbolName\":\"エスクロー・エージェント・ジャパン\",\"Exchange\":1,\"ExchangeName\":\"東証ス\",\"Price\":130.0,\"OrderQty\":2300.0,\"CumQty\":2300.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250428,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250428A02N11967823\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T09:37:18.441619+09:00\",\"OrdType\":1,\"Price\":130.0,\"Qty\":2300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250428B02N11967824\",\"RecType\":4,\"ExchangeID\":\"A4110500005902\",\"State\":3,\"TransactTime\":\"2025-04-28T09:37:18.426108+09:00\",\"OrdType\":1,\"Price\":130.0,\"Qty\":2300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250428E02N11967825\",\"RecType\":8,\"ExchangeID\":\"429\",\"State\":3,\"TransactTime\":\"2025-04-28T09:37:18.426207+09:00\",\"OrdType\":0,\"Price\":131.0,\"Qty\":2300.0,\"ExecutionID\":\"E2025042801E41\",\"ExecutionDay\":\"2025-04-28T09:37:18.426207+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250428A02N12119362\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-28T10:39:16.750158+09:00\",\"Symbol\":\"4586\",\"SymbolName\":\"メドレックス\",\"Exchange\":1,\"ExchangeName\":\"東証グ\",\"Price\":66.0,\"OrderQty\":4600.0,\"CumQty\":4600.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250428,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250428A02N12119362\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T10:39:16.750158+09:00\",\"OrdType\":1,\"Price\":66.0,\"Qty\":4600.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250428B02N12119363\",\"RecType\":4,\"ExchangeID\":\"A4110600008156\",\"State\":3,\"TransactTime\":\"2025-04-28T10:39:16.734035+09:00\",\"OrdType\":1,\"Price\":66.0,\"Qty\":4600.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250428E02N12119556\",\"RecType\":8,\"ExchangeID\":\"1119\",\"State\":3,\"TransactTime\":\"2025-04-28T10:41:38.244579+09:00\",\"OrdType\":0,\"Price\":66.0,\"Qty\":4600.0,\"ExecutionID\":\"E20250428023KJ\",\"ExecutionDay\":\"2025-04-28T10:41:38.244579+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":2,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250428A02N12380959\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-28T14:26:18.6974+09:00\",\"Symbol\":\"7545\",\"SymbolName\":\"西松屋チェーン\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":2184.0,\"OrderQty\":400.0,\"CumQty\":400.0,\"Side\":\"2\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250428,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250428A02N12380959\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T14:26:18.6974+09:00\",\"OrdType\":1,\"Price\":2184.0,\"Qty\":400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250428B02N12380960\",\"RecType\":4,\"ExchangeID\":\"A4110700012491\",\"State\":3,\"TransactTime\":\"2025-04-28T14:26:18.74922+09:00\",\"OrdType\":1,\"Price\":2184.0,\"Qty\":400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250428E02N12411513\",\"RecType\":8,\"ExchangeID\":\"3151\",\"State\":3,\"TransactTime\":\"2025-04-28T14:38:29.293744+09:00\",\"OrdType\":0,\"Price\":2184.0,\"Qty\":300.0,\"ExecutionID\":\"E2025042803HCM\",\"ExecutionDay\":\"2025-04-28T14:38:29.293744+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250428E02N12411514\",\"RecType\":8,\"ExchangeID\":\"3153\",\"State\":3,\"TransactTime\":\"2025-04-28T14:38:29.300069+09:00\",\"OrdType\":0,\"Price\":2184.0,\"Qty\":100.0,\"ExecutionID\":\"E2025042803HCN\",\"ExecutionDay\":\"2025-04-28T14:38:29.300069+09:00\",\"DelivDay\":20250501,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250428A02N12635510\",\"State\":3,\"OrderState\":3,\"OrdType\":1,\"RecvTime\":\"2025-04-28T17:47:03.173425+09:00\",\"Symbol\":\"3656\",\"SymbolName\":\"ＫＬａｂ\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":122.0,\"OrderQty\":2400.0,\"CumQty\":0.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250508,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250428A02N12635510\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T17:47:03.173425+09:00\",\"OrdType\":1,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":2,\"ID\":\"20250428B02N12635511\",\"RecType\":4,\"ExchangeID\":null,\"State\":1,\"TransactTime\":\"2025-04-28T17:47:03.173425+09:00\",\"OrdType\":1,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":3,\"ID\":\"20250428G02N12829819\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-29T06:06:14.244299+09:00\",\"OrdType\":1,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250428B02N12829820\",\"RecType\":4,\"ExchangeID\":\"A4110100000932\",\"State\":3,\"TransactTime\":\"2025-04-30T08:00:10.190882+09:00\",\"OrdType\":1,\"Price\":122.0,\"Qty\":2400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250428A02N12635515\",\"State\":3,\"OrderState\":3,\"OrdType\":1,\"RecvTime\":\"2025-04-28T17:47:03.501034+09:00\",\"Symbol\":\"7545\",\"SymbolName\":\"西松屋チェーン\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":2272.0,\"OrderQty\":400.0,\"CumQty\":0.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250508,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250428A02N12635515\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-28T17:47:03.501034+09:00\",\"OrdType\":1,\"Price\":2272.0,\"Qty\":400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":2,\"ID\":\"20250428B02N12635516\",\"RecType\":4,\"ExchangeID\":null,\"State\":1,\"TransactTime\":\"2025-04-28T17:47:03.501034+09:00\",\"OrdType\":1,\"Price\":2272.0,\"Qty\":400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":3,\"ID\":\"20250428G02N12829823\",\"RecType\":2,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-29T06:06:14.259899+09:00\",\"OrdType\":1,\"Price\":2272.0,\"Qty\":400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":6,\"ID\":\"20250428B02N12829824\",\"RecType\":4,\"ExchangeID\":\"A4110600000938\",\"State\":3,\"TransactTime\":\"2025-04-30T08:00:10.210136+09:00\",\"OrdType\":1,\"Price\":2272.0,\"Qty\":400.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":3,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250429A02N13290860\",\"State\":3,\"OrderState\":3,\"OrdType\":1,\"RecvTime\":\"2025-04-30T00:39:18.752342+09:00\",\"Symbol\":\"9076\",\"SymbolName\":\"セイノーホールディングス\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":2338.0,\"OrderQty\":300.0,\"CumQty\":0.0,\"Side\":\"1\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250430,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250429A02N13290860\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-30T00:39:18.752342+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250429B02N13290861\",\"RecType\":4,\"ExchangeID\":\"A4110400001470\",\"State\":3,\"TransactTime\":\"2025-04-30T08:00:16.165007+09:00\",\"OrdType\":1,\"Price\":2338.0,\"Qty\":300.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":2,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250429A02N13299461\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-30T03:16:41.690063+09:00\",\"Symbol\":\"3656\",\"SymbolName\":\"ＫＬａｂ\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":110.0,\"OrderQty\":0.0,\"CumQty\":0.0,\"Side\":\"2\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250430,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250429A02N13299461\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-30T03:16:41.690063+09:00\",\"OrdType\":1,\"Price\":110.0,\"Qty\":100.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250429B02N13299462\",\"RecType\":4,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-30T03:21:09.689334+09:00\",\"OrdType\":1,\"Price\":110.0,\"Qty\":100.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250429D02N13299472\",\"RecType\":6,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-30T03:21:09.689334+09:00\",\"OrdType\":null,\"Price\":null,\"Qty\":100.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0}]},{\"CashMargin\":2,\"MarginTradeType\":1,\"MarginPremium\":null,\"ID\":\"20250429A02N13299466\",\"State\":5,\"OrderState\":5,\"OrdType\":1,\"RecvTime\":\"2025-04-30T03:20:35.852067+09:00\",\"Symbol\":\"3656\",\"SymbolName\":\"ＫＬａｂ\",\"Exchange\":1,\"ExchangeName\":\"東証プ\",\"Price\":121.0,\"OrderQty\":0.0,\"CumQty\":0.0,\"Side\":\"2\",\"AccountType\":4,\"DelivType\":0,\"ExpireDay\":20250430,\"Details\":[{\"SeqNum\":1,\"ID\":\"20250429A02N13299466\",\"RecType\":1,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-30T03:20:35.852067+09:00\",\"OrdType\":1,\"Price\":121.0,\"Qty\":100.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":4,\"ID\":\"20250429B02N13299467\",\"RecType\":4,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-30T03:21:01.389922+09:00\",\"OrdType\":1,\"Price\":121.0,\"Qty\":100.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0},{\"SeqNum\":5,\"ID\":\"20250429D02N13299471\",\"RecType\":6,\"ExchangeID\":null,\"State\":3,\"TransactTime\":\"2025-04-30T03:21:01.374321+09:00\",\"OrdType\":null,\"Price\":null,\"Qty\":100.0,\"ExecutionID\":null,\"ExecutionDay\":null,\"DelivDay\":20250502,\"Commission\":0.0,\"CommissionTax\":0.0}]}]";

			JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
			jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
			//Common.DebugInfo("TestExec", raw);
			ResponseOrders[] res = JsonConvert.DeserializeObject<ResponseOrders[]>(raw, jsonSerializerSettings);
			if (res != null) {
				bool isOk = true;
				foreach (ResponseOrders order in res) {
					// 個数はワンちゃん0あるか？
					if (order.ID == "" || order.Price <= 0) {
						isOk = false;
						Common.DebugInfo("TestExec","No", order.Symbol, order.ID);
					}
				}
				if (isOk){
					//Common.DebugInfo("TestExec", raw);
				}
			}


			/*
			//CsvControll.Log("TestExec", "aaa", "", "");
			Dictionary<string, Dictionary<string, string>> rankingDic = new Dictionary<string, Dictionary<string, string>>();
			foreach (string[] tmpRank in CsvControll.GetRankingInfo()) {
				if (!rankingDic.ContainsKey(tmpRank[0])) rankingDic[tmpRank[0]] = new Dictionary<string, string>();
				DateTime date = DateTime.Parse(tmpRank[1]);
				rankingDic[tmpRank[0]][date.ToString("yyyy/MM/dd HH:mm")] = tmpRank[2];
			}
			List<string[]> saveRankingInfo = new List<string[]>();
			foreach (string symbol in rankingDic.Keys) {
				foreach (KeyValuePair<string, string> pair in rankingDic[symbol]) {
					saveRankingInfo.Add(new string[3] { symbol, pair.Key, pair.Value });
				}
			}

			CsvControll.SaveRankingInfoOld(saveRankingInfo, DateTime.Today);

			//CsvControll.SaveRankingInfo(new List<string[]>());

			return;

			string aa = "2025/04/24 9:02";
			string bb = "2025-04-23T15:30:00+09:00";
			DateTime dateB = DateTime.Parse(bb);
			DateTime dateA = DateTime.Parse(aa);

			Common.DebugInfo("TestExec", dateB.ToString("yyyy/MM/dd hh:mm"), dateA.ToString("yyyy/MM/dd hh:mm"));
			return;
			RankingInfo[] rankingInfos = RequestBasic.RequestRanking();
			foreach (RankingInfo rankingInfo in rankingInfos) {
				Common.DebugInfo("RankingInfo", rankingInfo.No, rankingInfo.ChangePercentage, rankingInfo.Symbol);
			}
			return;

			string d = "2025-04-04T13:10:07.881899+09:00";
			DateTime da = DateTime.Parse(d);

			ResponseBoard jScoreRes = RequestBasic.RequestBoard(101, 1);
			//CsvControll.Log("GetJapanScoreNow", jScoreRes.ChangePreviousClosePer.ToString(), "", "");

			Common.DebugInfo("TestExec", da.ToString(), jScoreRes.ChangePreviousClosePer.ToString());

			return;

			DateTime fisDate = DateTime.Parse("2025/10/25");

			for (int a = 1; a < 30; a++) {
				DateTime date = DateTime.Parse("2025/03/" + a.ToString());
				Common.DebugInfo("TestExec",
					date.ToString(),
					Common.DateBuyRatioOne(date, fisDate),
					Common.DateBuyRatioAll(date),
					Common.IsLossCutDate(date, fisDate),
					Common.IsHalfSellDate(date, fisDate)
				);
			}

			*/
		}


	}
}
