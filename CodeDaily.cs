using System;
using System.Collections.Generic;

namespace CSharp_sample
{

	// 前日に保有銘柄および購入予定から作り(売却or購入が必要な銘柄全部)、売買中はcsvファイルからとって操作
	public class CodeDaily
	{
		/// 初期ステータス ///
		public string Symbol;
		public int Exchange; // 市場コード
		private int yobine; //呼び値(値段指定の最低単位)
		private int TradingUnit; //単元(数指定の最低単位)
		private double lastEndPrice; // 前日終値(各種参考用)
		private int fisDate; // 決算日
		private int type; // タイプ
		/// デイリーステータス(前日にセット) ///
		private int startHave = 0; // 開始所持数
		private int idealSellPrice = 0; // ベース(理想)売却金額
		private bool isBuy = false;
		/// 両方ステータス(前日にセット or 当日セット) ///
		private bool isLossSell = false; // 終値売却フラグつまり損切(42日経過なら前日時点でtrue/当日14時に日経平均スコアと安値でtrue)
		/// 変動ステータス ///
		private int buyPrice = 0; //終値購入金額(板に応じて変動)
		private int lossSellPrice = 0; //終値売却金額(板に応じて変動)
		private bool isTodayBuy = false; // 当日に購入しているかどうか
		/// 算出ステータス ///
		private int buyNeedNum = 0; // 購入所持必要数
		private int sellOrderNeed = 0; // 売却注文必要数


		/// 保存なし一時メンバ ///
		HashSet<ResponsePositions> posList = new HashSet<ResponsePositions>();
		private HashSet<CodeResOrder> buyOrders = new HashSet<CodeResOrder>(); // 現在注文中購入注文ID
		private HashSet<CodeResOrder> sellOrders = new HashSet<CodeResOrder>(); // 現在注文中購入注文ID
		private HashSet<string> cancelIds = new HashSet<string>();
		private ResponseBoard board;
		private bool isProcess = false;
		DateTime now; int jScore; int buyBasePrice = 0; bool isLastDay; TimeIdx timeIdx; int expireDay = 0; double lastLastPrice;

		// 1から新規作成
		public CodeDaily(string Symbol, int Exchange, int yobine, int TradingUnit, double lastEndPrice, int fisDate)
		{
			this.Symbol = Symbol;
			this.Exchange = Exchange;
			this.yobine = yobine;
			this.TradingUnit = TradingUnit;
			this.lastEndPrice = lastEndPrice;
			this.fisDate = fisDate;
			this.type = Common.Sp10(Symbol) ? Def.TypeSp : Def.TypePro; // todo
		}
		// csvファイルデータをコンストラクタとする
		public CodeDaily(string[] csvInfo)
		{
			Symbol = csvInfo[0];
			Exchange = Int32.Parse(csvInfo[1]);
			yobine = Int32.Parse(csvInfo[2]);
			TradingUnit = Int32.Parse(csvInfo[3]);
			lastEndPrice = Double.Parse(csvInfo[4]);

			startHave = Int32.Parse(csvInfo[5]);
			idealSellPrice = Int32.Parse(csvInfo[6]);
			isBuy = Boolean.Parse(csvInfo[7]);

			isLossSell = Boolean.Parse(csvInfo[8]);
			buyPrice = Int32.Parse(csvInfo[9]);
			lossSellPrice = Int32.Parse(csvInfo[10]);

			buyNeedNum = Int32.Parse(csvInfo[11]);
			sellOrderNeed = Int32.Parse(csvInfo[12]);

			fisDate = Int32.Parse(csvInfo[13]);
			type = Int32.Parse(csvInfo[14]);
			isTodayBuy = Boolean.Parse(csvInfo[15]);
		}

