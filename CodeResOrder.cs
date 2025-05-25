using System;

namespace CSharp_sample
{
	// 前日に注文照会から作り、新規は順次追加していく
	public class CodeResOrder
	{
		/// 固定ステータス ///
		public string ID; // 注文番号
		public string Symbol; // 銘柄コード
		private string Side; // 売買区分(1:売,2:買)
		private string RecvTime; // 受注日時
		public double OrderQty; // 発注数量※注文期限切れと失効の場合、OrderQtyはゼロになりません。※期限切れと失効の確認方法としては、DetailsのRecType（3: 期限切れ、7: 失効）にてご確認ください。
		public double Price; // 値段(注文は整数でしかやらないからintでいいかな？)
		private int CashMargin; // 取引区分(2:新規,3:返済)

		/// デイリーステータス(前日にセット) ///
		public int startCumQty; // 今日開始時点の約定数(startCumQty==CumQtyなら処理終了)

		/// 変動ステータス ///
		/* 1:待機（発注待機）, 2:処理中（発注送信中）,3:処理済（発注済・訂正済）,4:訂正取消送信中, 5:終了（発注エラー・取消済・全約定・失効・期限切れ）*/
		private int State; // 状態※OrderStateと同一である
		public double CumQty; // 約定数量

		// 注文データをコンストラクタとする(前日セット or 新規追加)
		public CodeResOrder(ResponseOrders order, bool isLastDay)
		{
			ID = order.ID;
			Symbol = order.Symbol;
			Side = order.Side;
			RecvTime = order.RecvTime;
			OrderQty = order.OrderQty;
			Price = order.Price;
			State = order.State;
			CumQty = order.CumQty;
			CashMargin = order.CashMargin;

			if (isLastDay) {
				startCumQty = (int)order.CumQty;
			} else {
				startCumQty = 0;
			}
		}
		// csvファイルデータをコンストラクタとする
		public CodeResOrder(string[] csvInfo)
		{
			ID = csvInfo[0];
			Symbol = csvInfo[1];
			Side = csvInfo[2];
			RecvTime = csvInfo[3];
			OrderQty = Double.Parse(csvInfo[4]);
			Price = Double.Parse(csvInfo[5]);
			startCumQty = Int32.Parse(csvInfo[6]);
			State = Int32.Parse(csvInfo[7]);
			CumQty = Double.Parse(csvInfo[8]);
			CashMargin = csvInfo.Length >= 10 ? Int32.Parse(csvInfo[9]) : 0; // todo
		}

		public string[] GetSaveInfo()
		{
			return new string[10]{
				ID, Symbol, Side, RecvTime,
				OrderQty.ToString(), Price.ToString(),
				startCumQty.ToString(), State.ToString(), CumQty.ToString(),
				CashMargin.ToString(),
			};
		}

		// 既存データがある場合のアップデート
		public void UpdateData(ResponseOrders order)
		{
			State = order.State;
			CumQty = order.CumQty;
		}

		// 現在有効な(終了してない)オーダー
		public bool IsValid() { return State == 1 || State == 2 || State == 3; }
		// 現在処理中
		public bool IsProcess() { return State == 2 || State == 4; }

		// 売(返済売/新規空売り)
		public bool IsSell() { return Side == "1"; }
		// 返済(返済売/返済空買い)
		public bool IsReturn() { return CashMargin == 3; }

		public DateTime GetRecvTime() { return DateTime.Parse(RecvTime); }
	}


}
