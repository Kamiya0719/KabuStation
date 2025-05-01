using System;
using Newtonsoft.Json;
using System.Net.Http;

namespace CSharp_sample
{
    class TestExec
    {
        static void Main(string[] args)
        {
            string product = "4";
            string id = "";
            string updTime = "";
            string details = "";
            string symbol = "";
            string state = "";
            string side = "";
            string cashMargin = "";

            var token = GenerateToken.GetToken();

            var builder = new UriBuilder("http://localhost:18080/kabusapi/orders");
            var param = System.Web.HttpUtility.ParseQueryString(builder.Query);
            if (!string.IsNullOrEmpty(product))
            {
                param["product"] = product;
            }
            if (!string.IsNullOrEmpty(id))
            {
                param["id"] = id;
            }
            if (!string.IsNullOrEmpty(updTime))
            {
                param["updtime"] = updTime;
            }
            if (!string.IsNullOrEmpty(details))
            {
                param["details"] = details;
            }
            if (!string.IsNullOrEmpty(symbol))
            {
                param["symbol"] = symbol;
            }
            if (!string.IsNullOrEmpty(state))
            {
                param["state"] = state;
            }
            if (!string.IsNullOrEmpty(side))
            {
                param["side"] = side;
            }
            if (!string.IsNullOrEmpty(cashMargin))
            {
                param["cashmargin"] = cashMargin;
            }
            builder.Query = param.ToString();

            string url = builder.ToString();
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-API-KEY", token);
                HttpResponseMessage response = client.SendAsync(request).Result;
                Console.WriteLine("{0} \n {1}", JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result), response.Headers);

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("{0} {1}", e, e.Message);
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} {1}", ex, ex.Message);
                Console.ReadKey();
            }

            Console.ReadKey();
        }



		/*
		private static void TestCodeDaily()
		{

			TestCodeDailyOne();
		}
		private static void TestCodeDailyOne(int timeIdx)
		{
			string Symbol = "99999";
			int yobine = 10;
			int TradingUnit = 100;
			double lastEndPrice = 1000;
			CodeDaily a = new CodeDaily(Symbol, 1, yobine, TradingUnit, lastEndPrice);

			// 開始所持数(0 or たくさん)と理想売値段と所持日数(0 or 42日=>終売確定)
			int qty = 0; double sellPrice = lastEndPrice * 1.04; int havePeriod = 0;
			a.SetBeforePoss(qty, sellPrice, havePeriod);
			a.SetBasebuyNum();
			a.SetBeforeSell(true); // 売り注文用に個数と値段を内部で一時セット

			// 日経平均スコアによって損切基準は変動0-4; 時間によって値段基準変動6-2; 前日比(9999 or 1.025,15,1,5,0,)
			int jScore = 1; double ChangePreviousClose = 9999;

			// 
			List<ResponsePositions> positions = new List<ResponsePositions>();
			ResponsePositions position = new ResponsePositions();
			position.CurrentPrice = lastEndPrice * 0.95; // 前日値に対しての現在値が小さければ損切
			position.ProfitLossRate = 4.0; // 損失がでかければ損切
			positions.Add(position);
			a.SetIsEndSell(positions, jScore, ChangePreviousClose);

			bool isBuy = true;

			// 注文を設定
			List<CodeResOrder> orders = new List<CodeResOrder>();
			ResponseOrders resOrder = new ResponseOrders();
			resOrder.Symbol = Symbol;
			resOrder.State = 3;
			resOrder.ID = "abc"; // cancelordersで受け取るだけだし適当で
			resOrder.Side = isBuy ? "2" : "1"; // 買 or 売
			resOrder.CumQty = 100;
			resOrder.Price = lastEndPrice * 1.1; // 0.5％差があるとキャンセル
												 // trueだと開始数扱い falseなら開始数0扱い
			CodeResOrder order = new CodeResOrder(resOrder, true);
			orders.Add(order);
			a.SetOrders(orders, jScore);

			// 板情報見て終値売買値段決め
			ResponseBoard board = new ResponseBoard();
			board.TradingVolume = TradingUnit * 100;// 売買高が少なすぎると30に調整
			board.LowPrice = lastEndPrice; // timeIdx=6とかは安値高値が重要
			board.HighPrice = lastEndPrice;
			SellBuy[] list = isBuy ? new SellBuy[11]{
				board.Sell1,board.Buy1,board.Buy2,board.Buy3,board.Buy4,board.Buy5,board.Buy6,board.Buy7,board.Buy8,board.Buy9,board.Buy10,
			} : new SellBuy[11] {
				board.Buy1,board.Sell1,board.Sell2,board.Sell3,board.Sell4,board.Sell5,board.Sell6,board.Sell7,board.Sell8,board.Sell9,board.Sell10,
			};
			for (int i = 0; i <= 10; i++) {
				list[i] = new SellBuy();
				list[i].Price = lastEndPrice + 30 + (isBuy ? -i : i); // 2,3あたりの数が少なく数値の差が大きいときはどうのこうの
				list[i].Qty = 100; // 出来高の10分の1が一つの基準
			}
			a.SetBoard(board, timeIdx);

			// 結果確認用
			a.IsBoardCheck();
			a.CancelOrderIds();
			a.BuyOrderNeed();
			a.SellOrderNeed();
			a.BuyPrice();
			a.SellPrice();
		}
		*/

	}
}
