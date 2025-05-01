using System;
using System.Collections.Generic;

namespace CSharp_sample
{
	class MinitesExec
	{
		public static void ExecBasic()
		{
			CsvControll.Log("Interval", "MinitesExecStart", "", "");

			DateTime now = DateTime.Now;
			// 祝日は終了
			if (Common.IsRestDate(now)) return;

			// 当日暫定日経平均スコアを取得
			int jScore = GetJapanScoreNow();
			CsvControll.Log("GetJapanScoreNow", "", "", jScore.ToString());
			if (jScore == Def.JScoreNotGet) return;
			int timeIdx = GetTimeIdx(now);
			bool isSpOnly = now.Hour <= 12;
			// 1銘柄ごとの基礎購入値
			int buyBasePrice = Int32.Parse(CsvControll.GetBuyBasePriceInfo()[0]);

			ResponsePositions[] posRes = RequestBasic.RequestPositions();
			ResponseOrders[] orderRes = RequestBasic.RequestOrders();


			// todo 詳細ランキング確認 ボードで確認かな
			
			Dictionary<string, Dictionary<string, string>> rankingDic = new Dictionary<string, Dictionary<string, string>>();
			foreach (string[] tmpRank in CsvControll.GetRankingInfo()) {
				if(!rankingDic.ContainsKey(tmpRank[0])) rankingDic[tmpRank[0]] = new Dictionary<string, string>();
				rankingDic[tmpRank[0]][tmpRank[1]] = tmpRank[2];
			}
			foreach (RankingInfo rankingInfo in RequestBasic.RequestRanking()) {
				string symbol = rankingInfo.Symbol;
				if (!Common.Pro500(symbol)) continue;
				if (!rankingDic.ContainsKey(symbol)) rankingDic[symbol] = new Dictionary<string, string>();
				DateTime date = DateTime.Parse(rankingInfo.CurrentPriceTime);
				rankingDic[symbol][date.ToString("yyyy/MM/dd HH:mm")] = rankingInfo.ChangePercentage.ToString();
			}
			List<string[]> saveRankingInfo = new List<string[]>();
			foreach (string symbol in rankingDic.Keys) {
				ResponseBoard boardRes = RequestBasic.RequestBoard(Int32.Parse(symbol), 1);
				if(boardRes.CurrentPriceTime == null) continue;
				DateTime date = DateTime.Parse(boardRes.CurrentPriceTime);
				rankingDic[symbol][date.ToString("yyyy/MM/dd HH:mm")] = boardRes.ChangePreviousClosePer.ToString();
				foreach (KeyValuePair<string, string> pair in rankingDic[symbol]) {
					saveRankingInfo.Add(new string[3] { symbol, pair.Key, pair.Value});
				}
			}
			CsvControll.SaveRankingInfo(saveRankingInfo);

			CsvControll.Log("Interval", "Orders", "", "");

			// 所持銘柄情報一覧から終値売却フラグ(損切)をたてる
			Dictionary<string, List<ResponsePositions>> positionCode = new Dictionary<string, List<ResponsePositions>>();
			foreach (ResponsePositions position in posRes) {
				if (position.LeavesQty <= 0) continue;
				// 13時前ならSPのみ
				if (isSpOnly && !Common.Sp10(position.Symbol)) continue;
				if (!positionCode.ContainsKey(position.Symbol)) positionCode[position.Symbol] = new List<ResponsePositions>();
				positionCode[position.Symbol].Add(position);
			}
			foreach (KeyValuePair<string, List<ResponsePositions>> pair in positionCode) {
				// 終値売却フラグ(損切)をたてる
				GetCodeDailys()[pair.Key].SetIsLossSell(pair.Value, jScore, now);
			}

			Dictionary<string, List<ResponseOrders>> orderCode = new Dictionary<string, List<ResponseOrders>>();
			foreach (KeyValuePair<string, CodeDaily> pair in GetCodeDailys()) orderCode[pair.Key] = new List<ResponseOrders>();

			// order情報の追加および処理中のものがあるので今回は無視する銘柄取得
			List<string> removeSymbols = new List<string>();
			foreach (ResponseOrders order in orderRes) {
				if (!orderCode.ContainsKey(order.Symbol)) continue;
				orderCode[order.Symbol].Add(order);
				if (!removeSymbols.Contains(order.Symbol) && (order.State == 2 || order.State == 4)) removeSymbols.Add(order.Symbol);
			}
			foreach (string removeSymbol in removeSymbols) orderCode.Remove(removeSymbol);

			// 購入注文か売却注文をするため、板情報を見る必要がある銘柄
			List<string> boardChecks = new List<string>();
			foreach (KeyValuePair<string, List<ResponseOrders>> pair in orderCode) {
				// シンボルごとに処理
				string symbol = pair.Key;
				// 13時前ならSPのみ
				if (isSpOnly && !Common.Sp10(symbol)) continue;

				// 注文が一個もない場合もある
				List<CodeResOrder> orders = new List<CodeResOrder>();
				foreach (ResponseOrders order in pair.Value) {
					if (GetCodeResOrders().ContainsKey(order.ID)) {
						// 既存アップデート
						GetCodeResOrders()[order.ID].UpdateData(order);
					} else {
						// 新規追加
						GetCodeResOrders()[order.ID] = new CodeResOrder(order, false);
					}
					orders.Add(GetCodeResOrders()[order.ID]);
				}

				GetCodeDailys()[symbol].SetOrders(orders, jScore, buyBasePrice, now);
				if (GetCodeDailys()[symbol].IsBoardCheck()) boardChecks.Add(symbol);
			}

			CsvControll.Log("Interval", "Update", "", "");

			foreach (string symbol in boardChecks) {
				CodeDaily codeDaily = GetCodeDailys()[symbol];
				ResponseBoard boardRes = RequestBasic.RequestBoard(Int32.Parse(symbol), codeDaily.Exchange);
				codeDaily.SetBoard(boardRes, timeIdx);
				List<string> ids = codeDaily.CancelOrderIds(timeIdx);
				// キャンセル対象があるならキャンセルして終了
				if (ids.Count > 0) {
					foreach (string orderId in ids) RequestBasic.RequestCancelOrder(orderId);
					continue;
				}

				// 新規注文(買・売) 中で注文数0なら終了 基本今日中に終わらせるので期間は0
				RequestBasic.RequestSendOrder(Int32.Parse(symbol), codeDaily.Exchange, true, codeDaily.BuyOrderNeed(), codeDaily.BuyPrice(), 0);
				RequestBasic.RequestSendOrder(Int32.Parse(symbol), codeDaily.Exchange, false, codeDaily.SellOrderNeed(), codeDaily.SellPrice(true), 0);
			}

			SaveCodeResOrder();
			SaveCodeDaily();

			CsvControll.Log("Interval", "MinitesExecEnd", "", "");
		}

