using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharp_sample
{
	class Condtions
	{
		private const bool IsAllCheck = true;


		public static void Aaa()
		{
			List<string[]> conditions = CsvControll.GetConditions();
			string ands = "";
			string ors = "";
			foreach (string[] cond in conditions) {
				bool isAnd = cond[0] == "1"; // and条件かor条件か

				int period = Int32.Parse(cond[2]);
				int cnt = Int32.Parse(cond[4]);
				double ratio = Math.Round(Double.Parse(cond[6]), 2, MidpointRounding.AwayFromZero);
				int diffDay = Int32.Parse(cond[3]);

				bool isTrue = (cond[5] == "1");

				int idx = -1;
				for (int diffDayIdx = 0; diffDayIdx < diffDayList.Length; diffDayIdx++) {
					for (int ratioIdx = 0; ratioIdx < ratioList.Length; ratioIdx++) {
						for (int pIdx = 0; pIdx < periodCntList.GetLength(0); pIdx++) {
							if (diffDayList[diffDayIdx] == diffDay && ratioList[ratioIdx] == ratio && periodCntList[pIdx, 0] == period && periodCntList[pIdx, 1] == cnt) {
								idx = GetCondIdx(pIdx, ratioIdx, diffDayIdx, isTrue);
							}
						}
					}
				}
				if (idx == -1) {
					Common.DebugInfo("error", period, cnt, ratio, diffDay);
				}

				if (isAnd) {
					ands += idx + ",";
				} else {
					ors += idx + ",";
				}
			}
			Common.DebugInfo("res", ands, ors);
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


		private const bool IsAndCheck = false; // andチェックかorチェックか
		private const bool SkipMode = true; // 購入時に所持期間分日付をスキップすうかどうか
		private const int AllTrueCondIdx = 1;
		// skip   T0:23732,  T1:0.195, T2:10.2  #End#
		// noskip T0:116276, T1:0.256, T2:12.9  #End#
		private static readonly int[] OldAnd51List = new int[] {
			6335,6767,6559,7439,7215,6991,7855,6783,2580,1684,2806,1254,806,1686,134,2356,5696,1460,358,136,584,1236,1462,1480,3926,2132,360,790,788,808,1238,566,4765,3629,568,120,792,398,5641,8105,
			3197,342,7936,5645,3631,479,8323,5423,4287,6077,3671,7887,6079,8303,5685,7679,8135,8329,6093,8125,6105,8111,8127,8319,7903,4573,7037,5673,6103,5907,2747,6109,5909,3005,7919,6119,8378,
		};
		private static readonly int[] OldOr51List = new int[] {
			708,2322,1604,24,278,38,1202,4582,5026,3456,4907,6600,4558,6588,7266,3656,4308,4320,6112,6824,8352,1870,7046,6824,4094,6590,3014,6826,72,6826,4893,7706,78,3676,
		};

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
			8376, // T0:220872, T1:0.567,  T2:10 #End#  T0:58625, T1:0.701, T2:6 #End#
			8372, // T0:135461, T1:0.7001, T2:5.6597 #End#, T0:47472, T1:0.7487, T2:4.4662  #End#
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
			//AllTrueCondIdx
		/*
2734,
62,
6104,
8340,
8328,
526,
1630,
78,
6106,
1644,
2750,
96,
6114,
*/

		};
		private static readonly int[] KouhoOrs = new int[] {
			AllTrueCondIdx-1
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
								for (int c = 0; c < cond51All.Count; c++) {
									string[] cond51 = cond51All[c];

									if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
									if (IsAndCheck && (cond51[pIdx + 1] == "1") != isT) continue;
									if (!benefits.ContainsKey(cond51[0])) continue;
									bool isOrCheck = isOrOkForce || (!IsAndCheck && (cond51[pIdx + 1] == "1") == isT) || beforeOr[symbol].Contains(cond51[0]);

									if (KouhoAnds.Length > 0) {
										if (!isOrCheck) continue;
										for (int i = 0; i < KouhoAnds.Length; i++) {
											if (beforeNotAndKouho[i][symbol].Contains(cond51[0])) continue;
											if (SkipMode && nowHaves[i] > c) continue;

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
				double baseBenefit = 0;
				for (int j = 0; j < condNum(); j++) {
					if (trueAll[i, j] == 0) continue;
					benefitRes[j] = Common.Round((double)benefitAll[i, j] / (double)trueAll[i, j], 4);
					havePeriodRes[j] = Common.Round((double)havePeriodAll[i, j] / (double)trueAll[i, j], 4);

					if(tMin > trueAll[i, j]){
						tMin = trueAll[i, j];
						if (!IsAndCheck) baseBenefit = benefitRes[j];
					}
					if (tMax < trueAll[i, j]) {
						tMax = trueAll[i, j];
						if (IsAndCheck) baseBenefit = benefitRes[j];
					}
					maxBenefit = Math.Max(maxBenefit, benefitRes[j]); minBenefit = Math.Min(minBenefit, benefitRes[j]);
				}
				double needNum = tMax * 0.6;
				double needBenefit = 0;
				if(IsAndCheck) {
					needNum = tMax * 0.7;
					needBenefit = baseBenefit * 1.05;
				} else{
					needNum = tMin + 1000;
					needBenefit = baseBenefit * 0.8;
				}

				int max = maxNum;
				string result = "";
				string result2 = "";
				// OrderByDescending:高い順、OrderBy：低い順
				maxBenefit = 0;
				foreach (KeyValuePair<int, double> b in benefitRes.OrderByDescending(
					c => trueAll[i, c.Key] >= needNum && c.Value >= needBenefit ? c.Value : 0
				)) {
					if (max > 0) {
						double tr = (AllCond51Num * AllCond51Ratio - trueAll[i, b.Key] * b.Value) / (AllCond51Num - trueAll[i, b.Key]);
						result += "\nCond:" + b.Key + ", T:" + trueAll[i, b.Key] + ", TR:" + tr + ", Period:" +havePeriodRes[b.Key] + ", Benefit" + b.Value + ",";
						result2 += b.Key + ",";
						maxBenefit = Math.Max(maxBenefit, b.Value);
					}
					max--;
				}
				Common.DebugInfo("LowScoreRank1", kouhoList[i], result, result2);


				result = "";
				result2 = "";
				max = 30;
				foreach (KeyValuePair<int, double> b in benefitRes.OrderByDescending(c =>
					//c.Value >= maxBenefit * 0.5 & trueAll[i, c.Key] >= needNum ?
					trueAll[i, c.Key] >= needNum && c.Value >= needBenefit ? (AllCond51Num * AllCond51Ratio - trueAll[i, c.Key] * c.Value) / (AllCond51Num - trueAll[i, c.Key]) : -999

				)) {
					if (max > 0) {
						double tr = (AllCond51Num * AllCond51Ratio - trueAll[i, b.Key] * b.Value) / (AllCond51Num - trueAll[i, b.Key]);
						result += "\nCond:" + b.Key + ", T:" + trueAll[i, b.Key] + ", TR:" + tr + ", Period:" + havePeriodRes[b.Key] + ", Benefit" + b.Value + ",";
						result2 += b.Key + ",";
					}
					max--;
				}
				Common.DebugInfo("LowScoreRank2", kouhoList[i], result, result2);


				result = "";
				result2 = "";
				max = 30;
				foreach (KeyValuePair<int, double> b2 in benefitRes.OrderByDescending(
					c => trueAll[i, c.Key] >= needNum && c.Value >= needBenefit ? trueAll[i, c.Key] : 0
				)) {
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
				foreach (KeyValuePair<int, double> b in benefitRes.OrderBy(c =>
					trueAll[i, c.Key] >= needNum && c.Value >= needBenefit ? trueAll[i, c.Key] : 9999999
				)) {
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
			bool isOldCheck = false;

			bool isOrOkForce = ConfirmOrs.Length == 0; // orチェックを強制でOKにしておく
			Dictionary<string, HashSet<string>> beforeNotAnd = new Dictionary<string, HashSet<string>>();
			Dictionary<string, HashSet<string>> beforeOr = new Dictionary<string, HashSet<string>>();
			foreach (string symbol in CsvControll.GetCodeList()) {
				beforeNotAnd[symbol] = new HashSet<string>();
				foreach (int condIdx in (isOldCheck ? OldAnd51List : ConfirmAnds)) {
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(condIdx);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if ((cond51[pIdx + 1] == "1") != isT) beforeNotAnd[symbol].Add(cond51[0]);
					}
				}
				beforeOr[symbol] = new HashSet<string>();
				foreach (int condIdx in (isOldCheck ? OldOr51List : ConfirmOrs)) {
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
				List<string[]> list = CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx);
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
			Common.DebugInfo("DebugCheckCond51", trueAll,Common.Round((double)benefitAll / trueAll, 4), Common.Round((double)havePeriodAll / trueAll, 4));
		}









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
 LowScoreRank1 , T0:0 , T1:
Cond:1618, T:143399, TR:-0.0301919457364792, Period:5.661, Benefit0.7365,
Cond:1602, T:137310, TR:-0.028252418202686, Period:5.6578, Benefit0.7194,
Cond:2722, T:142935, TR:-0.0292070239967264, Period:5.6816, Benefit0.7141,
Cond:722, T:137423, TR:-0.0278578963301076, Period:5.6521, Benefit0.7084,
Cond:1616, T:136599, TR:-0.0276562496374666, Period:5.6595, Benefit0.7075,
Cond:1604, T:140758, TR:-0.0284516086956522, Period:5.6236, Benefit0.7062,
Cond:2738, T:158651, TR:-0.0320715613501342, Period:5.7219, Benefit0.7054,
Cond:1961, T:139444, TR:-0.0281151819423462, Period:5.6499, Benefit0.7044,
Cond:2998, T:138703, TR:-0.0279612450288052, Period:5.6094, Benefit0.7043,
Cond:2706, T:138016, TR:-0.0278068933724463, Period:5.6567, Benefit0.7039,
Cond:1588, T:136803, TR:-0.0275351133824183, Period:5.647, Benefit0.7032,
Cond:2530, T:136754, TR:-0.0275176577394081, Period:5.639, Benefit0.703,
Cond:3444, T:139081, TR:-0.0279647133714346, Period:5.5774, Benefit0.7024,
Cond:1632, T:143231, TR:-0.0287404917778691, Period:5.699, Benefit0.7008,
Cond:1172, T:136902, TR:-0.0274415907953158, Period:5.6618, Benefit0.7002,
Cond:3206, T:136755, TR:-0.0273742305162527, Period:5.6259, Benefit0.6992,
Cond:3890, T:137653, TR:-0.0275469960138039, Period:5.6048, Benefit0.699,
Cond:621, T:136690, TR:-0.0273309549930407, Period:5.6598, Benefit0.6984,
Cond:3491, T:136516, TR:-0.02728473750114, Period:5.6466, Benefit0.6981,
Cond:6584, T:137522, TR:-0.0274826674982114, Period:5.6522, Benefit0.698,
Cond:7339, T:136511, TR:-0.0272761897072581, Period:5.6471, Benefit0.6979,
Cond:4580, T:136962, TR:-0.0273590167819998, Period:5.6775, Benefit0.6977,
Cond:7581, T:136777, TR:-0.0273219432946879, Period:5.6475, Benefit0.6977,
Cond:1279, T:136588, TR:-0.0272765222853397, Period:5.6674, Benefit0.6975,
Cond:1041, T:137346, TR:-0.0274245914162663, Period:5.6297, Benefit0.6974,
Cond:369, T:136492, TR:-0.0272497488200566, Period:5.637, Benefit0.6973,
Cond:7095, T:137457, TR:-0.0274354329364861, Period:5.6456, Benefit0.6971,
Cond:1497, T:136617, TR:-0.0272634525050514, Period:5.6549, Benefit0.697,
Cond:7373, T:136581, TR:-0.0272562465110045, Period:5.6456, Benefit0.697,
Cond:5041, T:136943, TR:-0.027324928206995, Period:5.6344, Benefit0.6969, , T2:1618,1602,2722,722,1616,1604,2738,1961,2998,2706,1588,2530,3444,1632,1172,3206,3890,621,3491,6584,7339,4580,7581,1279,1041,369,7095,1497,7373,5041,  #End#
LowScoreRank2 , T0:0 , T1:
Cond:8306, T:156479, TR:-0.0258341371145621, Period:5.7086, Benefit0.5722,
Cond:4304, T:155084, TR:-0.0258350249352603, Period:5.7159, Benefit0.5776,
Cond:581, T:158622, TR:-0.0260934670068716, Period:5.5114, Benefit0.57,
Cond:2375, T:153974, TR:-0.026217717242743, Period:5.6065, Benefit0.5909,
Cond:1459, T:146022, TR:-0.0262295840552595, Period:5.5579, Benefit0.6248,
Cond:151, T:143076, TR:-0.0263086129203917, Period:5.5692, Benefit0.6402,
Cond:805, T:143345, TR:-0.0263185659617321, Period:5.5815, Benefit0.6392,
Cond:1253, T:157393, TR:-0.0263754587797669, Period:5.5236, Benefit0.5811,
Cond:8320, T:145294, TR:-0.0263840718374819, Period:5.6755, Benefit0.6319,
Cond:807, T:149703, TR:-0.0264001646329743, Period:5.6098, Benefit0.6129,
Cond:7854, T:144860, TR:-0.0264052502757598, Period:5.5696, Benefit0.6344,
Cond:1685, T:144156, TR:-0.0264720394025585, Period:5.6496, Benefit0.6393,
Cond:4274, T:143489, TR:-0.0265318443616141, Period:5.6293, Benefit0.6439,
Cond:3719, T:152564, TR:-0.0265394445116233, Period:5.6214, Benefit0.6042,
Cond:5495, T:151344, TR:-0.0265526968832332, Period:5.674, Benefit0.6096,
Cond:5721, T:163368, TR:-0.0266037010623363, Period:5.8479, Benefit0.5639,
Cond:4665, T:142335, TR:-0.0266053013596209, Period:5.6396, Benefit0.6512,
Cond:2581, T:147521, TR:-0.0266105269447367, Period:5.6249, Benefit0.6275,
Cond:7511, T:153159, TR:-0.0266393893629362, Period:5.6178, Benefit0.6041,
Cond:4290, T:157593, TR:-0.0266677045856426, Period:5.6766, Benefit0.587,
Cond:375, T:150027, TR:-0.02671565565588, Period:5.535, Benefit0.6191,
Cond:787, T:148826, TR:-0.0267399387231494, Period:5.6032, Benefit0.6249,
Cond:1477, T:145477, TR:-0.0267479478584998, Period:5.5728, Benefit0.6401,
Cond:6153, T:143027, TR:-0.0267747620912112, Period:5.8418, Benefit0.6522,
Cond:2599, T:139910, TR:-0.0267776024051059, Period:5.6396, Benefit0.6674,
Cond:1251, T:140830, TR:-0.0267867540049186, Period:5.5894, Benefit0.6631,
Cond:811, T:164961, TR:-0.0267876154047787, Period:5.8204, Benefit0.5622,
Cond:2100, T:151783, TR:-0.0267902827734872, Period:5.5485, Benefit0.6134,
Cond:1479, T:153250, TR:-0.0268042511880712, Period:5.6174, Benefit0.6076,
Cond:80, T:154957, TR:-0.0268080636957007, Period:5.616, Benefit0.6007, , T2:8306,4304,581,2375,1459,151,805,1253,8320,807,7854,1685,4274,3719,5495,5721,4665,2581,7511,4290,375,787,1477,6153,2599,1251,811,2100,1479,80,  #End#
LowScoreRank3 , T0:0 , T1:
Cond:3236, T:231482, TR:-0.0381309311200192, Period:6.6805, Benefit0.5656,
Cond:3458, T:218284, TR:-0.0365445005211217, Period:6.4747, Benefit0.5764,
Cond:504, T:213386, TR:-0.0359980450182126, Period:5.5195, Benefit0.5814,
Cond:56, T:212435, TR:-0.034926777119006, Period:5.5046, Benefit0.5663,
Cond:4114, T:209406, TR:-0.035523513496621, Period:5.188, Benefit0.5851,
Cond:2296, T:206715, TR:-0.0341323193081731, Period:5.5095, Benefit0.5693,
Cond:254, T:205108, TR:-0.0333911206931814, Period:5.4243, Benefit0.5612,
Cond:1176, T:203834, TR:-0.0351409158840817, Period:5.5188, Benefit0.5954,
Cond:1864, T:202989, TR:-0.0341908170850675, Period:5.7151, Benefit0.5814,
Cond:3000, T:202281, TR:-0.0340225847320487, Period:5.7752, Benefit0.5806,
Cond:5670, T:201059, TR:-0.0327193396673502, Period:5.2346, Benefit0.5613,
Cond:740, T:199435, TR:-0.0329808944159264, Period:5.6445, Benefit0.5708,
Cond:1608, T:199370, TR:-0.0345903787332134, Period:5.3443, Benefit0.5997,
Cond:3864, T:197840, TR:-0.032420935807971, Period:5.4746, Benefit0.5656,
Cond:2046, T:197337, TR:-0.0321588781579677, Period:5.4179, Benefit0.5624,
Cond:1850, T:193319, TR:-0.0319060733469285, Period:5.5273, Benefit0.5701,
Cond:2112, T:192895, TR:-0.0316557066067826, Period:6.5993, Benefit0.5668,
Cond:2546, T:191122, TR:-0.0316904716563723, Period:5.4277, Benefit0.573,
Cond:3680, T:191113, TR:-0.0310507881203432, Period:6.4229, Benefit0.5611,
Cond:3614, T:190893, TR:-0.0318337272918325, Period:5.4539, Benefit0.5764,
Cond:3404, T:190294, TR:-0.0319237029180338, Period:5.5242, Benefit0.58,
Cond:4072, T:190232, TR:-0.03117660632196, Period:5.3528, Benefit0.5662,
Cond:8104, T:190104, TR:-0.0317044385973672, Period:5.4182, Benefit0.5765,
Cond:476, T:189836, TR:-0.0308333549408844, Period:5.3333, Benefit0.561,
Cond:1162, T:189270, TR:-0.0307075590955392, Period:5.3651, Benefit0.5604,
Cond:7674, T:188904, TR:-0.0313037663207498, Period:5.619, Benefit0.5728,
Cond:6439, T:188717, TR:-0.0313250074889889, Period:5.6094, Benefit0.5738,
Cond:2740, T:187776, TR:-0.0322813639142051, Period:5.5971, Benefit0.595,
Cond:926, T:187478, TR:-0.0320923267627364, Period:5.4353, Benefit0.5924,
Cond:5196, T:186276, TR:-0.0316053297031617, Period:5.579, Benefit0.5871, , T2:3236,3458,504,56,4114,2296,254,1176,1864,3000,5670,740,1608,3864,2046,1850,2112,2546,3680,3614,3404,4072,8104,476,1162,7674,6439,2740,926,5196,  #End#
LowScoreRank4 , T0:0 , T1:
Cond:6699, T:136470, TR:-0.0271963135338778, Period:5.646, Benefit0.696,
Cond:383, T:136477, TR:-0.0272014843352424, Period:5.6672, Benefit0.6961,
Cond:6471, T:136477, TR:-0.0271901691135524, Period:5.6514, Benefit0.6958,
Cond:3943, T:136480, TR:-0.0272171713268218, Period:5.6574, Benefit0.6965,
Cond:7042, T:136483, TR:-0.0272026837065965, Period:5.6646, Benefit0.6961,
Cond:369, T:136492, TR:-0.0272497488200566, Period:5.637, Benefit0.6973,
Cond:7615, T:136510, TR:-0.027177899431126, Period:5.6466, Benefit0.6953,
Cond:7339, T:136511, TR:-0.0272761897072581, Period:5.6471, Benefit0.6979,
Cond:3491, T:136516, TR:-0.02728473750114, Period:5.6466, Benefit0.6981,
Cond:4893, T:136526, TR:-0.0272188256093921, Period:5.6504, Benefit0.6963,
Cond:3846, T:136532, TR:-0.0270200395074667, Period:5.6319, Benefit0.691,
Cond:3150, T:136537, TR:-0.0271606497692732, Period:5.6458, Benefit0.6947,
Cond:5192, T:136539, TR:-0.027123313582694, Period:5.6379, Benefit0.6937,
Cond:7997, T:136555, TR:-0.0272170764731032, Period:5.6601, Benefit0.6961,
Cond:1723, T:136561, TR:-0.0271767602501157, Period:5.6593, Benefit0.695,
Cond:8304, T:136563, TR:-0.0271054494946969, Period:5.6709, Benefit0.6931,
Cond:1983, T:136565, TR:-0.0271511387552403, Period:5.6529, Benefit0.6943,
Cond:1725, T:136576, TR:-0.0271797540929329, Period:5.6605, Benefit0.695,
Cond:825, T:136581, TR:-0.0272298234482721, Period:5.6551, Benefit0.6963,
Cond:7373, T:136581, TR:-0.0272562465110045, Period:5.6456, Benefit0.697,
Cond:3596, T:136582, TR:-0.0271205556061624, Period:5.6369, Benefit0.6934,
Cond:1279, T:136588, TR:-0.0272765222853397, Period:5.6674, Benefit0.6975,
Cond:6570, T:136592, TR:-0.0272320230561696, Period:5.6569, Benefit0.6963,
Cond:8205, T:136598, TR:-0.0271954707397532, Period:5.6647, Benefit0.6953,
Cond:1616, T:136599, TR:-0.0276562496374666, Period:5.6595, Benefit0.7075,
Cond:1059, T:136608, TR:-0.0271710390500675, Period:5.634, Benefit0.6946,
Cond:1497, T:136617, TR:-0.0272634525050514, Period:5.6549, Benefit0.697,
Cond:4889, T:136623, TR:-0.0271173923321771, Period:5.6462, Benefit0.6931,
Cond:7686, T:136634, TR:-0.0269647559186038, Period:5.6335, Benefit0.689,
Cond:3093, T:136635, TR:-0.0272141877476162, Period:5.6438, Benefit0.6956, , T2:6699,383,6471,3943,7042,369,7615,7339,3491,4893,3846,3150,5192,7997,1723,8304,1983,1725,825,7373,3596,1279,6570,8205,1616,1059,1497,4889,7686,3093,  #End#
End , T0:00:37:40.0457049  #End#





////////////////////////////////////////////////////////
 


LowScoreRank1 , T0:0 , T1:
Cond:3456, T:48726, TR:-0.0107358208582468, Period:4.5474, Benefit0.7463,
Cond:397, T:48648, TR:-0.0106240689432249, Period:4.4939, Benefit0.739,
Cond:6493, T:48880, TR:-0.0106393416610766, Period:4.4312, Benefit0.7366,
Cond:7599, T:48544, TR:-0.0105663613428011, Period:4.4473, Benefit0.7362,
Cond:2619, T:48535, TR:-0.0105580004516572, Period:4.4824, Benefit0.7357,
Cond:1273, T:48729, TR:-0.0105904895102933, Period:4.4738, Benefit0.7352,
Cond:7321, T:48596, TR:-0.0105611045093611, Period:4.4587, Benefit0.735,
Cond:8397, T:49675, TR:-0.0107701763899546, Period:4.6567, Benefit0.7344,
Cond:829, T:49041, TR:-0.0106294584452399, Period:4.5046, Benefit0.7334,
Cond:6925, T:49169, TR:-0.0106432165575515, Period:4.435, Benefit0.7325,
Cond:7357, T:48860, TR:-0.0105720262167616, Period:4.4482, Benefit0.7318,
Cond:4169, T:48796, TR:-0.0105552563569692, Period:4.492, Benefit0.7315,
Cond:4970, T:48641, TR:-0.0105242226382767, Period:4.4451, Benefit0.7315,
Cond:7672, T:48669, TR:-0.0105272021935035, Period:4.4216, Benefit0.7313,
Cond:6988, T:48978, TR:-0.0105798046880818, Period:4.4455, Benefit0.7306,
Cond:2399, T:48826, TR:-0.0105335964425832, Period:4.5178, Benefit0.7294,
Cond:6794, T:48525, TR:-0.010473505152898, Period:4.4563, Benefit0.7294,
Cond:1069, T:49094, TR:-0.0105857830747112, Period:4.4923, Benefit0.7293,
Cond:827, T:48830, TR:-0.0105317599172813, Period:4.4926, Benefit0.7292,
Cond:4911, T:49223, TR:-0.0106062263978133, Period:4.4348, Benefit0.7289,
Cond:6681, T:48624, TR:-0.0104814608443764, Period:4.4374, Benefit0.7285,
Cond:5343, T:49158, TR:-0.0105799897434458, Period:4.4487, Benefit0.7279,
Cond:6661, T:48679, TR:-0.0104832331901591, Period:4.4491, Benefit0.7278,
Cond:7565, T:49263, TR:-0.0105982559834068, Period:4.4534, Benefit0.7277,
Cond:3327, T:49274, TR:-0.0105964584613824, Period:4.4556, Benefit0.7274,
Cond:2829, T:49102, TR:-0.0105582301159485, Period:4.5176, Benefit0.7271,
Cond:4381, T:49444, TR:-0.0106263135226513, Period:4.561, Benefit0.7271,
Cond:6453, T:48764, TR:-0.0104896395833704, Period:4.4491, Benefit0.727,
Cond:129, T:49458, TR:-0.0106210923628795, Period:4.4387, Benefit0.7265,
Cond:3598, T:48588, TR:-0.0104336432828661, Period:4.4331, Benefit0.7254, , T2:3456,397,6493,7599,2619,1273,7321,8397,829,6925,7357,4169,4970,7672,6988,2399,6794,1069,827,4911,6681,5343,6661,7565,3327,2829,4381,6453,129,3598,  #End#
LowScoreRank2 , T0:0 , T1:
Cond:738, T:49633, TR:-0.00896407954652489, Period:4.4708, Benefit0.6002,
Cond:2724, T:51322, TR:-0.00936106129987223, Period:4.4196, Benefit0.6088,
Cond:4304, T:51325, TR:-0.00938789276914839, Period:4.4711, Benefit0.6107,
Cond:2752, T:51430, TR:-0.00947074247189652, Period:4.5578, Benefit0.6154,
Cond:3848, T:52789, TR:-0.00949907085956959, Period:4.3339, Benefit0.6013,
Cond:250, T:51916, TR:-0.00950788994563823, Period:4.3612, Benefit0.6122,
Cond:1620, T:52772, TR:-0.00952049883715088, Period:4.4563, Benefit0.603,
Cond:1606, T:51622, TR:-0.00952404900973844, Period:4.4086, Benefit0.6169,
Cond:2266, T:53088, TR:-0.00957565421776121, Period:4.3048, Benefit0.6032,
Cond:3402, T:52696, TR:-0.00957767009040595, Period:4.3571, Benefit0.6079,
Cond:292, T:49281, TR:-0.00957914587010758, Period:4.4953, Benefit0.6508,
Cond:6096, T:53271, TR:-0.00958723987556783, Period:4.4902, Benefit0.6019,
Cond:4056, T:52708, TR:-0.00958821375741523, Period:4.3161, Benefit0.6085,
Cond:2970, T:53000, TR:-0.00958828986954217, Period:4.4027, Benefit0.6051,
Cond:1188, T:50347, TR:-0.0095992905276473, Period:4.5062, Benefit0.6383,
Cond:1580, T:52070, TR:-0.00960546433633213, Period:4.3126, Benefit0.6173,
Cond:3834, T:51851, TR:-0.00961739676562604, Period:4.3431, Benefit0.6208,
Cond:151, T:51444, TR:-0.00966340945315196, Period:4.4562, Benefit0.6291,
Cond:922, T:50617, TR:-0.00966564548416975, Period:4.376, Benefit0.6397,
Cond:2710, T:50285, TR:-0.00966988928956182, Period:4.4038, Benefit0.6443,
Cond:2042, T:51367, TR:-0.00967786846294025, Period:4.3434, Benefit0.6311,
Cond:6082, T:53913, TR:-0.00969185655555985, Period:4.4851, Benefit0.6018,
Cond:1146, T:53135, TR:-0.00971118520181395, Period:4.3138, Benefit0.6121,
Cond:5626, T:53048, TR:-0.00971376770998623, Period:4.3307, Benefit0.6133,
Cond:8132, T:53697, TR:-0.00971855469472107, Period:4.2895, Benefit0.6061,
Cond:5446, T:51534, TR:-0.00972763713306216, Period:4.3212, Benefit0.6326,
Cond:2375, T:53543, TR:-0.00973343695301949, Period:4.5237, Benefit0.6089,
Cond:1459, T:51873, TR:-0.00974021288394073, Period:4.4561, Benefit0.6293,
Cond:264, T:51990, TR:-0.00974074806097714, Period:4.4041, Benefit0.6279,
Cond:4264, T:50187, TR:-0.00975012721195433, Period:4.3641, Benefit0.6515, , T2:738,2724,4304,2752,3848,250,1620,1606,2266,3402,292,6096,4056,2970,1188,1580,3834,151,922,2710,2042,6082,1146,5626,8132,5446,2375,1459,264,4264,  #End#
LowScoreRank3 , T0:0 , T1:
Cond:4367, T:68166, TR:-0.0128028874533461, Period:5.6231, Benefit0.6422,
Cond:8381, T:64932, TR:-0.0122569760034949, Period:5.7154, Benefit0.6438,
Cond:6159, T:63334, TR:-0.0124413535098517, Period:5.4147, Benefit0.6711,
Cond:1693, T:61686, TR:-0.0109370940734323, Period:5.0249, Benefit0.5993,
Cond:589, T:61571, TR:-0.010958103149068, Period:4.9195, Benefit0.6017,
Cond:5951, T:61546, TR:-0.0111139309762979, Period:5.1372, Benefit0.6113,
Cond:80, T:61357, TR:-0.0113080052524293, Period:4.7923, Benefit0.6249,
Cond:4365, T:60734, TR:-0.0111070006951528, Period:5.2417, Benefit0.6192,
Cond:5725, T:60636, TR:-0.0107784213932463, Period:5.0445, Benefit0.6002,
Cond:7305, T:59170, TR:-0.0105440664358409, Period:4.5477, Benefit0.6007,
Cond:6439, T:58782, TR:-0.010465587448838, Period:4.4313, Benefit0.5998,
Cond:1945, T:58467, TR:-0.0104752691542815, Period:4.5705, Benefit0.6037,
Cond:2381, T:58435, TR:-0.0105237008503562, Period:4.7721, Benefit0.6071,
Cond:1949, T:58145, TR:-0.0107206178447449, Period:4.6445, Benefit0.6227,
Cond:8399, T:58138, TR:-0.011578101464314, Period:5.1439, Benefit0.6773,
Cond:4653, T:58033, TR:-0.0104392720056762, Period:4.5731, Benefit0.606,
Cond:2377, T:57905, TR:-0.0103380489464241, Period:4.7105, Benefit0.6009,
Cond:2383, T:57893, TR:-0.0105913145175011, Period:4.9477, Benefit0.6172,
Cond:1485, T:57291, TR:-0.0108067343048657, Period:4.7568, Benefit0.6377,
Cond:6479, T:57116, TR:-0.0104176944569294, Period:4.3914, Benefit0.6145,
Cond:1051, T:56789, TR:-0.0103624368241287, Period:4.5988, Benefit0.6145,
Cond:7488, T:56764, TR:-0.0103121643991193, Period:4.7739, Benefit0.6115,
Cond:5103, T:56731, TR:-0.0102713329300692, Period:4.5095, Benefit0.6092,
Cond:992, T:56635, TR:-0.0110592373346211, Period:5.1263, Benefit0.6617,
Cond:4671, T:56622, TR:-0.010352592846806, Period:4.4528, Benefit0.6157,
Cond:7099, T:56540, TR:-0.0103066073406869, Period:4.4745, Benefit0.6136,
Cond:3390, T:56457, TR:-0.0104147266939557, Period:4.3658, Benefit0.6216,
Cond:4750, T:56379, TR:-0.0101178645956328, Period:4.4395, Benefit0.603,
Cond:813, T:56339, TR:-0.010744915620107, Period:4.7608, Benefit0.6446,
Cond:5085, T:56323, TR:-0.0102654333404622, Period:4.6101, Benefit0.6133, , T2:4367,8381,6159,1693,589,5951,80,4365,5725,7305,6439,1945,2381,1949,8399,4653,2377,2383,1485,6479,1051,7488,5103,992,4671,7099,3390,4750,813,5085,  #End#
LowScoreRank4 , T0:0 , T1:
Cond:3820, T:48471, TR:-0.0101632487274091, Period:4.4218, Benefit0.7065,
Cond:3162, T:48481, TR:-0.0103548470909142, Period:4.4324, Benefit0.721,
Cond:2968, T:48483, TR:-0.0102976858781951, Period:4.4525, Benefit0.7166,
Cond:4054, T:48492, TR:-0.0102471176113495, Period:4.4337, Benefit0.7126,
Cond:6598, T:48496, TR:-0.0103133198697385, Period:4.4966, Benefit0.7176,
Cond:920, T:48499, TR:-0.0101699713041299, Period:4.4365, Benefit0.7066,
Cond:1576, T:48504, TR:-0.010170938244358, Period:4.4197, Benefit0.7066,
Cond:1396, T:48505, TR:-0.0101737490106926, Period:4.4749, Benefit0.7068,
Cond:5236, T:48505, TR:-0.0100716712696293, Period:4.4193, Benefit0.699,
Cond:6794, T:48525, TR:-0.010473505152898, Period:4.4563, Benefit0.7294,
Cond:4663, T:48534, TR:-0.0103770906357518, Period:4.4584, Benefit0.7219,
Cond:2619, T:48535, TR:-0.0105580004516572, Period:4.4824, Benefit0.7357,
Cond:2736, T:48538, TR:-0.0101028668198453, Period:4.501, Benefit0.7009,
Cond:7599, T:48544, TR:-0.0105663613428011, Period:4.4473, Benefit0.7362,
Cond:2940, T:48556, TR:-0.0104010885247671, Period:4.4433, Benefit0.7234,
Cond:3305, T:48559, TR:-0.010418714628256, Period:4.4575, Benefit0.7247,
Cond:803, T:48587, TR:-0.010223695884223, Period:4.4492, Benefit0.7094,
Cond:3598, T:48588, TR:-0.0104336432828661, Period:4.4331, Benefit0.7254,
Cond:7321, T:48596, TR:-0.0105611045093611, Period:4.4587, Benefit0.735,
Cond:1632, T:48603, TR:-0.0101323840170867, Period:4.5149, Benefit0.7022,
Cond:1354, T:48612, TR:-0.0100173801591571, Period:4.4149, Benefit0.6933,
Cond:7240, T:48615, TR:-0.0104363804028362, Period:4.4363, Benefit0.7252,
Cond:2278, T:48621, TR:-0.0100636912719409, Period:4.4311, Benefit0.6967,
Cond:6681, T:48624, TR:-0.0104814608443764, Period:4.4374, Benefit0.7285,
Cond:391, T:48628, TR:-0.0103261243097485, Period:4.4415, Benefit0.7166,
Cond:6080, T:48630, TR:-0.0103698160709892, Period:4.4551, Benefit0.7199,
Cond:7077, T:48635, TR:-0.0104390378665665, Period:4.4422, Benefit0.7251,
Cond:4970, T:48641, TR:-0.0105242226382767, Period:4.4451, Benefit0.7315,
Cond:147, T:48643, TR:-0.0103907520181488, Period:4.4275, Benefit0.7213,
Cond:397, T:48648, TR:-0.0106240689432249, Period:4.4939, Benefit0.739, , T2:3820,3162,2968,4054,6598,920,1576,1396,5236,6794,4663,2619,2736,7599,2940,3305,803,3598,7321,1632,1354,7240,2278,6681,391,6080,7077,4970,147,397,  #End#
End , T0:00:34:54.9103209  #End#



 
 */

