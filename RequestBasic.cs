using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace CSharp_sample
{
	public enum REQUEST_TYPE
	{
		TOKEN, // トークン取得
		SENDORDER, // 注文 売りと買いにわける？ レスポンスはなしかな？
		CANCELORDER, // 注文取消 レスポンスはなしかな？
		WALLET, // 取引余力(信用)
		BOARD, // 時価情報・板情報
		SYMBOL, // 銘柄情報
		ORDERS, // 注文約定照会(これパラメータなしだと全部もってくるんかね？)
		POSITIONS, // 残高照会
		REGISTER, // 銘柄登録 うーん機能しょぼいからいらんかなー 市場コードの確認用に使うかな
		UNREGISTERALL, // 銘柄登録全解除
		RANKING, // 詳細ランキング
	}
	class RequestBasic
	{

		// リクエストテストか否か
		public static readonly bool isTest = false;

		private const int RequestTime = 100; // 0.2秒
		private const int OrderTime = 200; // 0.2秒
		private const int FailedTime = 3000; // 3秒
		private const int RepeatNum = 3; // 失敗時3回まで繰り返す

		static void Main(string[] args)
		{
			TestRequest();
		}

		public static void TestRequest()
		{
			REQUEST_TYPE type = REQUEST_TYPE.WALLET;
			RequestParam param = new RequestParam(type);
			if (type == REQUEST_TYPE.CANCELORDER) {
				param.SetOrderCancel("1");
			} else if (type == REQUEST_TYPE.SENDORDER || type == REQUEST_TYPE.BOARD || type == REQUEST_TYPE.SYMBOL || type == REQUEST_TYPE.REGISTER) {
				param.SetSymbol(2153, 1);
				if (type == REQUEST_TYPE.SENDORDER) param.SetOrder(false, 100, 1705, 0);
			}


			JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
			jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
			string result = Request(param);
			Common.DebugInfo(result);
			// タイプごとにレスポンスクラスに変換
			if (type == REQUEST_TYPE.TOKEN) {
				ResponseToken JsonResult = JsonConvert.DeserializeObject<ResponseToken>(result, jsonSerializerSettings);
				Common.DebugInfo("Request", JsonResult.token);
			} else if (type == REQUEST_TYPE.SENDORDER || type == REQUEST_TYPE.CANCELORDER) {
				ResponseOrder JsonResult = JsonConvert.DeserializeObject<ResponseOrder>(result, jsonSerializerSettings);
				Common.DebugInfo("Request", JsonResult.Result);
			} else if (type == REQUEST_TYPE.WALLET) {
				ResponseWallet JsonResult = JsonConvert.DeserializeObject<ResponseWallet>(result, jsonSerializerSettings);
				Common.DebugInfo("Request", JsonResult.MarginAccountWallet);
			} else if (type == REQUEST_TYPE.BOARD) {
				ResponseBoard JsonResult = JsonConvert.DeserializeObject<ResponseBoard>(result, jsonSerializerSettings);
				Common.DebugInfo("Request", JsonResult.Symbol);
			} else if (type == REQUEST_TYPE.SYMBOL) {
				ResponseSymbol JsonResult = JsonConvert.DeserializeObject<ResponseSymbol>(result, jsonSerializerSettings);
				Common.DebugInfo("Request", JsonResult.DisplayName);
			} else if (type == REQUEST_TYPE.ORDERS) {
				ResponseOrders[] JsonResult = JsonConvert.DeserializeObject<ResponseOrders[]>(result, jsonSerializerSettings);
				if (JsonResult.Length > 0) {
					Common.DebugInfo("Request", JsonResult[0].State);
				}
			} else if (type == REQUEST_TYPE.POSITIONS) {
				ResponsePositions[] JsonResult = JsonConvert.DeserializeObject<ResponsePositions[]>(result, jsonSerializerSettings);
				if (JsonResult.Length > 0) {
					Common.DebugInfo("Request", JsonResult[0].ExecutionID);
				}
			}
		}



		// トークンは別枠かなー
		private static string Request(RequestParam param)
		{
			if (param.requestType == REQUEST_TYPE.SENDORDER || param.requestType == REQUEST_TYPE.CANCELORDER) {
				Thread.Sleep(OrderTime);
			} else {
				Thread.Sleep(RequestTime);
			}

			var client = new HttpClient();
			var request = new HttpRequestMessage(requestMethods[param.requestType], param.GetUrl());
			var requestBody = param.GetRequestBody();
			if (requestBody != null) {
				request.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
			}

			try {
				if (param.requestType != REQUEST_TYPE.TOKEN) {
					string token = GenerateToken.GetToken(param.isTest);
					request.Headers.Add("ContentType", "application/json"); // この2個はトークン取得だけいらんかな
					request.Headers.Add("X-API-KEY", token); // この2個はトークン取得だけいらんかな
				}

				HttpResponseMessage response = client.SendAsync(request).Result;
				string result = response.Content.ReadAsStringAsync().Result;
				CsvControll.Log("Request", param.DebugLog(), result, JsonConvert.SerializeObject(requestBody));

				// 不一致ならトークンを取り直す result = {"Code":4001009,"Message":"APIキー不一致"} 
				if (result.IndexOf("APIキー不一致") >= 0) {
					GenerateToken.GetToken(param.isTest, false);
					CsvControll.ErrorLog("TokenDel", result, param.requestType.ToString(), "");
				}

				return result;
			} catch (HttpRequestException e) {
				Common.DebugInfo("HttpRequestException", e, e.Message);
				Console.ReadKey(); // なんか入力待ちらしい
				return "";
			} catch (Exception ex) {
				Common.DebugInfo("Exception", ex, ex.Message);
				Console.ReadKey();
				return "";
			}
		}

		private static Dictionary<REQUEST_TYPE, HttpMethod> requestMethods = new Dictionary<REQUEST_TYPE, HttpMethod>() {
			{ REQUEST_TYPE.TOKEN, HttpMethod.Post }, { REQUEST_TYPE.SENDORDER, HttpMethod.Post },
			{ REQUEST_TYPE.CANCELORDER, HttpMethod.Put }, { REQUEST_TYPE.WALLET, HttpMethod.Get },
			{ REQUEST_TYPE.BOARD, HttpMethod.Get }, { REQUEST_TYPE.SYMBOL, HttpMethod.Get },
			{ REQUEST_TYPE.ORDERS, HttpMethod.Get }, { REQUEST_TYPE.POSITIONS, HttpMethod.Get },
			{ REQUEST_TYPE.REGISTER, HttpMethod.Put },{ REQUEST_TYPE.UNREGISTERALL, HttpMethod.Put },
			{ REQUEST_TYPE.RANKING, HttpMethod.Get },
		};



		//////////////////////////////////////////////////
		/// 特定タイプでレスポンス返す ///
		//////////////////////////////////////////////////

		private static JsonSerializerSettings jsonSerializerSettings = null;
		private static JsonSerializerSettings GetJSSetting()
		{
			if (jsonSerializerSettings != null) return jsonSerializerSettings;
			jsonSerializerSettings = new JsonSerializerSettings();
			jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
			return jsonSerializerSettings;
		}
		/** トークン取得 */
		public static ResponseToken RequestToken()
		{
			ResponseToken res = null;
			for (int i = 0; i < RepeatNum; i++) {
				string raw = Request(new RequestParam(REQUEST_TYPE.TOKEN));
				res = JsonConvert.DeserializeObject<ResponseToken>(raw, GetJSSetting());
				if (res != null && res.token != "") return res;
				Failed(raw, i);
			}
			return res;
		}
		/** 注文一覧照会 */
		public static ResponseOrders[] RequestOrders()
		{
			ResponseOrders[] res = null;
			for (int i = 0; i < RepeatNum; i++) {
				string raw = Request(new RequestParam(REQUEST_TYPE.ORDERS));
				res = JsonConvert.DeserializeObject<ResponseOrders[]>(raw, GetJSSetting());
				if (res != null) {
					bool isOk = true;
					foreach (ResponseOrders order in res) {
						// 個数はワンちゃん0あるか？
						if (order.ID == "") isOk = false;
					}
					if (isOk) return res;
				}
				Failed(raw, i);
			}
			return res;
		}
		/** 注文送信 todo 引数はCodeDailyにするか */
		public static ResponseOrder RequestSendOrder(CodeDaily codeDaily, bool isBuy)
		{
			int symbol = Int32.Parse(codeDaily.Symbol);

			int qty = isBuy ? codeDaily.BuyOrderNeed() : codeDaily.SellOrderNeed();
			if (qty <= 0) return null;

			int price = isBuy ? codeDaily.BuyPrice() : codeDaily.SellPrice();
			if (price <= 5) {
				CsvControll.ErrorLog("RequestSendOrder", symbol.ToString(), price.ToString(), isBuy.ToString());
				return null;
			}

			CsvControll.SymbolLog(codeDaily.Symbol, "RequestSendOrder", isBuy ? "Buy" : "Sell", price.ToString(), qty.ToString());

			// todo テスト用
			if (true && isBuy) {
				//CsvControll.ErrorLog("BuyTest", symbol.ToString(), price.ToString(), qty.ToString());
				//return null;
			}
			//if (true && !isBuy) {
			//	CsvControll.ErrorLog("SellTest", symbol.ToString(), price.ToString(), qty.ToString());
			//	return null;
			//}


			int exchange = codeDaily.Exchange;
			int expireDay = codeDaily.ExpireDay();
			ResponseOrder res = null;
			for (int i = 0; i < RepeatNum; i++) {
				RequestParam param = new RequestParam(REQUEST_TYPE.SENDORDER);
				param.SetSymbol(symbol, exchange);
				param.SetOrder(isBuy, qty, price, expireDay);
				string raw = Request(param);
				res = JsonConvert.DeserializeObject<ResponseOrder>(raw, GetJSSetting());
				if (res != null && res.Result == 0) return res;
				Failed(raw, i);
			}
			return res;
		}

		private static Dictionary<string, ResponseBoard> boards = null;
		/** 板情報取得 */
		public static ResponseBoard RequestBoard(int symbol, int exchange, bool isLocal = false)
		{
			if (isLocal) {
				if (boards == null) {
					DateTime now = DateTime.Now;
					boards = new Dictionary<string, ResponseBoard>();
					List<string[]> infos = CsvControll.GetBoard();
					if (infos.Count == 0) {
						CsvControll.SaveBoard(new List<string[]>() { new string[1] { now.ToString() } });
					} else {
						for (int i = 0; i < infos.Count; i++) {
							string[] info = infos[i];
							if (i == 0) {
								DateTime date = DateTime.Parse(info[0]);
								// 時間が今日ならスルー、そうでないならリセット
								if (Common.SameD(date, now)) {

								} else {
									CsvControll.SaveBoard(new List<string[]>() { new string[1] { now.ToString() } });
									break;
								}
							} else {
								ResponseBoard b = JsonConvert.DeserializeObject<ResponseBoard>(String.Join(",", info), GetJSSetting());
								boards[b.Symbol] = b;
							}
						}
					}
				}
				if (boards.ContainsKey(symbol.ToString())) return boards[symbol.ToString()];
			}

			ResponseBoard res = null;
			for (int i = 0; i < RepeatNum; i++) {

				RequestParam param = new RequestParam(REQUEST_TYPE.BOARD);
				param.SetSymbol(symbol, exchange);
				string raw = Request(param);

				res = JsonConvert.DeserializeObject<ResponseBoard>(raw, GetJSSetting());
				if (res != null && res.Symbol != "") {
					if (isLocal && res.CurrentPrice <= 0) return null;
					SetRegister(res.Symbol);
					if (isLocal) {
						boards[symbol.ToString()] = res;
						CsvControll.SaveBoard(new List<string[]>() { new string[1] { raw } }, true);
					}
					return res;
				}
				Failed(raw, i);
			}
			return res;
		}
		/** 銘柄情報取得 */
		public static ResponseSymbol RequestSymbol(int symbol, int exchange)
		{
			ResponseSymbol res = null;
			for (int i = 0; i < RepeatNum; i++) {
				RequestParam param = new RequestParam(REQUEST_TYPE.SYMBOL);
				param.SetSymbol(symbol, exchange);
				string raw = Request(param);
				res = JsonConvert.DeserializeObject<ResponseSymbol>(raw, GetJSSetting());
				if (res != null && res.Symbol != "" && res.TradingUnit > 0) {
					SetRegister(res.Symbol);
					return res;
				}
				Failed(raw, i);
			}
			return res;
		}
		/** 注文キャンセル */
		public static ResponseOrder RequestCancelOrder(string orderId)
		{
			ResponseOrder res = null;
			for (int i = 0; i < RepeatNum; i++) {
				RequestParam param = new RequestParam(REQUEST_TYPE.CANCELORDER);
				param.SetOrderCancel(orderId);
				string raw = Request(param);
				res = JsonConvert.DeserializeObject<ResponseOrder>(raw, GetJSSetting());
				if (res != null && res.Result == 0) return res;
				Failed(raw, i);
			}
			return res;
		}
		/** 保有銘柄取得 */
		public static ResponsePositions[] RequestPositions()
		{
			for (int i = 0; i < RepeatNum; i++) {
				string raw = Request(new RequestParam(REQUEST_TYPE.POSITIONS));
				// todo rawがapi値エラーならトークン取り直し
				ResponsePositions[] res = JsonConvert.DeserializeObject<ResponsePositions[]>(raw, GetJSSetting());
				if (res != null) {
					bool isOk = true;
					List<ResponsePositions> list = new List<ResponsePositions>();
					foreach (ResponsePositions pos in res) {
						if (pos.ExecutionID == "" || pos.Symbol == "" || pos.Price <= 0) isOk = false;
						if (pos.LeavesQty > 0) list.Add(pos);
					}
					if (isOk) return list.ToArray();
				}
				Failed(raw, i);
			}
			return new ResponsePositions[0];
		}
		/** 登録銘柄全解除 */
		public static void RequestUnregisterAll()
		{
			for (int i = 0; i < RepeatNum; i++) {
				string raw = Request(new RequestParam(REQUEST_TYPE.UNREGISTERALL));
				ResponseRegister res = JsonConvert.DeserializeObject<ResponseRegister>(raw, GetJSSetting());
				if (res != null && res.RegistList.Length == 0) return;
				Failed(raw, i);
			}
		}
		/** 信用新規注文可能額 */
		public static double RequestWallet()
		{
			ResponseWallet res = null;
			for (int i = 0; i < RepeatNum; i++) {
				string raw = Request(new RequestParam(REQUEST_TYPE.WALLET));
				res = JsonConvert.DeserializeObject<ResponseWallet>(raw, GetJSSetting());
				if (res != null && res.MarginAccountWallet != null && res.MarginAccountWallet > 1) return (double)res.MarginAccountWallet;
				Failed(raw, i);
			}
			return 0;
		}
		/** 詳細ランキング */
		public static RankingInfo[] RequestRanking()
		{
			ResponseRanking res = null;
			for (int i = 0; i < RepeatNum; i++) {
				string raw = Request(new RequestParam(REQUEST_TYPE.RANKING));
				res = JsonConvert.DeserializeObject<ResponseRanking>(raw, GetJSSetting());
				if (res != null && res.Ranking != null && res.Ranking.Length > 1) return res.Ranking;
				Failed(raw, i);
			}
			return new RankingInfo[0];
		}



		/** 失敗時処理 */
		private static void Failed(string raw, int i)
		{
			Thread.Sleep(FailedTime);
			CsvControll.ErrorLog("FailedReq", i.ToString(), raw, "");
		}
		/** 登録銘柄数のカウントとリセット */
		private static List<string> registerList = null;
		private static void SetRegister(string setSymbol)
		{
			if (registerList == null) {
				registerList = new List<string>();
				foreach (string symbol in CsvControll.GetRegisterInfo()) registerList.Add(symbol);
			}
			bool isSave = false;
			if (!registerList.Contains(setSymbol)) {
				registerList.Add(setSymbol);
				isSave = true;
			}
			// 40件超えたら
			if (registerList.Count >= 40) {
				RequestUnregisterAll();
				registerList.Clear();
				isSave = true;
			}
			if (isSave) CsvControll.SaveRegisterInfo(registerList.ToArray());
		}

	}


}
