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


		private const bool IsAndCheck = true; // andチェックかorチェックか
		private const bool IsPro500Only = false;
		private const int AllTrueCondIdx = 1;
		private static readonly int[] OldPro500 = new int[] {
		1332,1333,1414,1417,1605,1662,1719,1720,1721,1812,1820,1832,1861,1911,1925,1928,1930,1934,1941,1959,1961,1964,1965,1969,1973,1975,2121,2124,2146,2154,2212,2216,2229,2327,2337,2501,2502,2503,2579,2585,2670,2674,2678,2685,2702,2749,2751,2760,2768,2782,2791,2801,2802,2805,2809,2811,2871,2875,2914,2915,2928,3002,3003,3038,3048,3050,3064,3075,3076,3086,3092,3093,3097,3099,3104,3107,3110,3132,3179,3196,3231,3289,3302,3355,3374,3382,3388,3401,3405,3407,3433,3434,3436,3445,3480,3498,3539,3569,3591,3608,3612,3679,3692,3733,3774,3791,3864,3901,3941,3964,3993,3994,4004,4022,4028,4042,4043,4045,4047,4063,4088,4091,4095,4107,4114,4116,4118,4182,4183,4186,4188,4189,4194,4203,4204,4206,4208,4212,4216,4218,4228,4275,4307,4369,4373,4374,4401,4413,4417,4419,4452,4486,4493,4502,4507,4519,4526,4543,4568,4578,4611,4617,4619,4661,4676,4681,4684,4687,4704,4725,4733,4743,4746,4751,4755,4765,4768,4886,4901,4956,4972,4980,4992,5013,5019,5020,5027,5101,5105,5108,5110,5161,5191,5201,5254,5310,5333,5334,5344,5384,5393,5401,5411,5595,5703,5706,5711,5713,5714,5715,5741,5801,5802,5803,5834,5838,5842,5857,5911,5943,5975,5991,6055,6062,6088,6098,6113,6135,6140,6141,6146,6178,6196,6201,6228,6254,6255,6258,6266,6269,6272,6273,6278,6282,6294,6301,6305,6315,6323,6326,6330,6339,6357,6361,6363,6367,6368,6369,6370,6371,6376,6383,6395,6432,6454,6458,6463,6465,6471,6472,6479,6480,6481,6486,6490,6501,6503,6504,6506,6508,6516,6524,6526,6544,6547,6563,6594,6670,6701,6702,6723,6724,6728,6741,6752,6758,6762,6777,6787,6841,6857,6861,6871,6877,6902,6920,6946,6954,6971,6981,7003,7004,7011,7012,7013,7059,7085,7105,7148,7157,7163,7167,7172,7180,7181,7182,7186,7187,7199,7201,7203,7226,7240,7259,7267,7269,7270,7272,7276,7287,7313,7318,7320,7352,7414,7420,7451,7453,7460,7516,7532,7545,7581,7599,7611,7634,7649,7701,7729,7731,7732,7733,7735,7739,7740,7741,7751,7752,7762,7867,7911,7912,7936,7942,7966,7972,7974,7976,7979,7984,7994,7995,8001,8002,8012,8014,8015,8016,8020,8022,8031,8035,8043,8050,8053,8058,8060,8074,8088,8097,8104,8113,8136,8174,8194,8218,8233,8242,8252,8253,8267,8279,8291,8306,8308,8309,8316,8354,8377,8411,8424,8425,8473,8566,8591,8593,8595,8596,8601,8604,8630,8697,8698,8725,8750,8766,8793,8795,8801,8802,8804,8830,8848,8923,8929,8934,9009,9020,9022,9024,9025,9066,9068,9069,9101,9104,9142,9201,9237,9267,9268,9275,9278,9279,9301,9303,9338,9384,9404,9409,9412,9418,9432,9433,9434,9435,9436,9503,9506,9551,9552,9600,9613,9616,9697,9719,9731,9735,9746,9765,9766,9769,9823,9837,9843,9882,9889,9956,9983,9984,
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
		private static readonly int[] ConfirmAnds = new int[6] {
			2806, // T2:4353.954 , T3:43200 , T4:0.76 , T5:3.43 , T6:130514 , T7:0.88 , T8:3.96
			5100, // T2:4393.302 , T3:43836 , T4:0.76 , T5:3.43 , T6:131573 , T7:0.88 , T8:3.97
			2808, // T2:4605.062 , T3:45670 , T4:0.76 , T5:3.41 , T6:139284 , T7:0.88 , T8:3.95
			126, // T2:4352.487 , T3:44426 , T4:0.75 , T5:3.43 , T6:134711 , T7:0.85 , T8:3.96
			8354, // T2:4217.273 , T3:46612 , T4:0.71 , T5:3.49 , T6:142942 , T7:0.8 , T8:4.08
			8344, // T2:4201.974 , T3:46693 , T4:0.74 , T5:3.72 , T6:136993 , T7:0.85 , T8:4.15
		};
		private static readonly int[] ConfirmOrs = new int[8] {
			472, // T3:4182.689 , T4:40694 , T5:0.78 , T6:3.47 , T7:123741 , T8:0.89 , T9:4.01
			66, // T3:4177.842 , T4:38963 , T5:0.79 , T6:3.38 , T7:121832 , T8:0.9 , T9:3.97
			1065, // T3:4338.98 , T4:41286 , T5:0.79 , T6:3.42 , T7:123811 , T8:0.91 , T9:3.95
			6851, // T3:4351.649 , T4:40931 , T5:0.81 , T6:3.45 , T7:121676 , T8:0.92 , T9:3.97
			1618, // T3:4395.805 , T4:41365 , T5:0.83 , T6:3.34 , T7:121683 , T8:0.86 , T9:3.89
			7436, // T3:4233.446 , T4:37492 , T5:0.85 , T6:3.37 , T7:104561 , T8:0.96 , T9:3.79
			5388, // T3:4322.379 , T4:42183 , T5:0.78 , T6:3.45 , T7:126578 , T8:0.89 , T9:3.99
			7599, // T3:4376.87 , T4:42214 , T5:0.79 , T6:3.44 , T7:126844 , T8:0.89 , T9:3.97
		};
		private static readonly int[] KouhoAnds = new int[] {
			AllTrueCondIdx
		};
		private static readonly int[] KouhoOrs = new int[] {
			//AllTrueCondIdx-1

		};
		private const int AllCond51Num = 3754886; // 2000日*2500銘柄
		private const double AllCond51Ratio = -0.000912;
		private const double PeriodPow = 0.65;
		private const int NoSkipRatio = 8;
		/** 51条件の全検証 */
		public static void CheckCond51All()
		{
			int[] confirmAnds =  ConfirmAnds;
			int[] confirmOrs = ConfirmOrs;

			if(IsAndCheck) {
				for (int i = -1; i < confirmAnds.Length; i++) {
					CheckCond51AllBase(confirmAnds, confirmOrs, i, -1);
				}
			} else {
				for (int i = -1; i < confirmOrs.Length; i++) {
					CheckCond51AllBase(confirmAnds, confirmOrs, -1, i);
				}
			}

		}
		private static void CheckCond51AllBase(int[] confirmAnds, int[] confirmOrs, int andSkip, int orSkip)
		{
			int idx = andSkip == -1 ? (orSkip == -1 ? -1 : confirmOrs[orSkip]) : confirmAnds[andSkip];

			bool isOrOkForce = IsAndCheck && ConfirmOrs.Length == 0 && KouhoOrs.Length == 0; // orチェックを強制でOKにしておく

			List<string> codeList = CsvControll.GetCodeList();

			// 確定条件と候補条件について コード*日付分の情報を保存 andは一個でもfalseならそいつはアウト symbol=>[日付1,...]でfalseを保存
			Dictionary<string, HashSet<string>> beforeNotAnd = new Dictionary<string, HashSet<string>>();
			// beforeNotAndがfalseのものはスルー orは一個でもtrueならそいつはOK
			Dictionary<string, HashSet<string>> beforeOr = new Dictionary<string, HashSet<string>>();
			// beforeNotAndがfalseのものはスルー
			Dictionary<string, HashSet<string>>[] beforeNotAndKouho = new Dictionary<string, HashSet<string>>[KouhoAnds.Length];
			// beforeNotAndがfalseのものはスルー beforeOrがtrueのものはスルー
			Dictionary<string, HashSet<string>>[] beforeOrKouho = new Dictionary<string, HashSet<string>>[KouhoOrs.Length];
			foreach (string symbol in codeList) {
				beforeNotAnd[symbol] = new HashSet<string>();
				for (int i = 0; i < confirmAnds.Length; i++) {
					if (andSkip == i) continue;
					int condIdx = confirmAnds[i];
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(condIdx);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if ((cond51[pIdx + 1] == "1") != isT) beforeNotAnd[symbol].Add(cond51[0]);
					}
				}
				beforeOr[symbol] = new HashSet<string>();
				for (int i = 0; i < confirmOrs.Length; i++) {
					if (orSkip == i) continue;
					int condIdx = confirmOrs[i];
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


			int[,,] benefitAll = new int[kouhoNum, condNum(), 2];
			int[,,] havePeriodAll = new int[kouhoNum, condNum(), 2];
			int[,,] trueAll = new int[kouhoNum, condNum(), 2];



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

											benefitAll[i, condIdx, 0] += benefits[cond51[0]];
											havePeriodAll[i, condIdx, 0] += havePeriods[cond51[0]];
											trueAll[i, condIdx, 0]++;

											if (nowHaves[i] > c) continue;
											benefitAll[i, condIdx, 1] += benefits[cond51[0]];
											havePeriodAll[i, condIdx, 1] += havePeriods[cond51[0]];
											trueAll[i, condIdx, 1]++;
											nowHaves[i] = c + havePeriods[cond51[0]] + 1;
										}
									} else if (KouhoOrs.Length > 0) {
										// 残るはORチェック
										for (int i = 0; i < KouhoOrs.Length; i++) {
											if (!isOrCheck && !beforeOrKouho[i][symbol].Contains(cond51[0])) continue;
											benefitAll[i, condIdx, 0] += benefits[cond51[0]];
											havePeriodAll[i, condIdx, 0] += havePeriods[cond51[0]];
											trueAll[i, condIdx, 0]++;

											if (nowHaves[i] > c) continue;
											benefitAll[i, condIdx, 1] += benefits[cond51[0]];
											havePeriodAll[i, condIdx, 1] += havePeriods[cond51[0]];
											trueAll[i, condIdx, 1]++;
											nowHaves[i] = c + havePeriods[cond51[0]] + 1;
										}
									} else {

									}
								}
							}
						}
					}
				}

				//Common.DebugInfo("CheckCond51AllSymbol", symbol);

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
				Dictionary<int, double> noBenefit = new Dictionary<int, double>();
				Dictionary<int, double> skipBenefit = new Dictionary<int, double>();
				Dictionary<int, double> noPeriod = new Dictionary<int, double>();
				Dictionary<int, double> skipPeriod = new Dictionary<int, double>();
				Dictionary<int, double> scores = new Dictionary<int, double>();
				Dictionary<int, double> scores2 = new Dictionary<int, double>();
				Dictionary<int, double> scores3 = new Dictionary<int, double>();
				Dictionary<int, double> scores4 = new Dictionary<int, double>();
				int tMin = AllCond51Num; int tMax = 0;
				double maxBenefit = 0; double minBenefit = 9999;
				double baseBenefit = 0;
				for (int j = 0; j < condNum(); j++) {
					if (trueAll[i, j, 1] == 0) continue;
					noBenefit[j] = Common.Round((double)benefitAll[i, j, 0] / (double)trueAll[i, j, 0], 4);
					noPeriod[j] = Common.Round((double)havePeriodAll[i, j, 0] / (double)trueAll[i, j, 0], 4);
					skipBenefit[j] = Common.Round((double)benefitAll[i, j, 1] / (double)trueAll[i, j, 1], 4);
					skipPeriod[j] = Common.Round((double)havePeriodAll[i, j, 1] / (double)trueAll[i, j, 1], 4);

					scores[j] = Common.Round(Math.Pow(trueAll[i, j, 1], 0.75) * skipBenefit[j] / Math.Pow(skipPeriod[j], PeriodPow)
						+ Math.Pow(trueAll[i, j, 0], 0.75) * noBenefit[j] / Math.Pow(noPeriod[j], PeriodPow) / NoSkipRatio, 3);
					scores2[j] = Common.Round(Math.Pow(trueAll[i, j, 1], 0.8) * skipBenefit[j] / Math.Pow(skipPeriod[j], PeriodPow)
						+ Math.Pow(trueAll[i, j, 0], 0.8) * noBenefit[j] / Math.Pow(noPeriod[j], PeriodPow) / NoSkipRatio, 3);
					scores3[j] = Common.Round(Math.Pow(trueAll[i, j, 1], 0.85) * skipBenefit[j] / Math.Pow(skipPeriod[j], PeriodPow)
						+ Math.Pow(trueAll[i, j, 0], 0.85) * noBenefit[j] / Math.Pow(noPeriod[j], PeriodPow) / NoSkipRatio, 3);
					scores4[j] = Common.Round(Math.Pow(trueAll[i, j, 1], 0.9) * skipBenefit[j] / Math.Pow(skipPeriod[j], PeriodPow)
						+ Math.Pow(trueAll[i, j, 0], 0.9) * noBenefit[j] / Math.Pow(noPeriod[j], PeriodPow) / NoSkipRatio, 3);

					/*
					if (tMin > trueAll[i, j]) {
						tMin = trueAll[i, j];
						if (!IsAndCheck) baseBenefit = benefitRes[j];
					}
					if (tMax < trueAll[i, j]) {
						tMax = trueAll[i, j];
						if (IsAndCheck) baseBenefit = benefitRes[j];
					}
					maxBenefit = Math.Max(maxBenefit, benefitRes[j]); minBenefit = Math.Min(minBenefit, benefitRes[j]);
					*/
				}
				/*
				double needNum = tMax * 0.6;
				double needBenefit = 0;
				if (IsAndCheck) {
					needNum = tMax * 0.75;
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
				*/
				for (int r = 1; r <= 3; r++) {
					string result = ""; string result2 = ""; int max = 8;
					foreach (KeyValuePair<int, double> b in (r==0?scores: r == 1 ? scores2 : r == 2 ? scores3 : scores4).OrderByDescending(c => trueAll[i, c.Key, 1] > 50000 ? c.Value : -999)) {
						if (max > 0) {
							result += "\nCond:" + b.Key + ", Score:" + b.Value + ", sT:" + trueAll[i, b.Key, 1] + ", sP:" + skipPeriod[b.Key] + ", sB:" + skipBenefit[b.Key]
								+ ", nT:" + trueAll[i, b.Key, 0] + ", nP:" + noPeriod[b.Key] + ", nB:" + noBenefit[b.Key];
							result2 += b.Key + ",";
						}
						max--;
					}
					Common.DebugInfo("LowScoreRank" + r, idx, kouhoList[i], result, result2);
				}

				/*
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
				Common.DebugInfo("LowScoreRank4", kouhoList[i], result, result2);
				*/


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
			bool isOldCheck = false;

			int[] confirmAnds = isOldCheck? OldAnd51List : ConfirmAnds;
			int[] confirmOrs = isOldCheck ? OldOr51List : ConfirmOrs;

			for (int i = confirmOrs.Length - 1; i >= 0; i--) {
				DebugCheckCond51ScoreBase(confirmAnds, confirmOrs, -1, i);
			}
			for (int i = confirmAnds.Length - 1; i >= 0; i--) {
				DebugCheckCond51ScoreBase(confirmAnds, confirmOrs, i, -1);
			}
		}
		private static void DebugCheckCond51ScoreBase(int[] confirmAnds, int[] confirmOrs, int andSkip, int orSkip)
		{
			DateTime startDate = DateTime.Parse("2024/06/10");
			DateTime endDate = DateTime.Parse("2024/09/10");
			bool isAllScore = false; // Andチェック・Orチェックを無視する
			bool isOrOkForce = confirmOrs.Length == 0; // orチェックを強制でOKにしておく
			List<string> codeList = CsvControll.GetCodeList();
			if (IsPro500Only) codeList = codeList.FindAll(c => Array.IndexOf(OldPro500, Int32.Parse(c)) >= 0);

			Dictionary<string, HashSet<string>> beforeNotAnd = new Dictionary<string, HashSet<string>>();
			Dictionary<string, HashSet<string>> beforeOr = new Dictionary<string, HashSet<string>>();
			foreach (string symbol in codeList) {
				beforeNotAnd[symbol] = new HashSet<string>();
				for (int i = 0; i < confirmAnds.Length; i++) {
					if (andSkip == i) continue;
					int condIdx = confirmAnds[i];
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(condIdx);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if ((cond51[pIdx + 1] == "1") != isT) beforeNotAnd[symbol].Add(cond51[0]);
					}
				}
				beforeOr[symbol] = new HashSet<string>();
				for (int i = 0; i < confirmOrs.Length; i++) {
					if (orSkip == i) continue;
					int condIdx = confirmOrs[i];
					(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(condIdx);
					foreach (string[] cond51 in CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx)) {
						if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
						if ((cond51[pIdx + 1] == "1") == isT) beforeOr[symbol].Add(cond51[0]);
					}
				}
			}


			int skipBenefit = 0; int skipPeriod = 0; int skipTrue = 0;
			int noBenefit = 0; int noPeriod = 0; int noTrue = 0;
			int numAll = 0;
			foreach (string symbol in codeList) {
				Dictionary<string, int> benefits = new Dictionary<string, int>();
				Dictionary<string, int> havePeriods = new Dictionary<string, int>();
				foreach (string[] benefitInfo in CsvControll.GetBenefitAll(symbol)) {
					benefits[benefitInfo[0]] = Int32.Parse(benefitInfo[1]);
					havePeriods[benefitInfo[0]] = Int32.Parse(benefitInfo[2]);
				}
				int nowHaves = 0;
				(int pIdx, int ratioIdx, int diffDayIdx, bool isT) = SplitCondIdx(AllTrueCondIdx);
				List<string[]> list = CsvControll.GetCond51All(symbol, diffDayIdx, ratioIdx);
				for (int i = 0; i < list.Count; i++) {
					string[] cond51 = list[i];
					if (IsPro500Only && (Common.NewD2(DateTime.Parse(cond51[0]), startDate) || Common.NewD2(endDate,DateTime.Parse(cond51[0])))) continue;
					if (!benefits.ContainsKey(cond51[0])) continue;

					numAll++;
					if (!isAllScore) {
						if (beforeNotAnd[symbol].Contains(cond51[0])) continue;
						if (!isOrOkForce && !beforeOr[symbol].Contains(cond51[0])) continue;
					}


					noBenefit += benefits[cond51[0]];
					noPeriod += havePeriods[cond51[0]];
					noTrue++;

					if (nowHaves > i) continue;
					skipBenefit += benefits[cond51[0]];
					skipPeriod += havePeriods[cond51[0]];
					skipTrue++;
					nowHaves = i + havePeriods[cond51[0]] + 1;
				}
			}

			double skipB = (double)skipBenefit / skipTrue;
			double skipP = (double)skipPeriod / skipTrue;
			double noB = (double)noBenefit / noTrue;
			double noP = (double)noPeriod / noTrue;
			double score = Common.Round(Math.Pow(skipTrue, 0.85) * skipB / Math.Pow(skipP, PeriodPow)
				+ Math.Pow(noTrue, 0.85) * noB / Math.Pow(noP, PeriodPow) / NoSkipRatio, 3);

			int idx = andSkip == - 1 ? confirmOrs[orSkip] :confirmAnds[andSkip];
			Common.DebugInfo("DebugCheckCond51", idx, andSkip, orSkip, score, skipTrue, Common.Round(skipB, 2), Common.Round(skipP, 2), noTrue, Common.Round(noB, 2), Common.Round(noP, 2));
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