		public string[] GetSaveInfo()
		{
			return new string[16]{
				Symbol, Exchange.ToString(), yobine.ToString(), TradingUnit.ToString(), lastEndPrice.ToString(),
				startHave.ToString(), idealSellPrice.ToString(), isBuy.ToString(), isLossSell.ToString(),
				buyPrice.ToString(), lossSellPrice.ToString(), buyNeedNum.ToString(), sellOrderNeed.ToString(),
				fisDate.ToString(), type.ToString(), isTodayBuy.ToString(),
			};
		}

		/** 一時メンバをセットする 基本的に必ず行う */
		public void SetData(ResponsePositions[] posRes, Dictionary<string, CodeResOrder> codeResOrdersAll,
			DateTime now, int jScore, bool isLastDay, double lastLastPrice = 0)
		{
			foreach (KeyValuePair<string, CodeResOrder> pair in codeResOrdersAll) {
				CodeResOrder order = pair.Value;
				if (order.Symbol != Symbol) continue;
				if (!isLastDay && order.IsProcess()) isProcess = true;
				if (order.IsSell()) {
					buyOrders.Add(order);
				} else {
					sellOrders.Add(order);
				}
			}
			foreach (ResponsePositions pos in posRes) {
				if (pos.Symbol == Symbol) posList.Add(pos);
			}
			// jscoreはEveryでは暫定かな
			this.now = now; this.jScore = jScore; this.isLastDay = isLastDay; this.lastLastPrice = lastLastPrice;
			timeIdx = isLastDay ? TimeIdx.T0000 : MinitesExec.GetTimeIdx(now);
		}
		public void SetBuyBasePrice(int buyBasePrice) { this.buyBasePrice = buyBasePrice; }
		// todo これだと終わったorderも対象やな
		public bool IsBoardCheck() { return !isProcess && (isBuy || posList.Count > 0 || buyOrders.Count > 0 || sellOrders.Count > 0); }
		public void SetBoard(ResponseBoard board) { this.board = board; }


		public void SetInfo()
		{
			if (isLastDay) SetPosListInfo();
			if (isLastDay) SetIdealSell();
			if (isLastDay) SetIsBuy();
			SetIsLossSell();
			if (!isLastDay) SetOrders();
			if (!isLastDay) SetBoardInfo();
			if (!isLastDay) SetCancelIds();
		}
		public HashSet<string> GetCancelIds() { return cancelIds; }

		/** 所持情報を使って、初期所持数/理想売り価格/42損切/注文有効期間 をセット Everyオンリー */
		private void SetPosListInfo()
		{
			startHave = 0;
			expireDay = 0;
			foreach (ResponsePositions pos in posList) {
				startHave += (int)pos.LeavesQty;

				// 約定日（建玉日）
				DateTime buyDate = DateTime.ParseExact(pos.ExecutionDay.ToString(), CsvControll.DFILEFORM, null);
				// 購入日翌日だと1になるな
				int havePeriod = Common.GetDateIdx(now) - Common.GetDateIdx(buyDate);

				int sellPeriod = 12; // 0なら今日のみ,1なら翌日まで的な
				double sellPrice = pos.Price * 1.01; // todo
				foreach (KeyValuePair<int, double> pair in (Common.IsHalfSellDate(now, FisDate()) ? Def.idealSellRatioHalf : Def.idealSellRatio)) {
					if (havePeriod <= pair.Key) { sellPrice = pos.Price * pair.Value; sellPeriod = pair.Key - havePeriod; }
				}
				if (type == Def.TypeSp) sellPrice = pos.Price + 1;

				// 所持銘柄の数を加算 理想売のベーススコア保存 42以上たっていたら終値売却フラグも保存
				sellPrice = YobinePrice(sellPrice);
				if (idealSellPrice == 0 || idealSellPrice > sellPrice) idealSellPrice = (int)sellPrice; // 基本複数値段なら安い方
				if (havePeriod >= 42) isLossSell = true;

				// 注文有効期限(yyyyMMdd形式。本日なら0) 複数あるなら小さい方優先
				int tmpExpireDay = sellPeriod > 0 ? Int32.Parse(Common.GetDateByIdx(Common.GetDateIdx(now) + sellPeriod).ToString(CsvControll.DFILEFORM)) : 0;
				if (expireDay == 0 || expireDay > tmpExpireDay) expireDay = tmpExpireDay;
			}
		}