		// 当日暫定日経平均スコアを取得
		private static int GetJapanScoreNow()
		{
			List<string[]> jScoreIkichis = CsvControll.GetJScoreIkichis();
			ResponseBoard jScoreRes = RequestBasic.RequestBoard(101, 1);
			if (jScoreRes == null) return Def.JScoreNotGet;

			if (jScoreRes.ChangePreviousClosePer >= 4) return Def.JScoreOverUp;
			if (jScoreRes.ChangePreviousClosePer <= -5) return Def.JScoreOverDown;
			//CsvControll.Log("GetJapanScoreNow", jScoreRes.ChangePreviousClosePer.ToString(), "", "");


			int jScore = -1; // 該当がなければ4にしておく
			int maxJscore = 1;
			for (int i = 0; i < jScoreIkichis.Count - 1; i++) {
				maxJscore = Math.Max(maxJscore, Int32.Parse(jScoreIkichis[i][1]) + 1);
				if (Int32.Parse(jScoreIkichis[i][0]) <= jScoreRes.CurrentPrice && Int32.Parse(jScoreIkichis[i + 1][0]) > jScoreRes.CurrentPrice) {
					jScore = Int32.Parse(jScoreIkichis[i][1]);
					break;
				}
			}
			if (jScore == -1) { jScore = maxJscore; Common.DebugInfo("maxJscore", maxJscore); }
			if (jScore >= 4) jScore = 4;

			// 保存と比較
			jScore = Math.Max(jScore, Int32.Parse(CsvControll.GetMinitesInfo()[0]));
			CsvControll.SaveMinitesInfo(new string[1] { jScore.ToString() });

			return jScore;
		}

