using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharp_sample
{
	class Condtions
	{
		private const int ExecType = 4;
		private const bool IsAllCheck = true;

		static void Main(string[] args)
		{
			int type = ExecType;
			if (type == 1) SaveJapanCond();
			if (type == 2) JapanCheck();
			if (type == 3) SaveBuyInfo();
			if (type == 4) BenefitSum();
			if (type == 5) SaveTrueJScoreIkichis(DateTime.Parse("2025/02/07")); // 指定日前日データを使って指定日のスコア作成後、指定日の偽データ入れて指定日翌日の暫定スコア作成
			if (type == 6) SaveJapanBaseScoreOneDay(DateTime.Parse("2025/01/14"));
		}


		/** 2500コード*2000日*数千パターンの51チェックを全て行って保存する */
		public static void SaveCond51All()
		{
			foreach (string symbol in CsvControll.GetCodeList()) {
				List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
				for (int diffDayIdx = 0; diffDayIdx < diffDayList.Length; diffDayIdx++) {
					for (int ratioIdx = 0; ratioIdx < ratioList.Length; ratioIdx++) {
						SaveCond51(symbol, codeInfo, diffDayIdx, ratioIdx);
					}
				}
				Common.DebugInfo("SaveCond51All", symbol);
			}
		}

		private static void SaveCond51(string symbol, List<string[]> codeInfo, int diffDayIdx, int ratioIdx)
		{
			int diffDay = diffDayList[diffDayIdx];
			double ratio = ratioList[ratioIdx];

			Dictionary<string, bool> list = new Dictionary<string, bool>();
			for (int i = codeInfo.Count - 1; i >= diffDay; i--) {
				list[codeInfo[i][0]] = Double.Parse(codeInfo[i - diffDay][4]) * ratio <= Double.Parse(codeInfo[i][4]);
			}

			List<string[]> saveData = new List<string[]>();
			for (int i = codeInfo.Count - 1; i >= 120; i--) {
				string[] saveRow = new string[periodCntList.GetLength(0) + 1];
				saveRow[0] = codeInfo[i][0]; // 日付
				for (int p = 0; p < periodCntList.GetLength(0); p++) {
					int cnt = periodCntList[p, 1];
					for (int j = 1; j <= periodCntList[p, 0]; j++) {
						if (list[codeInfo[i - j][0]]) cnt--;
						if (0 >= cnt) break;
					}
					saveRow[p + 1] = 0 >= cnt ? "1" : "0";
				}
				saveData.Add(saveRow);
			}

			// 保存
			CsvControll.SaveCond51All(saveData, symbol, diffDayIdx, ratioIdx);
		}



		/** 利益情報を全て保存する */
		public static void SaveBenefitAll()
		{
			foreach (string symbol in CsvControll.GetCodeList()) {
				SaveBenefit(symbol);
			}
		}

		private static void SaveBenefit(string symbol)
		{
			// 買って
			List<string[]> saveData = new List<string[]>();
			List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
			for (int i = 0; i < codeInfo.Count - 42; i++) {
				// 
				DateTime date = DateTime.Parse(codeInfo[i][0]);
				double buyPrice = Double.Parse(codeInfo[i][4]); // 終値で購入

				double sellPrice = Double.Parse(codeInfo[i + 42][4]);
				int sellPeriod = 0;
				for (int havePeriod = 1; havePeriod <= 42; havePeriod++) {
					sellPeriod = havePeriod;

					double sellRatio = 1.01;
					foreach (KeyValuePair<int, double> pair in Def.idealSellRatio) {
						if (havePeriod <= pair.Key) sellRatio = pair.Value;
					}
					// 理想売り
					if (Double.Parse(codeInfo[i + havePeriod][2]) >= buyPrice * sellRatio) {
						sellPrice = buyPrice * sellRatio;
						break;
					}
					// 損切 損失/前日比
					if (Double.Parse(codeInfo[i + havePeriod][3]) * 100 < buyPrice * (100 - Def.LossCutRatio[0, 0])
						|| Double.Parse(codeInfo[i + havePeriod][3]) * 100 < Double.Parse(codeInfo[i + havePeriod - 1][4]) * (100 - Def.LossCutRatio[0, 1])) {
						sellPrice = Double.Parse(codeInfo[i + havePeriod][4]);
						break;
					}
				}

				int benefit = (int)Math.Round((sellPrice / buyPrice - 1) * 100, MidpointRounding.AwayFromZero);
				//Common.DebugInfo("SaveBenefit", benefit, sellPeriod);
				saveData.Add(new string[3] { codeInfo[i][0], benefit.ToString(), sellPeriod.ToString() });
			}

			CsvControll.SaveBenefitAll(saveData, symbol);
		}


		private const bool IsAndCheck = true; // andチェックかorチェックか
		private const bool SkipMode = true; // 購入時に所持期間分日付をスキップすうかどうか
		private const int AllTrueCondIdx = 1;
		private static readonly int[] NotCond = new int[]{
			0,2,4,16,32,161,177,193,195,209,211,213,215,224,226,228,230,240,242,256,258,272,288,401,417,419,433,435,437,448,450,452,464,
			466,480,496,512,593,609,625,641,643,657,659,661,663,672,674,688,801,817,833,835,849,851,865,867,869,881,883,885,887,896,898,
			900,902,912,914,916,928,930,932,944,946,960,962,976,1073,1089,1105,1107,1120,1122,1124,1126,1136,1138,1152,1154,1168,1170,1184,
			1265,1281,1297,1299,1313,1315,1329,1331,1333,1335,1344,1346,1348,1360,1362,1376,1392,1408,1473,1489,1505,1507,1521,1523,1537,
			1539,1541,1553,1555,1557,1559,1568,1570,1584,1681,1697,1699,1713,1715,1729,1731,1733,1745,1747,1749,1761,1763,1765,1767,1777,
			1779,1781,1783,1792,1794,1796,1798,1808,1810,1812,1814,1824,1826,1828,1840,1842,1844,1856,1858,1872,1874,1969,1985,2001,2003,
			2016,2018,2020,2022,2032,2034,2036,2048,2050,2052,2064,2066,2080,2082,2096,2161,2177,2193,2209,2211,2225,2227,2229,2240,2242,
			2244,2246,2256,2258,2260,2272,2274,2288,2290,2304,2320,2369,2385,2401,2403,2417,2419,2433,2435,2437,2449,2451,2453,2455,2464,
			2466,2468,2480,2482,2496,2498,2512,2528,2577,2593,2609,2611,2625,2627,2629,2641,2643,2645,2657,2659,2661,2663,2673,2675,2677,
			2679,2688,2704,2801,2803,2817,2819,2833,2835,2837,2849,2851,2853,2865,2867,2869,2871,2881,2883,2885,2887,2897,2899,2901,2903,
			2905,2912,2914,2916,2918,2920,2928,2930,2932,2934,2936,2944,2946,2948,2950,2960,2962,2964,2966,2976,2978,2980,2992,2994,3008,
			3089,3105,3121,3136,3138,3140,3142,3144,3152,3154,3156,3158,3168,3170,3172,3174,3184,3186,3188,3190,3200,3202,3204,3216,3218,
			3232,3297,3313,3329,3331,3345,3347,3360,3362,3364,3366,3368,3376,3378,3380,3382,3392,3394,3396,3398,3408,3410,3412,3424,3426,
			3440,3442,3505,3521,3537,3539,3553,3555,3569,3571,3573,3584,3586,3588,3590,3600,3602,3604,3606,3616,3618,3620,3632,3634,3636,
			3648,3650,3664,3713,3729,3745,3747,3761,3763,3765,3777,3779,3781,3793,3795,3797,3799,3808,3810,3812,3814,3824,3826,3828,3840,
			3842,3844,3856,3858,3872,3874,3888,3921,3937,3953,3955,3969,3971,3973,3985,3987,3989,4001,4003,4005,4007,4017,4019,4021,4023,
			4032,4034,4036,4048,4050,4064,4066,4080,4096,4145,4147,4161,4163,4177,4179,4181,4193,4195,4197,4199,4209,4211,4213,4215,4225,
			4227,4229,4231,4241,4243,4245,4247,4249,4256,4369,4371,4373,4385,4387,4389,4391,4401,4403,4405,4407,4417,4419,4421,4423,4433,
			4435,4437,4439,4441,4449,4451,4453,4455,4457,4465,4467,4469,4471,4473,4475,4480,4482,4484,4486,4488,4490,4496,4498,4500,4502,
			4504,4512,4514,4516,4518,4520,4528,4530,4532,4534,4544,4546,4548,4550,4560,4562,4564,4566,4576,4578,4673,4689,4704,4706,4708,
			4710,4712,4714,4720,4722,4724,4726,4728,4736,4738,4740,4742,4744,4752,4754,4756,4758,4768,4770,4772,4774,4784,4786,4788,4800,
			4865,4881,4897,4899,4913,4915,4928,4930,4932,4934,4936,4944,4946,4948,4950,4952,4960,4962,4964,4966,4976,4978,4980,4982,4992,
			4994,4996,5008,5010,5024,5073,5089,5091,5105,5107,5121,5123,5125,5137,5139,5141,5152,5154,5156,5158,5160,5168,5170,5172,5174,
			5184,5186,5188,5190,5200,5202,5204,5216,5218,5220,5232,5234,5281,5297,5299,5313,5315,5329,5331,5333,5345,5347,5349,5351,5361,
			5363,5365,5367,5376,5378,5380,5382,5392,5394,5396,5398,5408,5410,5412,5424,5426,5428,5440,5442,5456,5489,5505,5507,5521,5523,
			5525,5537,5539,5541,5553,5555,5557,5559,5569,5571,5573,5575,5585,5587,5589,5591,5600,5602,5604,5606,5616,5618,5620,5632,5634,
			5636,5648,5650,5664,5680,5713,5715,5729,5731,5733,5745,5747,5749,5761,5763,5765,5767,5777,5779,5781,5783,5793,5795,5797,5799,
			5801,5809,5811,5813,5815,5817,5824,5826,5828,5840,5842,5856,5858,5872,5888,5937,5939,5941,5953,5955,5957,5969,5971,5973,5975,
			5985,5987,5989,5991,6001,6003,6005,6007,6009,6017,6019,6021,6023,6025,6033,6035,6037,6039,6041,6043,6048,6161,6163,6165,6167,
			6177,6179,6181,6183,6193,6195,6197,6199,6209,6211,6213,6215,6217,6225,6227,6229,6231,6233,6241,6243,6245,6247,6249,6251,6257,
			6259,6261,6263,6265,6267,6272,6274,6276,6278,6280,6282,6284,6288,6290,6292,6294,6296,6298,6304,6306,6308,6310,6312,6314,6320,
			6322,6324,6326,6328,6336,6338,6340,6342,6344,6352,6354,6356,6358,6368,6370,6372,6465,6481,6496,6498,6500,6502,6504,6506,6508,
			6512,6514,6516,6518,6520,6522,6528,6530,6532,6534,6536,6538,6544,6546,6548,6550,6552,6560,6562,6564,6566,6576,6578,6580,6582,
			6592,6594,6596,6657,6673,6689,6705,6707,6720,6722,6724,6726,6728,6730,6736,6738,6740,6742,6744,6746,6752,6754,6756,6758,6760,
			6768,6770,6772,6774,6776,6784,6786,6788,6790,6800,6802,6804,6806,6816,6818,6865,6881,6897,6899,6913,6915,6929,6931,6933,6944,
			6946,6948,6950,6952,6954,6960,6962,6964,6966,6968,6970,6976,6978,6980,6982,6984,6992,6994,6996,6998,7008,7010,7012,7014,7024,
			7026,7028,7040,7073,7089,7105,7107,7121,7123,7125,7137,7139,7141,7153,7155,7157,7159,7168,7170,7172,7174,7176,7178,7184,7186,
			7188,7190,7192,7200,7202,7204,7206,7208,7216,7218,7220,7222,7232,7234,7236,7238,7248,7250,7252,7264,7297,7313,7315,7329,7331,
			7333,7345,7347,7349,7361,7363,7365,7367,7377,7379,7381,7383,7392,7394,7396,7398,7400,7402,7408,7410,7412,7414,7416,7424,7426,
			7428,7430,7440,7442,7444,7446,7456,7458,7460,7472,7474,7505,7521,7523,7537,7539,7541,7553,7555,7557,7569,7571,7573,7575,7585,
			7587,7589,7591,7601,7603,7605,7607,7609,7616,7618,7620,7622,7624,7632,7634,7636,7638,7648,7650,7652,7654,7664,7666,7668,7680,
			7682,7684,7696,7729,7745,7747,7761,7763,7765,7777,7779,7781,7783,7793,7795,7797,7799,7809,7811,7813,7815,7817,7825,7827,7829,
			7831,7833,7840,7842,7844,7846,7848,7856,7858,7860,7862,7872,7874,7876,7878,7888,7890,7892,7904,7906,7920,7953,7955,7969,7971,
			7973,7985,7987,7989,7991,8001,8003,8005,8007,8017,8019,8021,8023,8033,8035,8037,8039,8041,8049,8051,8053,8055,8057,8059,8064,
			8066,8068,8070,8080,8082,8084,8096,8098,8112,8114,8128,8177,8179,8181,8193,8195,8197,8199,8209,8211,8213,8215,8225,8227,8229,
			8231,8233,8241,8243,8245,8247,8249,8257,8259,8261,8263,8265,8267,8273,8275,8277,8279,8281,8283,8288,8401,8403,8405,8407,8417,
			8419,8421,8423,8425,8433,8435,8437,8439,8441,8449,8451,8453,8455,8457,8459,8465,8467,8469,8471,8473,8475,8481,8483,8485,8487,
			8489,8491,8493,8497,8499,8501,8503,8505,8507,8509,
			// 追加分
			/*
			8443,645,6235,8025,8251,6269,8461,7351,7731,8285,1317,853,197,5593,8043,7593,
			8511,6201,7835,1109,1491,2595,7559,1249,7143,5819,2195,4251,1543,2213,385,7281,6027,7385,439,627,2387,5993,3123,871,8495,8217,
			6917,4459,5317,421,7801,7749,8061,5543,7611,8009,5143,8235,8269,5821,6045,7819,8287,8183,7767,1457,6047,6935,
			1888,7387,2821,4425,1525,6255,4253,2855,2907,7837,4479,5595,6449,6883,3731,7091,5751,6691,5206,4165,1091,3523,
			4025,3939,1785,3489,3575,3349,5369,2005,4027,5803,4233,889,199,6709,
			895,1791,2911,6253,2909,4255,5597,2231,2681,4917,1787,3557,5057,4461,7317,1561,5785,647,1111,665,7299,7975,4691,2889,5823,3991,217,2683,3281,2457,
			629,1751,5265,1319,3315,7109,2353,6029,2439,891,1337,3351,837,1789,2421,785,3801,3783,1717,179,3957,2687,7161,4183,7335,1987,5012,1301,5577,1563,
			2785,4353,6145,8385,8387,8389,6147,181,1093,4029,2613,5109,3830,3622,4883,671,4536,5371,5414,2007,7785,403,4919,2690,1567,1769,441,4849,855,2145,
			4355,7670,423,819,304,1800,3749,2685,611,5335,2459,6483,7577,2922,7543,6011,7698,1057,7957,6330,667,2179,6568,3428,8045,3125,4082,676,3592,4938,
			1775,2895,447,1343,8219,2891,219,1339,6237,2233,3803,5373,4235,3577,893,2463,2647,873,4443,5805,2665,5145,7595,5283,1565,2215,3333,3805,2461,7163,
			879,2823,2839,4375,1527,1113,1545,649,1735,443,201,3559,3697,1771,1989,1095,2873,5075,631,5127,2235,3507,4009,2423,2197,7525,7369,2893,1303,669,
			6149,6185,5977,8391,8201,1341,221,5491,3579,5509,8253,6031,577,4272,1509,1321,3127,8100,5527,2996,6849,6711,4237,4693,5301,2982,2405,6901,6732,2441,
			7993,2237,3581,4217,5147,3353,4031,6675,244,3541,405,5579,2371,7000,4506,3107,6762,4802,1378,445,7127,5666,18,2009,3860,1075,704,4409,1773,1115,
			1759,2879,4447,6239,655,4445,1551,1119,8011,1753,
			4431,6223,8463,2671,6013,2239,5581,6221,4239,1327,5149,431,3975,7803,6937,2667,5769,875,5353,7751,8411,8271,5995,5735,2631,7821,4901,2669,3785,4011,
			948,964,1741,1757,1860,1876,2637,2653,2861,2877,3757,3773,3789,3981,3997,4013,4205,4221,4429,5117,5133,5341,5357,5549,5565,5773,5789,5997,8013,8029,
			*/

		};
		private static readonly int[] ConfirmAnds = new int[] {
			1682,8408,5958,1282,6218,4999,8476,4476,4656,8444,6270,8478,6202,8426,8026,7612,8428,4462,222,2804,1474,1664,2786,206,2806,4356,6168,6186,862,2862,8412,8430,
			6170, // 611143,-0.230667781517583,
			981, // 676318,-0.222154371168592,
			5324, // 732437,-0.215408287675254,
			5100, // 783284,-0.211427783537006,
			2830, // 937785,-0.203682080647483,
			2808, // 1056027,-0.197063143271905,
			126,
		};
		private static readonly int[] ConfirmOrs = new int[] {
			706,7404,528,5164,52,5388,2322,845,619,66,7196,1065,5983,8395,8167,6851,8375,
			
			4524, // SKIPMODE:T0:89100,T1:0.5823,T2:7.528
			/*
			1683,8409,5959,1283,6219,4998,8477,4477,4657,8445,6271,8479,6203,8427,8027,7613,8429,4463,223,2805,1475,1665,2787,207,2807,4357,6169,6187,863,2863,8413,8431,
			6171, // 611143,-0.230667781517583,
			980, // 676318,-0.222154371168592,
			5325, // 732437,-0.215408287675254,
			5101, // 783284,-0.211427783537006,
			2831, // 937785,-0.203682080647483,
			2809, // 1056027,-0.197063143271905,
			*/
		};
		private static readonly int[] KouhoAnds = new int[] {
8376,
6844,
4606,
6142,
110,
3038,
7068,
4830,

		};
		private static readonly int[] KouhoOrs = new int[] {

		};
		private const int AllCond51Num = 3754886; // 2000日*2500銘柄
		private const double AllCond51Ratio = -0.000912;
		/** 51条件の全検証 */
		public static void CheckCond51All()
		{
			bool isOrOkForce = IsAndCheck && ConfirmOrs.Length == 0 && KouhoOrs.Length == 0; // orチェックを強制でOKにしておく

			// 確定条件と候補条件について コード*日付分の情報を保存 andは一個でもfalseならそいつはアウト symbol=>[日付1,...]でfalseを保存
			Dictionary<string, HashSet<string>> beforeNotAnd = new Dictionary<string, HashSet<string>>();
			// beforeNotAndがfalseのものはスルー orは一個でもtrueならそいつはOK
			Dictionary<string, HashSet<string>> beforeOr = new Dictionary<string, HashSet<string>>();
			// beforeNotAndがfalseのものはスルー
			Dictionary<string, HashSet<string>>[] beforeNotAndKouho = new Dictionary<string, HashSet<string>>[KouhoAnds.Length];
			// beforeNotAndがfalseのものはスルー beforeOrがtrueのものはスルー
			Dictionary<string, HashSet<string>>[] beforeOrKouho = new Dictionary<string, HashSet<string>>[KouhoOrs.Length];
			foreach (string symbol in CsvControll.GetCodeList()) {
				beforeNotAnd[symbol] = new HashSet<string>();
				foreach (int condIdx in ConfirmAnds) {
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(condIdx);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if ((cond51[pIdx + 1] == "1") != isT) beforeNotAnd[symbol].Add(cond51[0]);
					}
				}
				beforeOr[symbol] = new HashSet<string>();
				foreach (int condIdx in ConfirmOrs) {
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(condIdx);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
						if ((cond51[pIdx + 1] == "1") == isT) beforeOr[symbol].Add(cond51[0]);
					}
				}

				for (int i = 0; i < KouhoAnds.Length; i++) {
					if (beforeNotAndKouho[i] == null) beforeNotAndKouho[i] = new Dictionary<string, HashSet<string>>();
					beforeNotAndKouho[i][symbol] = new HashSet<string>();
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(KouhoAnds[i]);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
						if ((cond51[pIdx + 1] == "1") != isT) beforeNotAndKouho[i][symbol].Add(cond51[0]);
					}
				}
				for (int i = 0; i < KouhoOrs.Length; i++) {
					if (beforeOrKouho[i] == null) beforeOrKouho[i] = new Dictionary<string, HashSet<string>>();
					beforeOrKouho[i][symbol] = new HashSet<string>();
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(KouhoOrs[i]);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
						if (beforeOr[symbol].Contains(cond51[0])) continue;
						if ((cond51[pIdx + 1] == "1") == isT) beforeOrKouho[i][symbol].Add(cond51[0]);
					}
				}
			}

			int kouhoNum = KouhoAnds.Length > 0 ? KouhoAnds.Length : KouhoOrs.Length;
			int[] kouhoList = KouhoAnds.Length > 0 ? KouhoAnds : KouhoOrs;


			int[,] benefitAll = new int[kouhoNum, condNum()];
			int[,] havePeriodAll = new int[kouhoNum, condNum()];
			int[,] trueAll = new int[kouhoNum, condNum()];

			List<string> codeList = CsvControll.GetCodeList();

			if (false) {
				codeList = new List<string>();
				int aaa = 0;
				foreach (string code in CsvControll.GetCodeList()) {
					codeList.Add(code); aaa++;
					if (aaa >= 30) break;
				}
			}


			foreach (string symbol in codeList) {

				/*
				int[,,] benefitAllPs = new int[kouhoNum, condNum(), codeList.Count];
				int[,,] havePeriodAllPs = new int[kouhoNum, condNum(), codeList.Count];
				int[,,] trueAllPs = new int[kouhoNum, condNum(), codeList.Count];
				ParallelOptions parallelOptions = new ParallelOptions();
				parallelOptions.MaxDegreeOfParallelism = 2;
				Parallel.For(0, codeList.Count, parallelOptions, p => {
					string symbol = codeList[p];
					int[,] benefitAllP = new int[kouhoNum, condNum()];
					int[,] havePeriodAllP = new int[kouhoNum, condNum()];
					int[,] trueAllP = new int[kouhoNum, condNum()];

					*/

				// todo こいつらはstaticに持っておくか？
				Dictionary<string, int> benefits = new Dictionary<string, int>();
				Dictionary<string, int> havePeriods = new Dictionary<string, int>();
				foreach (string[] benefitInfo in CsvControll.GetBenefitAll(symbol)) {
					benefits[benefitInfo[0]] = Int32.Parse(benefitInfo[1]);
					havePeriods[benefitInfo[0]] = Int32.Parse(benefitInfo[2]);
				}

				for (int diffDayIdx = 0; diffDayIdx < diffDayList.Length; diffDayIdx++) {
					for (int ratioIdx = 0; ratioIdx < ratioList.Length; ratioIdx++) {
						List<string[]> cond51All = CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx);
						for (int pIdx = 0; pIdx < periodCntList.GetLength(0); pIdx++) {
							foreach (bool isT in new bool[2] { true, false }) {
								int condIdx = GetCondIdx(pIdx, ratioIdx, diffDayIdx, isT);
								if (Array.IndexOf(NotCond, condIdx) >= 0 || Array.IndexOf(ConfirmOrs, condIdx) >= 0 || Array.IndexOf(KouhoOrs, condIdx) >= 0) continue;
								int[] nowHaves = new int[kouhoNum];
								for (int c = 0; c< cond51All.Count; c++ ) {
									string[] cond51  = cond51All[c];

									if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
									if (IsAndCheck && (cond51[pIdx + 1] == "1") != isT) continue;
									if (!benefits.ContainsKey(cond51[0])) continue;
									bool isOrCheck = isOrOkForce || (!IsAndCheck && (cond51[pIdx + 1] == "1") == isT) || beforeOr[symbol].Contains(cond51[0]);

									if (KouhoAnds.Length > 0) {
										if (!isOrCheck) continue;
										for (int i = 0; i < KouhoAnds.Length; i++) {
											if (beforeNotAndKouho[i][symbol].Contains(cond51[0])) continue;
											if(SkipMode && nowHaves[i] > c) continue;

											benefitAll[i, condIdx] += benefits[cond51[0]];
											havePeriodAll[i, condIdx] += havePeriods[cond51[0]];
											trueAll[i, condIdx]++;

											if (SkipMode) nowHaves[i] = c + havePeriods[cond51[0]] + 1;
										}
									} else if (KouhoOrs.Length > 0) {
										// 残るはORチェック
										for (int i = 0; i < KouhoOrs.Length; i++) {
											if (!isOrCheck && !beforeOrKouho[i][symbol].Contains(cond51[0])) continue;
											if (SkipMode && nowHaves[i] > c) continue;
											benefitAll[i, condIdx] += benefits[cond51[0]];
											havePeriodAll[i, condIdx] += havePeriods[cond51[0]];
											trueAll[i, condIdx]++;

											if (SkipMode) nowHaves[i] = c + havePeriods[cond51[0]] + 1;
										}
									} else {

									}
								}
							}
						}
					}
				}

				Common.DebugInfo("CheckCond51AllSymbol", symbol);

				/*
				for (int i = 0; i < kouhoNum; i++) {
					for (int j = 0; j < condNum(); j++) {
						benefitAllPs[i, j, p] = benefitAllP[i, j];
						havePeriodAllPs[i, j, p] = havePeriodAllP[i, j];
						trueAllPs[i, j, p] = trueAllP[i, j];
					}
				}

				Console.WriteLine("CheckCond51AllSymbol:" + symbol + " End ");
			});

			for (int i = 0; i < kouhoNum; i++) {
				for (int j = 0; j < condNum(); j++) {
					for (int p = 0; p < codeList.Count; p++) {
						benefitAll[i, j] += benefitAllPs[i, j, p];
						havePeriodAll[i, j] += havePeriodAllPs[i, j, p];
						trueAll[i, j] += trueAllPs[i, j, p];
					}
				}
			}
			*/

			}



			// 並び変えるか
			int maxNum = 30;
			for (int i = 0; i < kouhoNum; i++) {
				Dictionary<int, double> benefitRes = new Dictionary<int, double>();
				Dictionary<int, double> havePeriodRes = new Dictionary<int, double>();
				int tMin = AllCond51Num; int tMax = 0;
				double maxBenefit = 0; double minBenefit = 9999;
				for (int j = 0; j < condNum(); j++) {
					if (trueAll[i, j] == 0) continue;
					benefitRes[j] = (double)benefitAll[i, j] / (double)trueAll[i, j];
					havePeriodRes[j] = (double)havePeriodAll[i, j] / (double)trueAll[i, j];

					tMin = Math.Min(tMin, trueAll[i, j]); tMax = Math.Max(tMax, trueAll[i, j]);
					maxBenefit = Math.Max(maxBenefit, benefitRes[j]); minBenefit = Math.Min(minBenefit, benefitRes[j]);
				}
				double needNum = tMax * 0.6;

				int max = maxNum;
				string result = "";
				string result2 = "";
				// OrderByDescending:高い順、OrderBy：低い順
				foreach (KeyValuePair<int, double> b in benefitRes.OrderByDescending(c => trueAll[i, c.Key] >= needNum ? c.Value : 0)) {
					if (max > 0) {
						double tr = (AllCond51Num * AllCond51Ratio - trueAll[i, b.Key] * b.Value) / (AllCond51Num - trueAll[i, b.Key]);
						result += "\nCond:" + b.Key + ", T:" + trueAll[i, b.Key] + ", TR:" + tr + ", Period:" + havePeriodRes[b.Key] + ", Benefit" + b.Value + ",";
						result2 += b.Key + ",";
					}
					max--;
				}
				Common.DebugInfo("LowScoreRank1", kouhoList[i], result, result2);

				
				result = "";
				result2 = "";
				max = 30;
				foreach (KeyValuePair<int, double> b in benefitRes.OrderByDescending(c =>
					//c.Value >= maxBenefit * 0.5 & trueAll[i, c.Key] >= needNum ?
					trueAll[i, c.Key] >= needNum ? (AllCond51Num * AllCond51Ratio - trueAll[i, c.Key] * c.Value) / (AllCond51Num - trueAll[i, c.Key]) : -999

				)) {
					if (max > 0) {
						double tr = (AllCond51Num * AllCond51Ratio - trueAll[i, b.Key] * b.Value) / (AllCond51Num - trueAll[i, b.Key]);
						result += "\nCond:" + b.Key + ", T:" + trueAll[i, b.Key] + ", TR:" + tr + ", Period:" + havePeriodRes[b.Key] + ", Benefit" + b.Value + ",";
						result2 += b.Key + ",";
					}
					max--;
				}
				Common.DebugInfo("LowScoreRank2", kouhoList[i], result,  result2);
				

				result = "";
				result2 = "";
				max = 30;
				foreach (KeyValuePair<int, double> b2 in benefitRes.OrderByDescending(c => c.Value >= maxBenefit * 0.85 && trueAll[i, c.Key] >= needNum ? trueAll[i, c.Key] : 0)) {
					if (max > 0) {
						double tr = (AllCond51Num * AllCond51Ratio - trueAll[i, b2.Key] * b2.Value) / (AllCond51Num - trueAll[i, b2.Key]);
						result += "\nCond:" + b2.Key + ", T:" + trueAll[i, b2.Key] + ", TR:" + tr + ", Period:" + havePeriodRes[b2.Key] + ", Benefit" + b2.Value + ",";
						result2 += b2.Key + ",";
					}
					max--;
				}
				Common.DebugInfo("LowScoreRank3", kouhoList[i], result, result2);
				result = "";
				result2 = "";
				max = 30;
				foreach (KeyValuePair<int, double> b in benefitRes.OrderBy(c => c.Value >= maxBenefit * 0.85 && trueAll[i, c.Key] >= needNum ? trueAll[i, c.Key] : 9999999)) {
					if (max > 0) {
						double tr = (AllCond51Num * AllCond51Ratio - trueAll[i, b.Key] * b.Value) / (AllCond51Num - trueAll[i, b.Key]);
						result += "\nCond:" + b.Key + ", T:" + trueAll[i, b.Key] + ", TR:" + tr + ", Period:" + havePeriodRes[b.Key] + ", Benefit" + b.Value + ",";
						result2 += b.Key + ",";
					}
					max--;
				}
				Common.DebugInfo("LowScoreRank4", kouhoList[i], result, result2);


				result = "";
				result2 = "";
				max = 30;
				foreach (KeyValuePair<int, double> benefitResB in benefitRes.OrderByDescending(c => c.Value)) {
					if (max > 0) {
						result += "\nCond:" + benefitResB.Key + ", T:" + trueAll[i, benefitResB.Key] + ", Period:" + havePeriodRes[benefitResB.Key] + ", Benefit:" + benefitResB.Value + ",";
						result2 += benefitResB.Key + ",";
					}
					max--;
				}
				//Common.DebugInfo("HighScoreRank", i, result, "AvB:" + avBenefit + ", AvT:" + avTrue + ", " + result2);
			}

		}

		private static int GetCondIdx(int pIdx, int ratioIdx, int diffDayIdx, bool isT)
		{
			return pIdx * ratioList.Length * diffDayList.Length * 2 + ratioIdx * diffDayList.Length * 2 + diffDayIdx * 2 + (isT ? 1 : 0);
		}
		private static (int, int, int, bool) SplitCondIdx(int condIdx)
		{
			int isTIdx = condIdx % 2;
			int diffDayIdx = (condIdx % (diffDayList.Length * 2) - isTIdx) / 2;
			int ratioIdx = (condIdx % (ratioList.Length * diffDayList.Length * 2) - diffDayIdx * 2 - isTIdx) / (diffDayList.Length * 2);
			int pIdx = (condIdx - ratioIdx * diffDayList.Length * 2 - diffDayIdx * 2 - isTIdx) / (ratioList.Length * diffDayList.Length * 2);
			return (pIdx, ratioIdx, diffDayIdx, isTIdx == 1);
		}
		private static int condNum()
		{
			return periodCntList.GetLength(0) * ratioList.Length * diffDayList.Length * 2;
		}

		// ささっと全体のスコアを調べる
		public static void DebugCheckCond51Score()
		{
			bool isAllScore = false; // Andチェック・Orチェックを無視する

			bool isOrOkForce = ConfirmOrs.Length == 0; // orチェックを強制でOKにしておく
			Dictionary<string, HashSet<string>> beforeNotAnd = new Dictionary<string, HashSet<string>>();
			Dictionary<string, HashSet<string>> beforeOr = new Dictionary<string, HashSet<string>>();
			foreach (string symbol in CsvControll.GetCodeList()) {
				beforeNotAnd[symbol] = new HashSet<string>();
				foreach (int condIdx in ConfirmAnds) {
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(condIdx);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if ((cond51[pIdx + 1] == "1") != isT) beforeNotAnd[symbol].Add(cond51[0]);
					}
				}
				beforeOr[symbol] = new HashSet<string>();
				foreach (int condIdx in ConfirmOrs) {
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(condIdx);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
						if ((cond51[pIdx + 1] == "1") == isT) beforeOr[symbol].Add(cond51[0]);
					}
				}
			}


			int benefitAll = 0;
			int havePeriodAll = 0;
			int trueAll = 0;
			foreach (string symbol in CsvControll.GetCodeList()) {
				Dictionary<string, int> benefits = new Dictionary<string, int>();
				Dictionary<string, int> havePeriods = new Dictionary<string, int>();
				foreach (string[] benefitInfo in CsvControll.GetBenefitAll(symbol)) {
					benefits[benefitInfo[0]] = Int32.Parse(benefitInfo[1]);
					havePeriods[benefitInfo[0]] = Int32.Parse(benefitInfo[2]);
				}
				(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(AllTrueCondIdx);
				List<string[]> list =  CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx);
				for (int i = 0; i < list.Count; i++) {
					string[] cond51 = list[i];
					if (!isAllScore) {
						if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
						if (!isOrOkForce && !beforeOr[symbol].Contains(cond51[0])) continue;
					}

					if (!benefits.ContainsKey(cond51[0])) continue;
					benefitAll += benefits[cond51[0]];
					havePeriodAll += havePeriods[cond51[0]];
					trueAll++;

					if (SkipMode) i += havePeriods[cond51[0]];
				}
			}
			Common.DebugInfo("DebugCheckCond51", trueAll, (double)benefitAll / trueAll, (double)havePeriodAll / trueAll);
		}


		/*
		public static void Aaa()
		{
			Parallel.For(startIndex, endIndex, i => {
				// カウンター変数iを使ったループ内の処理
			});
		}
		*/







		private const int Limit = 1600;
		// 設定したconditionsにて、全コードが(120日目～2024/8/30)の期間にてそれぞれ購入可能かどうかを調べて保存
		public static void SaveBuyInfo()
		{
			List<string[]> conditions = CsvControll.GetConditions(); // 昔決めた51条件のやつ
			List<DateTime> dateList = CsvControll.GetDateList();
			List<string> codeList = CsvControll.GetCodeList();
			string lastDate = "2025/03/05";

			int limit = 0;
			foreach (string code in codeList) {
				limit++;
				if (!(limit > Limit && limit <= Limit + 1000)) continue; // 仮

				List<string[]> saveData = new List<string[]>();
				List<string[]> codeInfo = CsvControll.GetCodeInfo(code);
				string date120 = codeInfo[120][0];

				if (IsAllCheck) {
				} else {
					if (!Common.Pro500(code)) continue;
					date120 = "2024/06/17";
					lastDate = "2024/09/10";
				}

				bool isStart = false;
				foreach (DateTime date in dateList) {
					// 後ろは2024/9/1で固定かな 前は最古(codeInfo[0])から120日後まで => condInfo[120]～2024/08/30
					if (date120 == date.ToString(CsvControll.DFORM)) isStart = true;
					if (!isStart) continue;
					if (lastDate == date.ToString(CsvControll.DFORM)) break;

					// 対象外は-1?いやデータなしかな
					string isCond = IsCondOk(date, codeInfo, conditions) ? "1" : "0";
					saveData.Add(new string[2] { date.ToString(CsvControll.DFORM), isCond });
				}
				CsvControll.SaveBuyInfo(code, saveData);
			}
		}


		// 指定コード・日付が条件を完全に満たすか
		public static bool IsCondOk(DateTime date, List<string[]> codeInfo, List<string[]> conditions)
		{
			bool isOrOk = false;
			bool isAndOk = true;
			foreach (string[] cond in conditions) {
				bool isAnd = cond[0] == "1"; // and条件かor条件か
				if (!isAnd && isOrOk) continue; // or条件はこれ以上調べなくてよい
												// cond[1]は51
				bool isCond = Is51Cond(codeInfo, date, Int32.Parse(cond[2]), Int32.Parse(cond[3]), Int32.Parse(cond[4]), Double.Parse(cond[6]));
				bool isTrue = (cond[5] == "1") == isCond; // 満たす満たさない
				if (isAnd && !isTrue) {
					isAndOk = false;
					break;
				}
				if (!isAnd && isTrue) isOrOk = true;
			}

			return isOrOk && isAndOk;
		}

		public static bool Is51Cond(List<string[]> codeInfo, DateTime date, int period, int diffDay, int cnt, double ratio)
		{
			// 51:period日間でdiffDay日前との比率がratio以上となるのがcnt件以上
			// 53:c1日間でc3日前との比率の平均がa1以上
			// 61:c1日間でc3日前とc3日前から2c3日前の比率の変化回数がd1回以上
			// cは有効日付のみカウント→是正したほうがよいな

			// dateは購入日とする
			// dateからc1日間分遡ってループ処理していき、各日とそのc3日前の比率がa1以上かチェックし、それがd1件以上あるか

			// 日付がdateの直前まである必要がある date以上がなく、-1がなければアウト
			DateTime lastInfo = DateTime.Parse(codeInfo[codeInfo.Count - 1][0]);
			if (!Common.NewD2(date, lastInfo)) {
				if (!Common.SameD(Common.GetDateByIdx(Common.GetDateIdx(lastInfo) + 1), date)) {
					CsvControll.ErrorLog("Is51Cond", date.ToString(), lastInfo.ToString(), "");
					return false;
				}
			}


			// 日付は古い→新しいの順、単純にインデックスがcの値と考える
			for (int i = codeInfo.Count - 1; i >= 0; i--) {
				// 日付,始値,高値,安値,終値
				string[] info = codeInfo[i];
				// 指定日<=計測日　ならスキップ
				if (Common.NewD2(date, DateTime.Parse(info[0]))) continue;
				// データが足りないのでアウト
				if (i - diffDay < 0) Common.DebugInfo("ERROR1", info[0], date.ToString(CsvControll.DFORM), i, diffDay);
				if (Double.Parse(codeInfo[i - diffDay][4]) * ratio <= Double.Parse(info[4])) cnt--;
				if (cnt <= 0) return true;
				period--;
				if (period <= 0) return false;
			}

			return false;
		}



		// 利益平均と所持期間合計など計算
		public static void BenefitSum()
		{
			Dictionary<DateTime, int> japanScores = GetTrueJScoreList();
			List<string> codeList = CsvControll.GetCodeList();
			for (int nowIdx = 0; nowIdx < 10; nowIdx += 10) {
				for (int beforeIdx = 0; beforeIdx < 10; beforeIdx += 10) {
					double havePeriodRatio = 0; // 所持期間比率合計 合計10とかなら常時10銘柄を所持していられるイメージ
					double benefitSum = 0; // 利益合計
					double havePeriodSum = 0; // 所持期間合計

					foreach (string code in codeList) {
						if (!IsAllCheck && !Common.Pro500(code)) continue;

						double[] res = Benefit(code, japanScores, nowIdx, beforeIdx);
						havePeriodRatio += res[1] / res[2];
						benefitSum += res[0];
						havePeriodSum += res[1];
					}
					// 1コード1日あたりの利益平均(とりあえず222を基準とする)
					int ratio = (int)(benefitSum * 1000000 / havePeriodSum);
					//if (ratio > 70) {
					// ni:17- , bi:2-6
					Common.DebugInfo("BenefitSum", nowIdx, beforeIdx, ratio, havePeriodRatio, benefitSum, havePeriodSum);
					//}
				}
			}
		}

		// 特定コードについて 利益合計・所持期間/測定期間 を調べる
		private static double[] Benefit(string code, Dictionary<DateTime, int> japanScores, int nowIdx, int beforeIdx)
		{
			List<string[]> buyInfo = CsvControll.GetBuyInfo(code); // 購入対象日か否か
			List<string[]> codeInfo = CsvControll.GetCodeInfo(code);

			double benefitSum = 0; // 利益合計
			int havePeriodSum = 0; // 所持期間合計

			int havePeriod = 0; // 所持期間
			int firstBuy = -1;
			double buyPrice = 0;
			int buyJScore = 0;
			DateTime buyDate = DateTime.Today;

			for (int i = 0; i < codeInfo.Count - 1; i++) {
				string[] info = codeInfo[i];
				if (info.Length == 0 || buyInfo.Count == 0) Common.DebugInfo("BenefitERROR", code, i, info.Length, buyInfo.Count);
				// その日付の日経平均スコアは必須 まあ基本は大体あるはず
				DateTime date = DateTime.Parse(info[0]);
				if (!japanScores.ContainsKey(date) || !japanScores.ContainsKey(DateTime.Parse(codeInfo[i + 1][0]))) continue;
				// buyinfoが存在する日付帯になったら開始 それまではcontinue
				if (info[0] == buyInfo[0][0]) firstBuy = i;
				if (firstBuy == -1) continue;
				if (i - firstBuy >= buyInfo.Count && buyPrice == 0) break;

				// 基本同じ日付の終値が購入値
				if (buyPrice == 0) {
					if (Int32.Parse(buyInfo[i - firstBuy][1]) == 1) {
						buyPrice = Double.Parse(info[4]);
						havePeriod = 1;
						// この日の日経平均スコア
						buyJScore = 0;
						if (japanScores.ContainsKey(date)) buyJScore = japanScores[date];
						if (japanScores.ContainsKey(DateTime.Parse(codeInfo[i + 1][0])) && buyJScore < japanScores[DateTime.Parse(codeInfo[i + 1][0])]) {
							buyJScore = japanScores[DateTime.Parse(codeInfo[i + 1][0])];
						}
						// 購入時日付
						buyDate = DateTime.Parse(info[0]);
					}
					continue;
				}

				bool isHalfDay = date.Month % 3 == 0 && date.Day >= 14;

				havePeriod++;

				int ratio1 = 7; int ratio2 = 4; int ratio3 = 3; int ratio4 = 2; int ratio5 = 1;
				if (isHalfDay) { ratio1 = 4; ratio2 = 3; ratio3 = 2; ratio4 = 1; ratio5 = 0; }

				bool isSell = false;
				double benefit = 0;
				if (havePeriod <= 5 && buyPrice * (1 + 0.01 * ratio1) <= Double.Parse(info[2])) {
					// 高値が購入値の1.04倍以上なら売却成功
					benefit = 0.01 * ratio1;
					isSell = true;
				} else if (havePeriod <= 10 && buyPrice * (1 + 0.01 * ratio2) <= Double.Parse(info[2])) {
					benefit = 0.01 * ratio2;
					isSell = true;
				} else if (havePeriod <= 20 && buyPrice * (1 + 0.01 * ratio3) <= Double.Parse(info[2])) {
					benefit = 0.01 * ratio3;
					isSell = true;
				} else if (havePeriod <= 30 && buyPrice * (1 + 0.01 * ratio4) <= Double.Parse(info[2])) {
					benefit = 0.01 * ratio4;
					isSell = true;
				} else if (buyPrice * (1 + 0.01 * ratio5) <= Double.Parse(info[2])) {
					benefit = 0.01 * ratio5;
					isSell = true;
				} else if (havePeriod >= 42) {
					// 売却失敗
					//Common.DebugInfo("FailedStartR:{0}:{1}:{2}:{3}:EndR\n", code, Double.Parse(info[4]), buyPrice, Double.Parse(info[4]) / buyPrice - 1);
					benefit = Double.Parse(info[4]) / buyPrice - 1;
					isSell = true;
				} else {
					// 損切 日経平均に応じた感じかな？模索する必要はあるな 危険度高 + 終値4％マイナスとかなら損切かしら
					// 利益最大化になるように検証かしら

					// 1:373, 2:48, 3:36, 4:103
					int jScore = 0;
					if (japanScores.ContainsKey(date)) jScore = japanScores[date];
					// 翌日分のも見て高いほうを参照
					if (japanScores.ContainsKey(DateTime.Parse(codeInfo[i + 1][0])) && jScore < japanScores[DateTime.Parse(codeInfo[i + 1][0])]) {
						jScore = japanScores[DateTime.Parse(codeInfo[i + 1][0])];
					}

					// 購入から現在までの損益(-0.01 ～ -0.30)
					//double nowBenefit = Double.Parse(info[3]) / buyPrice - 1;
					// 前日からの減少値(-0.01 ～ -0.30)
					double beforeBenefit = Double.Parse(info[3]) / Double.Parse(codeInfo[i - 1][4]) - 1;
					// 購入から現在(安値)までの損益(-0.01 ～ -0.30)
					double yasuneBenefit = Double.Parse(info[3]) / buyPrice - 1;

					double now0 = 6; double now1 = 3.5; double now2 = 3; double now3 = 2; double now4 = 0.5;
					double before0 = 3.5; double before1 = 1.5; double before2 = 1; double before3 = 0.5; double before4 = 0;
					if (isHalfDay) {
						now0 = 5; now1 = 2.5; now2 = 2; now3 = 1; now4 = 0;
						before0 = 2.5; before1 = 1; before2 = 0.5; before3 = 0; before4 = 0;
					}

					// ni:0-9=>2--8
					//double setNowBenefit = (double)3 - nowIdx;
					//double setBeforeBenefit = (double)3 - beforeIdx;
					if (jScore == 0) {
						if (yasuneBenefit <= -0.01 * now0 || beforeBenefit <= -0.01 * before0) isSell = true;
						//if (nowBenefit <= 0.01 * setNowBenefit || beforeBenefit <= 0.01 * setBeforeBenefit) isSell = true;
					} else if (jScore == 1) {
						if (yasuneBenefit <= -0.01 * now1 || beforeBenefit <= -0.01 * before1) isSell = true;
						//if (nowBenefit <= 0.01 * setNowBenefit || beforeBenefit <= 0.01 * setBeforeBenefit) isSell = true;
					} else if (jScore == 2) {
						if (yasuneBenefit <= -0.01 * now2 || beforeBenefit <= -0.01 * before2) isSell = true;
						//if (nowBenefit <= 0.01 * setNowBenefit || beforeBenefit <= 0.01 * setBeforeBenefit) isSell = true;
					} else if (jScore == 3) {
						if (yasuneBenefit <= -0.01 * now3 || beforeBenefit <= -0.01 * before3) isSell = true;
						//if (nowBenefit <= 0.01 * setNowBenefit || beforeBenefit <= 0.01 * setBeforeBenefit) isSell = true;
					} else if (jScore == 4) {
						if (yasuneBenefit <= -0.01 * now4 || beforeBenefit <= -0.01 * before4) isSell = true;
						//if (nowBenefit <= 0.01 * setNowBenefit || beforeBenefit <= 0.01 * setBeforeBenefit) isSell = true;
					}

					if (isSell) benefit = Double.Parse(info[4]) / buyPrice - 1;
				}
				if (isSell) {
					int jRatio = 5 - buyJScore; // 購入時JSCORE時によって調整
					bool isHalf = buyDate.Month % 3 == 0 && buyDate.Day >= 14;
					int dateRatio = isHalf ? 2 : 1;// 購入時期によって調整
					havePeriodSum += havePeriod * jRatio / dateRatio;
					benefitSum += benefit * jRatio / dateRatio;

					buyPrice = 0;
					havePeriod = 0;
				}
			}

			return new double[3] { benefitSum, havePeriodSum, buyInfo.Count };
		}



		private static readonly int[][] japanScore5 = new int[4][] {
			new int[] {3359,3486,4019},
			new int[] {2375,2924,4094},
			new int[] {3526,1877},
			new int[] {1767},
		};
		private static readonly int[][] japanScore4 = new int[4][] {
			new int[] {3359,3486},
			new int[] {3562,2915,2375,1396,2924,4094},
			new int[] {3526,1877,1928},
			new int[] {1767,1822},
		};
		private static readonly int[][] japanScore3 = new int[4][] {
			new int[] {3359,3486,189},
			new int[] {3501,3562,2915,2375},
			new int[] {3526,1877},
			new int[] {1767,1822},
		};
		private static readonly int[][] japanScore2 = new int[4][] {
			new int[] {3359,3486},
			new int[] {3501,3562,2915,2375},
			new int[] {3526,1877,1928},
			new int[] {1767,1822},
		};
		private static readonly int[][] japanScore1 = new int[4][] {
			new int[] {3359,3486},
			new int[] {3501,3562,1843,2915,2375},
			new int[] {3526,1928,2368,2941,1350},
			new int[] {1767,1822},
		};
		// 各日付について複数セットの条件を満たすかのチェック 厳しめのチェックから順番にやっていく
		private static Dictionary<DateTime, int> GetTrueJScoreList()
		{
			Dictionary<DateTime, int> res = new Dictionary<DateTime, int>();
			foreach (int[][] japanScoreConds in new int[5][][] { japanScore5, japanScore4, japanScore3, japanScore2, japanScore1 }) {
				Dictionary<DateTime, bool> isOrOks = new Dictionary<DateTime, bool>();
				Dictionary<DateTime, bool> isAndOks = new Dictionary<DateTime, bool>();
				for (int type = 0; type < 4; type++) {
					string isOmote = type % 2 == 0 ? "1" : "0"; //0,2はtrue, 1,3はfalse
					bool isAnd = type < 2; //0,1はAnd, 2,3はOr
					foreach (int idx in japanScoreConds[type]) {
						List<string[]> japanCond = CsvControll.GetJapanCond(idx);
						for (int i = 0; i < japanCond.Count; i++) {
							// この日付に対しての判定
							DateTime date = DateTime.Parse(japanCond[i][0]);
							if (isAnd) {
								if (japanCond[i][1] != isOmote) isAndOks[date] = false;
							} else {
								if (japanCond[i][1] == isOmote) isOrOks[date] = true; // Or条件成功
							}
						}
					}
				}

				List<string[]> japanCond2 = CsvControll.GetJapanCond(189);
				for (int i = 0; i < japanCond2.Count; i++) {
					// この日付に対しての判定
					DateTime date = DateTime.Parse(japanCond2[i][0]);
					if (!isAndOks.ContainsKey(date) && isOrOks.ContainsKey(date)) {
						if (!res.ContainsKey(date)) res[date] = 0;
						res[date]++;
					}
				}
			}

			Dictionary<DateTime, int> trueRes = new Dictionary<DateTime, int>();
			List<string[]> japanCond3 = CsvControll.GetJapanCond(189);
			for (int i = 2; i < japanCond3.Count; i++) {
				int[] jScoreRaws = new int[3];
				for (int j = 0; j < 3; j++) {
					DateTime date = DateTime.Parse(japanCond3[i - j][0]);
					jScoreRaws[j] = res.ContainsKey(date) ? res[date] : 0;
				}
				if (i == 2258 || i == 2259) jScoreRaws[0] /= 2;
				trueRes[DateTime.Parse(japanCond3[i][0])] = ConvertTrueJScore(jScoreRaws[0], jScoreRaws[1], jScoreRaws[2]);
			}
			return trueRes;
		}

		private static readonly int[][] japanScoreInfo = new int[5][] {
			// {1日前がこれ以上なら+1, 2日前がこれ以上なら+1, 1日前がこれ以下なら-1, 2日前がこれ以下なら-1}
			new int[] {3,3, -10,-10}, // 元が0 -1しない
			new int[] {3,3, 0,0}, // 元が1
			new int[] {4,3, 0,1}, // 元が2
			new int[] {5,3, 1,1}, // 元が3
			new int[] {10,10, 2,3}, // 元が4 +1しない
		};
		/** ベースJScoreを前日・前々日のスコアによってトゥルースコアに変換 */
		public static int ConvertTrueJScore(int baseJScore, int baseJScore1, int baseJScore2)
		{
			foreach (int score in new int[3] { baseJScore, baseJScore1, baseJScore2 }) {
				if (score < 0 || score > 5) CsvControll.ErrorLog("ConvertTrueJScore", baseJScore.ToString(), baseJScore1.ToString(), baseJScore2.ToString());
			}
			if (baseJScore == 5) return 4;
			int[] infos = japanScoreInfo[baseJScore];
			if (infos[0] <= baseJScore1 && infos[1] <= baseJScore2) return baseJScore + 1;
			if (infos[2] >= baseJScore1 && infos[3] >= baseJScore2) return baseJScore - 1;
			return baseJScore;
		}



		// 手順的には一日分のjapanCond4226(いや流石に必要分100くらいだけでええか)通りを追記保存=>それを元に指定idx達でスコア0-4を取得(これも保存？)
		// 指定日の前日までのcodeInfoはあらかじめ作っておく
		// 当日の場合はあらかじめ各数値でシミュレーションを行い、どの値であればスコアがいくつになるか逆算値をセットしておく
		public static void SaveJapanBaseScoreOneDay(DateTime setDate)
		{
			Dictionary<int, bool> updateIdxs = new Dictionary<int, bool>();
			foreach (int[][] japanScoreConds in new int[5][][] { japanScore5, japanScore4, japanScore3, japanScore2, japanScore1 }) {
				for (int type = 0; type < 4; type++) {
					foreach (int idx in japanScoreConds[type]) updateIdxs[idx] = true;
				}
			}
			// ここにはあらかじめ前日データまでいれとかねばならない
			List<string[]> japanInfo = CsvControll.GetJapanInfo();
			// 指定idx・dateの更新　JapanCondに追記する
			foreach (KeyValuePair<int, bool> pair in updateIdxs) SaveJapanCondOne(japanInfo, setDate, pair.Key);

			int score = 0;
			foreach (int[][] japanScoreConds in new int[5][][] { japanScore5, japanScore4, japanScore3, japanScore2, japanScore1 }) {
				bool isOrOk = false;
				bool isAndOk = true;
				for (int type = 0; type < 4; type++) {
					string isOmote = type % 2 == 0 ? "1" : "0"; //0,2はtrue, 1,3はfalse
					bool isAnd = type < 2; //0,1はAnd, 2,3はOr
					foreach (int idx in japanScoreConds[type]) {
						List<string[]> japanCond = CsvControll.GetJapanCond(idx);
						bool isExistData = false;
						for (int i = japanCond.Count - 1; i >= 0; i--) {
							// この日付に対しての判定
							if (Common.SameD(setDate, DateTime.Parse(japanCond[i][0]))) {
								if (isAnd) {
									if (japanCond[i][1] != isOmote) isAndOk = false;
								} else {
									if (japanCond[i][1] == isOmote) isOrOk = true; // Or条件成功
								}
								isExistData = true;
								break;
							}
						}
						if (!isExistData) {
							CsvControll.ErrorLog("SaveJapanBaseScoreOneDay", setDate.ToString(), "NoExist", "");
							return;
						}
					}
				}

				if (isAndOk && isOrOk) score++;
			}

			// date,スコアをセーブする　存在したらエラー？
			if (CsvControll.GetBaseJScore(setDate) != -99) {
				CsvControll.ErrorLog("SaveJapanScoreOneDay", setDate.ToString(), "DataExist", score.ToString());
				return;
			}

			CsvControll.SaveBaseJScores(new List<string[]>() { new string[2] { setDate.ToString(CsvControll.DFORM), score.ToString() } }, true);
		}

		// 指定した日付(基本最新日付)の日経平均について、設定した各条件(かぶりなし)を満たしているかどうかを判定してJapanCondに追記保存
		private static void SaveJapanCondOne(List<string[]> japanInfo, DateTime date, int idx)
		{
			// 保存前に当日データが既に存在してないかチェックかな
			foreach (string[] info in CsvControll.GetJapanCond(idx)) {
				if (Common.SameD(date, DateTime.Parse(info[0]))) {
					Common.DebugInfo("sjcEr3", idx, info[0]);
					return;
				}
			}

			int p = idx % periodCntList.GetLength(0);
			int diffDay = diffDayList[idx / (periodCntList.GetLength(0) * ratioList.Length)];
			double ratio = ratioList[(idx / periodCntList.GetLength(0)) % ratioList.Length];
			List<string[]> datas = new List<string[]>() {new string[2] {
				date.ToString(CsvControll.DFORM),
				Is51Cond(japanInfo, date, periodCntList[p, 0], diffDay, periodCntList[p, 1], ratio) ? "1" : "0",
			}};
			CsvControll.SaveJapanCond(idx, datas, true);
		}

		// 指定日前日データを使って指定日のスコア作成後、指定日の偽データ入れて指定日翌日の暫定スコアを200通り作成
		public static void SaveTrueJScoreIkichis(DateTime setDate)
		{
			List<string[]> japanInfo = CsvControll.GetJapanInfo();

			int lastEnd = (int)Double.Parse(japanInfo[japanInfo.Count - 1][4]);

			bool isExist = false;
			int japanInfoIdx = japanInfo.Count;
			for (int i = 0; i < japanInfo.Count; i++) {
				string[] info = japanInfo[i];
				if (Common.SameD(setDate, DateTime.Parse(info[0]))) {
					isExist = true;
					lastEnd = (int)Double.Parse(info[4]);
					japanInfoIdx = i;
					break;
				}
			}
			if (!isExist) {
				string[] last = japanInfo[japanInfo.Count - 1];
				// 最新の日付に翌日分のを暫定でいれる(これを200通りで変更させる)
				japanInfo.Add(new string[] { setDate.ToString(CsvControll.DFORM), last[1], last[2], last[3], last[4] });
			}


			DateTime nextDate = Common.GetDateByIdx(Common.GetDateIdx(setDate) + 1);

			// 直前に作ったやつとその前日分
			int baseJScore1 = CsvControll.GetBaseJScore(setDate);
			int baseJScore2 = CsvControll.GetBaseJScore(Common.GetDateByIdx(Common.GetDateIdx(setDate) - 1));

			List<string[]> scoreIkichis = new List<string[]>();
			for (int s = 0; s < 4000; s += 20) {
				//for (int s = 1100; s < 1500; s += 1) {
				// 200通りの値段でシミュレーション
				japanInfo[japanInfoIdx][4] = (lastEnd + s - 2000).ToString();

				int baseJScore = 0;
				int debug = 0;
				foreach (int[][] japanScoreConds in new int[5][][] { japanScore5, japanScore4, japanScore3, japanScore2, japanScore1 }) {
					bool isOrOk = false;
					bool isAndOk = true;
					for (int type = 0; type < 4; type++) {
						bool isOmote = type % 2 == 0; //0,2はtrue, 1,3はfalse
						bool isAnd = type < 2; //0,1はAnd, 2,3はOr
						foreach (int idx in japanScoreConds[type]) {
							int p = idx % periodCntList.GetLength(0);
							int diffDay = diffDayList[idx / (periodCntList.GetLength(0) * ratioList.Length)];
							double ratio = ratioList[(idx / periodCntList.GetLength(0)) % ratioList.Length];
							bool idCond = Is51Cond(japanInfo, nextDate, periodCntList[p, 0], diffDay, periodCntList[p, 1], ratio);
							if (isAnd) {
								if (idCond != isOmote) {
									isAndOk = false;
									//Common.DebugInfo("isAndOkFalse:{0}:{1}:{2}: {3},{4},{5},{6},{7} :EndR\n", s, debug, idx, nextDate, periodCntList[p, 0], diffDay, periodCntList[p, 1], ratio);
								}
							} else {
								if (idCond == isOmote) {
									isOrOk = true; // Or条件成功
												   //Common.DebugInfo("isOrOkTrue:{0}:{1}:{2}: {3},{4},{5},{6},{7} :EndR\n", s, debug, idx, nextDate, periodCntList[p, 0], diffDay, periodCntList[p, 1], ratio);
								}
							}
						}
					}

					if (isAndOk && isOrOk) baseJScore++;

					debug++;
					//if (isAndOk) Common.DebugInfo("baseJScore:{0}:{1}:{2}:EndR\n", s, debug, baseJScore);
					//if (isOrOk) Common.DebugInfo("baseJScore2:{0}:{1}:{2}:EndR\n", s, debug, baseJScore);
				}

				// 生スコア0-5を前日に0-4にする
				int trueScore = ConvertTrueJScore(baseJScore, baseJScore1, baseJScore2);
				// 翌日仮スコアを保存(値段,日経平均スコア)
				scoreIkichis.Add(new string[2] { (lastEnd + s - 2000).ToString(), trueScore.ToString() });
			}

			CsvControll.SaveTrueJScoreIkichis(scoreIkichis);
		}


		///////////////////////////////////////////
		// 以下日経平均調査用 もういらんかも？
		///////////////////////////////////////////

		// 2015-2024の各日付 × 4256通りのconditionについて全部の結果を保存する
		public static void SaveJapanCond()
		{
			List<string[]> japanInfo = CsvControll.GetJapanInfo();

			Dictionary<int, bool> needIdxs = new Dictionary<int, bool>();
			foreach (int[][] japanScoreConds in new int[5][][] { japanScore5, japanScore4, japanScore3, japanScore2, japanScore1 }) {
				for (int type = 0; type < 4; type++) {
					foreach (int needIdx in japanScoreConds[type]) needIdxs[needIdx] = true;
				}
			}

			int idx = 0;
			foreach (int diffDay in diffDayList) {
				foreach (double ratio in ratioList) {
					for (int p = 0; p < 38; p++) {
						// 特定idx以外スキップ
						if (true && !needIdxs.ContainsKey(idx)) {
							idx++;
							continue;
						}

						List<string[]> datas = new List<string[]>();
						for (int i = 120; i < japanInfo.Count - JScoreDiff; i++) {
							DateTime date = DateTime.Parse(japanInfo[i][0]);
							datas.Add(new string[2] {
								date.ToString(CsvControll.DFORM),
								Is51Cond(japanInfo, date, periodCntList[p, 0], diffDay, periodCntList[p, 1], ratio) ? "1" : "0",
							});
						}
						CsvControll.SaveJapanCond(idx, datas);
						idx++;
					}
				}
			}
		}

		private const double DRate = 0.03;
		private const int JScoreDiff = 6; // 6日後との差分を考える
		private static void JapanCheck()
		{
			// 日経平均について、各日付ごとに二日後に4％減ってれば1,8%なら2,それ以外は0とする
			List<string[]> japanInfo = new List<string[]>();
			foreach (string[] info in CsvControll.GetJapanInfo()) japanInfo.Add(new string[4] { info[0], info[4], "1", "0" });

			// デバッグ用チェック
			int[] trueCnt = new int[4];
			for (int i = 120; i < japanInfo.Count - JScoreDiff; i++) {
				double price = Double.Parse(japanInfo[i][1]);
				double price2Af = Double.Parse(japanInfo[i + JScoreDiff][1]);
				int point = 0;
				if (price * (1 - DRate) >= price2Af) point++;
				if (price * (1 - DRate * 1.6) >= price2Af) point++;
				if (price * (1 - DRate * 2.2) >= price2Af) point++;
				trueCnt[point]++;
			}
			Common.DebugInfo("JapanCheck",
				japanInfo.Count, japanInfo.Count - 120 - JScoreDiff, trueCnt[0], trueCnt[1], trueCnt[2], trueCnt[3],
				japanInfo.Count - 120 - JScoreDiff - trueCnt[0] - trueCnt[1] - trueCnt[2] - trueCnt[3]
			);

			// これらで全日付のand状況or状況を記録しておく
			// andを全て満たせてないものはfalse確定で終了 orを一個でも満たしているものはorは確定でOK
			// 検証側はandの場合は満たしてないもの全て強制でfalseに、orの場合はor条件満たしていないものについてtrueであればtrue
			/*
				BaseOScore:(0_3):161 : 2.25465838509317 : (112:3:33:13):EndR
				new int[] {189,4019,3453 },
				new int[] {4170,1869,2446,4100,2397,1304},
				new int[] {4027 ,2972},
				new int[] { 216,1760,1256},

				BaseOScore:(0_3):130 : 2.48461538461538 : (87:3:28:12):EndR
				new int[] {189,4019,3453 },
				new int[] {4170,1869,2446,4100,2397,1304},
				new int[] {4027 ,2972},
				new int[] { 216,1760},

				BaseOScore:0:3:226:1.79646017699115:166:6:41:13:EndR
				new int[] {189,4019,3453 },
				new int[] {797,2969,4170,3508,1869,2446,4100,2397},
				new int[] {4027 ,2972},
				new int[] { 216,1760,1256},

				BaseOScore:(0_3):472 : 1.22669491525424 : (369:24:63:16):End
				new int[] {189,4019,3453,3487 },
				new int[] {3032 ,1869,2446,2397,4095,2994},
				new int[] {2972,779,3457},
				new int[] { 2861,2362,3921},
			 */

			/*
				BaseOScore:(0_3):90 : 2.8 : (52:12:15:11):EndR
				new int[] {3404,1306,4018,233},
				new int[] {235,4028,2915,3510,4014,2445,1291,2357},
				new int[] {1929,3970},
				new int[] {2818,1296,1832},

				BaseOScore:(0_3):203 : 2.00492610837438 : (148:17:18:20):EndR
				new int[] {3404,1306,3450,4018},
				new int[] {2915,2950,2924,4094,3468,1884},
				new int[] {1929,2907,3526,3475},
				new int[] {1296,1832,1785},

				BaseOScore:(0_3):295 : 1.67118644067797 : (222:23:28:22):EndR
				new int[] {3404,1306,3450,4018},
				new int[] {4028,253,2915,3510,2950,2452,2924,4094},
				new int[] {1929,1878,2907,3526,3475},
				new int[] {1296,1832,1785},
				
				BaseOScore:(0_3):458 : 1.30786026200873 : (359:39:34:26):EndR
				new int[] {3404,1306,3450,3972},
				new int[] {235,4028,253,3562,2915,3510,3469,2452,2924,2930},
				new int[] {1929,2907,3526,3475,3995,1317},
				new int[] {1832,1785,3400,3932},
			 */

			/*
				BaseOScore:(0_3):127 : 5.06299212598425 : (60:13:18:36):EndR
				new int[] {3359,3486,4019},
				new int[] {2375,2924,4094},
				new int[] {3526,1877},
				new int[] {1767},
				/////////////////////////////////////////////////////////////////////
				BaseOScore:(0_3):241 : 3.33195020746888 : (146:28:23:44):EndR
				new int[] {3359,3486},
				new int[] {3562,2915,2375,1396,2924,4094},
				new int[] {3526,1877,1928},
				new int[] {1767,1822},
				/////////////////////////////////////////////////////////////////////
				BaseOScore:(0_3):316 : 2.67721518987342 : (204:41:26:45):EndR
				new int[] {3359,3486,189},
				new int[] {3501,3562,2915,2375},
				new int[] {3526,1877},
				new int[] {1767,1822},
				/////////////////////////////////////////////////////////////////////
				BaseOScore:(0_3):419 : 2.25536992840095 : (288:50:32:49):EndR
				new int[] {3359,3486},
				new int[] {3501,3562,2915,2375},
				new int[] {3526,1877,1928},
				new int[] {1767,1822},
				/////////////////////////////////////////////////////////////////////
				BaseOScore:(0_3):635 : 1.74173228346457 : (469:66:46:54):EndR
				new int[] {3359,3486},
				new int[] {3501,3562,1843,2915,2375},
				new int[] {3526,1928,2368,2941,1350},
				new int[] {1767,1822},
			*/
			int[][] defaultIdxList = new int[4][] {
				new int[] {3359,3486,4019},
				new int[] {2375,2924,4094},
				new int[] {3526,1877},
				new int[] {1767},
			};

			// 事前チェック
			BeforeCheck(japanInfo, defaultIdxList);

			int idx = 0;
			foreach (int diffDay in diffDayList) {
				foreach (double ratio in ratioList) {
					for (int p = 0; p < 38; p++) {
						for (int type = 0; type < 4; type++) {
							/*
							string[] condition = new string[7] {
								isAnd, // and条件かor条件か
								"51",
								periodCntList[p,0].ToString(), // c1:1,3,6,10,20,30,50
								diffDay.ToString(), //c3:1,3,6,10,20,30,50,70
								periodCntList[p,1].ToString(), // d1:1,2,3,4,5,6 いやc1に応じる感じか
								"1", // 満たす満たさない
								ratio.ToString(), // a1: 0.65,0.75,0.8,0.85,0.9,0.95,1,1.05,1.1,1.17,1.25,1.35,1.5,1.7
							};
							conditions = new List<string[]>();
							conditions.Add(condition);
							*/

							bool isAnd = type < 2;

							// 最終値計測用
							if (idx == 0 && (type == 0 || type == 3)) {
								//int[][] scoresA = JapanLossScore(codeInfo, idx, type % 2 == 0 ? "1" : "0", isAnd);
								// td:tu:fd:fu
								//Common.DebugInfo("BaseScore:{0}:{1}:{2}:{3}:EndR\n", scoresA[0], scoresA[1], scoresA[2], scoresA[3]);
								//continue;
							}

							int[][] scores = JapanLossScore(japanInfo, idx, type % 2 == 0 ? "1" : "0", isAnd);
							if (isAnd) {
								// andスコア
								int[] fSco = scores[1];
								int sum = fSco[0] + fSco[1] + fSco[2] + fSco[3];
								double fRatio = (double)(fSco[1] + fSco[2] * 2 + fSco[3] * 4) / sum;
								//double fRatio = (double)(fSco[1] + fSco[2] * 5 + fSco[3] * 15) / sum;
								if (idx == 0 && type == 0) Common.DebugInfo("BaseAScore", idx, type, sum, fRatio, fSco[0], fSco[1], fSco[2], fSco[3]);
								if (idx != 0 && sum >= 30 && fRatio <= 0.45) {
									//Common.DebugInfo("AndScore:({0}_{1}):{2} : {3} : ({4}:{5}:{6}:{7}):EndR\n", idx, type, sum, fRatio, fSco[0], fSco[1], fSco[2], fSco[3]);
								}
							} else {
								// orスコア
								int[] tSco = scores[0];
								int sum = tSco[0] + tSco[1] + tSco[2] + tSco[3];
								double tRatio = (double)(tSco[1] + tSco[2] * 5 + tSco[3] * 15) / sum;
								if (idx == 0 && type == 3) Common.DebugInfo("BaseOScore", idx, type, sum, tRatio, tSco[0], tSco[1], tSco[2], tSco[3]);
								if (idx != 0 && sum >= 40 && tRatio > 0.45) {
									//Common.DebugInfo("OrScore:({0}_{1}):{2} : {3} : ({4}:{5}:{6}:{7}):EndR\n", idx, type, sum, tRatio, tSco[0], tSco[1], tSco[2], tSco[3]);
								}
							}
						}
						idx++;
						break;
					}
				}
			}
		}

		private static void BeforeCheck(List<string[]> codeInfo, int[][] defaultIdxList)
		{
			for (int type = 0; type < 4; type++) {
				string isOmote = type % 2 == 0 ? "1" : "0"; //0,2はtrue, 1,3はfalse
				bool isAnd = type < 2; //0,1はAnd, 2,3はOr
				foreach (int idx in defaultIdxList[type]) {
					List<string[]> japanCond = CsvControll.GetJapanCond(idx);
					for (int i = 0; i < japanCond.Count; i++) {
						if (japanCond[i][0] != codeInfo[i + 120][0]) Common.DebugInfo("BeforeCheckERROR");
						if (isAnd) {
							if (japanCond[i][1] != isOmote) codeInfo[i + 120][2] = "0"; // And条件失敗
						} else {
							if (japanCond[i][1] == isOmote) codeInfo[i + 120][3] = "1"; // Or条件成功
						}
					}
				}
			}
		}

		private static int[][] JapanLossScore(List<string[]> codeInfo, int idx, string isOmote, bool isAnd)
		{
			// andはFになっているもののうち、Uになっているものの比率が最も高いやつを選ぶ
			// orはTのうちdになっている比率が最も高いやつ

			// 引数condIdxに対する 各日付(120～-1)の結果
			List<string[]> japanCond = CsvControll.GetJapanCond(idx);
			int[] tScores = new int[4]; // 0-3
			int[] fScores = new int[4]; // 0-3
			for (int i = 0; i < japanCond.Count; i++) {
				if (codeInfo.Count <= i + 120 + JScoreDiff) continue;
				if (japanCond[i][0] != codeInfo[i + 120][0]) Common.DebugInfo("ERRORJapanLossScore");

				bool isT = japanCond[i][1] == isOmote;

				// 事前and失敗は評価対象外とする
				if (codeInfo[i + 120][2] == "0") {
					if (idx != 0) continue;
					if (idx == 0) isT = false;
				}
				// or系の場合 事前にor系を満たしていたら評価対象外
				if (!isAnd && codeInfo[i + 120][3] == "1") {
					if (idx != 0) continue;
					if (idx == 0) isT = codeInfo[i + 120][2] != "0";
				}


				double price = Double.Parse(codeInfo[i + 120][1]);
				double price2Af = Double.Parse(codeInfo[i + 120 + JScoreDiff][1]);
				int point = 0;
				if (price * (1 - DRate) >= price2Af) point++;
				if (price * (1 - DRate * 1.6) >= price2Af) point++;
				if (price * (1 - DRate * 2.2) >= price2Af) point++;


				// 最終結果用 and系の場合、事前orがtrueかつ今回trueじゃないと			
				if (true && isAnd && codeInfo[i + 120][3] == "0") continue;

				//bool isAllT = codeInfo[i + 120][2] == "1" && ((isAnd && codeInfo[i + 120][3] == "1" && isT) || (!isAnd && (codeInfo[i + 120][3] == "1" || isT)));
				if (isT) {
					tScores[point]++;
				} else {
					fScores[point]++;
				}
			}

			//Common.DebugInfo("LastSt:{0}:{1}:{2}:EndR\n", idx,tScore, fScore);
			return new int[2][] { tScores, fScores };
		}


		public static readonly int[,] periodCntList = new int[38, 2]{
			{1,1},{3,1},{3,2},{3,3},{6,1},{6,3},{6,4},{6,6},{10,1},{10,3},{10,5},{10,7},{10,10},
			{20,1},{20,3},{20,5},{20,7},{20,10},{20,15},{20,20},{30,1},{30,3},{30,6},{30,10},
			{30,15},{30,20},{30,25},{30,30},{50,1},{50,3},{50,6},
			{50,10},{50,15},{50,20},{50,25},{50,30},{50,40},{50,50},
		};
		public static readonly int[] diffDayList = new int[8] { 1, 3, 6, 10, 20, 30, 50, 70 };
		public static readonly double[] ratioList = new double[14] {
			0.65,0.75,0.8,0.85,0.9,0.95,1,1.05,1.1,1.17,1.25,1.35,1.5,1.7
		};
		//private static readonly double[] ratioList = new double[1] {
		//	1
		//};

	}
}