		/** 理想売り注文をする必要がある数とキャンセルが必要なものを算出 Everyオンリー todo minitesのと合体でいいような */
		private void SetIdealSell()
		{
			sellOrderNeed = startHave;
			foreach (CodeResOrder order in sellOrders) {
				if (order.IsValid()) {
					if (order.Price == idealSellPrice && order.OrderQty == startHave) {
						// 理想売り注文要件を完全に満たしていればsellOrderNeedを0にする それ以外キャンセル
						sellOrderNeed = 0;
					} else {
						cancelIds.Add(order.ID);
					}
				}
			}
		}
		/** 購入対象となるか Everyオンリー */
		private void SetIsBuy()
		{
			if (type == Def.TypeSp) {
				isBuy = buyBasePrice > 0;
			} else {
				isBuy = TommorowBuy() > 0; // プロ500でもSPと同じでもいいといえばいい
			}
		}
		/** 損切となるか算出 両方 */
		private void SetIsLossSell()
		{
			if (jScore == Def.JScoreOverUp) { isLossSell = true; return; }
			//if (Def.TranpMode) { isLossSell = false; return; }
			// todo 普段はオーバーダウンでもあれかなー
			if (jScore == Def.JScoreOverDown) {
				if (Def.TranpMode || Def.SubTranpMode) { isLossSell = false; return; }
				jScore = 0;
			}

			if (isLossSell) return;
			if (type == Def.TypeSp) return; // todo sp系は損切はなし？やるならEveryのほうかな

			isLossSell = posList.Count > 0;
			bool isHalfLoss = Common.IsLossCutDate(now, FisDate());
			foreach (ResponsePositions pos in posList) {
				// 当日買ったやつについては損切なし EveryDayのほうではdateは翌営業日なので当日であることはありえない
				if (Common.SameD(Common.DateParse(pos.ExecutionDay), now)) { isLossSell = false; break; }
				double beforeBenefit = pos.CurrentPrice / (isLastDay ? lastLastPrice : lastEndPrice) - 1;
				// 損失が6％未満かつ前日からの上昇が-3.5％より大きい これが一個でもあればfalseで損切しない todo これ安値？
				if (pos.ProfitLossRate < (isHalfLoss ? Def.LossCutRatioHalf[jScore, 0] : Def.LossCutRatio[jScore, 0])
					&& beforeBenefit > -0.01 * (isHalfLoss ? Def.LossCutRatioHalf[jScore, 0] : Def.LossCutRatio[jScore, 1])
				) {
					isLossSell = false; break;
				}
			}
		}
		/** 注文の状態によって購入するかだったり購入必要数だったり売却必要数だったりを算出 Minitesのみ todo もうちょい後でもいいかも */
		private void SetOrders()
		{
			int sellConfirm = 0; // 今日確定分
			int sellNowOrder = 0; // 現在注文中
			foreach (CodeResOrder order in sellOrders) {
				sellConfirm += (int)order.CumQty - order.startCumQty;
				if (order.IsValid()) sellNowOrder += (int)(order.OrderQty - order.CumQty);
			}
			int buyConfirm = 0; // 今日確定分
			foreach (CodeResOrder order in buyOrders) {
				buyConfirm += (int)order.CumQty - order.startCumQty;
				if (!isLastDay && buyConfirm > 0) isTodayBuy = true;
			}

			// 損切フラグが立っているなら購入は行わない
			if (isLossSell) {
				isBuy = false;
			} else if (isBuy) {
				// 必要購入数 = JScoreに応じた所持必要数(50万相当-0) - 初期所持数 - 購入確定数 + 当日売却確定数
				buyNeedNum = BuyNeedNum() - buyConfirm;
				if (type == Def.TypeSp) buyNeedNum += sellConfirm;
			}
			if (buyNeedNum <= 0 || !isBuy) buyNeedNum = 0;
			// 理想売→購入中のものがあるなら何もしない/全部終わったら注文 損切売→0の状態から全売り/値段が変化でキャンセル&全売り
			sellOrderNeed = startHave + buyConfirm - sellConfirm - sellNowOrder;
			// todo これだと終わったやつが含まれる
			//if (type != Def.TypeSp && buyOrders.Count > 0) sellOrderNeed = 0;

			// todo あってる？理想売中は購入無し ただし現在購入中なら理想売はしない
			//if (sellOrderNeed > 0) buyNeedNum = 0;
		}