		// 現在時刻に応じた値
		private static int GetTimeIdx(DateTime now)
		{
			if (now.Hour == 15) {
				if (now.Minute >= 25) return 1;
				if (now.Minute >= 20) return 2;
				if (now.Minute >= 15) return 3;
				return 4;
			}
			if (now.Hour == 14 && now.Minute >= 20) return 5;
			return 6;
		}



		/// <summary>
		/// 汎用public ///
		/// </summary>

		// 両方初期化
		public static void InitInfo(DateTime setDate)
		{
			CsvControll.SaveCodeDailyOld(CsvControll.GetCodeDaily(), setDate);
			CsvControll.SaveCodeResOrderOld(CsvControll.GetCodeResOrder(), setDate);
			CsvControll.SaveLogOld(setDate);
			CsvControll.SaveErrorLogOld(setDate);

			codeDailys = new Dictionary<string, CodeDaily>();
			codeResOrders = new Dictionary<string, CodeResOrder>();
			CsvControll.SaveCodeDaily(new List<string[]>());
			CsvControll.SaveCodeResOrder(new List<string[]>());

			CsvControll.ResetLog();
			CsvControll.ResetErrorLog();

			// 今日最大JSCORE保存
			CsvControll.SaveMinitesInfo(new string[1] { "0" });
			// 購入基準費保存
			CsvControll.SaveBuyBasePriceInfo(new string[1] { "0" });
			// a
			CsvControll.SaveRankingInfoOld(CsvControll.GetRankingInfo(), setDate, true);
			CsvControll.SaveRankingInfo(new List<string[]>());
			//
			CsvControll.SaveSpInfo(new List<string[]>());
		}

		// コードごとの各種情報 使いまわしそうなので一時保存
		private static Dictionary<string, CodeDaily> codeDailys = new Dictionary<string, CodeDaily>();
		public static Dictionary<string, CodeDaily> GetCodeDailys(bool isFile = true)
		{
			// 既に作ってあったらそれ返して終了 まだなければファイルから持ってくる
			if (codeDailys.Count == 0 && isFile) {
				foreach (string[] info in CsvControll.GetCodeDaily()) {
					CodeDaily codeDaily = new CodeDaily(info);
					codeDailys[codeDaily.Symbol] = codeDaily;
				}
			}
			return codeDailys;
		}
		public static void SaveCodeDaily(bool isEveryDay = false)
		{
			List<string[]> saveDatas = new List<string[]>();
			foreach (KeyValuePair<string, CodeDaily> pair in codeDailys) {
				if (isEveryDay && !pair.Value.IsBuy() && pair.Value.StartHave() <= 0) continue;
				saveDatas.Add(pair.Value.GetSaveInfo());
			}
			CsvControll.SaveCodeDaily(saveDatas);
		}

		// 注文照会情報 使いまわしそうなので一時保存
		private static Dictionary<string, CodeResOrder> codeResOrders = new Dictionary<string, CodeResOrder>();
		public static Dictionary<string, CodeResOrder> GetCodeResOrders()
		{
			// 既に作ってあったらそれ返して終了 まだなければファイルから持ってくる
			if (codeResOrders.Count > 0) return codeResOrders;
			foreach (string[] info in CsvControll.GetCodeResOrder()) {
				CodeResOrder codeResOrder = new CodeResOrder(info);
				codeResOrders[codeResOrder.ID] = codeResOrder;
			}
			return codeResOrders;
		}
		public static void SaveCodeResOrder()
		{
			List<string[]> saveDatas = new List<string[]>();
			foreach (KeyValuePair<string, CodeResOrder> pair in GetCodeResOrders()) saveDatas.Add(pair.Value.GetSaveInfo());
			CsvControll.SaveCodeResOrder(saveDatas);
		}


	}




}
