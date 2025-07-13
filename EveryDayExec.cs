using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CSharp_sample
{
	class EveryDayExec
	{
		private static ResponsePositions[] posRes;
		private static Dictionary<string, double> lastLastEndPrices = new Dictionary<string, double>();
		private static double nowMargin = 0;
		private static double nowOneBuyMargin = 0;

		// 毎日15時過ぎたら一回だけ実行する関数
		public static void ExecBasic()
		{
			CsvControll.Log("Interval", "EveryDayExecStart", "", "");

			// 16時とかにやって次の営業日を指定する想定 今日が営業日で15時以降なら翌営業日にする。それ以外ならそのまま
			int add = 0;
			if (Common.SameD(DateTime.Today, Common.GetDateByIdx(Common.GetDateIdx(DateTime.Today))) && DateTime.Now.Hour >= 15) add = 1;
			DateTime setDate = Common.GetDateByIdx(Common.GetDateIdx(DateTime.Today) + add);

			// 各データの初期化 その日使ったデータ保存しておく
			MinitesExec.InitInfo(Common.GetDateByIdx(Common.GetDateIdx(setDate) - 1));
			CsvControll.Log("Interval", "InitInfo", setDate.ToString(CsvControll.DFORM), add.ToString());

			posRes = RequestBasic.RequestPositions();
			MinitesExec.SetResponseOrders(RequestBasic.RequestOrders(), true);
			CsvControll.Log("Interval", "SetResponseOrders", "", "");

			// 余力
			SetMargin(setDate);

			// 前日(というか当日15時のデータ)分のコード情報を全取得して保存(日経平均含む)
			SetEveryDay(setDate);
			CsvControll.Log("Interval", "SetEveryDay", "", "");

			// 今日の分の日経平均スコアセーブ
			Condtions.SaveJapanBaseScoreOneDay(setDate);
			// 翌日の日経平均スコアをシミュレーションして閾値を保存
			Condtions.SaveTrueJScoreIkichis(setDate);
			CsvControll.Log("Interval", "SaveTrueJScoreIkichis", "", "");

			SetCodeDailyData(setDate);
			CsvControll.Log("Interval", "SetCodeDailyData", "", "");

			SetSell(setDate);
			CsvControll.Log("Interval", "SetSell", "", "");

			// 注文一覧情報作成(売注文が終わった後である必要がある)
			MinitesExec.SetResponseOrders(RequestBasic.RequestOrders(), true);

			MinitesExec.SaveCodeDaily(true);
			MinitesExec.SaveCodeResOrder();

			Tools.DataChecker();
			CsvControll.Log("Interval", "DataChecker", "", "");
			Tools.OldCheckBase(Common.GetDateByIdx(Common.GetDateIdx(setDate) - 1), posRes, Int32.Parse(CsvControll.GetBuyBasePriceInfo()[0]));
			CsvControll.Log("Interval", "OldCheckBase", "", "");

			// デイメモを保存して終了 3種使用可能金額/今日買った・売った詳細(判断材料？と金額)
			SetDayMemo(setDate);

			CsvControll.Log("Interval", "EveryDayExecEnd", "", "");
		}

		// 今日購入できる余力を設定
		private static void SetMargin(DateTime setDate)
		{
			double allHavePrices = 0;
			double weekHavePrices = 0;
			foreach (ResponsePositions resP in posRes) {
				if (Common.Sp10(resP.Symbol)) continue; // sp系は別途計算
				allHavePrices += resP.Price * resP.LeavesQty;
				// 5営業日以内に購入したもの
				if (4 >= Common.GetDateIdx(setDate) - Common.GetDateIdx(DateTime.ParseExact(resP.ExecutionDay.ToString(), CsvControll.DFILEFORM, null))) weekHavePrices += resP.Price * resP.LeavesQty;
			}
			// 現金=(購入余力+現在株所持額)/3.33
			double cash = (RequestBasic.RequestWallet() + allHavePrices) / 3.33;

			// 決算期近くは半分とする
			double buyRatio = Common.DateBuyRatioAll(setDate);

			// 現在所持が少なすぎれば増やすかな
			double allMargin = cash * Def.BuyMaxPriceAll * buyRatio - allHavePrices;
			double weekMargin = cash * Def.BuyMaxPriceWeek * buyRatio - weekHavePrices;
			if (buyRatio == 1 && allHavePrices < cash * Def.BuyMaxPriceWeekSub) weekMargin = cash * Def.BuyMaxPriceWeekSub - weekHavePrices;
			double todayMargin = cash * Def.BuyMaxPriceDay * buyRatio;
			if (buyRatio == 1 && allHavePrices < cash * Def.BuyMaxPriceDaySub) todayMargin = cash * Def.BuyMaxPriceDaySub;

			nowMargin = Math.Min(Math.Min(allMargin, weekMargin), todayMargin);

			nowOneBuyMargin = cash * Def.BuyMaxPriceCode * buyRatio;
			CsvControll.Log("SetMargin", nowMargin.ToString() + ":" + cash.ToString(),
				allMargin.ToString() + ":" + weekMargin.ToString() + ":" + todayMargin.ToString(),
				allHavePrices.ToString() + ":" + weekHavePrices.ToString());
		}

		// 毎日やって当日分の(あるいは前日？)データを追記する 500+1銘柄分 あと既に所持しているもの
		private static void SetEveryDay(DateTime setDate)
		{
			List<string> codeList = CsvControll.GetCodeList();
			codeList.Add(Def.JapanSymbol);

			// 被りなし所持銘柄コード一覧
			List<string> posList = new List<string>();
			foreach (ResponsePositions pos in posRes) { if (!posList.Contains(pos.Symbol)) posList.Add(pos.Symbol); }

			DateTime dDate = Common.GetDateByIdx(Common.GetDateIdx(setDate) - 1);
			foreach (string symbol in codeList) {
				if (!(Common.Pro500(symbol) || Common.Sp10(symbol) || posList.Contains(symbol) || symbol == Def.JapanSymbol)) continue;

				List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
				// ファイル読み込みを行って、一番下の行のDateTimeが同じ日付だったらスキップ
				string lastEndPrice = codeInfo[codeInfo.Count - 1][4];
				double lastLastEndPrice = Double.Parse(codeInfo[codeInfo.Count - 2][4]);
				if (!Common.SameD(DateTime.Parse(codeInfo[codeInfo.Count - 1][0]), dDate)) {
					// 板情報の取得リクエスト とりあえずexchangeは1
					ResponseBoard resB = RequestBasic.RequestBoard(Int32.Parse(symbol), 1, true);
					if (resB != null && resB.CurrentPrice > 0) {
						// data[0]が日付,data[1-4]がデータ
						string[] data = new string[5] {
							// 始値,高値,安値,終値
							dDate.ToString(CsvControll.DFORM) , resB.OpeningPrice.ToString(), resB.HighPrice.ToString(), resB.LowPrice.ToString(), resB.CurrentPrice.ToString()
						};
						for (int i = 1; i <= 4; i++) {
							if (data[i] == "" || Double.Parse(data[i]) == 0) data[i] = lastEndPrice; // データがなければ前日のものをつっこむ
						}
						CsvControll.SaveCodeInfo(symbol, new List<string[]>() { data }, true);

						lastLastEndPrice = Double.Parse(lastEndPrice);
						lastEndPrice = data[4];
					} else {
						CsvControll.ErrorLog("SetEveryDay_CurrentPrice_0", symbol, lastEndPrice, resB?.CurrentPrice.ToString());
					}
				}

				if (Common.Sp10(symbol)) CsvControll.SaveSpInfo(new List<string[]>() { new string[2] { symbol, Tools.IsLowPriceCheck(symbol).ToString() } }, true);
				if (symbol == Def.JapanSymbol) continue;

				// 購入・売却対象以外はここで終了(101も) プロ500か現在所持中 なら シンボル情報取得でCodeDailyの基本をセット
				lastLastEndPrices[symbol] = lastLastEndPrice; // 前々日終値
				ResponseSymbol resS = RequestBasic.RequestSymbol(Int32.Parse(symbol), 1);
				int yobine = GetYobine(Double.Parse(lastEndPrice), resS.PriceRangeGroup);
				MinitesExec.GetCodeDailys()[symbol] = new CodeDaily(symbol, resS.Exchange, yobine, (int)resS.TradingUnit, Double.Parse(lastEndPrice), resS.FiscalYearEndBasic);
			}
		}

		/** CodeDailyの各処理 */
		private static void SetCodeDailyData(DateTime setDate)
		{
			// ここでループするのはプロ500とSpと所持中のもの
			int jScore = CsvControll.GetTrueJScore(setDate);
			foreach (KeyValuePair<string, CodeDaily> pair in MinitesExec.GetCodeDailys()) {
				pair.Value.SetData(posRes, MinitesExec.GetCodeResOrders(), setDate, jScore, true, lastLastEndPrices[pair.Key]);
			}

			// 通常プロ500新規買いの対象となるものを算出
			List<string[]> conditions = CsvControll.GetConditions();
			List<CodeDaily> buyList = new List<CodeDaily>();
			foreach (KeyValuePair<string, CodeDaily> pair in MinitesExec.GetCodeDailys()) {
				if (Common.Pro500(pair.Key) && Condtions.IsCondOk(setDate, CsvControll.GetCodeInfo(pair.Key), conditions)) buyList.Add(pair.Value);
			}

			// 通常プロ500新規買いの明日の基準購入費を決定する
			int setBuyBasePrice = 0;
			for (int buyBasePrice = 100000; buyBasePrice <= nowOneBuyMargin; buyBasePrice += 10000) {
				double buyPriceSum = 0;
				foreach (CodeDaily codeDaily in buyList) {
					codeDaily.SetBuyBasePrice(buyBasePrice);
					buyPriceSum += codeDaily.TommorowBuy();
				}
				if (nowMargin <= buyPriceSum) break;
				if (buyPriceSum > 0) setBuyBasePrice = buyBasePrice;
			}
			foreach (CodeDaily codeDaily in buyList) codeDaily.SetBuyBasePrice(setBuyBasePrice);
			CsvControll.SaveBuyBasePriceInfo(new string[1] { setBuyBasePrice.ToString() });

			CsvControll.Log("SetBuy", buyList.Count.ToString(), nowMargin.ToString(), setBuyBasePrice.ToString());

			// SP系の処理
			foreach (KeyValuePair<string, CodeDaily> pair in MinitesExec.GetCodeDailys()) {
				string symbol = pair.Key;
				if (Common.Sp10(symbol) && Common.Sp10BuyPrice(symbol) > 50) pair.Value.SetBuyBasePrice(setBuyBasePrice);
			}

			// データをセット
			foreach (KeyValuePair<string, CodeDaily> pair in MinitesExec.GetCodeDailys()) pair.Value.SetInfo();
		}

		// 所持銘柄全部について売却注文を行っておく
		private static void SetSell(DateTime setDate)
		{
			foreach (KeyValuePair<string, CodeDaily> pair in MinitesExec.GetCodeDailys()) {
				foreach (string orderId in pair.Value.GetCancelIds()) RequestBasic.RequestCancelOrder(orderId);
			}

			// キャンセル完了待ち スリープ
			Thread.Sleep(10000);

			// 理想売り
			foreach (KeyValuePair<string, CodeDaily> pair in MinitesExec.GetCodeDailys()) RequestBasic.RequestSendOrder(pair.Value, false);
		}

		// 明日購入する銘柄の数・推定値段/現在所持の銘柄の数・現在値段/エラーログの数
		public static void SetDayMemo(DateTime setDate)
		{
			DateTime lastDate = Common.GetDateByIdx(Common.GetDateIdx(setDate) - 1);
			DateTime lastLastDate = Common.GetDateByIdx(Common.GetDateIdx(setDate) - 2);
			List<string[]> data = new List<string[]>();

			data.Add(new string[1] { "-----ErrorLog-----" });
			foreach (string[] d in CsvControll.GetErrorLogOld(lastDate)) data.Add(d);
			foreach (string[] d in CsvControll.GetErrorLog()) data.Add(d);


			List<string[]> buyData = new List<string[]>(); List<string[]> haveData = new List<string[]>();
			double buySum = 0; double haveSum = 0;
			int buyBasePrice = Int32.Parse(CsvControll.GetBuyBasePriceInfo()[0]);
			var dailys = MinitesExec.GetCodeDailys();
			foreach (KeyValuePair<string, CodeDaily> pair in dailys.OrderBy(c => c.Value.IsSp() ? "SP_" + c.Key : "PRO_" + c.Key)) {
				(int leaveQty, int havePeriod, int minBuyPrice, double minBenefit) = pair.Value.GetPosInfo();
				double lastEndPrice = pair.Value.LastEndPrice();
				if (pair.Value.IsBuy()) {
					double tommorowBuy = pair.Value.TommorowBuy();
					buyData.Add(new string[]{
						"コード:"+pair.Key,
						"推定購入費用:"+tommorowBuy, // 推定購入費用(終値*数)
						"前日終値:" + lastEndPrice,
						"所持数:"+pair.Value.StartHave(),
						"SP系:" + pair.Value.IsSp(),
					});
					buySum += tommorowBuy;
				}
				if (pair.Value.StartHave() > 0) {
					haveData.Add(new string[]{
						"コード:"+pair.Key,
						"損切:"+pair.Value.IsLossSell(),
						"評価額:"+(pair.Value.StartHave()*lastEndPrice),
						"所持数:"+pair.Value.StartHave(),
						"最小購入値:"+minBuyPrice,
						"最小利益:"+minBenefit,
						"理想売値:"+pair.Value.IdealSellPrice(), // 理想売り値段
						"前日終値:" + lastEndPrice,
						"SP系:" + pair.Value.IsSp(),
					});
					haveSum += pair.Value.StartHave() * lastEndPrice;
				}
			}

			data.Add(new string[1] { "-----BuyData(明日購入総額:" + buySum.ToString() + ",余力:" + nowMargin + ", 単体余力:" + nowOneBuyMargin + ")-----" });
			foreach (string[] d in buyData) data.Add(d);
			data.Add(new string[1] { "-----HaveData(現在所持総額:" + haveSum.ToString() + ")-----" });
			foreach (string[] d in haveData) data.Add(d);

			Dictionary<string, CodeDaily> codeDailyOlds = new Dictionary<string, CodeDaily>();
			foreach (string[] info in CsvControll.GetCodeDailyOld(lastDate)) {
				CodeDaily codeDaily = new CodeDaily(info);
				if (!codeDailyOlds.ContainsKey(codeDaily.Symbol)) codeDailyOlds[codeDaily.Symbol] = codeDaily;
			}

			Dictionary<string, double> oldCumQtys = new Dictionary<string, double>();
			foreach (string[] info in CsvControll.GetCodeResOrderOld(lastLastDate)) {
				CodeResOrder codeResOrder = new CodeResOrder(info);
				oldCumQtys[codeResOrder.ID] = codeResOrder.CumQty;
			}

			buySum = 0; haveSum = 0;
			List<string[]> buyOrder = new List<string[]>(); List<string[]> sellOrder = new List<string[]>();
			var orders = MinitesExec.GetCodeResOrders();
			foreach (KeyValuePair<string, CodeResOrder> pair in orders.OrderBy(c => Common.Sp10(c.Value.Symbol) ? "SP_" + c.Value.Symbol : "PRO_" + c.Value.Symbol)) {
				if (pair.Value.CumQty <= 0) continue;
				DateTime date = pair.Value.GetRecvTime();
				bool isSameD = Common.SameD(lastDate, date);
				bool isSell = pair.Value.IsSell();
				double oldCumQty = oldCumQtys.ContainsKey(pair.Key) ? oldCumQtys[pair.Key] : 0;
				if (isSell && pair.Value.CumQty > oldCumQty) {
					bool isLossSell = false;
					if (codeDailyOlds.ContainsKey(pair.Value.Symbol)) isLossSell = codeDailyOlds[pair.Value.Symbol].IsLossSell();
					// 昨日の約定数データと比較して今日分に絞る
					sellOrder.Add(new string[] {
						"コード:"+pair.Value.Symbol,
						"数量:"+pair.Value.CumQty.ToString()+"/"+pair.Value.OrderQty.ToString(),
						"値段:"+pair.Value.Price.ToString(),
						"評価額:"+pair.Value.CumQty * pair.Value.Price + "/" + pair.Value.OrderQty * pair.Value.Price,
						"損切:"+isLossSell,
						"SP系:" + (Common.Sp10(pair.Value.Symbol) ? "True" : "False"),
					});
					haveSum += (pair.Value.CumQty - oldCumQty) * pair.Value.Price;
				} else if (!isSell && isSameD) {
					// 購入は今日のみでいいかな
					buyOrder.Add(new string[] {
						"コード:"+pair.Value.Symbol,
						"数量:"+pair.Value.CumQty.ToString()+"/"+pair.Value.OrderQty.ToString(),
						"値段:"+pair.Value.Price.ToString(),
						"評価額:"+pair.Value.CumQty * pair.Value.Price + "/" + pair.Value.OrderQty * pair.Value.Price,
						"SP系:" + (Common.Sp10(pair.Value.Symbol) ? "True" : "False"),
					});
					buySum += (pair.Value.CumQty - oldCumQty) * pair.Value.Price;
				}
			}
			data.Add(new string[1] { "-----BuyOrder(今日購入総額:" + buySum.ToString() + ")-----" });
			foreach (string[] d in buyOrder) data.Add(d);
			data.Add(new string[1] { "-----SellOrder(今日売却総額:" + haveSum.ToString() + ")-----" });
			foreach (string[] d in sellOrder) data.Add(d);

			CsvControll.SaveDayMemo(data);
		}


		private static int GetYobine(double price, string PriceRangeGroup)
		{
			int yobine = 1;
			if (PriceRangeGroup == "10000") {
				foreach (KeyValuePair<int, int> pair in new Dictionary<int, int>() {
						{ 5,2700 }, { 10,4500 }, { 50,27000 },{ 100,45000 },{ 500,270000 },{ 1000,450000 },{ 5000,2700000 }
				}) {
					if (pair.Value < price) yobine = pair.Key;
				}
			} else if (PriceRangeGroup == "10003") {
				foreach (KeyValuePair<int, int> pair in new Dictionary<int, int>() {
						{ 5,9000 }, { 10,27000 }, { 50,90000 },{ 100,270000 },{ 500,900000 },{ 1000,2700000 }
				}) {
					if (pair.Value < price) yobine = pair.Key;
				}
			} else {
				// エラー
				CsvControll.ErrorLog("GetYobine", price.ToString(), PriceRangeGroup, "");
				return -100;
			}
			return yobine;
		}
	}
}