		// #B4# 板情報を渡して売値あるいは買値を設定する
		// todo #A#でも使う？いやいらんやろ boardはメンバとしてもたせるかな そうすれば後でできる
		private void SetBoardInfo()
		{
			int low = YobinePrice(board.LowPrice + yobine);
			int high = YobinePrice(board.HighPrice - yobine);

			// 流石に全部で30単元は少なすぎなのでやめておく
			if (board.TradingVolume <= TradingUnit * 30) {
				buyNeedNum = 0;
			} else if (isBuy) {
				if (type == Def.TypeSp) {
					if (timeIdx == TimeIdx.T1525) {
						isBuy = false;
						buyPrice = 0;
					} else {
						buyPrice = Common.Sp10BuyPrice(Symbol);
					}
				} else {
					// 購入注文必要 or 現在購入注文中
					buyPrice = BoardPrice(low, high, true);
					// 前日比4％越えになっている場合は流石に買うのは控えるため3％で茶を濁す
					if (buyPrice >= lastEndPrice * 1.04) buyPrice = YobinePrice(lastEndPrice * 1.03);
				}
			}

			// 売却注文必要(新規・キャンセル後) or 現在売却注文中で終値売却フラグが立っている
			if (posList.Count > 0 && isLossSell) lossSellPrice = BoardPrice(high, low, false);
		}
		// maxは利益最大 minは利益最小
		private int BoardPrice(int max, int min, bool isBuy)
		{
			// listは値段高い順(損な順)に並ぶ (買 => 0:売1,1:買1,...,9:買9,10:買10) 売りは逆かしら
			int price = 0;
			if (timeIdx == TimeIdx.T0900) {
				// ⑥12時50分なら安値近く or 高値近く でワンちゃんねらい 一応即売・即買値段を見てお得な方にする
				price = Toku(isBuy, isBuy ? (int)board.Sell1.Price : (int)board.Buy1.Price, max);
			} else if (timeIdx == TimeIdx.T1420) {
				// ⑤14時なら現実的な数値に  volume以内に入るもので利益最大のもの
				price = MaxVolPrice(isBuy, Math.Max((int)(board.TradingVolume / 5), 30 * TradingUnit));
				price = Toku(isBuy, price, min); // 流石に利益今日最低未満は避ける
			} else if (timeIdx == TimeIdx.T1500) {
				// ④14時30分に本命くらい  volume以内に入るもので利益最大のもの
				price = MaxVolPrice(isBuy, Math.Max((int)(board.TradingVolume / 15), 30 * TradingUnit));
				price = Toku(isBuy, price, min); // 流石に利益今日最低未満は避ける
			} else if (timeIdx == TimeIdx.T1515) {
				// ③14時45分なら1本目狙い でも近いものに大き目の差があったら
				int maxVolPrice = MaxVolPrice(isBuy, 15 * TradingUnit);
				price = isBuy ? (int)board.Buy1.Price : (int)board.Sell1.Price;
				// 極端に差が大きい(0.8％以上かつ3yobine以上)
				if (IsBigDiff(isBuy, price, maxVolPrice, 1.008)) price = maxVolPrice;
			} else if (timeIdx == TimeIdx.T1520 || timeIdx == TimeIdx.T1525) {
				// ②14時50分なら1本目+1 でも近いものに大き目の差があったら
				int maxVolPrice = MaxVolPrice(isBuy, 8 * TradingUnit);
				price = isBuy ? (int)board.Buy1.Price + 2 * yobine : (int)board.Sell1.Price - 2 * yobine;
				// 極端に差が大きい(1％以上かつ4yobine以上)
				if (IsBigDiff(isBuy, price, maxVolPrice, 1.01)) price = maxVolPrice;
			}

			return price;
		}
		private int Toku(bool isBuy, int a, int b)
		{
			return isBuy ? Math.Min(a, b) : Math.Max(a, b);
		}
		// 買1-10 or 売1-10 のうちvolumeの範囲内で最も得なものを算出
		private int MaxVolPrice(bool isBuy, int volume)
		{
			SellBuy[] list = isBuy ? new SellBuy[10]{
				board.Buy1,board.Buy2,board.Buy3,board.Buy4,board.Buy5,board.Buy6,board.Buy7,board.Buy8,board.Buy9,board.Buy10,
			} : new SellBuy[10] {
				board.Sell1,board.Sell2,board.Sell3,board.Sell4,board.Sell5,board.Sell6,board.Sell7,board.Sell8,board.Sell9,board.Sell10,
			};
			double price = list[0].Price;
			foreach (SellBuy sellBuy in list) {
				volume -= (int)sellBuy.Qty;
				if (volume <= 0) break;
				price = sellBuy.Price;
			}
			return (int)price;
		}
		// 差が大きい(買ならbase=Buy1,max=Buy3 => maxが小さい、売りなら逆)
		private bool IsBigDiff(bool isBuy, int price, int maxVolPrice, double ratio)
		{
			int small = isBuy ? maxVolPrice : price; int big = isBuy ? price : maxVolPrice;
			return Math.Max(small + 4 * yobine, small * ratio) <= big;
		}



