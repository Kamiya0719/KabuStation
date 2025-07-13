using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharp_sample
{
	class Condtions
	{
		private const bool IsAllCheck = true;


		/** 2500コード*2000日*数千パターンの51チェックを全て行って保存する */
		public static void SaveCond51All()
		{
			CsvControll.CreateFolders();
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
				DateTime date = DateTime.Parse(codeInfo[i][0]); // 日付
				saveRow[0] = Common.GetDateIdx(date).ToString();
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
				if (false) {
					SaveBenefit(symbol);
				} else {
					SaveBenefit2(symbol);
				}
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

		/** 空売り用の利益を算出してセーブ */
		private static void SaveBenefit2(string symbol)
		{
			// 買って
			List<string[]> saveData = new List<string[]>();
			List<string[]> codeInfo = CsvControll.GetCodeInfo(symbol);
			int num = 20;
			for (int i = 0; i < codeInfo.Count - num; i++) {
				DateTime date = DateTime.Parse(codeInfo[i][0]);
				double buyPrice = Double.Parse(codeInfo[i][4]); // 終値で売却

				double sellPrice = Double.Parse(codeInfo[i + num][4]);
				int sellPeriod = 0;
				for (int havePeriod = 1; havePeriod <= num; havePeriod++) {
					sellPeriod = havePeriod;
					double sellRatio = 0.95;
					// 理想売り
					if (Double.Parse(codeInfo[i + havePeriod][3]) <= buyPrice * sellRatio) {
						sellPrice = buyPrice * sellRatio;
						break;
					}
					// 前日終値より3％および当日高値が7％以上増えてたら損切 でも前日より13％増しは流石にいったんステイ
					if (havePeriod >= 2
						&& Double.Parse(codeInfo[i + havePeriod - 1][4]) >= buyPrice * 1.03
						&& Double.Parse(codeInfo[i + havePeriod][4]) >= buyPrice * 1.08
						&& Double.Parse(codeInfo[i + havePeriod][4]) < Double.Parse(codeInfo[i + havePeriod - 1][4]) * 1.13
					) {
						sellPrice = Double.Parse(codeInfo[i + havePeriod][4]);
						break;
					}
				}

				int benefit = (int)Math.Round((1 - sellPrice / buyPrice) * 100, MidpointRounding.AwayFromZero);
				//Common.DebugInfo("SaveBenefit", benefit, sellPeriod);
				saveData.Add(new string[3] { codeInfo[i][0], benefit.ToString(), sellPeriod.ToString() });
			}

			CsvControll.SaveBenefitAll(saveData, "Kara_" + symbol);
		}


		private const int AllTrueCondIdx = 1;
		private static readonly int[] OldPro500 = new int[] {
			// 2024/06/10
			//1332,1333,1414,1417,1605,1662,1719,1720,1721,1812,1820,1832,1861,1911,1925,1928,1930,1934,1941,1959,1961,1964,1965,1969,1973,1975,2121,2124,2146,2154,2212,2216,2229,2327,2337,2501,2502,2503,2579,2585,2670,2674,2678,2685,2702,2749,2751,2760,2768,2782,2791,2801,2802,2805,2809,2811,2871,2875,2914,2915,2928,3002,3003,3038,3048,3050,3064,3075,3076,3086,3092,3093,3097,3099,3104,3107,3110,3132,3179,3196,3231,3289,3302,3355,3374,3382,3388,3401,3405,3407,3433,3434,3436,3445,3480,3498,3539,3569,3591,3608,3612,3679,3692,3733,3774,3791,3864,3901,3941,3964,3993,3994,4004,4022,4028,4042,4043,4045,4047,4063,4088,4091,4095,4107,4114,4116,4118,4182,4183,4186,4188,4189,4194,4203,4204,4206,4208,4212,4216,4218,4228,4275,4307,4369,4373,4374,4401,4413,4417,4419,4452,4486,4493,4502,4507,4519,4526,4543,4568,4578,4611,4617,4619,4661,4676,4681,4684,4687,4704,4725,4733,4743,4746,4751,4755,4765,4768,4886,4901,4956,4972,4980,4992,5013,5019,5020,5027,5101,5105,5108,5110,5161,5191,5201,5254,5310,5333,5334,5344,5384,5393,5401,5411,5595,5703,5706,5711,5713,5714,5715,5741,5801,5802,5803,5834,5838,5842,5857,5911,5943,5975,5991,6055,6062,6088,6098,6113,6135,6140,6141,6146,6178,6196,6201,6228,6254,6255,6258,6266,6269,6272,6273,6278,6282,6294,6301,6305,6315,6323,6326,6330,6339,6357,6361,6363,6367,6368,6369,6370,6371,6376,6383,6395,6432,6454,6458,6463,6465,6471,6472,6479,6480,6481,6486,6490,6501,6503,6504,6506,6508,6516,6524,6526,6544,6547,6563,6594,6670,6701,6702,6723,6724,6728,6741,6752,6758,6762,6777,6787,6841,6857,6861,6871,6877,6902,6920,6946,6954,6971,6981,7003,7004,7011,7012,7013,7059,7085,7105,7148,7157,7163,7167,7172,7180,7181,7182,7186,7187,7199,7201,7203,7226,7240,7259,7267,7269,7270,7272,7276,7287,7313,7318,7320,7352,7414,7420,7451,7453,7460,7516,7532,7545,7581,7599,7611,7634,7649,7701,7729,7731,7732,7733,7735,7739,7740,7741,7751,7752,7762,7867,7911,7912,7936,7942,7966,7972,7974,7976,7979,7984,7994,7995,8001,8002,8012,8014,8015,8016,8020,8022,8031,8035,8043,8050,8053,8058,8060,8074,8088,8097,8104,8113,8136,8174,8194,8218,8233,8242,8252,8253,8267,8279,8291,8306,8308,8309,8316,8354,8377,8411,8424,8425,8473,8566,8591,8593,8595,8596,8601,8604,8630,8697,8698,8725,8750,8766,8793,8795,8801,8802,8804,8830,8848,8923,8929,8934,9009,9020,9022,9024,9025,9066,9068,9069,9101,9104,9142,9201,9237,9267,9268,9275,9278,9279,9301,9303,9338,9384,9404,9409,9412,9418,9432,9433,9434,9435,9436,9503,9506,9551,9552,9600,9613,9616,9697,9719,9731,9735,9746,9765,9766,9769,9823,9837,9843,9882,9889,9956,9983,9984,
			// 2024/3/21
			1332,1414,1417,1605,1719,1721,1762,1812,1861,1885,1898,1909,1911,1925,1928,1930,1939,1959,1964,1973,2004,2154,2206,2212,2222,2282,2327,2354,2374,2415,2469,2501,2502,2585,2588,2607,2664,2669,2670,2674,2676,2685,2702,2726,2730,2733,2749,2751,2760,2768,2791,2801,2802,2805,2811,2875,2897,2914,2915,2932,3002,3003,3038,3048,3087,3092,3097,3099,3105,3107,3110,3132,3156,3197,3221,3231,3289,3382,3391,3395,3397,3436,3496,3498,3539,3549,3558,3569,3591,3692,3697,3741,3769,3773,3774,3778,3791,3798,3817,3843,3923,3941,3983,3993,4042,4043,4046,4062,4063,4088,4091,4095,4116,4180,4182,4183,4186,4188,4194,4202,4203,4204,4206,4249,4307,4310,4318,4323,4369,4373,4377,4413,4432,4434,4452,4502,4503,4507,4519,4523,4527,4543,4554,4568,4578,4617,4619,4626,4661,4665,4681,4684,4687,4689,4704,4709,4718,4732,4733,4743,4746,4765,4768,4812,4886,4901,4956,4966,4980,5011,5013,5018,5020,5021,5033,5105,5108,5161,5195,5201,5253,5334,5351,5352,5393,5401,5406,5585,5595,5706,5714,5715,5741,5802,5834,5838,5842,5844,5851,5901,5930,6023,6036,6055,6078,6088,6098,6113,6141,6146,6178,6201,6226,6228,6231,6254,6258,6269,6272,6273,6282,6294,6301,6315,6316,6323,6326,6328,6331,6332,6351,6358,6361,6363,6367,6368,6369,6371,6383,6390,6417,6418,6432,6436,6454,6458,6459,6463,6465,6479,6481,6490,6498,6501,6503,6504,6506,6507,6508,6525,6544,6547,6564,6592,6594,6617,6622,6645,6670,6677,6701,6702,6723,6728,6752,6754,6758,6762,6777,6814,6856,6857,6859,6861,6864,6871,6902,6905,6920,6946,6954,6971,6981,6988,7003,7004,7011,7014,7033,7038,7047,7059,7065,7088,7105,7148,7164,7172,7180,7182,7199,7201,7202,7203,7226,7229,7239,7241,7259,7261,7267,7269,7270,7278,7287,7292,7318,7342,7352,7388,7419,7420,7451,7453,7455,7460,7467,7532,7537,7545,7550,7552,7581,7611,7649,7721,7729,7730,7733,7735,7739,7740,7741,7744,7747,7751,7762,7817,7818,7832,7840,7867,7868,7871,7874,7911,7912,7931,7936,7944,7952,7966,7972,7974,7976,7979,7984,7994,7995,8001,8002,8007,8011,8014,8015,8020,8022,8031,8035,8050,8053,8056,8058,8060,8074,8081,8085,8101,8104,8113,8117,8136,8153,8174,8214,8218,8219,8227,8233,8252,8253,8267,8279,8291,8306,8316,8411,8424,8425,8473,8566,8584,8591,8593,8601,8604,8630,8697,8698,8725,8750,8766,8772,8793,8801,8802,8804,8830,8864,8897,8923,8934,9001,9009,9020,9022,9024,9039,9069,9072,9101,9104,9107,9119,9145,9201,9202,9248,9260,9267,9268,9302,9303,9404,9412,9418,9432,9433,9434,9435,9436,9551,9552,9605,9613,9616,9621,9697,9699,9701,9719,9735,9744,9765,9769,9787,9823,9828,9837,9842,9843,9861,9869,9880,9887,9889,9900,9956,9983,9984,
		};
		// skip   T0:23732,  T1:0.195, T2:10.2  #End#
		// noskip T0:116276, T1:0.256, T2:12.9  #End#
		private static readonly int[] OldAnd51List = new int[] {
			6335,6767,6559,7439,7215,6991,7855,6783,2580,1684,2806,1254,806,1686,134,2356,5696,1460,358,136,584,1236,1462,1480,3926,2132,360,790,788,808,1238,566,4765,3629,568,120,792,398,5641,8105,
			3197,342,7936,5645,3631,479,8323,5423,4287,6077,3671,7887,6079,8303,5685,7679,8135,8329,6093,8125,6105,8111,8127,8319,7903,4573,7037,5673,6103,5907,2747,6109,5909,3005,7919,6119,8378,
		};
		private static readonly int[] OldOr51List = new int[] {
			708,2322,1604,24,278,38,1202,4582,5026,3456,4907,6600,4558,6588,7266,3656,4308,4320,6112,6824,8352,1870,7046,6824,4094,6590,3014,6826,72,6826,4893,7706,78,3676,
		};
		private static readonly int[] Old2And51List = new int[7] {
			6130,2796,737,8046,8368,8344,51,
		};
		private static readonly int[] Old2Or51List = new int[14] {
			472,66,1977,2203,1618,7436,4954,2639,52,6717,6433,5059,2998,1069,
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
		};
		private static readonly int[] ConfirmAnds = new int[] {
			/* 旧設定
			6130, // Cond:6130, Score:4880.38, sT:52635, sP:3.1429, sB:0.7679, nT:163309, nP:3.7239, nB:0.7812
			2796, // Cond:2796, Score:4880.38, sT:52635, sP:3.1429, sB:0.7679, nT:163309, nP:3.7239, nB:0.7812
			737, // Cond:737, Score:4880.38, sT:52635, sP:3.1429, sB:0.7679, nT:163309, nP:3.7239, nB:0.7812
			8046, // Cond:8046, Score:4974.193, sT:56628, sP:3.1063, sB:0.7335, nT:175983, nP:3.6702, nB:0.728
			8368, // Cond:8354, Score:4880.38, sT:52635, sP:3.1429, sB:0.7679, nT:163309, nP:3.7239, nB:0.7812
			8344, // Cond:8344, Score:4880.38, sT:52635, sP:3.1429, sB:0.7679, nT:163309, nP:3.7239, nB:0.7812
			51,
			*/
			//AllTrueCondIdx, // 全部を許可するやつ 全スルーチェックとかOrのみチェック用
			//1877,214,1351,4390,7730,7090,7386,6200,6010,1682,8460,5508,4289,8446,2784,2646,6918,1282,446,8182,8430,2695,7388,6065,2487,2806
			6922,5752,6014,8414,1666,2101,182,4356,6823,206,7300,3582,1664,4305,5015,
		};
		private static readonly int[] ConfirmOrs = new int[] {
			/* 旧設定
			472,  // Cond:3444, Score:4839.551, sT:51450, sP:3.1411, sB:0.7736, nT:157219, nP:3.7187, nB:0.8079
			66,   // Cond:5904, Score:4707.361, sT:50150, sP:3.0857, sB:0.7569, nT:161393, nP:3.7145, nB:0.7789
			1977, // Cond:1977, Score:4864.97, sT:51332, sP:3.1188, sB:0.7739, nT:158137, nP:3.6953, nB:0.8106
			2203, // Cond:1083, Score:4876.456, sT:51314, sP:3.1596, sB:0.7885, nT:157830, nP:3.7373, nB:0.7994
			1618, // Cond:1602, Score:4847.739, sT:51510, sP:3.0996, sB:0.7727, nT:157259, nP:3.664, nB:0.7833
			7436, // Cond:8298, Score:4771.598, sT:51574, sP:3.0373, sB:0.745, nT:158351, nP:3.5353, nB:0.7652
			4954, // Cond:7642, Score:4878.302, sT:52784, sP:3.1533, sB:0.7683, nT:163227, nP:3.7368, nB:0.7798
			2639, // Cond:1083, Score:4863.172, sT:52714, sP:3.1514, sB:0.7662, nT:163416, nP:3.7414, nB:0.7782
			52,   // Cond:1424, Score:4813.474, sT:52168, sP:3.1161, sB:0.7588, nT:161722, nP:3.7236, nB:0.7772
			6717, // Cond:6717, Score:4880.38, sT:52635, sP:3.1429, sB:0.7679, nT:163309, nP:3.7239, nB:0.7812
			6433, // Cond:6433, Score:5031.917, sT:54780, sP:3.0866, sB:0.7562, nT:169701, nP:3.6544, nB:0.7707
			5059,
			2998,
			1069,
			*/
			//AllTrueCondIdx, // 全部を許可するやつ Andのみチェック用
			//1602,4068,4042,6056,6058 38290,8294
			4445,1079,4580,4659,3890, // 少ない
			6066,1618,8298,1937,24,528,8203,1251,3234,
			992, //Score:183554, Beny:【Be1】And:-1Or:-1,DAll:-34856,Sum:34856,Sub:183554,SB:0.5679,0:-631(0.018),1:-3245(0.093),2:-7786(0.223),3:-4691(0.135),4:-3588(0.103),5:-14915(0.428),, 【Be2】631,3245,7786,4691,3588,14915, , sT:12500, sP:4.7785, sB:0.5679, nT:34856, nP:4.8579, nB:0.8091,  #End#
			8296, //Score:165393, Beny:【Be1】And:-1Or:992,DAll:-5467,Sum:35354,Sub:165393,SB:0.573,0:-87(0.016),1:-611(0.112),2:-1077(0.197),3:-552(0.101),4:-960(0.176),5:-2180(0.399),, 【Be2】634,3298,7914,4770,3636,15102, , sT:12615, sP:4.7724, sB:0.573, nT:35354, nP:4.8587, nB:0.8032,
			1957,//
			5923,//Score:179760, Beny:【Be1】And:-1,Or:1957,DAll:-2882,Sum:39917,Sub:179760,Sub2:6062,SB:0.5289,0:-20(0.007),1:-320(0.111),2:-672(0.233),3:-357(0.124),4:-460(0.16),5:-1053(0.365),, 【Be2】692,3791,9052,5397,4205,16780, , sT:13990, sP:4.8051, sB:0.5289, nT:39917, nP:4.9273, nB:0.7518,
			6625, //Score:181178, Beny:【Be1】And:-1,Or:5923,DAll:-2035,Sum:40305,Sub:181178,Sub2:6058,SB:0.5343,0:-15(0.007),1:-241(0.118),2:-449(0.221),3:-221(0.109),4:-387(0.19),5:-722(0.355),, 【Be2】698,3819,9154,5466,4230,16938, , sT:14106, sP:4.7863, sB:0.5343, nT:40305, nP:4.9051, nB:0.7526,
			8393, //Score:181729, Beny:【Be1】And:-1,Or:6625,DAll:-3164,Sum:43061,Sub:181729,Sub2:5980,SB:0.5267,0:-25(0.008),1:-393(0.124),2:-716(0.226),3:-341(0.108),4:-615(0.194),5:-1074(0.339),, 【Be2】717,4174,9766,5736,4820,17848, , sT:14612, sP:4.9291, sB:0.5267, nT:43061, nP:5.2358, nB:0.7143,
			2615, //Score:182441, Beny:【Be1】And:-1,Or:8393,DAll:-3271,Sum:43576,Sub:182441,Sub2:5985,SB:0.5255,0:-23(0.007),1:-420(0.128),2:-728(0.223),3:-336(0.103),4:-631(0.193),5:-1133(0.346),, 【Be2】721,4239,9882,5802,4861,18071, , sT:14770, sP:4.9201, sB:0.5255, nT:43576, nP:5.2296, nB:0.7142,
			4762, //Score:182655, Beny:【Be1】And:-1,Or:2615,DAll:-1434,Sum:44495,Sub:182655,Sub2:5984,SB:0.5193,0:-22(0.015),1:-167(0.116),2:-329(0.229),3:-160(0.112),4:-125(0.087),5:-631(0.44),, 【Be2】739,4341,10095,5896,4945,18479, , sT:15072, sP:4.9112, sB:0.5193, nT:44495, nP:5.2276, nB:0.7112,
			6151, //Score:182366, Beny:【Be1】And:-1,Or:4762,DAll:-3548,Sum:47124,Sub:182366,Sub2:5906,SB:0.5105,0:-57(0.016),1:-439(0.124),2:-760(0.214),3:-353(0.099),4:-600(0.169),5:-1339(0.377),, 【Be2】778,4678,10642,6155,5461,19410, , sT:15703, sP:4.9992, sB:0.5105, nT:47124, nP:5.3924, nB:0.686,
			8411, //Score:183085, Beny:【Be1】And:-1,Or:6151,DAll:-2765,Sum:47260,Sub:183085,Sub2:5908,SB:0.5132,0:-39(0.014),1:-348(0.126),2:-588(0.213),3:-271(0.098),4:-541(0.196),5:-978(0.354),, 【Be2】778,4689,10683,6167,5486,19457, , sT:15735, sP:5.0037, sB:0.5132, nT:47260, nP:5.4023, nB:0.6851,
			1160, //Score:182376, Beny:【Be1】And:-1,Or:8411,DAll:-2001,Sum:49125,Sub:182376,Sub2:5861,SB:0.5036,0:-37(0.018),1:-191(0.095),2:-489(0.244),3:-282(0.141),4:-228(0.114),5:-774(0.387),, 【Be2】815,4869,11131,6437,5689,20184, , sT:16281, sP:4.9732, sB:0.5036, nT:49125, nP:5.3762, nB:0.6784,
			3012, //Score:182794, Beny:【Be1】And:-1,Or:1160,DAll:-3629,Sum:50889,Sub:182794,Sub2:5847,SB:0.4959,0:-90(0.025),1:-358(0.099),2:-758(0.209),3:-454(0.125),4:-512(0.141),5:-1457(0.401),, 【Be2】868,5047,11441,6621,5998,20914, , sT:16770, sP:5.0296, sB:0.4959, nT:50889, nP:5.4588, nB:0.6791,  #End#
			3456, //Score:183502, Beny:【Be1】And:-1,Or:3012,DAll:-2054,Sum:51179,Sub:183502,Sub2:5834,SB:0.5,0:-64(0.031),1:-203(0.099),2:-363(0.177),3:-215(0.105),4:-378(0.184),5:-831(0.405),, 【Be2】879,5072,11494,6652,6067,21015, , sT:16847, sP:5.036, sB:0.5, nT:51179, nP:5.4754, nB:0.678,
			1939, //Score:182904, Beny:【Be1】And:-1,Or:3456,DAll:-2692,Sum:53581,Sub:182904,Sub2:5797,SB:0.4873,0:-44(0.016),1:-261(0.097),2:-674(0.25),3:-407(0.151),4:-254(0.094),5:-1052(0.391),, 【Be2】912,5308,12115,7028,6252,21966, , sT:17505, sP:4.9598, sB:0.4873, nT:53581, nP:5.396, nB:0.6715,
			5612, //Score:183454, Beny:【Be1】And:-1,Or:1939,DAll:-2549,Sum:53728,Sub:183454,Sub2:5809,SB:0.4872,0:-34(0.013),1:-249(0.098),2:-643(0.252),3:-399(0.157),4:-201(0.079),5:-1023(0.401),, 【Be2】913,5321,12137,7051,6268,22038, , sT:17547, sP:4.9541, sB:0.4872, nT:53728, nP:5.3915, nB:0.6745,
			4806, //Score:182871, Beny:【Be1】And:-1,Or:5612,DAll:-9881,Sum:63462,Sub:182871,Sub2:5713,SB:0.4454,0:-214(0.022),1:-1278(0.129),2:-1816(0.184),3:-829(0.084),4:-1783(0.18),5:-3961(0.401),, 【Be2】1126,6586,13931,7857,8035,25927, , sT:19654, sP:5.1454, sB:0.4454, nT:63462, nP:5.8206, nB:0.645,
			7910, //Score:183295, Beny:【Be1】And:-1,Or:4806,DAll:-10202,Sum:63930,Sub:183295,Sub2:5709,SB:0.4458,0:-220(0.022),1:-1313(0.129),2:-1908(0.187),3:-870(0.085),4:-1818(0.178),5:-4073(0.399),, 【Be2】1133,6634,14045,7921,8086,26111, , sT:19784, sP:5.1336, sB:0.4458, nT:63930, nP:5.8077, nB:0.6446,
			2086, //Score:183043, Beny:【Be1】And:-1,Or:7910,DAll:-2629,Sum:66091,Sub:183043,Sub2:5682,SB:0.4386,0:-43(0.016),1:-273(0.104),2:-616(0.234),3:-329(0.125),4:-381(0.145),5:-987(0.375),, 【Be2】1169,6859,14547,8186,8416,26914, , sT:20449, sP:5.1241, sB:0.4386, nT:66091, nP:5.8283, nB:0.6351,
	445, //Score:183570, Beny:【Be1】And:-1,Or:2086,DAll:-2180,Sum:66110,Sub:183570,Sub2:5685,SB:0.4405,0:-36(0.017),1:-225(0.103),2:-504(0.231),3:-269(0.123),4:-331(0.152),5:-815(0.374),, 【Be2】1169,6859,14549,8190,8417,26926, , sT:20452, sP:5.1244, sB:0.4405, nT:66110, nP:5.8276, nB:0.6359,
	66, //Score:182628, Beny:【Be1】And:-1,Or:445,DAll:-2772,Sum:68863,Sub:182628,Sub2:5566,SB:0.4421,0:-72(0.026),1:-333(0.12),2:-683(0.246),3:-346(0.125),4:-294(0.106),5:-1044(0.377),, 【Be2】1241,7192,15230,8532,8710,27958, , sT:21965, sP:5.081, sB:0.4421, nT:68863, nP:5.7918, nB:0.6184,
	2722, //Score:183350, Beny:【Be1】And:-1,Or:66,DAll:-2896,Sum:69006,Sub:183350,Sub2:5578,SB:0.443,0:-75(0.026),1:-338(0.117),2:-711(0.246),3:-351(0.121),4:-313(0.108),5:-1108(0.383),, 【Be2】1244,7197,15260,8541,8730,28034, , sT:21993, sP:5.0799, sB:0.443, nT:69006, nP:5.7962, nB:0.6201,
	8165, //Score:181647, Beny:【Be1】And:-1,Or:2722,DAll:-3686,Sum:72549,Sub:181647,Sub2:5494,SB:0.4328,0:-69(0.019),1:-438(0.119),2:-794(0.215),3:-384(0.104),4:-711(0.193),5:-1290(0.35),, 【Be2】1310,7630,16024,8916,9421,29248, , sT:22685, sP:5.1574, sB:0.4328, nT:72549, nP:5.9721, nB:0.5989,
	7995, //Score:182519, Beny:【Be1】And:-1,Or:8165,DAll:-4407,Sum:73413,Sub:182519,Sub2:5484,SB:0.4349,0:-75(0.017),1:-530(0.12),2:-979(0.222),3:-498(0.113),4:-794(0.18),5:-1531(0.347),, 【Be2】1319,7727,16239,9039,9524,29565, , sT:22885, sP:5.1488, sB:0.4349, nT:73413, nP:5.9657, nB:0.595,

		};

		private static readonly int[] KaraConfirmAnds = new int[] {
			//AllTrueCondIdx
			//7181,1587,8297,2723,53,1619,937,7437,2739,993,739,5405,953,6809,7800,3611,3001,1635,7229,747,
		};
		private static readonly int[] KaraConfirmOrs = new int[] {
			// AndCond:1, OrCond:-1, Cond:1830, Score:0.00290070921985816, sT:864, sP:2.934, sB:-4.1574, nT:1551, nP:2.6983, nB:-4.499
			7180,1586,8296,2722,52,1618,936,7436,2738,992,738,5404,952,6808,7801,3610,//3000,1634,//7228,746,
			7004,968,8146,3000,1634,740,68,1937,1880,7228,6433,7839
		};
		private static readonly int[] KouhoAnds = new int[] {
			//AllTrueCondIdx,

	   };
		private static readonly int[] KouhoOrs = new int[] {
			//AllTrueCondIdx-1,

2280, //Score:182176, Beny:【Be1】And:-1,Or:7995,DAll:-1419,Sum:73968,Sub:182176,Sub2:5460,SB:0.4348,0:-23(0.016),1:-159(0.112),2:-372(0.262),3:-196(0.138),4:-147(0.104),5:-522(0.368),, 【Be2】1333,7789,16396,9112,9568,29770, , sT:23025, sP:5.1342, sB:0.4348, nT:73968, nP:5.9563, nB:0.5903,
14, //Score:181942, Beny:【Be1】And:-1,Or:7995,DAll:-1663,Sum:74212,Sub:181942,Sub2:5464,SB:0.4321,0:-35(0.021),1:-171(0.103),2:-426(0.256),3:-214(0.129),4:-169(0.102),5:-648(0.39),, 【Be2】1345,7801,16450,9130,9590,29896, , sT:23146, sP:5.131, sB:0.4321, nT:74212, nP:5.9444, nB:0.593,
2129, //Score:181924, Beny:【Be1】And:-1,Or:7995,DAll:-1510,Sum:74059,Sub:181924,Sub2:5462,SB:0.4329,0:-20(0.013),1:-147(0.097),2:-412(0.273),3:-243(0.161),4:-152(0.101),5:-536(0.355),, 【Be2】1330,7777,16436,9159,9573,29784, , sT:23121, sP:5.1328, sB:0.4329, nT:74059, nP:5.9432, nB:0.5898,
8320, //Score:181800, Beny:【Be1】And:-1,Or:7995,DAll:-1445,Sum:73994,Sub:181800,Sub2:5467,SB:0.4317,0:-21(0.015),1:-164(0.113),2:-357(0.247),3:-185(0.128),4:-199(0.138),5:-519(0.359),, 【Be2】1331,7794,16381,9101,9620,29767, , sT:22984, sP:5.1499, sB:0.4317, nT:73994, nP:5.9663, nB:0.5914,

		};
		private const int AllCond51Num = 3754886; // 2000日*2500銘柄
		private const double AllCond51Ratio = -0.000912;
		public const double PeriodPow = 0.5;
		public const int NoSkipRatio = 8;
		private const bool TestCodeNum = false; // とりあえず30コードだけでテスト実行
		private const bool IsKara = false; // 空売り用
		public const bool IsAndCheck = false; // andチェックかorチェックか
		private const bool IsPro500Only = false;
		private const bool IsPro500AllOnly = true; // 8年分のプロ500のみ
		public static readonly double[] BenScore = new double[6] { 12, 3, 1, 0.3, -1, -3 };
		public const int LowDiffAnd = 700; // Andチェックでスコアにおいて最低の必要Diff数
		public const int LowSumAnd = 20000; // Andチェックでスコアにおいて最低の必要Sum数
		public const int LowCntOr = 1400; // orチェックでスコアにおいて最低の必要数
		public const int IdxTrue = 6;
		public const int IdxBene = 7;
		public const int IdxPeri = 8;
		public const int IdxMax = 9; // noの方は後半


		/** 51条件の全検証(1個スキップも可能) */
		public static void CheckCond51All()
		{
			CheckCond51AllBase(-1, -1);

			/*
			if (IsAndCheck) {
				for (int i = -1; i < confirmAnds.Length; i++) {
					CheckCond51AllBase(confirmAnds, confirmOrs, i, -1);
				}
			} else {
				for (int i = -1; i < confirmOrs.Length; i++) {
					//for (int i = -1; i <= 1; i++) {
					CheckCond51AllBase(confirmAnds, confirmOrs, -1, i);
				}
				//CheckCond51AllBase(confirmAnds, confirmOrs, -1, 8);
			}
			*/
		}
		private static void CheckCond51AllBase(int andSkip, int orSkip)
		{
			(int[] confirmAnds, int[] confirmOrs) = GetConfirms();

			bool isOrOkForce = IsAndCheck && confirmOrs.Length == 0 && KouhoOrs.Length == 0; // orチェックを強制でOKにしておく

			List<string> codeList = CsvControll.GetCodeList();
			if (TestCodeNum) codeList = codeList.GetRange(0, 30);

			(Dictionary<string, HashSet<string>> beforeNotAnd, Dictionary<string, HashSet<string>> beforeOr) = GetBeforeInfo(codeList, confirmAnds, confirmOrs, andSkip, orSkip);
			Dictionary<string, HashSet<string>>[] beforeNotAndKouho = new Dictionary<string, HashSet<string>>[KouhoAnds.Length];
			Dictionary<string, HashSet<string>>[] beforeOrKouho = new Dictionary<string, HashSet<string>>[KouhoOrs.Length];
			foreach (string symbol in codeList) {
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

			int[] notCond = new int[NotCond.Length + confirmOrs.Length + confirmAnds.Length];
			NotCond.CopyTo(notCond, 0);
			confirmOrs.CopyTo(notCond, NotCond.Length);
			confirmAnds.CopyTo(notCond, NotCond.Length + confirmOrs.Length);

			Dictionary<string, HashSet<DateTime>> pro500Date = Getpro500Date();

			int[,,] dataAll = new int[kouhoNum, condNum(), IdxMax * 2];
			int[] confirmBeny = new int[6];
			foreach (string symbol in codeList) {
				if (IsPro500AllOnly && !pro500Date.ContainsKey(symbol)) continue;
				HashSet<string> pro500Ds = GetPro500Ds(pro500Date, symbol);
				(Dictionary<string, int> benefits, Dictionary<string, int> havePeriods) = GetBenefitData(symbol);

				foreach (var info in beforeOr[symbol]) {
					if (!beforeNotAnd[symbol].Contains(info) && benefits.ContainsKey(info) && (!IsPro500AllOnly || pro500Ds.Contains(info))) {
						confirmBeny[GetBeny(benefits[info])]++;
					}
				}

				for (int diffDayIdx = 0; diffDayIdx < diffDayList.Length; diffDayIdx++) {
					for (int ratioIdx = 0; ratioIdx < ratioList.Length; ratioIdx++) {
						List<string[]> cond51All = CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx);
						for (int pIdx = 0; pIdx < periodCntList.GetLength(0); pIdx++) {
							foreach (bool isT in new bool[2] { true, false }) {
								int condIdx = GetCondIdx(pIdx, ratioIdx, diffDayIdx, isT);
								if (Array.IndexOf(notCond, condIdx) >= 0) continue;
								int[] nowHaves = new int[kouhoNum];
								for (int c = 0; c < cond51All.Count; c++) {
									string[] cond51 = cond51All[c];

									if (IsPro500AllOnly && !pro500Ds.Contains(cond51[0])) continue;
									if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
									if (IsAndCheck && (cond51[pIdx + 1] == "1") != isT) continue;
									if (!benefits.ContainsKey(cond51[0])) continue;
									bool isOrCheck = isOrOkForce || (!IsAndCheck && (cond51[pIdx + 1] == "1") == isT) || beforeOr[symbol].Contains(cond51[0]);

									if (KouhoAnds.Length > 0) {
										if (!isOrCheck) continue;
										for (int i = 0; i < KouhoAnds.Length; i++) {
											if (beforeNotAndKouho[i][symbol].Contains(cond51[0])) continue;
											dataAll[i, condIdx, IdxMax + GetBeny(benefits[cond51[0]])]++;
											dataAll[i, condIdx, IdxMax + IdxTrue]++;
											dataAll[i, condIdx, IdxMax + IdxBene] += benefits[cond51[0]];
											dataAll[i, condIdx, IdxMax + IdxPeri] += havePeriods[cond51[0]];
											if (nowHaves[i] > c) continue;
											dataAll[i, condIdx, GetBeny(benefits[cond51[0]])]++;
											dataAll[i, condIdx, IdxTrue]++;
											dataAll[i, condIdx, IdxBene] += benefits[cond51[0]];
											dataAll[i, condIdx, IdxPeri] += havePeriods[cond51[0]];
											nowHaves[i] = c + havePeriods[cond51[0]] + 1;
										}
									} else if (KouhoOrs.Length > 0) {
										// 残るはORチェック
										for (int i = 0; i < KouhoOrs.Length; i++) {
											if (!isOrCheck && !beforeOrKouho[i][symbol].Contains(cond51[0])) continue;
											dataAll[i, condIdx, IdxMax + GetBeny(benefits[cond51[0]])]++;
											dataAll[i, condIdx, IdxMax + IdxTrue]++;
											dataAll[i, condIdx, IdxMax + IdxBene] += benefits[cond51[0]];
											dataAll[i, condIdx, IdxMax + IdxPeri] += havePeriods[cond51[0]];
											if (nowHaves[i] > c) continue;
											dataAll[i, condIdx, GetBeny(benefits[cond51[0]])]++;
											dataAll[i, condIdx, IdxTrue]++;
											dataAll[i, condIdx, IdxBene] += benefits[cond51[0]];
											dataAll[i, condIdx, IdxPeri] += havePeriods[cond51[0]];
											nowHaves[i] = c + havePeriods[cond51[0]] + 1;
										}
									}
								}
							}
						}
					}
				}
				Common.DebugInfo("CheckCond51AllSymbol", symbol);
			}

			// 並び変えるか
			HashSet<CondRes> condResAll = new HashSet<CondRes>();
			string result = "";
			for (int i = 0; i < kouhoNum; i++) {
				HashSet<CondRes> condResList = new HashSet<CondRes>();
				for (int j = 0; j < condNum(); j++) {
					if (dataAll[i, j, IdxMax + IdxTrue] == 0) continue;
					int[] info = new int[IdxMax * 2];
					for (int k = 0; k < IdxMax * 2; k++) info[k] = dataAll[i, j, k];
					CondRes condRes = new CondRes(
						IsAndCheck ? i : -1, !IsAndCheck ? i : -1, IsAndCheck ? kouhoList[i] : -1, !IsAndCheck ? kouhoList[i] : -1,
						j, info, confirmBeny
					);
					condResList.Add(condRes);
					condResAll.Add(condRes);
				}

				result = "";
				foreach (CondRes c in condResList.OrderByDescending(c => c.SortNum()).Take(10)) result += c.DispRes();
				Common.DebugInfo("LowScoreRank22", kouhoList[i], result);
			}

			result = "";
			foreach (CondRes c in condResAll.OrderByDescending(c => c.SortNum()).Take(20)) result += c.DispRes();
			Common.DebugInfo("LowScoreRankAll", result);
		}


		// confirmAnds,confirmOrsのうち一個のcondを抜いて、新しいものを一個追加した際の検証を全通り(Kouhoは使わない)
		public static void CheckCond51All2()
		{
			CheckCond51AllBase2(false);
			CheckCond51AllBase2(true);
		}
		private static void CheckCond51AllBase2(bool isAndCheck)
		{
			List<string> codeList = CsvControll.GetCodeList();
			if (TestCodeNum) codeList = codeList.GetRange(0, 30);
			(int[] confirmAnds, int[] confirmOrs) = GetConfirms();

			int kouhoNum = confirmOrs.Length + confirmAnds.Length + 1;

			int[,,] dataAll = new int[kouhoNum, condNum(), IdxMax * 2];
			int[] confirmBeny = new int[6];
			foreach (string symbol in codeList) {
				HashSet<string>[] beforeNotAndKouho = new HashSet<string>[confirmAnds.Length + 1];
				for (int i = 0; i < confirmAnds.Length; i++) {
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(confirmAnds[i]);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if ((cond51[pIdx + 1] == "1") != isT) {
							for (int i2 = 0; i2 < confirmAnds.Length + 1; i2++) {
								if (i == i2) continue; // 一致するもの(はぶくやつ)はスキップ
								if (beforeNotAndKouho[i2] == null) beforeNotAndKouho[i2] = new HashSet<string>();
								beforeNotAndKouho[i2].Add(cond51[0]);
							}
						}
					}
				}

				HashSet<string>[] beforeOrKouho = new HashSet<string>[confirmOrs.Length + 1];
				for (int i = 0; i < confirmOrs.Length; i++) {
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(confirmOrs[i]);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if ((cond51[pIdx + 1] == "1") == isT) {
							for (int i2 = 0; i2 < confirmOrs.Length + 1; i2++) {
								if (i == i2) continue; // 一致するもの(はぶくやつ)はスキップ
								if (beforeOrKouho[i2] == null) beforeOrKouho[i2] = new HashSet<string>();
								beforeOrKouho[i2].Add(cond51[0]);
							}
						}
					}
				}

				(Dictionary<string, int> benefits, Dictionary<string, int> havePeriods) = GetBenefitData(symbol);

				for (int diffDayIdx = 0; diffDayIdx < diffDayList.Length; diffDayIdx++) {
					for (int ratioIdx = 0; ratioIdx < ratioList.Length; ratioIdx++) {
						List<string[]> cond51All = CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx);
						for (int pIdx = 0; pIdx < periodCntList.GetLength(0); pIdx++) {
							foreach (bool isT in new bool[2] { true, false }) {
								int condIdx = GetCondIdx(pIdx, ratioIdx, diffDayIdx, isT);
								if (Array.IndexOf(NotCond, condIdx) >= 0) continue;
								int[] nowHaves = new int[kouhoNum];
								for (int c = 0; c < cond51All.Count; c++) {
									string[] cond51 = cond51All[c];
									if (!benefits.ContainsKey(cond51[0])) continue;

									if (isAndCheck && (cond51[pIdx + 1] == "1") != isT) continue;
									bool isOrCheck = !isAndCheck && (cond51[pIdx + 1] == "1") == isT;

									for (int i = 0; i < kouhoNum; i++) {
										int andIdx = i >= confirmAnds.Length ? confirmAnds.Length : i;
										int orIdx = i < confirmAnds.Length ? confirmOrs.Length : i - confirmAnds.Length;
										HashSet<string> beAnd = beforeNotAndKouho[andIdx] ?? new HashSet<string>();
										HashSet<string> beOr = beforeOrKouho[orIdx] ?? new HashSet<string>();
										if (beAnd.Contains(cond51[0]) || (!beOr.Contains(cond51[0]) && !isOrCheck)) continue;
										dataAll[i, condIdx, IdxMax + GetBeny(benefits[cond51[0]])]++;
										dataAll[i, condIdx, IdxMax + IdxTrue]++;
										dataAll[i, condIdx, IdxMax + IdxBene] += benefits[cond51[0]];
										dataAll[i, condIdx, IdxMax + IdxPeri] += havePeriods[cond51[0]];

										if (nowHaves[i] > c) continue;
										dataAll[i, condIdx, GetBeny(benefits[cond51[0]])]++;
										dataAll[i, condIdx, IdxTrue]++;
										dataAll[i, condIdx, IdxBene] += benefits[cond51[0]];
										dataAll[i, condIdx, IdxPeri] += havePeriods[cond51[0]];
										nowHaves[i] = c + havePeriods[cond51[0]] + 1;
									}
								}
							}
						}
					}
				}
				Common.DebugInfo("CheckCond51AllBase2Symbol", symbol, DateTime.Now);
			}

			// 並び変えるか
			HashSet<CondRes> condResAll = new HashSet<CondRes>();
			string result = "";
			for (int i = 0; i < kouhoNum; i++) {
				int andIdx = i >= confirmAnds.Length ? confirmAnds.Length : i;
				int orIdx = i < confirmAnds.Length ? confirmOrs.Length : i - confirmAnds.Length;

				HashSet<CondRes> condResList = new HashSet<CondRes>();
				for (int j = 0; j < condNum(); j++) {
					if (dataAll[i, j, IdxMax + IdxTrue] == 0) continue;
					int[] info = new int[IdxMax * 2];
					for (int k = 0; k < IdxMax * 2; k++) info[k] = dataAll[i, j, k];
					CondRes condRes = new CondRes(
						andIdx, orIdx, andIdx == confirmAnds.Length ? -1 : confirmAnds[andIdx], orIdx == confirmOrs.Length ? -1 : confirmOrs[orIdx],
						j, info, confirmBeny
					);
					condResList.Add(condRes);
					condResAll.Add(condRes);
				}

				result = "";
				foreach (CondRes c in condResList.OrderByDescending(c => c.SortNum()).Take(10)) result += c.DispRes();
				Common.DebugInfo("LowScoreRank", isAndCheck, andIdx, orIdx, result);
			}

			result = "";
			foreach (CondRes c in condResAll.OrderByDescending(c => c.SortNum()).Take(30)) result += c.DispRes();
			Common.DebugInfo("LowScoreRankAll", isAndCheck, result);
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
		private static (int[], int[]) GetConfirms()
		{
			int[] confirmAnds = ConfirmAnds; int[] confirmOrs = ConfirmOrs;

			if (IsKara) { confirmAnds = KaraConfirmAnds; confirmOrs = KaraConfirmOrs; }
			if (false) { confirmAnds = OldAnd51List; confirmOrs = OldOr51List; }
			if (false) { confirmAnds = Old2And51List; confirmOrs = Old2Or51List; }
			if (false) { confirmAnds = new int[0] { }; confirmOrs = new int[1] { AllTrueCondIdx }; }
			return (confirmAnds, confirmOrs);
		}
		private static int GetBeny(int benefit)
		{
			if (benefit >= 4) return 5;
			if (benefit >= 1) return 4;
			if (benefit >= -2) return 3;
			if (benefit >= -5) return 2;
			if (benefit >= -8) return 1;
			return 0;
		}
		private static Dictionary<string, HashSet<DateTime>> Getpro500Date()
		{
			List<string>[] pro500All = CsvControll.GetPro500All();
			Dictionary<string, HashSet<DateTime>> pro500Date = new Dictionary<string, HashSet<DateTime>>();
			foreach (List<string> info in pro500All) {
				DateTime date = DateTime.Now;
				for (int i = 0; i < info.Count; i++) {
					if (i == 0) {
						date = DateTime.Parse(info[0]);
					} else {
						if (!pro500Date.ContainsKey(info[i])) pro500Date[info[i]] = new HashSet<DateTime>();
						pro500Date[info[i]].Add(date);
					}
				}
			}
			return pro500Date;
		}
		private static HashSet<string> GetPro500Ds(Dictionary<string, HashSet<DateTime>> pro500Date, string symbol)
		{
			HashSet<string> pro500Ds = new HashSet<string>();
			if (IsPro500AllOnly) {
				if (!pro500Date.ContainsKey(symbol)) return pro500Ds;
				foreach (DateTime info in pro500Date[symbol]) {
					int idx = Common.GetDateIdx(info);
					for (int i = 0; i < 65; i++) pro500Ds.Add(Common.GetDateByIdx(idx + i).ToString(CsvControll.DFORM));
				}
			}
			return pro500Ds;
		}
		private static (Dictionary<string, int>, Dictionary<string, int>) GetBenefitData(string symbol)
		{
			Dictionary<string, int> benefits = new Dictionary<string, int>();
			Dictionary<string, int> havePeriods = new Dictionary<string, int>();
			foreach (string[] benefitInfo in CsvControll.GetBenefitAll(IsKara ? "Kara_" + symbol : symbol)) {
				benefits[benefitInfo[0]] = Int32.Parse(benefitInfo[1]);
				havePeriods[benefitInfo[0]] = Int32.Parse(benefitInfo[2]);
			}
			return (benefits, havePeriods);
		}
		private static (Dictionary<string, HashSet<string>>, Dictionary<string, HashSet<string>>) GetBeforeInfo(List<string> codeList, int[] confirmAnds, int[] confirmOrs, int andSkip, int orSkip)
		{
			Dictionary<string, HashSet<string>> beforeNotAnd = new Dictionary<string, HashSet<string>>();
			Dictionary<string, HashSet<string>> beforeOr = new Dictionary<string, HashSet<string>>();
			foreach (string symbol in codeList) {
				beforeNotAnd[symbol] = new HashSet<string>();
				for (int i = 0; i < confirmAnds.Length; i++) {
					if (andSkip == i) continue;
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(confirmAnds[i]);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if ((cond51[pIdx + 1] == "1") != isT) beforeNotAnd[symbol].Add(cond51[0]);
					}
				}
				beforeOr[symbol] = new HashSet<string>();
				for (int i = 0; i < confirmOrs.Length; i++) {
					if (orSkip == i) continue;
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(confirmOrs[i]);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
						if ((cond51[pIdx + 1] == "1") == isT) beforeOr[symbol].Add(cond51[0]);
					}
				}
			}
			return (beforeNotAnd, beforeOr);
		}



		// ささっと全体のスコアを調べる
		public static void DebugCheckCond51Score()
		{
			if (true) {
				DebugCheckCond51ScoreBase();
			} else {
				// AndのフルチェックをするときはconfirmOrsを空配列にする
				for (int i = -1; i < ConfirmOrs.Length; i++) DebugCheckCond51ScoreBase(-1, i);
				for (int i = -1; i < ConfirmAnds.Length; i++) DebugCheckCond51ScoreBase(i, -1);
			}

		}
		private static void DebugCheckCond51ScoreBase(int andSkip = -1, int orSkip = -1)
		{
			(int[] confirmAnds, int[] confirmOrs) = GetConfirms();

			bool isAllScore = false; // Andチェック・Orチェックを無視する
			bool isOrOkForce = confirmOrs.Length == 0; // orチェックを強制でOKにしておく
			List<string> codeList = CsvControll.GetCodeList();
			if (IsPro500Only) codeList = codeList.FindAll(c => Array.IndexOf(OldPro500, Int32.Parse(c)) >= 0);

			(Dictionary<string, HashSet<string>> beforeNotAnd, Dictionary<string, HashSet<string>> beforeOr) = GetBeforeInfo(codeList, confirmAnds, confirmOrs, andSkip, orSkip);

			Dictionary<string, HashSet<DateTime>> pro500Date = Getpro500Date();

			int debug = 0;

			int maxDay = 10;

			int[] info = new int[IdxMax * 2];
			int numAll = 0;
			Dictionary<int, int> benefitNum = new Dictionary<int, int>();
			foreach (string symbol in codeList) {
				HashSet<string> pro500Ds = GetPro500Ds(pro500Date, symbol);
				if (IsPro500AllOnly) {
					if (!pro500Date.ContainsKey(symbol)) continue;
					debug += pro500Ds.Count;
				}

				(Dictionary<string, int> benefits, Dictionary<string, int> havePeriods) = GetBenefitData(symbol);

				int nowHaves = 0;
				(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(AllTrueCondIdx);
				List<string[]> list = CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx);
				for (int i = 0; i < list.Count; i++) {
					string[] cond51 = list[i];
					//if (IsPro500Only && (Common.NewD2(DateTime.Parse(cond51[0]), startDate) || Common.NewD2(endDate, DateTime.Parse(cond51[0])))) continue;
					if (IsPro500AllOnly && !pro500Ds.Contains(cond51[0])) continue;
					if (!benefits.ContainsKey(cond51[0])) continue;

					numAll++;
					if (!isAllScore) {
						if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
						if (!isOrOkForce && !beforeOr[symbol].Contains(cond51[0])) continue;
					}

					if (!benefitNum.ContainsKey(benefits[cond51[0]])) benefitNum[benefits[cond51[0]]] = 0;
					benefitNum[benefits[cond51[0]]]++;

					info[IdxMax + GetBeny(benefits[cond51[0]])]++;
					info[IdxMax + IdxTrue]++;
					info[IdxMax + IdxBene] += benefits[cond51[0]];
					info[IdxMax + IdxPeri] += havePeriods[cond51[0]];

					if (nowHaves > i) continue;

					info[GetBeny(benefits[cond51[0]])]++;
					info[IdxTrue]++;
					info[IdxBene] += benefits[cond51[0]];
					info[IdxPeri] += havePeriods[cond51[0]];
					nowHaves = i + havePeriods[cond51[0]] + 1;
				}
			}

			int[] confirmBeny = new int[6];
			CondRes condRes = new CondRes(
				andSkip, orSkip,
				andSkip >= 0 ? confirmAnds[andSkip] : -1, orSkip >= 0 ? confirmOrs[orSkip] : -1,
				0, info, confirmBeny
			);

			//string res = "";
			//foreach (KeyValuePair<int, int> pair in benefitNum) res += pair.Key + ":" + pair.Value + ", ";
			//Common.DebugInfo(res);
			//int idx = andSkip == -1 ? confirmOrs[orSkip] : confirmAnds[andSkip];
			Common.DebugInfo("DebugCheckCond51", debug, condRes.DispRes());
		}




		private static readonly int[] ConfirmAndsTrue = new int[7] {
			6130,2796,737,8046,8368,8344,51,
		};
		private static readonly int[] ConfirmOrsTrue = new int[14] {
			472,66,1977,2203,1618,7436,4954,2639,52,6717,6433,5059,2998,1069,
		};
		/** 新しく作った条件一覧 */
		public static List<string[]> GetNewConditions()
		{
			List<string[]> res = new List<string[]>();

			for (int i = 0; i <= 1; i++) {
				foreach (int cond in (i == 0 ? ConfirmOrsTrue : ConfirmAndsTrue)) {
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(cond);
					res.Add(new string[7] {
						i.ToString(), // and条件かor条件か
						"51",
						periodCntList[pIdx,0].ToString(), // c1:1,3,6,10,20,30,50
						diffDayList[diffDayIdx].ToString(), //c3:1,3,6,10,20,30,50,70
						periodCntList[pIdx,1].ToString(), // d1:1,2,3,4,5,6 いやc1に応じる感じか
						isT ? "1" : "0", // 満たす満たさない
						ratioList[ratioIdx].ToString(), // a1: 0.65,0.75,0.8,0.85,0.9,0.95,1,1.05,1.1,1.17,1.25,1.35,1.5,1.7
					});
				}
			}

			return res;
		}


		/** 日経平均の激減した際の500の急上昇チェック */
		public static void JapanDownCheck()
		{
			// とりあえず2.5%上昇？を探す
			foreach (string[] info in CsvControll.GetJapanInfo()) {

			}
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



		public static readonly int[][] japanScore5 = new int[4][] {
			new int[] {3359,3486,4019},
			new int[] {2375,2924,4094},
			new int[] {3526,1877},
			new int[] {1767},
		};
		public static readonly int[][] japanScore4 = new int[4][] {
			new int[] {3359,3486},
			new int[] {3562,2915,2375,1396,2924,4094},
			new int[] {3526,1877,1928},
			new int[] {1767,1822},
		};
		public static readonly int[][] japanScore3 = new int[4][] {
			new int[] {3359,3486,189},
			new int[] {3501,3562,2915,2375},
			new int[] {3526,1877},
			new int[] {1767,1822},
		};
		public static readonly int[][] japanScore2 = new int[4][] {
			new int[] {3359,3486},
			new int[] {3501,3562,2915,2375},
			new int[] {3526,1877,1928},
			new int[] {1767,1822},
		};
		public static readonly int[][] japanScore1 = new int[4][] {
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


	}


	class CondRes
	{
		public int andIdx, orIdx, andCond, orCond, condIdx = -1, noTrue = 0, skipTrue = 0;
		public double noBenefit = 0, skipBenefit = 0, noPeriod = 0, skipPeriod = 0;
		public double[] scores = new double[4];
		public double subScore, subScore2 = 0;
		public int[] noBenys = new int[6];
		public int[] skipBenys = new int[6];
		public int[] confirmBenys = new int[6];
		public CondRes() { }
		public CondRes(int andIdx, int orIdx, int andCond, int orCond, int condIdx, int[] info, int[] confirmBeny)
		{
			this.andIdx = andIdx; this.orIdx = orIdx; this.andCond = andCond; this.orCond = orCond; this.condIdx = condIdx;

			skipTrue = info[Condtions.IdxTrue];
			skipBenefit = Common.Round((double)info[Condtions.IdxBene] / (double)skipTrue, 4);
			skipPeriod = Common.Round((double)info[Condtions.IdxPeri] / (double)skipTrue, 4);
			noTrue = info[Condtions.IdxTrue + Condtions.IdxMax];
			noBenefit = Common.Round((double)info[Condtions.IdxBene + Condtions.IdxMax] / (double)noTrue, 4);
			noPeriod = Common.Round((double)info[Condtions.IdxPeri + Condtions.IdxMax] / (double)noTrue, 4);
			for (int k = 0; k < 6; k++) {
				skipBenys[k] = info[k];
				noBenys[k] = info[k + Condtions.IdxMax];
			}
			this.confirmBenys = confirmBeny;
			for (int k = 0; k < 4; k++) { SetScore(k); }
		}

		public void SetScore(int i)
		{
			/*
			if (i == 0) {
				if (noBenefit == 0 || noTrue == 0 || noRatio == 0) {
					scores[i] = 0; return;
				}
				scores[i] = -(noBenefit * Math.Pow(noTrue, 0.9) * Math.Pow(noRatio, 2.5));
				//7004, //Score:7193.80390891577, sT:75200, sP:7.715, sB:-0.9481, sR:0.3212, nT:362778, nP:7.9275, nB:-1.6159, nR:0.3708
				return;
			}
			if (i == 1) {
				if (noBenefit == 0 || noTrue == 0 || noRatio == 0) {
					scores[i] = 0; return;
				}
				scores[i] = -(noBenefit * Math.Pow(noTrue, 1.5) * Math.Pow(noRatio, 3.5));
				return;
			}
			*/
			if (i == 3) {
				int confirmSum = confirmBenys.Sum(); int noSum = noBenys.Sum();
				int diffAll = confirmSum - noSum;
				scores[i] = 0; subScore = 0; subScore2 = 0;
				if (Condtions.IsAndCheck && (diffAll <= Condtions.LowDiffAnd || noSum <= Condtions.LowSumAnd)) { scores[i] = -999; return; } // とりあえず300
				if (!Condtions.IsAndCheck && -diffAll <= Condtions.LowCntOr) { scores[i] = -999; return; } // とりあえず300

				for (int beny = 0; beny < 6; beny++) {
					int diff = confirmBenys[beny] - noBenys[beny];
					scores[i] += (double)diff / diffAll * Condtions.BenScore[beny];
					subScore2 += (double)noBenys[beny] / noSum * Condtions.BenScore[beny];
				}
				scores[i] = Common.Round(scores[i], 3);
				subScore = Common.Round((subScore2 * -10000) * Math.Pow(noSum, 0.35) * Math.Pow(Math.Max(skipBenefit, 0.000001), 0.5));
				subScore2 = Common.Round(subScore2 * -10000);
				return;
			}

			double ratio = i == 0 ? 0.8 : i == 1 ? 0.85 : i == 2 ? 0.9 : 0.95;
			scores[i] = Common.Round(Math.Pow(skipTrue, ratio) * skipBenefit / Math.Pow(skipPeriod, Condtions.PeriodPow)
			  + Math.Pow(noTrue, ratio) * noBenefit / Math.Pow(noPeriod, Condtions.PeriodPow) / Condtions.NoSkipRatio, 3);
		}
		public double SortNum()
		{
			return subScore;
			return Condtions.IsAndCheck ? scores[3] : subScore;
		}

		public void SetNoBeny(int beny, int num, int confirmBeny)
		{
			noBenys[beny] = num; confirmBenys[beny] = confirmBeny;
		}
		public void SetSkipBeny(int beny, int num) { skipBenys[beny] = num; }

		public string DispRes()
		{
			return "\n" + condIdx + ", //Score:" + SortNum() + ", Beny:" + DispBeny()
				+ ", sT:" + skipTrue + ", sP:" + skipPeriod + ", sB:" + skipBenefit
				+ ", nT:" + noTrue + ", nP:" + noPeriod + ", nB:" + noBenefit + ",";
			/*
				result += "\nAndCond:" + c.andCond + ", OrCond:" + c.orCond + ", Cond:" + c.condIdx + ", 
			 */

		}
		public string DispBeny()
		{
			string res1 = "【Be1】"; string res2 = "【Be2】";
			int confirmSum = confirmBenys.Sum(); int noSum = noBenys.Sum();
			//if (confirmSum == 0) return res1 + ", " + res2;
			int difdAll = confirmSum - noSum;
			res1 += "And:" + andCond + ",Or:" + orCond + ",DAll:" + difdAll + ",Sum:" + noSum + ",Sub:" + subScore + ",Sub2:" + subScore2 + ",SB:" + skipBenefit + ",";
			if (difdAll == 0) return res1 + ", " + res2;
			for (int beny = 0; beny < 6; beny++) {
				//if (confirmBenys[beny] == 0) continue;
				int diff = confirmBenys[beny] - noBenys[beny];
				res1 += beny + ":" + diff + "(" + (Common.Round((double)diff / difdAll, 3)) + "),";
				//res2 += beny + ":" + noBenys[beny] + "/" + confirmBenys[beny] + "/" + skipBenys[beny] + ",";
				res2 += noBenys[beny] + ",";
			}
			return res1 + ", " + res2 + " ";
		}
	}

}