/*
 
 
LowScoreRank1 , T0:1 , T1:
Cond:98, T:70423, TR:-0.0147211292478714, Period:8.07686409269699, Benefit0.721568237649632,
Cond:96, T:73820, TR:-0.0151079757961417, Period:7.99726361419669, Benefit0.706976429151991,
Cond:544, T:66722, TR:-0.0134499051647378, Period:8.08024339797968, Benefit0.692140523365607,
Cond:6428, T:61327, TR:-0.0123973804214309, Period:8.45384577755312, Benefit0.690821334811747,
Cond:100, T:66339, TR:-0.013097421839006, Period:8.06221076591447, Benefit0.676615565504454,
Cond:546, T:66627, TR:-0.0131366739786984, Period:8.05545799750852, Benefit0.67580710522761,
Cond:126, T:69712, TR:-0.0135009787955738, Period:7.42755910029837, Benefit0.664577117282534,
/////////////////////////////////
/////////////////////////////////
/////////////////////////////////



LowScoreRank1 , T0:8376 , T1:
Cond:750, T:36052, TR:-0.00935090300669511, Period:3.81895595251304, Benefit0.869577277266171,
Cond:96, T:47865, TR:-0.0121109257357862, Period:6.42772380653923, Benefit0.86641596155855,
Cond:8354, T:39202, TR:-0.009923463898437, Period:3.63506963930412, Benefit0.853221774399265,
Cond:4330, T:35390, TR:-0.00903575539051527, Period:3.44877083922012, Benefit0.852896298389376,
Cond:6636, T:38153, TR:-0.00965268584856647, Period:6.54742746310906, Benefit0.850575315178361,
Cond:6128, T:38512, TR:-0.00964285511415159, Period:3.69409534690486, Benefit0.841607810552555,
Cond:92, T:36367, TR:-0.00914569914312661, Period:4.17037424038276, Benefit0.840982209145654,
Cond:6122, T:38707, TR:-0.00966488859444069, Period:3.50089131164906, Benefit0.839434727568657,
Cond:4318, T:35227, TR:-0.0088633006498714, Period:3.53742867686718, Benefit0.838674880063588,
Cond:98, T:46579, TR:-0.0114509009183975, Period:6.46383563408403, Benefit0.838124476695507,
Cond:6116, T:36656, TR:-0.00917411134652778, Period:3.48682343954605, Benefit0.837161719773025,
Cond:8344, T:38276, TR:-0.00951524535315785, Period:3.42616783362943, Benefit0.834465461385725,
Cond:1646, T:37319, TR:-0.00928845560335564, Period:3.88445563921863, Benefit0.833516439347249,
Cond:6120, T:38286, TR:-0.00950423936716354, Period:3.508358146581, Benefit0.83317661808494,
Cond:318, T:36695, TR:-0.00913601695878453, Period:4.49205613843848, Benefit0.832402234636871,
Cond:94, T:42222, TR:-0.0103261313256465, Period:4.79842262327696, Benefit0.82689119416418,
Cond:764, T:37598, TR:-0.0092025304555364, Period:4.19785626895048, Benefit0.818766955689132,
Cond:6110, T:37054, TR:-0.00907799385017935, Period:3.71336427916014, Benefit0.818427160360555,
Cond:6118, T:37082, TR:-0.00907456553169559, Period:3.48349603581252, Benefit0.817458605253223,
Cond:6108, T:36427, TR:-0.00890434882622075, Period:3.46849864111785, Benefit0.814944958409971,
Cond:8356, T:40375, TR:-0.00977395302692602, Period:3.57865015479876, Benefit0.814390092879257,
Cond:5260, T:35912, TR:-0.00876463670679064, Period:5.07365226108265, Benefit0.812291156159501,
Cond:3692, T:36251, TR:-0.00883320251436347, Period:4.8928029571598, Benefit0.811646575266889,
Cond:2766, T:38601, TR:-0.00932018293322498, Period:3.98168441232092, Benefit0.808580088598741,
Cond:1660, T:39095, TR:-0.00942745596617248, Period:4.24381634480113, Benefit0.808440977107047,
Cond:8358, T:40883, TR:-0.0098108310714881, Period:3.58249150013453, Benefit0.807499449648998,
Cond:7068, T:39291, TR:-0.00946051871423016, Period:6.17016619582093, Benefit0.807487719834059,
Cond:4828, T:35913, TR:-0.00870763407854803, Period:5.72004566591485, Benefit0.806365383008938,
Cond:6385, T:37477, TR:-0.0090472842864479, Period:3.74651119353203, Benefit0.806041038503616,
Cond:2780, T:41342, TR:-0.00985809136285985, Period:4.39117604373277, Benefit0.802670407817716, , T2:750,96,8354,4330,6636,6128,92,6122,4318,98,6116,8344,1646,6120,318,94,764,6110,6118,6108,8356,5260,3692,2766,1660,8358,7068,4828,6385,2780,  #End#
LowScoreRank2 , T0:8376 , T1:
Cond:47, T:35185, TR:-0.00562369288069122, Period:8.13684808867415, Benefit0.49720051158164,
Cond:6101, T:35828, TR:-0.00567145122017457, Period:8.06472591269398, Benefit0.493133861784079,
Cond:1885, T:36175, TR:-0.00601726136610239, Period:7.98482377332412, Benefit0.523897719419489,
Cond:2755, T:35443, TR:-0.00611716755223833, Period:7.64181925909206, Benefit0.545326298563891,
Cond:2303, T:35267, TR:-0.0061284384320007, Period:8.13755068477614, Benefit0.54926702016049,
Cond:287, T:35468, TR:-0.00614436345471254, Period:8.09535355813691, Benefit0.54778955678358,
Cond:301, T:35505, TR:-0.00614657547371458, Period:8.08534009294466, Benefit0.547444021968737,
Cond:3007, T:35651, TR:-0.0061513875923409, Period:7.7835684833525, Benefit0.54567894308715,
Cond:1871, T:36193, TR:-0.00615443545138036, Period:7.97502279446302, Benefit0.537728290000829,
Cond:327, T:35471, TR:-0.00617125435908604, Period:7.04598122409856, Benefit0.550562431281892,
Cond:3439, T:35478, TR:-0.0061984208325626, Period:7.97057331303907, Benefit0.553300637014488,
Cond:3661, T:36765, TR:-0.0062250410979094, Period:7.96360669114647, Benefit0.53640690874473,
Cond:1613, T:35679, TR:-0.0062358605025211, Period:8.12301353737493, Benefit0.554051402785953,
Cond:1599, T:36256, TR:-0.00624812256987116, Period:8.05670785525155, Benefit0.546392321270962,
Cond:75, T:36942, TR:-0.00628585477134674, Period:7.75808023388014, Benefit0.53992745384657,
Cond:495, T:36747, TR:-0.00631672350926095, Period:7.94494788690233, Benefit0.545949329196941,
Cond:3229, T:36806, TR:-0.00633941605129529, Period:7.88550779764169, Benefit0.547356409281096,
Cond:6075, T:36394, TR:-0.00635700064219582, Period:8.08185415178326, Benefit0.555421223278562,
Cond:2093, T:37029, TR:-0.00639735633511456, Period:7.94428691025953, Benefit0.549839315131384,
Cond:2525, T:36114, TR:-0.00640922757082177, Period:8.09231876834469, Benefit0.565154787616991,
Cond:97, T:39055, TR:-0.00641295474202137, Period:6.778824734349, Benefit0.522468313916272,
Cond:1627, T:37334, TR:-0.00643527139149634, Period:7.87992178711094, Benefit0.549070552311566,
Cond:703, T:37495, TR:-0.00643797115557658, Period:7.90262701693559, Benefit0.546952927056941,
Cond:537, T:37224, TR:-0.00643992273423458, Period:7.6373576187406, Benefit0.551176660219213,
Cond:3871, T:35643, TR:-0.00645304865318023, Period:8.04250483965996, Benefit0.577280251381758,
Cond:8121, T:36445, TR:-0.00647218983224421, Period:8.03808478529291, Benefit0.566387707504459,
Cond:6397, T:37449, TR:-0.00647420683443997, Period:6.46054634302652, Benefit0.551229672354402,
Cond:5021, T:37067, TR:-0.00649075601367361, Period:7.80991178136887, Benefit0.558637062616343,
Cond:7915, T:35465, TR:-0.00649253096973965, Period:8.10607641336529, Benefit0.584350768363175,
Cond:4079, T:35727, TR:-0.00651584297202674, Period:8.07935175077672, Benefit0.582444649704705, , T2:47,6101,1885,2755,2303,287,301,3007,1871,327,3439,3661,1613,1599,75,495,3229,6075,2093,2525,97,1627,703,537,3871,8121,6397,5021,7915,4079,  #End#
LowScoreRank3 , T0:8376 , T1:
Cond:1, T:58625, TR:-0.0120393164963188, Period:6.0880170575693, Benefit0.700656716417911,
Cond:3, T:58608, TR:-0.0120844417092004, Period:6.0894929019929, Benefit0.703709391209391,
Cond:5, T:58570, TR:-0.0121135357561421, Period:6.09185589892436, Benefit0.706009902680553,
Cond:6, T:149, TR:-0.000929613986811859, Period:1.95973154362416, Benefit0.442953020134228,
Cond:7, T:58567, TR:-0.0121064918996439, Period:6.09180938070927, Benefit0.705602130892824,
Cond:8, T:1163, TR:-0.00124608449584586, Period:1.65692175408426, Benefit1.07738607050731,
Cond:9, T:57916, TR:-0.0120286223669654, Period:6.14258581393743, Benefit0.708698805166103,
Cond:10, T:3047, TR:-0.00161452984309828, Period:1.87659993436167, Benefit0.864128651132261,
Cond:11, T:56572, TR:-0.0117938758126, Period:6.24664144806618, Benefit0.71047514671569,
Cond:12, T:7881, TR:-0.00302894072252372, Period:2.35503108742545, Benefit1.00558304783657,
Cond:13, T:52535, TR:-0.0104926453575039, Period:6.54594080137051, Benefit0.674274293328257,
Cond:14, T:12560, TR:-0.00398320617498315, Period:2.61616242038217, Benefit0.914171974522293,
Cond:15, T:48418, TR:-0.0094751812323754, Period:6.86094014622661, Benefit0.654611921186336,
Cond:17, T:58617, TR:-0.0120295508882065, Period:6.08871146595698, Benefit0.700138185168125,
Cond:18, T:690, TR:-0.00131544970800672, Period:2.16666666666667, Benefit2.19420289855072,
Cond:19, T:58497, TR:-0.0121170840060394, Period:6.09759474844864, Benefit0.707130280185309,
Cond:20, T:1095, TR:-0.00144665913259422, Period:2.2648401826484, Benefit1.83196347031963,
Cond:21, T:58232, TR:-0.0121654490877426, Period:6.11529743096579, Benefit0.713473691441132,
Cond:22, T:1418, TR:-0.00140042649411158, Period:2.38222849083216, Benefit1.2919605077574,
Cond:23, T:58080, TR:-0.0121067905732678, Period:6.12475895316804, Benefit0.7116391184573,
Cond:24, T:5533, TR:-0.00302064277009927, Period:2.08675221398879, Benefit1.42797758901139,
Cond:25, T:55058, TR:-0.0109052247920714, Period:6.36012931817356, Benefit0.670620073377166,
Cond:26, T:9228, TR:-0.00338270499655868, Period:2.33550065019506, Benefit1.00195058517555,
Cond:27, T:51895, TR:-0.0104346610704698, Period:6.60053955101648, Benefit0.678581751613836,
Cond:28, T:16842, TR:-0.00539117678443592, Period:2.77603610022563, Benefit0.993231207695048,
Cond:29, T:44668, TR:-0.00827618647529606, Period:7.19235246709053, Benefit0.610772812751858,
Cond:30, T:21665, TR:-0.00676827223247699, Period:2.93357950611585, Benefit1.00821601661666,
Cond:31, T:39354, TR:-0.00671356242713022, Period:7.67347664786299, Benefit0.546831325913503,
Cond:33, T:58566, TR:-0.0120807873863735, Period:6.09227196667008, Benefit0.703992077314483,
Cond:34, T:1885, TR:-0.00183412048970944, Period:2.63819628647215, Benefit1.83501326259947, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:8376 , T1:
Cond:1, T:58625, TR:-0.0120393164963188, Period:6.0880170575693, Benefit0.700656716417911,
Cond:3, T:58608, TR:-0.0120844417092004, Period:6.0894929019929, Benefit0.703709391209391,
Cond:5, T:58570, TR:-0.0121135357561421, Period:6.09185589892436, Benefit0.706009902680553,
Cond:6, T:149, TR:-0.000929613986811859, Period:1.95973154362416, Benefit0.442953020134228,
Cond:7, T:58567, TR:-0.0121064918996439, Period:6.09180938070927, Benefit0.705602130892824,
Cond:8, T:1163, TR:-0.00124608449584586, Period:1.65692175408426, Benefit1.07738607050731,
Cond:9, T:57916, TR:-0.0120286223669654, Period:6.14258581393743, Benefit0.708698805166103,
Cond:10, T:3047, TR:-0.00161452984309828, Period:1.87659993436167, Benefit0.864128651132261,
Cond:11, T:56572, TR:-0.0117938758126, Period:6.24664144806618, Benefit0.71047514671569,
Cond:12, T:7881, TR:-0.00302894072252372, Period:2.35503108742545, Benefit1.00558304783657,
Cond:13, T:52535, TR:-0.0104926453575039, Period:6.54594080137051, Benefit0.674274293328257,
Cond:14, T:12560, TR:-0.00398320617498315, Period:2.61616242038217, Benefit0.914171974522293,
Cond:15, T:48418, TR:-0.0094751812323754, Period:6.86094014622661, Benefit0.654611921186336,
Cond:17, T:58617, TR:-0.0120295508882065, Period:6.08871146595698, Benefit0.700138185168125,
Cond:18, T:690, TR:-0.00131544970800672, Period:2.16666666666667, Benefit2.19420289855072,
Cond:19, T:58497, TR:-0.0121170840060394, Period:6.09759474844864, Benefit0.707130280185309,
Cond:20, T:1095, TR:-0.00144665913259422, Period:2.2648401826484, Benefit1.83196347031963,
Cond:21, T:58232, TR:-0.0121654490877426, Period:6.11529743096579, Benefit0.713473691441132,
Cond:22, T:1418, TR:-0.00140042649411158, Period:2.38222849083216, Benefit1.2919605077574,
Cond:23, T:58080, TR:-0.0121067905732678, Period:6.12475895316804, Benefit0.7116391184573,
Cond:24, T:5533, TR:-0.00302064277009927, Period:2.08675221398879, Benefit1.42797758901139,
Cond:25, T:55058, TR:-0.0109052247920714, Period:6.36012931817356, Benefit0.670620073377166,
Cond:26, T:9228, TR:-0.00338270499655868, Period:2.33550065019506, Benefit1.00195058517555,
Cond:27, T:51895, TR:-0.0104346610704698, Period:6.60053955101648, Benefit0.678581751613836,
Cond:28, T:16842, TR:-0.00539117678443592, Period:2.77603610022563, Benefit0.993231207695048,
Cond:29, T:44668, TR:-0.00827618647529606, Period:7.19235246709053, Benefit0.610772812751858,
Cond:30, T:21665, TR:-0.00676827223247699, Period:2.93357950611585, Benefit1.00821601661666,
Cond:31, T:39354, TR:-0.00671356242713022, Period:7.67347664786299, Benefit0.546831325913503,
Cond:33, T:58566, TR:-0.0120807873863735, Period:6.09227196667008, Benefit0.703992077314483,
Cond:34, T:1885, TR:-0.00183412048970944, Period:2.63819628647215, Benefit1.83501326259947, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:6844 , T1:
Cond:8354, T:23708, TR:-0.00748086958917532, Period:3.45495191496541, Benefit1.03290028682301,
Cond:98, T:30627, TR:-0.00921349885494, Period:8.14066020178274, Benefit1.00855454337676,
Cond:78, T:24739, TR:-0.00758078864773962, Period:3.87355996604552, Benefit1.00460810865435,
Cond:96, T:32015, TR:-0.00952690975110338, Period:8.12715914415118, Benefit1.00087459003592,
Cond:2764, T:23950, TR:-0.00731276441943791, Period:3.41073068893528, Benefit0.996200417536534,
Cond:526, T:24080, TR:-0.00731891608194047, Period:3.76283222591362, Benefit0.991735880398671,
Cond:2750, T:24124, TR:-0.00732061065058559, Period:3.4696982258332, Benefit0.990175758580667,
Cond:6120, T:23896, TR:-0.00725476509773545, Period:3.37537663207231, Benefit0.989412453967191,
Cond:8356, T:24477, TR:-0.00737679327709106, Period:3.40282714384933, Benefit0.984352657596928,
Cond:8368, T:24727, TR:-0.00742500682464206, Period:3.59590730780119, Benefit0.981599061754358,
Cond:8344, T:24075, TR:-0.00723072169348702, Period:3.34434060228453, Benefit0.978276220145379,
Cond:750, T:25335, TR:-0.00754312141917352, Period:3.94454312216302, Benefit0.975251628182356,
Cond:8358, T:24749, TR:-0.0073805482297299, Period:3.40599620186674, Benefit0.974019152288981,
Cond:6122, T:24667, TR:-0.00735893952392608, Period:3.4051566870718, Benefit0.974013864677504,
Cond:6094, T:23790, TR:-0.00709106815584482, Period:3.37540983606557, Benefit0.96817990752417,
Cond:6134, T:24699, TR:-0.00732870926631828, Period:3.44483582331268, Benefit0.968176849265152,
Cond:4316, T:23725, TR:-0.00706843152359279, Period:3.34457323498419, Benefit0.967291886195996,
Cond:2556, T:24365, TR:-0.00721654054004789, Period:3.97816540119023, Benefit0.964375128257747,
Cond:3900, T:23749, TR:-0.00704971595307275, Period:3.78984378289612, Benefit0.963366878605415,
Cond:4338, T:24492, TR:-0.00723287031664752, Period:3.68561162828679, Benefit0.96182426914911,
Cond:8346, T:24675, TR:-0.00727745857593579, Period:3.46338399189463, Benefit0.961377912867275,
Cond:1422, T:23938, TR:-0.00706240237923445, Period:3.72495613668644, Benefit0.957682346060657,
Cond:8374, T:25194, TR:-0.00738169694226762, Period:3.49055330634278, Benefit0.956854806700008,
Cond:1436, T:23631, TR:-0.00697713129550245, Period:4.0108332275401, Benefit0.956751724429774,
Cond:546, T:29027, TR:-0.00833618664367063, Period:8.14314259138044, Benefit0.952044648086265,
Cond:4318, T:25077, TR:-0.00731416971539293, Period:3.61630179048531, Benefit0.951309965306855,
Cond:8334, T:24475, TR:-0.00714973659256313, Period:3.50553626149132, Benefit0.949826353421859,
Cond:92, T:24490, TR:-0.0071473527293081, Period:4.4207023274806, Benefit0.948877092690894,
Cond:544, T:28842, TR:-0.00824801210935781, Period:8.16788017474516, Benefit0.946813674502462,
Cond:6130, T:25758, TR:-0.00743832231878337, Period:3.78134948365556, Benefit0.943939746874757, , T2:8354,98,78,96,2764,526,2750,6120,8356,8368,8344,750,8358,6122,6094,6134,4316,2556,3900,4338,8346,1422,8374,1436,546,4318,8334,92,544,6130,  #End#
LowScoreRank2 , T0:6844 , T1:
Cond:99, T:25056, TR:-0.00394239309351901, Period:9.21735312899106, Benefit0.450191570881226,
Cond:4323, T:23666, TR:-0.00419285274843081, Period:10.4420265359588, Benefit0.516352573311924,
Cond:547, T:24507, TR:-0.00419460221923831, Period:9.31901089484637, Benefit0.498755457624352,
Cond:6101, T:25276, TR:-0.00437886428661442, Period:10.1966687767052, Benefit0.510642506725748,
Cond:101, T:23865, TR:-0.0044353693082939, Period:9.31866750471402, Benefit0.549926670856903,
Cond:2747, T:24091, TR:-0.00446619447919277, Period:10.5374206135071, Benefit0.549499813208252,
Cond:91, T:24021, TR:-0.00447790419433563, Period:10.2531118604554, Benefit0.552932850422547,
Cond:2761, T:23793, TR:-0.0044819188457645, Period:10.5227587946035, Benefit0.558903879292229,
Cond:2769, T:25115, TR:-0.00453069532472637, Period:10.0098347601035, Benefit0.536492136173601,
Cond:6836, T:23762, TR:-0.00463840280623212, Period:10.433254776534, Benefit0.584210083326319,
Cond:545, T:26303, TR:-0.00464800060291001, Period:9.06360491198723, Benefit0.528684940881268,
Cond:4612, T:24357, TR:-0.00464986494730372, Period:10.3116147308782, Benefit0.571581064991584,
Cond:717, T:23744, TR:-0.00469439545104421, Period:10.6038157008086, Benefit0.593455188679245,
Cond:1906, T:23882, TR:-0.00473396866687894, Period:10.4131144795243, Benefit0.596181224353069,
Cond:4614, T:24462, TR:-0.00474757186636157, Period:10.2238574114954, Benefit0.584007848908511,
Cond:6838, T:24145, TR:-0.00475494172122911, Period:10.0113481051978, Benefit0.592876371919652,
Cond:1651, T:23663, TR:-0.00477040799544814, Period:10.4120779275662, Benefit0.607488484131344,
Cond:4610, T:25072, TR:-0.00477998528398467, Period:10.1982291001914, Benefit0.574505424377792,
Cond:973, T:23926, TR:-0.00479754702060596, Period:10.5483992309621, Benefit0.604990387026666,
Cond:747, T:24987, TR:-0.00481178070290911, Period:10.1588826189619, Benefit0.581222235562493,
Cond:539, T:24349, TR:-0.00481739117773125, Period:10.2482237463551, Benefit0.597437266417512,
Cond:8147, T:24916, TR:-0.00482134066279353, Period:10.4365066623856, Benefit0.584323326376625,
Cond:2301, T:23640, TR:-0.0048231759664198, Period:10.6752115059222, Benefit0.616412859560068,
Cond:4602, T:24026, TR:-0.00482340694424342, Period:8.37309581286939, Benefit0.606467993007575,
Cond:479, T:23827, TR:-0.00482556186648348, Period:10.6244596466194, Benefit0.611910857430646,
Cond:4824, T:24765, TR:-0.00483160091375052, Period:9.15982232990107, Benefit0.589460932768019,
Cond:1869, T:24628, TR:-0.00483249577696771, Period:10.3485463699854, Benefit0.592902387526393,
Cond:3645, T:24178, TR:-0.00485496480346358, Period:10.492762015055, Benefit0.607494416411614,
Cond:3213, T:24261, TR:-0.0048934041968839, Period:10.437822018878, Benefit0.611310333456989,
Cond:1908, T:24226, TR:-0.00491507026424279, Period:10.0202262032527, Benefit0.615537026335342, , T2:99,4323,547,6101,101,2747,91,2761,2769,6836,545,4612,717,1906,4614,6838,1651,4610,973,747,539,8147,2301,4602,479,4824,1869,3645,3213,1908,  #End#
LowScoreRank3 , T0:6844 , T1:
Cond:1, T:39361, TR:-0.00909816406349036, Period:7.58654505729021, Benefit0.771829983994309,
Cond:3, T:39349, TR:-0.00912854751062902, Period:7.58857912526367, Benefit0.774937101324049,
Cond:5, T:39334, TR:-0.00913470085521613, Period:7.59020186098541, Benefit0.775817359027813,
Cond:6, T:68, TR:-0.000909353271450174, Period:2.11764705882353, Benefit-0.147058823529412,
Cond:7, T:39333, TR:-0.00912258714436317, Period:7.59019144230036, Benefit0.774693005872931,
Cond:8, T:585, TR:-0.00107941692261755, Period:1.63931623931624, Benefit1.07350427350427,
Cond:9, T:38986, TR:-0.00903830997389596, Period:7.64338480480172, Benefit0.77363669009388,
Cond:10, T:1730, TR:-0.00138562213561067, Period:1.90462427745665, Benefit1.02658959537572,
Cond:11, T:38210, TR:-0.00887983134176883, Period:7.76328186338655, Benefit0.774116723370845,
Cond:12, T:5711, TR:-0.00255961805783939, Period:2.45228506391175, Benefit1.08072141481352,
Cond:13, T:35059, TR:-0.00784134746911617, Period:8.24792492655238, Benefit0.734305028665963,
Cond:14, T:10675, TR:-0.00360755738178217, Period:2.69021077283372, Benefit0.944543325526932,
Cond:15, T:30623, TR:-0.00680361618714897, Period:9.04088430264834, Benefit0.715605917121118,
Cond:17, T:39355, TR:-0.00909115171747995, Period:7.58754923135561, Benefit0.771287002922119,
Cond:18, T:323, TR:-0.00106549178479626, Period:2.21362229102167, Benefit1.78328173374613,
Cond:19, T:39298, TR:-0.00912384689368143, Period:7.59590818871189, Benefit0.775510204081633,
Cond:20, T:529, TR:-0.00115637805142132, Period:2.24952741020794, Benefit1.73345935727788,
Cond:21, T:39163, TR:-0.00918864404908547, Period:7.61440645507239, Benefit0.78436279141026,
Cond:22, T:675, TR:-0.00115216114171526, Period:2.4162962962963, Benefit1.33481481481481,
Cond:23, T:39091, TR:-0.00919438667418413, Period:7.6234938988514, Benefit0.786370264255199,
Cond:24, T:2969, TR:-0.00219739829852313, Period:2.07713034691815, Benefit1.62344223644325,
Cond:25, T:37489, TR:-0.00840600453274159, Period:7.87078876470431, Benefit0.74219104270586,
Cond:26, T:5435, TR:-0.0025893006821532, Period:2.35657773689052, Benefit1.15620975160994,
Cond:27, T:35502, TR:-0.00787024303809448, Period:8.17787730268717, Benefit0.728071657934764,
Cond:28, T:12397, TR:-0.0045227804362284, Period:2.8990884891506, Benefit1.08913446801646,
Cond:29, T:29117, TR:-0.00602679769787123, Period:9.3204313631212, Benefit0.653570079335096,
Cond:30, T:17753, TR:-0.00598412099114482, Period:3.03774010026474, Benefit1.06680561031938,
Cond:31, T:23226, TR:-0.00437431492472519, Period:10.8071557737019, Benefit0.555368983036252,
Cond:33, T:39329, TR:-0.00913549597866484, Period:7.59129395611381, Benefit0.775992270334867,
Cond:34, T:884, TR:-0.00132590660100874, Period:2.53733031674208, Benefit1.75678733031674, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:6844 , T1:
Cond:1, T:39361, TR:-0.00909816406349036, Period:7.58654505729021, Benefit0.771829983994309,
Cond:3, T:39349, TR:-0.00912854751062902, Period:7.58857912526367, Benefit0.774937101324049,
Cond:5, T:39334, TR:-0.00913470085521613, Period:7.59020186098541, Benefit0.775817359027813,
Cond:6, T:68, TR:-0.000909353271450174, Period:2.11764705882353, Benefit-0.147058823529412,
Cond:7, T:39333, TR:-0.00912258714436317, Period:7.59019144230036, Benefit0.774693005872931,
Cond:8, T:585, TR:-0.00107941692261755, Period:1.63931623931624, Benefit1.07350427350427,
Cond:9, T:38986, TR:-0.00903830997389596, Period:7.64338480480172, Benefit0.77363669009388,
Cond:10, T:1730, TR:-0.00138562213561067, Period:1.90462427745665, Benefit1.02658959537572,
Cond:11, T:38210, TR:-0.00887983134176883, Period:7.76328186338655, Benefit0.774116723370845,
Cond:12, T:5711, TR:-0.00255961805783939, Period:2.45228506391175, Benefit1.08072141481352,
Cond:13, T:35059, TR:-0.00784134746911617, Period:8.24792492655238, Benefit0.734305028665963,
Cond:14, T:10675, TR:-0.00360755738178217, Period:2.69021077283372, Benefit0.944543325526932,
Cond:15, T:30623, TR:-0.00680361618714897, Period:9.04088430264834, Benefit0.715605917121118,
Cond:17, T:39355, TR:-0.00909115171747995, Period:7.58754923135561, Benefit0.771287002922119,
Cond:18, T:323, TR:-0.00106549178479626, Period:2.21362229102167, Benefit1.78328173374613,
Cond:19, T:39298, TR:-0.00912384689368143, Period:7.59590818871189, Benefit0.775510204081633,
Cond:20, T:529, TR:-0.00115637805142132, Period:2.24952741020794, Benefit1.73345935727788,
Cond:21, T:39163, TR:-0.00918864404908547, Period:7.61440645507239, Benefit0.78436279141026,
Cond:22, T:675, TR:-0.00115216114171526, Period:2.4162962962963, Benefit1.33481481481481,
Cond:23, T:39091, TR:-0.00919438667418413, Period:7.6234938988514, Benefit0.786370264255199,
Cond:24, T:2969, TR:-0.00219739829852313, Period:2.07713034691815, Benefit1.62344223644325,
Cond:25, T:37489, TR:-0.00840600453274159, Period:7.87078876470431, Benefit0.74219104270586,
Cond:26, T:5435, TR:-0.0025893006821532, Period:2.35657773689052, Benefit1.15620975160994,
Cond:27, T:35502, TR:-0.00787024303809448, Period:8.17787730268717, Benefit0.728071657934764,
Cond:28, T:12397, TR:-0.0045227804362284, Period:2.8990884891506, Benefit1.08913446801646,
Cond:29, T:29117, TR:-0.00602679769787123, Period:9.3204313631212, Benefit0.653570079335096,
Cond:30, T:17753, TR:-0.00598412099114482, Period:3.03774010026474, Benefit1.06680561031938,
Cond:31, T:23226, TR:-0.00437431492472519, Period:10.8071557737019, Benefit0.555368983036252,
Cond:33, T:39329, TR:-0.00913549597866484, Period:7.59129395611381, Benefit0.775992270334867,
Cond:34, T:884, TR:-0.00132590660100874, Period:2.53733031674208, Benefit1.75678733031674, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:4606 , T1:
Cond:8354, T:25536, TR:-0.00781220749782134, Period:3.43930137844612, Benefit1.00681390977444,
Cond:78, T:26538, TR:-0.00804041254518087, Period:3.85989901273645, Benefit1.00056522722134,
Cond:2764, T:24939, TR:-0.00760344745702821, Period:3.38939011187297, Benefit0.999879706483821,
Cond:4316, T:25319, TR:-0.00764283254115022, Period:3.35435838698211, Benefit0.990560448674908,
Cond:3900, T:24740, TR:-0.00748588822850366, Period:3.74765561843169, Benefit0.990258690379952,
Cond:6122, T:26070, TR:-0.00782834444821091, Period:3.37602608362102, Benefit0.988339087073264,
Cond:526, T:26002, TR:-0.00780192036866795, Period:3.75205753403584, Benefit0.98715483424352,
Cond:98, T:31640, TR:-0.00926435052424685, Period:7.77000632111252, Benefit0.981953223767383,
Cond:1630, T:25369, TR:-0.0075919900705641, Period:3.41869998817454, Benefit0.981118688162718,
Cond:6120, T:25349, TR:-0.00757049897400133, Period:3.34624640025248, Benefit0.97873683380015,
Cond:2556, T:25182, TR:-0.00750554361203999, Period:3.91315225160829, Benefit0.975657215471368,
Cond:8356, T:26406, TR:-0.00780035189460585, Period:3.39843217450579, Benefit0.971710974778459,
Cond:96, T:33172, TR:-0.00955835296102817, Period:7.70885083805619, Benefit0.969160737971783,
Cond:2750, T:26193, TR:-0.00769558020249991, Period:3.48314435154431, Benefit0.964761577520712,
Cond:4302, T:25170, TR:-0.00740953360309471, Period:3.32324195470799, Benefit0.961899086213747,
Cond:8358, T:26767, TR:-0.00782363868535312, Period:3.41965853476295, Benefit0.961743938431651,
Cond:8368, T:26730, TR:-0.0077996886482218, Period:3.57796483352039, Benefit0.959745604190049,
Cond:750, T:27381, TR:-0.0079539681454485, Period:3.93071838135934, Benefit0.95774442131405,
Cond:92, T:25322, TR:-0.00740634991972252, Period:4.29472395545376, Benefit0.955611721033094,
Cond:8330, T:24944, TR:-0.00728817124555824, Period:3.2910519563823, Benefit0.95253367543297,
Cond:8344, T:26461, TR:-0.00767387195182953, Period:3.4109444087525, Benefit0.951853671440989,
Cond:6094, T:26005, TR:-0.00754340404856041, Period:3.41284368390694, Benefit0.949971159392424,
Cond:1422, T:26009, TR:-0.00751659441488684, Period:3.71575223960937, Benefit0.945980237610058,
Cond:1198, T:24769, TR:-0.00718193451626316, Period:3.59727885663531, Benefit0.94331624207679,
Cond:8152, T:26953, TR:-0.00771297553684575, Period:3.65833116907209, Benefit0.939746966942455,
Cond:6134, T:26653, TR:-0.00762759624519176, Period:3.45124376242824, Benefit0.938468465088358,
Cond:7930, T:25071, TR:-0.00722514549166648, Period:3.52044194487655, Benefit0.9382952415141,
Cond:5914, T:25702, TR:-0.0073813617220282, Period:3.79596918527741, Benefit0.93774803517236,
Cond:4338, T:26433, TR:-0.00755607111904053, Period:3.64888586236901, Benefit0.93625392501797,
Cond:1886, T:25337, TR:-0.00726829330624159, Period:4.1836839404823, Benefit0.934719974740498, , T2:8354,78,2764,4316,3900,6122,526,98,1630,6120,2556,8356,96,2750,4302,8358,8368,750,92,8330,8344,6094,1422,1198,8152,6134,7930,5914,4338,1886,  #End#
LowScoreRank2 , T0:4606 , T1:
Cond:99, T:26158, TR:-0.00372954423921509, Period:8.68166526492851, Benefit0.400718709381451,
Cond:6101, T:25950, TR:-0.0038049609947717, Period:9.72042389210019, Benefit0.414797687861272,
Cond:547, T:25471, TR:-0.00392164884626677, Period:8.78226218051902, Benefit0.439755015507832,
Cond:91, T:24717, TR:-0.00403103881673994, Period:9.72881013067929, Benefit0.469798114657928,
Cond:4610, T:25528, TR:-0.00404934469471689, Period:9.7704873080539, Benefit0.457419304293325,
Cond:2769, T:25951, TR:-0.00409753885010063, Period:9.50102115525413, Benefit0.456822473122423,
Cond:8147, T:25553, TR:-0.00416681911537532, Period:9.96724455054201, Benefit0.47411262865417,
Cond:1869, T:25305, TR:-0.004209710429134, Period:9.83841138114997, Benefit0.485121517486663,
Cond:3645, T:24847, TR:-0.00423359005951412, Period:9.9782267476959, Benefit0.497726083631827,
Cond:479, T:24709, TR:-0.00426077798238529, Period:10.0373548099883, Benefit0.504633939050548,
Cond:3213, T:24911, TR:-0.00426208112172334, Period:9.92991048131348, Benefit0.500702500903215,
Cond:101, T:24810, TR:-0.00427134890334674, Period:8.78460298266828, Benefit0.504151551793632,
Cond:45, T:26067, TR:-0.00428485695658599, Period:9.64165419879541, Benefit0.481566731883224,
Cond:539, T:24996, TR:-0.00429864045105888, Period:9.75436069771163, Benefit0.504440710513682,
Cond:747, T:25706, TR:-0.00432493364010319, Period:9.65735625923909, Benefit0.494203687854975,
Cond:545, T:27418, TR:-0.00432638349464033, Period:8.53180392442921, Benefit0.46327230286673,
Cond:6628, T:27031, TR:-0.00433371363210211, Period:9.31952203026155, Benefit0.470977766268359,
Cond:8325, T:27153, TR:-0.00436792442806392, Period:9.39008581003941, Benefit0.473538835487791,
Cond:3250, T:26739, TR:-0.00440096810345729, Period:9.45282172108157, Benefit0.485545457945323,
Cond:6605, T:24742, TR:-0.00441201627390256, Period:9.39932099264409, Benefit0.526756123191335,
Cond:1181, T:24744, TR:-0.00441657610675411, Period:10.0146298092467, Benefit0.527400581959263,
Cond:7713, T:24800, TR:-0.00443943009142417, Period:9.51193548387097, Benefit0.529637096774194,
Cond:285, T:25567, TR:-0.00444758306596995, Period:9.76942934251183, Benefit0.514804239840419,
Cond:3044, T:27153, TR:-0.00445564530292272, Period:9.29853054911059, Benefit0.485581703679151,
Cond:3046, T:26865, TR:-0.00446361649572253, Period:9.29022892238973, Benefit0.491941187418574,
Cond:8339, T:26853, TR:-0.00448828002112642, Period:9.38963989125982, Benefit0.495587085241872,
Cond:5867, T:25423, TR:-0.0044970163350595, Period:9.83782401762184, Benefit0.524997049915431,
Cond:4061, T:25291, TR:-0.00450061093282246, Period:9.88371357399866, Benefit0.528290696295125,
Cond:3853, T:25622, TR:-0.0045015466944684, Period:9.77441261415971, Benefit0.521543985637343,
Cond:2509, T:25226, TR:-0.00450240934347903, Period:9.89752636169032, Benefit0.529929437881551, , T2:99,6101,547,91,4610,2769,8147,1869,3645,479,3213,101,45,539,747,545,6628,8325,3250,6605,1181,7713,285,3044,3046,8339,5867,4061,3853,2509,  #End#
LowScoreRank3 , T0:4606 , T1:
Cond:1, T:41154, TR:-0.00879424148861577, Period:7.19298245614035, Benefit0.710380521941974,
Cond:3, T:41145, TR:-0.00881279982422038, Period:7.19436140478795, Benefit0.712212905577834,
Cond:5, T:41127, TR:-0.00883160593673418, Period:7.19627009020838, Benefit0.714226663748875,
Cond:6, T:67, TR:-0.000906423460624866, Period:2.13432835820896, Benefit-0.313432835820896,
Cond:7, T:41126, TR:-0.00882594891215372, Period:7.19610465399018, Benefit0.713733404658853,
Cond:8, T:638, TR:-0.00111885417052896, Period:1.6551724137931, Benefit1.21630094043887,
Cond:9, T:40750, TR:-0.0087049736552458, Period:7.248, Benefit0.709374233128834,
Cond:10, T:1975, TR:-0.00147204557528809, Period:1.91139240506329, Benefit1.06329113924051,
Cond:11, T:39851, TR:-0.0085136360847206, Period:7.3726631703094, Benefit0.707736317783744,
Cond:12, T:6535, TR:-0.00281789406381633, Period:2.44728385615914, Benefit1.09227237949503,
Cond:13, T:36126, TR:-0.00733482559562865, Period:7.88830759010131, Benefit0.660244699108675,
Cond:14, T:11380, TR:-0.00380751520953887, Period:2.68163444639719, Benefit0.951581722319859,
Cond:15, T:31858, TR:-0.00629553579290835, Period:8.5701236738025, Benefit0.628225249544855,
Cond:17, T:41149, TR:-0.0087864208025501, Period:7.19373496318258, Benefit0.709762084133272,
Cond:18, T:331, TR:-0.00106096622156288, Period:2.20845921450151, Benefit1.68882175226586,
Cond:19, T:41091, TR:-0.00883852125171153, Period:7.20162566012022, Benefit0.715485142731985,
Cond:20, T:553, TR:-0.00117742779662859, Period:2.26943942133816, Benefit1.80108499095841,
Cond:21, T:40958, TR:-0.00886001452693752, Period:7.21768641046926, Benefit0.719786122369256,
Cond:22, T:715, TR:-0.00116282823345021, Period:2.39300699300699, Benefit1.31608391608392,
Cond:23, T:40868, TR:-0.00885118382086463, Period:7.22792894195948, Benefit0.720588235294118,
Cond:24, T:3143, TR:-0.00232704000034117, Period:2.07158765510659, Benefit1.68819599109131,
Cond:25, T:39157, TR:-0.00800608871960253, Period:7.46451464616799, Benefit0.672268049135531,
Cond:26, T:6080, TR:-0.00279221064840378, Period:2.35986842105263, Benefit1.15838815789474,
Cond:27, T:36808, TR:-0.00742492654323013, Period:7.78944794609867, Benefit0.656976744186046,
Cond:28, T:13510, TR:-0.00489190501890214, Period:2.90022205773501, Benefit1.10125832716506,
Cond:29, T:29876, TR:-0.00534158459494069, Period:8.90018744142455, Benefit0.551379033337796,
Cond:30, T:18814, TR:-0.00632280535064635, Period:3.02524715637291, Benefit1.07356224088445,
Cond:31, T:24101, TR:-0.00375831253529753, Period:10.1978756068213, Benefit0.439691299116219,
Cond:33, T:41121, TR:-0.00881381994606552, Period:7.19732010408307, Benefit0.712725857834197,
Cond:34, T:890, TR:-0.00130566362670605, Period:2.51123595505618, Benefit1.65955056179775, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:4606 , T1:
Cond:1, T:41154, TR:-0.00879424148861577, Period:7.19298245614035, Benefit0.710380521941974,
Cond:3, T:41145, TR:-0.00881279982422038, Period:7.19436140478795, Benefit0.712212905577834,
Cond:5, T:41127, TR:-0.00883160593673418, Period:7.19627009020838, Benefit0.714226663748875,
Cond:6, T:67, TR:-0.000906423460624866, Period:2.13432835820896, Benefit-0.313432835820896,
Cond:7, T:41126, TR:-0.00882594891215372, Period:7.19610465399018, Benefit0.713733404658853,
Cond:8, T:638, TR:-0.00111885417052896, Period:1.6551724137931, Benefit1.21630094043887,
Cond:9, T:40750, TR:-0.0087049736552458, Period:7.248, Benefit0.709374233128834,
Cond:10, T:1975, TR:-0.00147204557528809, Period:1.91139240506329, Benefit1.06329113924051,
Cond:11, T:39851, TR:-0.0085136360847206, Period:7.3726631703094, Benefit0.707736317783744,
Cond:12, T:6535, TR:-0.00281789406381633, Period:2.44728385615914, Benefit1.09227237949503,
Cond:13, T:36126, TR:-0.00733482559562865, Period:7.88830759010131, Benefit0.660244699108675,
Cond:14, T:11380, TR:-0.00380751520953887, Period:2.68163444639719, Benefit0.951581722319859,
Cond:15, T:31858, TR:-0.00629553579290835, Period:8.5701236738025, Benefit0.628225249544855,
Cond:17, T:41149, TR:-0.0087864208025501, Period:7.19373496318258, Benefit0.709762084133272,
Cond:18, T:331, TR:-0.00106096622156288, Period:2.20845921450151, Benefit1.68882175226586,
Cond:19, T:41091, TR:-0.00883852125171153, Period:7.20162566012022, Benefit0.715485142731985,
Cond:20, T:553, TR:-0.00117742779662859, Period:2.26943942133816, Benefit1.80108499095841,
Cond:21, T:40958, TR:-0.00886001452693752, Period:7.21768641046926, Benefit0.719786122369256,
Cond:22, T:715, TR:-0.00116282823345021, Period:2.39300699300699, Benefit1.31608391608392,
Cond:23, T:40868, TR:-0.00885118382086463, Period:7.22792894195948, Benefit0.720588235294118,
Cond:24, T:3143, TR:-0.00232704000034117, Period:2.07158765510659, Benefit1.68819599109131,
Cond:25, T:39157, TR:-0.00800608871960253, Period:7.46451464616799, Benefit0.672268049135531,
Cond:26, T:6080, TR:-0.00279221064840378, Period:2.35986842105263, Benefit1.15838815789474,
Cond:27, T:36808, TR:-0.00742492654323013, Period:7.78944794609867, Benefit0.656976744186046,
Cond:28, T:13510, TR:-0.00489190501890214, Period:2.90022205773501, Benefit1.10125832716506,
Cond:29, T:29876, TR:-0.00534158459494069, Period:8.90018744142455, Benefit0.551379033337796,
Cond:30, T:18814, TR:-0.00632280535064635, Period:3.02524715637291, Benefit1.07356224088445,
Cond:31, T:24101, TR:-0.00375831253529753, Period:10.1978756068213, Benefit0.439691299116219,
Cond:33, T:41121, TR:-0.00881381994606552, Period:7.19732010408307, Benefit0.712725857834197,
Cond:34, T:890, TR:-0.00130566362670605, Period:2.51123595505618, Benefit1.65955056179775, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:6142 , T1:
Cond:6106, T:33701, TR:-0.00921036068671673, Period:3.227589685766, Benefit0.915373431055458,
Cond:526, T:32893, TR:-0.00893162776824137, Period:3.65953242331195, Benefit0.906545465600584,
Cond:78, T:34850, TR:-0.0093873973348645, Period:3.77308464849354, Benefit0.903787661406026,
Cond:6114, T:33075, TR:-0.0088914928866619, Period:3.48734693877551, Benefit0.896991685563114,
Cond:2750, T:33339, TR:-0.00890314055740798, Period:3.39035363988122, Benefit0.891118509853325,
Cond:2764, T:34195, TR:-0.00910595801478811, Period:3.39918116683726, Benefit0.890656528732271,
Cond:8342, T:33775, TR:-0.00894234437833217, Period:3.19493708364175, Benefit0.883819393042191,
Cond:540, T:33503, TR:-0.00880034547156259, Period:3.92964809121571, Benefit0.875294749723905,
Cond:2556, T:33320, TR:-0.00875262081392618, Period:3.76746698679472, Benefit0.874819927971188,
Cond:96, T:44247, TR:-0.01134776409993, Period:6.37647750129952, Benefit0.874251361674238,
Cond:4316, T:33552, TR:-0.00879723669845276, Period:3.29902837386743, Benefit0.873658798283262,
Cond:750, T:36101, TR:-0.00935237074259469, Period:3.8296446081826, Benefit0.868535497631645,
Cond:4330, T:34180, TR:-0.00882694199219181, Period:3.38080748976009, Benefit0.860678759508484,
Cond:8354, T:37464, TR:-0.00958256986481492, Period:3.53974482169549, Benefit0.859438394191757,
Cond:92, T:35155, TR:-0.00901125808075907, Period:4.11688237804011, Benefit0.856065993457545,
Cond:1436, T:32939, TR:-0.00849325797277608, Period:3.82309724035338, Benefit0.85573332523756,
Cond:98, T:42887, TR:-0.0107886494667698, Period:6.42234243477044, Benefit0.853941753911442,
Cond:4328, T:33463, TR:-0.00858554806373798, Period:3.38242237695365, Benefit0.852463915369214,
Cond:6128, T:36476, TR:-0.00925757407924355, Period:3.56149248821143, Benefit0.849846474394122,
Cond:8330, T:33235, TR:-0.00849339608469467, Period:3.20054159771325, Benefit0.848051752670378,
Cond:6122, T:37323, TR:-0.0094302251318942, Period:3.44066661308041, Benefit0.847547088926399,
Cond:6116, T:35044, TR:-0.00889673702055087, Period:3.39570254537153, Benefit0.846649925807556,
Cond:6120, T:36727, TR:-0.00927837029884951, Period:3.43069131701473, Benefit0.846080540202031,
Cond:8344, T:37276, TR:-0.00938948841648263, Period:3.39253138748793, Benefit0.844564867475051,
Cond:6412, T:33473, TR:-0.00848560910385383, Period:6.70367161592926, Benefit0.841095808562125,
Cond:4336, T:32773, TR:-0.00831314257036259, Period:3.50645348305007, Benefit0.839654593720441,
Cond:4318, T:35323, TR:-0.00889202737848505, Period:3.55380347082637, Benefit0.839396427257028,
Cond:764, T:36326, TR:-0.00907836797900263, Period:4.14193690469636, Benefit0.835049276000661,
Cond:8334, T:34725, TR:-0.0086900690674409, Period:3.53402447804176, Benefit0.832368610511159,
Cond:1646, T:37383, TR:-0.00927812459922696, Period:3.89727951207768, Benefit0.831046197469438, , T2:6106,526,78,6114,2750,2764,8342,540,2556,96,4316,750,4330,8354,92,1436,98,4328,6128,8330,6122,6116,6120,8344,6412,4336,4318,764,8334,1646,  #End#
LowScoreRank2 , T0:6142 , T1:
Cond:2093, T:32819, TR:-0.00560749068514887, Period:8.04043389499985, Benefit0.531612785276821,
Cond:703, T:33227, TR:-0.00565888922977629, Period:7.99957865591236, Benefit0.530773166400819,
Cond:1627, T:33380, TR:-0.00567336342652679, Period:7.94203115638107, Benefit0.529928100659077,
Cond:5021, T:32800, TR:-0.00568859935853175, Period:7.89804878048781, Benefit0.541128048780488,
Cond:75, T:33334, TR:-0.00569505841433896, Period:7.78934421311574, Benefit0.533089338213236,
Cond:4077, T:33339, TR:-0.0058237759813325, Period:7.99223132067549, Benefit0.547376945919194,
Cond:717, T:33149, TR:-0.00582858381234354, Period:7.99182479109475, Benefit0.551087513952155,
Cond:3437, T:34443, TR:-0.00584566301163598, Period:7.78262636820254, Benefit0.532009406846094,
Cond:3869, T:33719, TR:-0.00586521809744094, Period:7.9221210593434, Benefit0.545716065126487,
Cond:3215, T:33046, TR:-0.00586791910237947, Period:7.85583731767839, Benefit0.557253525388852,
Cond:2079, T:32954, TR:-0.00587099818911254, Period:7.99089640104388, Benefit0.559173393214784,
Cond:2745, T:34861, TR:-0.00587535192155967, Period:7.7679928860331, Benefit0.528728378417142,
Cond:5437, T:33953, TR:-0.00588198068387687, Period:7.83503666833564, Benefit0.543751656701912,
Cond:537, T:33884, TR:-0.00588617152906663, Period:7.65119230315193, Benefit0.545331129736749,
Cond:4297, T:35287, TR:-0.00589323097247849, Period:7.79867940034574, Benefit0.524159038739479,
Cond:8325, T:36099, TR:-0.00590150929106722, Period:7.59032106152525, Benefit0.513089005235602,
Cond:31, T:35088, TR:-0.00592571317904897, Period:7.73700410396717, Benefit0.530608755129959,
Cond:4589, T:33620, TR:-0.00592606280550759, Period:7.4296252230815, Benefit0.5540749553837,
Cond:1405, T:32690, TR:-0.00595413461085875, Period:8.07834200061181, Benefit0.573202814316305,
Cond:5229, T:33697, TR:-0.00597455706549708, Period:7.84532747722349, Benefit0.558150577202718,
Cond:327, T:33273, TR:-0.00598113130838698, Period:7.08279986776065, Benefit0.566074595016981,
Cond:8339, T:34294, TR:-0.00599191097330747, Period:7.70936606986645, Benefit0.550212865224238,
Cond:959, T:33745, TR:-0.00600123887592542, Period:7.86981775077789, Benefit0.560290413394577,
Cond:6829, T:33730, TR:-0.00600820176095815, Period:7.13112955825674, Benefit0.561310406166617,
Cond:745, T:34031, TR:-0.00601352539456657, Period:7.65390379359995, Benefit0.556874614322236,
Cond:5645, T:34344, TR:-0.00601913808041947, Period:7.8088749126485, Benefit0.552352667132541,
Cond:1391, T:32796, TR:-0.0060281873979404, Period:8.03543115014026, Benefit0.579735333577265,
Cond:3647, T:33504, TR:-0.00604062040177547, Period:7.85094317096466, Benefit0.56873806112703,
Cond:5855, T:33016, TR:-0.00605863612431385, Period:7.98203901138842, Benefit0.579264598982312,
Cond:8147, T:34162, TR:-0.00606238356620916, Period:8.02584743282009, Benefit0.560037468532287, , T2:2093,703,1627,5021,75,4077,717,3437,3869,3215,2079,2745,5437,537,4297,8325,31,4589,1405,5229,327,8339,959,6829,745,5645,1391,3647,5855,8147,  #End#
LowScoreRank3 , T0:6142 , T1:
Cond:1, T:54367, TR:-0.0112382765855276, Period:6.00457998418158, Benefit0.701951551492633,
Cond:3, T:54350, TR:-0.0112833535552688, Period:6.00614535418583, Benefit0.70524379024839,
Cond:5, T:54311, TR:-0.011310527696912, Period:6.00872751376333, Benefit0.707609876452284,
Cond:6, T:149, TR:-0.000929613986811859, Period:1.95973154362416, Benefit0.442953020134228,
Cond:7, T:54309, TR:-0.01130538724961, Period:6.00858052993058, Benefit0.707286085179252,
Cond:8, T:1163, TR:-0.00124608449584586, Period:1.65692175408426, Benefit1.07738607050731,
Cond:9, T:53658, TR:-0.0112269376628514, Period:6.06237653285624, Benefit0.710593015021059,
Cond:10, T:3045, TR:-0.00161639473314567, Period:1.87717569786535, Benefit0.866995073891626,
Cond:11, T:52315, TR:-0.0109895140517224, Period:6.17276115836758, Benefit0.712319602408487,
Cond:12, T:7876, TR:-0.00302973731908909, Period:2.35436769933977, Benefit1.00660233621127,
Cond:13, T:48276, TR:-0.00969442591262636, Period:6.49256359267545, Benefit0.673398790289171,
Cond:14, T:12560, TR:-0.00398320617498315, Period:2.61616242038217, Benefit0.914171974522293,
Cond:15, T:44157, TR:-0.00868143592054284, Period:6.83302760604208, Benefit0.651991756686369,
Cond:17, T:54359, TR:-0.0112285239459137, Period:6.00531650692618, Benefit0.701392593682739,
Cond:18, T:685, TR:-0.00131464885124691, Period:2.17080291970803, Benefit2.20583941605839,
Cond:19, T:54231, TR:-0.0113100129658128, Period:6.01528645977393, Benefit0.708635282403054,
Cond:20, T:1089, TR:-0.00144932078958985, Period:2.26813590449954, Benefit1.85123966942149,
Cond:21, T:53963, TR:-0.0113591814885098, Period:6.03393065619035, Benefit0.715582899394029,
Cond:22, T:1410, TR:-0.00140228844729525, Period:2.38085106382979, Benefit1.30425531914894,
Cond:23, T:53813, TR:-0.0112936048632383, Period:6.04381840819133, Benefit0.713099065281623,
Cond:24, T:5526, TR:-0.00301636973563488, Period:2.08595729279768, Benefit1.42689106044155,
Cond:25, T:50776, TR:-0.0101088941829481, Period:6.29580904364267, Benefit0.670001575547503,
Cond:26, T:9190, TR:-0.00336718624042101, Period:2.32992383025027, Benefit0.999782372143634,
Cond:27, T:47624, TR:-0.00964227940512432, Period:6.55226356458928, Benefit0.678691416092726,
Cond:28, T:16810, TR:-0.00538310511396772, Period:2.7743010113028, Benefit0.993337299226651,
Cond:29, T:40415, TR:-0.00749109524128739, Period:7.19584312755165, Benefit0.60376097983422,
Cond:30, T:21665, TR:-0.00676827223247699, Period:2.93357950611585, Benefit1.00821601661666,
Cond:31, T:35088, TR:-0.00592571317904897, Period:7.73700410396717, Benefit0.530608755129959,
Cond:33, T:54304, TR:-0.0112799705646301, Period:6.00942840306423, Benefit0.705620212139069,
Cond:34, T:1847, TR:-0.00183037160871496, Period:2.63616675690309, Benefit1.8651867893882, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:6142 , T1:
Cond:1, T:54367, TR:-0.0112382765855276, Period:6.00457998418158, Benefit0.701951551492633,
Cond:3, T:54350, TR:-0.0112833535552688, Period:6.00614535418583, Benefit0.70524379024839,
Cond:5, T:54311, TR:-0.011310527696912, Period:6.00872751376333, Benefit0.707609876452284,
Cond:6, T:149, TR:-0.000929613986811859, Period:1.95973154362416, Benefit0.442953020134228,
Cond:7, T:54309, TR:-0.01130538724961, Period:6.00858052993058, Benefit0.707286085179252,
Cond:8, T:1163, TR:-0.00124608449584586, Period:1.65692175408426, Benefit1.07738607050731,
Cond:9, T:53658, TR:-0.0112269376628514, Period:6.06237653285624, Benefit0.710593015021059,
Cond:10, T:3045, TR:-0.00161639473314567, Period:1.87717569786535, Benefit0.866995073891626,
Cond:11, T:52315, TR:-0.0109895140517224, Period:6.17276115836758, Benefit0.712319602408487,
Cond:12, T:7876, TR:-0.00302973731908909, Period:2.35436769933977, Benefit1.00660233621127,
Cond:13, T:48276, TR:-0.00969442591262636, Period:6.49256359267545, Benefit0.673398790289171,
Cond:14, T:12560, TR:-0.00398320617498315, Period:2.61616242038217, Benefit0.914171974522293,
Cond:15, T:44157, TR:-0.00868143592054284, Period:6.83302760604208, Benefit0.651991756686369,
Cond:17, T:54359, TR:-0.0112285239459137, Period:6.00531650692618, Benefit0.701392593682739,
Cond:18, T:685, TR:-0.00131464885124691, Period:2.17080291970803, Benefit2.20583941605839,
Cond:19, T:54231, TR:-0.0113100129658128, Period:6.01528645977393, Benefit0.708635282403054,
Cond:20, T:1089, TR:-0.00144932078958985, Period:2.26813590449954, Benefit1.85123966942149,
Cond:21, T:53963, TR:-0.0113591814885098, Period:6.03393065619035, Benefit0.715582899394029,
Cond:22, T:1410, TR:-0.00140228844729525, Period:2.38085106382979, Benefit1.30425531914894,
Cond:23, T:53813, TR:-0.0112936048632383, Period:6.04381840819133, Benefit0.713099065281623,
Cond:24, T:5526, TR:-0.00301636973563488, Period:2.08595729279768, Benefit1.42689106044155,
Cond:25, T:50776, TR:-0.0101088941829481, Period:6.29580904364267, Benefit0.670001575547503,
Cond:26, T:9190, TR:-0.00336718624042101, Period:2.32992383025027, Benefit0.999782372143634,
Cond:27, T:47624, TR:-0.00964227940512432, Period:6.55226356458928, Benefit0.678691416092726,
Cond:28, T:16810, TR:-0.00538310511396772, Period:2.7743010113028, Benefit0.993337299226651,
Cond:29, T:40415, TR:-0.00749109524128739, Period:7.19584312755165, Benefit0.60376097983422,
Cond:30, T:21665, TR:-0.00676827223247699, Period:2.93357950611585, Benefit1.00821601661666,
Cond:31, T:35088, TR:-0.00592571317904897, Period:7.73700410396717, Benefit0.530608755129959,
Cond:33, T:54304, TR:-0.0112799705646301, Period:6.00942840306423, Benefit0.705620212139069,
Cond:34, T:1847, TR:-0.00183037160871496, Period:2.63616675690309, Benefit1.8651867893882, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:110 , T1:
Cond:78, T:34850, TR:-0.0093873973348645, Period:3.77308464849354, Benefit0.903787661406026,
Cond:96, T:45827, TR:-0.0120077507615813, Period:6.99463198551073, Benefit0.897134876819342,
Cond:2764, T:34171, TR:-0.00910804940233262, Period:3.42837493781277, Benefit0.891516197945626,
Cond:8354, T:37248, TR:-0.00978994082586847, Period:3.58048754295533, Benefit0.885175042955326,
Cond:4330, T:34172, TR:-0.00903548513322981, Period:3.41750556010769, Benefit0.883588903195599,
Cond:6128, T:36314, TR:-0.00951156950356212, Period:3.63072093407501, Benefit0.879688274494685,
Cond:8334, T:33497, TR:-0.00875464941504368, Period:3.45514523688689, Benefit0.870376451622533,
Cond:750, T:35978, TR:-0.0093265162870391, Period:3.83320362443716, Benefit0.868864305964756,
Cond:8344, T:36658, TR:-0.00948044499476632, Period:3.38935566588466, Benefit0.868187026024333,
Cond:6116, T:34910, TR:-0.00906362192444252, Period:3.43142366084217, Benefit0.867716986536809,
Cond:4328, T:33488, TR:-0.00872560689074375, Period:3.41662685140946, Benefit0.867385332059245,
Cond:6122, T:37051, TR:-0.00956348413310435, Period:3.4689212167013, Benefit0.867210061806699,
Cond:6120, T:36557, TR:-0.00942989607213348, Period:3.46283885439177, Benefit0.865470361353503,
Cond:6412, T:35259, TR:-0.00912334920463799, Period:7.52939674976602, Benefit0.865339345982586,
Cond:6385, T:34077, TR:-0.00882884771349457, Period:3.51116588901605, Benefit0.863514980778824,
Cond:4318, T:34564, TR:-0.00893348909906186, Period:3.5259807892605, Benefit0.862486980673533,
Cond:98, T:44949, TR:-0.0113288867255697, Period:7.05806580791564, Benefit0.858862266123829,
Cond:540, T:33983, TR:-0.00874047402794429, Period:4.04755318835889, Benefit0.856251655239384,
Cond:6110, T:35994, TR:-0.00918215856550822, Period:3.69697727399011, Benefit0.853558926487748,
Cond:8356, T:38169, TR:-0.00964815347307853, Period:3.51397731143074, Benefit0.849773376300139,
Cond:6636, T:39850, TR:-0.0100134846693276, Period:7.38042659974906, Benefit0.847578419071518,
Cond:6118, T:35346, TR:-0.00897219979674906, Period:3.43594749052227, Benefit0.847281163356533,
Cond:8358, T:38614, TR:-0.00968052285516238, Period:3.51719583570726, Benefit0.842984409799555,
Cond:1646, T:37026, TR:-0.00931031723410779, Period:3.89102252471236, Benefit0.842381029546805,
Cond:6844, T:35878, TR:-0.00904339437613471, Period:7.05259490495568, Benefit0.841964435029823,
Cond:8368, T:39102, TR:-0.00973265831167797, Period:3.72287862513426, Benefit0.837297324945016,
Cond:6108, T:35324, TR:-0.00886218754573791, Period:3.45660174385687, Benefit0.836230324991507,
Cond:8346, T:37417, TR:-0.00933066450103552, Period:3.51350990191624, Benefit0.835502579041612,
Cond:92, T:35974, TR:-0.00898984865250912, Period:4.29868794129093, Benefit0.834158003002168,
Cond:5470, T:33700, TR:-0.00843641141077065, Period:4.63753709198813, Benefit0.829940652818991, , T2:78,96,2764,8354,4330,6128,8334,750,8344,6116,4328,6122,6120,6412,6385,4318,98,540,6110,8356,6636,6118,8358,1646,6844,8368,6108,8346,92,5470,  #End#
LowScoreRank2 , T0:110 , T1:
Cond:6101, T:33466, TR:-0.0055160277614459, Period:9.02005020020319, Benefit0.511055997131417,
Cond:3661, T:33937, TR:-0.00603836710258593, Period:8.95034917641512, Benefit0.561157438783628,
Cond:2755, T:33470, TR:-0.00614321431197157, Period:8.55996414699731, Benefit0.580729011054676,
Cond:6075, T:33713, TR:-0.0061645766085049, Period:9.06605760389167, Benefit0.578856820810963,
Cond:495, T:33822, TR:-0.0061784629428572, Period:8.95023948908994, Benefit0.578499201703033,
Cond:3229, T:33920, TR:-0.00617916316139411, Period:8.87160966981132, Benefit0.57688679245283,
Cond:75, T:34365, TR:-0.00618178368889733, Period:8.6925942092245, Benefit0.569620253164557,
Cond:2093, T:34175, TR:-0.00620888212817389, Period:8.9269056327725, Benefit0.575771762984638,
Cond:1627, T:34632, TR:-0.00624700787419354, Period:8.82524832524832, Benefit0.572187572187572,
Cond:703, T:34564, TR:-0.00629070710330988, Period:8.88256567526907, Benefit0.578029163291286,
Cond:8121, T:33810, TR:-0.00630824418313413, Period:9.00677314404022, Benefit0.59299023957409,
Cond:5021, T:34228, TR:-0.00631970367392004, Period:8.78418254061003, Benefit0.586917143858829,
Cond:537, T:34852, TR:-0.00634011840536941, Period:8.53876391598761, Benefit0.57847469298749,
Cond:509, T:33951, TR:-0.0063582019121538, Period:8.93028187682248, Benefit0.595976554446114,
Cond:3254, T:33515, TR:-0.00638056674059104, Period:8.31356109204834, Benefit0.606295688497688,
Cond:7713, T:33489, TR:-0.00639610770686385, Period:8.49168383648362, Benefit0.608498312878856,
Cond:4077, T:34739, TR:-0.00643105125469504, Period:8.8627191341144, Benefit0.590114856501339,
Cond:717, T:34492, TR:-0.00645481527816678, Period:8.87214426533689, Benefit0.596950017395338,
Cond:2511, T:33827, TR:-0.00646494883096452, Period:8.98025246105182, Benefit0.609926981405386,
Cond:3437, T:35832, TR:-0.00646682087218954, Period:8.63420964501005, Benefit0.575630721143112,
Cond:3869, T:35125, TR:-0.00647446328729185, Period:8.78320284697509, Benefit0.588156583629893,
Cond:4799, T:33476, TR:-0.00647777483050779, Period:8.61100489903214, Benefit0.617815748596009,
Cond:8135, T:34031, TR:-0.00648008482781511, Period:9.03349886867856, Benefit0.607886926625724,
Cond:745, T:35017, TR:-0.00650572803289578, Period:8.55127509495388, Benefit0.593311819973156,
Cond:2745, T:36117, TR:-0.00651141709312947, Period:8.62308608134673, Benefit0.575629205083479,
Cond:2079, T:34318, TR:-0.00651418171418988, Period:8.87828544786992, Benefit0.606445597062766,
Cond:5437, T:35377, TR:-0.00651711181018785, Period:8.69217288068519, Benefit0.588404895836278,
Cond:4589, T:34947, TR:-0.00652361665930544, Period:8.31773828940968, Benefit0.596417432111483,
Cond:4610, T:34784, TR:-0.00653381440401365, Period:8.7733728150874, Benefit0.600333486660534,
Cond:3215, T:34415, TR:-0.00653773568776642, Period:8.7434548888566, Benefit0.607264274298998, , T2:6101,3661,2755,6075,495,3229,75,2093,1627,703,8121,5021,537,509,3254,7713,4077,717,2511,3437,3869,4799,8135,745,2745,2079,5437,4589,4610,3215,  #End#
LowScoreRank3 , T0:110 , T1:
Cond:1, T:55717, TR:-0.0118784667669955, Period:6.59927849668862, Benefit0.727174829944182,
Cond:3, T:55699, TR:-0.0119216617143172, Period:6.60108799080773, Benefit0.730282410815275,
Cond:5, T:55662, TR:-0.0119526300737668, Period:6.60382307498832, Benefit0.73283389026625,
Cond:6, T:149, TR:-0.000929613986811859, Period:1.95973154362416, Benefit0.442953020134228,
Cond:7, T:55659, TR:-0.0119455918850073, Period:6.60380172119513, Benefit0.732406259544728,
Cond:8, T:1163, TR:-0.00124608449584586, Period:1.65692175408426, Benefit1.07738607050731,
Cond:9, T:55006, TR:-0.0118599673589414, Period:6.66352761516925, Benefit0.735483401810712,
Cond:10, T:3045, TR:-0.00161346283917682, Period:1.87651888341544, Benefit0.863382594417077,
Cond:11, T:53663, TR:-0.0116273610187768, Period:6.78620278404115, Benefit0.738143599873283,
Cond:12, T:7877, TR:-0.0030305387662533, Period:2.35394185603656, Benefit1.00685540180272,
Cond:13, T:49624, TR:-0.0103335353969571, Period:7.14750926970821, Benefit0.702563275834274,
Cond:14, T:12560, TR:-0.00398320617498315, Period:2.61616242038217, Benefit0.914171974522293,
Cond:15, T:45503, TR:-0.00933482900849009, Period:7.53728325604905, Benefit0.685713029910116,
Cond:17, T:55708, TR:-0.0118668136629273, Period:6.60018309758024, Benefit0.726520427945717,
Cond:18, T:687, TR:-0.00130985492031722, Period:2.16593886462882, Benefit2.17321688500728,
Cond:19, T:55578, TR:-0.0119488444952407, Period:6.61164489546223, Benefit0.733707582136817,
Cond:20, T:1090, TR:-0.00145171874870131, Period:2.26880733944954, Benefit1.85779816513761,
Cond:21, T:55313, TR:-0.0120050222098604, Period:6.63269032596316, Benefit0.741037369153725,
Cond:22, T:1415, TR:-0.00140308957548893, Period:2.37879858657244, Benefit1.3017667844523,
Cond:23, T:55155, TR:-0.0119504515414769, Period:6.64418457075514, Benefit0.739534040431511,
Cond:24, T:5527, TR:-0.00301717067690771, Period:2.08503709064592, Benefit1.42717568301067,
Cond:25, T:52134, TR:-0.0107348415535256, Period:6.92304446234703, Benefit0.696743008401427,
Cond:26, T:9201, TR:-0.00337200165844165, Period:2.33268123030105, Benefit1.00054341919357,
Cond:27, T:48981, TR:-0.0102677904673757, Period:7.21285804699782, Benefit0.706947591923399,
Cond:28, T:16822, TR:-0.00539088042152301, Period:2.77588871715611, Benefit0.994352633456188,
Cond:29, T:41747, TR:-0.00812047597248581, Period:7.95290679569789, Benefit0.640237621865044,
Cond:30, T:21665, TR:-0.00676827223247699, Period:2.93357950611585, Benefit1.00821601661666,
Cond:31, T:36428, TR:-0.0065584863489113, Period:8.58408367190074, Benefit0.575463928845943,
Cond:33, T:55655, TR:-0.0119220605666421, Period:6.60458179858054, Benefit0.730895696702902,
Cond:34, T:1870, TR:-0.00182825120702923, Period:2.62727272727273, Benefit1.8379679144385, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:110 , T1:
Cond:1, T:55717, TR:-0.0118784667669955, Period:6.59927849668862, Benefit0.727174829944182,
Cond:3, T:55699, TR:-0.0119216617143172, Period:6.60108799080773, Benefit0.730282410815275,
Cond:5, T:55662, TR:-0.0119526300737668, Period:6.60382307498832, Benefit0.73283389026625,
Cond:6, T:149, TR:-0.000929613986811859, Period:1.95973154362416, Benefit0.442953020134228,
Cond:7, T:55659, TR:-0.0119455918850073, Period:6.60380172119513, Benefit0.732406259544728,
Cond:8, T:1163, TR:-0.00124608449584586, Period:1.65692175408426, Benefit1.07738607050731,
Cond:9, T:55006, TR:-0.0118599673589414, Period:6.66352761516925, Benefit0.735483401810712,
Cond:10, T:3045, TR:-0.00161346283917682, Period:1.87651888341544, Benefit0.863382594417077,
Cond:11, T:53663, TR:-0.0116273610187768, Period:6.78620278404115, Benefit0.738143599873283,
Cond:12, T:7877, TR:-0.0030305387662533, Period:2.35394185603656, Benefit1.00685540180272,
Cond:13, T:49624, TR:-0.0103335353969571, Period:7.14750926970821, Benefit0.702563275834274,
Cond:14, T:12560, TR:-0.00398320617498315, Period:2.61616242038217, Benefit0.914171974522293,
Cond:15, T:45503, TR:-0.00933482900849009, Period:7.53728325604905, Benefit0.685713029910116,
Cond:17, T:55708, TR:-0.0118668136629273, Period:6.60018309758024, Benefit0.726520427945717,
Cond:18, T:687, TR:-0.00130985492031722, Period:2.16593886462882, Benefit2.17321688500728,
Cond:19, T:55578, TR:-0.0119488444952407, Period:6.61164489546223, Benefit0.733707582136817,
Cond:20, T:1090, TR:-0.00145171874870131, Period:2.26880733944954, Benefit1.85779816513761,
Cond:21, T:55313, TR:-0.0120050222098604, Period:6.63269032596316, Benefit0.741037369153725,
Cond:22, T:1415, TR:-0.00140308957548893, Period:2.37879858657244, Benefit1.3017667844523,
Cond:23, T:55155, TR:-0.0119504515414769, Period:6.64418457075514, Benefit0.739534040431511,
Cond:24, T:5527, TR:-0.00301717067690771, Period:2.08503709064592, Benefit1.42717568301067,
Cond:25, T:52134, TR:-0.0107348415535256, Period:6.92304446234703, Benefit0.696743008401427,
Cond:26, T:9201, TR:-0.00337200165844165, Period:2.33268123030105, Benefit1.00054341919357,
Cond:27, T:48981, TR:-0.0102677904673757, Period:7.21285804699782, Benefit0.706947591923399,
Cond:28, T:16822, TR:-0.00539088042152301, Period:2.77588871715611, Benefit0.994352633456188,
Cond:29, T:41747, TR:-0.00812047597248581, Period:7.95290679569789, Benefit0.640237621865044,
Cond:30, T:21665, TR:-0.00676827223247699, Period:2.93357950611585, Benefit1.00821601661666,
Cond:31, T:36428, TR:-0.0065584863489113, Period:8.58408367190074, Benefit0.575463928845943,
Cond:33, T:55655, TR:-0.0119220605666421, Period:6.60458179858054, Benefit0.730895696702902,
Cond:34, T:1870, TR:-0.00182825120702923, Period:2.62727272727273, Benefit1.8379679144385, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:3038 , T1:
Cond:2764, T:27670, TR:-0.00819632026477671, Period:3.34886158294181, Benefit0.980303577882183,
Cond:8354, T:28453, TR:-0.00837327708079013, Period:3.40582715355147, Benefit0.976276666783819,
Cond:78, T:29364, TR:-0.00858227545884845, Period:3.79764337283749, Benefit0.972244925759433,
Cond:4316, T:27984, TR:-0.00818627804863128, Period:3.29759862778731, Benefit0.967874499714122,
Cond:1630, T:27995, TR:-0.00816349499676808, Period:3.3663868547955, Benefit0.964457938917664,
Cond:526, T:28669, TR:-0.00833806942322468, Period:3.69119955352471, Benefit0.96428197704838,
Cond:6122, T:29000, TR:-0.00840376115425969, Period:3.34775862068966, Benefit0.961620689655172,
Cond:2556, T:27934, TR:-0.00806757265239799, Period:3.85669793083697, Benefit0.953783919238204,
Cond:6120, T:28288, TR:-0.00813542432856992, Period:3.31585831447964, Benefit0.950685803167421,
Cond:98, T:35414, TR:-0.00995933187076015, Period:7.70836392387192, Benefit0.949313830688428,
Cond:2750, T:28883, TR:-0.0082746728953251, Period:3.4271716926912, Benefit0.948897275213794,
Cond:8356, T:29369, TR:-0.0083530570473843, Period:3.36286560659198, Benefit0.943001123633764,
Cond:96, T:37121, TR:-0.0103307917611791, Period:7.66638829772905, Benefit0.942404568842434,
Cond:8358, T:29757, TR:-0.00840922717897823, Period:3.38290150216756, Benefit0.93762812111436,
Cond:6094, T:28424, TR:-0.00803777310274464, Period:3.34333661694343, Benefit0.933295806360822,
Cond:750, T:30285, TR:-0.00849526057475687, Period:3.86874690440812, Benefit0.931715370645534,
Cond:92, T:28320, TR:-0.00799729725221558, Period:4.27683615819209, Benefit0.931426553672316,
Cond:8344, T:29329, TR:-0.00825123760876562, Period:3.35701865048246, Benefit0.931364860718061,
Cond:8368, T:29787, TR:-0.00836312163300895, Period:3.54453284990096, Benefit0.930909457145735,
Cond:1422, T:28590, TR:-0.00801478358992415, Period:3.65019237495628, Benefit0.924833857992305,
Cond:8152, T:29882, TR:-0.00827152293849886, Period:3.59892242821766, Benefit0.916504919349441,
Cond:5914, T:28566, TR:-0.00792161060563773, Period:3.74907232374151, Benefit0.913463558076034,
Cond:6134, T:29687, TR:-0.00819458397578223, Period:3.4188365277731, Benefit0.912924849260619,
Cond:4338, T:29567, TR:-0.00814788103569117, Period:3.63959820069672, Benefit0.910778908918727,
Cond:6108, T:29642, TR:-0.00816280920981283, Period:3.47702584171109, Benefit0.910329937251198,
Cond:8374, T:30536, TR:-0.00837259012498825, Period:3.51100340581609, Benefit0.909025412627718,
Cond:4318, T:30191, TR:-0.0082805319716111, Period:3.58325328740353, Benefit0.908151435858368,
Cond:764, T:29293, TR:-0.00803293758389604, Period:4.30123237633564, Benefit0.904755402314546,
Cond:8334, T:29684, TR:-0.0080909588344471, Period:3.52529982482145, Benefit0.900013475272874,
Cond:2780, T:32220, TR:-0.0086979213370203, Period:4.57312228429547, Benefit0.898665425201738, , T2:2764,8354,78,4316,1630,526,6122,2556,6120,98,2750,8356,96,8358,6094,750,92,8344,8368,1422,8152,5914,6134,4338,6108,8374,4318,764,8334,2780,  #End#
LowScoreRank2 , T0:3038 , T1:
Cond:99, T:28939, TR:-0.00398783343724428, Period:8.69356232074363, Benefit0.395106949099831,
Cond:6101, T:28719, TR:-0.00400906777178801, Period:9.76162122636582, Benefit0.400919252063094,
Cond:547, T:28130, TR:-0.00423141628590656, Period:8.80447920369712, Benefit0.438855314610736,
Cond:2769, T:28440, TR:-0.00431415242083207, Period:9.59641350210971, Benefit0.444866385372715,
Cond:3005, T:27784, TR:-0.00442634948869121, Period:9.87366829830118, Benefit0.470522602936942,
Cond:4612, T:28018, TR:-0.00443387209635544, Period:9.73856092511957, Benefit0.46755657077593,
Cond:4614, T:27992, TR:-0.00446442963819202, Period:9.62767933695342, Benefit0.472063446699057,
Cond:747, T:28082, TR:-0.00447124561205795, Period:9.77209600455808, Benefit0.471440780571184,
Cond:4610, T:28888, TR:-0.00451112857065409, Period:9.66318194405982, Benefit0.46330656327887,
Cond:973, T:27706, TR:-0.00451345414817637, Period:9.96704684905797, Benefit0.483577564426478,
Cond:1869, T:28587, TR:-0.0045249874022455, Period:9.76335397208521, Benefit0.470038828838283,
Cond:8147, T:28755, TR:-0.00452707004450461, Period:9.8999478351591, Benefit0.467536080681621,
Cond:45, T:29189, TR:-0.0046250288286997, Period:9.62622905889205, Benefit0.473020658467231,
Cond:3645, T:28514, TR:-0.00466122438446833, Period:9.7824928105492, Benefit0.489058006593252,
Cond:545, T:30344, TR:-0.00468928959104234, Period:8.55869364619035, Benefit0.462727392565252,
Cond:1181, T:27899, TR:-0.00469426269316206, Period:9.95078676655077, Benefit0.504354994802681,
Cond:1627, T:29502, TR:-0.00471158302929309, Period:9.58745169818995, Benefit0.47888278760762,
Cond:3213, T:28600, TR:-0.00472332398318326, Period:9.73157342657343, Benefit0.495664335664336,
Cond:3254, T:28462, TR:-0.004735761693248, Period:9.15504883704589, Benefit0.499718923476917,
Cond:4797, T:27770, TR:-0.00473595563754925, Period:9.83583003240907, Benefit0.512315448325531,
Cond:8325, T:30582, TR:-0.00474033699504659, Period:9.32492969720751, Benefit0.465306389379373,
Cond:479, T:28371, TR:-0.00474342811769173, Period:9.84110535405872, Benefit0.502343942758451,
Cond:7713, T:27697, TR:-0.00474605823101538, Period:9.48189334584973, Benefit0.51503772971802,
Cond:1910, T:28174, TR:-0.00476088735378532, Period:9.15056435011003, Benefit0.508199048768368,
Cond:2495, T:27748, TR:-0.00476517264238673, Period:9.99571140262361, Benefit0.51664984863774,
Cond:285, T:28759, TR:-0.00478713045261206, Period:9.72238255850342, Benefit0.50116485274175,
Cond:5867, T:28740, TR:-0.00479220514494064, Period:9.7562630480167, Benefit0.502157272094642,
Cond:1908, T:27692, TR:-0.00481527283849459, Period:9.50700563339593, Benefit0.524447493861043,
Cond:2509, T:28529, TR:-0.0048241368263964, Period:9.81580146517579, Benefit0.510077465035578,
Cond:6628, T:30540, TR:-0.00482620466304688, Period:9.22603143418468, Benefit0.476424361493124, , T2:99,6101,547,2769,3005,4612,4614,747,4610,973,1869,8147,45,3645,545,1181,1627,3213,3254,4797,8325,479,7713,1910,2495,285,5867,1908,2509,6628,  #End#
LowScoreRank3 , T0:3038 , T1:
Cond:1, T:46051, TR:-0.00954732578612961, Period:7.15908449327919, Benefit0.69455603569955,
Cond:3, T:46040, TR:-0.00957102452676655, Period:7.16057775847089, Benefit0.696633362293658,
Cond:5, T:46019, TR:-0.00959038327122542, Period:7.16258501923119, Benefit0.698515830417871,
Cond:6, T:80, TR:-0.000906692924214993, Period:2.075, Benefit-0.25,
Cond:7, T:46018, TR:-0.00958741482091031, Period:7.16241470728845, Benefit0.698291972706332,
Cond:8, T:862, TR:-0.00118045490172679, Period:1.6461716937355, Benefit1.16821345707657,
Cond:9, T:45504, TR:-0.00947070321471339, Period:7.22576037271449, Benefit0.696773909985935,
Cond:10, T:2621, TR:-0.00160981594636839, Period:1.89584128195345, Benefit0.998092331171309,
Cond:11, T:44263, TR:-0.00919669177709511, Period:7.37855545263538, Benefit0.693604138897047,
Cond:12, T:7349, TR:-0.00301676968953209, Period:2.38767179208056, Benefit1.07239080146959,
Cond:13, T:40339, TR:-0.00792033484352197, Period:7.86759711445499, Benefit0.644438384689754,
Cond:14, T:12192, TR:-0.00398895983267668, Period:2.63672900262467, Benefit0.94365157480315,
Cond:15, T:36083, TR:-0.00693756997399432, Period:8.46503893800405, Benefit0.620098107141867,
Cond:17, T:46045, TR:-0.00954030006462936, Period:7.15988706699967, Benefit0.694081876425236,
Cond:18, T:384, TR:-0.00107563027852962, Period:2.234375, Benefit1.59895833333333,
Cond:19, T:45976, TR:-0.00961615569857451, Period:7.16821820080042, Benefit0.701257177657909,
Cond:20, T:643, TR:-0.00123445819356925, Period:2.2706065318818, Benefit1.88180404354588,
Cond:21, T:45819, TR:-0.00962194967952857, Period:7.18496693511425, Benefit0.704162028852659,
Cond:22, T:842, TR:-0.00119829603275827, Period:2.37529691211401, Benefit1.27553444180523,
Cond:23, T:45709, TR:-0.00962678675943477, Period:7.19619768535737, Benefit0.706272287733269,
Cond:24, T:4034, TR:-0.00264831991024972, Period:2.03470500743679, Benefit1.61353495290035,
Cond:25, T:43393, TR:-0.0085570566971297, Period:7.4865761758809, Benefit0.652985504574471,
Cond:26, T:7525, TR:-0.00309243118877525, Period:2.31162790697674, Benefit1.08491694352159,
Cond:27, T:40477, TR:-0.00797285814028557, Period:7.85470761173012, Benefit0.647034118141167,
Cond:28, T:15064, TR:-0.00521587819741154, Period:2.82235793945831, Benefit1.06757833244822,
Cond:29, T:33390, TR:-0.00581498838961536, Period:8.8890086852351, Benefit0.5455525606469,
Cond:30, T:20390, TR:-0.00664573105232942, Period:2.96537518391368, Benefit1.04923982344286,
Cond:31, T:27623, TR:-0.0042238650806235, Period:10.011258733664, Benefit0.445968938927705,
Cond:33, T:46012, TR:-0.00958200683873327, Period:7.16371816047988, Benefit0.697948361297053,
Cond:34, T:1021, TR:-0.00138722517511951, Period:2.54946131243879, Benefit1.74632713026445, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:3038 , T1:
Cond:1, T:46051, TR:-0.00954732578612961, Period:7.15908449327919, Benefit0.69455603569955,
Cond:3, T:46040, TR:-0.00957102452676655, Period:7.16057775847089, Benefit0.696633362293658,
Cond:5, T:46019, TR:-0.00959038327122542, Period:7.16258501923119, Benefit0.698515830417871,
Cond:6, T:80, TR:-0.000906692924214993, Period:2.075, Benefit-0.25,
Cond:7, T:46018, TR:-0.00958741482091031, Period:7.16241470728845, Benefit0.698291972706332,
Cond:8, T:862, TR:-0.00118045490172679, Period:1.6461716937355, Benefit1.16821345707657,
Cond:9, T:45504, TR:-0.00947070321471339, Period:7.22576037271449, Benefit0.696773909985935,
Cond:10, T:2621, TR:-0.00160981594636839, Period:1.89584128195345, Benefit0.998092331171309,
Cond:11, T:44263, TR:-0.00919669177709511, Period:7.37855545263538, Benefit0.693604138897047,
Cond:12, T:7349, TR:-0.00301676968953209, Period:2.38767179208056, Benefit1.07239080146959,
Cond:13, T:40339, TR:-0.00792033484352197, Period:7.86759711445499, Benefit0.644438384689754,
Cond:14, T:12192, TR:-0.00398895983267668, Period:2.63672900262467, Benefit0.94365157480315,
Cond:15, T:36083, TR:-0.00693756997399432, Period:8.46503893800405, Benefit0.620098107141867,
Cond:17, T:46045, TR:-0.00954030006462936, Period:7.15988706699967, Benefit0.694081876425236,
Cond:18, T:384, TR:-0.00107563027852962, Period:2.234375, Benefit1.59895833333333,
Cond:19, T:45976, TR:-0.00961615569857451, Period:7.16821820080042, Benefit0.701257177657909,
Cond:20, T:643, TR:-0.00123445819356925, Period:2.2706065318818, Benefit1.88180404354588,
Cond:21, T:45819, TR:-0.00962194967952857, Period:7.18496693511425, Benefit0.704162028852659,
Cond:22, T:842, TR:-0.00119829603275827, Period:2.37529691211401, Benefit1.27553444180523,
Cond:23, T:45709, TR:-0.00962678675943477, Period:7.19619768535737, Benefit0.706272287733269,
Cond:24, T:4034, TR:-0.00264831991024972, Period:2.03470500743679, Benefit1.61353495290035,
Cond:25, T:43393, TR:-0.0085570566971297, Period:7.4865761758809, Benefit0.652985504574471,
Cond:26, T:7525, TR:-0.00309243118877525, Period:2.31162790697674, Benefit1.08491694352159,
Cond:27, T:40477, TR:-0.00797285814028557, Period:7.85470761173012, Benefit0.647034118141167,
Cond:28, T:15064, TR:-0.00521587819741154, Period:2.82235793945831, Benefit1.06757833244822,
Cond:29, T:33390, TR:-0.00581498838961536, Period:8.8890086852351, Benefit0.5455525606469,
Cond:30, T:20390, TR:-0.00664573105232942, Period:2.96537518391368, Benefit1.04923982344286,
Cond:31, T:27623, TR:-0.0042238650806235, Period:10.011258733664, Benefit0.445968938927705,
Cond:33, T:46012, TR:-0.00958200683873327, Period:7.16371816047988, Benefit0.697948361297053,
Cond:34, T:1021, TR:-0.00138722517511951, Period:2.54946131243879, Benefit1.74632713026445, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:7068 , T1:
Cond:96, T:37767, TR:-0.0107094919565395, Period:8.14772155585564, Benefit0.963380729208039,
Cond:98, T:36149, TR:-0.0102589820231977, Period:8.20019917563418, Benefit0.960635148966776,
Cond:750, T:28273, TR:-0.00816839742468563, Period:3.9494924486259, Benefit0.955540621794645,
Cond:8356, T:27726, TR:-0.00802124299251977, Period:3.44142681959172, Benefit0.954771694438433,
Cond:8368, T:28246, TR:-0.00807763992014254, Period:3.66887346880974, Benefit0.944487715074701,
Cond:8358, T:28129, TR:-0.00803633186494317, Period:3.45913470084255, Benefit0.942976998826834,
Cond:6134, T:28003, TR:-0.00793114676044298, Period:3.48573367139235, Benefit0.933257151019534,
Cond:4318, T:28208, TR:-0.00793694975310451, Period:3.63510351673284, Benefit0.92718377765173,
Cond:8346, T:28121, TR:-0.00788846520561398, Period:3.52889299811529, Benefit0.923651363749511,
Cond:1646, T:29141, TR:-0.00813701851092869, Period:4.0299234755156, Benefit0.9228235132631,
Cond:8374, T:28862, TR:-0.00805535767670847, Period:3.57934308086758, Benefit0.921280576536623,
Cond:4338, T:28149, TR:-0.00787269293003504, Period:3.81505559700167, Benefit0.920636612313048,
Cond:544, T:34110, TR:-0.00932425279887851, Period:8.19155672823219, Benefit0.916710642040457,
Cond:6108, T:27679, TR:-0.00771367300823378, Period:3.51150691860255, Benefit0.914989703385238,
Cond:546, T:34261, TR:-0.00931199893351252, Period:8.18391173637664, Benefit0.911298561046087,
Cond:6110, T:29214, TR:-0.00803759859483068, Period:3.83607174642295, Benefit0.90781816937085,
Cond:8152, T:28398, TR:-0.007819012440668, Period:3.69666877949151, Benefit0.905451088104796,
Cond:6130, T:29632, TR:-0.00810936812147574, Period:3.91222327213823, Benefit0.903921436285097,
Cond:764, T:27876, TR:-0.00767598048623427, Period:4.45192997560626, Benefit0.903429473382121,
Cond:94, T:32871, TR:-0.00889181156765892, Period:5.26762191597457, Benefit0.902649752061087,
Cond:2766, T:30117, TR:-0.00819499304037378, Period:4.14596407344689, Benefit0.899824019656672,
Cond:318, T:30006, TR:-0.00814669359335066, Period:4.83899886689329, Benefit0.897187229220823,
Cond:4332, T:29304, TR:-0.00797068915192311, Period:3.91745154245154, Benefit0.896498771498772,
Cond:8348, T:29456, TR:-0.00799329366865033, Period:3.85564910374796, Benefit0.894690385659967,
Cond:2780, T:31152, TR:-0.00838525416477117, Period:4.72996918335901, Benefit0.892398561890087,
Cond:8370, T:30712, TR:-0.00826504240457079, Period:4.08820656420943, Benefit0.89072675175827,
Cond:1660, T:29146, TR:-0.00788553576792798, Period:4.53280038427228, Benefit0.890516708982365,
Cond:6132, T:30544, TR:-0.00822144046706774, Period:4.16549895233106, Benefit0.890354897852279,
Cond:4344, T:30105, TR:-0.00809348416242458, Period:4.59627968775951, Benefit0.887626640093008,
Cond:8360, T:30099, TR:-0.00808568544510062, Period:3.92986477956078, Benefit0.886840094355294, , T2:96,98,750,8356,8368,8358,6134,4318,8346,1646,8374,4338,544,6108,546,6110,8152,6130,764,94,2766,318,4332,8348,2780,8370,1660,6132,4344,8360,  #End#
LowScoreRank2 , T0:7068 , T1:
Cond:99, T:29064, TR:-0.00421798358375682, Period:9.19082025873933, Benefit0.422894302229562,
Cond:547, T:28503, TR:-0.00447819132708581, Period:9.30473985194541, Benefit0.465319440058941,
Cond:101, T:27794, TR:-0.00479474508061513, Period:9.25120529610707, Benefit0.519752464560697,
Cond:4313, T:27891, TR:-0.0048176764476475, Period:10.6780323401814, Benefit0.52099243483561,
Cond:4323, T:28195, TR:-0.00486100297341529, Period:10.3588224862564, Benefit0.521049831530413,
Cond:6089, T:28004, TR:-0.00488946417729351, Period:10.6617983145265, Benefit0.528424510784174,
Cond:8327, T:27903, TR:-0.00489979590247662, Period:10.6453069562413, Benefit0.531734938895459,
Cond:61, T:27846, TR:-0.0049617004464669, Period:10.6010198951375, Benefit0.541119011707247,
Cond:6103, T:27948, TR:-0.00505279562793908, Period:10.6794403892944, Benefit0.551273794189209,
Cond:545, T:30596, TR:-0.00509881239967887, Period:9.04307752647405, Benefit0.508726630932148,
Cond:6101, T:30420, TR:-0.00513347578740147, Period:10.0610453648915, Benefit0.515943458251151,
Cond:91, T:28940, TR:-0.00515586002373625, Period:10.1215272978576, Benefit0.545473393227367,
Cond:2761, T:28709, TR:-0.00517701011841359, Period:10.3686300463269, Benefit0.552648995088648,
Cond:2769, T:29684, TR:-0.0052073568176974, Period:9.97847325158335, Benefit0.5381350222342,
Cond:1613, T:27821, TR:-0.00521548618873027, Period:10.6760001437763, Benefit0.575608353402106,
Cond:1643, T:28135, TR:-0.00522934213528084, Period:10.4795095077306, Benefit0.57096143593389,
Cond:2747, T:29301, TR:-0.00523742070896249, Period:10.3256202859971, Benefit0.549059759052592,
Cond:301, T:27845, TR:-0.00524074085366917, Period:10.6172382833543, Benefit0.578488058897468,
Cond:4299, T:28497, TR:-0.00526500481619069, Period:10.5409692248307, Benefit0.568305435659894,
Cond:31, T:28966, TR:-0.00528687036543994, Period:10.4088586618794, Benefit0.561831112338604,
Cond:3869, T:28086, TR:-0.00532774928410433, Period:10.628462579221, Benefit0.585024567400128,
Cond:2093, T:28188, TR:-0.00533997013763927, Period:10.5798212005109, Benefit0.584504044274159,
Cond:1651, T:28014, TR:-0.0053475021497921, Period:10.3475405154566, Benefit0.589169700863854,
Cond:6075, T:28432, TR:-0.00536876505975922, Period:10.5605655599325, Benefit0.583216094541362,
Cond:4614, T:28647, TR:-0.00537095340153973, Period:10.2054665409991, Benefit0.579083324606416,
Cond:4077, T:27909, TR:-0.0053808907412093, Period:10.6885592461213, Benefit0.595865133111183,
Cond:5645, T:28095, TR:-0.00538464755120424, Period:10.610108560242, Benefit0.592382986296494,
Cond:3437, T:28531, TR:-0.00538688773130848, Period:10.4804248010935, Benefit0.583540710104798,
Cond:4612, T:28788, TR:-0.00539718923978919, Period:10.2717104349034, Benefit0.579616506877866,
Cond:3034, T:28463, TR:-0.00543804501850702, Period:8.3723430418438, Benefit0.591645293890314, , T2:99,547,101,4313,4323,6089,8327,61,6103,545,6101,91,2761,2769,1613,1643,2747,301,4299,31,3869,2093,1651,6075,4614,4077,5645,3437,4612,3034,  #End#
LowScoreRank3 , T0:7068 , T1:
Cond:1, T:46129, TR:-0.01029953055215, Period:7.62817316655466, Benefit0.753842485204535,
Cond:3, T:46116, TR:-0.0103318501907641, Period:7.63006331858791, Benefit0.756657125509585,
Cond:5, T:46098, TR:-0.0103385407933805, Period:7.63171938045034, Benefit0.757494902164953,
Cond:6, T:78, TR:-0.000910154668893856, Period:2.1025641025641, Benefit-0.0897435897435897,
Cond:7, T:46097, TR:-0.0103371898568508, Period:7.63173308458251, Benefit0.757402867865588,
Cond:8, T:671, TR:-0.00109808735834256, Period:1.63338301043219, Benefit1.04023845007452,
Cond:9, T:45702, TR:-0.0102748895800262, Period:7.684127609295, Benefit0.758982101439762,
Cond:10, T:1961, TR:-0.00141501789457556, Period:1.89495155532891, Benefit0.961754207037226,
Cond:11, T:44828, TR:-0.0100770543296089, Period:7.80030338181494, Benefit0.75760685285982,
Cond:12, T:6316, TR:-0.00274516843276236, Period:2.43651044965168, Benefit1.08708043065231,
Cond:13, T:41333, TR:-0.00892876876457667, Period:8.26010693634626, Benefit0.719352575423995,
Cond:14, T:11321, TR:-0.00374922194004912, Period:2.67635367900362, Benefit0.937284692165003,
Cond:15, T:36879, TR:-0.00790704698296695, Period:8.92150004067355, Benefit0.704303262019035,
Cond:17, T:46122, TR:-0.010291691795973, Period:7.62917913360219, Benefit0.753328129742856,
Cond:18, T:371, TR:-0.00109586884910568, Period:2.19946091644205, Benefit1.85983827493261,
Cond:19, T:46057, TR:-0.0103454368028291, Period:7.63749267212367, Benefit0.758733742970667,
Cond:20, T:614, TR:-0.00119555962700625, Period:2.22149837133551, Benefit1.7328990228013,
Cond:21, T:45908, TR:-0.0103943609350069, Period:7.65485318463013, Benefit0.765182538991026,
Cond:22, T:792, TR:-0.00120520584513867, Period:2.39015151515152, Benefit1.38888888888889,
Cond:23, T:45816, TR:-0.0104092012369678, Period:7.66537890693208, Benefit0.767941330539549,
Cond:24, T:3380, TR:-0.00236583815459711, Period:2.08461538461538, Benefit1.61272189349112,
Cond:25, T:43984, TR:-0.00946871031140138, Period:7.90780738450346, Benefit0.721012186249545,
Cond:26, T:6104, TR:-0.00276368592038694, Period:2.34518348623853, Benefit1.13630406290957,
Cond:27, T:41779, TR:-0.00898074201255175, Period:8.20220685033151, Benefit0.716197132530697,
Cond:28, T:13445, TR:-0.00480254961444, Period:2.87571587950911, Benefit1.08174042394942,
Cond:29, T:34971, TR:-0.00696049668661784, Period:9.21812358811587, Benefit0.642475193731949,
Cond:30, T:19002, TR:-0.006298765173651, Period:3.01847173981686, Benefit1.05815177349753,
Cond:31, T:28966, TR:-0.00528687036543994, Period:10.4088586618794, Benefit0.561831112338604,
Cond:33, T:46092, TR:-0.0103323225911172, Period:7.63290809684978, Benefit0.757094506638896,
Cond:34, T:1038, TR:-0.00137044867879573, Period:2.57032755298651, Benefit1.65703275529865, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:7068 , T1:
Cond:1, T:46129, TR:-0.01029953055215, Period:7.62817316655466, Benefit0.753842485204535,
Cond:3, T:46116, TR:-0.0103318501907641, Period:7.63006331858791, Benefit0.756657125509585,
Cond:5, T:46098, TR:-0.0103385407933805, Period:7.63171938045034, Benefit0.757494902164953,
Cond:6, T:78, TR:-0.000910154668893856, Period:2.1025641025641, Benefit-0.0897435897435897,
Cond:7, T:46097, TR:-0.0103371898568508, Period:7.63173308458251, Benefit0.757402867865588,
Cond:8, T:671, TR:-0.00109808735834256, Period:1.63338301043219, Benefit1.04023845007452,
Cond:9, T:45702, TR:-0.0102748895800262, Period:7.684127609295, Benefit0.758982101439762,
Cond:10, T:1961, TR:-0.00141501789457556, Period:1.89495155532891, Benefit0.961754207037226,
Cond:11, T:44828, TR:-0.0100770543296089, Period:7.80030338181494, Benefit0.75760685285982,
Cond:12, T:6316, TR:-0.00274516843276236, Period:2.43651044965168, Benefit1.08708043065231,
Cond:13, T:41333, TR:-0.00892876876457667, Period:8.26010693634626, Benefit0.719352575423995,
Cond:14, T:11321, TR:-0.00374922194004912, Period:2.67635367900362, Benefit0.937284692165003,
Cond:15, T:36879, TR:-0.00790704698296695, Period:8.92150004067355, Benefit0.704303262019035,
Cond:17, T:46122, TR:-0.010291691795973, Period:7.62917913360219, Benefit0.753328129742856,
Cond:18, T:371, TR:-0.00109586884910568, Period:2.19946091644205, Benefit1.85983827493261,
Cond:19, T:46057, TR:-0.0103454368028291, Period:7.63749267212367, Benefit0.758733742970667,
Cond:20, T:614, TR:-0.00119555962700625, Period:2.22149837133551, Benefit1.7328990228013,
Cond:21, T:45908, TR:-0.0103943609350069, Period:7.65485318463013, Benefit0.765182538991026,
Cond:22, T:792, TR:-0.00120520584513867, Period:2.39015151515152, Benefit1.38888888888889,
Cond:23, T:45816, TR:-0.0104092012369678, Period:7.66537890693208, Benefit0.767941330539549,
Cond:24, T:3380, TR:-0.00236583815459711, Period:2.08461538461538, Benefit1.61272189349112,
Cond:25, T:43984, TR:-0.00946871031140138, Period:7.90780738450346, Benefit0.721012186249545,
Cond:26, T:6104, TR:-0.00276368592038694, Period:2.34518348623853, Benefit1.13630406290957,
Cond:27, T:41779, TR:-0.00898074201255175, Period:8.20220685033151, Benefit0.716197132530697,
Cond:28, T:13445, TR:-0.00480254961444, Period:2.87571587950911, Benefit1.08174042394942,
Cond:29, T:34971, TR:-0.00696049668661784, Period:9.21812358811587, Benefit0.642475193731949,
Cond:30, T:19002, TR:-0.006298765173651, Period:3.01847173981686, Benefit1.05815177349753,
Cond:31, T:28966, TR:-0.00528687036543994, Period:10.4088586618794, Benefit0.561831112338604,
Cond:33, T:46092, TR:-0.0103323225911172, Period:7.63290809684978, Benefit0.757094506638896,
Cond:34, T:1038, TR:-0.00137044867879573, Period:2.57032755298651, Benefit1.65703275529865, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:4830 , T1:
Cond:78, T:28157, TR:-0.00839032192359573, Period:3.8518663209859, Benefit0.988883758923181,
Cond:8354, T:27657, TR:-0.00820836498964781, Period:3.47300864157356, Benefit0.982391437972304,
Cond:526, T:27410, TR:-0.00809702222951939, Period:3.74512951477563, Benefit0.976176577891281,
Cond:6122, T:28085, TR:-0.0082085026895721, Period:3.40121061064625, Benefit0.967313512551184,
Cond:2750, T:27767, TR:-0.00803233168353358, Period:3.46796557064141, Benefit0.954838477329204,
Cond:98, T:35635, TR:-0.0100516087868229, Period:7.87661007436509, Benefit0.952995650343763,
Cond:8356, T:28566, TR:-0.00815454819553876, Period:3.42554085276202, Benefit0.943849331372961,
Cond:96, T:37243, TR:-0.0103752447537324, Period:7.84509840775448, Benefit0.943720967698628,
Cond:750, T:29071, TR:-0.00827831119687907, Period:3.92504557806749, Benefit0.94317360943896,
Cond:8358, T:28961, TR:-0.00821714232895187, Period:3.44718759711336, Benefit0.938917855046442,
Cond:8344, T:28427, TR:-0.00807078677962108, Period:3.4158370563197, Benefit0.937524184753931,
Cond:6094, T:27618, TR:-0.00780986396255917, Period:3.4025635455138, Benefit0.930009414150192,
Cond:8368, T:29104, TR:-0.00815787290614427, Period:3.63822842221001, Benefit0.926676745464541,
Cond:5914, T:27557, TR:-0.00771100593266653, Period:3.81627172769169, Benefit0.918713938382262,
Cond:764, T:28171, TR:-0.00785315110814752, Period:4.37929076000142, Benefit0.917326328493841,
Cond:6134, T:28947, TR:-0.00803353356885338, Period:3.49169171243998, Benefit0.915742563996269,
Cond:8152, T:29000, TR:-0.00804089444282514, Period:3.65858620689655, Benefit0.915,
Cond:8374, T:29868, TR:-0.00820786799741639, Period:3.59217222445427, Benefit0.908999598232222,
Cond:2780, T:31217, TR:-0.00853766971017027, Period:4.64804433481757, Benefit0.908703590992088,
Cond:1660, T:29367, TR:-0.00807792311138394, Period:4.45322981577962, Benefit0.908162222903259,
Cond:4338, T:28902, TR:-0.00794218548227797, Period:3.75008649920421, Benefit0.905404470278873,
Cond:544, T:33658, TR:-0.00910948107237718, Period:7.86597539960782, Benefit0.905401390456949,
Cond:6108, T:28736, TR:-0.00789486629148048, Period:3.52954482182628, Benefit0.904544821826281,
Cond:4318, T:29298, TR:-0.00801684352429737, Period:3.64441258788996, Benefit0.902553075295242,
Cond:94, T:33601, TR:-0.00906795798548082, Period:5.18389333650784, Benefit0.902354096604268,
Cond:8346, T:29546, TR:-0.00804448883377088, Period:3.60228118865498, Benefit0.898395721925134,
Cond:2778, T:27860, TR:-0.00763194462072441, Period:4.43467336683417, Benefit0.898061737257717,
Cond:4332, T:29893, TR:-0.00811530546017133, Period:3.88850232495902, Benefit0.896698223664403,
Cond:990, T:27879, TR:-0.00762259261439541, Period:4.41030883460669, Benefit0.896194268087091,
Cond:1646, T:30123, TR:-0.00816305789979121, Period:4.01045712578428, Benefit0.895694319954852, , T2:78,8354,526,6122,2750,98,8356,96,750,8358,8344,6094,8368,5914,764,6134,8152,8374,2780,1660,4338,544,6108,4318,94,8346,2778,4332,990,1646,  #End#
LowScoreRank2 , T0:4830 , T1:
Cond:99, T:28845, TR:-0.00396679908567834, Period:8.85099670653493, Benefit0.393690414283238,
Cond:547, T:28213, TR:-0.00425485574720401, Period:8.95810441994825, Benefit0.440647928260022,
Cond:6101, T:29249, TR:-0.00445305219805365, Period:9.84197750350439, Benefit0.450135047352046,
Cond:2747, T:27948, TR:-0.00456499572356718, Period:10.1448761986546, Benefit0.486224416774009,
Cond:91, T:27750, TR:-0.00456502151571609, Period:9.88436036036036, Benefit0.48972972972973,
Cond:2761, T:27593, TR:-0.00463673127709574, Period:10.1387308375313, Benefit0.50222882615156,
Cond:31, T:28024, TR:-0.00463860911190165, Period:10.1282115329717, Benefit0.494683128746788,
Cond:509, T:27558, TR:-0.00467451644502443, Period:10.1809637854706, Benefit0.507983162783947,
Cond:2769, T:28797, TR:-0.00469029484588264, Period:9.69094002847519, Benefit0.487967496614231,
Cond:4612, T:27694, TR:-0.00472244414347316, Period:10.0153462843937, Benefit0.511915938470427,
Cond:545, T:30199, TR:-0.00472803648521339, Period:8.72151395741581, Benefit0.469750653995165,
Cond:4614, T:27520, TR:-0.00474824743049113, Period:9.92314680232558, Benefit0.518677325581395,
Cond:3005, T:28134, TR:-0.00480645238320124, Period:10.0099523707969, Benefit0.514964100376768,
Cond:4610, T:28733, TR:-0.00480910366053139, Period:9.89047436745206, Benefit0.504472209654404,
Cond:717, T:28064, TR:-0.00482916974086769, Period:10.0880843785633, Benefit0.519277366020525,
Cond:8135, T:27404, TR:-0.00483368022488103, Period:10.329367975478, Benefit0.532513501678587,
Cond:747, T:28924, TR:-0.00490623791439634, Period:9.7910040105103, Benefit0.513621905683861,
Cond:539, T:28181, TR:-0.00491921309360414, Period:9.88769028778255, Benefit0.529008906710195,
Cond:271, T:27434, TR:-0.0049458600759983, Period:10.2492892031785, Benefit0.547167748049865,
Cond:3254, T:27528, TR:-0.00495161882276937, Period:9.48968323161871, Benefit0.546062191223482,
Cond:2301, T:28103, TR:-0.00496687250961486, Period:10.1145429313596, Benefit0.536811016617443,
Cond:1855, T:27486, TR:-0.00496846489027204, Period:10.222185840064, Benefit0.549188677872371,
Cond:973, T:28338, TR:-0.00497255262296367, Period:10.0225139388807, Benefit0.533065142211871,
Cond:8147, T:29188, TR:-0.00499005985777699, Period:10.000513909826, Benefit0.519631355351514,
Cond:1869, T:29147, TR:-0.00499510460394569, Period:9.83919442824304, Benefit0.521014169554328,
Cond:4797, T:27640, TR:-0.00502903646070047, Period:10.105499276411, Benefit0.554269175108538,
Cond:1910, T:27597, TR:-0.00503836864595152, Period:9.37819328187847, Benefit0.556401058086024,
Cond:3252, T:27634, TR:-0.0050451260156276, Period:9.81323731634942, Benefit0.556560758485923,
Cond:3645, T:28833, TR:-0.00506446259138021, Period:9.92314362015746, Benefit0.535705615093816,
Cond:45, T:29781, TR:-0.0050740733568584, Period:9.69598065880931, Benefit0.519693764480709, , T2:99,547,6101,2747,91,2761,31,509,2769,4612,545,4614,3005,4610,717,8135,747,539,271,3254,2301,1855,973,8147,1869,4797,1910,3252,3645,45,  #End#
LowScoreRank3 , T0:4830 , T1:
Cond:1, T:45651, TR:-0.00979944814281112, Period:7.34553459946113, Benefit0.721210926376202,
Cond:3, T:45641, TR:-0.00981883268212264, Period:7.34694682412743, Benefit0.722946473565435,
Cond:5, T:45621, TR:-0.00984250411658374, Period:7.34878674294733, Benefit0.725192345630302,
Cond:6, T:79, TR:-0.000906959008012928, Period:2.07594936708861, Benefit-0.240506329113924,
Cond:7, T:45618, TR:-0.00983225154720554, Period:7.34896312858959, Benefit0.724407032311807,
Cond:8, T:716, TR:-0.00114258438802718, Period:1.66620111731844, Benefit1.20810055865922,
Cond:9, T:45187, TR:-0.00972193593927701, Period:7.4037444397725, Benefit0.722353774315622,
Cond:10, T:2215, TR:-0.00153529473593608, Period:1.91151241534989, Benefit1.05507900677201,
Cond:11, T:44169, TR:-0.00947645860139698, Period:7.53487740270325, Benefit0.718603545473069,
Cond:12, T:6861, TR:-0.00294327173164533, Period:2.42778020696691, Benefit1.10873050575718,
Cond:13, T:40365, TR:-0.00825313843480761, Period:8.02140468227425, Benefit0.674643874643875,
Cond:14, T:11715, TR:-0.00390536687530439, Period:2.66145966709347, Benefit0.955527102005975,
Cond:15, T:36090, TR:-0.00722988193813266, Period:8.64034358548074, Benefit0.65009697977279,
Cond:17, T:45645, TR:-0.00979242277112757, Period:7.3463687150838, Benefit0.72073611567532,
Cond:18, T:375, TR:-0.00107003442845153, Period:2.22933333333333, Benefit1.58133333333333,
Cond:19, T:45577, TR:-0.00985829329182336, Period:7.35478421133466, Benefit0.727186958334248,
Cond:20, T:625, TR:-0.00122433044266235, Period:2.2864, Benefit1.8752,
Cond:21, T:45423, TR:-0.00986597144438427, Period:7.37183805561059, Benefit0.730312837108954,
Cond:22, T:803, TR:-0.00119375518122535, Period:2.39103362391034, Benefit1.31631382316314,
Cond:23, T:45324, TR:-0.00985411647844139, Period:7.38233606919072, Benefit0.73095931515312,
Cond:24, T:3493, TR:-0.00247573528873141, Period:2.07300314915545, Benefit1.67849985685657,
Cond:25, T:43429, TR:-0.00895725210665245, Period:7.62591816528126, Benefit0.686637960809597,
Cond:26, T:6651, TR:-0.00292736608883915, Period:2.34024958652834, Benefit1.13486693730266,
Cond:27, T:40848, TR:-0.00835544925280786, Period:7.95926361143752, Benefit0.675871523697611,
Cond:28, T:14174, TR:-0.00505798255305407, Period:2.87138422463666, Benefit1.0932693664456,
Cond:29, T:33803, TR:-0.00621041133239973, Period:8.99349170191995, Benefit0.582344762299204,
Cond:30, T:19510, TR:-0.0064950505737575, Period:3.00297283444388, Benefit1.06801640184521,
Cond:31, T:28024, TR:-0.00463860911190165, Period:10.1282115329717, Benefit0.494683128746788,
Cond:33, T:45611, TR:-0.00982630191398589, Period:7.35050755300257, Benefit0.724035868540484,
Cond:34, T:1008, TR:-0.00137709750609903, Period:2.54960317460317, Benefit1.73115079365079, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:4830 , T1:
Cond:1, T:45651, TR:-0.00979944814281112, Period:7.34553459946113, Benefit0.721210926376202,
Cond:3, T:45641, TR:-0.00981883268212264, Period:7.34694682412743, Benefit0.722946473565435,
Cond:5, T:45621, TR:-0.00984250411658374, Period:7.34878674294733, Benefit0.725192345630302,
Cond:6, T:79, TR:-0.000906959008012928, Period:2.07594936708861, Benefit-0.240506329113924,
Cond:7, T:45618, TR:-0.00983225154720554, Period:7.34896312858959, Benefit0.724407032311807,
Cond:8, T:716, TR:-0.00114258438802718, Period:1.66620111731844, Benefit1.20810055865922,
Cond:9, T:45187, TR:-0.00972193593927701, Period:7.4037444397725, Benefit0.722353774315622,
Cond:10, T:2215, TR:-0.00153529473593608, Period:1.91151241534989, Benefit1.05507900677201,
Cond:11, T:44169, TR:-0.00947645860139698, Period:7.53487740270325, Benefit0.718603545473069,
Cond:12, T:6861, TR:-0.00294327173164533, Period:2.42778020696691, Benefit1.10873050575718,
Cond:13, T:40365, TR:-0.00825313843480761, Period:8.02140468227425, Benefit0.674643874643875,
Cond:14, T:11715, TR:-0.00390536687530439, Period:2.66145966709347, Benefit0.955527102005975,
Cond:15, T:36090, TR:-0.00722988193813266, Period:8.64034358548074, Benefit0.65009697977279,
Cond:17, T:45645, TR:-0.00979242277112757, Period:7.3463687150838, Benefit0.72073611567532,
Cond:18, T:375, TR:-0.00107003442845153, Period:2.22933333333333, Benefit1.58133333333333,
Cond:19, T:45577, TR:-0.00985829329182336, Period:7.35478421133466, Benefit0.727186958334248,
Cond:20, T:625, TR:-0.00122433044266235, Period:2.2864, Benefit1.8752,
Cond:21, T:45423, TR:-0.00986597144438427, Period:7.37183805561059, Benefit0.730312837108954,
Cond:22, T:803, TR:-0.00119375518122535, Period:2.39103362391034, Benefit1.31631382316314,
Cond:23, T:45324, TR:-0.00985411647844139, Period:7.38233606919072, Benefit0.73095931515312,
Cond:24, T:3493, TR:-0.00247573528873141, Period:2.07300314915545, Benefit1.67849985685657,
Cond:25, T:43429, TR:-0.00895725210665245, Period:7.62591816528126, Benefit0.686637960809597,
Cond:26, T:6651, TR:-0.00292736608883915, Period:2.34024958652834, Benefit1.13486693730266,
Cond:27, T:40848, TR:-0.00835544925280786, Period:7.95926361143752, Benefit0.675871523697611,
Cond:28, T:14174, TR:-0.00505798255305407, Period:2.87138422463666, Benefit1.0932693664456,
Cond:29, T:33803, TR:-0.00621041133239973, Period:8.99349170191995, Benefit0.582344762299204,
Cond:30, T:19510, TR:-0.0064950505737575, Period:3.00297283444388, Benefit1.06801640184521,
Cond:31, T:28024, TR:-0.00463860911190165, Period:10.1282115329717, Benefit0.494683128746788,
Cond:33, T:45611, TR:-0.00982630191398589, Period:7.35050755300257, Benefit0.724035868540484,
Cond:34, T:1008, TR:-0.00137709750609903, Period:2.54960317460317, Benefit1.73115079365079, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
End , T0:00:42:28.4809449  #End#



//////////////////////////////////////////////////////////////////
/////////////////////////////////
/////////////////////////////////
LowScoreRank1 , T0:8376 , T1:
Cond:766, T:134242, TR:-0.0277219897984999, Period:6.93934089182223, Benefit0.722180837591812,
Cond:6126, T:132883, TR:-0.0266770778577489, Period:6.30636725540513, Benefit0.701368873369806,
Cond:8372, T:135606, TR:-0.0272141022612232, Period:5.66635694585785, Benefit0.701082547969854,
Cond:1228, T:132700, TR:-0.0261887313440006, Period:8.33335342878674, Benefit0.68904295403165,
Cond:6387, T:134581, TR:-0.0264832537678455, Period:5.884121829976, Benefit0.68696918584347,
Cond:2348, T:133804, TR:-0.0262326166687195, Period:8.39189411377836, Benefit0.684329317509193,
Cond:556, T:140693, TR:-0.0274687754726989, Period:8.37038800793216, Benefit0.681291890854556,
Cond:6844, T:138650, TR:-0.0270467569129891, Period:10.1320952037505, Benefit0.680728452939055,
Cond:4346, T:133533, TR:-0.0260089132520359, Period:6.89484247339609, Benefit0.679704642298159,
Cond:1662, T:144158, TR:-0.0280290999576817, Period:7.33830935501325, Benefit0.678290486826954,
Cond:3916, T:144334, TR:-0.0278773040886823, Period:8.67609156539693, Benefit0.673631992461929,
Cond:3036, T:137481, TR:-0.0265312996559689, Period:9.67602068649486, Benefit0.673183930870448,
Cond:1452, T:143081, TR:-0.0275963004735859, Period:8.52280875867516, Benefit0.672681907451024,
Cond:1006, T:139180, TR:-0.0268098280203092, Period:8.50051013076591, Benefit0.67187814341141,
Cond:108, T:142759, TR:-0.0273895286716109, Period:8.35431041125253, Benefit0.669029623351242,
Cond:6412, T:144448, TR:-0.0276582664020266, Period:10.8405377713779, Benefit0.667603566681436,
Cond:4828, T:144379, TR:-0.0275948657714831, Period:9.84467962792373, Benefit0.666350369513572,
Cond:3470, T:139616, TR:-0.0266534604696191, Period:8.6176942470777, Benefit0.665647203758882,
Cond:2572, T:149693, TR:-0.0285531054875564, Period:8.68856927177623, Benefit0.664793944940645,
Cond:4344, T:139043, TR:-0.0264838534283706, Period:7.09772516415785, Benefit0.66408952626166,
Cond:8366, T:148580, TR:-0.0282581278549297, Period:7.02981558756226, Benefit0.662828106070804,
Cond:334, T:155754, TR:-0.0296231024680395, Period:8.66599894705754, Benefit0.662538361775621,
Cond:780, T:154890, TR:-0.0292468258386954, Period:8.67937891406805, Benefit0.657653818839176,
Cond:8364, T:143954, TR:-0.0271321797342071, Period:6.58586770773997, Benefit0.656793142253776,
Cond:2782, T:153428, TR:-0.0289198030442115, Period:7.69192715801549, Benefit0.656522929321897,
Cond:7068, T:160059, TR:-0.0301709806986539, Period:10.0886360654509, Benefit0.656226766379897,
Cond:6138, T:152233, TR:-0.0286151500108392, Period:7.48608383202065, Benefit0.654693791753431,
Cond:4126, T:133683, TR:-0.025024406538932, Period:7.39933274986348, Benefit0.652244488828048,
Cond:4606, T:145840, TR:-0.0273015240127169, Period:9.64434311574328, Benefit0.65213933077345,
Cond:7500, T:146571, TR:-0.0273771707935699, Period:8.95625328339167, Benefit0.650613013488344, , T2:766,6126,8372,1228,6387,2348,556,6844,4346,1662,3916,3036,1452,1006,108,6412,4828,3470,2572,4344,8366,334,780,8364,2782,7068,6138,4126,4606,7500,  #End#
LowScoreRank2 , T0:8376 , T1:
Cond:8341, T:133823, TR:-0.0119996409982373, Period:13.5198732654327, Benefit0.299104040411588,
Cond:1631, T:132932, TR:-0.0129301631196862, Period:13.4769054855114, Benefit0.326542894111275,
Cond:6105, T:132995, TR:-0.0130772726269233, Period:13.6083311402684, Benefit0.330388360464679,
Cond:1645, T:132642, TR:-0.0130862128647325, Period:13.506483617557, Benefit0.331546568960058,
Cond:735, T:136494, TR:-0.0133704297466941, Period:13.2689422245666, Benefit0.329355136489516,
Cond:6115, T:135446, TR:-0.0136956148000796, Period:13.396593476367, Benefit0.340696661400115,
Cond:2749, T:137857, TR:-0.0137105497445555, Period:13.2752199743212, Benefit0.334890502477205,
Cond:4303, T:135477, TR:-0.0138905152835725, Period:13.3736427585494, Benefit0.345822538143006,
Cond:749, T:136820, TR:-0.0139553717461207, Period:13.2315012425084, Benefit0.344006724163134,
Cond:6091, T:139463, TR:-0.0142322090753973, Period:13.1942378982239, Benefit0.344399589855374,
Cond:63, T:140787, TR:-0.0142559614531865, Period:13.0064139444693, Benefit0.341636656793596,
Cond:2735, T:140783, TR:-0.0142797413443944, Period:13.0654269336497, Benefit0.342257232762478,
Cond:6093, T:133028, TR:-0.0143007970030852, Period:13.5864254141985, Benefit0.363615178759359,
Cond:303, T:138735, TR:-0.014346319064663, Period:12.92560637186, Benefit0.349255775399142,
Cond:4301, T:139945, TR:-0.014426640996907, Period:13.16222801815, Benefit0.348186787666583,
Cond:1887, T:133872, TR:-0.0145653830755694, Period:12.4183399067766, Benefit0.368389207601291,
Cond:4315, T:139328, TR:-0.0146379773279809, Period:13.173554490124, Benefit0.355276757005053,
Cond:4337, T:136867, TR:-0.0147330503327926, Period:13.2207690677811, Benefit0.3644413920083,
Cond:1629, T:142618, TR:-0.0147415573905369, Period:12.9544026700697, Benefit0.349366840090311,
Cond:1615, T:143883, TR:-0.0147699838609937, Period:12.8754543622249, Benefit0.346879061459658,
Cond:1199, T:137514, TR:-0.014854279856205, Period:12.9257457422517, Benefit0.365846386549733,
Cond:2333, T:135638, TR:-0.0148700658346706, Period:12.7837110544243, Benefit0.371533051209838,
Cond:1213, T:134890, TR:-0.0149073247683147, Period:12.7816072355252, Benefit0.374675661650234,
Cond:7931, T:133283, TR:-0.0149998373736713, Period:13.263161843596, Benefit0.381886662214986,
Cond:8327, T:149046, TR:-0.0150221463048832, Period:12.6033640621016, Benefit0.3404519410115,
Cond:511, T:143896, TR:-0.0150267533368965, Period:12.8159017623839, Benefit0.353289876021571,
Cond:77, T:141604, TR:-0.0150756171347822, Period:12.9289214993927, Benefit0.360498291008729,
Cond:4109, T:137883, TR:-0.0150874787861663, Period:13.1863899102863, Benefit0.37094493157242,
Cond:317, T:136985, TR:-0.0151398438022489, Period:12.7314815490747, Benefit0.374858561156331,
Cond:8313, T:147458, TR:-0.0152214974303021, Period:12.7289872370438, Benefit0.349157048108614, , T2:8341,1631,6105,1645,735,6115,2749,4303,749,6091,63,2735,6093,303,4301,1887,4315,4337,1629,1615,1199,2333,1213,7931,8327,511,77,4109,317,8313,  #End#
LowScoreRank3 , T0:8376 , T1:
Cond:1, T:220871, TR:-0.0363802802285785, Period:9.66746200270746, Benefit0.566593169768779,
Cond:3, T:220788, TR:-0.0364012135577452, Period:9.67048027972535, Benefit0.567154917839738,
Cond:5, T:220675, TR:-0.0363808091910755, Period:9.67427211963294, Benefit0.567137192704203,
Cond:6, T:230, TR:-0.00089660837956926, Period:2.01304347826087, Benefit-0.252173913043478,
Cond:7, T:220642, TR:-0.0363932020630155, Period:9.67540178207232, Benefit0.567425966044543,
Cond:8, T:1904, TR:-0.00139394647562925, Period:1.86029411764706, Benefit0.949054621848739,
Cond:9, T:218968, TR:-0.0358485281706193, Period:9.7353083555588, Benefit0.563246684447043,
Cond:10, T:5801, TR:-0.00241217684635051, Period:2.06860886054129, Benefit0.968626098948457,
Cond:11, T:215071, TR:-0.0347321699105744, Period:9.87238167860846, Benefit0.555728108392112,
Cond:12, T:19361, TR:-0.00653708810194016, Period:2.79427715510562, Benefit1.08439646712463,
Cond:13, T:201511, TR:-0.0302724750503395, Period:10.3277885574485, Benefit0.516820421713951,
Cond:14, T:34407, TR:-0.0100982846649585, Period:3.15889208591275, Benefit0.992414334292441,
Cond:15, T:186465, TR:-0.0264594497207588, Period:10.8683935322983, Benefit0.487995066098195,
Cond:17, T:220826, TR:-0.0363543505294194, Period:9.66911052140599, Benefit0.566301069620425,
Cond:18, T:888, TR:-0.00130726122709708, Period:2.1722972972973, Benefit1.67004504504505,
Cond:19, T:219984, TR:-0.0359504891598126, Period:9.6976780129464, Benefit0.562118154047567,
Cond:20, T:1714, TR:-0.00164193275234921, Period:2.31155192532089, Benefit1.59743290548425,
Cond:21, T:219158, TR:-0.0355871424589222, Period:9.72495186121428, Benefit0.558510298506101,
Cond:22, T:2190, TR:-0.00157099216989599, Period:2.42694063926941, Benefit1.12831050228311,
Cond:23, T:218682, TR:-0.0356578568521499, Period:9.73993287056091, Benefit0.560946945793435,
Cond:24, T:9666, TR:-0.00465565601807104, Period:2.15083798882682, Benefit1.44961721498034,
Cond:25, T:211206, TR:-0.0323258465865992, Period:10.0114248648239, Benefit0.526159294717006,
Cond:26, T:18558, TR:-0.00607453522067656, Period:2.4870136868197, Benefit1.03847397348852,
Cond:27, T:202314, TR:-0.0307643183676503, Period:10.3260723429916, Benefit0.523285585772611,
Cond:28, T:43984, TR:-0.0130713384594904, Period:3.36144961804292, Benefit1.02496362313569,
Cond:29, T:176888, TR:-0.0233321695629791, Period:11.2354314594546, Benefit0.452591470308896,
Cond:30, T:60561, TR:-0.0175061631101757, Period:3.5760307788841, Benefit1.01136044649197,
Cond:31, T:160311, TR:-0.0187269582723966, Period:11.9685798229691, Benefit0.398544079944608,
Cond:33, T:220545, TR:-0.0363112829328013, Period:9.67848284930513, Benefit0.566378743567073,
Cond:34, T:2541, TR:-0.00199061014698808, Period:2.64344746162928, Benefit1.59189295552932, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:8376 , T1:
Cond:1, T:220871, TR:-0.0363802802285785, Period:9.66746200270746, Benefit0.566593169768779,
Cond:3, T:220788, TR:-0.0364012135577452, Period:9.67048027972535, Benefit0.567154917839738,
Cond:5, T:220675, TR:-0.0363808091910755, Period:9.67427211963294, Benefit0.567137192704203,
Cond:6, T:230, TR:-0.00089660837956926, Period:2.01304347826087, Benefit-0.252173913043478,
Cond:7, T:220642, TR:-0.0363932020630155, Period:9.67540178207232, Benefit0.567425966044543,
Cond:8, T:1904, TR:-0.00139394647562925, Period:1.86029411764706, Benefit0.949054621848739,
Cond:9, T:218968, TR:-0.0358485281706193, Period:9.7353083555588, Benefit0.563246684447043,
Cond:10, T:5801, TR:-0.00241217684635051, Period:2.06860886054129, Benefit0.968626098948457,
Cond:11, T:215071, TR:-0.0347321699105744, Period:9.87238167860846, Benefit0.555728108392112,
Cond:12, T:19361, TR:-0.00653708810194016, Period:2.79427715510562, Benefit1.08439646712463,
Cond:13, T:201511, TR:-0.0302724750503395, Period:10.3277885574485, Benefit0.516820421713951,
Cond:14, T:34407, TR:-0.0100982846649585, Period:3.15889208591275, Benefit0.992414334292441,
Cond:15, T:186465, TR:-0.0264594497207588, Period:10.8683935322983, Benefit0.487995066098195,
Cond:17, T:220826, TR:-0.0363543505294194, Period:9.66911052140599, Benefit0.566301069620425,
Cond:18, T:888, TR:-0.00130726122709708, Period:2.1722972972973, Benefit1.67004504504505,
Cond:19, T:219984, TR:-0.0359504891598126, Period:9.6976780129464, Benefit0.562118154047567,
Cond:20, T:1714, TR:-0.00164193275234921, Period:2.31155192532089, Benefit1.59743290548425,
Cond:21, T:219158, TR:-0.0355871424589222, Period:9.72495186121428, Benefit0.558510298506101,
Cond:22, T:2190, TR:-0.00157099216989599, Period:2.42694063926941, Benefit1.12831050228311,
Cond:23, T:218682, TR:-0.0356578568521499, Period:9.73993287056091, Benefit0.560946945793435,
Cond:24, T:9666, TR:-0.00465565601807104, Period:2.15083798882682, Benefit1.44961721498034,
Cond:25, T:211206, TR:-0.0323258465865992, Period:10.0114248648239, Benefit0.526159294717006,
Cond:26, T:18558, TR:-0.00607453522067656, Period:2.4870136868197, Benefit1.03847397348852,
Cond:27, T:202314, TR:-0.0307643183676503, Period:10.3260723429916, Benefit0.523285585772611,
Cond:28, T:43984, TR:-0.0130713384594904, Period:3.36144961804292, Benefit1.02496362313569,
Cond:29, T:176888, TR:-0.0233321695629791, Period:11.2354314594546, Benefit0.452591470308896,
Cond:30, T:60561, TR:-0.0175061631101757, Period:3.5760307788841, Benefit1.01136044649197,
Cond:31, T:160311, TR:-0.0187269582723966, Period:11.9685798229691, Benefit0.398544079944608,
Cond:33, T:220545, TR:-0.0363112829328013, Period:9.67848284930513, Benefit0.566378743567073,
Cond:34, T:2541, TR:-0.00199061014698808, Period:2.64344746162928, Benefit1.59189295552932, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:6844 , T1:
Cond:8376, T:138650, TR:-0.0270467569129891, Period:10.1320952037505, Benefit0.680728452939055,
Cond:4350, T:147432, TR:-0.0268761447913127, Period:10.906309349395, Benefit0.634394161376092,
Cond:8378, T:155236, TR:-0.0282537068970594, Period:11.1397936045763, Benefit0.633094127650803,
Cond:1006, T:139492, TR:-0.0253669326308557, Period:11.7689975052333, Benefit0.632918016803831,
Cond:6140, T:144210, TR:-0.0259177107090196, Period:10.7149989598502, Benefit0.625171624713959,
Cond:5038, T:137950, TR:-0.0246784726166015, Period:11.5277419354839, Benefit0.62222544400145,
Cond:6142, T:161128, TR:-0.0287789150054066, Period:11.5512449729408, Benefit0.620624596594012,
Cond:334, T:152121, TR:-0.0270662827111954, Period:12.2409594993459, Benefit0.618514209083558,
Cond:3689, T:142005, TR:-0.0251578881319368, Period:15.8276046618077, Benefit0.615950142600613,
Cond:2126, T:152041, TR:-0.0268766644226993, Period:12.2082530370098, Benefit0.614360600101288,
Cond:2348, T:138760, TR:-0.0244779789288316, Period:12.2423248774863, Benefit0.613224272124532,
Cond:3470, T:147467, TR:-0.0259558027587037, Period:11.9569191751375, Benefit0.611723300806282,
Cond:7936, T:173582, TR:-0.0305264384235463, Period:12.7975596548029, Benefit0.610086299270662,
Cond:3916, T:152137, TR:-0.0265087731707094, Period:12.3879069522864, Benefit0.605243957748608,
Cond:8380, T:171208, TR:-0.0298420940809972, Period:11.974679921499, Benefit0.604644642773702,
Cond:5262, T:158630, TR:-0.0275290346493687, Period:12.3248376725714, Benefit0.602515287146189,
Cond:7500, T:159863, TR:-0.027715387643417, Period:12.2659151898813, Benefit0.601846581135097,
Cond:8382, T:177433, TR:-0.030802488818721, Period:12.2679321208569, Benefit0.601748265542487,
Cond:110, T:167106, TR:-0.0289695733941323, Period:12.7811449020382, Benefit0.601486481634412,
Cond:3694, T:159716, TR:-0.0276541738031859, Period:12.4151243457136, Benefit0.601048110395953,
Cond:556, T:141131, TR:-0.0243891619747327, Period:12.4460890945292, Benefit0.600236659557432,
Cond:1230, T:164717, TR:-0.0284756667532921, Period:12.6653958000692, Benefit0.599865223383136,
Cond:558, T:168325, TR:-0.0291007056709756, Period:12.7963166493391, Benefit0.599714837368187,
Cond:5257, T:146678, TR:-0.0253251630815075, Period:16.3754073548862, Benefit0.599640027816032,
Cond:1452, T:147490, TR:-0.0254406380757754, Period:12.600345786155, Benefit0.599023662621195,
Cond:2121, T:148732, TR:-0.025549506768707, Period:15.42424629535, Benefit0.596448645886561,
Cond:3465, T:162322, TR:-0.0278735343426032, Period:15.6770308399354, Benefit0.595809563706706,
Cond:2350, T:168654, TR:-0.028868030855784, Period:12.7568275878426, Benefit0.593540621627711,
Cond:770, T:173637, TR:-0.0297260693216249, Period:14.4073843708426, Benefit0.593375835795366,
Cond:2572, T:157978, TR:-0.0269621730753191, Period:12.7939776424565, Benefit0.592209041765309, , T2:8376,4350,8378,1006,6140,5038,6142,334,3689,2126,2348,3470,7936,3916,8380,5262,7500,8382,110,3694,556,1230,558,5257,1452,2121,3465,2350,770,2572,  #End#
LowScoreRank2 , T0:6844 , T1:
Cond:8155, T:137568, TR:-0.0161855982891192, Period:19.7916448592696, Benefit0.400703652012096,
Cond:319, T:142625, TR:-0.0163350477808774, Period:19.2479228746713, Benefit0.389707274320771,
Cond:4598, T:138627, TR:-0.0167696661196004, Period:18.9626191146025, Benefit0.412755090999589,
Cond:8143, T:140617, TR:-0.0168132078802104, Period:19.698073490403, Benefit0.407795643485496,
Cond:8351, T:144160, TR:-0.0168169659043638, Period:19.7857450055494, Benefit0.397454217536071,
Cond:2767, T:146337, TR:-0.0168243402076569, Period:19.6043174316817, Benefit0.391473106596418,
Cond:5693, T:138154, TR:-0.0168451674141186, Period:19.6013651432459, Benefit0.416202209128943,
Cond:4125, T:141149, TR:-0.0168832031860647, Period:19.4694896882018, Benefit0.407987304196275,
Cond:8373, T:142491, TR:-0.0169855334292069, Period:19.8754938908422, Benefit0.406580064705841,
Cond:8371, T:149509, TR:-0.0170369023910676, Period:19.6628162853072, Benefit0.387936512183213,
Cond:6125, T:142945, TR:-0.0170380568320468, Period:19.7874077442373, Benefit0.406561964391899,
Cond:6386, T:146879, TR:-0.017051645418648, Period:19.4311984694885, Benefit0.395550078636156,
Cond:1647, T:150949, TR:-0.0170697922943714, Period:19.423109792049, Benefit0.384858462129593,
Cond:1661, T:143032, TR:-0.017108237495757, Period:19.2952066670395, Benefit0.40807651434644,
Cond:8363, T:145078, TR:-0.0171423122869693, Period:19.7874040171494, Benefit0.402928080067274,
Cond:751, T:154865, TR:-0.0172383594517921, Period:19.2543053627353, Benefit0.378613631227198,
Cond:5903, T:143849, TR:-0.0172453109818592, Period:19.6104526274079, Benefit0.409102600643731,
Cond:4345, T:140741, TR:-0.0173386668304675, Period:19.3649682750584, Benefit0.420915014103921,
Cond:6111, T:151107, TR:-0.0173660637991397, Period:19.5707611162951, Benefit0.391504033565619,
Cond:8369, T:157972, TR:-0.0174125531030211, Period:19.1963385916492, Benefit0.374794267338516,
Cond:2111, T:144523, TR:-0.0174127798318341, Period:19.1531313354968, Benefit0.411297855704628,
Cond:6131, T:154819, TR:-0.0174231357449736, Period:19.4101434578443, Benefit0.383027922929356,
Cond:7709, T:144677, TR:-0.0175326292832354, Period:19.4231909702302, Benefit0.413832191709809,
Cond:8349, T:151460, TR:-0.0175395459854039, Period:19.5549914168757, Benefit0.394678462960518,
Cond:8355, T:162573, TR:-0.0175489875275345, Period:18.9749281861072, Benefit0.366709109138664,
Cond:6133, T:150073, TR:-0.0176307220463308, Period:19.5837225883403, Benefit0.400678336542882,
Cond:79, T:158391, TR:-0.0176459180485445, Period:19.080219204374, Benefit0.379055628160691,
Cond:3679, T:141206, TR:-0.0176696486772487, Period:19.2789187428296, Benefit0.427942155432489,
Cond:4333, T:152443, TR:-0.0177380894109914, Period:19.4798449256443, Benefit0.396712213745466,
Cond:4319, T:155148, TR:-0.0177680864640704, Period:19.3903176321964, Benefit0.390182277567226, , T2:8155,319,4598,8143,8351,2767,5693,4125,8373,8371,6125,6386,1647,1661,8363,751,5903,4345,6111,8369,2111,6131,7709,8349,8355,6133,79,3679,4333,4319,  #End#
LowScoreRank3 , T0:6844 , T1:
Cond:1, T:229237, TR:-0.0375532153178039, Period:14.5941274750586, Benefit0.562627324559299,
Cond:3, T:229194, TR:-0.0375794187444621, Period:14.5965775718387, Benefit0.563143014215032,
Cond:5, T:229137, TR:-0.0375807966000983, Period:14.5995845280334, Benefit0.563313650785338,
Cond:6, T:106, TR:-0.000888855281001816, Period:2.16981132075472, Benefit-0.820754716981132,
Cond:7, T:229131, TR:-0.0375767618657564, Period:14.5998751805736, Benefit0.563267301238156,
Cond:8, T:1000, TR:-0.00111842928421375, Period:1.914, Benefit0.774,
Cond:9, T:228237, TR:-0.037323095105864, Period:14.6496843193698, Benefit0.561701214088864,
Cond:10, T:3386, TR:-0.00185884473730508, Period:2.08417011222682, Benefit1.0481393975192,
Cond:11, T:225851, TR:-0.0365115268145541, Period:14.7816790716003, Benefit0.555348437686794,
Cond:12, T:14351, TR:-0.0052365386320406, Period:2.87931154623371, Benefit1.12626297818967,
Cond:13, T:214886, TR:-0.0328351570711864, Period:15.3764926519178, Benefit0.524985341064565,
Cond:14, T:29904, TR:-0.00890486344148777, Period:3.22538790797218, Benefit0.994716425896201,
Cond:15, T:199333, TR:-0.0288713052602507, Period:16.2996693974405, Benefit0.497805180276221,
Cond:17, T:229210, TR:-0.0375381787867064, Period:14.5957200820209, Benefit0.562466733563108,
Cond:18, T:418, TR:-0.00107191112882038, Period:2.14832535885167, Benefit1.43540669856459,
Cond:19, T:228819, TR:-0.0373786022874778, Period:14.6168631101438, Benefit0.561032956179338,
Cond:20, T:823, TR:-0.00123744754203645, Period:2.27946537059538, Benefit1.48359659781288,
Cond:21, T:228414, TR:-0.0371982128404819, Period:14.6384985158528, Benefit0.559308974055881,
Cond:22, T:1048, TR:-0.00121061591682966, Period:2.39026717557252, Benefit1.06870229007634,
Cond:23, T:228189, TR:-0.0372244783240522, Period:14.6501759506374, Benefit0.560303082094229,
Cond:24, T:5282, TR:-0.00313005214203953, Period:2.18004543733434, Benefit1.57364634608103,
Cond:25, T:223955, TR:-0.0351429852444015, Period:14.8869147819875, Benefit0.538782344667456,
Cond:26, T:11233, TR:-0.00441986905089761, Period:2.50752247841182, Benefit1.16816522745482,
Cond:27, T:218004, TR:-0.0337239003257672, Period:15.2169088640575, Benefit0.531426028880204,
Cond:28, T:34234, TR:-0.0107984987663453, Period:3.51562773850558, Benefit1.07358181924403,
Cond:29, T:195003, TR:-0.0268678650483738, Period:16.5390276047035, Benefit0.472926057547833,
Cond:30, T:51594, TR:-0.015422077446769, Period:3.68626196844594, Benefit1.0405861146645,
Cond:31, T:177643, TR:-0.0220033852975602, Period:17.7621690694258, Benefit0.423810676469098,
Cond:33, T:229067, TR:-0.0375159519056423, Period:14.6033780509633, Benefit0.562499181462192,
Cond:34, T:1192, TR:-0.00141259677320527, Period:2.54697986577181, Benefit1.5755033557047, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:6844 , T1:
Cond:1, T:229237, TR:-0.0375532153178039, Period:14.5941274750586, Benefit0.562627324559299,
Cond:3, T:229194, TR:-0.0375794187444621, Period:14.5965775718387, Benefit0.563143014215032,
Cond:5, T:229137, TR:-0.0375807966000983, Period:14.5995845280334, Benefit0.563313650785338,
Cond:6, T:106, TR:-0.000888855281001816, Period:2.16981132075472, Benefit-0.820754716981132,
Cond:7, T:229131, TR:-0.0375767618657564, Period:14.5998751805736, Benefit0.563267301238156,
Cond:8, T:1000, TR:-0.00111842928421375, Period:1.914, Benefit0.774,
Cond:9, T:228237, TR:-0.037323095105864, Period:14.6496843193698, Benefit0.561701214088864,
Cond:10, T:3386, TR:-0.00185884473730508, Period:2.08417011222682, Benefit1.0481393975192,
Cond:11, T:225851, TR:-0.0365115268145541, Period:14.7816790716003, Benefit0.555348437686794,
Cond:12, T:14351, TR:-0.0052365386320406, Period:2.87931154623371, Benefit1.12626297818967,
Cond:13, T:214886, TR:-0.0328351570711864, Period:15.3764926519178, Benefit0.524985341064565,
Cond:14, T:29904, TR:-0.00890486344148777, Period:3.22538790797218, Benefit0.994716425896201,
Cond:15, T:199333, TR:-0.0288713052602507, Period:16.2996693974405, Benefit0.497805180276221,
Cond:17, T:229210, TR:-0.0375381787867064, Period:14.5957200820209, Benefit0.562466733563108,
Cond:18, T:418, TR:-0.00107191112882038, Period:2.14832535885167, Benefit1.43540669856459,
Cond:19, T:228819, TR:-0.0373786022874778, Period:14.6168631101438, Benefit0.561032956179338,
Cond:20, T:823, TR:-0.00123744754203645, Period:2.27946537059538, Benefit1.48359659781288,
Cond:21, T:228414, TR:-0.0371982128404819, Period:14.6384985158528, Benefit0.559308974055881,
Cond:22, T:1048, TR:-0.00121061591682966, Period:2.39026717557252, Benefit1.06870229007634,
Cond:23, T:228189, TR:-0.0372244783240522, Period:14.6501759506374, Benefit0.560303082094229,
Cond:24, T:5282, TR:-0.00313005214203953, Period:2.18004543733434, Benefit1.57364634608103,
Cond:25, T:223955, TR:-0.0351429852444015, Period:14.8869147819875, Benefit0.538782344667456,
Cond:26, T:11233, TR:-0.00441986905089761, Period:2.50752247841182, Benefit1.16816522745482,
Cond:27, T:218004, TR:-0.0337239003257672, Period:15.2169088640575, Benefit0.531426028880204,
Cond:28, T:34234, TR:-0.0107984987663453, Period:3.51562773850558, Benefit1.07358181924403,
Cond:29, T:195003, TR:-0.0268678650483738, Period:16.5390276047035, Benefit0.472926057547833,
Cond:30, T:51594, TR:-0.015422077446769, Period:3.68626196844594, Benefit1.0405861146645,
Cond:31, T:177643, TR:-0.0220033852975602, Period:17.7621690694258, Benefit0.423810676469098,
Cond:33, T:229067, TR:-0.0375159519056423, Period:14.6033780509633, Benefit0.562499181462192,
Cond:34, T:1192, TR:-0.00141259677320527, Period:2.54697986577181, Benefit1.5755033557047, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:4606 , T1:
Cond:5260, T:135245, TR:-0.0253838587948363, Period:11.2030980812599, Benefit0.654042663314725,
Cond:8376, T:145840, TR:-0.0273015240127169, Period:9.64434311574328, Benefit0.65213933077345,
Cond:2348, T:134075, TR:-0.0246816130507779, Period:11.730277829573, Benefit0.641006899123625,
Cond:8156, T:135775, TR:-0.0246407076301335, Period:9.1974811268643, Benefit0.631581660835942,
Cond:3916, T:147678, TR:-0.0267986919612066, Period:11.8994162976205, Benefit0.631400750281017,
Cond:1902, T:136423, TR:-0.0246031135407492, Period:10.7931727054822, Benefit0.627467509144352,
Cond:2782, T:136522, TR:-0.0245797426770773, Period:9.44798640512152, Benefit0.626375236225663,
Cond:556, T:135882, TR:-0.0243408009584958, Period:11.8677087472954, Benefit0.623077375958552,
Cond:1452, T:141361, TR:-0.0252740623164362, Period:12.047070974314, Benefit0.621840535932825,
Cond:2572, T:150401, TR:-0.0267850902506183, Period:12.249825466586, Benefit0.619158117299752,
Cond:7276, T:140594, TR:-0.0247471582351398, Period:11.0704510861061, Benefit0.611825540207975,
Cond:108, T:135871, TR:-0.023895025589007, Period:11.8836101890764, Benefit0.61125626513384,
Cond:1006, T:145685, TR:-0.0255171313628695, Period:11.249936506847, Benefit0.608655661186807,
Cond:3246, T:144137, TR:-0.0252414266491523, Period:10.8953148740434, Benefit0.608559911750626,
Cond:3036, T:170648, TR:-0.0298644944984122, Period:14.1184250621162, Benefit0.607197271576579,
Cond:8158, T:148318, TR:-0.0258912229110889, Period:10.0798149921115, Benefit0.606494154451921,
Cond:4604, T:167187, TR:-0.029156419206851, Period:14.1217618594747, Benefit0.605190594962527,
Cond:5918, T:143341, TR:-0.0249514974981621, Period:9.75782225601886, Benefit0.604774628333833,
Cond:780, T:151480, TR:-0.0262758223836004, Period:12.4052020068656, Benefit0.602442566675469,
Cond:8378, T:164929, TR:-0.0285155103618233, Period:10.7144468225721, Benefit0.599924816133003,
Cond:3260, T:188027, TR:-0.0325276261360486, Period:14.085237758407, Benefit0.598834209980482,
Cond:334, T:156919, TR:-0.0270112138415944, Period:11.6747047839968, Benefit0.597512092225926,
Cond:7936, T:168020, TR:-0.0289295602433991, Period:12.0293596000476, Benefit0.597202713962623,
Cond:5482, T:133710, TR:-0.022984924243395, Period:12.0287487846833, Benefit0.596873831426221,
Cond:4828, T:186236, TR:-0.0321035842775279, Period:14.0995135204794, Benefit0.596780429132928,
Cond:1916, T:178294, TR:-0.0306580834582194, Period:14.170673157818, Benefit0.595796829955018,
Cond:3484, T:197757, TR:-0.0340596183135332, Period:14.0272303888105, Benefit0.595326587680841,
Cond:1676, T:165299, TR:-0.0283610499012839, Period:12.7423819865819, Benefit0.595163915087206,
Cond:7720, T:142805, TR:-0.0243221168163172, Period:11.8816988200693, Benefit0.591218794860124,
Cond:5706, T:161530, TR:-0.0275245358467127, Period:12.7904228316721, Benefit0.59110381972389, , T2:5260,8376,2348,8156,3916,1902,2782,556,1452,2572,7276,108,1006,3246,3036,8158,4604,5918,780,8378,3260,334,7936,5482,4828,1916,3484,1676,7720,5706,  #End#
LowScoreRank2 , T0:4606 , T1:
Cond:1647, T:136448, TR:-0.0140888018620189, Period:19.1185359990619, Benefit0.348521048311445,
Cond:8371, T:134824, TR:-0.0141095528286532, Period:19.466170711446, Benefit0.353445974010562,
Cond:751, T:141349, TR:-0.0141389049100646, Period:18.8492313352058, Benefit0.337229127903275,
Cond:8369, T:144416, TR:-0.0142736142474525, Period:18.8507367604698, Benefit0.333134832705517,
Cond:8355, T:149345, TR:-0.0144709090902031, Period:18.549111118551, Benefit0.326432086778935,
Cond:6111, T:135293, TR:-0.0145028615184083, Period:19.3844766543724, Benefit0.362694300518135,
Cond:79, T:145760, TR:-0.0145249725368413, Period:18.5873970911087, Benefit0.336155323819978,
Cond:6131, T:141000, TR:-0.0145506681815641, Period:19.0943829787234, Benefit0.348652482269504,
Cond:8359, T:145096, TR:-0.0146763263325567, Period:18.9640444946794, Benefit0.341525610630203,
Cond:8153, T:143529, TR:-0.0146832495463617, Period:18.8984525775279, Benefit0.345588696361014,
Cond:8357, T:146143, TR:-0.0146980419586543, Period:18.8611770662981, Benefit0.339509932052852,
Cond:4333, T:138814, TR:-0.0147077978624319, Period:19.089501058971, Benefit0.358465284481392,
Cond:6133, T:136890, TR:-0.014788699609397, Period:19.2570531083352, Benefit0.365848491489517,
Cond:1661, T:133859, TR:-0.0147923382046033, Period:18.5102458557138, Benefit0.374565774434293,
Cond:4319, T:140268, TR:-0.0147961571684753, Period:19.1195212022699, Benefit0.356873984087604,
Cond:8347, T:140803, TR:-0.0148030512946161, Period:19.1936677485565, Benefit0.355638729288438,
Cond:8361, T:136426, TR:-0.0148058721201837, Period:19.4383768489877, Benefit0.367598551595737,
Cond:527, T:146228, TR:-0.0148089555818257, Period:18.5356771616927, Benefit0.342041195940586,
Cond:991, T:136935, TR:-0.0148159707060709, Period:18.4517763902582, Benefit0.366443933253003,
Cond:5469, T:139382, TR:-0.0148251685054145, Period:18.6490436354766, Benefit0.359989094718113,
Cond:8345, T:144682, TR:-0.014833360118154, Period:18.9672868774277, Benefit0.346463278085733,
Cond:6123, T:147047, TR:-0.0148738499783388, Period:18.7673668962985, Benefit0.341645868327814,
Cond:2751, T:145746, TR:-0.0149704516954177, Period:18.7633965940746, Benefit0.347220506909281,
Cond:4317, T:148701, TR:-0.0150842111627662, Period:18.6015561428639, Benefit0.342781823928555,
Cond:6109, T:142863, TR:-0.0151099414461093, Period:19.004738805709, Benefit0.358056319690893,
Cond:1423, T:145064, TR:-0.0151130598771906, Period:18.5895122152981, Benefit0.352472012353168,
Cond:8343, T:151070, TR:-0.0151346395132271, Period:18.428099556497, Benefit0.338372939696829,
Cond:8341, T:157123, TR:-0.0151456491247478, Period:17.8707000248213, Benefit0.325006523551612,
Cond:3901, T:146547, TR:-0.0152049061997778, Period:18.2632124847319, Benefit0.351013667970003,
Cond:2557, T:144114, TR:-0.0152472811996991, Period:18.2186949220756, Benefit0.35825804571381, , T2:1647,8371,751,8369,8355,6111,79,6131,8359,8153,8357,4333,6133,1661,4319,8347,8361,527,991,5469,8345,6123,2751,4317,6109,1423,8343,8341,3901,2557,  #End#
LowScoreRank3 , T0:4606 , T1:
Cond:1, T:222055, TR:-0.0355413140430437, Period:13.7562495778073, Benefit0.5500303978744,
Cond:3, T:222018, TR:-0.0355514148935086, Period:13.7582718518318, Benefit0.550288715329388,
Cond:5, T:221955, TR:-0.0355635182323119, Period:13.7615012051993, Benefit0.550647653803699,
Cond:6, T:107, TR:-0.000886458572395339, Period:2.13084112149533, Benefit-0.897196261682243,
Cond:7, T:221948, TR:-0.0355674104759268, Period:13.7618541279939, Benefit0.550728098473516,
Cond:8, T:1071, TR:-0.00117226236029213, Period:1.88702147525677, Benefit0.911297852474323,
Cond:9, T:220984, TR:-0.0352543607694837, Period:13.8137738478804, Benefit0.548279513448937,
Cond:10, T:3826, TR:-0.00203447986222561, Period:2.0684788290643, Benefit1.09958180867747,
Cond:11, T:218229, TR:-0.0343133235798665, Period:13.9611600658024, Benefit0.54039563944297,
Cond:12, T:16606, TR:-0.00593038938549279, Period:2.88690834638083, Benefit1.12880886426593,
Cond:13, T:205449, TR:-0.0300939151848589, Period:14.6347950099538, Benefit0.503248981499058,
Cond:14, T:31867, TR:-0.00951202667297696, Period:3.21843913766592, Benefit1.0038284118367,
Cond:15, T:190188, TR:-0.0262497569308817, Period:15.5219151576335, Benefit0.473994153153722,
Cond:17, T:222032, TR:-0.0355235331072272, Period:13.7575394537724, Benefit0.549808135764214,
Cond:18, T:434, TR:-0.00108496686919955, Period:2.18433179723502, Benefit1.49539170506912,
Cond:19, T:221621, TR:-0.0353532656146652, Period:13.7789108432865, Benefit0.54817909855113,
Cond:20, T:875, TR:-0.00127795470817747, Period:2.30857142857143, Benefit1.56914285714286,
Cond:21, T:221180, TR:-0.0351439695413257, Period:13.8015372095126, Benefit0.545998734062754,
Cond:22, T:1115, TR:-0.00123461341461693, Period:2.43139013452915, Benefit1.08520179372197,
Cond:23, T:220940, TR:-0.0351877068953515, Period:13.8134018285507, Benefit0.547329591744365,
Cond:24, T:5591, TR:-0.00333648220052036, Period:2.16383473439456, Benefit1.62493292791987,
Cond:25, T:216464, TR:-0.0329176271320945, Period:14.0556674550965, Benefit0.522266982038584,
Cond:26, T:12586, TR:-0.00482122118269513, Period:2.50468774829175, Benefit1.1614492293024,
Cond:27, T:209469, TR:-0.0312920753840803, Period:14.432302631893, Benefit0.513293136454559,
Cond:28, T:37489, TR:-0.0117839057899923, Period:3.51639147483262, Benefit1.07714262850436,
Cond:29, T:184566, TR:-0.0238579331914226, Period:15.836167008008, Benefit0.442963492734306,
Cond:30, T:54836, TR:-0.0163420645753436, Period:3.68090305638632, Benefit1.04022904661172,
Cond:31, T:167219, TR:-0.0190986108889147, Period:17.060250330405, Benefit0.389279926324162,
Cond:33, T:221876, TR:-0.0354795078508128, Period:13.7655537327156, Benefit0.549518650056788,
Cond:34, T:1232, TR:-0.00139742662269884, Period:2.53084415584416, Benefit1.47808441558442, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:4606 , T1:
Cond:1, T:222055, TR:-0.0355413140430437, Period:13.7562495778073, Benefit0.5500303978744,
Cond:3, T:222018, TR:-0.0355514148935086, Period:13.7582718518318, Benefit0.550288715329388,
Cond:5, T:221955, TR:-0.0355635182323119, Period:13.7615012051993, Benefit0.550647653803699,
Cond:6, T:107, TR:-0.000886458572395339, Period:2.13084112149533, Benefit-0.897196261682243,
Cond:7, T:221948, TR:-0.0355674104759268, Period:13.7618541279939, Benefit0.550728098473516,
Cond:8, T:1071, TR:-0.00117226236029213, Period:1.88702147525677, Benefit0.911297852474323,
Cond:9, T:220984, TR:-0.0352543607694837, Period:13.8137738478804, Benefit0.548279513448937,
Cond:10, T:3826, TR:-0.00203447986222561, Period:2.0684788290643, Benefit1.09958180867747,
Cond:11, T:218229, TR:-0.0343133235798665, Period:13.9611600658024, Benefit0.54039563944297,
Cond:12, T:16606, TR:-0.00593038938549279, Period:2.88690834638083, Benefit1.12880886426593,
Cond:13, T:205449, TR:-0.0300939151848589, Period:14.6347950099538, Benefit0.503248981499058,
Cond:14, T:31867, TR:-0.00951202667297696, Period:3.21843913766592, Benefit1.0038284118367,
Cond:15, T:190188, TR:-0.0262497569308817, Period:15.5219151576335, Benefit0.473994153153722,
Cond:17, T:222032, TR:-0.0355235331072272, Period:13.7575394537724, Benefit0.549808135764214,
Cond:18, T:434, TR:-0.00108496686919955, Period:2.18433179723502, Benefit1.49539170506912,
Cond:19, T:221621, TR:-0.0353532656146652, Period:13.7789108432865, Benefit0.54817909855113,
Cond:20, T:875, TR:-0.00127795470817747, Period:2.30857142857143, Benefit1.56914285714286,
Cond:21, T:221180, TR:-0.0351439695413257, Period:13.8015372095126, Benefit0.545998734062754,
Cond:22, T:1115, TR:-0.00123461341461693, Period:2.43139013452915, Benefit1.08520179372197,
Cond:23, T:220940, TR:-0.0351877068953515, Period:13.8134018285507, Benefit0.547329591744365,
Cond:24, T:5591, TR:-0.00333648220052036, Period:2.16383473439456, Benefit1.62493292791987,
Cond:25, T:216464, TR:-0.0329176271320945, Period:14.0556674550965, Benefit0.522266982038584,
Cond:26, T:12586, TR:-0.00482122118269513, Period:2.50468774829175, Benefit1.1614492293024,
Cond:27, T:209469, TR:-0.0312920753840803, Period:14.432302631893, Benefit0.513293136454559,
Cond:28, T:37489, TR:-0.0117839057899923, Period:3.51639147483262, Benefit1.07714262850436,
Cond:29, T:184566, TR:-0.0238579331914226, Period:15.836167008008, Benefit0.442963492734306,
Cond:30, T:54836, TR:-0.0163420645753436, Period:3.68090305638632, Benefit1.04022904661172,
Cond:31, T:167219, TR:-0.0190986108889147, Period:17.060250330405, Benefit0.389279926324162,
Cond:33, T:221876, TR:-0.0354795078508128, Period:13.7655537327156, Benefit0.549518650056788,
Cond:34, T:1232, TR:-0.00139742662269884, Period:2.53084415584416, Benefit1.47808441558442, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:6142 , T1:
Cond:6136, T:135061, TR:-0.0271522673145801, Period:6.66585468788177, Benefit0.702364116954561,
Cond:766, T:142251, TR:-0.0279080106437545, Period:7.63271259955993, Benefit0.684684114698666,
Cond:6138, T:141971, TR:-0.0275640185368324, Period:7.23737242112826, Benefit0.677335512182065,
Cond:6126, T:143580, TR:-0.0277823192030805, Period:7.08780470817663, Benefit0.67492687003761,
Cond:5260, T:137934, TR:-0.0265805175274651, Period:9.37321472588339, Benefit0.672176548204214,
Cond:1228, T:136130, TR:-0.0261104799638329, Period:9.00977741864394, Benefit0.668941453022846,
Cond:2348, T:138129, TR:-0.0264506175095534, Period:9.1033526630903, Benefit0.667788806116022,
Cond:556, T:143274, TR:-0.0271755260620465, Period:9.06406605525078, Benefit0.661131817356952,
Cond:1902, T:140962, TR:-0.0266127500279475, Period:9.32250535605341, Benefit0.657992934265972,
Cond:1452, T:146644, TR:-0.0276690022542834, Period:9.24247838302283, Benefit0.657456152314449,
Cond:3916, T:151225, TR:-0.028390699356016, Period:9.45120846420896, Benefit0.653899818151761,
Cond:2572, T:154263, TR:-0.0288062527046014, Period:9.43460842846308, Benefit0.650162385017794,
Cond:4604, T:140287, TR:-0.026164024289278, Period:10.9902485618767, Benefit0.649725206184465,
Cond:108, T:144598, TR:-0.0269519927584725, Period:9.0499730286726, Benefit0.649248260695169,
Cond:8364, T:152595, TR:-0.0281982927065026, Period:7.30746092598054, Benefit0.643232084930699,
Cond:3036, T:150023, TR:-0.0276233676652899, Period:10.8199076141658, Benefit0.64092839098005,
Cond:780, T:157890, TR:-0.029011835440462, Period:9.44972449173475, Benefit0.639248844131991,
Cond:6620, T:138865, TR:-0.025485044481766, Period:11.7162639974076, Benefit0.638965902135167,
Cond:4348, T:155752, TR:-0.0285747782749961, Period:8.08949483794751, Benefit0.638322461348811,
Cond:6389, T:143898, TR:-0.0263690868072672, Period:6.86769795271651, Benefit0.637910186382021,
Cond:1662, T:156629, TR:-0.0285920255368085, Period:8.27309757452324, Benefit0.634984581399358,
Cond:7276, T:139571, TR:-0.0254405096186639, Period:9.93014308129912, Benefit0.63445128285962,
Cond:3246, T:143652, TR:-0.0261571130621832, Period:9.65919026536352, Benefit0.633718987553254,
Cond:8366, T:162740, TR:-0.0294580053349725, Period:7.9996866166892, Benefit0.629181516529433,
Cond:1006, T:151687, TR:-0.0274193726274902, Period:9.49318662772683, Benefit0.628748673254794,
Cond:5916, T:143137, TR:-0.0258335936500571, Period:7.72949691554245, Benefit0.62792988535459,
Cond:6391, T:152684, TR:-0.0275174618280707, Period:7.60247308165885, Benefit0.626778182389772,
Cond:7718, T:138823, TR:-0.0249955423984593, Period:9.34131231856393, Benefit0.626416371926842,
Cond:1676, T:169944, TR:-0.0306391166250388, Period:9.7190074377442, Benefit0.626176858259191,
Cond:334, T:166850, TR:-0.0299652668011135, Period:9.5368894216362, Benefit0.623865747677555, , T2:6136,766,6138,6126,5260,1228,2348,556,1902,1452,3916,2572,4604,108,8364,3036,780,6620,4348,6389,1662,7276,3246,8366,1006,5916,6391,7718,1676,334,  #End#
LowScoreRank2 , T0:6142 , T1:
Cond:8341, T:138302, TR:-0.0113674826941667, Period:14.7545805555957, Benefit0.272497866986739,
Cond:8329, T:134631, TR:-0.012088777180613, Period:15.0731926525095, Benefit0.299633813906158,
Cond:1631, T:135555, TR:-0.0122051439981588, Period:14.8478034746044, Benefit0.300615986131091,
Cond:1645, T:136771, TR:-0.0124720900336225, Period:14.7900724568805, Benefit0.304896505838226,
Cond:6105, T:137307, TR:-0.0125220364315472, Period:14.8566205655939, Benefit0.304973526477164,
Cond:735, T:139194, TR:-0.0126729422838007, Period:14.6085894506947, Benefit0.304589278273489,
Cond:2749, T:141259, TR:-0.0130382731897896, Period:14.5543929944287, Benefit0.309297106733022,
Cond:4303, T:138069, TR:-0.0131799469069074, Period:14.7207265932251, Benefit0.3204557141719,
Cond:3901, T:135140, TR:-0.013188344163375, Period:14.3089388782004, Benefit0.327911795175374,
Cond:749, T:140865, TR:-0.013346202479731, Period:14.4873957335037, Benefit0.318098889007205,
Cond:6115, T:141366, TR:-0.0135193539905688, Period:14.5435111695882, Benefit0.321350253950738,
Cond:6091, T:142828, TR:-0.0135381148453319, Period:14.4622763043661, Benefit0.318396952978408,
Cond:63, T:143520, TR:-0.0135636919747265, Period:14.3111134336678, Benefit0.317440078037904,
Cond:6093, T:135994, TR:-0.0135758281905069, Period:14.9283718399341, Benefit0.336081003573687,
Cond:2735, T:143484, TR:-0.0135760173007602, Period:14.3685219257896, Benefit0.31783334727217,
Cond:303, T:141434, TR:-0.0136552128081402, Period:14.2499469717324, Benefit0.324660265565564,
Cond:4301, T:142966, TR:-0.0137462778887683, Period:14.4512891176923, Benefit0.323335618258887,
Cond:1887, T:135515, TR:-0.0138627557197093, Period:13.7508172527027, Benefit0.344980260487769,
Cond:4592, T:137981, TR:-0.0138912844080782, Period:14.5546850653351, Benefit0.339314833201673,
Cond:4315, T:143288, TR:-0.0140465400722893, Period:14.4056236391045, Benefit0.33014627882307,
Cond:1615, T:146609, TR:-0.0140741567324238, Period:14.1550314100771, Benefit0.323029281967683,
Cond:1629, T:145971, TR:-0.0140777646555821, Period:14.2028279589782, Benefit0.324591870988073,
Cond:1199, T:140089, TR:-0.0141314867839052, Period:14.2601489053387, Benefit0.340198016974923,
Cond:2333, T:139073, TR:-0.014194997371822, Period:14.1045062664931, Benefit0.344437813234776,
Cond:1213, T:138623, TR:-0.0142355398465211, Period:14.0970546013288, Benefit0.346659645224818,
Cond:4329, T:136676, TR:-0.014262150630284, Period:14.8683821592672, Benefit0.352505194767187,
Cond:8327, T:152420, TR:-0.0143081034025026, Period:13.8030573415562, Benefit0.315706600183703,
Cond:511, T:146622, TR:-0.0143261291391096, Period:14.0964862026163, Benefit0.329200256441735,
Cond:7931, T:136465, TR:-0.0143486498757331, Period:14.6025574323087, Benefit0.35536584472209,
Cond:4109, T:140914, TR:-0.0143834694989336, Period:14.4928183147168, Benefit0.34458605958244, , T2:8341,8329,1631,1645,6105,735,2749,4303,3901,749,6115,6091,63,6093,2735,303,4301,1887,4592,4315,1615,1629,1199,2333,1213,4329,8327,511,7931,4109,  #End#
LowScoreRank3 , T0:6142 , T1:
Cond:1, T:223612, TR:-0.0356943290245957, Period:10.5457757186555, Benefit0.548369497164732,
Cond:3, T:223529, TR:-0.0357152947243793, Period:10.5490831167321, Benefit0.54891759011135,
Cond:5, T:223416, TR:-0.0356948964686094, Period:10.5532728184195, Benefit0.548890858309163,
Cond:6, T:230, TR:-0.00089660837956926, Period:2.01304347826087, Benefit-0.252173913043478,
Cond:7, T:223383, TR:-0.0357073053688472, Period:10.5545184727576, Benefit0.549173392782799,
Cond:8, T:1904, TR:-0.00139394647562925, Period:1.86029411764706, Benefit0.949054621848739,
Cond:9, T:221709, TR:-0.0351625338985282, Period:10.6203221339684, Benefit0.544907964945041,
Cond:10, T:5799, TR:-0.00241404268079135, Period:2.06897740989826, Benefit0.970167270219003,
Cond:11, T:217814, TR:-0.034044106546884, Period:10.7714150605563, Benefit0.537118826154425,
Cond:12, T:19356, TR:-0.00653788245095074, Period:2.79412068609217, Benefit1.08483157677206,
Cond:13, T:204257, TR:-0.0295847456977341, Period:11.2802988392075, Benefit0.497510489236599,
Cond:14, T:34407, TR:-0.0100982846649585, Period:3.15889208591275, Benefit0.992414334292441,
Cond:15, T:189206, TR:-0.0257724910906195, Period:11.8890257180005, Benefit0.467596165026479,
Cond:17, T:223567, TR:-0.0356683879400303, Period:10.5475808146998, Benefit0.548077310157581,
Cond:18, T:883, TR:-0.00130646033900346, Period:2.17553793884485, Benefit1.67610419026048,
Cond:19, T:222730, TR:-0.0352652759481744, Period:10.5789161765366, Benefit0.543878238225654,
Cond:20, T:1706, TR:-0.00164565942267624, Period:2.31418522860492, Benefit1.61313012895662,
Cond:21, T:221907, TR:-0.0348970248710791, Period:10.6090163897489, Benefit0.54016322152974,
Cond:22, T:2175, TR:-0.00157898011117829, Period:2.42850574712644, Benefit1.14988505747126,
Cond:23, T:221438, TR:-0.034963428365721, Period:10.6254617545317, Benefit0.542440773489645,
Cond:24, T:9651, TR:-0.00465056425885158, Period:2.14889648741063, Benefit1.4499015646047,
Cond:25, T:213962, TR:-0.0316441290555798, Period:10.9244819173498, Benefit0.507683607369533,
Cond:26, T:18502, TR:-0.00605517420907487, Period:2.48275862068966, Benefit1.03772565128094,
Cond:27, T:205111, TR:-0.0300983741313182, Period:11.2730521522493, Benefit0.504205040197747,
Cond:28, T:43933, TR:-0.0130603799164258, Period:3.36141397127444, Benefit1.02524298363417,
Cond:29, T:179680, TR:-0.0226561647166625, Period:12.3023486197685, Benefit0.431745325022262,
Cond:30, T:60561, TR:-0.0175061631101757, Period:3.5760307788841, Benefit1.01136044649197,
Cond:31, T:163052, TR:-0.0180391009250428, Period:13.1344295071511, Benefit0.376376861369379,
Cond:33, T:223292, TR:-0.0356197388578642, Period:10.5576957526468, Benefit0.548026798989664,
Cond:34, T:2486, TR:-0.00198951498560921, Period:2.64641995172969, Benefit1.6255028157683, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:6142 , T1:
Cond:1, T:223612, TR:-0.0356943290245957, Period:10.5457757186555, Benefit0.548369497164732,
Cond:3, T:223529, TR:-0.0357152947243793, Period:10.5490831167321, Benefit0.54891759011135,
Cond:5, T:223416, TR:-0.0356948964686094, Period:10.5532728184195, Benefit0.548890858309163,
Cond:6, T:230, TR:-0.00089660837956926, Period:2.01304347826087, Benefit-0.252173913043478,
Cond:7, T:223383, TR:-0.0357073053688472, Period:10.5545184727576, Benefit0.549173392782799,
Cond:8, T:1904, TR:-0.00139394647562925, Period:1.86029411764706, Benefit0.949054621848739,
Cond:9, T:221709, TR:-0.0351625338985282, Period:10.6203221339684, Benefit0.544907964945041,
Cond:10, T:5799, TR:-0.00241404268079135, Period:2.06897740989826, Benefit0.970167270219003,
Cond:11, T:217814, TR:-0.034044106546884, Period:10.7714150605563, Benefit0.537118826154425,
Cond:12, T:19356, TR:-0.00653788245095074, Period:2.79412068609217, Benefit1.08483157677206,
Cond:13, T:204257, TR:-0.0295847456977341, Period:11.2802988392075, Benefit0.497510489236599,
Cond:14, T:34407, TR:-0.0100982846649585, Period:3.15889208591275, Benefit0.992414334292441,
Cond:15, T:189206, TR:-0.0257724910906195, Period:11.8890257180005, Benefit0.467596165026479,
Cond:17, T:223567, TR:-0.0356683879400303, Period:10.5475808146998, Benefit0.548077310157581,
Cond:18, T:883, TR:-0.00130646033900346, Period:2.17553793884485, Benefit1.67610419026048,
Cond:19, T:222730, TR:-0.0352652759481744, Period:10.5789161765366, Benefit0.543878238225654,
Cond:20, T:1706, TR:-0.00164565942267624, Period:2.31418522860492, Benefit1.61313012895662,
Cond:21, T:221907, TR:-0.0348970248710791, Period:10.6090163897489, Benefit0.54016322152974,
Cond:22, T:2175, TR:-0.00157898011117829, Period:2.42850574712644, Benefit1.14988505747126,
Cond:23, T:221438, TR:-0.034963428365721, Period:10.6254617545317, Benefit0.542440773489645,
Cond:24, T:9651, TR:-0.00465056425885158, Period:2.14889648741063, Benefit1.4499015646047,
Cond:25, T:213962, TR:-0.0316441290555798, Period:10.9244819173498, Benefit0.507683607369533,
Cond:26, T:18502, TR:-0.00605517420907487, Period:2.48275862068966, Benefit1.03772565128094,
Cond:27, T:205111, TR:-0.0300983741313182, Period:11.2730521522493, Benefit0.504205040197747,
Cond:28, T:43933, TR:-0.0130603799164258, Period:3.36141397127444, Benefit1.02524298363417,
Cond:29, T:179680, TR:-0.0226561647166625, Period:12.3023486197685, Benefit0.431745325022262,
Cond:30, T:60561, TR:-0.0175061631101757, Period:3.5760307788841, Benefit1.01136044649197,
Cond:31, T:163052, TR:-0.0180391009250428, Period:13.1344295071511, Benefit0.376376861369379,
Cond:33, T:223292, TR:-0.0356197388578642, Period:10.5576957526468, Benefit0.548026798989664,
Cond:34, T:2486, TR:-0.00198951498560921, Period:2.64641995172969, Benefit1.6255028157683, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:110 , T1:
Cond:6136, T:134984, TR:-0.0274494878679036, Period:6.96709980442126, Benefit0.710750903810822,
Cond:8366, T:142509, TR:-0.0285433818319627, Period:7.36702243367086, Benefit0.699499680721919,
Cond:8364, T:135410, TR:-0.027041333063681, Period:6.70152869064323, Benefit0.697518647071856,
Cond:766, T:140669, TR:-0.0278930280146433, Period:7.62053473046656, Benefit0.692313160682169,
Cond:6391, T:136960, TR:-0.0268959221476614, Period:7.1636609228972, Benefit0.685477511682243,
Cond:6138, T:142105, TR:-0.0276713302112694, Period:7.59448998979628, Benefit0.679399035924141,
Cond:1662, T:151151, TR:-0.0286992401028378, Period:8.17495749283829, Benefit0.661590065563575,
Cond:8156, T:144229, TR:-0.0272768795352203, Period:8.14777887942092, Benefit0.659111551768368,
Cond:4126, T:136569, TR:-0.0256554237873575, Period:8.12909225373255, Benefit0.654650762618164,
Cond:5916, T:137562, TR:-0.0258396693334631, Period:7.71244238961341, Benefit0.654584841744086,
Cond:8158, T:150360, TR:-0.0278831269442917, Period:8.89146714551742, Benefit0.645657089651503,
Cond:2124, T:135669, TR:-0.0251036221458951, Period:10.1724786060191, Benefit0.644443461660365,
Cond:2782, T:160296, TR:-0.0296711046411413, Period:8.6322116584319, Benefit0.644002345660528,
Cond:5260, T:146140, TR:-0.0268390892659112, Period:10.578192144519, Benefit0.639325304502532,
Cond:4348, T:156694, TR:-0.0287818037592213, Period:8.44664122429704, Benefit0.639067226568982,
Cond:8376, T:174519, TR:-0.0320906923876798, Period:8.84566150390502, Benefit0.638738475466855,
Cond:7276, T:138578, TR:-0.0253270617524835, Period:10.5198227712913, Benefit0.636219313310915,
Cond:3692, T:144924, TR:-0.0264308754585228, Period:10.4764428252049, Benefit0.634746487814303,
Cond:3246, T:147077, TR:-0.0267146780863399, Period:10.4424961074811, Benefit0.632029481156129,
Cond:5918, T:149457, TR:-0.0270623706726717, Period:8.73500070254321, Benefit0.62992700241541,
Cond:1902, T:152363, TR:-0.0274944687464868, Period:10.4696612694683, Benefit0.627613003156934,
Cond:332, T:143772, TR:-0.0257088134110416, Period:10.2734816236819, Benefit0.621908299251593,
Cond:5038, T:145659, TR:-0.0259588704262713, Period:10.5358680205137, Benefit0.619714538751467,
Cond:7498, T:137656, TR:-0.0245047884795824, Period:10.5052885453594, Benefit0.619043121985239,
Cond:4350, T:173919, TR:-0.0309691365578069, Period:9.29525813740879, Benefit0.617960084867093,
Cond:2348, T:153064, TR:-0.0269303857969661, Period:10.6740905764909, Benefit0.611339047718601,
Cond:1228, T:152049, TR:-0.0266771591476384, Period:10.6265414438766, Benefit0.609599536991365,
Cond:3470, T:160827, TR:-0.0282089014209283, Period:10.8082349356762, Benefit0.60910170555939,
Cond:3916, T:164793, TR:-0.0288592680000212, Period:10.9225937994939, Benefit0.607932375768388,
Cond:6142, T:182256, TR:-0.0318839219376202, Period:9.66855960846282, Benefit0.606207751733825, , T2:6136,8366,8364,766,6391,6138,1662,8156,4126,5916,8158,2124,2782,5260,4348,8376,7276,3692,3246,5918,1902,332,5038,7498,4350,2348,1228,3470,3916,6142,  #End#
LowScoreRank2 , T0:110 , T1:
Cond:8341, T:140214, TR:-0.0111759672888716, Period:16.3227709073274, Benefit0.263689788466202,
Cond:8329, T:137000, TR:-0.0118686592203292, Period:16.640197080292, Benefit0.288430656934307,
Cond:1631, T:136416, TR:-0.0120524575392362, Period:16.5437631949331, Benefit0.294591543513957,
Cond:6105, T:138932, TR:-0.0122834682166864, Period:16.4583465292373, Benefit0.295050816226643,
Cond:1645, T:137138, TR:-0.0123520090487231, Period:16.4969884350071, Benefit0.300879406145634,
Cond:735, T:139697, TR:-0.0125729127943242, Period:16.290629004202, Benefit0.300858286147877,
Cond:6095, T:135882, TR:-0.0127378847970326, Period:16.6030452892951, Benefit0.314051897970298,
Cond:1423, T:134766, TR:-0.0128298111753202, Period:16.3312779187629, Benefit0.319227401570129,
Cond:2749, T:141984, TR:-0.0128933627405338, Period:16.1858167117422, Benefit0.303963826910074,
Cond:4303, T:139792, TR:-0.0129145897816212, Period:16.316799244592, Benefit0.309481229254893,
Cond:6117, T:135276, TR:-0.0129645061296659, Period:16.78027883734, Benefit0.321579585440137,
Cond:6115, T:142684, TR:-0.0131101350456038, Period:16.1147851195649, Benefit0.307897171371703,
Cond:3901, T:136197, TR:-0.0131325062839056, Period:15.9716660425707, Benefit0.323780993707644,
Cond:749, T:141005, TR:-0.013233544776931, Period:16.1669657104358, Benefit0.314882450976916,
Cond:6093, T:137911, TR:-0.0133764972199144, Period:16.5214667430444, Benefit0.32599285046153,
Cond:6091, T:144331, TR:-0.0134033288599675, Period:16.0166076587843, Benefit0.311568547297531,
Cond:2735, T:144364, TR:-0.0134546904940615, Period:15.970297304037, Benefit0.312778809121388,
Cond:63, T:143906, TR:-0.01345519942841, Period:15.9528025238697, Benefit0.313829861159368,
Cond:303, T:141820, TR:-0.0135467926774656, Period:15.9159497955154, Benefit0.320977295162883,
Cond:4301, T:143903, TR:-0.013649041281003, Period:16.0476987971064, Benefit0.318700791505389,
Cond:1887, T:135901, TR:-0.0137545350511262, Period:15.4907984488709, Benefit0.341079167923709,
Cond:4315, T:144264, TR:-0.0138528641414139, Period:15.9944892696723, Benefit0.322970387622692,
Cond:6610, T:138131, TR:-0.0139081734958547, Period:16.4079316011612, Benefit0.33937349327812,
Cond:1615, T:147176, TR:-0.0139372222357119, Period:15.7479140620753, Benefit0.318373919660814,
Cond:4329, T:137737, TR:-0.0139395573784768, Period:16.5140376224253, Benefit0.341208244698229,
Cond:4592, T:143588, TR:-0.0139480198067288, Period:15.9472379307463, Benefit0.326949327241831,
Cond:1629, T:146415, TR:-0.0139716949455878, Period:15.8076358296623, Benefit0.320950722262063,
Cond:2771, T:138929, TR:-0.0139839760351132, Period:16.1965536353101, Benefit0.339317205191141,
Cond:1199, T:140662, TR:-0.0140147528299297, Period:15.9281184683852, Benefit0.335755214627973,
Cond:2333, T:139397, TR:-0.0141525685825624, Period:15.7594854982532, Benefit0.342503784156044, , T2:8341,8329,1631,6105,1645,735,6095,1423,2749,4303,6117,6115,3901,749,6093,6091,2735,63,303,4301,1887,4315,6610,1615,4329,4592,1629,2771,1199,2333,  #End#
LowScoreRank3 , T0:110 , T1:
Cond:1, T:223998, TR:-0.035585794857271, Period:11.6069563121099, Benefit0.545652193323155,
Cond:3, T:223915, TR:-0.0356067654002256, Period:11.6106513632405, Benefit0.546198334189313,
Cond:5, T:223802, TR:-0.0355863683877246, Period:11.615369835837, Benefit0.54617027551139,
Cond:6, T:230, TR:-0.00089660837956926, Period:2.01304347826087, Benefit-0.252173913043478,
Cond:7, T:223769, TR:-0.0355987796586746, Period:11.6167699726057, Benefit0.546451921401088,
Cond:8, T:1904, TR:-0.00139394647562925, Period:1.86029411764706, Benefit0.949054621848739,
Cond:9, T:222095, TR:-0.0350540000900138, Period:11.6904657916657, Benefit0.542173394268219,
Cond:10, T:5799, TR:-0.00241110863311521, Period:2.06863252284877, Benefit0.968270391446801,
Cond:11, T:218200, TR:-0.0339386804573547, Period:11.860403299725, Benefit0.534399633363886,
Cond:12, T:19352, TR:-0.00653867854823433, Period:2.7942848284415, Benefit1.08521083092187,
Cond:13, T:204647, TR:-0.0294753271630445, Period:12.4402556597458, Benefit0.49460778804478,
Cond:14, T:34407, TR:-0.0100982846649585, Period:3.15889208591275, Benefit0.992414334292441,
Cond:15, T:189592, TR:-0.0256639301084286, Period:13.1400481032955, Benefit0.464550191991223,
Cond:17, T:223953, TR:-0.0355598523200525, Period:11.6089715252754, Benefit0.545359963921001,
Cond:18, T:885, TR:-0.00130166615086144, Period:2.17175141242938, Benefit1.65197740112994,
Cond:19, T:223114, TR:-0.0351617986755657, Period:11.6443342865082, Benefit0.541243489875131,
Cond:20, T:1709, TR:-0.00164699294277888, Period:2.31421884142774, Benefit1.6132241076653,
Cond:21, T:222290, TR:-0.0347870110343781, Period:11.6783526024563, Benefit0.537424085653876,
Cond:22, T:2185, TR:-0.00157338834935157, Period:2.42471395881007, Benefit1.1350114416476,
Cond:23, T:221814, TR:-0.0348607263118329, Period:11.6973590485722, Benefit0.539826160657127,
Cond:24, T:9656, TR:-0.00465430855568283, Period:2.14944076222038, Benefit1.45060066280033,
Cond:25, T:214343, TR:-0.0315314504108551, Period:12.0329611883756, Benefit0.504863699770928,
Cond:26, T:18507, TR:-0.00605652050608356, Period:2.48408710217755, Benefit1.03771545901551,
Cond:27, T:205492, TR:-0.0299883461886733, Period:12.428527631246, Benefit0.5013139197633,
Cond:28, T:43942, TR:-0.0130668789483215, Period:3.36177233626144, Benefit1.02557917254563,
Cond:29, T:180057, TR:-0.0225407861556455, Period:13.619092842822, Benefit0.428503196210089,
Cond:30, T:60561, TR:-0.0175061631101757, Period:3.5760307788841, Benefit1.01136044649197,
Cond:31, T:163438, TR:-0.0179304993506797, Period:14.582704144691, Benefit0.373058896951749,
Cond:33, T:223673, TR:-0.0355145543562509, Period:11.6206068680619, Benefit0.545372038645702,
Cond:34, T:2518, TR:-0.001977539524908, Period:2.63502779984114, Benefit1.58697378872121, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:110 , T1:
Cond:1, T:223998, TR:-0.035585794857271, Period:11.6069563121099, Benefit0.545652193323155,
Cond:3, T:223915, TR:-0.0356067654002256, Period:11.6106513632405, Benefit0.546198334189313,
Cond:5, T:223802, TR:-0.0355863683877246, Period:11.615369835837, Benefit0.54617027551139,
Cond:6, T:230, TR:-0.00089660837956926, Period:2.01304347826087, Benefit-0.252173913043478,
Cond:7, T:223769, TR:-0.0355987796586746, Period:11.6167699726057, Benefit0.546451921401088,
Cond:8, T:1904, TR:-0.00139394647562925, Period:1.86029411764706, Benefit0.949054621848739,
Cond:9, T:222095, TR:-0.0350540000900138, Period:11.6904657916657, Benefit0.542173394268219,
Cond:10, T:5799, TR:-0.00241110863311521, Period:2.06863252284877, Benefit0.968270391446801,
Cond:11, T:218200, TR:-0.0339386804573547, Period:11.860403299725, Benefit0.534399633363886,
Cond:12, T:19352, TR:-0.00653867854823433, Period:2.7942848284415, Benefit1.08521083092187,
Cond:13, T:204647, TR:-0.0294753271630445, Period:12.4402556597458, Benefit0.49460778804478,
Cond:14, T:34407, TR:-0.0100982846649585, Period:3.15889208591275, Benefit0.992414334292441,
Cond:15, T:189592, TR:-0.0256639301084286, Period:13.1400481032955, Benefit0.464550191991223,
Cond:17, T:223953, TR:-0.0355598523200525, Period:11.6089715252754, Benefit0.545359963921001,
Cond:18, T:885, TR:-0.00130166615086144, Period:2.17175141242938, Benefit1.65197740112994,
Cond:19, T:223114, TR:-0.0351617986755657, Period:11.6443342865082, Benefit0.541243489875131,
Cond:20, T:1709, TR:-0.00164699294277888, Period:2.31421884142774, Benefit1.6132241076653,
Cond:21, T:222290, TR:-0.0347870110343781, Period:11.6783526024563, Benefit0.537424085653876,
Cond:22, T:2185, TR:-0.00157338834935157, Period:2.42471395881007, Benefit1.1350114416476,
Cond:23, T:221814, TR:-0.0348607263118329, Period:11.6973590485722, Benefit0.539826160657127,
Cond:24, T:9656, TR:-0.00465430855568283, Period:2.14944076222038, Benefit1.45060066280033,
Cond:25, T:214343, TR:-0.0315314504108551, Period:12.0329611883756, Benefit0.504863699770928,
Cond:26, T:18507, TR:-0.00605652050608356, Period:2.48408710217755, Benefit1.03771545901551,
Cond:27, T:205492, TR:-0.0299883461886733, Period:12.428527631246, Benefit0.5013139197633,
Cond:28, T:43942, TR:-0.0130668789483215, Period:3.36177233626144, Benefit1.02557917254563,
Cond:29, T:180057, TR:-0.0225407861556455, Period:13.619092842822, Benefit0.428503196210089,
Cond:30, T:60561, TR:-0.0175061631101757, Period:3.5760307788841, Benefit1.01136044649197,
Cond:31, T:163438, TR:-0.0179304993506797, Period:14.582704144691, Benefit0.373058896951749,
Cond:33, T:223673, TR:-0.0355145543562509, Period:11.6206068680619, Benefit0.545372038645702,
Cond:34, T:2518, TR:-0.001977539524908, Period:2.63502779984114, Benefit1.58697378872121, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:3038 , T1:
Cond:8376, T:161027, TR:-0.0299770959383771, Period:9.66357815770026, Benefit0.647773354779012,
Cond:2348, T:147211, TR:-0.0266480367638437, Period:11.6697053888636, Benefit0.629796686388925,
Cond:2782, T:147498, TR:-0.0266806498308471, Period:9.30660754722098, Benefit0.62931700768824,
Cond:3916, T:161113, TR:-0.0289073505844693, Period:11.816644218654, Benefit0.623549930793915,
Cond:3246, T:152126, TR:-0.0269874918207152, Period:10.6763998264596, Benefit0.616627006560351,
Cond:8158, T:156915, TR:-0.0277204724640638, Period:9.87402096676545, Benefit0.613790905904471,
Cond:5918, T:151970, TR:-0.0268345018401761, Period:9.53756004474567, Benefit0.6136605909061,
Cond:556, T:149821, TR:-0.026422951051368, Period:11.8129901682675, Benefit0.612944780771721,
Cond:1452, T:155682, TR:-0.0274034080957901, Period:11.9867871687157, Benefit0.611541475571999,
Cond:1006, T:157224, TR:-0.0276605906925109, Period:11.0941713733272, Benefit0.611159873810614,
Cond:2572, T:165399, TR:-0.0288939494785745, Period:12.1936589701268, Benefit0.606351912647598,
Cond:108, T:149937, TR:-0.0260065970508875, Period:11.8255800769657, Benefit0.602439691337028,
Cond:8378, T:180926, TR:-0.0313250444974202, Period:10.6629174358578, Benefit0.599858505687408,
Cond:4350, T:169037, TR:-0.0290635372632813, Period:10.2937936664754, Benefit0.596277738010021,
Cond:334, T:170479, TR:-0.0293059510351364, Period:11.537855102388, Benefit0.596085148317388,
Cond:3036, T:186993, TR:-0.032091336828767, Period:14.0601733754739, Benefit0.594000844951415,
Cond:780, T:167053, TR:-0.0286062523066152, Period:12.3400597415192, Benefit0.593883378329033,
Cond:4604, T:179963, TR:-0.0307848465636882, Period:14.1224029383818, Benefit0.592505126053689,
Cond:5038, T:155611, TR:-0.0265360262919616, Period:10.9814473269885, Benefit0.591770504655841,
Cond:3260, T:206653, TR:-0.0351863747482197, Period:14.0271759906704, Benefit0.587579178623102,
Cond:6142, T:182188, TR:-0.0309047828929285, Period:10.8706391200299, Benefit0.587245043581356,
Cond:6140, T:167361, TR:-0.0283377693624435, Period:10.195547349741, Benefit0.586982630361912,
Cond:3470, T:169816, TR:-0.0287395939359622, Period:11.2697272341829, Benefit0.586570170066425,
Cond:1676, T:182193, TR:-0.0308614415042099, Period:12.6772927609733, Benefit0.586378181379087,
Cond:7936, T:185943, TR:-0.0314918047253767, Period:11.997316381902, Benefit0.586029051913759,
Cond:3484, T:217644, TR:-0.036971023196038, Period:13.9684944220838, Benefit0.585134439727261,
Cond:2126, T:173372, TR:-0.029277131412023, Period:11.5198244237824, Benefit0.585054103315414,
Cond:4828, T:202557, TR:-0.0342660986727299, Period:14.0861140320996, Benefit0.584033136351743,
Cond:1916, T:196257, TR:-0.0331673956548997, Period:14.103124984077, Benefit0.583958788731103,
Cond:6396, T:149369, TR:-0.0250647704703653, Period:14.7846340271408, Benefit0.582095347762923, , T2:8376,2348,2782,3916,3246,8158,5918,556,1452,1006,2572,108,8378,4350,334,3036,780,4604,5038,3260,6142,6140,3470,1676,7936,3484,2126,4828,1916,6396,  #End#
LowScoreRank2 , T0:3038 , T1:
Cond:8371, T:149590, TR:-0.0154715329981228, Period:19.2649642355772, Benefit0.349989972591751,
Cond:1647, T:152643, TR:-0.0157503133553178, Period:18.8823856973461, Benefit0.349259383004789,
Cond:8369, T:160205, TR:-0.0158716325682307, Period:18.6899722230892, Benefit0.334752348553416,
Cond:751, T:157909, TR:-0.0158762360815763, Period:18.6232007042031, Benefit0.339955290705406,
Cond:6131, T:156294, TR:-0.0160336198246425, Period:18.9241749523334, Benefit0.347255812763126,
Cond:8349, T:149545, TR:-0.0161042897279342, Period:19.2633120465412, Benefit0.365354909893343,
Cond:8355, T:165781, TR:-0.0161425915463604, Period:18.3919930510734, Benefit0.328825378059006,
Cond:6111, T:151786, TR:-0.0161886864178069, Period:19.1162887222801, Benefit0.361726377926818,
Cond:6133, T:151406, TR:-0.0162219454616093, Period:19.0986948998058, Benefit0.363466441224258,
Cond:79, T:162716, TR:-0.0162760827110076, Period:18.3679416898154, Benefit0.338270360628334,
Cond:8359, T:161373, TR:-0.0163220380813983, Period:18.7780793565218, Benefit0.342244365538225,
Cond:1661, T:148431, TR:-0.0163233579878302, Period:18.3324979283303, Benefit0.373540567671174,
Cond:4333, T:155001, TR:-0.0163570380809387, Period:18.8638137818466, Benefit0.357797691627796,
Cond:8153, T:159868, TR:-0.0163967067847783, Period:18.7011471964371, Benefit0.347299021692897,
Cond:8357, T:162468, TR:-0.0164007796509204, Period:18.6821712583401, Benefit0.34156880124086,
Cond:8361, T:152136, TR:-0.0164433990790368, Period:19.219152600305, Benefit0.366888836304359,
Cond:4319, T:157003, TR:-0.0164442412474224, Period:18.8663210257129, Benefit0.355025063215353,
Cond:8347, T:157021, TR:-0.0164740633770305, Period:18.9810980696849, Benefit0.355665802663338,
Cond:8345, T:161150, TR:-0.0165133042694288, Period:18.7655538318337, Benefit0.347005895128762,
Cond:6123, T:163457, TR:-0.0165592737687422, Period:18.5858727371725, Benefit0.342885284814967,
Cond:527, T:163431, TR:-0.0166463051971972, Period:18.3033879741298, Benefit0.344855015266382,
Cond:2751, T:162762, TR:-0.0166509998073563, Period:18.5318010346395, Benefit0.346444501787887,
Cond:991, T:154158, TR:-0.0166656454005968, Period:18.1812750554626, Benefit0.367051985625138,
Cond:5469, T:156852, TR:-0.0167295406413614, Period:18.3516882156428, Benefit0.36192716701094,
Cond:4317, T:165809, TR:-0.0168554355428986, Period:18.3881152410303, Benefit0.344197238991852,
Cond:6109, T:159596, TR:-0.0168763732639092, Period:18.7708338554851, Benefit0.358724529436828,
Cond:8341, T:174515, TR:-0.0168846904502355, Period:17.7151763458728, Benefit0.326785663123514,
Cond:2557, T:160379, TR:-0.016903974879448, Period:18.0235442296061, Benefit0.357509399609675,
Cond:8343, T:168072, TR:-0.0169449701133095, Period:18.2455495263935, Benefit0.341246608596316,
Cond:6107, T:168011, TR:-0.0170021135478655, Period:18.2476921153972, Benefit0.342596615697782, , T2:8371,1647,8369,751,6131,8349,8355,6111,6133,79,8359,1661,4333,8153,8357,8361,4319,8347,8345,6123,527,2751,991,5469,4317,6109,8341,2557,8343,6107,  #End#
LowScoreRank3 , T0:3038 , T1:
Cond:1, T:245100, TR:-0.0389218761576917, Period:13.6915585475316, Benefit0.543382292941657,
Cond:3, T:245054, TR:-0.0389321927750388, Period:13.6938307475087, Benefit0.543639361120406,
Cond:5, T:244981, TR:-0.0389393604761382, Period:13.6972050893743, Benefit0.543915650601475,
Cond:6, T:127, TR:-0.000893387839805431, Period:2.11811023622047, Benefit-0.551181102362205,
Cond:7, T:244973, TR:-0.0389404113526461, Period:13.6975585064476, Benefit0.543949741400073,
Cond:8, T:1417, TR:-0.00127787282431266, Period:1.83839096683133, Benefit0.968242766407904,
Cond:9, T:243683, TR:-0.0385154193682336, Period:13.7604839073715, Benefit0.540911758308951,
Cond:10, T:5101, TR:-0.00237119089014437, Period:2.10252891589884, Benefit1.07175063712997,
Cond:11, T:239999, TR:-0.0373100062767309, Period:13.9378747411448, Benefit0.532152217300905,
Cond:12, T:18363, TR:-0.00645451828665313, Period:2.8376082339487, Benefit1.12688558514404,
Cond:13, T:226737, TR:-0.0328541838884922, Period:14.5705994169456, Benefit0.496125466950696,
Cond:14, T:33672, TR:-0.0100277640662429, Period:3.17878355904015, Benefit1.00650392017106,
Cond:15, T:211428, TR:-0.0289876318646926, Period:15.3658219346539, Benefit0.469625593582685,
Cond:17, T:245072, TR:-0.0389030461534429, Period:13.6929800221976, Benefit0.543179147352615,
Cond:18, T:501, TR:-0.00109217782193355, Period:2.19161676646707, Benefit1.34930139720559,
Cond:19, T:244599, TR:-0.0387237442499716, Period:13.715113307904, Benefit0.541731568812628,
Cond:20, T:1019, TR:-0.00134566728975747, Period:2.28164867517174, Benefit1.59666339548577,
Cond:21, T:244081, TR:-0.0384471527276508, Period:13.7391931367046, Benefit0.538985009074856,
Cond:22, T:1304, TR:-0.00128076488857843, Period:2.38650306748466, Benefit1.06058282208589,
Cond:23, T:243796, TR:-0.0385135260081627, Period:13.7520262842705, Benefit0.540615924789578,
Cond:24, T:7065, TR:-0.00388397845894988, Period:2.10714791224345, Benefit1.57565463552725,
Cond:25, T:238035, TR:-0.0356783543095798, Period:14.0353897536077, Benefit0.512743924212826,
Cond:26, T:15491, TR:-0.00554968277809646, Period:2.48931637725131, Benefit1.1185849848299,
Cond:27, T:229609, TR:-0.0338354847099958, Period:14.447338736722, Benefit0.504575169091804,
Cond:28, T:40747, TR:-0.0125890431219725, Period:3.43615480894299, Benefit1.06346479495423,
Cond:29, T:204353, TR:-0.0262705503742678, Period:15.7364364604386, Benefit0.439680357029258,
Cond:30, T:58171, TR:-0.0171894387400706, Period:3.62147805607605, Benefit1.03350466727407,
Cond:31, T:186929, TR:-0.0214373256269624, Period:16.825297305394, Benefit0.390859631196872,
Cond:33, T:244901, TR:-0.0388615495598984, Period:13.7008832140334, Benefit0.542990841197055,
Cond:34, T:1411, TR:-0.00147528784179993, Period:2.55209071580439, Benefit1.4975194897236, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:3038 , T1:
Cond:1, T:245100, TR:-0.0389218761576917, Period:13.6915585475316, Benefit0.543382292941657,
Cond:3, T:245054, TR:-0.0389321927750388, Period:13.6938307475087, Benefit0.543639361120406,
Cond:5, T:244981, TR:-0.0389393604761382, Period:13.6972050893743, Benefit0.543915650601475,
Cond:6, T:127, TR:-0.000893387839805431, Period:2.11811023622047, Benefit-0.551181102362205,
Cond:7, T:244973, TR:-0.0389404113526461, Period:13.6975585064476, Benefit0.543949741400073,
Cond:8, T:1417, TR:-0.00127787282431266, Period:1.83839096683133, Benefit0.968242766407904,
Cond:9, T:243683, TR:-0.0385154193682336, Period:13.7604839073715, Benefit0.540911758308951,
Cond:10, T:5101, TR:-0.00237119089014437, Period:2.10252891589884, Benefit1.07175063712997,
Cond:11, T:239999, TR:-0.0373100062767309, Period:13.9378747411448, Benefit0.532152217300905,
Cond:12, T:18363, TR:-0.00645451828665313, Period:2.8376082339487, Benefit1.12688558514404,
Cond:13, T:226737, TR:-0.0328541838884922, Period:14.5705994169456, Benefit0.496125466950696,
Cond:14, T:33672, TR:-0.0100277640662429, Period:3.17878355904015, Benefit1.00650392017106,
Cond:15, T:211428, TR:-0.0289876318646926, Period:15.3658219346539, Benefit0.469625593582685,
Cond:17, T:245072, TR:-0.0389030461534429, Period:13.6929800221976, Benefit0.543179147352615,
Cond:18, T:501, TR:-0.00109217782193355, Period:2.19161676646707, Benefit1.34930139720559,
Cond:19, T:244599, TR:-0.0387237442499716, Period:13.715113307904, Benefit0.541731568812628,
Cond:20, T:1019, TR:-0.00134566728975747, Period:2.28164867517174, Benefit1.59666339548577,
Cond:21, T:244081, TR:-0.0384471527276508, Period:13.7391931367046, Benefit0.538985009074856,
Cond:22, T:1304, TR:-0.00128076488857843, Period:2.38650306748466, Benefit1.06058282208589,
Cond:23, T:243796, TR:-0.0385135260081627, Period:13.7520262842705, Benefit0.540615924789578,
Cond:24, T:7065, TR:-0.00388397845894988, Period:2.10714791224345, Benefit1.57565463552725,
Cond:25, T:238035, TR:-0.0356783543095798, Period:14.0353897536077, Benefit0.512743924212826,
Cond:26, T:15491, TR:-0.00554968277809646, Period:2.48931637725131, Benefit1.1185849848299,
Cond:27, T:229609, TR:-0.0338354847099958, Period:14.447338736722, Benefit0.504575169091804,
Cond:28, T:40747, TR:-0.0125890431219725, Period:3.43615480894299, Benefit1.06346479495423,
Cond:29, T:204353, TR:-0.0262705503742678, Period:15.7364364604386, Benefit0.439680357029258,
Cond:30, T:58171, TR:-0.0171894387400706, Period:3.62147805607605, Benefit1.03350466727407,
Cond:31, T:186929, TR:-0.0214373256269624, Period:16.825297305394, Benefit0.390859631196872,
Cond:33, T:244901, TR:-0.0388615495598984, Period:13.7008832140334, Benefit0.542990841197055,
Cond:34, T:1411, TR:-0.00147528784179993, Period:2.55209071580439, Benefit1.4975194897236, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:7068 , T1:
Cond:8376, T:160059, TR:-0.0301709806986539, Period:10.0886360654509, Benefit0.656226766379897,
Cond:4350, T:164691, TR:-0.0288776670994194, Period:10.6742687821435, Benefit0.608727860053069,
Cond:8378, T:178489, TR:-0.0312771361881805, Period:11.034612777258, Benefit0.607516429583896,
Cond:334, T:166249, TR:-0.0290688236319249, Period:11.9646012908348, Benefit0.606878838368953,
Cond:6140, T:162992, TR:-0.0283623225050628, Period:10.5366459703544, Benefit0.604017375085894,
Cond:2126, T:165194, TR:-0.0286332242521085, Period:11.916934029081, Benefit0.601474629829171,
Cond:3916, T:162975, TR:-0.028239969206364, Period:12.1757999693204, Benefit0.60138671575395,
Cond:3470, T:159226, TR:-0.0275541781013778, Period:11.6521799203648, Benefit0.600724756007185,
Cond:1452, T:158706, TR:-0.0270849779577218, Period:12.47673685935, Benefit0.592151525462175,
Cond:6142, T:181226, TR:-0.0309583049400335, Period:11.3389579861609, Benefit0.59158178186353,
Cond:3694, T:173643, TR:-0.0294739720348494, Period:12.095264421831, Benefit0.588155007688188,
Cond:110, T:184599, TR:-0.0313351436542776, Period:12.5021153960747, Benefit0.587495056852962,
Cond:7936, T:196977, TR:-0.0334388698620454, Period:12.6822827030567, Benefit0.586606558126076,
Cond:7500, T:173667, TR:-0.0293878860890663, Period:11.9319214358514, Benefit0.586294460087409,
Cond:1230, T:180547, TR:-0.0305053482705474, Period:12.3804438733405, Benefit0.58495571790171,
Cond:558, T:185632, TR:-0.0313666262003209, Period:12.5169636700569, Benefit0.584656740217204,
Cond:5262, T:171971, TR:-0.0290164449985556, Period:12.0061173104768, Benefit0.584627640706863,
Cond:2572, T:169611, TR:-0.0285798037896675, Period:12.6479119868405, Benefit0.583936183384332,
Cond:8380, T:195548, TR:-0.0328464045932137, Period:11.817165095015, Benefit0.580353672755538,
Cond:3689, T:168000, TR:-0.0281273104391943, Period:15.422125, Benefit0.58014880952381,
Cond:2350, T:184877, TR:-0.0309157360757354, Period:12.4609334855066, Benefit0.578465682588965,
Cond:4606, T:208323, TR:-0.034822011065925, Period:14.0160375954647, Benefit0.576383788635916,
Cond:8382, T:201769, TR:-0.0336401689085949, Period:12.0932105526617, Benefit0.575425362667209,
Cond:770, T:200463, TR:-0.0332932957141004, Period:14.2068960356774, Benefit0.573242942587909,
Cond:780, T:173118, TR:-0.0286622293883914, Period:12.8746230894534, Benefit0.573233285966797,
Cond:6620, T:192984, TR:-0.0319339094764539, Period:14.7827177382581, Benefit0.571658790366041,
Cond:1454, T:192951, TR:-0.0318926246638414, Period:12.6970422542511, Benefit0.570999891164078,
Cond:4604, T:187128, TR:-0.0308340577001019, Period:14.5369212517635, Benefit0.569578042836989,
Cond:5484, T:179311, TR:-0.0295201348124428, Period:12.3102096357725, Benefit0.569552342020289,
Cond:5257, T:174104, TR:-0.02864778029827, Period:15.921477967192, Benefit0.569527408905022, , T2:8376,4350,8378,334,6140,2126,3916,3470,1452,6142,3694,110,7936,7500,1230,558,5262,2572,8380,3689,2350,4606,8382,770,780,6620,1454,4604,5484,5257,  #End#
LowScoreRank2 , T0:7068 , T1:
Cond:95, T:158129, TR:-0.0178161760808417, Period:18.9577496853835, Benefit0.383585553567024,
Cond:543, T:158225, TR:-0.0180237881835402, Period:18.9284499920999, Benefit0.388061305103492,
Cond:4335, T:158915, TR:-0.0181331985246822, Period:19.417650945474, Benefit0.388773872825095,
Cond:2781, T:160104, TR:-0.0184955460531404, Period:18.8850684555039, Benefit0.393887722980063,
Cond:8155, T:159581, TR:-0.0185760195677418, Period:19.3161090606025, Benefit0.397052280659978,
Cond:8373, T:162973, TR:-0.0187469618646109, Period:19.4530136893841, Benefit0.392169255029974,
Cond:319, T:168954, TR:-0.0189366268049701, Period:18.6025190288481, Benefit0.381648259289511,
Cond:6386, T:165381, TR:-0.0189740524200412, Period:19.1249478476971, Benefit0.391115061585067,
Cond:8371, T:171446, TR:-0.0190083986426451, Period:19.2347503003861, Benefit0.377325805209804,
Cond:2335, T:160518, TR:-0.0190218297158221, Period:18.8012123250975, Benefit0.404608828916383,
Cond:1215, T:162454, TR:-0.0190830212045767, Period:18.7378334790156, Benefit0.400913489356987,
Cond:8351, T:166696, TR:-0.0192482717002165, Period:19.3088616403513, Benefit0.393782694245813,
Cond:2767, T:171191, TR:-0.01931901460141, Period:19.0475550700679, Benefit0.384418573406312,
Cond:4125, T:166876, TR:-0.0193841310453427, Period:18.8242827009276, Benefit0.396258299575733,
Cond:4345, T:164021, TR:-0.0194770496891418, Period:18.7800037800038, Benefit0.405527341011212,
Cond:1647, T:176757, TR:-0.0194812585102438, Period:18.8471517393936, Benefit0.374989392216433,
Cond:5693, T:162928, TR:-0.019558262104401, Period:18.9966733771973, Benefit0.410168908965924,
Cond:6131, T:178128, TR:-0.0195625356907009, Period:18.9705548818827, Benefit0.37358528698464,
Cond:8143, T:163759, TR:-0.0195750403792458, Period:19.1451462209711, Benefit0.408356181950305,
Cond:8363, T:166664, TR:-0.019609839087994, Period:19.3772920366726, Benefit0.401646426342821,
Cond:6125, T:166056, TR:-0.0196123683852398, Period:19.2942441104206, Benefit0.403243484125837,
Cond:8369, T:182532, TR:-0.0196180042716931, Period:18.7451077071418, Benefit0.365185282580589,
Cond:751, T:181393, TR:-0.0196596036516652, Period:18.667870314731, Benefit0.368421052631579,
Cond:6133, T:172638, TR:-0.0196738070708672, Period:19.1273300200419, Benefit0.388396529153489,
Cond:5903, T:168378, TR:-0.0197502573623145, Period:19.0419947974201, Benefit0.400349214267897,
Cond:1661, T:169891, TR:-0.019831117207137, Period:18.55213636979, Benefit0.398314213230836,
Cond:6111, T:175732, TR:-0.0198676715313172, Period:19.0484487742699, Benefit0.385160357817586,
Cond:4596, T:162527, TR:-0.0199942310977271, Period:18.8540611713746, Benefit0.420865456201124,
Cond:8349, T:175086, TR:-0.0200464986960165, Period:19.0888477662406, Benefit0.390311047142547,
Cond:8355, T:188601, TR:-0.0200506846850434, Period:18.5030143000302, Benefit0.360984300189289, , T2:95,543,4335,2781,8155,8373,319,6386,8371,2335,1215,8351,2767,4125,4345,1647,5693,6131,8143,8363,6125,8369,751,6133,5903,1661,6111,4596,8349,8355,  #End#
LowScoreRank3 , T0:7068 , T1:
Cond:1, T:263313, TR:-0.041956005511556, Period:14.3680258855431, Benefit0.543338156490564,
Cond:3, T:263264, TR:-0.0419820519036711, Period:14.3704228455087, Benefit0.543792542846724,
Cond:5, T:263201, TR:-0.041987022320742, Period:14.3732584602642, Benefit0.543998693014084,
Cond:6, T:120, TR:-0.000889391251545369, Period:2.13333333333333, Benefit-0.708333333333333,
Cond:7, T:263193, TR:-0.0419789070894835, Period:14.3736041612049, Benefit0.543908842560402,
Cond:8, T:1122, TR:-0.00115096634524706, Period:1.88413547237077, Benefit0.798573975044563,
Cond:9, T:262191, TR:-0.0416859920582816, Period:14.4214484860274, Benefit0.542245919959114,
Cond:10, T:3797, TR:-0.00190703447238922, Period:2.06926520937582, Benefit0.982091124572031,
Cond:11, T:259516, TR:-0.0408435890998664, Period:14.5479700673562, Benefit0.536918725627707,
Cond:12, T:15894, TR:-0.00574124149824338, Period:2.8766830250409, Benefit1.13514533786334,
Cond:13, T:247419, TR:-0.0366219998739831, Period:15.1062206216984, Benefit0.50532093331555,
Cond:14, T:31578, TR:-0.00935578148033953, Period:3.21552979922731, Benefit0.994679840395212,
Cond:15, T:231735, TR:-0.0326646391346837, Period:15.8877510950008, Benefit0.481834854467387,
Cond:17, T:263283, TR:-0.0419358833269418, Period:14.3695415199614, Benefit0.543137992198509,
Cond:18, T:480, TR:-0.00109803149472913, Period:2.13333333333333, Benefit1.45416666666667,
Cond:19, T:262833, TR:-0.0417503560318243, Period:14.3903695502467, Benefit0.541674751648385,
Cond:20, T:951, TR:-0.00129023438924755, Period:2.25236593059937, Benefit1.49211356466877,
Cond:21, T:262362, TR:-0.0415382846422816, Period:14.4119422782263, Benefit0.539899070749575,
Cond:22, T:1214, TR:-0.00127034435400855, Period:2.37561779242175, Benefit1.10708401976936,
Cond:23, T:262099, TR:-0.0415566297148953, Period:14.4235727721205, Benefit0.540726977210901,
Cond:24, T:5984, TR:-0.00341338771512299, Period:2.16794786096257, Benefit1.56617647058824,
Cond:25, T:257329, TR:-0.0392046379893165, Period:14.6517298866432, Benefit0.51955279039673,
Cond:26, T:12548, TR:-0.00474074122433623, Period:2.50103602167676, Benefit1.14097864201466,
Cond:27, T:250765, TR:-0.037720003399426, Period:14.9618367794549, Benefit0.513432895340259,
Cond:28, T:36954, TR:-0.0115990437781003, Period:3.50178600422146, Benefit1.07430859988093,
Cond:29, T:226359, TR:-0.0302654495861871, Period:16.1419824261461, Benefit0.456655136310021,
Cond:30, T:54824, TR:-0.016271742482153, Period:3.66890412957829, Benefit1.03571428571429,
Cond:31, T:208489, TR:-0.0252962248817603, Period:17.1814532181554, Benefit0.413863561147111,
Cond:33, T:263118, TR:-0.0419089859440833, Period:14.3771007684765, Benefit0.54314794122789,
Cond:34, T:1405, TR:-0.00145850106394571, Period:2.56797153024911, Benefit1.45907473309609, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:7068 , T1:
Cond:1, T:263313, TR:-0.041956005511556, Period:14.3680258855431, Benefit0.543338156490564,
Cond:3, T:263264, TR:-0.0419820519036711, Period:14.3704228455087, Benefit0.543792542846724,
Cond:5, T:263201, TR:-0.041987022320742, Period:14.3732584602642, Benefit0.543998693014084,
Cond:6, T:120, TR:-0.000889391251545369, Period:2.13333333333333, Benefit-0.708333333333333,
Cond:7, T:263193, TR:-0.0419789070894835, Period:14.3736041612049, Benefit0.543908842560402,
Cond:8, T:1122, TR:-0.00115096634524706, Period:1.88413547237077, Benefit0.798573975044563,
Cond:9, T:262191, TR:-0.0416859920582816, Period:14.4214484860274, Benefit0.542245919959114,
Cond:10, T:3797, TR:-0.00190703447238922, Period:2.06926520937582, Benefit0.982091124572031,
Cond:11, T:259516, TR:-0.0408435890998664, Period:14.5479700673562, Benefit0.536918725627707,
Cond:12, T:15894, TR:-0.00574124149824338, Period:2.8766830250409, Benefit1.13514533786334,
Cond:13, T:247419, TR:-0.0366219998739831, Period:15.1062206216984, Benefit0.50532093331555,
Cond:14, T:31578, TR:-0.00935578148033953, Period:3.21552979922731, Benefit0.994679840395212,
Cond:15, T:231735, TR:-0.0326646391346837, Period:15.8877510950008, Benefit0.481834854467387,
Cond:17, T:263283, TR:-0.0419358833269418, Period:14.3695415199614, Benefit0.543137992198509,
Cond:18, T:480, TR:-0.00109803149472913, Period:2.13333333333333, Benefit1.45416666666667,
Cond:19, T:262833, TR:-0.0417503560318243, Period:14.3903695502467, Benefit0.541674751648385,
Cond:20, T:951, TR:-0.00129023438924755, Period:2.25236593059937, Benefit1.49211356466877,
Cond:21, T:262362, TR:-0.0415382846422816, Period:14.4119422782263, Benefit0.539899070749575,
Cond:22, T:1214, TR:-0.00127034435400855, Period:2.37561779242175, Benefit1.10708401976936,
Cond:23, T:262099, TR:-0.0415566297148953, Period:14.4235727721205, Benefit0.540726977210901,
Cond:24, T:5984, TR:-0.00341338771512299, Period:2.16794786096257, Benefit1.56617647058824,
Cond:25, T:257329, TR:-0.0392046379893165, Period:14.6517298866432, Benefit0.51955279039673,
Cond:26, T:12548, TR:-0.00474074122433623, Period:2.50103602167676, Benefit1.14097864201466,
Cond:27, T:250765, TR:-0.037720003399426, Period:14.9618367794549, Benefit0.513432895340259,
Cond:28, T:36954, TR:-0.0115990437781003, Period:3.50178600422146, Benefit1.07430859988093,
Cond:29, T:226359, TR:-0.0302654495861871, Period:16.1419824261461, Benefit0.456655136310021,
Cond:30, T:54824, TR:-0.016271742482153, Period:3.66890412957829, Benefit1.03571428571429,
Cond:31, T:208489, TR:-0.0252962248817603, Period:17.1814532181554, Benefit0.413863561147111,
Cond:33, T:263118, TR:-0.0419089859440833, Period:14.3771007684765, Benefit0.54314794122789,
Cond:34, T:1405, TR:-0.00145850106394571, Period:2.56797153024911, Benefit1.45907473309609, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank1 , T0:4830 , T1:
Cond:8376, T:161402, TR:-0.0294603944339254, Period:9.79932714588419, Benefit0.634694737363849,
Cond:3916, T:158827, TR:-0.0283450455156603, Period:11.9793108224672, Benefit0.620209410238813,
Cond:1006, T:153374, TR:-0.0270529311111555, Period:11.2449437323145, Benefit0.612926571648389,
Cond:1452, T:153560, TR:-0.0270121216551903, Period:12.1850872623079, Benefit0.611194321437874,
Cond:2572, T:163499, TR:-0.0285063837542431, Period:12.383402956593, Benefit0.605220827038698,
Cond:334, T:167351, TR:-0.0288695318741141, Period:11.6826430675646, Benefit0.598418892029328,
Cond:5918, T:153899, TR:-0.0264553734939893, Period:9.77469639178942, Benefit0.596761512420484,
Cond:8158, T:160869, TR:-0.0275094569758574, Period:10.1414691457024, Benefit0.593308841355389,
Cond:4604, T:179785, TR:-0.030686813052834, Period:14.2845176182663, Benefit0.591172789721056,
Cond:3036, T:186405, TR:-0.0318313187129202, Period:14.3153402537486, Benefit0.590998095544647,
Cond:780, T:165988, TR:-0.0282664639764072, Period:12.5506422150999, Benefit0.590530640769212,
Cond:8378, T:182314, TR:-0.0309159496385237, Period:10.8173919720921, Benefit0.58703665105258,
Cond:2126, T:169178, TR:-0.0285582250512312, Period:11.6529099528307, Benefit0.58504651905094,
Cond:3260, T:207931, TR:-0.0352441618323322, Period:14.2753028648927, Benefit0.584737244566707,
Cond:5038, T:157358, TR:-0.0265286207729307, Period:11.0233226146748, Benefit0.584736715006546,
Cond:4828, T:204441, TR:-0.034531292846953, Period:14.2903086954182, Benefit0.582940799546079,
Cond:3484, T:220213, TR:-0.0372726008974522, Period:14.2129075031901, Benefit0.58271764155613,
Cond:4350, T:169932, TR:-0.0285600473623929, Period:10.4879186968905, Benefit0.582362356707389,
Cond:1676, T:181868, TR:-0.0305927526903027, Period:12.8847240856005, Benefit0.582202476521433,
Cond:110, T:183875, TR:-0.0308667366278065, Period:12.1851148878314, Benefit0.580834806254249,
Cond:1916, T:196714, TR:-0.0329743632494438, Period:14.3631363299003, Benefit0.579033520745854,
Cond:5052, T:223223, TR:-0.0375478792942588, Period:14.1967718380274, Benefit0.578712767053574,
Cond:3470, T:166731, TR:-0.0278314220071318, Period:11.4170910028729, Benefit0.578410733456886,
Cond:6140, T:167803, TR:-0.0280050548125036, Period:10.3490998373092, Benefit0.578249494943475,
Cond:7722, T:168324, TR:-0.0280899803299093, Period:12.1659596967753, Benefit0.578182552695991,
Cond:7720, T:154214, TR:-0.0256583926644804, Period:11.9800666606145, Benefit0.576880179490837,
Cond:6620, T:185990, TR:-0.0310074757101356, Period:14.7098607452014, Benefit0.576579385988494,
Cond:2796, T:198521, TR:-0.0331047167633243, Period:13.1249540350895, Benefit0.575798026405267,
Cond:5484, T:177265, TR:-0.0294836865145861, Period:12.139948664429, Benefit0.57573124982371,
Cond:7500, T:177936, TR:-0.0295753242376885, Period:11.8143658394029, Benefit0.575291115906843, , T2:8376,3916,1006,1452,2572,334,5918,8158,4604,3036,780,8378,2126,3260,5038,4828,3484,4350,1676,110,1916,5052,3470,6140,7722,7720,6620,2796,5484,7500,  #End#
LowScoreRank2 , T0:4830 , T1:
Cond:319, T:155277, TR:-0.0158312905740596, Period:18.3831024556116, Benefit0.344944840510829,
Cond:8371, T:157097, TR:-0.0162715089828781, Period:19.194815941743, Benefit0.350846928967453,
Cond:4125, T:154293, TR:-0.0163204938830909, Period:18.5135813031051, Benefit0.358661766898045,
Cond:2767, T:156085, TR:-0.0165084026685554, Period:18.9719575872121, Benefit0.358689175769613,
Cond:1647, T:162306, TR:-0.016536153970684, Period:18.710645324264, Benefit0.344922553694873,
Cond:751, T:167661, TR:-0.0165884370319676, Period:18.4652185063909, Benefit0.334496394510351,
Cond:8369, T:168481, TR:-0.0167213284701533, Period:18.6094099631412, Benefit0.335616479009503,
Cond:5903, T:151732, TR:-0.0167362971529943, Period:19.0795086072813, Benefit0.374864893364617,
Cond:6131, T:164142, TR:-0.0167807719046526, Period:18.8548330104422, Benefit0.346230702684261,
Cond:6111, T:160001, TR:-0.016910820800109, Period:19.0174373910163, Benefit0.358547759076506,
Cond:6133, T:159130, TR:-0.0169111741820079, Period:19.0160372022874, Benefit0.360610821341042,
Cond:1661, T:158042, TR:-0.0169385872815168, Period:18.197941053644, Benefit0.363833664468939,
Cond:8349, T:157348, TR:-0.0169428247962912, Period:19.1754582199964, Benefit0.365609985509825,
Cond:4345, T:152285, TR:-0.0169509351804432, Period:18.5386479298683, Benefit0.378520537150737,
Cond:79, T:172403, TR:-0.0169609893562649, Period:18.2319797219306, Benefit0.332581219584346,
Cond:8355, T:174561, TR:-0.0169770777881896, Period:18.3183471680387, Benefit0.328590005785943,
Cond:8359, T:170122, TR:-0.0171463047586954, Period:18.6850671870775, Benefit0.341172805398479,
Cond:2111, T:157001, TR:-0.0171507583016133, Period:18.3169406564289, Benefit0.371220565474105,
Cond:4333, T:164220, TR:-0.0171952657339892, Period:18.7342893679211, Benefit0.355121178906345,
Cond:8357, T:171226, TR:-0.0172417182522896, Period:18.5928013269013, Benefit0.340859448915468,
Cond:8153, T:168847, TR:-0.0173131011770926, Period:18.5954503189278, Benefit0.347421037981131,
Cond:8345, T:170116, TR:-0.0173183930996968, Period:18.6557936937149, Benefit0.344811775494369,
Cond:8361, T:160678, TR:-0.0173210498758002, Period:19.1125356302667, Benefit0.366142222332864,
Cond:4319, T:165925, TR:-0.0173405216807873, Period:18.7464878710261, Benefit0.354437245743559,
Cond:6123, T:172515, TR:-0.0173553928479211, Period:18.4817668028867, Benefit0.340544300495609,
Cond:527, T:173215, TR:-0.0173942430870954, Period:18.1618335594492, Benefit0.339901278757613,
Cond:8347, T:165830, TR:-0.0174111120116265, Period:18.8686365555087, Benefit0.356178013628415,
Cond:5469, T:166065, TR:-0.0174612932860123, Period:18.2490651251016, Benefit0.356733809050673,
Cond:991, T:164187, TR:-0.0174972772799948, Period:18.016846644375, Benefit0.361800873394361,
Cond:2751, T:172312, TR:-0.0175168624659253, Period:18.3944588885278, Benefit0.344323088351363, , T2:319,8371,4125,2767,1647,751,8369,5903,6131,6111,6133,1661,8349,4345,79,8355,8359,2111,4333,8357,8153,8345,8361,4319,6123,527,8347,5469,991,2751,  #End#
LowScoreRank3 , T0:4830 , T1:
Cond:1, T:252176, TR:-0.0392097707295209, Period:13.8879909269716, Benefit0.531041812067762,
Cond:3, T:252135, TR:-0.0392258701894597, Period:13.8899914728221, Benefit0.531358200963769,
Cond:5, T:252065, TR:-0.0392456411652208, Period:13.8931624779323, Benefit0.531791403011128,
Cond:6, T:123, TR:-0.000877673512815589, Period:2.10569105691057, Benefit-1.04878048780488,
Cond:7, T:252053, TR:-0.0392452212343552, Period:13.8937406021749, Benefit0.531812753666888,
Cond:8, T:1201, TR:-0.00120027547117033, Period:1.86677768526228, Benefit0.900083263946711,
Cond:9, T:250975, TR:-0.0388878187922011, Period:13.9455164857057, Benefit0.529275824285287,
Cond:10, T:4277, TR:-0.00215550488787288, Period:2.05985503857844, Benefit1.08954874912322,
Cond:11, T:247899, TR:-0.0378331758948636, Period:14.0920616864126, Benefit0.52140589514278,
Cond:12, T:17311, TR:-0.00620628509983077, Period:2.8658078678297, Benefit1.1421639420022,
Cond:13, T:234865, TR:-0.0333999302935977, Period:14.7003938432717, Benefit0.485998339471611,
Cond:14, T:32642, TR:-0.00976573702100131, Period:3.2009680779364, Benefit1.00870044727651,
Cond:15, T:219534, TR:-0.0295343875325569, Period:15.4770195049514, Benefit0.460019860249437,
Cond:17, T:252151, TR:-0.0391906484595609, Period:13.8892409706882, Benefit0.530832715317409,
Cond:18, T:491, TR:-0.00108231979639862, Period:2.19551934826884, Benefit1.30142566191446,
Cond:19, T:251685, TR:-0.0390218705783653, Period:13.9108011999126, Benefit0.529538907761686,
Cond:20, T:981, TR:-0.00131075667391689, Period:2.29663608562691, Benefit1.5249745158002,
Cond:21, T:251195, TR:-0.0387718140760701, Period:13.9332590218754, Benefit0.527160174366528,
Cond:22, T:1250, TR:-0.00126129865335904, Period:2.4032, Benefit1.048,
Cond:23, T:250926, TR:-0.0388219203506889, Period:13.9452029682058, Benefit0.52846655986227,
Cond:24, T:6175, TR:-0.00354053860967143, Period:2.1497975708502, Benefit1.59481781376518,
Cond:25, T:246001, TR:-0.0363341791001985, Period:14.1826374689534, Benefit0.504339413254418,
Cond:26, T:13726, TR:-0.00510629217461964, Period:2.49453591723736, Benefit1.14228471513915,
Cond:27, T:238450, TR:-0.0345979440638192, Period:14.5438372824492, Benefit0.495856573705179,
Cond:28, T:38917, TR:-0.0121934429571398, Period:3.48418428964206, Benefit1.07629056710435,
Cond:29, T:213259, TR:-0.026952148273096, Period:15.7865506262338, Benefit0.431540990063725,
Cond:30, T:56336, TR:-0.0167799424185154, Period:3.65604586765124, Benefit1.04084422039193,
Cond:31, T:195840, TR:-0.0221136383266752, Period:16.8313470179739, Benefit0.384390318627451,
Cond:33, T:251979, TR:-0.0391476154040059, Period:13.8970985677378, Benefit0.530623583711341,
Cond:34, T:1394, TR:-0.00147021920707437, Period:2.54662840746055, Benefit1.50215208034433, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
LowScoreRank4 , T0:4830 , T1:
Cond:1, T:252176, TR:-0.0392097707295209, Period:13.8879909269716, Benefit0.531041812067762,
Cond:3, T:252135, TR:-0.0392258701894597, Period:13.8899914728221, Benefit0.531358200963769,
Cond:5, T:252065, TR:-0.0392456411652208, Period:13.8931624779323, Benefit0.531791403011128,
Cond:6, T:123, TR:-0.000877673512815589, Period:2.10569105691057, Benefit-1.04878048780488,
Cond:7, T:252053, TR:-0.0392452212343552, Period:13.8937406021749, Benefit0.531812753666888,
Cond:8, T:1201, TR:-0.00120027547117033, Period:1.86677768526228, Benefit0.900083263946711,
Cond:9, T:250975, TR:-0.0388878187922011, Period:13.9455164857057, Benefit0.529275824285287,
Cond:10, T:4277, TR:-0.00215550488787288, Period:2.05985503857844, Benefit1.08954874912322,
Cond:11, T:247899, TR:-0.0378331758948636, Period:14.0920616864126, Benefit0.52140589514278,
Cond:12, T:17311, TR:-0.00620628509983077, Period:2.8658078678297, Benefit1.1421639420022,
Cond:13, T:234865, TR:-0.0333999302935977, Period:14.7003938432717, Benefit0.485998339471611,
Cond:14, T:32642, TR:-0.00976573702100131, Period:3.2009680779364, Benefit1.00870044727651,
Cond:15, T:219534, TR:-0.0295343875325569, Period:15.4770195049514, Benefit0.460019860249437,
Cond:17, T:252151, TR:-0.0391906484595609, Period:13.8892409706882, Benefit0.530832715317409,
Cond:18, T:491, TR:-0.00108231979639862, Period:2.19551934826884, Benefit1.30142566191446,
Cond:19, T:251685, TR:-0.0390218705783653, Period:13.9108011999126, Benefit0.529538907761686,
Cond:20, T:981, TR:-0.00131075667391689, Period:2.29663608562691, Benefit1.5249745158002,
Cond:21, T:251195, TR:-0.0387718140760701, Period:13.9332590218754, Benefit0.527160174366528,
Cond:22, T:1250, TR:-0.00126129865335904, Period:2.4032, Benefit1.048,
Cond:23, T:250926, TR:-0.0388219203506889, Period:13.9452029682058, Benefit0.52846655986227,
Cond:24, T:6175, TR:-0.00354053860967143, Period:2.1497975708502, Benefit1.59481781376518,
Cond:25, T:246001, TR:-0.0363341791001985, Period:14.1826374689534, Benefit0.504339413254418,
Cond:26, T:13726, TR:-0.00510629217461964, Period:2.49453591723736, Benefit1.14228471513915,
Cond:27, T:238450, TR:-0.0345979440638192, Period:14.5438372824492, Benefit0.495856573705179,
Cond:28, T:38917, TR:-0.0121934429571398, Period:3.48418428964206, Benefit1.07629056710435,
Cond:29, T:213259, TR:-0.026952148273096, Period:15.7865506262338, Benefit0.431540990063725,
Cond:30, T:56336, TR:-0.0167799424185154, Period:3.65604586765124, Benefit1.04084422039193,
Cond:31, T:195840, TR:-0.0221136383266752, Period:16.8313470179739, Benefit0.384390318627451,
Cond:33, T:251979, TR:-0.0391476154040059, Period:13.8970985677378, Benefit0.530623583711341,
Cond:34, T:1394, TR:-0.00147021920707437, Period:2.54662840746055, Benefit1.50215208034433, , T2:1,3,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,33,34,  #End#
End , T0:00:45:36.2631459  #End#

 
 */