		// #B5# 注文中の中でキャンセルする必要があるやつ あったらキャンセルして今回注文はしない
		// todo #A#でも使う？
		private void SetCancelIds()
		{
			int buyNowOrderNum = 0;
			foreach (CodeResOrder order in buyOrders) {
				if (!order.IsValid()) continue;
				if (!isBuy || buyPrice * Def.CancelDiff < order.Price || buyPrice > order.Price * Def.CancelDiff || (buyPrice != order.Price && (timeIdx == TimeIdx.T1525 || timeIdx == TimeIdx.T1520 || timeIdx == TimeIdx.T1515))) {
					// 金額とのずれが大きければキャンセル timeIdx=1,2,3(15時15分以降)ならわずかな差も許さん
					cancelIds.Add(order.ID);
				} else {
					// 注文中の数
					buyNowOrderNum += (int)(order.OrderQty - order.CumQty);
				}
			}
			// 注文数との差が大きければキャンセル(基本JScoreの増加にともなう減少)
			if (buyNeedNum * Def.CancelDiffNum < buyNowOrderNum || buyNeedNum > buyNowOrderNum * Def.CancelDiffNum) {
				foreach (CodeResOrder order in buyOrders) {
					if (order.IsValid()) cancelIds.Add(order.ID);
				}
			}

			// 理想売では起こらない？ 金額差が大きければキャンセル
			if (isLossSell) {
				foreach (CodeResOrder order in sellOrders) {
					if (!order.IsValid()) continue;
					if (lossSellPrice * Def.CancelDiff < order.Price || lossSellPrice > order.Price * Def.CancelDiff) cancelIds.Add(order.ID);
				}
			}
		}


