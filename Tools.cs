using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSharp_sample
{
	class Tools
	{


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
				if (pair.Key != Def.CapitalSymbol) continue;
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
			CsvControll.SaveCodeInfo(Def.JapanSymbol, trueWriteDatas);
		}

		// エラーデータチェック あとSkip対象の選別
		public static void DataChecker()
		{
			List<DateTime> dateList = CsvControll.GetDateList();
			List<string> codeList = CsvControll.GetCodeList();
			codeList.Add(Def.JapanSymbol);
			List<string> skipCode = new List<string>();
			foreach (string code in codeList) {
				if (!Common.Pro500(code)) continue;
				List<string[]> codeInfo = CsvControll.GetCodeInfo(code);
				int codeStart = -1;
				if (codeInfo.Count == 0) {
					CsvControll.ErrorLog("DataChecker0", code, "", "");
					skipCode.Add(code);
					continue;
				}
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
						if (code != Def.JapanSymbol) { skipCode.Add(code); break; }
					}
					for (int j = 1; j <= 4; j++) {
						if (Double.Parse(info[j]) <= 5) {
							// エラー2 いくらなんでも5円以下は存在しないはず
							CsvControll.ErrorLog("DataChecker2", code, i.ToString(), info[j]);
							return;
						}
					}
					if (beforePrice > 0 && (beforePrice >= Double.Parse(info[4]) * 1.6 || beforePrice * 1.6 <= Double.Parse(info[4]))) {
						// エラー3 1.3倍はありえるんか？ => skipCodeにしてしまう
						CsvControll.ErrorLog("DataChecker3", code, info[0], info[4]);
						skipCode.Add(code);
						break;
					}
					beforePrice = Double.Parse(info[4]);
				}
				if (codeStart == -1 || codeStart >= 1250) {
					// エラー4 データがなさすぎる
					CsvControll.ErrorLog("DataChecker4", code, codeStart.ToString(), codeStart.ToString());
					skipCode.Add(code);
				}
			}
			string res = "";
			foreach (string code in skipCode) { res += code + ","; }
			CsvControll.Log("DataCheckerEnd", codeList.Count.ToString(), dateList.Count.ToString(), res);
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
				ResponseBoard resB = RequestBasic.RequestBoard(Int32.Parse(symbol), 1, true);
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
				//if (8886 >= Int32.Parse(symbol)) continue;
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
				DateTime setDate = Common.GetDateByIdx(Common.GetDateIdx(DateTime.Parse("2025/06/18")) - i);
				List<string[]> conditions = CsvControll.GetConditions();
				foreach (string symbol in CsvControll.GetCodeList()) {
					List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
					if (Common.Pro500(symbol) && Condtions.IsCondOk(setDate, codeInfo, conditions)) {
						double buyPrice = 0;
						string res = "";
						foreach (string[] info in codeInfo) {
							if (Common.SameD(DateTime.Parse(info[0]), setDate)) {
								buyPrice = Double.Parse(info[4]);
							} else if (buyPrice > 0) { // 買った日以降
								res += Common.Round((1 - Double.Parse(info[4]) / buyPrice) * 100) + ",";
							}
						}
						Common.DebugInfo("BuyCheck", symbol, setDate.ToString(CsvControll.DFORM), res);
					}
				}
			}
		}

		// 日経平均が下がりすぎのラインを調査
		public static void CheckJapanInfo()
		{
			string symbol = Def.JapanSymbol;
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
		public static double IsLowPriceCheck(string symbol)
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

			return isOk ? basePrice : 0;
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
		public static void OldCheck()
		{
			DateTime now = DateTime.Parse("2025/05/02");
			OldCheckBase(now, RequestBasic.RequestPositions(), 800000);
		}
		public static void OldCheckBase(DateTime now, ResponsePositions[] posRes, int basePrice)
		{
			DateTime nowLast = DateTime.Parse(now.ToString("yyyy/MM/dd") + " 15:30");
			DateTime beforeDay = Common.GetDateByIdx(Common.GetDateIdx(now) - 1);
			DateTime beforeLast = DateTime.Parse(beforeDay.ToString("yyyy/MM/dd") + " 15:40");


			Dictionary<string, double> posNums = new Dictionary<string, double>();
			// 今日買ったなら、売ってない可能性もある
			Dictionary<string, bool> nowBuy = new Dictionary<string, bool>();
			// 前日に買ったものがあるなら売ってないのはありえない
			Dictionary<string, bool> beforeBuy = new Dictionary<string, bool>();
			foreach (ResponsePositions resP in posRes) {
				if (resP.LeavesQty <= 0) continue;
				//Common.DebugInfo("posRes", resP.Symbol,resP.LeavesQty, resP.HoldQty);
				if (!posNums.ContainsKey(resP.Symbol)) posNums[resP.Symbol] = 0;
				posNums[resP.Symbol] += resP.LeavesQty;
				if (Common.SameD(Common.DateParse(resP.ExecutionDay), now)) {
					nowBuy[resP.Symbol] = true;
				} else {
					beforeBuy[resP.Symbol] = true;
				}
			}

			Dictionary<string, CodeDaily> codeDailys = new Dictionary<string, CodeDaily>();
			Dictionary<string, List<CodeResOrder>> codeResOrders = new Dictionary<string, List<CodeResOrder>>();
			foreach (string[] info in CsvControll.GetCodeDailyOld(now)) {
				CodeDaily codeDaily = new CodeDaily(info);
				codeDailys[codeDaily.Symbol] = codeDaily;
				if (!codeResOrders.ContainsKey(codeDaily.Symbol)) codeResOrders[codeDaily.Symbol] = new List<CodeResOrder>();
			}
			foreach (string[] info in CsvControll.GetCodeResOrderOld(now)) {
				CodeResOrder codeResOrder = new CodeResOrder(info);
				if (!codeResOrders.ContainsKey(codeResOrder.Symbol)) continue;
				// 当日1530以降の日付のものは不要
				if (Common.NewD2Second(nowLast, codeResOrder.GetRecvTime())) continue;
				codeResOrders[codeResOrder.Symbol].Add(codeResOrder);
			}

			// 所持の状況は？→codeDailyの初期値と最終所持値はわかる isbuyがfalseなのに増えてたらおかしい
			// orderは基本的に1か3か5である模様 5は完了かキャンセル 3は現在実施？ 1は注文しようとしている(1529時点で)
			// 買い・損切は今日受注のはず→でもeveryが...
			foreach (KeyValuePair<string, CodeDaily> pair in codeDailys) {
				CodeDaily codeDaily = pair.Value;
				bool isBuy = codeDaily.IsBuy();
				bool isLossSell = codeDaily.IsLossSell();
				bool isSp = codeDaily.IsSp();
				// 今日開始時点で買うはずのやつ 存在してれば前日16時以降に注文しているはず
				codeDaily.SetBuyBasePrice(basePrice);
				double todayBuy = codeDaily.TommorowBuy();
				bool isTodayBuy = false;
				bool isTodaySell = false;
				bool isSellValid = false;
				//todo 今日有効であった売りは見られるかな？
				foreach (CodeResOrder codeResOrder in codeResOrders[codeDaily.Symbol]) {
					if (codeResOrder.IsSell()) {
						// 売
						if (Common.NewD2Second(beforeLast, codeResOrder.GetRecvTime())) isTodaySell = true;
						if (codeResOrder.IsValid()) isSellValid = true;
					} else {
						// 買
						if (Common.NewD2Second(beforeLast, codeResOrder.GetRecvTime())) isTodayBuy = true;
					}
				}

				if (isBuy && todayBuy > 0 && !isTodayBuy) CsvControll.ErrorLog("OldCheck", "NotTodayBuy", codeDaily.Symbol, "");
				if (isLossSell && !isTodaySell) CsvControll.ErrorLog("OldCheck", "NotLossSell", codeDaily.Symbol, "");
				if (beforeBuy.ContainsKey(codeDaily.Symbol) && !isSellValid) CsvControll.ErrorLog("OldCheck", "NotTodaySell", codeDaily.Symbol, "");
				if (!isBuy && isLossSell && isTodayBuy) CsvControll.ErrorLog("OldCheck", "TodayBuy", codeDaily.Symbol, "");
			}


		}


		/** ランキング関連 */
		public static void CheckRanking()
		{

			List<string> resAll = new List<string>();
			Dictionary<int, int> lowestSum = new Dictionary<int, int>();
			Dictionary<int, int> lowestTimeSum = new Dictionary<int, int>();
			Dictionary<int, Dictionary<int, int>> diffSum = new Dictionary<int, Dictionary<int, int>>();

			foreach (string dateString in CsvControll.GetRankingOldList()) {
				DateTime date = Common.DateParse(Int32.Parse(dateString));
				Dictionary<string, Dictionary<int, double>> infoAll = new Dictionary<string, Dictionary<int, double>>();
				foreach (string[] rankingInfo in CsvControll.GetRankingInfoOld(date)) {
					string symbol = rankingInfo[0];
					DateTime time = DateTime.Parse(rankingInfo[1]);
					if (!Common.SameD(time, date)) continue;
					double ratio = Double.Parse(rankingInfo[2]);
					if (!infoAll.ContainsKey(symbol)) infoAll[symbol] = new Dictionary<int, double>();
					infoAll[symbol][Int32.Parse(time.ToString("HHmm"))] = ratio;
				}

				foreach (KeyValuePair<string, Dictionary<int, double>> pair in infoAll) {
					string symbol = pair.Key;
					int lowest = 0; int lowTime = 0;
					foreach (KeyValuePair<int, double> pair2 in pair.Value) {
						// 何を調べるべきか => その日の最大値と時間 %ごとの分類(その後の減少率上昇率)
						int time = pair2.Key;
						int ratio = (int)Math.Round(pair2.Value, MidpointRounding.AwayFromZero);
						if (lowest > ratio) { lowest = ratio; lowTime = time; }
					}
					if (!lowestSum.ContainsKey(lowest)) lowestSum[lowest] = 0;
					lowestSum[lowest]++;
					if (!lowestTimeSum.ContainsKey(lowTime / 10)) lowestTimeSum[lowTime / 10] = 0;
					lowestTimeSum[lowTime / 10]++;

					int maxUp = lowest;
					foreach (KeyValuePair<int, double> pair2 in pair.Value) {
						int time = pair2.Key;
						int ratio = (int)Math.Round(pair2.Value, MidpointRounding.AwayFromZero);
						if (time > lowTime && maxUp < ratio) maxUp = ratio;
					}
					if (!diffSum.ContainsKey(lowest)) diffSum[lowest] = new Dictionary<int, int>();
					if (!diffSum[lowest].ContainsKey(maxUp - lowest)) diffSum[lowest][maxUp - lowest] = 0;
					diffSum[lowest][maxUp - lowest]++;
				}



			}


			// 
			foreach (KeyValuePair<int, int> pair in lowestSum.OrderBy(c => c.Key)) {
				Common.DebugInfo("lowestSum", pair.Key, pair.Value);
			}
			foreach (KeyValuePair<int, int> pair in lowestTimeSum.OrderBy(c => c.Key)) {
				Common.DebugInfo("lowestTimeSum", pair.Key, pair.Value);
			}
			foreach (KeyValuePair<int, Dictionary<int, int>> pair in diffSum.OrderBy(c => c.Key)) {
				foreach (KeyValuePair<int, int> pair2 in pair.Value.OrderBy(c => c.Key)) {
					Common.DebugInfo("diffSum", pair.Key, pair2.Key, pair2.Value);
				}
			}




		}


		/** ランキング利益チェック */
		public static void CheckRankingBenfitAll()
		{
			// todo 各パラメータごとの情報開示も欲しいか？

			double maxBenefit = 0;
			double maxOkRate = 0;
			int okSum = 0;
			int buySum = 0;

			Dictionary<DateTime, Dictionary<string, double>> stPList = GetStPList();
			for (int i = 0; i < 6; i++) {
				double buyRatio = -3 - i;
				for (int j = 0; j < 5; j++) {
					double sellDiff = 1 + j;
					for (int t1 = 0; t1 < 10; t1++) {
						int timeMin = 900 + t1 * 3;
						for (int t2 = 0; t2 < 6; t2++) {
							int timeMax = timeMin + 4 + t2 * 4;
							(double benefit, int[] res) = CheckRankingBenfit(timeMin, timeMax, buyRatio, sellDiff, stPList);
							Common.DebugInfo("CheckRankingBenfitAll", benefit, res[0], res[1], timeMin, timeMax, buyRatio, sellDiff);
							if (res[1] > 15) maxBenefit = Math.Max(benefit / res[1], maxBenefit);
							if (res[1] > 15) maxOkRate = Math.Max((double)res[0] / res[1], maxOkRate);
						}
					}
					break;
				}
				break;
			}
			Common.DebugInfo("Max", maxBenefit, maxOkRate);
		}
		private static Dictionary<DateTime, Dictionary<string, double>> GetStPList()
		{
			Dictionary<DateTime, Dictionary<string, double>> stPList = new Dictionary<DateTime, Dictionary<string, double>>();
			Dictionary<string, List<DateTime>> list = new Dictionary<string, List<DateTime>>();
			foreach (string dateString in CsvControll.GetRankingOldList()) {
				DateTime date = Common.DateParse(Int32.Parse(dateString));
				stPList[date] = new Dictionary<string, double>();
				List<string> symbols = new List<string>();
				foreach (string[] rankingInfo in CsvControll.GetRankingInfoOld(date)) {
					if (!symbols.Contains(rankingInfo[0])) symbols.Add(rankingInfo[0]);
				}
				foreach (string symbol in symbols) {
					if (!list.ContainsKey(symbol)) list[symbol] = new List<DateTime>();
					list[symbol].Add(date);
				}
			}
			foreach (KeyValuePair<string, List<DateTime>> pair in list) {
				string symbol = pair.Key;
				List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
				for (int i = codeInfo.Count - 1; i >= 0; i--) {
					foreach (DateTime date in pair.Value) {
						if (Common.SameD(DateTime.Parse(codeInfo[i][0]), date)) {
							stPList[date][symbol] = ((Double.Parse(codeInfo[i][1]) / Double.Parse(codeInfo[i - 1][4])) - 1) * 100;
							break;
						}
					}
				}
			}
			return stPList;
		}

		// この間の時間なら購入。これ以下なら購入。買い値にこの利益をのせて売る
		private static (double, int[]) CheckRankingBenfit(int timeMin, int timeMax, double buyRatio, double sellDiff, Dictionary<DateTime, Dictionary<string, double>> stPList)
		{
			// 追加パラメータとしてダウンし始めているかとかかな	その日の現状の最大値より1下がったら買うとか
			double downRatio = 0.5;
			int timeDiff = 10; // 5分で3％は上がりすぎという考え
			int upTimeDiff = 6; // そこから4分以内に0.5％下がったら

			double benefitAll = 0;
			int[] res = new int[2];
			foreach (string dateString in CsvControll.GetRankingOldList()) {
				DateTime date = Common.DateParse(Int32.Parse(dateString));
				Dictionary<string, Dictionary<int, double>> infoAll = new Dictionary<string, Dictionary<int, double>>();
				foreach (string[] rankingInfo in CsvControll.GetRankingInfoOld(date)) {
					string symbol = rankingInfo[0];
					DateTime time = DateTime.Parse(rankingInfo[1]);
					if (!Common.SameD(time, date)) continue;
					double ratio = Double.Parse(rankingInfo[2]);
					if (!infoAll.ContainsKey(symbol)) infoAll[symbol] = new Dictionary<int, double>();
					infoAll[symbol][Int32.Parse(time.ToString("HHmm"))] = ratio;
				}

				foreach (KeyValuePair<string, Dictionary<int, double>> pair in infoAll) {
					string symbol = pair.Key;
					double buyPrice = -100;
					int lastTime = 900;
					double lastPrice = 0;
					double nowLowest = 0;

					Dictionary<int, double> list = pair.Value;
					if (stPList[date].ContainsKey(symbol)) list[0900] = stPList[date][symbol];

					// 時間差
					int upTime = 0;
					foreach (KeyValuePair<int, double> pair2 in list.OrderBy(c => c.Key)) {
						int time = pair2.Key;
						double ratio = pair2.Value;

						for (int t = time - 1; t >= time - timeDiff; t--) {
							if (list.ContainsKey(t) && ratio >= list[t] + 2) {
								upTime = time;
								//Common.DebugInfo("uptime", symbol, t, upTime, time);
								break;
							}
						}


						nowLowest = Math.Min(nowLowest, ratio);
						// 購入
						if (upTime + upTimeDiff >= time && buyPrice == -100 && timeMin <= time && timeMax >= time && buyRatio >= ratio && nowLowest + downRatio <= ratio) {
							buyPrice = ratio;
							res[1]++;
						}
						if (buyPrice != -100 && buyPrice + sellDiff <= ratio) {
							// 理想売却
							benefitAll += sellDiff;
							buyPrice = -100;
							res[0]++;
						}

						lastTime = time;
						lastPrice = ratio;
					}
					// 売却失敗
					if (buyPrice != -100) {
						benefitAll += lastPrice - buyPrice;
					}
				}
			}

			return (benefitAll, res);
		}

		/** 損切が問題ないかチェック */
		public static void CheckLossSell()
		{
			var posRes = RequestBasic.RequestPositions();
			int jScore = 0;
			DateTime setDate = DateTime.Parse("2025/06/09");
			foreach (KeyValuePair<string, CodeDaily> pair in MinitesExec.GetCodeDailys()) {
				CodeDaily codeDaily = pair.Value;
				//if (codeDaily.Symbol != "3591") continue;
				codeDaily.SetData(posRes, MinitesExec.GetCodeResOrders(), setDate, jScore, true, codeDaily.LastEndPrice());
				codeDaily.SetBuyBasePrice(600000);
				codeDaily.SetInfo();
				Common.DebugInfo("Check", codeDaily.Symbol, codeDaily.IsLossSell());
				foreach (CodeResOrder order in codeDaily.BuyValidOrders()) {
					Common.DebugInfo("CheckB", order.Symbol, order.OrderQty, order.startCumQty, order.Price);
				}
				foreach (CodeResOrder order in codeDaily.SellValidOrders()) {
					Common.DebugInfo("CheckS", order.Symbol, order.OrderQty, order.startCumQty, order.Price);
				}
			}

		}


		/** テスト */
		public static void CodeDailyTest()
		{

			// todo ダミーの何かを用意？

			// 事前に調べる？所持状態やら注文状態やら？

			// 
			EveryDayExec.ExecBasic();

			MinitesExec.ExecBasic();

			// 終わったら調べる？所持状態やら注文状態やら？

		}


		// 関数とかを簡易テストしとこ
		public static void TestExec()
		{
			CsvControll.FileTest();

			CsvControll.GetCodeList();

			string res = "aa:";
			List<string[]> a = CsvControll.GetPro500();
			Common.DebugInfo(res + a.Count);

			TimeZoneInfo localZone = TimeZoneInfo.Local;

			Console.WriteLine($"ID: {localZone.Id}");
			Console.WriteLine($"表示名: {localZone.DisplayName}");
			Console.WriteLine($"標準時名: {localZone.StandardName}");
			Console.WriteLine($"夏時間名: {localZone.DaylightName}");
			Console.WriteLine($"夏時間対応: {localZone.SupportsDaylightSavingTime}");
			Console.WriteLine($"UTCオフセット: {localZone.BaseUtcOffset}");
			Console.WriteLine($"現在のUTCオフセット: {localZone.GetUtcOffset(DateTime.Now)}");
		}


	}
}
