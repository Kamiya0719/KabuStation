using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;

namespace CSharp_sample
{

	public class RequestParam
	{
		public bool isTest;
		public REQUEST_TYPE requestType;

		// 注文発注・板情報・銘柄情報
		public string symbol = "0"; // 銘柄コード
		public int exchange = 0;// 市場コード 東証くらいしか使わんか？

		// 注文発注
		public string side = "0"; // 売買区分(1:売,2:買)
		public int cashMargin = 0; // 信用区分(2:新規,3:返済)
		public int qty = 0; // 注文数量
		public int frontOrderType = 0; // 執行条件(10:成行,20:指値,30:逆指値)
		public int price = 0; // 注文価格(成行なら0)
		public int expireDay = 0; // 注文有効期限(yyyyMMdd形式。本日なら0)

		// 注文取消用
		public string orderId = ""; // 注文番号 sendorderのレスポンスで受け取るOrderID

		// 銘柄登録
		private List<object> symbols = new List<object>(); // 銘柄のリスト

		public RequestParam(REQUEST_TYPE requestType)
		{
			this.isTest = RequestBasic.isTest;
			this.requestType = requestType;
		}

		// 銘柄セット(注文発注・板情報・銘柄情報) exchangeは市場コード
		public void SetSymbol(int symbol, int exchange)
		{
			if (requestType == REQUEST_TYPE.REGISTER) {
				symbols.Add(new { Symbol = symbol.ToString().PadLeft(4, '0'), Exchange = exchange });
			} else {
				this.symbol = symbol.ToString();
				this.exchange = exchange;
			}
		}
		// 注文発注
		public void SetOrder(bool isBuy, int qty, int price, int expireDay)
		{
			this.side = isBuy ? "2" : "1";
			this.cashMargin = isBuy ? 2 : 3;
			this.qty = qty;
			this.frontOrderType = price == 0 ? 10 : 20;
			this.price = price;
			this.expireDay = expireDay;
		}
		// 注文取消
		public void SetOrderCancel(string orderId) { this.orderId = orderId; }





		public string GetSymbol()
		{
			//return symbol.ToString().PadLeft(4, '0'); 
			return symbol;
		}

		public string GetUrl()
		{
			string url = "http://localhost:1808" + (isTest ? "1" : "0") + "/kabusapi/";
			url += requestUrls[requestType];
			if (requestType == REQUEST_TYPE.BOARD || requestType == REQUEST_TYPE.SYMBOL) {
				url += "/" + GetSymbol() + "@" + exchange; // 銘柄コード [銘柄コード]@[市場コード]
			}
			if (!querySolidParams.ContainsKey(requestType)) return url;

			var builder = new UriBuilder(url);
			var param = System.Web.HttpUtility.ParseQueryString(builder.Query);
			foreach (KeyValuePair<string, string> pair in querySolidParams[requestType]) {
				param[pair.Key] = pair.Value;
			}
			builder.Query = param.ToString();

			return builder.ToString();
		}

		public object GetRequestBody()
		{
			if (requestType == REQUEST_TYPE.TOKEN) {
				return new {
					// todo
					APIPassword = isTest ? Secret.TestPass : Secret.Pass,
				};
			} else if (requestType == REQUEST_TYPE.SENDORDER) {
				return new {
					Password = Secret.SendPass, // 注文パスワード
					Symbol = GetSymbol(), // 銘柄コード
					Exchange = exchange,// 市場コード 東証くらいしか使わんか？
					SecurityType = 1, // 商品種別
					Side = side, // 売買区分(1:売,2:買)
					CashMargin = cashMargin, // 信用区分(2:新規,3:返済)
					MarginTradeType = 1, // 信用取引区分(1:制度信用)
					DelivType = side == "1" ? 2 : 0, // 受渡区分(0:指定なし,2:お預り金,3	:auマネーコネクト)
					AccountType = 4, // 口座種別(2:一般,4	:特定)
					Qty = qty, // 注文数量
					ClosePositionOrder = 0, // 決済順序
					Price = price, // 注文価格(成行なら0)
					ExpireDay = expireDay, // 注文有効期限(yyyyMMdd形式。本日なら0)
					FrontOrderType = frontOrderType, // 執行条件(10:成行,20:指値,30:逆指値)
				};
			} else if (requestType == REQUEST_TYPE.CANCELORDER) {
				return new {
					OrderId = orderId, // 注文番号 sendorderのレスポンスで受け取るOrderID
					Password = Secret.SendPass, // 注文パスワード
				};
			} else if (requestType == REQUEST_TYPE.REGISTER) {
				object[] tmp = new object[symbols.Count];
				for (int i = 0; i < symbols.Count; i++) tmp[i] = symbols[i];
				return new { Symbols = tmp, }; // 登録する銘柄のリスト
			}
			return null;
		}


