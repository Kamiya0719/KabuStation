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
		List<CodeResOrder> codeResOrders = new List<CodeResOrder>();
		private List<CodeResOrder> buyNowOrders = new List<CodeResOrder>(); // 現在注文中購入注文ID
		private List<CodeResOrder> sellNowOrders = new List<CodeResOrder>(); // 現在注文中購入注文ID

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


		/**
		 * EveryDayExecで行う
		 */

		// #A1# 前日所持銘柄についての情報セット
		public void SetBeforePoss(int qty, double setSellPrice, int havePeriod)
		{
			startHave += qty;
			setSellPrice = YobinePrice(setSellPrice);
			if (idealSellPrice == 0 || idealSellPrice > setSellPrice) idealSellPrice = (int)setSellPrice; // 基本複数値段なら安い方
			if (havePeriod >= 42) isLossSell = true;
		}

		// 予想購入費用
		public double TommorowBuy(int buyBasePrice, DateTime date)
		{
			double buy = lastEndPrice * BuyNeedNum(buyBasePrice, date);
			return buy >= Def.BuyLowestPrice ? buy : 0;
		}

		// #A3# EveryDayにて理想売の数と値段を設定・キャンセル対象の返却
		public List<string> SetIdealSell(Dictionary<string, CodeResOrder> codeResOrdersAll)
		{
			// todo これはもっと前にやるかな
			foreach (KeyValuePair<string, CodeResOrder> pair in codeResOrdersAll) {
				CodeResOrder order = pair.Value;
				if (order.Symbol == Symbol) codeResOrders.Add(order);
			}


			sellOrderNeed = startHave;
			List<string> res = new List<string>();
			foreach (CodeResOrder order in codeResOrders) {
				if (order.IsValid() && order.IsSell()) {
					if (order.Price == idealSellPrice && order.OrderQty == startHave) {
						// 理想売り注文要件を完全に満たしていればsellOrderNeedを0にする それ以外キャンセル
						sellOrderNeed = 0;
					} else {
						res.Add(order.ID);
					}
				}
			}
			return res;
		}

		// #A5# 今日購入する銘柄かどうかの設定 プロ500+条件を満たす+残高 + 55万以下+10万以上
		public void SetIsBuy(int buyBasePrice, DateTime date)
		{
			if (type == Def.TypeSp) {
				isBuy = true;
			} else {
				isBuy = TommorowBuy(buyBasePrice, date) > 0;
			}
		}


		/**
		 * 両方MinitesExecで行う
		 */

		// #A2# #B1# positionsの損益率および日経平均スコアから、損切するか決め終値売却フラグを立てる これは先かな？
		public void SetIsLossSell(List<ResponsePositions> positions, int jScore, DateTime date, double lastLastEndPrice = 9999)
		{
			if (jScore == Def.JScoreOverUp) { isLossSell = true; return; }
			//if (Def.TranpMode) { isLossSell = false; return; }
			// todo 普段はオーバーダウンでもあれかなー
			if (jScore == Def.JScoreOverDown) {
				if (Def.TranpMode || Def.SubTranpMode) { isLossSell = false; return; }
				jScore = 0;
			}

			if (isLossSell) return;

			// todo sp系は損切はなし？やるならEveryのほうかな
			if (type == Def.TypeSp) {

				return;
			}

			isLossSell = true;
			bool isHalfLoss = Common.IsLossCutDate(date, FisDate());
			foreach (ResponsePositions position in positions) {
				// 当日買ったやつについては損切なし EveryDayのほうではdateは翌営業日なので当日であることはありえない
				if (Common.SameD(Common.DateParse(position.ExecutionDay), date)) { isLossSell = false; break; }
				double beforeBenefit = position.CurrentPrice / (lastLastEndPrice == 9999 ? lastEndPrice : lastLastEndPrice) - 1;
				// 損失が6％未満かつ前日からの上昇が-3.5％より大きい これが一個でもあればfalseで損切しない todo これ安値？
				if (position.ProfitLossRate < (isHalfLoss ? Def.LossCutRatioHalf[jScore, 0] : Def.LossCutRatio[jScore, 0])
					&& beforeBenefit > -0.01 * (isHalfLoss ? Def.LossCutRatioHalf[jScore, 0] : Def.LossCutRatio[jScore, 1])
				) {
					isLossSell = false; break;
				}
			}
		}


		/**
		 * MinitesExecで行う
		 */

		// #B2# 5分おきとかに取得する注文照会一覧を渡す(3,5のみとする) 旧データとIDごとに比較してどうのこうの
		// todo #A#でも使う？
		public void SetOrders(List<CodeResOrder> orders, int jScore, int buyBasePrice, DateTime date)
		{
			int buyConfirm = 0; // 今日確定分
			int sellConfirm = 0; // 今日確定分
			int sellNowOrder = 0; // 現在注文中
			buyNowOrders = new List<CodeResOrder>();
			sellNowOrders = new List<CodeResOrder>();
			foreach (CodeResOrder order in orders) {
				if (order.Symbol != Symbol) continue; // これエラーやな
				if (order.IsSell()) {
					// 売
					sellConfirm += (int)order.CumQty - order.startCumQty;
					if (order.IsValid()) {
						sellNowOrder += (int)(order.OrderQty - order.CumQty);
						sellNowOrders.Add(order);
					}
				} else {
					// 買
					buyConfirm += (int)order.CumQty - order.startCumQty;
					if (buyConfirm > 0) isTodayBuy = true;
					if (order.IsValid()) buyNowOrders.Add(order);
				}
			}

			// 損切フラグが立っているなら購入は行わない
			if (isLossSell) {
				isBuy = false;
			} else if (isBuy) {
				// 必要購入数 = JScoreに応じた所持必要数(50万相当-0) - 初期所持数 - 購入確定数 + 当日売却確定数
				buyNeedNum = BuyNeedNum(buyBasePrice, date, jScore) - buyConfirm;
				if (type == Def.TypeSp) buyNeedNum += sellConfirm;
			}
			if (buyNeedNum <= 0 || !isBuy) buyNeedNum = 0;
			// 理想売→購入中のものがあるなら何もしない/全部終わったら注文 損切売→0の状態から全売り/値段が変化でキャンセル&全売り
			sellOrderNeed = startHave + buyConfirm - sellConfirm - sellNowOrder;
			if (type != Def.TypeSp && buyNowOrders.Count > 0) sellOrderNeed = 0;

			// todo あってる？理想売中は購入無し ただし現在購入中なら理想売はしない
			//if (sellOrderNeed > 0) buyNeedNum = 0;
		}


		// #B3# 今回板情報(すなわち板・時間に対しての値段)取得する必要があるもの これがfalseなら売り買いしない
		public bool IsBoardCheck()
		{
			return isBuy || buyNowOrders.Count > 0 || sellNowOrders.Count > 0 || sellOrderNeed > 0;
		}

		// #B4# 板情報を渡して売値あるいは買値を設定する
		// todo #A#でも使う？
		public void SetBoard(ResponseBoard board, TimeIdx timeIdx)
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
					buyPrice = BoardPrice(board, timeIdx, low, high, true);
					// 前日比4％越えになっている場合は流石に買うのは控えるため3％で茶を濁す
					if (buyPrice >= lastEndPrice * 1.04) buyPrice = YobinePrice(lastEndPrice * 1.03);
				}
			}

			// 売却注文必要(新規・キャンセル後) or 現在売却注文中で終値売却フラグが立っている
			if ((sellOrderNeed > 0 || sellNowOrders.Count > 0) && isLossSell) lossSellPrice = BoardPrice(board, timeIdx, high, low, false);
		}
		// maxは利益最大 minは利益最小
		private int BoardPrice(ResponseBoard board, TimeIdx timeIdx, int max, int min, bool isBuy)
		{
			// listは値段高い順(損な順)に並ぶ (買 => 0:売1,1:買1,...,9:買9,10:買10) 売りは逆かしら
			int price = 0;
			if (timeIdx == TimeIdx.T0900) {
				// ⑥12時50分なら安値近く or 高値近く でワンちゃんねらい 一応即売・即買値段を見てお得な方にする
				price = Toku(isBuy, isBuy ? (int)board.Sell1.Price : (int)board.Buy1.Price, max);
			} else if (timeIdx == TimeIdx.T1420) {
				// ⑤14時なら現実的な数値に  volume以内に入るもので利益最大のもの
				price = MaxVolPrice(board, isBuy, Math.Max((int)(board.TradingVolume / 5), 30 * TradingUnit));
				price = Toku(isBuy, price, min); // 流石に利益今日最低未満は避ける
			} else if (timeIdx == TimeIdx.T1500) {
				// ④14時30分に本命くらい  volume以内に入るもので利益最大のもの
				price = MaxVolPrice(board, isBuy, Math.Max((int)(board.TradingVolume / 15), 30 * TradingUnit));
				price = Toku(isBuy, price, min); // 流石に利益今日最低未満は避ける
			} else if (timeIdx == TimeIdx.T1515) {
				// ③14時45分なら1本目狙い でも近いものに大き目の差があったら
				int maxVolPrice = MaxVolPrice(board, isBuy, 15 * TradingUnit);
				price = isBuy ? (int)board.Buy1.Price : (int)board.Sell1.Price;
				// 極端に差が大きい(0.8％以上かつ3yobine以上)
				if (IsBigDiff(isBuy, price, maxVolPrice, 1.008)) price = maxVolPrice;
			} else if (timeIdx == TimeIdx.T1520 || timeIdx == TimeIdx.T1525) {
				// ②14時50分なら1本目+1 でも近いものに大き目の差があったら
				int maxVolPrice = MaxVolPrice(board, isBuy, 8 * TradingUnit);
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
		private int MaxVolPrice(ResponseBoard board, bool isBuy, int volume)
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
		public List<string> CancelOrderIds(TimeIdx timeIdx)
		{
			List<string> res = new List<string>();
			int buyNowOrderNum = 0;
			foreach (CodeResOrder order in buyNowOrders) {
				if (!isBuy || buyPrice * Def.CancelDiff < order.Price || buyPrice > order.Price * Def.CancelDiff || (buyPrice != order.Price && (timeIdx == TimeIdx.T1525 || timeIdx == TimeIdx.T1520 || timeIdx == TimeIdx.T1515))) {
					// 金額とのずれが大きければキャンセル timeIdx=1,2,3(15時15分以降)ならわずかな差も許さん
					res.Add(order.ID);
				} else {
					// 注文中の数
					buyNowOrderNum += (int)(order.OrderQty - order.CumQty);
				}
			}
			// 注文数との差が大きければキャンセル(基本JScoreの増加にともなう減少)
			if (buyNeedNum * Def.CancelDiffNum < buyNowOrderNum || buyNeedNum > buyNowOrderNum * Def.CancelDiffNum) {
				foreach (CodeResOrder order in buyNowOrders) {
					if (!res.Contains(order.ID)) res.Add(order.ID);
				}
			}

			// 理想売では起こらない？ 金額差が大きければキャンセル
			if (isLossSell) {
				foreach (CodeResOrder order in sellNowOrders) {
					if (lossSellPrice * Def.CancelDiff < order.Price || lossSellPrice > order.Price * Def.CancelDiff) {
						res.Add(order.ID);
					}
				}
			}
			return res;
		}


		/**
		 * 汎用
		 */

		// 必要購入数を計算する 当日のjscoreによって割合を変えたりする 日付(3月や決算日など)でも変える
		private int BuyNeedNum(int buyBasePrice, DateTime date, int jScore = 0)
		{
			if (jScore == Def.JScoreOverUp) return 0;
			if (jScore == Def.JScoreOverDown) {
				// todo オーバーダウンでは指定通りの購入が基本かな？
				jScore = 0;
			}
			if (type == Def.TypeSp) buyBasePrice = Def.SpBuyBasePricew;
			return unitNum(buyBasePrice * Def.BuyJScoreRatio[jScore] * Common.DateBuyRatioOne(date, FisDate()) / lastEndPrice) - startHave;
		}


		/** 
		 * 売り買い注文用
		 */

		// #B6#
		public int BuyOrderNeed()
		{
			if (!isBuy) return 0;
			int num = buyNeedNum;
			foreach (CodeResOrder order in buyNowOrders) num -= (int)(order.OrderQty - order.CumQty);
			return num * lastEndPrice > Def.BuyLowestPrice ? num : 0;
		}
		public int BuyPrice() { return buyPrice; }
		// #A4# #B6#
		public int SellOrderNeed() { return sellOrderNeed; }
		public int SellPrice(TimeIdx timeIdx)
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