LowScoreRank1 , T0:-1 , T1:1 , T2:
Cond:52, Score:2280.209, sT:50798, sP:3.19, sB:0.6242, nT:159989, nP:3.8445, nB:0.754
Cond:6717, Score:2274.268, sT:50488, sP:3.17, sB:0.6241, nT:158814, nP:3.8384, nB:0.752
Cond:6907, Score:2273.257, sT:50889, sP:3.1768, sB:0.6214, nT:160121, nP:3.8447, nB:0.7452
Cond:7149, Score:2269.766, sT:50597, sP:3.1714, sB:0.622, nT:159112, nP:3.8378, nB:0.7492
Cond:8144, Score:2268.613, sT:50335, sP:3.1693, sB:0.6243, nT:158289, nP:3.8358, nB:0.7506
Cond:6941, Score:2268.213, sT:50356, sP:3.1706, sB:0.6239, nT:158354, nP:3.838, nB:0.7514
Cond:528, Score:2266.84, sT:50410, sP:3.1692, sB:0.6231, nT:158502, nP:3.8354, nB:0.749
Cond:1424, Score:2266.724, sT:50327, sP:3.1702, sB:0.6234, nT:158288, nP:3.8369, nB:0.7522 , T3:52,6717,6907,7149,8144,6941,528,1424,  #End#
LowScoreRank2 , T0:-1 , T1:1 , T2:
Cond:52, Score:3977.883, sT:50798, sP:3.19, sB:0.6242, nT:159989, nP:3.8445, nB:0.754
Cond:6717, Score:3965.957, sT:50488, sP:3.17, sB:0.6241, nT:158814, nP:3.8384, nB:0.752
Cond:6907, Score:3965.596, sT:50889, sP:3.1768, sB:0.6214, nT:160121, nP:3.8447, nB:0.7452
Cond:7149, Score:3958.51, sT:50597, sP:3.1714, sB:0.622, nT:159112, nP:3.8378, nB:0.7492
Cond:8144, Score:3955.389, sT:50335, sP:3.1693, sB:0.6243, nT:158289, nP:3.8358, nB:0.7506
Cond:6941, Score:3954.844, sT:50356, sP:3.1706, sB:0.6239, nT:158354, nP:3.838, nB:0.7514
Cond:528, Score:3952.574, sT:50410, sP:3.1692, sB:0.6231, nT:158502, nP:3.8354, nB:0.749
Cond:7922, Score:3952.24, sT:50425, sP:3.1667, sB:0.6224, nT:158630, nP:3.8322, nB:0.7486 , T3:52,6717,6907,7149,8144,6941,528,7922,  #End#
LowScoreRank3 , T0:-1 , T1:1 , T2:
Cond:52, Score:6943.933, sT:50798, sP:3.19, sB:0.6242, nT:159989, nP:3.8445, nB:0.754
Cond:6907, Score:6922.176, sT:50889, sP:3.1768, sB:0.6214, nT:160121, nP:3.8447, nB:0.7452
Cond:6717, Score:6920.365, sT:50488, sP:3.17, sB:0.6241, nT:158814, nP:3.8384, nB:0.752
Cond:7149, Score:6908.073, sT:50597, sP:3.1714, sB:0.622, nT:159112, nP:3.8378, nB:0.7492
Cond:80, Score:6907.208, sT:54836, sP:3.2437, sB:0.5902, nT:165036, nP:3.8268, nB:0.713
Cond:8144, Score:6900.688, sT:50335, sP:3.1693, sB:0.6243, nT:158289, nP:3.8358, nB:0.7506
Cond:6941, Score:6900.002, sT:50356, sP:3.1706, sB:0.6239, nT:158354, nP:3.838, nB:0.7514
Cond:3327, Score:6897.833, sT:50992, sP:3.1788, sB:0.6172, nT:160412, nP:3.8427, nB:0.7448 , T3:52,6907,6717,7149,80,8144,6941,3327,  #End#
LowScoreRank1 , T0:2322 , T1:1 , T2:
Cond:472, Score:2298.041, sT:50646, sP:3.1771, sB:0.6268, nT:159803, nP:3.8406, nB:0.7678
Cond:1144, Score:2279.327, sT:50296, sP:3.1688, sB:0.6231, nT:158400, nP:3.8465, nB:0.7713
Cond:1368, Score:2247.506, sT:51492, sP:3.1509, sB:0.5973, nT:162754, nP:3.827, nB:0.7542
Cond:24, Score:2245.479, sT:50669, sP:3.1881, sB:0.6109, nT:158843, nP:3.846, nB:0.7644
Cond:696, Score:2245.012, sT:52336, sP:3.1653, sB:0.5882, nT:166342, nP:3.8245, nB:0.7489
Cond:3668, Score:2242.165, sT:51600, sP:3.1365, sB:0.5915, nT:161712, nP:3.7894, nB:0.7573
Cond:1862, Score:2241.961, sT:50018, sP:3.2168, sB:0.6225, nT:155144, nP:3.8774, nB:0.7723
Cond:80, Score:2240.015, sT:51964, sP:3.2888, sB:0.6224, nT:154414, nP:3.8888, nB:0.7372 , T3:472,1144,1368,24,696,3668,1862,80,  #End#
LowScoreRank2 , T0:2322 , T1:1 , T2:
Cond:472, Score:4009.081, sT:50646, sP:3.1771, sB:0.6268, nT:159803, nP:3.8406, nB:0.7678
Cond:1144, Score:3975.229, sT:50296, sP:3.1688, sB:0.6231, nT:158400, nP:3.8465, nB:0.7713
Cond:1368, Score:3925.508, sT:51492, sP:3.1509, sB:0.5973, nT:162754, nP:3.827, nB:0.7542
Cond:696, Score:3925.35, sT:52336, sP:3.1653, sB:0.5882, nT:166342, nP:3.8245, nB:0.7489
Cond:24, Score:3917.887, sT:50669, sP:3.1881, sB:0.6109, nT:158843, nP:3.846, nB:0.7644
Cond:3668, Score:3916.596, sT:51600, sP:3.1365, sB:0.5915, nT:161712, nP:3.7894, nB:0.7573
Cond:1862, Score:3907.96, sT:50018, sP:3.2168, sB:0.6225, nT:155144, nP:3.8774, nB:0.7723
Cond:80, Score:3907.075, sT:51964, sP:3.2888, sB:0.6224, nT:154414, nP:3.8888, nB:0.7372 , T3:472,1144,1368,696,24,3668,1862,80,  #End#
LowScoreRank3 , T0:2322 , T1:1 , T2:
Cond:472, Score:6998.592, sT:50646, sP:3.1771, sB:0.6268, nT:159803, nP:3.8406, nB:0.7678
Cond:1144, Score:6937.396, sT:50296, sP:3.1688, sB:0.6231, nT:158400, nP:3.8465, nB:0.7713
Cond:696, Score:6867.937, sT:52336, sP:3.1653, sB:0.5882, nT:166342, nP:3.8245, nB:0.7489
Cond:1368, Score:6860.792, sT:51492, sP:3.1509, sB:0.5973, nT:162754, nP:3.827, nB:0.7542
Cond:3668, Score:6845.9, sT:51600, sP:3.1365, sB:0.5915, nT:161712, nP:3.7894, nB:0.7573
Cond:24, Score:6840.261, sT:50669, sP:3.1881, sB:0.6109, nT:158843, nP:3.846, nB:0.7644
Cond:80, Score:6818.59, sT:51964, sP:3.2888, sB:0.6224, nT:154414, nP:3.8888, nB:0.7372
Cond:1862, Score:6816.209, sT:50018, sP:3.2168, sB:0.6225, nT:155144, nP:3.8774, nB:0.7723 , T3:472,1144,696,1368,3668,24,80,1862,  #End#
LowScoreRank1 , T0:66 , T1:1 , T2:
Cond:80, Score:2127.523, sT:52671, sP:3.1973, sB:0.5649, nT:159060, nP:3.8214, nB:0.7104
Cond:306, Score:2104.519, sT:50634, sP:3.269, sB:0.5768, nT:157755, nP:3.8662, nB:0.7425
Cond:68, Score:2073.393, sT:53908, sP:3.2702, sB:0.533, nT:170316, nP:3.8904, nB:0.718
Cond:82, Score:2060.366, sT:63929, sP:3.5583, sB:0.4904, nT:189418, nP:3.9417, nB:0.653
Cond:516, Score:2020.902, sT:51087, sP:3.229, sB:0.5384, nT:167113, nP:3.9207, nB:0.7123
Cond:530, Score:2009.401, sT:57752, sP:3.3872, sB:0.4928, nT:182559, nP:3.9361, nB:0.6891
Cond:1620, Score:2000.316, sT:50054, sP:3.1549, sB:0.526, nT:170437, nP:3.9556, nB:0.7244
Cond:6645, Score:1984.37, sT:50187, sP:3.184, sB:0.5327, nT:174608, nP:3.9865, nB:0.6781 , T3:80,306,68,82,516,530,1620,6645,  #End#
LowScoreRank2 , T0:66 , T1:1 , T2:
Cond:80, Score:3716.673, sT:52671, sP:3.1973, sB:0.5649, nT:159060, nP:3.8214, nB:0.7104
Cond:306, Score:3672.988, sT:50634, sP:3.269, sB:0.5768, nT:157755, nP:3.8662, nB:0.7425
Cond:82, Score:3637.002, sT:63929, sP:3.5583, sB:0.4904, nT:189418, nP:3.9417, nB:0.653
Cond:68, Score:3632.91, sT:53908, sP:3.2702, sB:0.533, nT:170316, nP:3.8904, nB:0.718
Cond:530, Score:3535.158, sT:57752, sP:3.3872, sB:0.4928, nT:182559, nP:3.9361, nB:0.6891
Cond:516, Score:3533.042, sT:51087, sP:3.229, sB:0.5384, nT:167113, nP:3.9207, nB:0.7123
Cond:1620, Score:3497.725, sT:50054, sP:3.1549, sB:0.526, nT:170437, nP:3.9556, nB:0.7244
Cond:6645, Score:3468.746, sT:50187, sP:3.184, sB:0.5327, nT:174608, nP:3.9865, nB:0.6781 , T3:80,306,82,68,530,516,1620,6645,  #End#
LowScoreRank3 , T0:66 , T1:1 , T2:
Cond:80, Score:6496.687, sT:52671, sP:3.1973, sB:0.5649, nT:159060, nP:3.8214, nB:0.7104
Cond:82, Score:6423.945, sT:63929, sP:3.5583, sB:0.4904, nT:189418, nP:3.9417, nB:0.653
Cond:306, Score:6414.543, sT:50634, sP:3.269, sB:0.5768, nT:157755, nP:3.8662, nB:0.7425
Cond:68, Score:6369.729, sT:53908, sP:3.2702, sB:0.533, nT:170316, nP:3.8904, nB:0.718
Cond:530, Score:6223.736, sT:57752, sP:3.3872, sB:0.4928, nT:182559, nP:3.9361, nB:0.6891
Cond:516, Score:6181.063, sT:51087, sP:3.229, sB:0.5384, nT:167113, nP:3.9207, nB:0.7123
Cond:1620, Score:6120.863, sT:50054, sP:3.1549, sB:0.526, nT:170437, nP:3.9556, nB:0.7244
Cond:6645, Score:6068.28, sT:50187, sP:3.184, sB:0.5327, nT:174608, nP:3.9865, nB:0.6781 , T3:80,82,306,68,530,516,1620,6645,  #End#
LowScoreRank1 , T0:1065 , T1:1 , T2:
Cond:1961, Score:2243.214, sT:51033, sP:3.1914, sB:0.6061, nT:161331, nP:3.8712, nB:0.7614
Cond:1049, Score:2235.238, sT:54141, sP:3.2792, sB:0.5931, nT:172296, nP:3.9778, nB:0.7082
Cond:3103, Score:2230.753, sT:50068, sP:3.1641, sB:0.6108, nT:157895, nP:3.8336, nB:0.7575
Cond:5119, Score:2230.324, sT:50095, sP:3.1613, sB:0.6097, nT:158193, nP:3.8356, nB:0.7578
Cond:80, Score:2229.498, sT:53358, sP:3.2213, sB:0.5906, nT:159830, nP:3.8029, nB:0.7323
Cond:472, Score:2218.757, sT:50760, sP:3.1413, sB:0.5929, nT:161117, nP:3.801, nB:0.7558
Cond:1273, Score:2217.746, sT:50287, sP:3.1784, sB:0.607, nT:157255, nP:3.837, nB:0.7556
Cond:7450, Score:2217.592, sT:50090, sP:3.1523, sB:0.6028, nT:159772, nP:3.8353, nB:0.7559 , T3:1961,1049,3103,5119,80,472,1273,7450,  #End#
LowScoreRank2 , T0:1065 , T1:1 , T2:
Cond:1961, Score:3916.085, sT:51033, sP:3.1914, sB:0.6061, nT:161331, nP:3.8712, nB:0.7614
Cond:1049, Score:3912.085, sT:54141, sP:3.2792, sB:0.5931, nT:172296, nP:3.9778, nB:0.7082
Cond:80, Score:3896.409, sT:53358, sP:3.2213, sB:0.5906, nT:159830, nP:3.8029, nB:0.7323
Cond:3103, Score:3889.881, sT:50068, sP:3.1641, sB:0.6108, nT:157895, nP:3.8336, nB:0.7575
Cond:5119, Score:3889.409, sT:50095, sP:3.1613, sB:0.6097, nT:158193, nP:3.8356, nB:0.7578
Cond:472, Score:3873.41, sT:50760, sP:3.1413, sB:0.5929, nT:161117, nP:3.801, nB:0.7558
Cond:7450, Score:3868.361, sT:50090, sP:3.1523, sB:0.6028, nT:159772, nP:3.8353, nB:0.7559
Cond:1273, Score:3867.582, sT:50287, sP:3.1784, sB:0.607, nT:157255, nP:3.837, nB:0.7556 , T3:1961,1049,80,3103,5119,472,7450,1273,  #End#
LowScoreRank3 , T0:1065 , T1:1 , T2:
Cond:1049, Score:6851.302, sT:54141, sP:3.2792, sB:0.5931, nT:172296, nP:3.9778, nB:0.7082
Cond:1961, Score:6840.951, sT:51033, sP:3.1914, sB:0.6061, nT:161331, nP:3.8712, nB:0.7614
Cond:80, Score:6813.562, sT:53358, sP:3.2213, sB:0.5906, nT:159830, nP:3.8029, nB:0.7323
Cond:3103, Score:6787.362, sT:50068, sP:3.1641, sB:0.6108, nT:157895, nP:3.8336, nB:0.7575
Cond:5119, Score:6787.037, sT:50095, sP:3.1613, sB:0.6097, nT:158193, nP:3.8356, nB:0.7578
Cond:472, Score:6766.51, sT:50760, sP:3.1413, sB:0.5929, nT:161117, nP:3.801, nB:0.7558
Cond:7450, Score:6752.432, sT:50090, sP:3.1523, sB:0.6028, nT:159772, nP:3.8353, nB:0.7559
Cond:1273, Score:6749.054, sT:50287, sP:3.1784, sB:0.607, nT:157255, nP:3.837, nB:0.7556 , T3:1049,1961,80,3103,5119,472,7450,1273,  #End#
LowScoreRank1 , T0:6851 , T1:1 , T2:
Cond:80, Score:2246.218, sT:52629, sP:3.2448, sB:0.6105, nT:156102, nP:3.8156, nB:0.731
Cond:6663, Score:2235, sT:51097, sP:3.2027, sB:0.6117, nT:162657, nP:3.8954, nB:0.7314
Cond:6889, Score:2232.389, sT:50264, sP:3.1838, sB:0.6172, nT:158265, nP:3.857, nB:0.7401
Cond:6683, Score:2228.501, sT:50292, sP:3.1869, sB:0.6167, nT:157586, nP:3.8587, nB:0.7399
Cond:472, Score:2221.918, sT:50087, sP:3.1669, sB:0.6086, nT:157529, nP:3.8125, nB:0.753
Cond:4895, Score:2219.751, sT:50583, sP:3.2093, sB:0.6132, nT:159457, nP:3.8748, nB:0.7358
Cond:1049, Score:2219.016, sT:52118, sP:3.2828, sB:0.6089, nT:164283, nP:3.9762, nB:0.7249
Cond:24, Score:2218.29, sT:50104, sP:3.1761, sB:0.6095, nT:156649, nP:3.8176, nB:0.7525 , T3:80,6663,6889,6683,472,4895,1049,24,  #End#
LowScoreRank2 , T0:6851 , T1:1 , T2:
Cond:80, Score:3920.81, sT:52629, sP:3.2448, sB:0.6105, nT:156102, nP:3.8156, nB:0.731
Cond:6663, Score:3900.375, sT:51097, sP:3.2027, sB:0.6117, nT:162657, nP:3.8954, nB:0.7314
Cond:6889, Score:3891.925, sT:50264, sP:3.1838, sB:0.6172, nT:158265, nP:3.857, nB:0.7401
Cond:6683, Score:3884.885, sT:50292, sP:3.1869, sB:0.6167, nT:157586, nP:3.8587, nB:0.7399
Cond:1049, Score:3875.417, sT:52118, sP:3.2828, sB:0.6089, nT:164283, nP:3.9762, nB:0.7249
Cond:472, Score:3874.393, sT:50087, sP:3.1669, sB:0.6086, nT:157529, nP:3.8125, nB:0.753
Cond:4895, Score:3871.337, sT:50583, sP:3.2093, sB:0.6132, nT:159457, nP:3.8748, nB:0.7358
Cond:24, Score:3867.578, sT:50104, sP:3.1761, sB:0.6095, nT:156649, nP:3.8176, nB:0.7525 , T3:80,6663,6889,6683,1049,472,4895,24,  #End#
LowScoreRank3 , T0:6851 , T1:1 , T2:
Cond:80, Score:6847.665, sT:52629, sP:3.2448, sB:0.6105, nT:156102, nP:3.8156, nB:0.731
Cond:6663, Score:6811.076, sT:51097, sP:3.2027, sB:0.6117, nT:162657, nP:3.8954, nB:0.7314
Cond:6889, Score:6789.437, sT:50264, sP:3.1838, sB:0.6172, nT:158265, nP:3.857, nB:0.7401
Cond:6683, Score:6776.655, sT:50292, sP:3.1869, sB:0.6167, nT:157586, nP:3.8587, nB:0.7399
Cond:1049, Score:6772.529, sT:52118, sP:3.2828, sB:0.6089, nT:164283, nP:3.9762, nB:0.7249
Cond:472, Score:6760.177, sT:50087, sP:3.1669, sB:0.6086, nT:157529, nP:3.8125, nB:0.753
Cond:4895, Score:6756.059, sT:50583, sP:3.2093, sB:0.6132, nT:159457, nP:3.8748, nB:0.7358
Cond:6833, Score:6749.466, sT:51834, sP:3.1558, sB:0.587, nT:163469, nP:3.8009, nB:0.7284 , T3:80,6663,6889,6683,1049,472,4895,6833,  #End#
LowScoreRank1 , T0:1618 , T1:1 , T2:
Cond:710, Score:2246.609, sT:50205, sP:3.0806, sB:0.6248, nT:153093, nP:3.7405, nB:0.6893
Cond:724, Score:2230.024, sT:51012, sP:3.142, sB:0.6071, nT:159700, nP:3.809, nB:0.7171
Cond:696, Score:2213.943, sT:52352, sP:3.0597, sB:0.5866, nT:160557, nP:3.6925, nB:0.6713
Cond:80, Score:2202.952, sT:52834, sP:3.1508, sB:0.6057, nT:150900, nP:3.7019, nB:0.6451
Cond:472, Score:2199.757, sT:50725, sP:3.0655, sB:0.6026, nT:154085, nP:3.698, nB:0.6747
Cond:24, Score:2169.421, sT:50737, sP:3.0725, sB:0.5947, nT:153029, nP:3.7022, nB:0.6709
Cond:7341, Score:2158.105, sT:50511, sP:3.1078, sB:0.6057, nT:152777, nP:3.7707, nB:0.648
Cond:462, Score:2157.826, sT:50574, sP:3.0539, sB:0.59, nT:153795, nP:3.686, nB:0.6654 , T3:710,724,696,80,472,24,7341,462,  #End#
LowScoreRank2 , T0:1618 , T1:1 , T2:
Cond:710, Score:3910.417, sT:50205, sP:3.0806, sB:0.6248, nT:153093, nP:3.7405, nB:0.6893
Cond:724, Score:3889.544, sT:51012, sP:3.142, sB:0.6071, nT:159700, nP:3.809, nB:0.7171
Cond:696, Score:3863.672, sT:52352, sP:3.0597, sB:0.5866, nT:160557, nP:3.6925, nB:0.6713
Cond:80, Score:3838.882, sT:52834, sP:3.1508, sB:0.6057, nT:150900, nP:3.7019, nB:0.6451
Cond:472, Score:3831.274, sT:50725, sP:3.0655, sB:0.6026, nT:154085, nP:3.698, nB:0.6747
Cond:24, Score:3778.256, sT:50737, sP:3.0725, sB:0.5947, nT:153029, nP:3.7022, nB:0.6709
Cond:462, Score:3758.028, sT:50574, sP:3.0539, sB:0.59, nT:153795, nP:3.686, nB:0.6654
Cond:1025, Score:3757.505, sT:50843, sP:3.0873, sB:0.5848, nT:154797, nP:3.7113, nB:0.6878 , T3:710,724,696,80,472,24,462,1025,  #End#
LowScoreRank3 , T0:1618 , T1:1 , T2:
Cond:710, Score:6810.263, sT:50205, sP:3.0806, sB:0.6248, nT:153093, nP:3.7405, nB:0.6893
Cond:724, Score:6788.234, sT:51012, sP:3.142, sB:0.6071, nT:159700, nP:3.809, nB:0.7171
Cond:696, Score:6746.645, sT:52352, sP:3.0597, sB:0.5866, nT:160557, nP:3.6925, nB:0.6713
Cond:80, Score:6692.893, sT:52834, sP:3.1508, sB:0.6057, nT:150900, nP:3.7019, nB:0.6451
Cond:472, Score:6676.63, sT:50725, sP:3.0655, sB:0.6026, nT:154085, nP:3.698, nB:0.6747
Cond:24, Score:6583.878, sT:50737, sP:3.0725, sB:0.5947, nT:153029, nP:3.7022, nB:0.6709
Cond:1025, Score:6552.695, sT:50843, sP:3.0873, sB:0.5848, nT:154797, nP:3.7113, nB:0.6878
Cond:462, Score:6548.633, sT:50574, sP:3.0539, sB:0.59, nT:153795, nP:3.686, nB:0.6654 , T3:710,724,696,80,472,24,1025,462,  #End#
LowScoreRank1 , T0:7436 , T1:1 , T2:
Cond:7868, Score:2182.699, sT:50217, sP:3.1245, sB:0.5913, nT:157363, nP:3.7442, nB:0.7321
Cond:5182, Score:2148.833, sT:50665, sP:3.1258, sB:0.5807, nT:158771, nP:3.7641, nB:0.7089
Cond:7660, Score:2138.65, sT:54687, sP:3.2493, sB:0.5528, nT:180200, nP:4.0159, nB:0.6818
Cond:5404, Score:2138.178, sT:50430, sP:3.1161, sB:0.5721, nT:158008, nP:3.7339, nB:0.7286
Cond:3390, Score:2131.834, sT:51265, sP:3.1263, sB:0.5648, nT:160895, nP:3.7583, nB:0.7167
Cond:7228, Score:2129.573, sT:54573, sP:3.3177, sB:0.5607, nT:180124, nP:4.1514, nB:0.6875
Cond:4750, Score:2123.347, sT:50304, sP:3.1694, sB:0.5798, nT:157535, nP:3.8476, nB:0.7242
Cond:3614, Score:2108.301, sT:53723, sP:3.1581, sB:0.5384, nT:173115, nP:3.8349, nB:0.6886 , T3:7868,5182,7660,5404,3390,7228,4750,3614,  #End#
LowScoreRank2 , T0:7436 , T1:1 , T2:
Cond:7868, Score:3806.348, sT:50217, sP:3.1245, sB:0.5913, nT:157363, nP:3.7442, nB:0.7321
Cond:7660, Score:3748.681, sT:54687, sP:3.2493, sB:0.5528, nT:180200, nP:4.0159, nB:0.6818
Cond:5182, Score:3748.246, sT:50665, sP:3.1258, sB:0.5807, nT:158771, nP:3.7641, nB:0.7089
Cond:7228, Score:3731.922, sT:54573, sP:3.3177, sB:0.5607, nT:180124, nP:4.1514, nB:0.6875
Cond:5404, Score:3730.655, sT:50430, sP:3.1161, sB:0.5721, nT:158008, nP:3.7339, nB:0.7286
Cond:3390, Score:3722.544, sT:51265, sP:3.1263, sB:0.5648, nT:160895, nP:3.7583, nB:0.7167
Cond:4750, Score:3703.128, sT:50304, sP:3.1694, sB:0.5798, nT:157535, nP:3.8476, nB:0.7242
Cond:3614, Score:3692.352, sT:53723, sP:3.1581, sB:0.5384, nT:173115, nP:3.8349, nB:0.6886 , T3:7868,7660,5182,7228,5404,3390,4750,3614,  #End#
LowScoreRank3 , T0:7436 , T1:1 , T2:
Cond:7868, Score:6642.017, sT:50217, sP:3.1245, sB:0.5913, nT:157363, nP:3.7442, nB:0.7321
Cond:7660, Score:6575.393, sT:54687, sP:3.2493, sB:0.5528, nT:180200, nP:4.0159, nB:0.6818
Cond:7228, Score:6544.496, sT:54573, sP:3.3177, sB:0.5607, nT:180124, nP:4.1514, nB:0.6875
Cond:5182, Score:6542.265, sT:50665, sP:3.1258, sB:0.5807, nT:158771, nP:3.7641, nB:0.7089
Cond:5404, Score:6513.383, sT:50430, sP:3.1161, sB:0.5721, nT:158008, nP:3.7339, nB:0.7286
Cond:3390, Score:6504.397, sT:51265, sP:3.1263, sB:0.5648, nT:160895, nP:3.7583, nB:0.7167
Cond:3614, Score:6470.989, sT:53723, sP:3.1581, sB:0.5384, nT:173115, nP:3.8349, nB:0.6886
Cond:4750, Score:6462.387, sT:50304, sP:3.1694, sB:0.5798, nT:157535, nP:3.8476, nB:0.7242 , T3:7868,7660,7228,5182,5404,3390,3614,4750,  #End#
LowScoreRank1 , T0:5388 , T1:1 , T2:
Cond:7880, Score:2254.629, sT:50445, sP:3.1675, sB:0.6188, nT:158729, nP:3.8346, nB:0.7454
Cond:3372, Score:2253.072, sT:50214, sP:3.1711, sB:0.6204, nT:157789, nP:3.8394, nB:0.7516
Cond:4954, Score:2245.056, sT:50011, sP:3.1781, sB:0.6216, nT:157067, nP:3.851, nB:0.7513
Cond:7866, Score:2244.066, sT:50886, sP:3.17, sB:0.611, nT:160860, nP:3.8496, nB:0.7393
Cond:3594, Score:2243.067, sT:50120, sP:3.1711, sB:0.6193, nT:157338, nP:3.8399, nB:0.7474
Cond:3596, Score:2242.812, sT:50546, sP:3.1647, sB:0.6126, nT:159022, nP:3.8317, nB:0.7459
Cond:7672, Score:2242.605, sT:50934, sP:3.1611, sB:0.6089, nT:160658, nP:3.8302, nB:0.7376
Cond:52, Score:2238.407, sT:50017, sP:3.2012, sB:0.6218, nT:156880, nP:3.8615, nB:0.7541 , T3:7880,3372,4954,7866,3594,3596,7672,52,  #End#
LowScoreRank2 , T0:5388 , T1:1 , T2:
Cond:7880, Score:3931.562, sT:50445, sP:3.1675, sB:0.6188, nT:158729, nP:3.8346, nB:0.7454
Cond:3372, Score:3928.07, sT:50214, sP:3.1711, sB:0.6204, nT:157789, nP:3.8394, nB:0.7516
Cond:7866, Score:3915.345, sT:50886, sP:3.17, sB:0.611, nT:160860, nP:3.8496, nB:0.7393
Cond:4954, Score:3913.135, sT:50011, sP:3.1781, sB:0.6216, nT:157067, nP:3.851, nB:0.7513
Cond:7672, Score:3912.905, sT:50934, sP:3.1611, sB:0.6089, nT:160658, nP:3.8302, nB:0.7376
Cond:3596, Score:3911.788, sT:50546, sP:3.1647, sB:0.6126, nT:159022, nP:3.8317, nB:0.7459
Cond:3594, Score:3910.012, sT:50120, sP:3.1711, sB:0.6193, nT:157338, nP:3.8399, nB:0.7474
Cond:472, Score:3905.988, sT:51945, sP:3.1746, sB:0.5961, nT:164246, nP:3.8322, nB:0.7341 , T3:7880,3372,7866,4954,7672,3596,3594,472,  #End#
LowScoreRank3 , T0:5388 , T1:1 , T2:
Cond:7880, Score:6860.093, sT:50445, sP:3.1675, sB:0.6188, nT:158729, nP:3.8346, nB:0.7454
Cond:3372, Score:6852.646, sT:50214, sP:3.1711, sB:0.6204, nT:157789, nP:3.8394, nB:0.7516
Cond:7866, Score:6835.693, sT:50886, sP:3.17, sB:0.611, nT:160860, nP:3.8496, nB:0.7393
Cond:7672, Score:6831.605, sT:50934, sP:3.1611, sB:0.6089, nT:160658, nP:3.8302, nB:0.7376
Cond:472, Score:6828.084, sT:51945, sP:3.1746, sB:0.5961, nT:164246, nP:3.8322, nB:0.7341
Cond:3596, Score:6827.065, sT:50546, sP:3.1647, sB:0.6126, nT:159022, nP:3.8317, nB:0.7459
Cond:4954, Score:6824.906, sT:50011, sP:3.1781, sB:0.6216, nT:157067, nP:3.851, nB:0.7513
Cond:7658, Score:6820.71, sT:51141, sP:3.1831, sB:0.6076, nT:162551, nP:3.8737, nB:0.7368 , T3:7880,3372,7866,7672,472,3596,4954,7658,  #End#
LowScoreRank1 , T0:7599 , T1:1 , T2:
Cond:6717, Score:2284.482, sT:50068, sP:3.167, sB:0.6317, nT:157286, nP:3.8331, nB:0.757
Cond:6907, Score:2276.516, sT:50094, sP:3.1721, sB:0.6295, nT:157185, nP:3.8403, nB:0.7571
Cond:4911, Score:2271.479, sT:50721, sP:3.1706, sB:0.6211, nT:159506, nP:3.8373, nB:0.7484
Cond:129, Score:2270.376, sT:50221, sP:3.1612, sB:0.6249, nT:156105, nP:3.8249, nB:0.7581
Cond:6493, Score:2268.912, sT:50611, sP:3.1657, sB:0.6206, nT:159216, nP:3.8337, nB:0.7491
Cond:3327, Score:2267.336, sT:50494, sP:3.1779, sB:0.6228, nT:158478, nP:3.8405, nB:0.7525
Cond:7149, Score:2266.903, sT:50209, sP:3.1684, sB:0.6249, nT:157588, nP:3.8339, nB:0.7527
Cond:5119, Score:2266.317, sT:51619, sP:3.187, sB:0.6126, nT:162972, nP:3.8568, nB:0.7382 , T3:6717,6907,4911,129,6493,3327,7149,5119,  #End#
LowScoreRank2 , T0:7599 , T1:1 , T2:
Cond:6717, Score:3981.767, sT:50068, sP:3.167, sB:0.6317, nT:157286, nP:3.8331, nB:0.757
Cond:6907, Score:3968.033, sT:50094, sP:3.1721, sB:0.6295, nT:157185, nP:3.8403, nB:0.7571
Cond:4911, Score:3961.997, sT:50721, sP:3.1706, sB:0.6211, nT:159506, nP:3.8373, nB:0.7484
Cond:80, Score:3957.718, sT:54023, sP:3.241, sB:0.5998, nT:161904, nP:3.8229, nB:0.7248
Cond:129, Score:3957.411, sT:50221, sP:3.1612, sB:0.6249, nT:156105, nP:3.8249, nB:0.7581
Cond:6493, Score:3957.179, sT:50611, sP:3.1657, sB:0.6206, nT:159216, nP:3.8337, nB:0.7491
Cond:5119, Score:3956.808, sT:51619, sP:3.187, sB:0.6126, nT:162972, nP:3.8568, nB:0.7382
Cond:3327, Score:3953.873, sT:50494, sP:3.1779, sB:0.6228, nT:158478, nP:3.8405, nB:0.7525 , T3:6717,6907,4911,80,129,6493,5119,3327,  #End#
LowScoreRank3 , T0:7599 , T1:1 , T2:
Cond:6717, Score:6944.437, sT:50068, sP:3.167, sB:0.6317, nT:157286, nP:3.8331, nB:0.757
Cond:80, Score:6923.277, sT:54023, sP:3.241, sB:0.5998, nT:161904, nP:3.8229, nB:0.7248
Cond:6907, Score:6920.746, sT:50094, sP:3.1721, sB:0.6295, nT:157185, nP:3.8403, nB:0.7571
Cond:4911, Score:6915.032, sT:50721, sP:3.1706, sB:0.6211, nT:159506, nP:3.8373, nB:0.7484
Cond:5119, Score:6912.678, sT:51619, sP:3.187, sB:0.6126, nT:162972, nP:3.8568, nB:0.7382
Cond:6493, Score:6906.032, sT:50611, sP:3.1657, sB:0.6206, nT:159216, nP:3.8337, nB:0.7491
Cond:129, Score:6902.29, sT:50221, sP:3.1612, sB:0.6249, nT:156105, nP:3.8249, nB:0.7581
Cond:3327, Score:6899.272, sT:50494, sP:3.1779, sB:0.6228, nT:158478, nP:3.8405, nB:0.7525 , T3:6717,80,6907,4911,5119,6493,129,3327,  #End#
End , T0:04:00:48.3035875  #End#


