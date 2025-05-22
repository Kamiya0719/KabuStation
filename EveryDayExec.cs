using System;
using System.Collections.Generic;
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

			posRes = RequestBasic.RequestPositions();
			MinitesExec.SetResponseOrders(RequestBasic.RequestOrders(), true);

			CsvControll.Log("Interval", "PosAndOrders", "", "");

			// 各データの初期化 その日使ったデータ保存しておく
			MinitesExec.InitInfo(Common.GetDateByIdx(Common.GetDateIdx(setDate) - 1));

			CsvControll.Log("Interval", "InitInfo", "", "");

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

			// 所持銘柄の売却注文
			SetSell(setDate);

			CsvControll.Log("Interval", "SetSell", "", "");

			// 明日購入する銘柄の選定 どっかに保存しておく
			SetBuy(setDate);

			CsvControll.Log("Interval", "SetBuy", "", "");

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
			codeList.Add("101");

			// 被りなし所持銘柄コード一覧
			List<string> posList = new List<string>();
			foreach (ResponsePositions pos in posRes) { if (!posList.Contains(pos.Symbol)) posList.Add(pos.Symbol); }

			DateTime dDate = Common.GetDateByIdx(Common.GetDateIdx(setDate) - 1);
			foreach (string symbol in codeList) {
				if (!(Common.Pro500(symbol) || Common.Sp10(symbol) || posList.Contains(symbol) || symbol == "101")) continue;

				List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
				// ファイル読み込みを行って、一番下の行のDateTimeが同じ日付だったらスキップ
				string lastEndPrice = codeInfo[codeInfo.Count - 1][4];
				double lastLastEndPrice = Double.Parse(codeInfo[codeInfo.Count - 2][4]);
				if (!Common.SameD(DateTime.Parse(codeInfo[codeInfo.Count - 1][0]), dDate)) {
					// 板情報の取得リクエスト とりあえずexchangeは1
					ResponseBoard resB = RequestBasic.RequestBoard(Int32.Parse(symbol), 1);
					if (resB.CurrentPrice > 0) {
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
						CsvControll.ErrorLog("SetEveryDay_CurrentPrice_0", symbol, lastEndPrice, resB.CurrentPrice.ToString());
					}
				}

				if (Common.Sp10(symbol)) CsvControll.SaveSpInfo(new List<string[]>() { new string[2] { symbol, Tools.IsLowPriceCheck(symbol).ToString() } }, true);
				if (symbol == "101") continue;

				// 購入・売却対象以外はここで終了(101も) プロ500か現在所持中 なら シンボル情報取得でCodeDailyの基本をセット
				lastLastEndPrices[symbol] = lastLastEndPrice; // 前々日終値
				ResponseSymbol resS = RequestBasic.RequestSymbol(Int32.Parse(symbol), 1);
				int yobine = GetYobine(Double.Parse(lastEndPrice), resS.PriceRangeGroup);
				MinitesExec.GetCodeDailys()[symbol] = new CodeDaily(symbol, resS.Exchange, yobine, (int)resS.TradingUnit, Double.Parse(lastEndPrice), resS.FiscalYearEndBasic);
			}
		}


		// 所持銘柄全部について売却注文を行っておく
		// 既に注文済みでかつ同じ値だった場合のみ放置？
		private static void SetSell(DateTime setDate)
		{
			//bool isHalf = Common.IsHalf(setDate);
			// 日経平均スコア取得
			int jScore = CsvControll.GetTrueJScore(setDate);

			Dictionary<string, int> expireList = new Dictionary<string, int>();
			Dictionary<string, List<ResponsePositions>> poss = new Dictionary<string, List<ResponsePositions>>();
			//Dictionary<string, List<ResponseOrders>> sellNowOrders = new Dictionary<string, List<ResponseOrders>>();
			foreach (ResponsePositions resP in posRes) {
				if (resP.LeavesQty <= 0) continue;

				string symbol = resP.Symbol;
				CodeDaily codeDaily = MinitesExec.GetCodeDailys()[symbol];

				// 約定日（建玉日）
				DateTime buyDate = DateTime.ParseExact(resP.ExecutionDay.ToString(), CsvControll.DFILEFORM, null);
				// 購入日翌日だと1になるな
				int havePeriod = Common.GetDateIdx(setDate) - Common.GetDateIdx(buyDate);

				int sellPeriod = 12; // 0なら今日のみ,1なら翌日まで的な
				double sellPrice = resP.Price * 1.01; // todo
				foreach (KeyValuePair<int, double> pair in (Common.IsHalfSellDate(setDate, codeDaily.FisDate()) ? Def.idealSellRatioHalf : Def.idealSellRatio)) {
					if (havePeriod <= pair.Key) { sellPrice = resP.Price * pair.Value; sellPeriod = pair.Key - havePeriod; }
				}
				if (Common.Sp10(symbol)) sellPrice = resP.Price + 1;

				// 所持銘柄の数を加算 理想売のベーススコア保存 42以上たっていたら終値売却フラグも保存
				codeDaily.SetBeforePoss((int)resP.LeavesQty, sellPrice, havePeriod);

				// 注文有効期限(yyyyMMdd形式。本日なら0) 複数あるなら小さい方優先
				int expireDay = sellPeriod > 0 ? Int32.Parse(Common.GetDateByIdx(Common.GetDateIdx(setDate) + sellPeriod).ToString(CsvControll.DFILEFORM)) : 0;
				if (!expireList.ContainsKey(symbol) || expireList[symbol] > expireDay) expireList[symbol] = expireDay;

				// 所持銘柄を銘柄単位でまとめる
				if (!poss.ContainsKey(symbol)) {
					poss[symbol] = new List<ResponsePositions>();
					//sellNowOrders[symbol] = new List<ResponseOrders>();
				}
				poss[symbol].Add(resP);
			}

			//foreach (ResponseOrders order in ordersRes) {
			//	if ((order.State == 1 || order.State == 2 || order.State == 3) && order.Side == "1") sellNowOrders[order.Symbol].Add(order);
			//}


			// 所持株について損切フラグをたてる + 注文中なら売却必要数を0 or キャンセル対象を選出
			foreach (KeyValuePair<string, List<ResponsePositions>> pair in poss) {
				string symbol = pair.Key;
				CodeDaily codeDaily = MinitesExec.GetCodeDailys()[symbol];
				codeDaily.SetIsLossSell(pair.Value, jScore, setDate, lastLastEndPrices[symbol]);
				// 理想売りに関する設定およびキャンセル対象の取得
				foreach (string orderId in codeDaily.SetIdealSell(MinitesExec.GetCodeResOrders())) {
					//foreach (string orderId in codeDaily.SetIdealSell(sellNowOrders[symbol])) {
					RequestBasic.RequestCancelOrder(orderId);
				}
			}

			// キャンセル完了待ち スリープ
			Thread.Sleep(10000);

			// 理想売り
			foreach (KeyValuePair<string, List<ResponsePositions>> pair in poss) {
				string symbol = pair.Key;
				CodeDaily codeDaily = MinitesExec.GetCodeDailys()[symbol];
				// todo この時点ではsp系はまだbasepriceがない
				RequestBasic.RequestSendOrder(Int32.Parse(symbol), codeDaily.Exchange, false, codeDaily.SellOrderNeed(), codeDaily.SellPrice(TimeIdx.T0000), expireList[symbol]);
			}
		}

		// 明日購入する銘柄の選定 プロ500で条件を満たしていればIsBuyをtrueにする todo setIsBuyがfalse(高額系)でもマージンを減らしちゃうな
		private static void SetBuy(DateTime setDate)
		{
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
				foreach (CodeDaily codeDaily in buyList) buyPriceSum += codeDaily.TommorowBuy(buyBasePrice, setDate);
				if (nowMargin <= buyPriceSum) break;
				if (buyPriceSum > 0) setBuyBasePrice = buyBasePrice;
			}
			if (setBuyBasePrice > 0) {
				foreach (CodeDaily codeDaily in buyList) codeDaily.SetIsBuy(setBuyBasePrice, setDate);
				CsvControll.SaveBuyBasePriceInfo(new string[1] { setBuyBasePrice.ToString() });
			}
			CsvControll.Log("SetBuy", buyList.Count.ToString(), nowMargin.ToString(), setBuyBasePrice.ToString());

			// SP系の処理
			foreach (KeyValuePair<string, CodeDaily> pair in MinitesExec.GetCodeDailys()) {
				string symbol = pair.Key;
				if (!Common.Sp10(symbol)) continue;
				double basePrice = Common.Sp10BuyPrice(symbol);
				if (basePrice > 50) { // 50円以下はやめとこうかな
					if (symbol == "6740") continue;
					CodeDaily codeDaily = pair.Value;
					codeDaily.SetIsBuy(Def.SpBuyBasePricew, setDate);
					// todo 購入注文？
					//RequestBasic.RequestSendOrder(Int32.Parse(symbol), codeDaily.Exchange, true, buyOrderNeed, basePrice, 0);
				}
			}
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
			foreach (KeyValuePair<string, CodeDaily> pair in MinitesExec.GetCodeDailys()) {
				double lastEndPrice = pair.Value.LastEndPrice();
				if (pair.Value.IsBuy()) {
					double tommorowBuy = pair.Value.TommorowBuy(buyBasePrice, setDate);
					buyData.Add(new string[]{
						"コード:"+pair.Key,
						"推定購入費用:"+tommorowBuy.ToString(), // 推定購入費用(終値*数)
						"前日終値:" + lastEndPrice.ToString(),
					});
					buySum += tommorowBuy;
				}
				if (pair.Value.StartHave() > 0) {
					haveData.Add(new string[]{
						"コード:"+pair.Key,
						"損切:"+pair.Value.IsLossSell().ToString(),
						"評価額:"+(pair.Value.StartHave()*lastEndPrice).ToString(),
						"所持数:"+pair.Value.StartHave().ToString(),
						"理想売値:"+pair.Value.SellPrice(TimeIdx.T0000).ToString(), // 理想売り値段
						"前日終値:" + lastEndPrice.ToString(),
					});
					haveSum += pair.Value.StartHave() * lastEndPrice;
				}
			}

			data.Add(new string[1] { "-----BuyData(明日購入総額:" + buySum.ToString() + ",余力:" + nowMargin + ", 単体余力:" + nowOneBuyMargin + ")-----" });
			foreach (string[] d in buyData) data.Add(d);
			data.Add(new string[1] { "-----HaveData(現在所持総額:" + haveSum.ToString() + ")-----" });
			foreach (string[] d in haveData) data.Add(d);


			Dictionary<string, double> oldCumQtys = new Dictionary<string, double>();
			foreach (string[] info in CsvControll.GetCodeResOrderOld(lastLastDate)) {
				CodeResOrder codeResOrder = new CodeResOrder(info);
				oldCumQtys[codeResOrder.ID] = codeResOrder.CumQty;
			}

			buySum = 0; haveSum = 0;
			List<string[]> buyOrder = new List<string[]>(); List<string[]> sellOrder = new List<string[]>();
			foreach (KeyValuePair<string, CodeResOrder> pair in MinitesExec.GetCodeResOrders()) {
				if (pair.Value.CumQty <= 0) continue;
				DateTime date = pair.Value.GetRecvTime();
				bool isSameD = Common.SameD(lastDate, date);
				bool isSell = pair.Value.IsSell();
				double oldCumQty = oldCumQtys.ContainsKey(pair.Key) ? oldCumQtys[pair.Key] : 0;
				if (isSell && pair.Value.CumQty > oldCumQty) {
					// 昨日の約定数データと比較して今日分に絞る
					sellOrder.Add(new string[] {
						"コード:"+pair.Value.Symbol,
						"数量:"+pair.Value.CumQty.ToString()+"/"+pair.Value.OrderQty.ToString(),
						"値段:"+pair.Value.Price.ToString(),
					});
					haveSum += (pair.Value.CumQty - oldCumQty) * pair.Value.Price;
				} else if (!isSell && isSameD) {
					// 購入は今日のみでいいかな
					buyOrder.Add(new string[] {
						"コード:"+pair.Value.Symbol,
						"数量:"+pair.Value.CumQty.ToString()+"/"+pair.Value.OrderQty.ToString(),
						"値段:"+pair.Value.Price.ToString(),
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
