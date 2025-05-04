using System.Collections.Generic;

namespace CSharp_sample
{
	class Def
	{

		// トランプモード todo 理想売りはhalfにするかな 損切はオーバーアップ時のみ(全売) 購入はオーバーダウン時のみ？
		public const bool TranpMode = false;
		public const bool SubTranpMode = true;
		public const int JScoreOverUp = 10;
		public const int JScoreOverDown = 20;
		public const int JScoreNotGet = -10;
		// オーバーダウン(5％減少+α)とオーバーアップ(4％上昇)についての定義 
		// オーバーダウン時のCodeDaily情報入手(13時50分とかに一度だけ？フラグで管理するか) 既にあったら上書き、ないならnew


		// todo Sp系の基準購入費ベタ打ち
		public const int SpBuyBasePricew = 300000;
		public static readonly Dictionary<string, int> SpBuyInfo = new Dictionary<string, int>() {
			{ "1435", 0 },
			{ "2193", 0 },
			{ "2930", 137 },
			//{ "3071", 99 }, 制限
			{ "3656", 121 },
			{ "4571", 0 },
			{ "4586", 64 },
			{ "4591", 0 },
			//{ "4596", 46 }, 制限
			{ "4829", 107 },
			{ "4833", 0 },
			{ "4883", 0 },
			{ "6046", 0 },
			{ "6048", 0 },
			{ "6054", 0 },
			{ "6072", 0 },
			{ "6093", 129 },
			{ "6538", 0 },
			{ "6740", 0 },
			{ "6786", 0 },
			{ "7256", 89 },
			{ "8202", 162 },
			{ "8946", 83 },
		};


		public const int TypePro = 1;
		public const int TypeSp = 2;

		// 最大購入額55万をとりあえず真の上限
		public const double BuyMax = 1.1;
		// 最低購入注文金額(前日半端に買ったときとか)
		public const int BuyLowestPrice = 100000;

		// JScoreに応じて購入数に倍率をかける 4なら買わない
		public static readonly double[] BuyJScoreRatio = new double[5] {
			1.0, 0.7, 0.5, 0.3, 0
		};
		// 損切ライン(日経平均スコア,{損失%,前日比-%})
		public static readonly double[,] LossCutRatio = new double[5, 2] {
			//{6, 3.5},{3.5, 1.5},{3, 1},{2, 0.5},{0.5, 0},
			{6, 3.5},{3.5, 1.5},{3, 1},{2, 0.5},{0.5, 0},
		};
		// todo
		public static readonly double[,] LossCutRatioHalf = new double[5, 2] {
			{5, 2.5},{2.5, 1},{2, 0.5},{1, 0},{0, 0},
		};

		// 注文金額が変わったとき、キャンセルして注文しなおすかの差の割合
		public const double CancelDiff = 1.003;
		public const double CancelDiffNum = 1.1;



		// 一日の一銘柄あたりの上限(保証金に対する割合) 800万*0.1=80
		public const double BuyMaxPriceCode = 0.115;
		// 一日の購入上限(保証金に対する割合) 800万*0.3=240
		public const double BuyMaxPriceDay = 0.33;
		// 一日の購入上限(保証金に対する割合)でも所持がこれ以下ならこれに補正 800万*0.43=320
		public const double BuyMaxPriceDaySub = 0.45;
		// 一週間の購入上限
		public const double BuyMaxPriceWeek = 0.75;
		// 一週間の購入上限でも所持がこれ以下ならこれに補正(800万*0.8=640万)
		public const double BuyMaxPriceWeekSub = 0.8;
		// 全体の購入上限
		public const double BuyMaxPriceAll = 2.15;
		// 理想売り倍率
		public static readonly Dictionary<int, double> idealSellRatio = new Dictionary<int, double>() {
			{ 30, 1.02 }, { 20, 1.03 }, { 10, 1.04 }, { 5, 1.07 }
		};
		public static readonly Dictionary<int, double> idealSellRatioHalf = new Dictionary<int, double>() {
			{ 30, 1.01 }, { 20, 1.02 }, { 10, 1.03 }, { 5, 1.04 }
		};


	}
}