/////////////////////////////////////////




LowScoreRank1 , T0:-1 , T1:1 , T2:
Cond:35, Score:2363.582, sT:50082, sP:3.1906, sB:0.667, nT:156568, nP:3.8696, nB:0.753
Cond:49, Score:2347.14, sT:50262, sP:3.1843, sB:0.6529, nT:157942, nP:3.8545, nB:0.765
Cond:275, Score:2338.387, sT:50414, sP:3.1821, sB:0.6445, nT:158442, nP:3.8476, nB:0.7742
Cond:305, Score:2331.635, sT:50462, sP:3.1819, sB:0.6459, nT:158861, nP:3.8509, nB:0.7571
Cond:5, Score:2321.219, sT:50489, sP:3.1805, sB:0.6387, nT:159143, nP:3.8471, nB:0.7661
Cond:677, Score:2316.513, sT:50321, sP:3.1862, sB:0.6399, nT:158688, nP:3.8531, nB:0.7669
Cond:513, Score:2312.584, sT:50490, sP:3.1804, sB:0.6352, nT:159015, nP:3.8477, nB:0.7678
Cond:19, Score:2310.309, sT:50430, sP:3.1821, sB:0.6376, nT:158562, nP:3.8529, nB:0.7615 , T3:35,49,275,305,5,677,513,19, 
LowScoreRank2 , T0:-1 , T1:1 , T2:
Cond:35, Score:4116.595, sT:50082, sP:3.1906, sB:0.667, nT:156568, nP:3.8696, nB:0.753
Cond:49, Score:4090.798, sT:50262, sP:3.1843, sB:0.6529, nT:157942, nP:3.8545, nB:0.765
Cond:275, Score:4077.298, sT:50414, sP:3.1821, sB:0.6445, nT:158442, nP:3.8476, nB:0.7742
Cond:305, Score:4064.762, sT:50462, sP:3.1819, sB:0.6459, nT:158861, nP:3.8509, nB:0.7571
Cond:5, Score:4047.846, sT:50489, sP:3.1805, sB:0.6387, nT:159143, nP:3.8471, nB:0.7661
Cond:677, Score:4038.977, sT:50321, sP:3.1862, sB:0.6399, nT:158688, nP:3.8531, nB:0.7669
Cond:513, Score:4033.053, sT:50490, sP:3.1804, sB:0.6352, nT:159015, nP:3.8477, nB:0.7678
Cond:19, Score:4028.15, sT:50430, sP:3.1821, sB:0.6376, nT:158562, nP:3.8529, nB:0.7615 , T3:35,49,275,305,5,677,513,19, 
LowScoreRank3 , T0:-1 , T1:1 , T2:
Cond:35, Score:7174.109, sT:50082, sP:3.1906, sB:0.667, nT:156568, nP:3.8696, nB:0.753
Cond:49, Score:7134.237, sT:50262, sP:3.1843, sB:0.6529, nT:157942, nP:3.8545, nB:0.765
Cond:275, Score:7113.811, sT:50414, sP:3.1821, sB:0.6445, nT:158442, nP:3.8476, nB:0.7742
Cond:305, Score:7090.574, sT:50462, sP:3.1819, sB:0.6459, nT:158861, nP:3.8509, nB:0.7571
Cond:5, Score:7063.292, sT:50489, sP:3.1805, sB:0.6387, nT:159143, nP:3.8471, nB:0.7661
Cond:677, Score:7046.664, sT:50321, sP:3.1862, sB:0.6399, nT:158688, nP:3.8531, nB:0.7669
Cond:513, Score:7037.951, sT:50490, sP:3.1804, sB:0.6352, nT:159015, nP:3.8477, nB:0.7678
Cond:19, Score:7027.721, sT:50430, sP:3.1821, sB:0.6376, nT:158562, nP:3.8529, nB:0.7615 , T3:35,49,275,305,5,677,513,19, 
LowScoreRank1 , T0:2806 , T1:1 , T2:
Cond:65, Score:2453.731, sT:50437, sP:3.1848, sB:0.6929, nT:158141, nP:3.8605, nB:0.7555
Cond:291, Score:2402.836, sT:50276, sP:3.1947, sB:0.6767, nT:157096, nP:3.8642, nB:0.7622
Cond:35, Score:2349.057, sT:50914, sP:3.18, sB:0.651, nT:159694, nP:3.8583, nB:0.7418
Cond:49, Score:2334.034, sT:51098, sP:3.1738, sB:0.6376, nT:161071, nP:3.8436, nB:0.7539
Cond:275, Score:2326.594, sT:51248, sP:3.1717, sB:0.6299, nT:161571, nP:3.8369, nB:0.7629
Cond:6130, Score:2321.714, sT:50784, sP:3.1346, sB:0.6243, nT:158587, nP:3.7304, nB:0.7733
Cond:305, Score:2319.609, sT:51297, sP:3.1715, sB:0.6312, nT:161990, nP:3.8401, nB:0.7461
Cond:721, Score:2317.717, sT:50431, sP:3.1827, sB:0.6462, nT:157823, nP:3.8528, nB:0.7432 , T3:65,291,35,49,275,6130,305,721, 
LowScoreRank2 , T0:2806 , T1:1 , T2:
Cond:65, Score:4273.845, sT:50437, sP:3.1848, sB:0.6929, nT:158141, nP:3.8605, nB:0.7555
Cond:291, Score:4185.704, sT:50276, sP:3.1947, sB:0.6767, nT:157096, nP:3.8642, nB:0.7622
Cond:35, Score:4095.338, sT:50914, sP:3.18, sB:0.651, nT:159694, nP:3.8583, nB:0.7418
Cond:49, Score:4071.97, sT:51098, sP:3.1738, sB:0.6376, nT:161071, nP:3.8436, nB:0.7539
Cond:275, Score:4060.692, sT:51248, sP:3.1717, sB:0.6299, nT:161571, nP:3.8369, nB:0.7629
Cond:6130, Score:4050.954, sT:50784, sP:3.1346, sB:0.6243, nT:158587, nP:3.7304, nB:0.7733
Cond:305, Score:4047.74, sT:51297, sP:3.1715, sB:0.6312, nT:161990, nP:3.8401, nB:0.7461
Cond:721, Score:4039.042, sT:50431, sP:3.1827, sB:0.6462, nT:157823, nP:3.8528, nB:0.7432 , T3:65,291,35,49,275,6130,305,721, 
LowScoreRank3 , T0:2806 , T1:1 , T2:
Cond:65, Score:7448.521, sT:50437, sP:3.1848, sB:0.6929, nT:158141, nP:3.8605, nB:0.7555
Cond:291, Score:7295.833, sT:50276, sP:3.1947, sB:0.6767, nT:157096, nP:3.8642, nB:0.7622
Cond:35, Score:7144.162, sT:50914, sP:3.18, sB:0.651, nT:159694, nP:3.8583, nB:0.7418
Cond:49, Score:7108.457, sT:51098, sP:3.1738, sB:0.6376, nT:161071, nP:3.8436, nB:0.7539
Cond:275, Score:7091.797, sT:51248, sP:3.1717, sB:0.6299, nT:161571, nP:3.8369, nB:0.7629
Cond:6130, Score:7072.635, sT:50784, sP:3.1346, sB:0.6243, nT:158587, nP:3.7304, nB:0.7733
Cond:305, Score:7067.81, sT:51297, sP:3.1715, sB:0.6312, nT:161990, nP:3.8401, nB:0.7461
Cond:721, Score:7043.069, sT:50431, sP:3.1827, sB:0.6462, nT:157823, nP:3.8528, nB:0.7432 , T3:65,291,35,49,275,6130,305,721, 
LowScoreRank1 , T0:5100 , T1:1 , T2:
Cond:51, Score:2462.273, sT:51689, sP:3.1988, sB:0.6926, nT:158278, nP:3.8758, nB:0.7268
Cond:65, Score:2461.077, sT:52574, sP:3.1713, sB:0.6716, nT:163122, nP:3.8388, nB:0.7322
Cond:529, Score:2427.304, sT:50135, sP:3.2341, sB:0.7085, nT:156033, nP:3.9309, nB:0.7173
Cond:291, Score:2414.596, sT:52446, sP:3.1797, sB:0.6558, nT:161953, nP:3.8425, nB:0.7433
Cond:35, Score:2354.785, sT:53062, sP:3.167, sB:0.6308, nT:164669, nP:3.837, nB:0.7189
Cond:8206, Score:2332.981, sT:50828, sP:3.1564, sB:0.633, nT:159953, nP:3.8163, nB:0.7714
Cond:49, Score:2331.717, sT:53318, sP:3.1587, sB:0.614, nT:166302, nP:3.8199, nB:0.7303
Cond:305, Score:2327.65, sT:53541, sP:3.1563, sB:0.6114, nT:167291, nP:3.8166, nB:0.7222 , T3:51,65,529,291,35,8206,49,305, 
LowScoreRank2 , T0:5100 , T1:1 , T2:
Cond:65, Score:4294.669, sT:52574, sP:3.1713, sB:0.6716, nT:163122, nP:3.8388, nB:0.7322
Cond:51, Score:4290.335, sT:51689, sP:3.1988, sB:0.6926, nT:158278, nP:3.8758, nB:0.7268
Cond:529, Score:4222.708, sT:50135, sP:3.2341, sB:0.7085, nT:156033, nP:3.9309, nB:0.7173
Cond:291, Score:4214.36, sT:52446, sP:3.1797, sB:0.6558, nT:161953, nP:3.8425, nB:0.7433
Cond:35, Score:4112.951, sT:53062, sP:3.167, sB:0.6308, nT:164669, nP:3.837, nB:0.7189
Cond:49, Score:4075.963, sT:53318, sP:3.1587, sB:0.614, nT:166302, nP:3.8199, nB:0.7303
Cond:8206, Score:4070.292, sT:50828, sP:3.1564, sB:0.633, nT:159953, nP:3.8163, nB:0.7714
Cond:305, Score:4069.557, sT:53541, sP:3.1563, sB:0.6114, nT:167291, nP:3.8166, nB:0.7222 , T3:65,51,529,291,35,49,8206,305, 
LowScoreRank3 , T0:5100 , T1:1 , T2:
Cond:65, Score:7498.732, sT:52574, sP:3.1713, sB:0.6716, nT:163122, nP:3.8388, nB:0.7322
Cond:51, Score:7479.755, sT:51689, sP:3.1988, sB:0.6926, nT:158278, nP:3.8758, nB:0.7268
Cond:291, Score:7359.951, sT:52446, sP:3.1797, sB:0.6558, nT:161953, nP:3.8425, nB:0.7433
Cond:529, Score:7350.266, sT:50135, sP:3.2341, sB:0.7085, nT:156033, nP:3.9309, nB:0.7173
Cond:35, Score:7188.119, sT:53062, sP:3.167, sB:0.6308, nT:164669, nP:3.837, nB:0.7189
Cond:49, Score:7129.395, sT:53318, sP:3.1587, sB:0.614, nT:166302, nP:3.8199, nB:0.7303
Cond:305, Score:7119.424, sT:53541, sP:3.1563, sB:0.6114, nT:167291, nP:3.8166, nB:0.7222
Cond:275, Score:7118.422, sT:53509, sP:3.1557, sB:0.6061, nT:166799, nP:3.8134, nB:0.7414 , T3:65,51,291,529,35,49,305,275, 
LowScoreRank1 , T0:2808 , T1:1 , T2:
Cond:737, Score:2637.656, sT:50484, sP:3.1964, sB:0.7583, nT:158801, nP:3.8845, nB:0.7682
Cond:65, Score:2584.266, sT:52948, sP:3.1907, sB:0.7009, nT:167862, nP:3.8652, nB:0.766
Cond:51, Score:2578.024, sT:52195, sP:3.2146, sB:0.718, nT:163507, nP:3.8976, nB:0.7581
Cond:529, Score:2548.769, sT:50697, sP:3.2489, sB:0.736, nT:161239, nP:3.9489, nB:0.7486
Cond:291, Score:2531.46, sT:52787, sP:3.2002, sB:0.6847, nT:166825, nP:3.8685, nB:0.7721
Cond:35, Score:2479.819, sT:53422, sP:3.1862, sB:0.6606, nT:169437, nP:3.863, nB:0.753
Cond:49, Score:2461.326, sT:53604, sP:3.1802, sB:0.6466, nT:170810, nP:3.8491, nB:0.7641
Cond:275, Score:2451.1, sT:53756, sP:3.1782, sB:0.6382, nT:171318, nP:3.8427, nB:0.7726 , T3:737,65,51,529,291,35,49,275, 
LowScoreRank2 , T0:2808 , T1:1 , T2:
Cond:737, Score:4591.215, sT:50484, sP:3.1964, sB:0.7583, nT:158801, nP:3.8845, nB:0.7682
Cond:65, Score:4513.304, sT:52948, sP:3.1907, sB:0.7009, nT:167862, nP:3.8652, nB:0.766
Cond:51, Score:4496.471, sT:52195, sP:3.2146, sB:0.718, nT:163507, nP:3.8976, nB:0.7581
Cond:529, Score:4438.556, sT:50697, sP:3.2489, sB:0.736, nT:161239, nP:3.9489, nB:0.7486
Cond:291, Score:4421.641, sT:52787, sP:3.2002, sB:0.6847, nT:166825, nP:3.8685, nB:0.7721
Cond:35, Score:4334.766, sT:53422, sP:3.1862, sB:0.6606, nT:169437, nP:3.863, nB:0.753
Cond:49, Score:4305.335, sT:53604, sP:3.1802, sB:0.6466, nT:170810, nP:3.8491, nB:0.7641
Cond:275, Score:4289.241, sT:53756, sP:3.1782, sB:0.6382, nT:171318, nP:3.8427, nB:0.7726 , T3:737,65,51,529,291,35,49,275, 
LowScoreRank3 , T0:2808 , T1:1 , T2:
Cond:737, Score:7996.286, sT:50484, sP:3.1964, sB:0.7583, nT:158801, nP:3.8845, nB:0.7682
Cond:65, Score:7887.113, sT:52948, sP:3.1907, sB:0.7009, nT:167862, nP:3.8652, nB:0.766
Cond:51, Score:7847.136, sT:52195, sP:3.2146, sB:0.718, nT:163507, nP:3.8976, nB:0.7581
Cond:529, Score:7734.117, sT:50697, sP:3.2489, sB:0.736, nT:161239, nP:3.9489, nB:0.7486
Cond:291, Score:7727.956, sT:52787, sP:3.2002, sB:0.6847, nT:166825, nP:3.8685, nB:0.7721
Cond:35, Score:7581.993, sT:53422, sP:3.1862, sB:0.6606, nT:169437, nP:3.863, nB:0.753
Cond:49, Score:7535.717, sT:53604, sP:3.1802, sB:0.6466, nT:170810, nP:3.8491, nB:0.7641
Cond:275, Score:7510.747, sT:53756, sP:3.1782, sB:0.6382, nT:171318, nP:3.8427, nB:0.7726 , T3:737,65,51,529,291,35,49,275, 
LowScoreRank1 , T0:126 , T1:1 , T2:
Cond:65, Score:2478.033, sT:52134, sP:3.1855, sB:0.6854, nT:163742, nP:3.8494, nB:0.7268
Cond:51, Score:2464.039, sT:51395, sP:3.2101, sB:0.6996, nT:159406, nP:3.882, nB:0.7176
Cond:291, Score:2415.319, sT:51975, sP:3.1942, sB:0.6653, nT:162719, nP:3.8528, nB:0.7326
Cond:35, Score:2354.802, sT:52616, sP:3.1805, sB:0.6381, nT:165346, nP:3.8471, nB:0.7132
Cond:49, Score:2342.095, sT:52792, sP:3.1746, sB:0.6261, nT:166716, nP:3.8331, nB:0.725
Cond:721, Score:2334.154, sT:52134, sP:3.1828, sB:0.6373, nT:163405, nP:3.8421, nB:0.7137
Cond:275, Score:2330.641, sT:52939, sP:3.1727, sB:0.6173, nT:167224, nP:3.8265, nB:0.7337
Cond:305, Score:2323.625, sT:52986, sP:3.1724, sB:0.6185, nT:167647, nP:3.8297, nB:0.7177 , T3:65,51,291,35,49,721,275,305, 
LowScoreRank2 , T0:126 , T1:1 , T2:
Cond:65, Score:4322.327, sT:52134, sP:3.1855, sB:0.6854, nT:163742, nP:3.8494, nB:0.7268
Cond:51, Score:4292.342, sT:51395, sP:3.2101, sB:0.6996, nT:159406, nP:3.882, nB:0.7176
Cond:291, Score:4213.704, sT:51975, sP:3.1942, sB:0.6653, nT:162719, nP:3.8528, nB:0.7326
Cond:35, Score:4111.523, sT:52616, sP:3.1805, sB:0.6381, nT:165346, nP:3.8471, nB:0.7132
Cond:49, Score:4092.027, sT:52792, sP:3.1746, sB:0.6261, nT:166716, nP:3.8331, nB:0.725
Cond:275, Score:4073.786, sT:52939, sP:3.1727, sB:0.6173, nT:167224, nP:3.8265, nB:0.7337
Cond:721, Score:4073.517, sT:52134, sP:3.1828, sB:0.6373, nT:163405, nP:3.8421, nB:0.7137
Cond:305, Score:4060.764, sT:52986, sP:3.1724, sB:0.6185, nT:167647, nP:3.8297, nB:0.7177 , T3:65,51,291,35,49,275,721,305, 
LowScoreRank3 , T0:126 , T1:1 , T2:
Cond:65, Score:7543.711, sT:52134, sP:3.1855, sB:0.6854, nT:163742, nP:3.8494, nB:0.7268
Cond:51, Score:7481.464, sT:51395, sP:3.2101, sB:0.6996, nT:159406, nP:3.882, nB:0.7176
Cond:291, Score:7355.528, sT:51975, sP:3.1942, sB:0.6653, nT:162719, nP:3.8528, nB:0.7326
Cond:35, Score:7183.156, sT:52616, sP:3.1805, sB:0.6381, nT:165346, nP:3.8471, nB:0.7132
Cond:49, Score:7153.926, sT:52792, sP:3.1746, sB:0.6261, nT:166716, nP:3.8331, nB:0.725
Cond:275, Score:7125.193, sT:52939, sP:3.1727, sB:0.6173, nT:167224, nP:3.8265, nB:0.7337
Cond:721, Score:7113.325, sT:52134, sP:3.1828, sB:0.6373, nT:163405, nP:3.8421, nB:0.7137
Cond:305, Score:7101.052, sT:52986, sP:3.1724, sB:0.6185, nT:167647, nP:3.8297, nB:0.7177 , T3:65,51,291,35,49,275,721,305, 
LowScoreRank1 , T0:8354 , T1:1 , T2:
Cond:737, Score:2405.114, sT:51070, sP:3.2443, sB:0.7005, nT:162845, nP:4.0157, nB:0.6697
Cond:65, Score:2355.954, sT:53497, sP:3.2371, sB:0.6473, nT:171843, nP:3.9902, nB:0.6723
Cond:51, Score:2348.66, sT:52747, sP:3.262, sB:0.6629, nT:167534, nP:4.0248, nB:0.6623
Cond:529, Score:2325.082, sT:51280, sP:3.2957, sB:0.6801, nT:165341, nP:4.076, nB:0.6523
Cond:291, Score:2302.322, sT:53334, sP:3.2462, sB:0.6305, nT:170819, nP:3.9944, nB:0.6778
Cond:8354, Score:2294.373, sT:50580, sP:3.1775, sB:0.6267, nT:159587, nP:3.8414, nB:0.7668
Cond:8368, Score:2274.777, sT:52602, sP:3.1865, sB:0.6051, nT:167807, nP:3.8693, nB:0.7272
Cond:6130, Score:2261.74, sT:53368, sP:3.183, sB:0.5928, nT:169571, nP:3.8243, nB:0.7169 , T3:737,65,51,529,291,8354,8368,6130, 
LowScoreRank2 , T0:8354 , T1:1 , T2:
Cond:737, Score:4187.069, sT:51070, sP:3.2443, sB:0.7005, nT:162845, nP:4.0157, nB:0.6697
Cond:65, Score:4115.123, sT:53497, sP:3.2371, sB:0.6473, nT:171843, nP:3.9902, nB:0.6723
Cond:51, Score:4096.95, sT:52747, sP:3.262, sB:0.6629, nT:167534, nP:4.0248, nB:0.6623
Cond:529, Score:4049.566, sT:51280, sP:3.2957, sB:0.6801, nT:165341, nP:4.076, nB:0.6523
Cond:291, Score:4022.08, sT:53334, sP:3.2462, sB:0.6305, nT:170819, nP:3.9944, nB:0.6778
Cond:8354, Score:4002.363, sT:50580, sP:3.1775, sB:0.6267, nT:159587, nP:3.8414, nB:0.7668
Cond:8368, Score:3976.016, sT:52602, sP:3.1865, sB:0.6051, nT:167807, nP:3.8693, nB:0.7272
Cond:6130, Score:3956.314, sT:53368, sP:3.183, sB:0.5928, nT:169571, nP:3.8243, nB:0.7169 , T3:737,65,51,529,291,8354,8368,6130, 
LowScoreRank3 , T0:8354 , T1:1 , T2:
Cond:737, Score:7293.456, sT:51070, sP:3.2443, sB:0.7005, nT:162845, nP:4.0157, nB:0.6697
Cond:65, Score:7192.238, sT:53497, sP:3.2371, sB:0.6473, nT:171843, nP:3.9902, nB:0.6723
Cond:51, Score:7150.795, sT:52747, sP:3.262, sB:0.6629, nT:167534, nP:4.0248, nB:0.6623
Cond:529, Score:7057.228, sT:51280, sP:3.2957, sB:0.6801, nT:165341, nP:4.076, nB:0.6523
Cond:291, Score:7030.782, sT:53334, sP:3.2462, sB:0.6305, nT:170819, nP:3.9944, nB:0.6778
Cond:8354, Score:6986.306, sT:50580, sP:3.1775, sB:0.6267, nT:159587, nP:3.8414, nB:0.7668
Cond:8368, Score:6954.081, sT:52602, sP:3.1865, sB:0.6051, nT:167807, nP:3.8693, nB:0.7272
Cond:6130, Score:6925.015, sT:53368, sP:3.183, sB:0.5928, nT:169571, nP:3.8243, nB:0.7169 , T3:737,65,51,529,291,8354,8368,6130, 
LowScoreRank1 , T0:8344 , T1:1 , T2:
Cond:737, Score:2451.551, sT:50651, sP:3.4235, sB:0.7349, nT:152577, nP:4.001, nB:0.7519
Cond:65, Score:2364.643, sT:53522, sP:3.4531, sB:0.667, nT:162719, nP:4.0072, nB:0.7441
Cond:51, Score:2361.164, sT:52871, sP:3.4876, sB:0.6854, nT:158344, nP:4.0483, nB:0.7334
Cond:529, Score:2321.479, sT:51186, sP:3.5164, sB:0.6997, nT:156053, nP:4.1084, nB:0.7205
Cond:291, Score:2320.827, sT:53503, sP:3.4689, sB:0.6529, nT:161912, nP:4.0117, nB:0.7478
Cond:8344, Score:2294.373, sT:50580, sP:3.1775, sB:0.6267, nT:159587, nP:3.8414, nB:0.7668
Cond:35, Score:2268.824, sT:54175, sP:3.4517, sB:0.6285, nT:164907, nP:4.0165, nB:0.726
Cond:723, Score:2254.871, sT:50156, sP:3.5676, sB:0.7034, nT:146408, nP:4.0896, nB:0.7116 , T3:737,65,51,529,291,8344,35,723, 
LowScoreRank2 , T0:8344 , T1:1 , T2:
Cond:737, Score:4265.901, sT:50651, sP:3.4235, sB:0.7349, nT:152577, nP:4.001, nB:0.7519
Cond:65, Score:4130.491, sT:53522, sP:3.4531, sB:0.667, nT:162719, nP:4.0072, nB:0.7441
Cond:51, Score:4118.938, sT:52871, sP:3.4876, sB:0.6854, nT:158344, nP:4.0483, nB:0.7334
Cond:291, Score:4054.679, sT:53503, sP:3.4689, sB:0.6529, nT:161912, nP:4.0117, nB:0.7478
Cond:529, Score:4042.879, sT:51186, sP:3.5164, sB:0.6997, nT:156053, nP:4.1084, nB:0.7205
Cond:8344, Score:4002.363, sT:50580, sP:3.1775, sB:0.6267, nT:159587, nP:3.8414, nB:0.7668
Cond:35, Score:3966.965, sT:54175, sP:3.4517, sB:0.6285, nT:164907, nP:4.0165, nB:0.726
Cond:49, Score:3942.958, sT:54316, sP:3.4447, sB:0.6154, nT:166223, nP:3.9991, nB:0.7389 , T3:737,65,51,291,529,8344,35,49, 
LowScoreRank3 , T0:8344 , T1:1 , T2:
Cond:737, Score:7426.991, sT:50651, sP:3.4235, sB:0.7349, nT:152577, nP:4.001, nB:0.7519
Cond:65, Score:7219.157, sT:53522, sP:3.4531, sB:0.667, nT:162719, nP:4.0072, nB:0.7441
Cond:51, Score:7189.184, sT:52871, sP:3.4876, sB:0.6854, nT:158344, nP:4.0483, nB:0.7334
Cond:291, Score:7087.939, sT:53503, sP:3.4689, sB:0.6529, nT:161912, nP:4.0117, nB:0.7478
Cond:529, Score:7044.596, sT:51186, sP:3.5164, sB:0.6997, nT:156053, nP:4.1084, nB:0.7205
Cond:8344, Score:6986.306, sT:50580, sP:3.1775, sB:0.6267, nT:159587, nP:3.8414, nB:0.7668
Cond:35, Score:6940.163, sT:54175, sP:3.4517, sB:0.6285, nT:164907, nP:4.0165, nB:0.726
Cond:49, Score:6902.879, sT:54316, sP:3.4447, sB:0.6154, nT:166223, nP:3.9991, nB:0.7389 , T3:737,65,51,291,529,8344,35,49, 
End , T0:04:17:05.1711452 

 */