		public string DebugLog()
		{

			return "PARAM Url:" + GetUrl() + ", symbol:" + GetSymbol() + ", qty:" + qty.ToString() + ", price:" + price.ToString() + ", side:" + side.ToString();
		}

		private static Dictionary<REQUEST_TYPE, string> requestUrls = new Dictionary<REQUEST_TYPE, string>() {
			{ REQUEST_TYPE.TOKEN, "token" }, { REQUEST_TYPE.SENDORDER, "sendorder" },
			{ REQUEST_TYPE.CANCELORDER, "cancelorder" }, { REQUEST_TYPE.WALLET, "wallet/margin" },
			{ REQUEST_TYPE.BOARD, "board" }, { REQUEST_TYPE.SYMBOL, "symbol" },
			{ REQUEST_TYPE.ORDERS, "orders" }, { REQUEST_TYPE.POSITIONS, "positions" },
			{ REQUEST_TYPE.REGISTER, "register" },{ REQUEST_TYPE.UNREGISTERALL, "unregister/all" },
			{ REQUEST_TYPE.RANKING, "ranking" },
		};
		// 現状不要
		private static Dictionary<REQUEST_TYPE, Dictionary<string, string>> querySolidParams = new Dictionary<REQUEST_TYPE, Dictionary<string, string>>() {
			{ REQUEST_TYPE.RANKING, new Dictionary<string, string>(){ { "Type", "2" }, { "ExchangeDivision", "T" } } },
		};
	}

