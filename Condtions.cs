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
			1618, //
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

		};
		private static readonly int[] KouhoOrs = new int[] {
			//AllTrueCondIdx-1
			3458,
			2619,
7599,
7321,
1273,
1961,
2998,
3444,
3890,
3206,

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

					if (tMin > trueAll[i, j]) {
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
				if (IsAndCheck) {
					needNum = tMax * 0.7;
					needBenefit = baseBenefit * 1.05;
				} else {
					needNum = tMin + 1000;
					needBenefit = baseBenefit * 0.8;
				}

				int max = maxNum;
				string result = "";
				string result2 = "";
				// OrderByDescending:高い順、OrderBy：低い順

				var rankRes = benefitRes.Where(c => trueAll[i, c.Key] >= needNum && c.Value >= needBenefit);


				maxBenefit = 0;
				foreach (KeyValuePair<int, double> b in rankRes.OrderByDescending(c => c.Value)) {
					if (max > 0) {
						double tr = (AllCond51Num * AllCond51Ratio - trueAll[i, b.Key] * b.Value) / (AllCond51Num - trueAll[i, b.Key]);
						result += "\nCond:" + b.Key + ", T:" + trueAll[i, b.Key] + ", TR:" + tr + ", Period:" + havePeriodRes[b.Key] + ", Benefit" + b.Value + ",";
						result2 += b.Key + ",";
						maxBenefit = Math.Max(maxBenefit, b.Value);
					}
					max--;
				}
				Common.DebugInfo("LowScoreRank1", kouhoList[i], result, result2);


				result = "";
				result2 = "";
				max = 30;
				foreach (KeyValuePair<int, double> b in rankRes.OrderByDescending(c =>
					//c.Value >= maxBenefit * 0.5 & trueAll[i, c.Key] >= needNum ?
					(AllCond51Num * AllCond51Ratio - trueAll[i, c.Key] * c.Value) / (AllCond51Num - trueAll[i, c.Key])
				)) {
					if (max > 0) {
						double tr = (AllCond51Num * AllCond51Ratio - trueAll[i, b.Key] * b.Value) / (AllCond51Num - trueAll[i, b.Key]);
						result += "\nCond:" + b.Key + ", T:" + trueAll[i, b.Key] + ", TR:" + tr + ", Period:" + havePeriodRes[b.Key] + ", Benefit" + b.Value + ",";
						result2 += b.Key + ",";
					}
					max--;
				}
				//Common.DebugInfo("LowScoreRank2", kouhoList[i], result, result2);


				result = "";
				result2 = "";
				max = 30;
				foreach (KeyValuePair<int, double> b2 in rankRes.OrderByDescending(c => trueAll[i, c.Key])) {
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
				foreach (KeyValuePair<int, double> b in rankRes.OrderBy(c => trueAll[i, c.Key])) {
					if (max > 0) {
						double tr = (AllCond51Num * AllCond51Ratio - trueAll[i, b.Key] * b.Value) / (AllCond51Num - trueAll[i, b.Key]);
						result += "\nCond:" + b.Key + ", T:" + trueAll[i, b.Key] + ", TR:" + tr + ", Period:" + havePeriodRes[b.Key] + ", Benefit" + b.Value + ",";
						result2 += b.Key + ",";
					}
					max--;
				}
				//Common.DebugInfo("LowScoreRank4", kouhoList[i], result, result2);


				result = "";
				result2 = "";
				max = 30;
				foreach (KeyValuePair<int, double> benefitResB in rankRes.OrderByDescending(c => c.Value)) {
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
			Common.DebugInfo("DebugCheckCond51", trueAll, Common.Round((double)benefitAll / trueAll, 4), Common.Round((double)havePeriodAll / trueAll, 4));
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
 * 

 LowScoreRank1 , T0:3458 , T1:
Cond:6159, T:84795, TR:-0.0117828653382164, Period:5.707, Benefit0.4696,
Cond:8397, T:72391, TR:-0.0101358348978071, Period:5.3041, Benefit0.4683,
Cond:397, T:71619, TR:-0.0100258341933941, Period:5.2172, Benefit0.4678,
Cond:6493, T:71739, TR:-0.0100004991199102, Period:5.1777, Benefit0.4657,
Cond:5248, T:72602, TR:-0.0101080435490581, Period:5.249, Benefit0.4655,
Cond:829, T:72007, TR:-0.0100272947148141, Period:5.2224, Benefit0.4653,
Cond:6157, T:76248, TR:-0.0105669955108385, Period:5.4336, Benefit0.4649,
Cond:6925, T:72000, TR:-0.010012706348228, Period:5.1762, Benefit0.4646,
Cond:6988, T:71875, TR:-0.00998684053129355, Period:5.1838, Benefit0.4641,
Cond:7357, T:71666, TR:-0.0099599390294362, Period:5.1887, Benefit0.4641,
Cond:4381, T:72239, TR:-0.0100278153817078, Period:5.25, Benefit0.4638,
Cond:7565, T:71978, TR:-0.00999423619379034, Period:5.1871, Benefit0.4638,
Cond:1069, T:72051, TR:-0.00999775839319437, Period:5.2109, Benefit0.4635,
Cond:4970, T:71479, TR:-0.00992422844719576, Period:5.1879, Benefit0.4635,
Cond:8379, T:76601, TR:-0.0105813876390764, Period:5.6069, Benefit0.4634,
Cond:4169, T:71771, TR:-0.00995396726195082, Period:5.2149, Benefit0.4631,
Cond:4911, T:71986, TR:-0.00997767396128051, Period:5.1787, Benefit0.4629,
Cond:2829, T:72037, TR:-0.00998031033365745, Period:5.229, Benefit0.4627,
Cond:3327, T:72001, TR:-0.00997177987691715, Period:5.188, Benefit0.4625,
Cond:4367, T:89343, TR:-0.0122046472328929, Period:5.8404, Benefit0.4624,
Cond:2399, T:71757, TR:-0.00993073824783221, Period:5.2296, Benefit0.462,
Cond:827, T:71819, TR:-0.0099328326994866, Period:5.2156, Benefit0.4617,
Cond:7755, T:72369, TR:-0.010003273123247, Period:5.2134, Benefit0.4617,
Cond:5343, T:71881, TR:-0.00993491712120945, Period:5.1867, Benefit0.4614,
Cond:7658, T:72064, TR:-0.0099544243061435, Period:5.1804, Benefit0.4612,
Cond:1965, T:72770, TR:-0.0100427860045691, Period:5.1986, Benefit0.4611,
Cond:129, T:72071, TR:-0.00994944897639442, Period:5.1823, Benefit0.4609,
Cond:1067, T:72045, TR:-0.00993829995158629, Period:5.1975, Benefit0.4605,
Cond:6661, T:71528, TR:-0.00987226873738583, Period:5.1901, Benefit0.4605,
Cond:7630, T:71672, TR:-0.00988871263847281, Period:5.1818, Benefit0.4604, , T2:6159,8397,397,6493,5248,829,6157,6925,6988,7357,4381,7565,1069,4970,8379,4169,4911,2829,3327,4367,2399,827,7755,5343,7658,1965,129,1067,6661,7630,  #End#
LowScoreRank3 , T0:3458 , T1:
Cond:320, T:138525, TR:-0.015341983566353, Period:6.6118, Benefit0.3758,
Cond:4351, T:118054, TR:-0.0132669201744815, Period:6.4181, Benefit0.3797,
Cond:6143, T:108734, TR:-0.0132346331782109, Period:6.285, Benefit0.4123,
Cond:1679, T:107934, TR:-0.0124280316362815, Period:6.2887, Benefit0.3882,
Cond:6135, T:100210, TR:-0.0116910002506378, Period:6.4528, Benefit0.3922,
Cond:2799, T:98700, TR:-0.0124393359725134, Period:6.099, Benefit0.4261,
Cond:6141, T:98523, TR:-0.0122429602399981, Period:6.1736, Benefit0.4196,
Cond:8383, T:98414, TR:-0.0122892999678379, Period:6.1024, Benefit0.4218,
Cond:6139, T:98032, TR:-0.0114021912912028, Period:6.1687, Benefit0.3904,
Cond:8371, T:97758, TR:-0.0116554339995756, Period:6.5445, Benefit0.401,
Cond:1471, T:92668, TR:-0.010930074624722, Period:5.9046, Benefit0.395,
Cond:5933, T:91642, TR:-0.0107613453081476, Period:5.837, Benefit0.3928,
Cond:8173, T:91253, TR:-0.0103822405060769, Period:5.7926, Benefit0.3793,
Cond:2591, T:89812, TR:-0.0106872690788781, Period:5.779, Benefit0.398,
Cond:4367, T:89343, TR:-0.0122046472328929, Period:5.8404, Benefit0.4624,
Cond:1039, T:88966, TR:-0.0100444888137221, Period:5.6176, Benefit0.3754,
Cond:4159, T:87457, TR:-0.010637072819133, Period:5.6265, Benefit0.4069,
Cond:4363, T:87001, TR:-0.0102934117705435, Period:5.6502, Benefit0.3946,
Cond:8191, T:86291, TR:-0.00990456657984869, Period:5.4841, Benefit0.3814,
Cond:3069, T:86257, TR:-0.00989623764954156, Period:5.2777, Benefit0.3812,
Cond:8381, T:86136, TR:-0.0115620369967973, Period:5.8973, Benefit0.4527,
Cond:2112, T:85832, TR:-0.011158631961263, Period:5.7024, Benefit0.4371,
Cond:6159, T:84795, TR:-0.0117828653382164, Period:5.707, Benefit0.4696,
Cond:6647, T:84742, TR:-0.00976019541249607, Period:5.0536, Benefit0.3823,
Cond:1257, T:84677, TR:-0.00976248053775684, Period:5.3334, Benefit0.3827,
Cond:3951, T:84309, TR:-0.00969786778264017, Period:5.4256, Benefit0.3816,
Cond:1693, T:84174, TR:-0.0103989316601248, Period:5.4787, Benefit0.4128,
Cond:589, T:84166, TR:-0.0103062932699852, Period:5.4108, Benefit0.4088,
Cond:6441, T:84046, TR:-0.00989649917512068, Period:5.09, Benefit0.3915,
Cond:7228, T:83804, TR:-0.0096280605096808, Period:5.1088, Benefit0.3809, , T2:320,4351,6143,1679,6135,2799,6141,8383,6139,8371,1471,5933,8173,2591,4367,1039,4159,4363,8191,3069,8381,2112,6159,6647,1257,3951,1693,589,6441,7228,  #End#
LowScoreRank1 , T0:2619 , T1:
Cond:3456, T:51246, TR:-0.0106075122938515, Period:4.6174, Benefit0.6998,
Cond:397, T:51084, TR:-0.0105171329439317, Period:4.5661, Benefit0.6955,
Cond:829, T:51411, TR:-0.0105142264581238, Period:4.5732, Benefit0.6908,
Cond:6493, T:51388, TR:-0.0105098710548784, Period:4.5081, Benefit0.6908,
Cond:8397, T:52196, TR:-0.010643175213696, Period:4.7196, Benefit0.6894,
Cond:1069, T:51502, TR:-0.0104772229485249, Period:4.5627, Benefit0.6869,
Cond:6925, T:51681, TR:-0.0105053494289406, Period:4.51, Benefit0.6865,
Cond:4381, T:51835, TR:-0.0105315364363062, Period:4.6242, Benefit0.6863,
Cond:6681, T:51043, TR:-0.0103797753662885, Period:4.5153, Benefit0.6861,
Cond:6988, T:51477, TR:-0.0104613953608689, Period:4.5204, Benefit0.6861,
Cond:7357, T:51362, TR:-0.0104383792388007, Period:4.5234, Benefit0.686,
Cond:4911, T:51716, TR:-0.0104951786798878, Period:4.511, Benefit0.6853,
Cond:4970, T:51142, TR:-0.0103859646973441, Period:4.52, Benefit0.6852,
Cond:1067, T:51045, TR:-0.0103581008288423, Period:4.5401, Benefit0.6845,
Cond:2829, T:51442, TR:-0.0104311988603041, Period:4.5831, Benefit0.6844,
Cond:3963, T:51016, TR:-0.0103512829640349, Period:4.5369, Benefit0.6844,
Cond:7672, T:51148, TR:-0.0103719007208393, Period:4.4995, Benefit0.6841,
Cond:6661, T:51137, TR:-0.0103643154360622, Period:4.5243, Benefit0.6837,
Cond:2399, T:51311, TR:-0.010384454704441, Period:4.5892, Benefit0.6828,
Cond:1501, T:51526, TR:-0.0104205238032489, Period:4.5645, Benefit0.6825,
Cond:7115, T:51342, TR:-0.0103847117334099, Period:4.5224, Benefit0.6824,
Cond:1965, T:52224, TR:-0.0105483274552201, Period:4.5568, Benefit0.6823,
Cond:5343, T:51645, TR:-0.0104386063537318, Period:4.5242, Benefit0.6822,
Cond:6794, T:51037, TR:-0.0103249072605282, Period:4.5314, Benefit0.6822,
Cond:3327, T:51739, TR:-0.0104547910012754, Period:4.5305, Benefit0.6821,
Cond:7565, T:51758, TR:-0.0104527536266637, Period:4.5276, Benefit0.6817,
Cond:7755, T:52149, TR:-0.0105258432699919, Period:4.5785, Benefit0.6817,
Cond:6453, T:51220, TR:-0.010343912769672, Period:4.5238, Benefit0.6811,
Cond:7077, T:51069, TR:-0.0103143446428374, Period:4.5185, Benefit0.681,
Cond:5305, T:51359, TR:-0.0103657036203597, Period:4.5336, Benefit0.6808, , T2:3456,397,829,6493,8397,1069,6925,4381,6681,6988,7357,4911,4970,1067,2829,3963,7672,6661,2399,1501,7115,1965,5343,6794,3327,7565,7755,6453,7077,5305,  #End#
LowScoreRank3 , T0:2619 , T1:
Cond:2112, T:70821, TR:-0.0118120131246327, Period:5.5854, Benefit0.5661,
Cond:4367, T:70477, TR:-0.0126188354039956, Period:5.6319, Benefit0.6111,
Cond:8381, T:67398, TR:-0.0121565269994099, Period:5.7262, Benefit0.6143,
Cond:6159, T:65754, TR:-0.0122748257400386, Period:5.4353, Benefit0.6366,
Cond:5951, T:63894, TR:-0.0109801651783586, Period:5.1704, Benefit0.5807,
Cond:1693, T:63654, TR:-0.0108365024013663, Period:5.0564, Benefit0.5746,
Cond:80, T:63653, TR:-0.0111105293358615, Period:4.8431, Benefit0.5905,
Cond:589, T:63511, TR:-0.0107708100997596, Period:4.9565, Benefit0.5721,
Cond:1261, T:63097, TR:-0.0105242801070159, Period:4.9023, Benefit0.5615,
Cond:4365, T:62928, TR:-0.0109668224914801, Period:5.2661, Benefit0.589,
Cond:4361, T:62876, TR:-0.0105070695994865, Period:5.3192, Benefit0.5625,
Cond:5725, T:62850, TR:-0.0107311442878672, Period:5.0729, Benefit0.5759,
Cond:4157, T:61210, TR:-0.0102784486327442, Period:5.0386, Benefit0.5643,
Cond:5743, T:60882, TR:-0.0102768742621827, Period:4.8865, Benefit0.5673,
Cond:6439, T:60842, TR:-0.0103513245732861, Period:4.4899, Benefit0.5722,
Cond:7305, T:60735, TR:-0.010493927436101, Period:4.601, Benefit0.5819,
Cond:7674, T:60710, TR:-0.0101579521473801, Period:4.4834, Benefit0.5617,
Cond:8399, T:60613, TR:-0.0114473163277321, Period:5.1777, Benefit0.6412,
Cond:5196, T:60524, TR:-0.01013407566232, Period:4.4716, Benefit0.562,
Cond:2381, T:60401, TR:-0.0104191183972868, Period:4.8182, Benefit0.5806,
Cond:4653, T:60203, TR:-0.0103776686746874, Period:4.6341, Benefit0.58,
Cond:1945, T:60201, TR:-0.0103643139352881, Period:4.6193, Benefit0.5792,
Cond:2383, T:60195, TR:-0.0104106041701458, Period:4.9885, Benefit0.5821,
Cond:1949, T:60078, TR:-0.0106129792487187, Period:4.6959, Benefit0.5957,
Cond:2811, T:60018, TR:-0.0101840483156638, Period:4.9657, Benefit0.5699,
Cond:6667, T:59904, TR:-0.0100380743484001, Period:4.4841, Benefit0.562,
Cond:811, T:59710, TR:-0.0101437766515046, Period:4.8196, Benefit0.5704,
Cond:2377, T:59706, TR:-0.0102158583430307, Period:4.7566, Benefit0.5749,
Cond:6479, T:59501, TR:-0.0102944991745109, Period:4.4608, Benefit0.5818,
Cond:7079, T:59343, TR:-0.0101021815283979, Period:4.498, Benefit0.5714, , T2:2112,4367,8381,6159,5951,1693,80,589,1261,4365,4361,5725,4157,5743,6439,7305,7674,8399,5196,2381,4653,1945,2383,1949,2811,6667,811,2377,6479,7079,  #End#
LowScoreRank1 , T0:7599 , T1:
Cond:3456, T:51240, TR:-0.0105966769048662, Period:4.5846, Benefit0.6991,
Cond:397, T:51169, TR:-0.0105057048181597, Period:4.5337, Benefit0.6935,
Cond:5343, T:51044, TR:-0.0104102824126947, Period:4.5017, Benefit0.6883,
Cond:8397, T:52182, TR:-0.0106222075088908, Period:4.6876, Benefit0.6881,
Cond:829, T:51554, TR:-0.010491191400609, Period:4.5437, Benefit0.6872,
Cond:4169, T:51316, TR:-0.0104269579978237, Period:4.5316, Benefit0.6858,
Cond:3327, T:51314, TR:-0.010423810967358, Period:4.5047, Benefit0.6856,
Cond:6988, T:51482, TR:-0.0104498246564512, Period:4.4878, Benefit0.6852,
Cond:4970, T:51106, TR:-0.0103764440738921, Period:4.4882, Benefit0.685,
Cond:6681, T:51033, TR:-0.0103613609211813, Period:4.4841, Benefit0.6849,
Cond:1069, T:51607, TR:-0.010466337786594, Period:4.5315, Benefit0.6847,
Cond:7565, T:51310, TR:-0.0104022779691844, Period:4.5039, Benefit0.6841,
Cond:827, T:51351, TR:-0.0104044202989846, Period:4.5322, Benefit0.6837,
Cond:7672, T:51123, TR:-0.0103616919149524, Period:4.468, Benefit0.6837,
Cond:2399, T:51327, TR:-0.0103985364434588, Period:4.5573, Benefit0.6836,
Cond:6719, T:51466, TR:-0.0104231945153399, Period:4.4815, Benefit0.6835,
Cond:4687, T:51906, TR:-0.0105042469664973, Period:4.4811, Benefit0.6834,
Cond:7807, T:50995, TR:-0.0103308223789523, Period:4.5032, Benefit0.6832,
Cond:2829, T:51615, TR:-0.0104399641646534, Period:4.5557, Benefit0.6827,
Cond:6661, T:51143, TR:-0.0103502504984822, Period:4.4927, Benefit0.6826,
Cond:4381, T:51957, TR:-0.0105011758345893, Period:4.5966, Benefit0.6825,
Cond:6794, T:51029, TR:-0.0103206560166875, Period:4.499, Benefit0.682,
Cond:7115, T:51301, TR:-0.0103618054755055, Period:4.4935, Benefit0.6813,
Cond:6453, T:51221, TR:-0.0103330356098621, Period:4.4924, Benefit0.6803,
Cond:7077, T:51123, TR:-0.0103120011545015, Period:4.4855, Benefit0.6801,
Cond:1067, T:51587, TR:-0.010395719419901, Period:4.509, Benefit0.6799,
Cond:129, T:51919, TR:-0.0104562077469229, Period:4.4785, Benefit0.6798,
Cond:1501, T:51694, TR:-0.0104128729571678, Period:4.5342, Benefit0.6797,
Cond:7630, T:51304, TR:-0.0103291196555119, Period:4.4806, Benefit0.6789,
Cond:1965, T:52355, TR:-0.0105219360842624, Period:4.5247, Benefit0.6787, , T2:3456,397,5343,8397,829,4169,3327,6988,4970,6681,1069,7565,827,7672,2399,6719,4687,7807,2829,6661,4381,6794,7115,6453,7077,1067,129,1501,7630,1965,  #End#
LowScoreRank3 , T0:7599 , T1:
Cond:2112, T:70842, TR:-0.0118172303132102, Period:5.5638, Benefit0.5662,
Cond:4367, T:70518, TR:-0.0126430020106569, Period:5.6137, Benefit0.612,
Cond:8381, T:67372, TR:-0.0121429748150109, Period:5.7027, Benefit0.6138,
Cond:6159, T:65692, TR:-0.0122728241540022, Period:5.4129, Benefit0.6371,
Cond:1693, T:64157, TR:-0.0108050370081358, Period:5.0349, Benefit0.5682,
Cond:589, T:64027, TR:-0.0107690304972366, Period:4.9343, Benefit0.5673,
Cond:5951, T:63677, TR:-0.0109591820815348, Period:5.1507, Benefit0.5815,
Cond:80, T:63650, TR:-0.0110893481294612, Period:4.8191, Benefit0.5893,
Cond:4361, T:63458, TR:-0.0105664682155524, Period:5.2942, Benefit0.5607,
Cond:4365, T:63204, TR:-0.0109312105517214, Period:5.2444, Benefit0.5843,
Cond:5725, T:62963, TR:-0.0107269289830801, Period:5.0514, Benefit0.5746,
Cond:4157, T:61523, TR:-0.0102621771085052, Period:5.0152, Benefit0.5604,
Cond:7305, T:61356, TR:-0.0104606337113818, Period:4.5815, Benefit0.5739,
Cond:2811, T:60951, TR:-0.0102200117847228, Period:4.9432, Benefit0.5632,
Cond:6439, T:60943, TR:-0.0103425055914507, Period:4.468, Benefit0.5707,
Cond:2381, T:60875, TR:-0.0104109106691886, Period:4.7942, Benefit0.5755,
Cond:1945, T:60864, TR:-0.0103498510923866, Period:4.5994, Benefit0.5719,
Cond:7674, T:60653, TR:-0.0101507705204301, Period:4.4577, Benefit0.5618,
Cond:811, T:60566, TR:-0.0101766477273219, Period:4.7988, Benefit0.5642,
Cond:1949, T:60502, TR:-0.0106072320126982, Period:4.6716, Benefit0.5911,
Cond:8399, T:60499, TR:-0.0114370027373959, Period:5.1526, Benefit0.6418,
Cond:5196, T:60483, TR:-0.0101211775845786, Period:4.4449, Benefit0.5616,
Cond:5743, T:60453, TR:-0.0102654407677714, Period:4.8692, Benefit0.5707,
Cond:2377, T:60373, TR:-0.0101989345367035, Period:4.7365, Benefit0.5674,
Cond:2383, T:60189, TR:-0.0104226744796664, Period:4.9646, Benefit0.5829,
Cond:4653, T:59848, TR:-0.0103614404052137, Period:4.6149, Benefit0.5825,
Cond:7079, T:59814, TR:-0.0100597538646067, Period:4.476, Benefit0.5642,
Cond:1485, T:59766, TR:-0.0106264820173634, Period:4.7799, Benefit0.5997,
Cond:4651, T:59346, TR:-0.00997097128755202, Period:4.4953, Benefit0.5632,
Cond:1051, T:59295, TR:-0.0102374273917217, Period:4.6283, Benefit0.5803, , T2:2112,4367,8381,6159,1693,589,5951,80,4361,4365,5725,4157,7305,2811,6439,2381,1945,7674,811,1949,8399,5196,5743,2377,2383,4653,7079,1485,4651,1051,  #End#
LowScoreRank1 , T0:7321 , T1:
Cond:3456, T:51278, TR:-0.0106039585809297, Period:4.5948, Benefit0.6991,
Cond:397, T:51140, TR:-0.0105001925164415, Period:4.5461, Benefit0.6935,
Cond:6493, T:51380, TR:-0.0104861587997967, Period:4.4869, Benefit0.6892,
Cond:829, T:51508, TR:-0.01049921537364, Period:4.5547, Benefit0.6884,
Cond:8397, T:52176, TR:-0.0106224844052059, Period:4.6967, Benefit0.6882,
Cond:4169, T:51261, TR:-0.010448452565257, Period:4.5424, Benefit0.6881,
Cond:1069, T:51549, TR:-0.0104791137376912, Period:4.5437, Benefit0.6864,
Cond:7357, T:51347, TR:-0.010423080662037, Period:4.5017, Benefit0.6851,
Cond:6988, T:51527, TR:-0.010454103513054, Period:4.4981, Benefit0.6849,
Cond:4970, T:51175, TR:-0.0103852537446901, Period:4.4982, Benefit0.6847,
Cond:6925, T:51665, TR:-0.0104772255104408, Period:4.4888, Benefit0.6847,
Cond:4911, T:51714, TR:-0.0104836310687162, Period:4.4899, Benefit0.6845,
Cond:827, T:51335, TR:-0.0104083521549994, Period:4.5427, Benefit0.6842,
Cond:2829, T:51501, TR:-0.0104325339471862, Period:4.5646, Benefit0.6837,
Cond:7672, T:51187, TR:-0.0103695391639547, Period:4.4775, Benefit0.6834,
Cond:2399, T:51362, TR:-0.0103995476826936, Period:4.5668, Benefit0.6832,
Cond:6453, T:51055, TR:-0.0103420571921343, Period:4.5043, Benefit0.6832,
Cond:3963, T:51392, TR:-0.0104023908320089, Period:4.5238, Benefit0.683,
Cond:4381, T:51808, TR:-0.0104788878959611, Period:4.6031, Benefit0.6829,
Cond:7565, T:51722, TR:-0.0104585951991324, Period:4.5067, Benefit0.6826,
Cond:7755, T:51765, TR:-0.0104638471257083, Period:4.5582, Benefit0.6824,
Cond:6794, T:51081, TR:-0.0103248593357372, Period:4.509, Benefit0.6816,
Cond:1067, T:51513, TR:-0.0104041816830225, Period:4.5206, Benefit0.6815,
Cond:5343, T:51644, TR:-0.0104244736995314, Period:4.5026, Benefit0.6812,
Cond:1499, T:51773, TR:-0.0104471687285805, Period:4.5437, Benefit0.6811,
Cond:3327, T:51748, TR:-0.010442500072101, Period:4.5089, Benefit0.6811,
Cond:5305, T:51364, TR:-0.0103707974279618, Period:4.5166, Benefit0.6811,
Cond:7077, T:51079, TR:-0.0103162111395113, Period:4.4977, Benefit0.681,
Cond:6943, T:51535, TR:-0.0103971593651263, Period:4.4911, Benefit0.6807,
Cond:1965, T:52258, TR:-0.0105306962600618, Period:4.5377, Benefit0.6806, , T2:3456,397,6493,829,8397,4169,1069,7357,6988,4970,6925,4911,827,2829,7672,2399,6453,3963,4381,7565,7755,6794,1067,5343,1499,3327,5305,7077,6943,1965,  #End#
LowScoreRank3 , T0:7321 , T1:
Cond:2112, T:70849, TR:-0.0118337136765999, Period:5.5703, Benefit0.567,
Cond:4367, T:70499, TR:-0.0126512615075452, Period:5.617, Benefit0.6126,
Cond:8381, T:67292, TR:-0.0121385194877744, Period:5.7106, Benefit0.6143,
Cond:6159, T:65724, TR:-0.0122695491366332, Period:5.4166, Benefit0.6366,
Cond:5951, T:63838, TR:-0.0109469747974017, Period:5.1519, Benefit0.5793,
Cond:1693, T:63802, TR:-0.0107614494365341, Period:5.0455, Benefit0.5689,
Cond:80, T:63706, TR:-0.011117441693984, Period:4.826, Benefit0.5904,
Cond:589, T:63670, TR:-0.0107493449399873, Period:4.9447, Benefit0.5694,
Cond:4365, T:62845, TR:-0.0109124825623551, Period:5.255, Benefit0.5866,
Cond:5725, T:62497, TR:-0.0106513648838191, Period:5.0622, Benefit0.5745,
Cond:4157, T:61084, TR:-0.0102274289829287, Period:5.0258, Benefit0.5624,
Cond:7674, T:60802, TR:-0.0101639644447717, Period:4.4633, Benefit0.5612,
Cond:2811, T:60792, TR:-0.0102545743643773, Period:4.9548, Benefit0.5668,
Cond:5743, T:60786, TR:-0.0102602190606643, Period:4.8668, Benefit0.5672,
Cond:1945, T:60754, TR:-0.0103999424037907, Period:4.609, Benefit0.576,
Cond:8399, T:60607, TR:-0.0114544591602313, Period:5.1574, Benefit0.6417,
Cond:5196, T:60587, TR:-0.0101323523439765, Period:4.4514, Benefit0.5613,
Cond:7305, T:60485, TR:-0.0104554773648015, Period:4.5964, Benefit0.582,
Cond:2381, T:60457, TR:-0.0103822568066676, Period:4.8054, Benefit0.5778,
Cond:811, T:60437, TR:-0.0102122122221744, Period:4.8096, Benefit0.5676,
Cond:6439, T:60397, TR:-0.010320391326649, Period:4.4776, Benefit0.5746,
Cond:2377, T:60320, TR:-0.0102249346829912, Period:4.746, Benefit0.5695,
Cond:2383, T:60245, TR:-0.0104088390541869, Period:4.9701, Benefit0.5815,
Cond:1949, T:60118, TR:-0.0106130348189656, Period:4.6836, Benefit0.5953,
Cond:4653, T:59891, TR:-0.0103051258883977, Period:4.6195, Benefit0.5786,
Cond:6667, T:59463, TR:-0.00997946373987497, Period:4.4722, Benefit0.5626,
Cond:1485, T:59427, TR:-0.0105704942016675, Period:4.7892, Benefit0.5997,
Cond:6479, T:59323, TR:-0.0102836381715046, Period:4.4451, Benefit0.5829,
Cond:7488, T:59226, TR:-0.0101687087643344, Period:4.8068, Benefit0.5767,
Cond:1951, T:59209, TR:-0.00993690672426189, Period:4.7968, Benefit0.5624, , T2:2112,4367,8381,6159,5951,1693,80,589,4365,5725,4157,7674,2811,5743,1945,8399,5196,7305,2381,811,6439,2377,2383,1949,4653,6667,1485,6479,7488,1951,  #End#
LowScoreRank1 , T0:1273 , T1:
Cond:3456, T:51441, TR:-0.0106365838380211, Period:4.6084, Benefit0.6992,
Cond:397, T:51338, TR:-0.0105461470546622, Period:4.5576, Benefit0.6941,
Cond:6493, T:51573, TR:-0.0105393347070582, Period:4.5, Benefit0.6904,
Cond:8397, T:52395, TR:-0.0106680646980641, Period:4.7104, Benefit0.6885,
Cond:829, T:51717, TR:-0.0105288840266269, Period:4.5672, Benefit0.6877,
Cond:4169, T:51423, TR:-0.0104512388356519, Period:4.5535, Benefit0.6861,
Cond:6925, T:51862, TR:-0.0105310151465397, Period:4.5021, Benefit0.6859,
Cond:1069, T:51763, TR:-0.0105082031658144, Period:4.555, Benefit0.6856,
Cond:7357, T:51551, TR:-0.0104669619497021, Period:4.5153, Benefit0.6855,
Cond:4970, T:51329, TR:-0.0104169284101743, Period:4.5121, Benefit0.6849,
Cond:4911, T:51914, TR:-0.0105211676005112, Period:4.5026, Benefit0.6845,
Cond:827, T:51396, TR:-0.0104197930146969, Period:4.554, Benefit0.6842,
Cond:6988, T:51658, TR:-0.010467541785707, Period:4.5126, Benefit0.6841,
Cond:7672, T:51324, TR:-0.0103965884821153, Period:4.4918, Benefit0.6835,
Cond:2399, T:51527, TR:-0.0104290590601667, Period:4.5801, Benefit0.6831,
Cond:6681, T:51316, TR:-0.0103895472832969, Period:4.5049, Benefit0.6831,
Cond:4381, T:52140, TR:-0.0105297933025922, Period:4.6195, Benefit0.6821,
Cond:2829, T:51791, TR:-0.0104631174819982, Period:4.5788, Benefit0.682,
Cond:6661, T:51347, TR:-0.0103787148810908, Period:4.5164, Benefit0.6819,
Cond:3327, T:51950, TR:-0.0104886422644086, Period:4.5215, Benefit0.6817,
Cond:6794, T:51221, TR:-0.0103510143687402, Period:4.5227, Benefit0.6816,
Cond:5343, T:51844, TR:-0.0104632281329782, Period:4.5155, Benefit0.6813,
Cond:7565, T:51952, TR:-0.0104834041416887, Period:4.5192, Benefit0.6813,
Cond:6453, T:51425, TR:-0.0103752438683707, Period:4.5159, Benefit0.6806,
Cond:1501, T:51841, TR:-0.0104472681892875, Period:4.5577, Benefit0.6802,
Cond:129, T:52086, TR:-0.0104901523258075, Period:4.5014, Benefit0.68,
Cond:1965, T:52506, TR:-0.0105684819040725, Period:4.548, Benefit0.68,
Cond:7115, T:51625, TR:-0.0104041967422766, Period:4.5135, Benefit0.68,
Cond:6943, T:51742, TR:-0.0104246126621055, Period:4.5032, Benefit0.6799,
Cond:7630, T:51463, TR:-0.0103573208979909, Period:4.506, Benefit0.6788, , T2:3456,397,6493,8397,829,4169,6925,1069,7357,4970,4911,827,6988,7672,2399,6681,4381,2829,6661,3327,6794,5343,7565,6453,1501,129,1965,7115,6943,7630,  #End#
LowScoreRank3 , T0:1273 , T1:
Cond:2112, T:71082, TR:-0.0118355923474756, Period:5.5764, Benefit0.5652,
Cond:4367, T:70823, TR:-0.0127024052607135, Period:5.6246, Benefit0.6124,
Cond:8381, T:67622, TR:-0.0121835804086716, Period:5.7159, Benefit0.6137,
Cond:6159, T:66020, TR:-0.012323405087634, Period:5.4259, Benefit0.6367,
Cond:5951, T:64181, TR:-0.01102269797559, Period:5.1618, Benefit0.5805,
Cond:1693, T:64160, TR:-0.0108350606444369, Period:5.0518, Benefit0.5699,
Cond:589, T:63999, TR:-0.0107889207477769, Period:4.951, Benefit0.5687,
Cond:80, T:63848, TR:-0.0111423128214882, Period:4.836, Benefit0.5905,
Cond:4361, T:63486, TR:-0.0106258361142114, Period:5.3091, Benefit0.5639,
Cond:4365, T:63372, TR:-0.0109703162528979, Period:5.2598, Benefit0.585,
Cond:5725, T:63225, TR:-0.0107342172079181, Period:5.067, Benefit0.5726,
Cond:4157, T:61691, TR:-0.0102881359993177, Period:5.0334, Benefit0.5604,
Cond:7305, T:61589, TR:-0.0105375215510694, Period:4.5995, Benefit0.5763,
Cond:5743, T:61143, TR:-0.0103094092989144, Period:4.8779, Benefit0.5668,
Cond:6439, T:61119, TR:-0.0103586084428173, Period:4.4851, Benefit0.57,
Cond:2811, T:60927, TR:-0.010293806545227, Period:4.9597, Benefit0.5679,
Cond:2381, T:60915, TR:-0.0104222022944955, Period:4.8138, Benefit0.5758,
Cond:8399, T:60830, TR:-0.0114856548011183, Period:5.1681, Benefit0.6412,
Cond:7674, T:60767, TR:-0.0101453913184713, Period:4.4772, Benefit0.5604,
Cond:5196, T:60593, TR:-0.0101152384588878, Period:4.4646, Benefit0.5602,
Cond:1949, T:60574, TR:-0.0106304365283712, Period:4.6912, Benefit0.5918,
Cond:2383, T:60517, TR:-0.0104720757271404, Period:4.9811, Benefit0.5827,
Cond:4653, T:60512, TR:-0.0103860995210555, Period:4.6269, Benefit0.5775,
Cond:811, T:60239, TR:-0.010220377219258, Period:4.8148, Benefit0.57,
Cond:7079, T:59910, TR:-0.01010385886999, Period:4.4934, Benefit0.566,
Cond:1945, T:59840, TR:-0.0103115728551147, Period:4.616, Benefit0.5795,
Cond:1485, T:59784, TR:-0.0106553419180309, Period:4.7988, Benefit0.6013,
Cond:6479, T:59652, TR:-0.0103348371529381, Period:4.4551, Benefit0.5828,
Cond:4651, T:59474, TR:-0.0100133561378271, Period:4.5163, Benefit0.5646,
Cond:1951, T:59468, TR:-0.00998828463572998, Period:4.8064, Benefit0.5631, , T2:2112,4367,8381,6159,5951,1693,589,80,4361,4365,5725,4157,7305,5743,6439,2811,2381,8399,7674,5196,1949,2383,4653,811,7079,1945,1485,6479,4651,1951,  #End#
LowScoreRank1 , T0:1961 , T1:
Cond:3456, T:51120, TR:-0.0104660262100791, Period:4.6009, Benefit0.6913,
Cond:397, T:51030, TR:-0.0103883528495708, Period:4.55, Benefit0.6869,
Cond:4169, T:50999, TR:-0.0103260637357457, Period:4.549, Benefit0.6828,
Cond:6493, T:51261, TR:-0.0103654079805596, Period:4.4917, Benefit0.6821,
Cond:829, T:51408, TR:-0.0103776245010771, Period:4.5596, Benefit0.681,
Cond:8397, T:52073, TR:-0.0105003849052059, Period:4.7041, Benefit0.6809,
Cond:827, T:51057, TR:-0.0102900489822829, Period:4.5456, Benefit0.6794,
Cond:6988, T:51349, TR:-0.010334721195441, Period:4.5045, Benefit0.6787,
Cond:1069, T:51462, TR:-0.010352966074638, Period:4.5473, Benefit0.6785,
Cond:6681, T:50960, TR:-0.0102541011974861, Period:4.4986, Benefit0.6781,
Cond:6925, T:51546, TR:-0.0103574557647961, Period:4.4942, Benefit0.6777,
Cond:1067, T:51254, TR:-0.010299056610376, Period:4.5246, Benefit0.6774,
Cond:7357, T:51237, TR:-0.0102959000250834, Period:4.5072, Benefit0.6774,
Cond:1499, T:51508, TR:-0.0103420506445737, Period:4.5473, Benefit0.6771,
Cond:4911, T:51593, TR:-0.0103508633618782, Period:4.495, Benefit0.6766,
Cond:4970, T:51018, TR:-0.0102400866963941, Period:4.5036, Benefit0.6763,
Cond:2829, T:51449, TR:-0.0103074821124269, Period:4.5714, Benefit0.6754,
Cond:7672, T:51025, TR:-0.0102276080371267, Period:4.483, Benefit0.6753,
Cond:4381, T:51796, TR:-0.010366141798336, Period:4.6123, Benefit0.675,
Cond:2399, T:51190, TR:-0.0102470912925899, Period:4.5736, Benefit0.6745,
Cond:3327, T:51614, TR:-0.0103171278350605, Period:4.5144, Benefit0.6739,
Cond:5343, T:51521, TR:-0.0102999455716625, Period:4.5078, Benefit0.6739,
Cond:1965, T:52183, TR:-0.0104194538778833, Period:4.5403, Benefit0.6737,
Cond:6661, T:51019, TR:-0.0102030808428056, Period:4.5087, Benefit0.6736,
Cond:6794, T:50914, TR:-0.0101768235375429, Period:4.5148, Benefit0.6731,
Cond:7565, T:51635, TR:-0.0103098532970085, Period:4.5115, Benefit0.6731,
Cond:129, T:51803, TR:-0.010339459048582, Period:4.4942, Benefit0.673,
Cond:1501, T:51541, TR:-0.0102911149331213, Period:4.5505, Benefit0.673,
Cond:7115, T:51268, TR:-0.0102407483795575, Period:4.507, Benefit0.673,
Cond:3963, T:51375, TR:-0.0102535522459634, Period:4.5274, Benefit0.6725, , T2:3456,397,4169,6493,829,8397,827,6988,1069,6681,6925,1067,7357,1499,4911,4970,2829,7672,4381,2399,3327,5343,1965,6661,6794,7565,129,1501,7115,3963,  #End#
LowScoreRank3 , T0:1961 , T1:
Cond:2112, T:70637, TR:-0.0116451074104926, Period:5.5786, Benefit0.5589,
Cond:4367, T:70441, TR:-0.0125247961448739, Period:5.6251, Benefit0.6065,
Cond:8381, T:67291, TR:-0.0120179132285406, Period:5.7162, Benefit0.6077,
Cond:6159, T:65685, TR:-0.0121594226045152, Period:5.4244, Benefit0.6308,
Cond:1693, T:63901, TR:-0.0107129401046062, Period:5.049, Benefit0.5652,
Cond:5951, T:63824, TR:-0.0108600076704211, Period:5.1595, Benefit0.5744,
Cond:589, T:63791, TR:-0.0106784946559219, Period:4.9478, Benefit0.5642,
Cond:80, T:63548, TR:-0.0109970100901082, Period:4.8319, Benefit0.5849,
Cond:4361, T:63090, TR:-0.0104616742181854, Period:5.3102, Benefit0.5579,
Cond:4365, T:62997, TR:-0.0108278757654957, Period:5.2574, Benefit0.5802,
Cond:5725, T:62843, TR:-0.0105666285392667, Period:5.0649, Benefit0.5663,
Cond:4157, T:61291, TR:-0.0101350667661181, Period:5.0292, Benefit0.5549,
Cond:7305, T:61099, TR:-0.0104398252882475, Period:4.5969, Benefit0.5751,
Cond:5743, T:60790, TR:-0.0101505892191215, Period:4.8745, Benefit0.5605,
Cond:2381, T:60603, TR:-0.0102857489618419, Period:4.8086, Benefit0.5705,
Cond:7674, T:60568, TR:-0.0100425204413913, Period:4.4692, Benefit0.556,
Cond:6439, T:60538, TR:-0.0102116224113159, Period:4.4814, Benefit0.5666,
Cond:8399, T:60506, TR:-0.0113186821691326, Period:5.1651, Benefit0.6345,
Cond:2811, T:60455, TR:-0.0101430266344127, Period:4.9614, Benefit0.5632,
Cond:5196, T:60378, TR:-0.0100052388659058, Period:4.4573, Benefit0.5555,
Cond:1949, T:60256, TR:-0.0105035630718096, Period:4.6854, Benefit0.5872,
Cond:4653, T:60165, TR:-0.0102087562313907, Period:4.6216, Benefit0.57,
Cond:2383, T:60132, TR:-0.0102995963552648, Period:4.9787, Benefit0.5759,
Cond:811, T:59972, TR:-0.0101183950782075, Period:4.8157, Benefit0.5663,
Cond:1945, T:59840, TR:-0.0103115728551147, Period:4.616, Benefit0.5795,
Cond:2377, T:59692, TR:-0.0101781028092165, Period:4.7534, Benefit0.5727,
Cond:1485, T:59534, TR:-0.0105092232707466, Period:4.794, Benefit0.5948,
Cond:7079, T:59476, TR:-0.0100072293012142, Period:4.4896, Benefit0.5642,
Cond:6558, T:59414, TR:-0.00982717402053107, Period:4.5333, Benefit0.5536,
Cond:6479, T:59365, TR:-0.0101650802503896, Period:4.4467, Benefit0.5751, , T2:2112,4367,8381,6159,1693,5951,589,80,4361,4365,5725,4157,7305,5743,2381,7674,6439,8399,2811,5196,1949,4653,2383,811,1945,2377,1485,7079,6558,6479,  #End#
LowScoreRank1 , T0:2998 , T1:
Cond:3456, T:51326, TR:-0.0103248987007096, Period:4.5756, Benefit0.6783,
Cond:397, T:51323, TR:-0.0102314940590993, Period:4.5249, Benefit0.6716,
Cond:129, T:52004, TR:-0.0103386327817089, Period:4.474, Benefit0.6703,
Cond:6493, T:51520, TR:-0.0102343759790418, Period:4.4676, Benefit0.6692,
Cond:8397, T:52346, TR:-0.0103703994101347, Period:4.6788, Benefit0.6681,
Cond:829, T:51709, TR:-0.0102229731746552, Period:4.535, Benefit0.6659,
Cond:6925, T:51806, TR:-0.0102364868250213, Period:4.47, Benefit0.6656,
Cond:7672, T:51223, TR:-0.0101301022344636, Period:4.4609, Benefit0.6656,
Cond:4970, T:51256, TR:-0.0101264355327071, Period:4.48, Benefit0.6649,
Cond:7357, T:51495, TR:-0.0101699986666274, Period:4.4828, Benefit0.6649,
Cond:147, T:51201, TR:-0.0101108816035921, Period:4.4633, Benefit0.6645,
Cond:4169, T:51467, TR:-0.0101579461659618, Period:4.523, Benefit0.6644,
Cond:6988, T:51622, TR:-0.0101806039299386, Period:4.479, Benefit0.664,
Cond:1069, T:51766, TR:-0.0101984327896476, Period:4.5226, Benefit0.6634,
Cond:6681, T:51251, TR:-0.0100992319793932, Period:4.4731, Benefit0.663,
Cond:4911, T:51846, TR:-0.010201784380401, Period:4.4707, Benefit0.6626,
Cond:827, T:51499, TR:-0.0101345724149272, Period:4.5239, Benefit0.6623,
Cond:1862, T:51540, TR:-0.0101392335558168, Period:4.497, Benefit0.6621,
Cond:6794, T:51155, TR:-0.0100693548024951, Period:4.4914, Benefit0.6621,
Cond:2399, T:51491, TR:-0.0101247775978528, Period:4.5479, Benefit0.6617,
Cond:4381, T:52107, TR:-0.0102323216783934, Period:4.5881, Benefit0.6614,
Cond:472, T:51899, TR:-0.0101903908741781, Period:4.4611, Benefit0.6611,
Cond:2829, T:51767, TR:-0.0101664623070444, Period:4.5471, Benefit0.6611,
Cond:7565, T:51879, TR:-0.0101839632309634, Period:4.4875, Benefit0.6609,
Cond:3327, T:51899, TR:-0.0101847846973268, Period:4.4892, Benefit0.6607,
Cond:6661, T:51318, TR:-0.0100781535081845, Period:4.4832, Benefit0.6606,
Cond:7240, T:51186, TR:-0.0100514864681265, Period:4.4739, Benefit0.6604,
Cond:5343, T:51784, TR:-0.0101499663881794, Period:4.4831, Benefit0.6597,
Cond:6453, T:51390, TR:-0.0100745409288953, Period:4.4831, Benefit0.6594,
Cond:6943, T:51685, TR:-0.0101250804458089, Period:4.4712, Benefit0.6592, , T2:3456,397,129,6493,8397,829,6925,7672,4970,7357,147,4169,6988,1069,6681,4911,827,1862,6794,2399,4381,472,2829,7565,3327,6661,7240,5343,6453,6943,  #End#
LowScoreRank3 , T0:2998 , T1:
Cond:2112, T:70859, TR:-0.0115198453844122, Period:5.5568, Benefit0.5506,
Cond:4367, T:70813, TR:-0.0123912441289844, Period:5.6019, Benefit0.5963,
Cond:8381, T:67567, TR:-0.0118938851865, Period:5.6921, Benefit0.5984,
Cond:6159, T:65986, TR:-0.0120240645807693, Period:5.4012, Benefit0.6203,
Cond:1693, T:64332, TR:-0.0105396427831702, Period:5.0272, Benefit0.5514,
Cond:589, T:64213, TR:-0.0105145644255126, Period:4.9261, Benefit0.551,
Cond:5951, T:64138, TR:-0.0107255928695213, Period:5.1357, Benefit0.5638,
Cond:80, T:63782, TR:-0.0108550761593279, Period:4.8129, Benefit0.5745,
Cond:4361, T:63610, TR:-0.0102970390271548, Period:5.2857, Benefit0.5437,
Cond:4365, T:63376, TR:-0.0106739548943386, Period:5.235, Benefit0.5677,
Cond:5725, T:63170, TR:-0.0104431903840924, Period:5.0421, Benefit0.5561,
Cond:7305, T:61605, TR:-0.0102231383238914, Period:4.5732, Benefit0.5573,
Cond:6439, T:61236, TR:-0.0100686276263317, Period:4.4586, Benefit0.5514,
Cond:2811, T:61108, TR:-0.0099647993008784, Period:4.935, Benefit0.5463,
Cond:5743, T:61094, TR:-0.0100073480130987, Period:4.8503, Benefit0.549,
Cond:2381, T:61053, TR:-0.0101317173061154, Period:4.7863, Benefit0.5569,
Cond:1945, T:61049, TR:-0.010084826951487, Period:4.5917, Benefit0.5541,
Cond:8399, T:60796, TR:-0.0111883473418352, Period:5.1412, Benefit0.6235,
Cond:7674, T:60767, TR:-0.00996773499500152, Period:4.4503, Benefit0.5496,
Cond:811, T:60727, TR:-0.00991071622309706, Period:4.7906, Benefit0.5465,
Cond:1949, T:60700, TR:-0.0103355045013976, Period:4.6637, Benefit0.5726,
Cond:2377, T:60536, TR:-0.00992127611947975, Period:4.7286, Benefit0.5489,
Cond:2383, T:60517, TR:-0.0101641152608199, Period:4.954, Benefit0.5639,
Cond:5196, T:60497, TR:-0.00996450301578962, Period:4.4406, Benefit0.5519,
Cond:4653, T:60423, TR:-0.0100889955135564, Period:4.6004, Benefit0.5602,
Cond:7079, T:60049, TR:-0.00982324217062891, Period:4.4667, Benefit0.5474,
Cond:1485, T:59935, TR:-0.0103608010043976, Period:4.7715, Benefit0.5816,
Cond:6479, T:59578, TR:-0.0100601608937604, Period:4.4273, Benefit0.5665,
Cond:4651, T:59525, TR:-0.00974100663832302, Period:4.4891, Benefit0.5472,
Cond:1951, T:59473, TR:-0.00966718532732336, Period:4.7781, Benefit0.5431, , T2:2112,4367,8381,6159,1693,589,5951,80,4361,4365,5725,7305,6439,2811,5743,2381,1945,8399,7674,811,1949,2377,2383,5196,4653,7079,1485,6479,4651,1951,  #End#
LowScoreRank1 , T0:3444 , T1:
Cond:3456, T:51403, TR:-0.0103864068316231, Period:4.5508, Benefit0.6817,
Cond:397, T:51379, TR:-0.0102820357925609, Period:4.4988, Benefit0.6745,
Cond:6493, T:51581, TR:-0.0102692456959392, Period:4.4412, Benefit0.6709,
Cond:8397, T:52403, TR:-0.0104204742417453, Period:4.653, Benefit0.6709,
Cond:829, T:51767, TR:-0.0102727049635726, Period:4.509, Benefit0.6687,
Cond:7672, T:51255, TR:-0.0101691545491438, Period:4.4372, Benefit0.668,
Cond:6925, T:51873, TR:-0.0102739299678397, Period:4.4439, Benefit0.6674,
Cond:6988, T:51678, TR:-0.0102382456594391, Period:4.4541, Benefit0.6674,
Cond:4169, T:51525, TR:-0.0102074672255824, Period:4.4969, Benefit0.6672,
Cond:7357, T:51556, TR:-0.0102131376982338, Period:4.4569, Benefit0.6672,
Cond:4970, T:51306, TR:-0.0101660254758909, Period:4.4547, Benefit0.6671,
Cond:4911, T:51907, TR:-0.0102731434156121, Period:4.4444, Benefit0.6669,
Cond:1069, T:51822, TR:-0.0102492029929809, Period:4.4968, Benefit0.6663,
Cond:6794, T:51213, TR:-0.0101268949855994, Period:4.4655, Benefit0.6655,
Cond:827, T:51558, TR:-0.0101828749794779, Period:4.4978, Benefit0.665,
Cond:6681, T:51307, TR:-0.0101371162953457, Period:4.4471, Benefit0.665,
Cond:2399, T:51549, TR:-0.0101742743185403, Period:4.5218, Benefit0.6645,
Cond:4381, T:52167, TR:-0.0102854715229538, Period:4.5621, Benefit0.6644,
Cond:7240, T:51232, TR:-0.0101123783247571, Period:4.4495, Benefit0.6642,
Cond:3598, T:51211, TR:-0.0101044068207929, Period:4.4457, Benefit0.6639,
Cond:2829, T:51825, TR:-0.0102147631464888, Period:4.5211, Benefit0.6638,
Cond:6661, T:51372, TR:-0.0101267717178874, Period:4.4576, Benefit0.6634,
Cond:5343, T:51842, TR:-0.0102108575085794, Period:4.4567, Benefit0.6633,
Cond:3327, T:51956, TR:-0.0102301893992055, Period:4.4629, Benefit0.6632,
Cond:129, T:52030, TR:-0.0102324065618539, Period:4.4498, Benefit0.6624,
Cond:7565, T:51942, TR:-0.0102164215370257, Period:4.4615, Benefit0.6624,
Cond:1862, T:51585, TR:-0.010150188043586, Period:4.4728, Benefit0.6623,
Cond:6453, T:51442, TR:-0.0101228338897523, Period:4.4577, Benefit0.6622,
Cond:6943, T:51750, TR:-0.0101731629710602, Period:4.4451, Benefit0.6618,
Cond:1067, T:51794, TR:-0.0101755526549165, Period:4.4747, Benefit0.6614, , T2:3456,397,6493,8397,829,7672,6925,6988,4169,7357,4970,4911,1069,6794,827,6681,2399,4381,7240,3598,2829,6661,5343,3327,129,7565,1862,6453,6943,1067,  #End#
LowScoreRank3 , T0:3444 , T1:
Cond:2112, T:71027, TR:-0.0115859697485707, Period:5.5367, Benefit0.5527,
Cond:4367, T:70876, TR:-0.0124382070710992, Period:5.5821, Benefit0.5982,
Cond:8381, T:67630, TR:-0.0119373273870868, Period:5.6715, Benefit0.6002,
Cond:6159, T:66048, TR:-0.0120740829583733, Period:5.3802, Benefit0.6225,
Cond:1693, T:64392, TR:-0.0105941437737062, Period:5.0058, Benefit0.554,
Cond:589, T:64272, TR:-0.0105635955513093, Period:4.9047, Benefit0.5533,
Cond:5951, T:64205, TR:-0.0107760347025386, Period:5.1147, Benefit0.5661,
Cond:80, T:63807, TR:-0.0108970718134182, Period:4.7941, Benefit0.5767,
Cond:4361, T:63668, TR:-0.0103471403834723, Period:5.2638, Benefit0.5461,
Cond:4365, T:63436, TR:-0.0107228801777079, Period:5.2135, Benefit0.57,
Cond:5725, T:63247, TR:-0.0104858458077835, Period:5.0202, Benefit0.5579,
Cond:7305, T:61680, TR:-0.0102547044578613, Period:4.5527, Benefit0.5585,
Cond:3614, T:61581, TR:-0.0100227009499622, Period:4.3772, Benefit0.5455,
Cond:6439, T:61274, TR:-0.0101175359057746, Period:4.4379, Benefit0.554,
Cond:2811, T:61168, TR:-0.0100052991679386, Period:4.9125, Benefit0.5482,
Cond:5743, T:61163, TR:-0.0100542194506735, Period:4.8283, Benefit0.5512,
Cond:2381, T:61114, TR:-0.0101824442959663, Period:4.7642, Benefit0.5594,
Cond:1945, T:61104, TR:-0.0101312751082766, Period:4.5696, Benefit0.5564,
Cond:8399, T:60856, TR:-0.0112365467611254, Period:5.1186, Benefit0.6258,
Cond:811, T:60783, TR:-0.0099586407666489, Period:4.7683, Benefit0.5489,
Cond:7674, T:60783, TR:-0.0100277478272804, Period:4.4324, Benefit0.5531,
Cond:1949, T:60767, TR:-0.0103839113823891, Period:4.641, Benefit0.5749,
Cond:2377, T:60593, TR:-0.00997418310675412, Period:4.7063, Benefit0.5516,
Cond:2383, T:60575, TR:-0.0102124803602079, Period:4.9315, Benefit0.5663,
Cond:5196, T:60502, TR:-0.0100618862662896, Period:4.4241, Benefit0.5578,
Cond:4653, T:60496, TR:-0.0101199143652944, Period:4.5789, Benefit0.5614,
Cond:7079, T:60105, TR:-0.00985120878125118, Period:4.4456, Benefit0.5486,
Cond:1485, T:59995, TR:-0.0104110068556826, Period:4.749, Benefit0.5841,
Cond:6558, T:59758, TR:-0.00975186695345872, Period:4.4893, Benefit0.5457,
Cond:6479, T:59636, TR:-0.010078893615317, Period:4.405, Benefit0.5671, , T2:2112,4367,8381,6159,1693,589,5951,80,4361,4365,5725,7305,3614,6439,2811,5743,2381,1945,8399,811,7674,1949,2377,2383,5196,4653,7079,1485,6558,6479,  #End#
LowScoreRank1 , T0:3890 , T1:
Cond:3456, T:50810, TR:-0.0103414511559698, Period:4.5734, Benefit0.6865,
Cond:397, T:50766, TR:-0.0102249019016663, Period:4.5206, Benefit0.6786,
Cond:6493, T:50976, TR:-0.0102323017654317, Period:4.4623, Benefit0.6763,
Cond:8397, T:51791, TR:-0.0103610206413824, Period:4.676, Benefit0.6747,
Cond:829, T:51154, TR:-0.0102141938001994, Period:4.5306, Benefit0.6726,
Cond:4970, T:50703, TR:-0.010120107843484, Period:4.4752, Benefit0.6718,
Cond:7357, T:50948, TR:-0.0101610874782461, Period:4.4778, Benefit0.6715,
Cond:6925, T:51263, TR:-0.0102162957547245, Period:4.4647, Benefit0.6713,
Cond:4169, T:50912, TR:-0.0101503386449257, Period:4.5186, Benefit0.6712,
Cond:7672, T:50682, TR:-0.0101052960452502, Period:4.456, Benefit0.671,
Cond:6988, T:51067, TR:-0.0101692009064158, Period:4.4749, Benefit0.6705,
Cond:1069, T:51209, TR:-0.0101911499928314, Period:4.5183, Benefit0.6702,
Cond:4911, T:51300, TR:-0.0102023271585971, Period:4.466, Benefit0.6698,
Cond:6681, T:50706, TR:-0.0100850690927547, Period:4.4678, Benefit0.6692,
Cond:827, T:50946, TR:-0.0101277085028375, Period:4.5193, Benefit0.6691,
Cond:1862, T:51178, TR:-0.0101619644507612, Period:4.4841, Benefit0.6685,
Cond:2399, T:50936, TR:-0.0101148732655678, Period:4.5437, Benefit0.6683,
Cond:4381, T:51554, TR:-0.010226692835533, Period:4.5842, Benefit0.6682,
Cond:129, T:51478, TR:-0.0102086003572925, Period:4.4674, Benefit0.6679,
Cond:6661, T:50763, TR:-0.0100763358646568, Period:4.4787, Benefit0.6678,
Cond:2829, T:51212, TR:-0.0101571327368445, Period:4.5429, Benefit0.6677,
Cond:6453, T:50829, TR:-0.0100815532082795, Period:4.4789, Benefit0.6673,
Cond:3327, T:51347, TR:-0.0101735231442142, Period:4.4846, Benefit0.6671,
Cond:6794, T:50615, TR:-0.0100383209090264, Period:4.4861, Benefit0.667,
Cond:7565, T:51333, TR:-0.0101681908513257, Period:4.483, Benefit0.6669,
Cond:5343, T:51235, TR:-0.0101488920073733, Period:4.4785, Benefit0.6668,
Cond:7240, T:50649, TR:-0.0100418005737754, Period:4.4695, Benefit0.6668,
Cond:147, T:50613, TR:-0.0100270246366831, Period:4.4598, Benefit0.6662,
Cond:1067, T:51185, TR:-0.0101259332305713, Period:4.4956, Benefit0.6658,
Cond:6943, T:51140, TR:-0.0101135785315732, Period:4.4658, Benefit0.6655, , T2:3456,397,6493,8397,829,4970,7357,6925,4169,7672,6988,1069,4911,6681,827,1862,2399,4381,129,6661,2829,6453,3327,6794,7565,5343,7240,147,1067,6943,  #End#
LowScoreRank3 , T0:3890 , T1:
Cond:2112, T:70481, TR:-0.0115406060766935, Period:5.5599, Benefit0.5547,
Cond:4367, T:70264, TR:-0.0123787356293264, Period:5.6072, Benefit0.6004,
Cond:8381, T:67019, TR:-0.0118777069596056, Period:5.6985, Benefit0.6025,
Cond:6159, T:65436, TR:-0.0120131607778937, Period:5.4053, Benefit0.625,
Cond:1693, T:63779, TR:-0.0105349370885211, Period:5.028, Benefit0.556,
Cond:589, T:63659, TR:-0.0105044470936087, Period:4.9262, Benefit0.5553,
Cond:5951, T:63593, TR:-0.0107182924606635, Period:5.1379, Benefit0.5683,
Cond:80, T:63241, TR:-0.010856661361534, Period:4.8135, Benefit0.5796,
Cond:4365, T:62824, TR:-0.0106623524827048, Period:5.2379, Benefit0.5721,
Cond:5725, T:62643, TR:-0.0104335843908432, Period:5.0425, Benefit0.5603,
Cond:7305, T:61101, TR:-0.0102234477729483, Period:4.5709, Benefit0.562,
Cond:6439, T:60693, TR:-0.0100863175075043, Period:4.455, Benefit0.5575,
Cond:2811, T:60555, TR:-0.00995037627435116, Period:4.9349, Benefit0.5505,
Cond:5743, T:60553, TR:-0.0100025233599678, Period:4.8501, Benefit0.5537,
Cond:2381, T:60502, TR:-0.010125755587941, Period:4.785, Benefit0.5617,
Cond:1945, T:60497, TR:-0.0100840432428745, Period:4.5883, Benefit0.5592,
Cond:8399, T:60243, TR:-0.011178138221203, Period:5.1432, Benefit0.6287,
Cond:7674, T:60228, TR:-0.00990403323717649, Period:4.4473, Benefit0.5507,
Cond:811, T:60172, TR:-0.00990042207109942, Period:4.7892, Benefit0.551,
Cond:1949, T:60154, TR:-0.0103242521601025, Period:4.661, Benefit0.5772,
Cond:2377, T:59982, TR:-0.00991865711044184, Period:4.7267, Benefit0.5539,
Cond:5196, T:59969, TR:-0.00989882025279594, Period:4.4364, Benefit0.5528,
Cond:2383, T:59963, TR:-0.0101559394152463, Period:4.9542, Benefit0.5687,
Cond:4653, T:59923, TR:-0.0100831813016802, Period:4.598, Benefit0.5646,
Cond:7079, T:59531, TR:-0.00982245933935982, Period:4.4625, Benefit0.5522,
Cond:1485, T:59385, TR:-0.0103498334953772, Period:4.7697, Benefit0.5864,
Cond:6479, T:59071, TR:-0.0100098368917275, Period:4.4224, Benefit0.5683,
Cond:4651, T:59021, TR:-0.00972095050333278, Period:4.4841, Benefit0.5507,
Cond:1051, T:58903, TR:-0.00996123811500215, Period:4.6173, Benefit0.5669,
Cond:992, T:58720, TR:-0.0106587009436265, Period:5.1262, Benefit0.6126, , T2:2112,4367,8381,6159,1693,589,5951,80,4365,5725,7305,6439,2811,5743,2381,1945,8399,7674,811,1949,2377,5196,2383,4653,7079,1485,6479,4651,1051,992,  #End#
LowScoreRank1 , T0:3206 , T1:
Cond:3456, T:50623, TR:-0.0103322386212858, Period:4.5815, Benefit0.6884,
Cond:397, T:50562, TR:-0.0102224563596489, Period:4.5289, Benefit0.6812,
Cond:6493, T:50773, TR:-0.0102248329173543, Period:4.4703, Benefit0.6785,
Cond:8397, T:51587, TR:-0.0103595013883567, Period:4.6848, Benefit0.6773,
Cond:829, T:50950, TR:-0.0102123514099596, Period:4.5389, Benefit0.6752,
Cond:6925, T:51065, TR:-0.0102212243874637, Period:4.4725, Benefit0.6743,
Cond:7672, T:50495, TR:-0.010113169892703, Period:4.4633, Benefit0.6741,
Cond:1862, T:50938, TR:-0.0101922527616478, Period:4.4946, Benefit0.6739,
Cond:4169, T:50708, TR:-0.0101484071316227, Period:4.5268, Benefit0.6738,
Cond:7357, T:50748, TR:-0.0101544228730139, Period:4.4858, Benefit0.6737,
Cond:4911, T:51103, TR:-0.010218589164646, Period:4.4738, Benefit0.6736,
Cond:4970, T:50513, TR:-0.0101069493358255, Period:4.4827, Benefit0.6734,
Cond:6988, T:50867, TR:-0.0101722733690081, Period:4.4828, Benefit0.6734,
Cond:1069, T:51005, TR:-0.01018810256917, Period:4.5266, Benefit0.6727,
Cond:827, T:50742, TR:-0.0101245478663896, Period:4.5276, Benefit0.6716,
Cond:6681, T:50506, TR:-0.0100797528957612, Period:4.4756, Benefit0.6715,
Cond:2399, T:50732, TR:-0.0101131202514798, Period:4.5521, Benefit0.6709,
Cond:4381, T:51350, TR:-0.0102253727335174, Period:4.5927, Benefit0.6708,
Cond:129, T:51258, TR:-0.0102029197403195, Period:4.4763, Benefit0.6704,
Cond:2829, T:51008, TR:-0.0101556040539132, Period:4.5512, Benefit0.6703,
Cond:6661, T:50568, TR:-0.0100706840049909, Period:4.4865, Benefit0.67,
Cond:7565, T:51136, TR:-0.0101749783414107, Period:4.4907, Benefit0.67,
Cond:5343, T:51038, TR:-0.0101556036403222, Period:4.4865, Benefit0.6699,
Cond:6794, T:50416, TR:-0.0100414187271054, Period:4.4941, Benefit0.6699,
Cond:3327, T:51150, TR:-0.0101706441906227, Period:4.4924, Benefit0.6695,
Cond:3598, T:50429, TR:-0.0100342756933067, Period:4.4731, Benefit0.6692,
Cond:6453, T:50640, TR:-0.0100661306057967, Period:4.4864, Benefit0.6687,
Cond:7115, T:50813, TR:-0.0100909735396684, Period:4.4845, Benefit0.6682,
Cond:6943, T:50936, TR:-0.0101107477239164, Period:4.474, Benefit0.668,
Cond:1067, T:50978, TR:-0.0101170607455693, Period:4.504, Benefit0.6679, , T2:3456,397,6493,8397,829,6925,7672,1862,4169,7357,4911,4970,6988,1069,827,6681,2399,4381,129,2829,6661,7565,5343,6794,3327,3598,6453,7115,6943,1067,  #End#
LowScoreRank3 , T0:3206 , T1:
Cond:2112, T:70285, TR:-0.0115352832048843, Period:5.5687, Benefit0.556,
Cond:4367, T:70060, TR:-0.0123771331487565, Period:5.6164, Benefit0.6021,
Cond:8381, T:66815, TR:-0.0118745216759656, Period:5.7084, Benefit0.6042,
Cond:6159, T:65232, TR:-0.0120115319300943, Period:5.4144, Benefit0.6269,
Cond:1693, T:63575, TR:-0.0105329064746915, Period:5.0362, Benefit0.5577,
Cond:589, T:63456, TR:-0.0105025553869368, Period:4.9338, Benefit0.557,
Cond:5951, T:63389, TR:-0.0107154864359906, Period:5.1465, Benefit0.57,
Cond:80, T:63076, TR:-0.0108217290792321, Period:4.8195, Benefit0.5791,
Cond:4365, T:62620, TR:-0.0106606820938686, Period:5.247, Benefit0.5739,
Cond:5725, T:62438, TR:-0.0104323353590897, Period:5.051, Benefit0.5621,
Cond:7305, T:60913, TR:-0.0102207089310073, Period:4.5771, Benefit0.5636,
Cond:6439, T:60520, TR:-0.0100728439012269, Period:4.4606, Benefit0.5583,
Cond:5743, T:60352, TR:-0.00999635852099345, Period:4.858, Benefit0.5552,
Cond:2811, T:60351, TR:-0.00994883343424815, Period:4.9432, Benefit0.5523,
Cond:2381, T:60298, TR:-0.0101235588466156, Period:4.7929, Benefit0.5635,
Cond:1945, T:60289, TR:-0.0100732070458564, Period:4.5958, Benefit0.5605,
Cond:7674, T:60073, TR:-0.00992116235165352, Period:4.4533, Benefit0.5532,
Cond:8399, T:60039, TR:-0.0111769329642066, Period:5.1523, Benefit0.6308,
Cond:811, T:59968, TR:-0.00989542199096164, Period:4.7972, Benefit0.5526,
Cond:1949, T:59951, TR:-0.0103228013840568, Period:4.6682, Benefit0.5791,
Cond:5196, T:59796, TR:-0.00993558728799569, Period:4.4439, Benefit0.5567,
Cond:2377, T:59779, TR:-0.00991518471102461, Period:4.7344, Benefit0.5556,
Cond:2383, T:59759, TR:-0.0101514750729813, Period:4.9627, Benefit0.5704,
Cond:4653, T:59727, TR:-0.0100914913355555, Period:4.6051, Benefit0.567,
Cond:7079, T:59337, TR:-0.00981382888767001, Period:4.4692, Benefit0.5535,
Cond:1485, T:59179, TR:-0.0103501975486693, Period:4.778, Benefit0.5885,
Cond:6479, T:58877, TR:-0.0100272716143278, Period:4.429, Benefit0.5713,
Cond:4651, T:58825, TR:-0.00970555573406391, Period:4.4909, Benefit0.5516,
Cond:1051, T:58697, TR:-0.00995767260061647, Period:4.625, Benefit0.5687,
Cond:992, T:58524, TR:-0.010657318312438, Period:5.1351, Benefit0.6146, , T2:2112,4367,8381,6159,1693,589,5951,80,4365,5725,7305,6439,5743,2811,2381,1945,7674,8399,811,1949,5196,2377,2383,4653,7079,1485,6479,4651,1051,992,  #End#
End , T0:01:47:27.1185370  #End#








/////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////






 LowScoreRank1 , T0:3458 , T1:
Cond:621, T:226880, TR:-0.0395684792009991, Period:6.4486, Benefit0.6002,
Cond:6584, T:227330, TR:-0.0396436481892846, Period:6.4438, Benefit0.6001,
Cond:7581, T:226803, TR:-0.0395480878233307, Period:6.4423, Benefit0.6001,
Cond:7283, T:226914, TR:-0.0395489174041064, Period:6.439, Benefit0.5998,
Cond:1279, T:226772, TR:-0.039516757234035, Period:6.4536, Benefit0.5997,
Cond:395, T:228275, TR:-0.0397762380177457, Period:6.4398, Benefit0.5995,
Cond:7373, T:226660, TR:-0.0394836175551113, Period:6.4415, Benefit0.5995,
Cond:1497, T:226808, TR:-0.039503993741635, Period:6.4459, Benefit0.5994,
Cond:3456, T:226697, TR:-0.0394838932472155, Period:6.4638, Benefit0.5994,
Cond:6570, T:226660, TR:-0.0394643449801685, Period:6.4472, Benefit0.5992,
Cond:825, T:226770, TR:-0.0394714023099014, Period:6.4462, Benefit0.599,
Cond:5041, T:227059, TR:-0.0395237059617719, Period:6.4335, Benefit0.599,
Cond:2706, T:227342, TR:-0.039568487262526, Period:6.4486, Benefit0.5989,
Cond:2722, T:230662, TR:-0.0401699573670686, Period:6.4521, Benefit0.5989,
Cond:6173, T:226892, TR:-0.0394870498169781, Period:6.4576, Benefit0.5989,
Cond:7095, T:227385, TR:-0.0395762701504549, Period:6.4396, Benefit0.5989,
Cond:1495, T:227412, TR:-0.0395747102975103, Period:6.4321, Benefit0.5988,
Cond:3343, T:226833, TR:-0.039469944593236, Period:6.4417, Benefit0.5988,
Cond:383, T:226665, TR:-0.0394331283476857, Period:6.4538, Benefit0.5987,
Cond:4538, T:227636, TR:-0.0396087970038982, Period:6.4329, Benefit0.5987,
Cond:6941, T:226974, TR:-0.0394890206535764, Period:6.4368, Benefit0.5987,
Cond:2393, T:227392, TR:-0.0395453057700453, Period:6.4446, Benefit0.5984,
Cond:5323, T:226787, TR:-0.0394359106226894, Period:6.4429, Benefit0.5984,
Cond:3093, T:226742, TR:-0.0394213486274937, Period:6.4402, Benefit0.5983,
Cond:3164, T:234385, TR:-0.0408058402858002, Period:6.3776, Benefit0.5983,
Cond:6717, T:228087, TR:-0.0396645536453878, Period:6.4263, Benefit0.5983,
Cond:6905, T:226719, TR:-0.0394171913438338, Period:6.4408, Benefit0.5983,
Cond:1041, T:227185, TR:-0.0394949920733078, Period:6.433, Benefit0.5982,
Cond:1396, T:226968, TR:-0.03945576785855, Period:6.4503, Benefit0.5982,
Cond:3283, T:227111, TR:-0.0394816155316028, Period:6.4359, Benefit0.5982, , T2:621,6584,7581,7283,1279,395,7373,1497,3456,6570,825,5041,2706,2722,6173,7095,1495,3343,383,4538,6941,2393,5323,3093,3164,6717,6905,1041,1396,3283,  #End#
LowScoreRank3 , T0:3458 , T1:
Cond:6627, T:318445, TR:-0.0461531871293585, Period:6.1066, Benefit0.4873,
Cond:5420, T:312860, TR:-0.0452058061246487, Period:6.1813, Benefit0.4864,
Cond:3656, T:312093, TR:-0.0450511122893534, Period:6.2996, Benefit0.486,
Cond:8381, T:311903, TR:-0.0462176065440927, Period:7.921, Benefit0.4992,
Cond:1838, T:309244, TR:-0.0448633152347226, Period:6.239, Benefit0.4888,
Cond:8146, T:308552, TR:-0.046036445461177, Period:5.8338, Benefit0.5031,
Cond:6378, T:306062, TR:-0.0441490799855255, Period:7.0222, Benefit0.4863,
Cond:4988, T:305927, TR:-0.0446516544360197, Period:6.3769, Benefit0.4922,
Cond:728, T:304813, TR:-0.0442397652258372, Period:6.2091, Benefit0.4895,
Cond:478, T:304341, TR:-0.0449870228708798, Period:6.1285, Benefit0.4988,
Cond:6072, T:304297, TR:-0.0434191793725651, Period:6.0305, Benefit0.4811,
Cond:30, T:304040, TR:-0.044948401647596, Period:6.1264, Benefit0.4989,
Cond:2074, T:302546, TR:-0.0431882013451746, Period:6.2302, Benefit0.4815,
Cond:2754, T:302451, TR:-0.0446718112381551, Period:6.419, Benefit0.4986,
Cond:2270, T:300435, TR:-0.0441286375264839, Period:6.1253, Benefit0.496,
Cond:3838, T:298757, TR:-0.0442208122821804, Period:6.1314, Benefit0.5001,
Cond:5002, T:298056, TR:-0.0431533630615333, Period:6.3333, Benefit0.489,
Cond:1150, T:297795, TR:-0.0441727132817736, Period:6.1384, Benefit0.5013,
Cond:3642, T:297389, TR:-0.0431969249234345, Period:6.2194, Benefit0.4907,
Cond:3182, T:297233, TR:-0.0443247518857445, Period:6.2596, Benefit0.5041,
Cond:4060, T:296842, TR:-0.0434300838369899, Period:6.0718, Benefit0.4944,
Cond:4629, T:296469, TR:-0.0429606026202161, Period:6.187, Benefit0.4896,
Cond:2728, T:295749, TR:-0.0436534335101501, Period:6.1297, Benefit0.499,
Cond:6796, T:295625, TR:-0.0427622998183716, Period:6.4454, Benefit0.4888,
Cond:4542, T:295598, TR:-0.0429717772651482, Period:6.405, Benefit0.4913,
Cond:968, T:295559, TR:-0.0447343266571793, Period:6.4224, Benefit0.512,
Cond:4609, T:295511, TR:-0.0423005819351761, Period:6.316, Benefit0.4836,
Cond:4367, T:295464, TR:-0.0432670937607496, Period:7.3151, Benefit0.495,
Cond:7228, T:294609, TR:-0.0445559362247589, Period:6.3284, Benefit0.5117,
Cond:6853, T:293393, TR:-0.0445639489468851, Period:6.1417, Benefit0.5141, , T2:6627,5420,3656,8381,1838,8146,6378,4988,728,478,6072,30,2074,2754,2270,3838,5002,1150,3642,3182,4060,4629,2728,6796,4542,968,4609,4367,7228,6853,  #End#
LowScoreRank1 , T0:2619 , T1:
Cond:621, T:148025, TR:-0.030551465258018, Period:5.6601, Benefit0.7213,
Cond:7095, T:148653, TR:-0.0306617843139919, Period:5.6486, Benefit0.7208,
Cond:7581, T:148181, TR:-0.030555225512483, Period:5.6487, Benefit0.7206,
Cond:4580, T:148365, TR:-0.0305812070779568, Period:5.6764, Benefit0.7203,
Cond:395, T:148908, TR:-0.0306860182818642, Period:5.6555, Benefit0.7201,
Cond:1279, T:147994, TR:-0.0304957662807758, Period:5.6668, Benefit0.7201,
Cond:6584, T:148918, TR:-0.0306881003469803, Period:5.6532, Benefit0.7201,
Cond:3755, T:147914, TR:-0.0304709170550811, Period:5.6565, Benefit0.7199,
Cond:7373, T:147991, TR:-0.0304869359745709, Period:5.6468, Benefit0.7199,
Cond:1497, T:147983, TR:-0.0304729633239375, Period:5.6552, Benefit0.7196,
Cond:2187, T:148048, TR:-0.0304823759847268, Period:5.6578, Benefit0.7195,
Cond:5963, T:147949, TR:-0.0304617911352486, Period:5.6625, Benefit0.7195,
Cond:1725, T:147899, TR:-0.0304472948286201, Period:5.6608, Benefit0.7194,
Cond:2393, T:148328, TR:-0.0305241509583376, Period:5.6561, Benefit0.7191,
Cond:3343, T:148188, TR:-0.030490944357415, Period:5.648, Benefit0.719,
Cond:4893, T:147936, TR:-0.0304385810815232, Period:5.6512, Benefit0.719,
Cond:7283, T:148087, TR:-0.0304699566102796, Period:5.6455, Benefit0.719,
Cond:5041, T:148262, TR:-0.0305022114398396, Period:5.6379, Benefit0.7189,
Cond:6570, T:148014, TR:-0.0304506843137211, Period:5.6571, Benefit0.7189,
Cond:7997, T:147940, TR:-0.0304353106567162, Period:5.66, Benefit0.7189,
Cond:825, T:147969, TR:-0.0304372330253233, Period:5.6554, Benefit0.7188,
Cond:383, T:147898, TR:-0.030418384711011, Period:5.6665, Benefit0.7187,
Cond:3456, T:150154, TR:-0.0308830505102737, Period:5.719, Benefit0.7186,
Cond:6905, T:148032, TR:-0.0304421113890388, Period:5.6473, Benefit0.7186,
Cond:7042, T:147902, TR:-0.0304151150190852, Period:5.6642, Benefit0.7186,
Cond:2706, T:148662, TR:-0.0305647227770654, Period:5.6614, Benefit0.7184,
Cond:7615, T:147927, TR:-0.0304080030108465, Period:5.6476, Benefit0.7183,
Cond:1981, T:148364, TR:-0.0304946097187262, Period:5.6566, Benefit0.7182,
Cond:3093, T:147986, TR:-0.0304161471712551, Period:5.6456, Benefit0.7182,
Cond:6173, T:148309, TR:-0.0304831921880498, Period:5.6765, Benefit0.7182, , T2:621,7095,7581,4580,395,1279,6584,3755,7373,1497,2187,5963,1725,2393,3343,4893,7283,5041,6570,7997,825,383,3456,6905,7042,2706,7615,1981,3093,6173,  #End#
LowScoreRank3 , T0:2619 , T1:
Cond:3236, T:241780, TR:-0.0408022251625769, Period:6.6415, Benefit0.5787,
Cond:504, T:221521, TR:-0.0382533349178474, Period:5.5381, Benefit0.5947,
Cond:56, T:220995, TR:-0.0374211998989216, Period:5.522, Benefit0.5829,
Cond:4114, T:218549, TR:-0.0382405177821005, Period:5.229, Benefit0.6031,
Cond:2296, T:215894, TR:-0.0367345018671984, Period:5.5271, Benefit0.5863,
Cond:254, T:214881, TR:-0.0362952024169457, Period:5.4489, Benefit0.582,
Cond:1864, T:213579, TR:-0.037249970824896, Period:5.7229, Benefit0.6016,
Cond:3000, T:213060, TR:-0.037228543703728, Period:5.7776, Benefit0.6028,
Cond:1176, T:212221, TR:-0.0374304030248415, Period:5.537, Benefit0.6087,
Cond:5670, T:211101, TR:-0.0356476079197807, Period:5.2702, Benefit0.5822,
Cond:3864, T:208459, TR:-0.0354400808283943, Period:5.4943, Benefit0.5865,
Cond:2046, T:207813, TR:-0.0351862026329878, Period:5.4401, Benefit0.5841,
Cond:1608, T:206847, TR:-0.0361252741956895, Period:5.3722, Benefit0.6031,
Cond:490, T:204710, TR:-0.0342989494132122, Period:5.3842, Benefit0.5781,
Cond:42, T:204108, TR:-0.0343330813787852, Period:5.3856, Benefit0.5805,
Cond:2112, T:204105, TR:-0.0349131272900244, Period:6.5475, Benefit0.5906,
Cond:1850, T:204078, TR:-0.0350463077789619, Period:5.5438, Benefit0.593,
Cond:6159, T:202582, TR:-0.0339434988199208, Period:6.8727, Benefit0.5783,
Cond:3680, T:202230, TR:-0.0341333011223152, Period:6.3825, Benefit0.5827,
Cond:3614, T:201603, TR:-0.0349322505784088, Period:5.4725, Benefit0.5987,
Cond:3404, T:201060, TR:-0.0350730536700446, Period:5.5398, Benefit0.6029,
Cond:4072, T:200261, TR:-0.0340282412158807, Period:5.3807, Benefit0.5869,
Cond:28, T:200231, TR:-0.0335442023014892, Period:5.3669, Benefit0.5784,
Cond:8104, T:200227, TR:-0.0345912153689004, Period:5.4418, Benefit0.597,
Cond:476, T:199829, TR:-0.0338010214272233, Period:5.3641, Benefit0.5842,
Cond:7674, T:199641, TR:-0.0344421890001955, Period:5.628, Benefit0.5962,
Cond:2546, T:198916, TR:-0.0338549155454067, Period:5.457, Benefit0.588,
Cond:1162, T:198756, TR:-0.0334692335859488, Period:5.3941, Benefit0.5816,
Cond:6439, T:198273, TR:-0.0344839348368799, Period:5.6174, Benefit0.6013,
Cond:926, T:197728, TR:-0.0350757907385615, Period:5.4582, Benefit0.6137, , T2:3236,504,56,4114,2296,254,1864,3000,1176,5670,3864,2046,1608,490,42,2112,1850,6159,3680,3614,3404,4072,28,8104,476,7674,2546,1162,6439,926,  #End#
LowScoreRank1 , T0:7599 , T1:
Cond:7149, T:148571, TR:-0.0305993804567821, Period:5.6121, Benefit0.7197,
Cond:621, T:148631, TR:-0.030595377845438, Period:5.6262, Benefit0.7193,
Cond:5135, T:148457, TR:-0.030559197514217, Period:5.615, Benefit0.7193,
Cond:1279, T:148512, TR:-0.0305459251957784, Period:5.6335, Benefit0.7187,
Cond:7283, T:148632, TR:-0.0305708567483045, Period:5.6123, Benefit0.7187,
Cond:6584, T:149426, TR:-0.0307275734669085, Period:5.6202, Benefit0.7185,
Cond:4580, T:148870, TR:-0.0306079241001704, Period:5.6431, Benefit0.7184,
Cond:1497, T:148554, TR:-0.0305340547769867, Period:5.6218, Benefit0.7182,
Cond:3119, T:149054, TR:-0.0306296100406231, Period:5.6147, Benefit0.718,
Cond:825, T:148517, TR:-0.0305016639262372, Period:5.622, Benefit0.7176,
Cond:6570, T:148525, TR:-0.0304992050246772, Period:5.6238, Benefit0.7175,
Cond:7095, T:149316, TR:-0.0306633031759195, Period:5.6144, Benefit0.7175,
Cond:8031, T:148561, TR:-0.0305066718978461, Period:5.6206, Benefit0.7175,
Cond:395, T:150034, TR:-0.0308039953462722, Period:5.6203, Benefit0.7173,
Cond:5041, T:148800, TR:-0.0305479947045079, Period:5.6045, Benefit0.7173,
Cond:7042, T:148417, TR:-0.0304603441016684, Period:5.6308, Benefit0.7171,
Cond:2706, T:149166, TR:-0.0306114945231466, Period:5.6282, Benefit0.717,
Cond:3456, T:150650, TR:-0.0309151345894109, Period:5.686, Benefit0.7169,
Cond:3093, T:148504, TR:-0.0304660247394757, Period:5.6125, Benefit0.7168,
Cond:6905, T:148549, TR:-0.0304753491512302, Period:5.614, Benefit0.7168,
Cond:8205, T:148452, TR:-0.0304552501534757, Period:5.6306, Benefit0.7168,
Cond:1495, T:149162, TR:-0.0305982547283153, Period:5.6043, Benefit0.7167,
Cond:6173, T:148832, TR:-0.0305298673930008, Period:5.643, Benefit0.7167,
Cond:2054, T:148575, TR:-0.0304724969732228, Period:5.6157, Benefit0.7166,
Cond:5323, T:148665, TR:-0.0304828966477651, Period:5.6164, Benefit0.7164,
Cond:2393, T:149136, TR:-0.030576321939125, Period:5.6231, Benefit0.7163,
Cond:4746, T:148590, TR:-0.0304632434586623, Period:5.6177, Benefit0.7163,
Cond:7167, T:148574, TR:-0.0304599303199501, Period:5.6148, Benefit0.7163,
Cond:1723, T:148504, TR:-0.0304413178725936, Period:5.6258, Benefit0.7162,
Cond:1725, T:148517, TR:-0.0304440093157411, Period:5.627, Benefit0.7162, , T2:7149,621,5135,1279,7283,6584,4580,1497,3119,825,6570,7095,8031,395,5041,7042,2706,3456,3093,6905,8205,1495,6173,2054,5323,2393,4746,7167,1723,1725,  #End#
LowScoreRank3 , T0:7599 , T1:
Cond:3236, T:241926, TR:-0.0407453318660047, Period:6.622, Benefit0.5775,
Cond:3670, T:223871, TR:-0.037558721821346, Period:5.5047, Benefit0.5771,
Cond:504, T:221221, TR:-0.0382183797932175, Period:5.5204, Benefit0.595,
Cond:56, T:220750, TR:-0.0373906892751156, Period:5.5036, Benefit0.5831,
Cond:4114, T:218250, TR:-0.0381739542978129, Period:5.2134, Benefit0.6029,
Cond:2296, T:215527, TR:-0.0366577198673545, Period:5.51, Benefit0.5861,
Cond:254, T:215160, TR:-0.0363317793614534, Period:5.4284, Benefit0.5818,
Cond:1864, T:213410, TR:-0.0371953806921182, Period:5.7033, Benefit0.6012,
Cond:3000, T:212859, TR:-0.0370900619707303, Period:5.7604, Benefit0.6011,
Cond:1176, T:211965, TR:-0.0374076468349139, Period:5.5185, Benefit0.6091,
Cond:5670, T:210627, TR:-0.0355530930533011, Period:5.2537, Benefit0.582,
Cond:2046, T:208243, TR:-0.0352084417946774, Period:5.418, Benefit0.5832,
Cond:3864, T:208045, TR:-0.0353616201098386, Period:5.4763, Benefit0.5864,
Cond:1608, T:206504, TR:-0.0360925826001823, Period:5.3538, Benefit0.6036,
Cond:2112, T:204591, TR:-0.0348835008448594, Period:6.5222, Benefit0.5886,
Cond:490, T:204234, TR:-0.0342283553082645, Period:5.3667, Benefit0.5783,
Cond:1850, T:203687, TR:-0.0349714218583639, Period:5.5258, Benefit0.5929,
Cond:42, T:203631, TR:-0.0342447637333844, Period:5.368, Benefit0.5804,
Cond:6159, T:202915, TR:-0.0340008971165587, Period:6.848, Benefit0.5783,
Cond:3680, T:202597, TR:-0.0341285890680629, Period:6.3582, Benefit0.5815,
Cond:3614, T:202150, TR:-0.0349956684178053, Period:5.4487, Benefit0.5981,
Cond:3404, T:201256, TR:-0.0351648749115693, Period:5.5185, Benefit0.6039,
Cond:8104, T:200764, TR:-0.034873053100597, Period:5.4216, Benefit0.6003,
Cond:4072, T:200015, TR:-0.0340246580064368, Period:5.3619, Benefit0.5876,
Cond:28, T:200011, TR:-0.033562594868174, Period:5.3494, Benefit0.5794,
Cond:7674, T:199990, TR:-0.0345828626300179, Period:5.6073, Benefit0.5976,
Cond:476, T:199630, TR:-0.0338394256368599, Period:5.3464, Benefit0.5855,
Cond:6439, T:199343, TR:-0.0344902508651984, Period:5.5989, Benefit0.598,
Cond:2546, T:199203, TR:-0.0338154709607128, Period:5.4352, Benefit0.5864,
Cond:1162, T:198358, TR:-0.0333725166319512, Period:5.3762, Benefit0.5811, , T2:3236,3670,504,56,4114,2296,254,1864,3000,1176,5670,2046,3864,1608,2112,490,1850,42,6159,3680,3614,3404,8104,4072,28,7674,476,6439,2546,1162,  #End#
LowScoreRank1 , T0:7321 , T1:
Cond:621, T:148782, TR:-0.0307257986547254, Period:5.6425, Benefit0.7217,
Cond:7581, T:148893, TR:-0.0307324437213272, Period:5.6309, Benefit0.7213,
Cond:4580, T:149110, TR:-0.0307694313323956, Period:5.6581, Benefit0.7211,
Cond:1279, T:148764, TR:-0.0306931656865741, Period:5.6485, Benefit0.721,
Cond:6584, T:149684, TR:-0.0308766840892688, Period:5.635, Benefit0.7208,
Cond:7283, T:148837, TR:-0.0307001279328151, Period:5.628, Benefit0.7208,
Cond:395, T:150142, TR:-0.0309680230917924, Period:5.6364, Benefit0.7207,
Cond:7373, T:148726, TR:-0.0306687478181778, Period:5.6289, Benefit0.7206,
Cond:1497, T:148796, TR:-0.0306792049094726, Period:5.637, Benefit0.7205,
Cond:3343, T:148941, TR:-0.030680497853406, Period:5.6297, Benefit0.7198,
Cond:825, T:148771, TR:-0.0306409930720457, Period:5.6371, Benefit0.7197,
Cond:5041, T:149049, TR:-0.0306988422749004, Period:5.6195, Benefit0.7197,
Cond:6570, T:148780, TR:-0.0306428657482614, Period:5.6388, Benefit0.7197,
Cond:383, T:148658, TR:-0.0306051146050666, Period:5.6481, Benefit0.7194,
Cond:1981, T:149037, TR:-0.0306839453987119, Period:5.6375, Benefit0.7194,
Cond:2393, T:149241, TR:-0.0307222445171391, Period:5.6387, Benefit0.7193,
Cond:3093, T:148724, TR:-0.0306105928774137, Period:5.6274, Benefit0.7192,
Cond:3456, T:150889, TR:-0.0310568338242235, Period:5.7009, Benefit0.7191,
Cond:6173, T:148754, TR:-0.0306127056447185, Period:5.6555, Benefit0.7191,
Cond:7615, T:148672, TR:-0.0305956582809561, Period:5.6297, Benefit0.7191,
Cond:7547, T:148726, TR:-0.0306027602857333, Period:5.6365, Benefit0.719,
Cond:2706, T:149405, TR:-0.0307397849363233, Period:5.6431, Benefit0.7189,
Cond:1495, T:149418, TR:-0.0307341994526092, Period:5.6193, Benefit0.7187,
Cond:4927, T:149120, TR:-0.0306722621578882, Period:5.6263, Benefit0.7187,
Cond:6941, T:149060, TR:-0.0306597927997635, Period:5.6227, Benefit0.7187,
Cond:2054, T:148821, TR:-0.0306060003444198, Period:5.631, Benefit0.7186,
Cond:2187, T:149783, TR:-0.0308059214485689, Period:5.6388, Benefit0.7186,
Cond:3150, T:148704, TR:-0.0305734457196004, Period:5.629, Benefit0.7184,
Cond:4112, T:148757, TR:-0.0305844535323057, Period:5.6121, Benefit0.7184,
Cond:6907, T:149381, TR:-0.0307140792848713, Period:5.6226, Benefit0.7184, , T2:621,7581,4580,1279,6584,7283,395,7373,1497,3343,825,5041,6570,383,1981,2393,3093,3456,6173,7615,7547,2706,1495,4927,6941,2054,2187,3150,4112,6907,  #End#
LowScoreRank3 , T0:7321 , T1:
Cond:3236, T:241880, TR:-0.0408680565965444, Period:6.6326, Benefit0.5794,
Cond:504, T:221391, TR:-0.0382300480776115, Period:5.531, Benefit0.5947,
Cond:56, T:220863, TR:-0.0374292782565365, Period:5.5155, Benefit0.5834,
Cond:4114, T:218573, TR:-0.0382757744667964, Period:5.2221, Benefit0.6036,
Cond:2296, T:215851, TR:-0.0367208327219143, Period:5.5199, Benefit0.5862,
Cond:254, T:215664, TR:-0.0364807393353681, Period:5.437, Benefit0.5828,
Cond:1864, T:213577, TR:-0.0372254859522284, Period:5.7149, Benefit0.6012,
Cond:3000, T:213212, TR:-0.0372740724391912, Period:5.7688, Benefit0.6031,
Cond:1176, T:212204, TR:-0.0374452722632175, Period:5.5292, Benefit0.609,
Cond:5670, T:211046, TR:-0.0356677954512619, Period:5.2629, Benefit0.5827,
Cond:2046, T:208590, TR:-0.0353630627652063, Period:5.428, Benefit0.5848,
Cond:3864, T:208446, TR:-0.0353966577277495, Period:5.4867, Benefit0.5858,
Cond:1608, T:206844, TR:-0.0361072442862852, Period:5.3643, Benefit0.6028,
Cond:2112, T:204722, TR:-0.0350968033679571, Period:6.5317, Benefit0.5919,
Cond:1850, T:204155, TR:-0.0350254294205897, Period:5.5352, Benefit0.5924,
Cond:42, T:204016, TR:-0.0343573702309575, Period:5.3779, Benefit0.5812,
Cond:6159, T:203115, TR:-0.0341040000979793, Period:6.8548, Benefit0.5795,
Cond:3680, T:202652, TR:-0.0343035635692919, Period:6.368, Benefit0.5844,
Cond:3614, T:202399, TR:-0.0351368988069485, Period:5.4599, Benefit0.5998,
Cond:3404, T:201839, TR:-0.0352697355627438, Period:5.5266, Benefit0.6039,
Cond:8104, T:201064, TR:-0.0348644374512848, Period:5.4309, Benefit0.5992,
Cond:28, T:200851, TR:-0.0337131205888518, Period:5.3554, Benefit0.5795,
Cond:7674, T:200574, TR:-0.034681091877134, Period:5.614, Benefit0.5975,
Cond:476, T:200497, TR:-0.0339735747640452, Period:5.3524, Benefit0.5852,
Cond:4072, T:200377, TR:-0.0340259559145862, Period:5.3714, Benefit0.5865,
Cond:2546, T:199306, TR:-0.0339791792146429, Period:5.4456, Benefit0.589,
Cond:1162, T:198720, TR:-0.0334630070789721, Period:5.3854, Benefit0.5816,
Cond:926, T:198503, TR:-0.0352562418704622, Period:5.4453, Benefit0.6144,
Cond:5196, T:197941, TR:-0.0349866482141276, Period:5.5762, Benefit0.6114,
Cond:1836, T:197870, TR:-0.0334161595089817, Period:5.4924, Benefit0.5834, , T2:3236,504,56,4114,2296,254,1864,3000,1176,5670,2046,3864,1608,2112,1850,42,6159,3680,3614,3404,8104,28,7674,476,4072,2546,1162,926,5196,1836,  #End#
LowScoreRank1 , T0:1273 , T1:
Cond:621, T:147787, TR:-0.0306862275285486, Period:5.6485, Benefit0.7258,
Cond:7581, T:147888, TR:-0.0306828098135901, Period:5.6372, Benefit0.7252,
Cond:7339, T:147614, TR:-0.0306213025887707, Period:5.6368, Benefit0.7251,
Cond:1279, T:147698, TR:-0.03063071185422, Period:5.6556, Benefit0.7249,
Cond:4580, T:148071, TR:-0.0307088453197627, Period:5.665, Benefit0.7249,
Cond:6584, T:148624, TR:-0.0308205924117549, Period:5.6418, Benefit0.7248,
Cond:7373, T:147696, TR:-0.0306139149953288, Period:5.6354, Benefit0.7245,
Cond:7283, T:147827, TR:-0.0306331424387569, Period:5.6342, Benefit0.7243,
Cond:7095, T:148531, TR:-0.0307640392950777, Period:5.6358, Benefit0.7239,
Cond:3343, T:147901, TR:-0.0306199276215454, Period:5.6363, Benefit0.7236,
Cond:4893, T:147647, TR:-0.0305668200061044, Period:5.6397, Benefit0.7236,
Cond:6570, T:147719, TR:-0.0305818733737584, Period:5.6457, Benefit0.7236,
Cond:7997, T:147646, TR:-0.0305666109357847, Period:5.6485, Benefit0.7236,
Cond:383, T:147598, TR:-0.0305483923745484, Period:5.6553, Benefit0.7234,
Cond:7042, T:147608, TR:-0.0305463905005381, Period:5.6528, Benefit0.7233,
Cond:3456, T:149860, TR:-0.0310131488738223, Period:5.7079, Benefit0.7232,
Cond:395, T:149103, TR:-0.0308506738569681, Period:5.643, Benefit0.7231,
Cond:3093, T:147650, TR:-0.0305469814095889, Period:5.6351, Benefit0.7231,
Cond:3717, T:148325, TR:-0.0306880331518031, Period:5.629, Benefit0.7231,
Cond:1495, T:148116, TR:-0.0306402470997596, Period:5.6274, Benefit0.723,
Cond:2706, T:148368, TR:-0.0306929065741527, Period:5.65, Benefit0.723,
Cond:6905, T:147821, TR:-0.0305786114284051, Period:5.6344, Benefit0.723,
Cond:5041, T:147965, TR:-0.0306045944815537, Period:5.6265, Benefit0.7229,
Cond:7615, T:147632, TR:-0.0305350354679765, Period:5.6361, Benefit0.7229,
Cond:6173, T:148023, TR:-0.0306126072523409, Period:5.665, Benefit0.7228,
Cond:5323, T:147856, TR:-0.0305736262886641, Period:5.6382, Benefit0.7227,
Cond:6941, T:148036, TR:-0.0306112184404674, Period:5.6289, Benefit0.7227,
Cond:8205, T:147688, TR:-0.0305385436651939, Period:5.6521, Benefit0.7227,
Cond:1725, T:147692, TR:-0.0305352845541437, Period:5.6492, Benefit0.7226,
Cond:2054, T:147772, TR:-0.0305519878861605, Period:5.6375, Benefit0.7226, , T2:621,7581,7339,1279,4580,6584,7373,7283,7095,3343,4893,6570,7997,383,7042,3456,395,3093,3717,1495,2706,6905,5041,7615,6173,5323,6941,8205,1725,2054,  #End#
LowScoreRank3 , T0:1273 , T1:
Cond:504, T:221243, TR:-0.0383913703597109, Period:5.5302, Benefit0.5977,
Cond:56, T:220724, TR:-0.0375547513758566, Period:5.514, Benefit0.5858,
Cond:4114, T:218310, TR:-0.0383391534727375, Period:5.2205, Benefit0.6054,
Cond:2296, T:215601, TR:-0.0368656735278453, Period:5.5191, Benefit0.5893,
Cond:254, T:214513, TR:-0.0364066469075434, Period:5.4413, Benefit0.5849,
Cond:1864, T:213285, TR:-0.0373836283454856, Period:5.7151, Benefit0.6047,
Cond:3000, T:212766, TR:-0.0373616295980938, Period:5.7699, Benefit0.6059,
Cond:1176, T:211932, TR:-0.0375691151598355, Period:5.5288, Benefit0.6119,
Cond:5670, T:210477, TR:-0.0358060573517334, Period:5.264, Benefit0.5867,
Cond:3864, T:208103, TR:-0.0355888804959311, Period:5.4863, Benefit0.5901,
Cond:2046, T:207414, TR:-0.0352802594726611, Period:5.4325, Benefit0.5869,
Cond:1608, T:206565, TR:-0.0362549383305513, Period:5.3636, Benefit0.6062,
Cond:2112, T:203891, TR:-0.0350419430418798, Period:6.5405, Benefit0.5935,
Cond:42, T:203809, TR:-0.0344707113453186, Period:5.377, Benefit0.5838,
Cond:1850, T:203777, TR:-0.0351767021040469, Period:5.5355, Benefit0.5962,
Cond:3680, T:201972, TR:-0.0342874728833853, Period:6.3749, Benefit0.5862,
Cond:3614, T:201096, TR:-0.0350116127379502, Period:5.4657, Benefit0.6017,
Cond:3404, T:200615, TR:-0.0351794415878812, Period:5.5323, Benefit0.6062,
Cond:8104, T:200045, TR:-0.0348402491228159, Period:5.435, Benefit0.602,
Cond:4072, T:199853, TR:-0.0341762252085986, Period:5.3729, Benefit0.5908,
Cond:476, T:199486, TR:-0.0339153354986781, Period:5.3555, Benefit0.5873,
Cond:7674, T:199032, TR:-0.0344685162079208, Period:5.6225, Benefit0.5986,
Cond:2546, T:198714, TR:-0.0339592890985026, Period:5.4473, Benefit0.5905,
Cond:6439, T:198601, TR:-0.0346207569787011, Period:5.6106, Benefit0.6027,
Cond:1162, T:198455, TR:-0.0336069028281443, Period:5.3853, Benefit0.585,
Cond:926, T:197385, TR:-0.035201884562225, Period:5.4499, Benefit0.6171,
Cond:1836, T:196773, TR:-0.0333531852788262, Period:5.4969, Benefit0.5857,
Cond:5196, T:196509, TR:-0.0348148813439385, Period:5.5839, Benefit0.613,
Cond:7688, T:196181, TR:-0.0344023600247843, Period:5.4168, Benefit0.6066,
Cond:4764, T:196108, TR:-0.034245937238007, Period:5.7658, Benefit0.604, , T2:504,56,4114,2296,254,1864,3000,1176,5670,3864,2046,1608,2112,42,1850,3680,3614,3404,8104,4072,476,7674,2546,6439,1162,926,1836,5196,7688,4764,  #End#
LowScoreRank1 , T0:1961 , T1:
Cond:621, T:148486, TR:-0.0313639818744454, Period:5.6532, Benefit0.7387,
Cond:7339, T:148275, TR:-0.0313024855000997, Period:5.642, Benefit0.7383,
Cond:7581, T:148568, TR:-0.0313567735379964, Period:5.6423, Benefit0.7381,
Cond:1279, T:148377, TR:-0.0313119091154355, Period:5.6607, Benefit0.738,
Cond:4580, T:148751, TR:-0.0313834462192902, Period:5.67, Benefit0.7378,
Cond:6584, T:149304, TR:-0.0314931365954234, Period:5.6468, Benefit0.7376,
Cond:395, T:149554, TR:-0.0315423187190528, Period:5.649, Benefit0.7375,
Cond:1497, T:148365, TR:-0.0312846665892144, Period:5.649, Benefit0.7374,
Cond:7373, T:148374, TR:-0.0312824707728686, Period:5.6405, Benefit0.7373,
Cond:7095, T:149142, TR:-0.0314337651347406, Period:5.6421, Benefit0.737,
Cond:825, T:148379, TR:-0.0312670795126697, Period:5.6491, Benefit0.7369,
Cond:7283, T:148520, TR:-0.0312971129474934, Period:5.6389, Benefit0.7369,
Cond:2393, T:148299, TR:-0.0312377046309988, Period:5.6532, Benefit0.7366,
Cond:5323, T:148432, TR:-0.0312660212030987, Period:5.6444, Benefit0.7366,
Cond:7997, T:148326, TR:-0.0312434529390888, Period:5.6536, Benefit0.7366,
Cond:383, T:148282, TR:-0.0312299739677547, Period:5.6602, Benefit0.7365,
Cond:4893, T:148326, TR:-0.0312393402666253, Period:5.6448, Benefit0.7365,
Cond:6570, T:148398, TR:-0.0312546674304753, Period:5.6508, Benefit0.7365,
Cond:3343, T:148570, TR:-0.031287165082594, Period:5.6415, Benefit0.7364,
Cond:7042, T:148288, TR:-0.0312189164503502, Period:5.6578, Benefit0.7362,
Cond:1725, T:148368, TR:-0.0312277116132513, Period:5.6539, Benefit0.736,
Cond:2706, T:149046, TR:-0.0313678386816941, Period:5.655, Benefit0.7359,
Cond:3456, T:150538, TR:-0.0316854449770111, Period:5.7126, Benefit0.7359,
Cond:6905, T:148474, TR:-0.0312461450971215, Period:5.6401, Benefit0.7359,
Cond:3755, T:148785, TR:-0.0313081799516985, Period:5.6479, Benefit0.7358,
Cond:7615, T:148312, TR:-0.031207574177599, Period:5.6413, Benefit0.7358,
Cond:3093, T:148340, TR:-0.0312094158876665, Period:5.6399, Benefit0.7357,
Cond:6173, T:148703, TR:-0.0312866133338214, Period:5.67, Benefit0.7357,
Cond:8205, T:148368, TR:-0.0312153699585029, Period:5.6572, Benefit0.7357,
Cond:2054, T:148451, TR:-0.0312289037878126, Period:5.6427, Benefit0.7356, , T2:621,7339,7581,1279,4580,6584,395,1497,7373,7095,825,7283,2393,5323,7997,383,4893,6570,3343,7042,1725,2706,3456,6905,3755,7615,3093,6173,8205,2054,  #End#
LowScoreRank3 , T0:1961 , T1:
Cond:504, T:221887, TR:-0.0390537140916258, Period:5.5343, Benefit0.6064,
Cond:56, T:221350, TR:-0.0382039113318783, Period:5.5183, Benefit0.5944,
Cond:4114, T:218835, TR:-0.0389607679674303, Period:5.2261, Benefit0.6139,
Cond:2296, T:216277, TR:-0.0375537020993277, Period:5.523, Benefit0.5986,
Cond:254, T:215178, TR:-0.0370947946644187, Period:5.4456, Benefit0.5943,
Cond:1864, T:213965, TR:-0.0380749422345203, Period:5.7184, Benefit0.6141,
Cond:3000, T:213446, TR:-0.038051690790187, Period:5.773, Benefit0.6153,
Cond:1176, T:212600, TR:-0.0382437544659014, Period:5.5328, Benefit0.6211,
Cond:5670, T:211393, TR:-0.0363845731124628, Period:5.2669, Benefit0.5937,
Cond:3864, T:208840, TR:-0.0362607445114925, Period:5.49, Benefit0.5993,
Cond:2046, T:208140, TR:-0.0360355435748712, Period:5.4364, Benefit0.5976,
Cond:1608, T:207220, TR:-0.0369167689495009, Period:5.3685, Benefit0.6155,
Cond:42, T:204474, TR:-0.0351278760977599, Period:5.3814, Benefit0.5932,
Cond:1850, T:204447, TR:-0.0358486510068192, Period:5.5393, Benefit0.6058,
Cond:2112, T:204338, TR:-0.0355757278121575, Period:6.5426, Benefit0.6014,
Cond:3680, T:202598, TR:-0.0349272257857471, Period:6.3767, Benefit0.5955,
Cond:3614, T:201894, TR:-0.0357967251353226, Period:5.4689, Benefit0.613,
Cond:3404, T:201375, TR:-0.035888975306957, Period:5.5352, Benefit0.6163,
Cond:8104, T:200666, TR:-0.0355104583936841, Period:5.4388, Benefit0.6119,
Cond:4072, T:200634, TR:-0.0348328863659639, Period:5.3766, Benefit0.6,
Cond:476, T:200096, TR:-0.0345172799608416, Period:5.3607, Benefit0.5961,
Cond:7674, T:199849, TR:-0.0352155130121009, Period:5.6253, Benefit0.6093,
Cond:1162, T:199127, TR:-0.0342614249818393, Period:5.39, Benefit0.5946,
Cond:2546, T:198986, TR:-0.0345442657645041, Period:5.4544, Benefit0.6001,
Cond:926, T:198061, TR:-0.0358938439006698, Period:5.4542, Benefit0.6273,
Cond:6439, T:197797, TR:-0.0346546455070424, Period:5.6162, Benefit0.6059,
Cond:1836, T:197478, TR:-0.0340198833060475, Period:5.501, Benefit0.5955,
Cond:5196, T:197355, TR:-0.0355680681438897, Period:5.5858, Benefit0.6238,
Cond:7688, T:197021, TR:-0.0350686970787256, Period:5.4202, Benefit0.6159,
Cond:4764, T:196843, TR:-0.0349531456567557, Period:5.7682, Benefit0.6144, , T2:504,56,4114,2296,254,1864,3000,1176,5670,3864,2046,1608,42,1850,2112,3680,3614,3404,8104,4072,476,7674,1162,2546,926,6439,1836,5196,7688,4764,  #End#
LowScoreRank1 , T0:2998 , T1:
Cond:621, T:147730, TR:-0.031128870786847, Period:5.6169, Benefit0.7369,
Cond:7581, T:147778, TR:-0.0311349941371315, Period:5.6063, Benefit0.7368,
Cond:7339, T:147520, TR:-0.0310637135328104, Period:5.6054, Benefit0.7364,
Cond:1279, T:147625, TR:-0.0310778679535526, Period:5.624, Benefit0.7362,
Cond:7283, T:147783, TR:-0.0310991856157143, Period:5.602, Benefit0.7359,
Cond:7373, T:147599, TR:-0.0310559709366069, Period:5.6042, Benefit0.7358,
Cond:1497, T:147657, TR:-0.0310642077151187, Period:5.6123, Benefit0.7357,
Cond:6584, T:148476, TR:-0.0312383365263517, Period:5.612, Benefit0.7357,
Cond:2054, T:147523, TR:-0.0310234568109725, Period:5.6105, Benefit0.7354,
Cond:4893, T:147537, TR:-0.0310182514727574, Period:5.6086, Benefit0.7352,
Cond:6570, T:147603, TR:-0.0310322704462056, Period:5.6147, Benefit0.7352,
Cond:825, T:147619, TR:-0.0310315767953966, Period:5.6126, Benefit0.7351,
Cond:7095, T:148461, TR:-0.0312104472246061, Period:5.6044, Benefit0.7351,
Cond:2292, T:147513, TR:-0.0310049753746009, Period:5.6134, Benefit0.735,
Cond:2706, T:148204, TR:-0.0311517333748858, Period:5.6198, Benefit0.735,
Cond:5041, T:147911, TR:-0.0310853970243764, Period:5.5947, Benefit0.7349,
Cond:7997, T:147546, TR:-0.0310078926388974, Period:5.6173, Benefit0.7349,
Cond:383, T:147518, TR:-0.0309978583920465, Period:5.6237, Benefit0.7348,
Cond:3343, T:147807, TR:-0.0310592142927837, Period:5.6051, Benefit0.7348,
Cond:395, T:149134, TR:-0.0313369321661612, Period:5.611, Benefit0.7347,
Cond:3456, T:149653, TR:-0.0314472088577909, Period:5.678, Benefit0.7347,
Cond:4580, T:147792, TR:-0.0310519322291019, Period:5.6356, Benefit0.7347,
Cond:6471, T:147505, TR:-0.0309869207139473, Period:5.6093, Benefit0.7346,
Cond:3384, T:147610, TR:-0.0309969292707295, Period:5.6059, Benefit0.7343,
Cond:7615, T:147536, TR:-0.0309812302194131, Period:5.6049, Benefit0.7343,
Cond:3093, T:147611, TR:-0.0309930493882501, Period:5.6029, Benefit0.7342,
Cond:6173, T:147931, TR:-0.0310609353961998, Period:5.6338, Benefit0.7342,
Cond:6905, T:147716, TR:-0.0310153231569347, Period:5.6032, Benefit0.7342,
Cond:8205, T:147596, TR:-0.0309857759237544, Period:5.6208, Benefit0.7341,
Cond:1396, T:147933, TR:-0.031053157064148, Period:5.6199, Benefit0.734, , T2:621,7581,7339,1279,7283,7373,1497,6584,2054,4893,6570,825,7095,2292,2706,5041,7997,383,3343,395,3456,4580,6471,3384,7615,3093,6173,6905,8205,1396,  #End#
LowScoreRank3 , T0:2998 , T1:
Cond:504, T:218191, TR:-0.0380953969262263, Period:5.5367, Benefit0.6018,
Cond:4114, T:215975, TR:-0.0382439643246185, Period:5.2212, Benefit0.6108,
Cond:2296, T:212457, TR:-0.0365678271129781, Period:5.5258, Benefit0.5936,
Cond:3000, T:210538, TR:-0.0372186358201847, Period:5.7712, Benefit0.6103,
Cond:1864, T:210089, TR:-0.0370714246350355, Period:5.725, Benefit0.6092,
Cond:1176, T:208826, TR:-0.0372711361432125, Period:5.5358, Benefit0.6165,
Cond:2046, T:205957, TR:-0.0354078471369813, Period:5.4269, Benefit0.5935,
Cond:3864, T:205522, TR:-0.0354349969831215, Period:5.4904, Benefit0.5953,
Cond:1608, T:203611, TR:-0.0359900080765359, Period:5.3689, Benefit0.6109,
Cond:2112, T:203536, TR:-0.0354204173714221, Period:6.5213, Benefit0.6012,
Cond:1850, T:201313, TR:-0.0349711825061705, Period:5.5383, Benefit0.6003,
Cond:3680, T:201036, TR:-0.034537001289306, Period:6.3628, Benefit0.5935,
Cond:3614, T:200615, TR:-0.0354447253830673, Period:5.4474, Benefit0.6109,
Cond:3404, T:199740, TR:-0.0354654323709912, Period:5.5202, Benefit0.6141,
Cond:7674, T:199090, TR:-0.0350387452013558, Period:5.6001, Benefit0.6086,
Cond:8104, T:199047, TR:-0.035176503332125, Period:5.427, Benefit0.6112,
Cond:6439, T:199001, TR:-0.035095388611274, Period:5.5868, Benefit0.6099,
Cond:476, T:197561, TR:-0.0339346058434357, Period:5.3553, Benefit0.5937,
Cond:4072, T:197508, TR:-0.034052952492538, Period:5.374, Benefit0.596,
Cond:2546, T:197055, TR:-0.0338453427192017, Period:5.4478, Benefit0.5937,
Cond:5196, T:196387, TR:-0.0353004026225664, Period:5.5631, Benefit0.6222,
Cond:4764, T:196055, TR:-0.0347157126966692, Period:5.7433, Benefit0.6127,
Cond:926, T:195772, TR:-0.0352527962948082, Period:5.4457, Benefit0.6234,
Cond:7688, T:195664, TR:-0.0347545381636773, Period:5.3999, Benefit0.6147,
Cond:1836, T:195098, TR:-0.0334291006183514, Period:5.494, Benefit0.5924,
Cond:2958, T:194901, TR:-0.0349000307394554, Period:5.5882, Benefit0.6199,
Cond:7305, T:194759, TR:-0.0336047425645209, Period:5.6637, Benefit0.5967,
Cond:5850, T:194053, TR:-0.0338666984472454, Period:5.3554, Benefit0.6038,
Cond:5725, T:193054, TR:-0.0337745709601127, Period:6.2132, Benefit0.6054,
Cond:3850, T:193050, TR:-0.0333185767205452, Period:5.3982, Benefit0.597, , T2:504,4114,2296,3000,1864,1176,2046,3864,1608,2112,1850,3680,3614,3404,7674,8104,6439,476,4072,2546,5196,4764,926,7688,1836,2958,7305,5850,5725,3850,  #End#
LowScoreRank1 , T0:3444 , T1:
Cond:621, T:148079, TR:-0.0311210755474302, Period:5.5877, Benefit0.7349,
Cond:7581, T:148137, TR:-0.0311169650374894, Period:5.5769, Benefit0.7345,
Cond:7339, T:147865, TR:-0.0310510322873086, Period:5.5763, Benefit0.7343,
Cond:1279, T:147974, TR:-0.0310700584965755, Period:5.5948, Benefit0.7342,
Cond:4098, T:148320, TR:-0.031131138049879, Period:5.5573, Benefit0.7339,
Cond:4580, T:148258, TR:-0.0311179867266599, Period:5.6055, Benefit0.7339,
Cond:7283, T:148131, TR:-0.0310910491375211, Period:5.5729, Benefit0.7339,
Cond:6584, T:148802, TR:-0.0312292679904295, Period:5.5835, Benefit0.7338,
Cond:7373, T:147951, TR:-0.0310487712786618, Period:5.5748, Benefit0.7338,
Cond:1497, T:148006, TR:-0.031056330743468, Period:5.5831, Benefit0.7337,
Cond:6570, T:147953, TR:-0.0310327877540281, Period:5.5856, Benefit0.7334,
Cond:4893, T:147888, TR:-0.0310108122133697, Period:5.5793, Benefit0.7332,
Cond:7095, T:148807, TR:-0.0312055693821461, Period:5.5755, Benefit0.7332,
Cond:825, T:147968, TR:-0.0310236597649295, Period:5.5833, Benefit0.7331,
Cond:2706, T:148564, TR:-0.0311499429146926, Period:5.591, Benefit0.7331,
Cond:3456, T:150004, TR:-0.0314552288901551, Period:5.6493, Benefit0.7331,
Cond:7997, T:147893, TR:-0.0310036712108951, Period:5.5881, Benefit0.733,
Cond:3384, T:147895, TR:-0.0309999946027035, Period:5.5785, Benefit0.7329,
Cond:383, T:147867, TR:-0.0309899652959965, Period:5.5944, Benefit0.7328,
Cond:3343, T:148156, TR:-0.0310511662453247, Period:5.5759, Benefit0.7328,
Cond:395, T:149483, TR:-0.0313281622420573, Period:5.5821, Benefit0.7327,
Cond:5041, T:148248, TR:-0.0310665405377529, Period:5.5659, Benefit0.7327,
Cond:6471, T:147852, TR:-0.0309785910617976, Period:5.5801, Benefit0.7326,
Cond:3093, T:147958, TR:-0.0309887248739093, Period:5.5737, Benefit0.7323,
Cond:3150, T:147868, TR:-0.0309655803303449, Period:5.576, Benefit0.7322,
Cond:6173, T:148280, TR:-0.0310527604157482, Period:5.6046, Benefit0.7322,
Cond:6905, T:148058, TR:-0.0310057822640836, Period:5.5741, Benefit0.7322,
Cond:6941, T:148290, TR:-0.0310548766848297, Period:5.5685, Benefit0.7322,
Cond:7615, T:147885, TR:-0.0309691771729478, Period:5.5757, Benefit0.7322,
Cond:8205, T:147943, TR:-0.0309814490087589, Period:5.5916, Benefit0.7322, , T2:621,7581,7339,1279,4098,4580,7283,6584,7373,1497,6570,4893,7095,825,2706,3456,7997,3384,383,3343,395,5041,6471,3093,3150,6173,6905,6941,7615,8205,  #End#
LowScoreRank3 , T0:3444 , T1:
Cond:504, T:218134, TR:-0.0380974193361593, Period:5.5358, Benefit0.602,
Cond:56, T:217839, TR:-0.0373357734663973, Period:5.5163, Benefit0.5905,
Cond:4114, T:215353, TR:-0.0381055713372357, Period:5.2219, Benefit0.6104,
Cond:254, T:212642, TR:-0.0364326538860677, Period:5.4323, Benefit0.5908,
Cond:2296, T:212421, TR:-0.0365554266681534, Period:5.5259, Benefit0.5935,
Cond:3000, T:210806, TR:-0.0372735486873885, Period:5.7646, Benefit0.6104,
Cond:1864, T:210270, TR:-0.0370747649483047, Period:5.7227, Benefit0.6087,
Cond:1176, T:208753, TR:-0.0372635645171797, Period:5.5356, Benefit0.6166,
Cond:2046, T:205683, TR:-0.0353650903687391, Period:5.4214, Benefit0.5936,
Cond:3864, T:205424, TR:-0.0353307704750748, Period:5.4877, Benefit0.5938,
Cond:2112, T:204006, TR:-0.0354472292029018, Period:6.4976, Benefit0.6002,
Cond:1608, T:203373, TR:-0.0359409312121341, Period:5.3683, Benefit0.6108,
Cond:3680, T:201440, TR:-0.0346310798115407, Period:6.3417, Benefit0.5939,
Cond:1850, T:201172, TR:-0.0349176725060036, Period:5.537, Benefit0.5998,
Cond:3614, T:200696, TR:-0.0356062709174242, Period:5.4339, Benefit0.6135,
Cond:3404, T:199775, TR:-0.0354943048281755, Period:5.5083, Benefit0.6145,
Cond:7674, T:199397, TR:-0.0350606715509456, Period:5.5811, Benefit0.608,
Cond:6439, T:199289, TR:-0.0351140030020275, Period:5.5664, Benefit0.6093,
Cond:8104, T:198688, TR:-0.0350442062089906, Period:5.4218, Benefit0.61,
Cond:476, T:197280, TR:-0.0338406692680415, Period:5.3508, Benefit0.5929,
Cond:4072, T:197028, TR:-0.033873807339135, Period:5.375, Benefit0.5943,
Cond:2546, T:196872, TR:-0.0337190016767781, Period:5.4386, Benefit0.592,
Cond:5196, T:196663, TR:-0.0353900918329177, Period:5.5446, Benefit0.6229,
Cond:4764, T:196411, TR:-0.0347860015405476, Period:5.7224, Benefit0.6128,
Cond:7688, T:195893, TR:-0.0347963266946577, Period:5.3829, Benefit0.6147,
Cond:926, T:195659, TR:-0.0352263878454507, Period:5.4377, Benefit0.6233,
Cond:2958, T:195151, TR:-0.0350282499770348, Period:5.5704, Benefit0.6214,
Cond:7305, T:195144, TR:-0.0336016468699136, Period:5.6417, Benefit0.5954,
Cond:1836, T:194818, TR:-0.0333415728665857, Period:5.4899, Benefit0.5917,
Cond:5850, T:193713, TR:-0.0337351031617953, Period:5.3522, Benefit0.6025, , T2:504,56,4114,254,2296,3000,1864,1176,2046,3864,2112,1608,3680,1850,3614,3404,7674,6439,8104,476,4072,2546,5196,4764,7688,926,2958,7305,1836,5850,  #End#
LowScoreRank1 , T0:3890 , T1:
Cond:621, T:146686, TR:-0.0307480998924672, Period:5.6125, Benefit0.733,
Cond:7581, T:146750, TR:-0.0307494454288863, Period:5.6015, Benefit0.7327,
Cond:7339, T:146476, TR:-0.0306874145765032, Period:5.601, Benefit0.7326,
Cond:4580, T:146932, TR:-0.0307798120574708, Period:5.6296, Benefit0.7325,
Cond:1279, T:146581, TR:-0.0306974389171647, Period:5.6197, Benefit0.7323,
Cond:6584, T:147453, TR:-0.0308818730471224, Period:5.6071, Benefit0.7323,
Cond:7283, T:146734, TR:-0.0307175928375523, Period:5.5977, Benefit0.732,
Cond:7373, T:146562, TR:-0.0306812359510953, Period:5.5995, Benefit0.732,
Cond:1497, T:146613, TR:-0.0306838893376416, Period:5.6079, Benefit0.7318,
Cond:3456, T:148661, TR:-0.0311004198107439, Period:5.6741, Benefit0.7314,
Cond:825, T:146575, TR:-0.0306514865353901, Period:5.6082, Benefit0.7312,
Cond:4893, T:146503, TR:-0.0306362849043463, Period:5.604, Benefit0.7312,
Cond:2054, T:146474, TR:-0.0306261029594182, Period:5.6062, Benefit0.7311,
Cond:3343, T:146760, TR:-0.0306864815785258, Period:5.6006, Benefit0.7311,
Cond:6570, T:146569, TR:-0.0306461577328156, Period:5.6103, Benefit0.7311,
Cond:7997, T:146498, TR:-0.0306311693287972, Period:5.6131, Benefit0.7311,
Cond:4098, T:146733, TR:-0.0306767143832315, Period:5.5865, Benefit0.731,
Cond:7095, T:147417, TR:-0.0308211333297667, Period:5.6, Benefit0.731,
Cond:383, T:146474, TR:-0.0306179844851419, Period:5.6193, Benefit0.7309,
Cond:5041, T:146851, TR:-0.0306975547443415, Period:5.5906, Benefit0.7309,
Cond:395, T:148090, TR:-0.0309550714905972, Period:5.6066, Benefit0.7308,
Cond:2706, T:147200, TR:-0.0307589895661651, Period:5.6152, Benefit0.7306,
Cond:6471, T:146462, TR:-0.0306032753445826, Period:5.6048, Benefit0.7306,
Cond:6905, T:146670, TR:-0.0306430909435577, Period:5.5989, Benefit0.7305,
Cond:6941, T:146892, TR:-0.0306899241051953, Period:5.5933, Benefit0.7305,
Cond:7615, T:146493, TR:-0.0306016953341834, Period:5.6004, Benefit0.7304,
Cond:6173, T:146887, TR:-0.0306807269436605, Period:5.6295, Benefit0.7303,
Cond:1396, T:146916, TR:-0.0306827715396747, Period:5.6148, Benefit0.7302,
Cond:8205, T:146552, TR:-0.0306060155273874, Period:5.6165, Benefit0.7302,
Cond:3093, T:146518, TR:-0.0305947862945243, Period:5.5995, Benefit0.7301, , T2:621,7581,7339,4580,1279,6584,7283,7373,1497,3456,825,4893,2054,3343,6570,7997,4098,7095,383,5041,395,2706,6471,6905,6941,7615,6173,1396,8205,3093,  #End#
LowScoreRank3 , T0:3890 , T1:
Cond:504, T:218135, TR:-0.0380852649881205, Period:5.5359, Benefit0.6018,
Cond:56, T:217703, TR:-0.0372562417132503, Period:5.5183, Benefit0.5896,
Cond:4114, T:215145, TR:-0.0380188403705243, Period:5.2226, Benefit0.6096,
Cond:2296, T:212474, TR:-0.0365708512821208, Period:5.5249, Benefit0.5936,
Cond:254, T:211942, TR:-0.0362369428452722, Period:5.4391, Benefit0.5896,
Cond:1864, T:210278, TR:-0.0371414781075933, Period:5.7222, Benefit0.6098,
Cond:3000, T:210234, TR:-0.0372876540862121, Period:5.7707, Benefit0.6124,
Cond:1176, T:208837, TR:-0.0373084998915695, Period:5.5344, Benefit0.6171,
Cond:5670, T:207737, TR:-0.035518464556183, Period:5.2632, Benefit0.59,
Cond:3864, T:205127, TR:-0.0353128042867136, Period:5.4901, Benefit0.5944,
Cond:2046, T:204914, TR:-0.0351480312610916, Period:5.4297, Benefit0.5922,
Cond:1608, T:203445, TR:-0.035971228448396, Period:5.3673, Benefit0.6111,
Cond:2112, T:202721, TR:-0.0351031436411315, Period:6.5204, Benefit0.5982,
Cond:1850, T:200864, TR:-0.034998308066748, Period:5.539, Benefit0.6022,
Cond:42, T:200821, TR:-0.0342334371577335, Period:5.3791, Benefit0.5888,
Cond:3680, T:200405, TR:-0.0343522154238551, Period:6.3612, Benefit0.5922,
Cond:3614, T:199293, TR:-0.0350530723375819, Period:5.4527, Benefit0.6082,
Cond:3404, T:198537, TR:-0.0350726441729988, Period:5.5235, Benefit0.611,
Cond:7674, T:198008, TR:-0.034575739294966, Period:5.5994, Benefit0.6038,
Cond:6439, T:197937, TR:-0.0347188110462084, Period:5.5844, Benefit0.6066,
Cond:8104, T:197773, TR:-0.0347615222603274, Period:5.4317, Benefit0.6079,
Cond:4072, T:196882, TR:-0.0338756981813399, Period:5.3752, Benefit0.5948,
Cond:476, T:196730, TR:-0.0337437911749794, Period:5.355, Benefit0.5929,
Cond:2546, T:196073, TR:-0.0337438047551248, Period:5.4458, Benefit0.595,
Cond:1162, T:195458, TR:-0.033371588814832, Period:5.3877, Benefit0.5902,
Cond:5196, T:195303, TR:-0.0349246001658059, Period:5.5622, Benefit0.619,
Cond:4764, T:195038, TR:-0.0343280381724164, Period:5.7417, Benefit0.609,
Cond:926, T:194899, TR:-0.0350529547810147, Period:5.4462, Benefit0.6227,
Cond:7688, T:194682, TR:-0.0343949478265852, Period:5.3985, Benefit0.6114,
Cond:1836, T:194123, TR:-0.033203220273857, Period:5.4968, Benefit0.5914, , T2:504,56,4114,2296,254,1864,3000,1176,5670,3864,2046,1608,2112,1850,42,3680,3614,3404,7674,6439,8104,4072,476,2546,1162,5196,4764,926,7688,1836,  #End#
LowScoreRank1 , T0:3206 , T1:
Cond:621, T:145847, TR:-0.0305665087941693, Period:5.6313, Benefit0.7329,
Cond:7581, T:145910, TR:-0.0305677073031242, Period:5.6203, Benefit0.7326,
Cond:7339, T:145640, TR:-0.0305025459699893, Period:5.6197, Benefit0.7324,
Cond:6584, T:146624, TR:-0.0307065316299094, Period:5.6255, Benefit0.7323,
Cond:1279, T:145742, TR:-0.0305160305136065, Period:5.6385, Benefit0.7322,
Cond:4580, T:146068, TR:-0.0305808823919632, Period:5.6487, Benefit0.7321,
Cond:7283, T:145900, TR:-0.0305372938637058, Period:5.6162, Benefit0.7319,
Cond:7373, T:145722, TR:-0.0304996912947153, Period:5.6183, Benefit0.7319,
Cond:1497, T:145774, TR:-0.0304985587679185, Period:5.6267, Benefit0.7316,
Cond:4098, T:146228, TR:-0.0305782287021934, Period:5.5966, Benefit0.7312,
Cond:825, T:145736, TR:-0.0304703449931424, Period:5.6269, Benefit0.7311,
Cond:2054, T:145628, TR:-0.0304475564872337, Period:5.6252, Benefit0.7311,
Cond:4893, T:145666, TR:-0.0304555745097279, Period:5.6228, Benefit0.7311,
Cond:6570, T:145738, TR:-0.0304707670153732, Period:5.6289, Benefit0.7311,
Cond:7095, T:146582, TR:-0.0306489021523685, Period:5.6185, Benefit0.7311,
Cond:7997, T:145669, TR:-0.0304521714909356, Period:5.6316, Benefit0.731,
Cond:3343, T:145927, TR:-0.0305025632965074, Period:5.6192, Benefit0.7309,
Cond:383, T:145635, TR:-0.0304369283355466, Period:5.6382, Benefit0.7308,
Cond:5041, T:146024, TR:-0.0305189822254218, Period:5.6089, Benefit0.7308,
Cond:395, T:147251, TR:-0.0307738343075173, Period:5.6252, Benefit0.7307,
Cond:2706, T:146333, TR:-0.030580118715729, Period:5.6343, Benefit0.7307,
Cond:3456, T:147863, TR:-0.0308989335061074, Period:5.6925, Benefit0.7306,
Cond:3384, T:145710, TR:-0.0304406354890978, Period:5.6214, Benefit0.7305,
Cond:6471, T:145621, TR:-0.0304218716364689, Period:5.6237, Benefit0.7305,
Cond:4260, T:145631, TR:-0.0304199449559535, Period:5.6208, Benefit0.7304,
Cond:6905, T:145843, TR:-0.0304605954908268, Period:5.6174, Benefit0.7303,
Cond:7615, T:145657, TR:-0.0304213900342705, Period:5.6191, Benefit0.7303,
Cond:3093, T:145727, TR:-0.0304321066021198, Period:5.6171, Benefit0.7302,
Cond:6941, T:146061, TR:-0.030502503787798, Period:5.6118, Benefit0.7302,
Cond:1396, T:146066, TR:-0.0304995102642969, Period:5.6339, Benefit0.7301, , T2:621,7581,7339,6584,1279,4580,7283,7373,1497,4098,825,2054,4893,6570,7095,7997,3343,383,5041,395,2706,3456,3384,6471,4260,6905,7615,3093,6941,1396,  #End#
LowScoreRank3 , T0:3206 , T1:
Cond:504, T:218056, TR:-0.0380709722638634, Period:5.5369, Benefit0.6018,
Cond:56, T:217589, TR:-0.0372606445068084, Period:5.5197, Benefit0.59,
Cond:4114, T:215168, TR:-0.0380230483987707, Period:5.2223, Benefit0.6096,
Cond:2296, T:212405, TR:-0.0365525809544215, Period:5.5259, Benefit0.5935,
Cond:254, T:211650, TR:-0.0361793939302942, Period:5.443, Benefit0.5895,
Cond:1864, T:210099, TR:-0.0370613938247912, Period:5.7248, Benefit0.609,
Cond:3000, T:209762, TR:-0.0370768990399207, Period:5.7776, Benefit0.6103,
Cond:1176, T:208736, TR:-0.0372545436690495, Period:5.5359, Benefit0.6165,
Cond:5670, T:207614, TR:-0.0354733638784959, Period:5.2646, Benefit0.5896,
Cond:3864, T:204980, TR:-0.0352636312150237, Period:5.4924, Benefit0.594,
Cond:2046, T:204595, TR:-0.0350628490825118, Period:5.4339, Benefit0.5917,
Cond:1608, T:203373, TR:-0.0359409312121341, Period:5.3683, Benefit0.6108,
Cond:2112, T:201908, TR:-0.0349127676084682, Period:6.5373, Benefit0.5974,
Cond:1850, T:200638, TR:-0.034867470933936, Period:5.5423, Benefit0.6006,
Cond:42, T:200630, TR:-0.0341604428696188, Period:5.3817, Benefit0.5881,
Cond:3680, T:199788, TR:-0.034170418152186, Period:6.374, Benefit0.5909,
Cond:3614, T:198930, TR:-0.035009784719496, Period:5.4571, Benefit0.6086,
Cond:3404, T:198149, TR:-0.0350188781830088, Period:5.5291, Benefit0.6113,
Cond:8104, T:197483, TR:-0.0347035810764201, Period:5.4363, Benefit0.6078,
Cond:7674, T:197395, TR:-0.034504580203295, Period:5.6094, Benefit0.6045,
Cond:6439, T:197207, TR:-0.0345761325943122, Period:5.5965, Benefit0.6064,
Cond:4072, T:196771, TR:-0.0338505556262234, Period:5.3768, Benefit0.5947,
Cond:476, T:196461, TR:-0.0336467307957875, Period:5.3586, Benefit0.592,
Cond:2546, T:195833, TR:-0.0336408799565502, Period:5.4494, Benefit0.5939,
Cond:1162, T:195271, TR:-0.0332894586442635, Period:5.3904, Benefit0.5893,
Cond:5196, T:194649, TR:-0.0347990097659229, Period:5.5728, Benefit0.6189,
Cond:926, T:194515, TR:-0.0349437701947353, Period:5.4516, Benefit0.622,
Cond:4764, T:194324, TR:-0.0342263198989373, Period:5.7541, Benefit0.6095,
Cond:7688, T:194131, TR:-0.0342895639638223, Period:5.4065, Benefit0.6113,
Cond:1836, T:193793, TR:-0.033090919903524, Period:5.5015, Benefit0.5904, , T2:504,56,4114,2296,254,1864,3000,1176,5670,3864,2046,1608,2112,1850,42,3680,3614,3404,8104,7674,6439,4072,476,2546,1162,5196,926,4764,7688,1836,  #End#
End , T0:01:23:14.8899955  #End#
 
 /////////////////////////////////
/////////////////////////////////
/////////////////////////////////
/////////////////////////////////
/////////////////////////////////
/////////////////////////////////
/////////////////////////////////
/////////////////////////////////
/////////////////////////////////
/////////////////////////////////
///


 */