		/**
		 * 汎用
		 */
		// 予想購入費用
		public double TommorowBuy()
		{
			double buy = lastEndPrice * BuyNeedNum();
			return buy >= Def.BuyLowestPrice ? buy : 0;
		}

		// 必要購入数を計算する 当日のjscoreによって割合を変えたりする 日付(3月や決算日など)でも変える
		private int BuyNeedNum()
		{
			// Everyではみなし
			int tmpJScore = isLastDay ? 0 : jScore;
			if (tmpJScore == Def.JScoreOverUp) return 0;
			if (tmpJScore == Def.JScoreOverDown) {
				// todo オーバーダウンでは指定通りの購入が基本かな？
				tmpJScore = 0;
			}
			return unitNum(buyBasePrice * Def.BuyJScoreRatio[tmpJScore] * Common.DateBuyRatioOne(now, FisDate()) / lastEndPrice) - startHave;
		}


		/** 
		 * 売り買い注文用
		 */

		// #B6#
		public int BuyOrderNeed()
		{
			if (!isBuy) return 0;
			int num = buyNeedNum;
			// todo キャンセルが含まれる？
			foreach (CodeResOrder order in buyOrders) {
				if (order.IsValid()) num -= (int)(order.OrderQty - order.CumQty);
			}
			if(num > buyNeedNum) {
				CsvControll.ErrorLog("BuyOrderNeed", Symbol, buyNeedNum.ToString(), buyBasePrice.ToString());
				return 0;
			}
			return num * lastEndPrice > Def.BuyLowestPrice ? num : 0;
		}
		public int BuyPrice() { return buyPrice; }
		// #A4# #B6#
		public int SellOrderNeed() { return sellOrderNeed; }
		public int SellPrice()
		{
			if (isLossSell && timeIdx != TimeIdx.T0000) return lossSellPrice;
			if (type == Def.TypeSp) {
				// 時間に応じて+1をなくすか
				int spPrice = YobinePrice(Common.Sp10BuyPrice(Symbol) + (timeIdx == TimeIdx.T1525 ? 0 : 1));
				// 前日設定値+1と買値+1が同値か片方の設定がないなら適当に
				if (spPrice <= 5 || idealSellPrice <= 5 || spPrice == idealSellPrice) return Math.Max(spPrice, idealSellPrice);
				// 前日設定値+4<=買値 なら 前日設定値+4
				if (spPrice + 4 <= idealSellPrice) return spPrice + 3;
				// そうでない(前日設定値+1<=買値+2 or 前日設定値+2>=買値+1)なら高い方-1
				return Math.Max(spPrice, idealSellPrice) - 1;
			}
			if (idealSellPrice > 0) return idealSellPrice;
			if (buyPrice > 0) return YobinePrice(buyPrice * 1.04); // 当日理想売り
			CsvControll.ErrorLog("SellPrice", Symbol, type.ToString(), isLossSell.ToString());
			return 0;
		}


		// げったー
		public bool IsBuy() { return isBuy; }
		public int StartHave() { return startHave; }
		public double LastEndPrice() { return lastEndPrice; }
		public bool IsLossSell() { return isLossSell; }
		public DateTime FisDate() { return Common.DateParse(fisDate); }
		public bool IsSp() { return type == Def.TypeSp; }
		public int ExpireDay() { return expireDay; }

		// 呼び値(値段最低単位)に応じて1,5,10,50...単位に変換
		private int YobinePrice(double price) { return yobine * (int)Math.Ceiling(price / yobine); }
		// 単元化で基準値の1.1倍になったら1単元マイナス(100単元として、90要求→100→0/101-181要求→200→100)
		private int unitNum(double tradeNum)
		{
			int unitNum = TradingUnit * (int)Math.Ceiling(tradeNum / TradingUnit);
			if (unitNum >= tradeNum * Def.BuyMax) unitNum -= TradingUnit;
			return unitNum;
		}

	}


}