	// トークン取得
	public class ResponseToken
	{
		public string resultCode;
		public string token;
	}
	// 注文/注文取消
	public class ResponseOrder
	{
		public int Result; // 結果コード 0が成功。それ以外はエラーコード。
		public string OrderId; // 受付注文番号
	}
	// 取引余力(信用)
	public class ResponseWallet
	{
		public double? MarginAccountWallet; // 信用新規可能額
		public double? DepositkeepRate; // 保証金維持率
		public double? ConsignmentDepositRate; // 委託保証金率
		public double? CashOfConsignmentDepositRate; // 現金委託保証金率
	}
	// 時価情報・板情報
	public class ResponseBoard
	{
		public string Symbol; // 銘柄コード
		public string SymbolName; // 銘柄名
		public int Exchange; // 市場コード※株式・先物・オプション銘柄の場合のみ
		public string ExchangeName; // 市場名称※株式・先物・オプション銘柄の場合のみ
		public double CurrentPrice; // 現値
		public string CurrentPriceTime; // 現値時刻
		public string CurrentPriceChangeStatus; // 現値前値比較
		public int CurrentPriceStatus; // 現値ステータス
		public double CalcPrice; // 計算用現値
		public double PreviousClose; // 前日終値
		public string PreviousCloseTime; // 前日終値日付
		public double ChangePreviousClose; // 前日比(CurrentPrice-PreviousClose)
		public double ChangePreviousClosePer; // 騰落率((CurrentPrice/PreviousClose-1)*100)
		public double OpeningPrice; // 始値
		public string OpeningPriceTime; // 始値時刻
		public double HighPrice; // 高値
		public string HighPriceTime; // 高値時刻
		public double LowPrice; // 安値
		public string LowPriceTime; // 安値時刻
		public double TradingVolume; // 売買高※株式・先物・オプション銘柄の場合のみ
		public string TradingVolumeTime; // 売買高時刻※株式・先物・オプション銘柄の場合のみ
		public double VWAP; // 売買高加重平均価格（VWAP）※株式・先物・オプション銘柄の場合のみ
		public double TradingValue; // 売買代金※株式・先物・オプション銘柄の場合のみ
		public double BidQty; // 最良売気配数量 ※①※株式・先物・オプション銘柄の場合のみ
		public double BidPrice; // 最良売気配値段 ※①※株式・先物・オプション銘柄の場合のみ
		public string BidTime; // 最良売気配時刻 ※①※株式銘柄の場合のみ
		public string BidSign; // 最良売気配フラグ ※①※株式・先物・オプション銘柄の場合のみ
		public double MarketOrderSellQty; // 売成行数量※株式銘柄の場合のみ
		public SellBuy Sell1; // 売気配数量1本目
		public SellBuy Sell2; // 売気配数量2本目
		public SellBuy Sell3; // 売気配数量3本目
		public SellBuy Sell4; // 売気配数量4本目
		public SellBuy Sell5; // 売気配数量5本目
		public SellBuy Sell6; // 売気配数量6本目
		public SellBuy Sell7; // 売気配数量7本目
		public SellBuy Sell8; // 売気配数量8本目
		public SellBuy Sell9; // 売気配数量9本目
		public SellBuy Sell10; // 売気配数量10本目
		public double AskQty; // 最良買気配数量 ※①※株式・先物・オプション銘柄の場合のみ
		public double AskPrice; // 最良買気配値段 ※①※株式・先物・オプション銘柄の場合のみ
		public string AskTime; // 最良買気配時刻 ※①※株式銘柄の場合のみ
		public string AskSign; // 最良買気配フラグ ※①※株式・先物・オプション銘柄の場合のみ
		public double MarketOrderBuyQty; // 買成行数量※株式銘柄の場合のみ
		public SellBuy Buy1; // 買気配数量1本目
		public SellBuy Buy2; // 買気配数量2本目
		public SellBuy Buy3; // 買気配数量3本目
		public SellBuy Buy4; // 買気配数量4本目
		public SellBuy Buy5; // 買気配数量5本目
		public SellBuy Buy6; // 買気配数量6本目
		public SellBuy Buy7; // 買気配数量7本目
		public SellBuy Buy8; // 買気配数量8本目
		public SellBuy Buy9; // 買気配数量9本目
		public SellBuy Buy10; // 買気配数量10本目
		public double OverSellQty; // OVER気配数量※株式銘柄の場合のみ
		public double UnderBuyQty; // UNDER気配数量※株式銘柄の場合のみ
		public double TotalMarketValue; //  時価総額※株式銘柄の場合のみ
		public double ClearingPrice; // 清算値※先物銘柄の場合のみ
		public double IV; // インプライド・ボラティリティ※オプション銘柄かつ日通しの場合のみ
		public double Gamma; // ガンマ※オプション銘柄かつ日通しの場合のみ
		public double Theta; // セータ※オプション銘柄かつ日通しの場合のみ
		public double Vega; // ベガ※オプション銘柄かつ日通しの場合のみ
		public double Delta; // デルタ※オプション銘柄かつ日通しの場合のみ
		public int SecurityType; // 銘柄種別

	}
	public class SellBuy
	{
		public string Time; // 時刻※株式銘柄の場合のみ
		public string Sign; // 気配フラグ※株式・先物・オプション銘柄の場合のみ
		public double Price; // 値段※株式・先物・オプション銘柄の場合のみ
		public double Qty; // 数量※株式・先物・オプション銘柄の場合のみ
	}
	// 銘柄情報
	public class ResponseSymbol
	{
		public string Symbol; // 銘柄コード
		public string SymbolName; // 銘柄名
		public string DisplayName; // 銘柄略称※株式・先物・オプション銘柄の場合のみ
		public int Exchange; // 市場コード※株式・先物・オプション銘柄の場合のみ
		public string ExchangeName; // 市場名称※株式・先物・オプション銘柄の場合のみ
		public string BisCategory; // 業種コード名※株式銘柄の場合のみ
		public double TotalMarketValue; // 時価総額※株式銘柄の場合のみ 追加情報出力フラグ：falseの場合、null
		public double TotalStocks; // 発行済み株式数（千株）※株式銘柄の場合のみ追加情報出力フラグ：falseの場合、null
		public double TradingUnit; // 売買単位※株式・先物・オプション銘柄の場合のみ
		public int FiscalYearEndBasic; // 決算期日※株式銘柄の場合のみ 追加情報出力フラグ：falseの場合、null
		public string PriceRangeGroup; // 呼値グループ※株式・先物・オプション銘柄の場合のみ※各呼値コードが対応する商品は以下となります。
		public bool KCMarginBuy; // 一般信用買建フラグ※trueのとき、一般信用(長期) または一般信用(デイトレ) が買建可能※株式銘柄の場合のみ
		public bool KCMarginSell; // 一般信用売建フラグ※trueのとき、一般信用(長期) または一般信用(デイトレ) が売建可能※株式銘柄の場合のみ
		public bool MarginBuy; // 制度信用買建フラグ※trueのとき制度信用買建可能※株式銘柄の場合のみ
		public bool MarginSell; // 制度信用売建フラグ※trueのとき制度信用売建可能※株式銘柄の場合のみ
		public double UpperLimit; // 値幅上限※株式・先物・オプション銘柄の場合のみ
		public double LowerLimit; // 値幅下限※株式・先物・オプション銘柄の場合のみ
	}
	// 注文約定照会
	public class ResponseOrders
	{
		public string ID; // 注文番号
		/* 1:待機（発注待機）, 2:処理中（発注送信中）,3:処理済（発注済・訂正済）,4:訂正取消送信中, 5:終了（発注エラー・取消済・全約定・失効・期限切れ）*/
		public int State; // 状態※OrderStateと同一である
		public int OrderState; // 注文状態※Stateと同一である
		public int OrdType; // 執行条件
		public string RecvTime; // 受注日時
		public string Symbol; // 銘柄コード
		public string SymbolName; // 銘柄名
		public int Exchange; // 市場コード
		public string ExchangeName; // 市場名
		public int TimeInForce; // 有効期間条件※先物・オプション銘柄の場合のみ
		public double Price; // 値段
		public double OrderQty; // 発注数量※注文期限切れと失効の場合、OrderQtyはゼロになりません。※期限切れと失効の確認方法としては、DetailsのRecType（3: 期限切れ、7: 失効）にてご確認ください。
		public double CumQty; // 約定数量
		public string Side; // 売買区分(1:売,2:買)
		public int CashMargin; // 取引区分
		public int AccountType; // 口座種別
		public int DelivType; // 受渡区分
		public int ExpireDay; // 注文有効期限yyyyMMdd形式
		public int MarginTradeType; // 信用取引区分※信用を注文した際に表示されます。
		public double MarginPremium; // プレミアム料※（注文中数量＋約定済数量）×１株あたりプレミアム料として計算されます。
									 //※信用を注文した際に表示されます。※制度信用売/買、一般（長期）買、一般（デイトレ）買の場合は、Noneと返されます。
									 //一般（長期）売、一般（デイトレ）売の場合は、プレミアム料= 0の場合、0（ゼロ）と返されます。
		public object[] Details; // 注文詳細

	}
	// 残高照会
	public class ResponsePositions
	{
		public string ExecutionID; // 約定番号※現物取引では、nullが返ります。
		public int AccountType; // 口座種別
		public string Symbol; // 銘柄コード
		public string SymbolName; // 銘柄名
		public int Exchange; // 市場コード
		public string ExchangeName; // 市場名
		public int SecurityType; // 銘柄種別※先物・オプション銘柄の場合のみ
		public int ExecutionDay; // 約定日（建玉日）※信用・先物・オプションの場合のみ※現物取引では、nullが返ります。
		public double Price; // 値段
		public double LeavesQty; // 残数量（保有数量）
		public double HoldQty; // 拘束数量（返済のために拘束されている数量）
		public string Side; // 売買区分
		public double Expenses; // 諸経費※信用・先物・オプションの場合のみ
		public double Commission; // 手数料※信用・先物・オプションの場合のみ
		public double CommissionTax; // 手数料消費税※信用・先物・オプションの場合のみ
		public int ExpireDay; // 返済期日※信用・先物・オプションの場合のみ
		public int MarginTradeType; // 信用取引区分※信用の場合のみ
		public double CurrentPrice; // 現在値 追加情報出力フラグ：falseの場合、null
		public double Valuation; // 評価金額 追加情報出力フラグ：falseの場合、null
		public double ProfitLoss; // 評価損益額 追加情報出力フラグ：falseの場合、null
		public double ProfitLossRate; // 評価損益率 追加情報出力フラグ：falseの場合、null おそらくマイナスのパーセントで表してると思われる
	}
	// 登録銘柄
	public class ResponseRegister
	{
		public RegistInfo[] RegistList; // 現在登録されている銘柄のリスト
	}
	public class RegistInfo
	{
		public string Symbol; // 銘柄コード
		public int Exchange; // 市場コード
	}
	// 詳細ランキング
	public class ResponseRanking {
		public int Type; // 種別
		public string ExchangeDivision; // 市場
		public RankingInfo[] Ranking; // ランキング
	}
	public class RankingInfo{
		public int No; // 順位 ※ランキング内で同じ順位が返却される場合があります（10位が2件など）
		public string Trend; // トレンド
		/*
			定義値 内容
			0	対象データ無し
			1	過去10営業日より20位以上上昇
			2	過去10営業日より1～19位上昇
			3	過去10営業日と変わらず
			4	過去10営業日より1～19位下落
			5	過去10営業日より20位以上下落
		*/
		public double AverageRanking; // 平均順位 ※100位以下は「999」となります
		public string Symbol; // 銘柄コード
		public string SymbolName; // 銘柄名称
		public double CurrentPrice; // 現在値
		public double ChangeRatio; // 前日比
		public double ChangePercentage; // 騰落率（%）
		public string CurrentPriceTime; // 時刻 HH:mm ※日付は返しません
		public double TradingVolume; // 売買高 売買高を千株単位で表示する ※百株の位を四捨五入
		public double Turnover; // 売買代金 売買代金を百万円単位で表示する ※十万円の位を四捨五入
		public string ExchangeName; // 市場名
		public string CategoryName; // 業種名
	}

	public class ClosePosition
	{
		public ClosePosition(string HoldID = "", int Qty = 0)
		{
			this.HoldID = HoldID;
			this.Qty = Qty;
		}
		public string HoldID; // 返済建玉ID
		public int Qty; // 返済建玉数量
	}


}
