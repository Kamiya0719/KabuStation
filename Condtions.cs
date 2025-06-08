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
		private static readonly int[] ConfirmAnds = new int[] {
			1682,8408,5958,1282,6218,4999,8476,4476,4656,8444,6270,8478,6202,8426,8026,7612,8428,4462,222,2804,1474,1664,2786,206,2806,4356,6168,6186,862,2862,8412,8430,
			6170, // 611143,-0.230667781517583,
			981, // 676318,-0.222154371168592,
			5324, // 732437,-0.215408287675254,
			5100, // 783284,-0.211427783537006,
			2830, // 937785,-0.203682080647483,
			2808, // 1056027,-0.197063143271905,
			126,
			8376, // ２　T0:220872, T1:0.567,  T2:10 #End#  T0:58625, T1:0.701, T2:6 #End#
			8372, // ３　T0:135461, T1:0.7001, T2:5.6597 #End#, T0:47472, T1:0.7487, T2:4.4662  #End#
			8354,
			8344, // 
			6130, // 
		};
		private static readonly int[] ConfirmOrs = new int[] {
			706,7404,528,5164,52,5388,2322,845,619,66,7196,1065,5983,8395,8167,6851,8375,

			4524, // １　T0:89100,T1:0.5823,T2:7.528
			1618, // ４　T0:48934, T1:0.7123, T2:4.5271 #End#, T0:143399, T1:0.7365, T2:5.661 #End#
			7599, // ５　T0:49996, T2:0.7005, T3:4.5082 #End#, T0:147404, T2:0.7211, T3:5.6259 #End#
			7436, // 
			6717, // 
			5041, // 
			//6159, // 仮  T0:65692, T1:0.6371, T2:5.4129 #End#, T0:202915, T1:0.5783, T2:6.848  #End#
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
			6130,
1395,
65, 
275,
1575,
6385,
2695,
291,

		};
		private static readonly int[] KouhoOrs = new int[] {
			//AllTrueCondIdx-1

		};
		private const int AllCond51Num = 3754886; // 2000日*2500銘柄
		private const double AllCond51Ratio = -0.000912;
		/** 51条件の全検証 */
		public static void CheckCond51All()
		{
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

					scores[j] = Common.Round(Math.Pow(trueAll[i, j, 1], 0.75) * skipBenefit[j] / Math.Pow(skipPeriod[j], 0.85)
						+ Math.Pow(trueAll[i, j, 0], 0.75) * noBenefit[j] / Math.Pow(noPeriod[j], 0.9) / 3, 3);
					scores2[j] = Common.Round(Math.Pow(trueAll[i, j, 1], 0.8) * skipBenefit[j] / Math.Pow(skipPeriod[j], 0.85)
						+ Math.Pow(trueAll[i, j, 0], 0.8) * noBenefit[j] / Math.Pow(noPeriod[j], 0.9) / 3, 3);
					scores3[j] = Common.Round(Math.Pow(trueAll[i, j, 1], 0.85) * skipBenefit[j] / Math.Pow(skipPeriod[j], 0.85)
						+ Math.Pow(trueAll[i, j, 0], 0.85) * noBenefit[j] / Math.Pow(noPeriod[j], 0.9) / 3, 3);
					scores4[j] = Common.Round(Math.Pow(trueAll[i, j, 1], 0.9) * skipBenefit[j] / Math.Pow(skipPeriod[j], 0.85)
						+ Math.Pow(trueAll[i, j, 0], 0.9) * noBenefit[j] / Math.Pow(noPeriod[j], 0.9) / 3, 3);

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
				for (int r = 0; r < 4; r++) {
					string result = ""; string result2 = ""; int max = 30;
					foreach (KeyValuePair<int, double> b in (r==0?scores: r == 1 ? scores2 : r == 2 ? scores3 : scores4).OrderByDescending(c => c.Value)) {
						if (max > 0) {
							result += "\nCond:" + b.Key + ", Score:" + b.Value + ", sT:" + trueAll[i, b.Key, 1] + ", sP:" + skipPeriod[b.Key] + ", sB:" + skipBenefit[b.Key]
								+ ", nT:" + trueAll[i, b.Key, 0] + ", nP:" + noPeriod[b.Key] + ", nB:" + noBenefit[b.Key];
							result2 += b.Key + ",";
						}
						max--;
					}
					Common.DebugInfo("LowScoreRank" + r, kouhoList[i], result, result2);
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

			for(int i = 0; i < confirmAnds.Length; i++) {
				DebugCheckCond51ScoreBase(confirmAnds, confirmOrs, i, -1);
			}
			for (int i = 0; i < confirmOrs.Length; i++) {
				DebugCheckCond51ScoreBase(confirmAnds, confirmOrs, -1, i);
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
			double score = Common.Round(Math.Pow(skipTrue, 0.85) * skipB / Math.Pow(skipP, 0.85)
				+ Math.Pow(noTrue, 0.85) * noB / Math.Pow(noP, 0.85) / 3, 3);
			Common.DebugInfo("DebugCheckCond51", andSkip, orSkip, score, skipTrue, Common.Round(skipB, 2), Common.Round(skipP, 2), noTrue, Common.Round(noB, 2), Common.Round(noP, 2));
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


 DebugCheckCond51 , T0:0 , T1:-1 , T2:4366.456 , T3:42592 , T4:0.78 , T5:3.44 , T6:128285 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:1 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:2 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:3 , T1:-1 , T2:4365.501 , T3:42593 , T4:0.78 , T5:3.44 , T6:128255 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:4 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:5 , T1:-1 , T2:4355.432 , T3:42864 , T4:0.77 , T5:3.42 , T6:128905 , T7:0.88 , T8:3.97  #End#
DebugCheckCond51 , T0:6 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:7 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:8 , T1:-1 , T2:4363.196 , T3:42595 , T4:0.78 , T5:3.44 , T6:128261 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:9 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:10 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:11 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:12 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:13 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:14 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:15 , T1:-1 , T2:4356.833 , T3:42584 , T4:0.78 , T5:3.44 , T6:128254 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:16 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:17 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:18 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:19 , T1:-1 , T2:4370.598 , T3:42609 , T4:0.78 , T5:3.44 , T6:128370 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:20 , T1:-1 , T2:4364.727 , T3:42786 , T4:0.78 , T5:3.43 , T6:128873 , T7:0.88 , T8:3.97  #End#
DebugCheckCond51 , T0:21 , T1:-1 , T2:4406.172 , T3:42903 , T4:0.78 , T5:3.43 , T6:130376 , T7:0.88 , T8:3.98  #End#
DebugCheckCond51 , T0:22 , T1:-1 , T2:4365.229 , T3:42737 , T4:0.78 , T5:3.44 , T6:129242 , T7:0.88 , T8:3.98  #End#
DebugCheckCond51 , T0:23 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:24 , T1:-1 , T2:4353.954 , T3:43200 , T4:0.76 , T5:3.43 , T6:130514 , T7:0.88 , T8:3.96  #End#
DebugCheckCond51 , T0:25 , T1:-1 , T2:4368.806 , T3:42586 , T4:0.78 , T5:3.44 , T6:128286 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:26 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:27 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:28 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:29 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:30 , T1:-1 , T2:4365.294 , T3:42567 , T4:0.78 , T5:3.44 , T6:128188 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:31 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:32 , T1:-1 , T2:4362.189 , T3:42574 , T4:0.78 , T5:3.44 , T6:128217 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:33 , T1:-1 , T2:4512.765 , T3:50017 , T4:0.64 , T5:3.21 , T6:153685 , T7:0.79 , T8:3.78  #End#
DebugCheckCond51 , T0:34 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:35 , T1:-1 , T2:4393.302 , T3:43836 , T4:0.76 , T5:3.43 , T6:131573 , T7:0.88 , T8:3.97  #End#
DebugCheckCond51 , T0:36 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:37 , T1:-1 , T2:4605.062 , T3:45670 , T4:0.76 , T5:3.41 , T6:139284 , T7:0.88 , T8:3.95  #End#
DebugCheckCond51 , T0:38 , T1:-1 , T2:4352.487 , T3:44426 , T4:0.75 , T5:3.43 , T6:134711 , T7:0.85 , T8:3.96  #End#
DebugCheckCond51 , T0:39 , T1:-1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:40 , T1:-1 , T2:4362.984 , T3:42568 , T4:0.78 , T5:3.44 , T6:128195 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:41 , T1:-1 , T2:4217.273 , T3:46612 , T4:0.71 , T5:3.49 , T6:142942 , T7:0.8 , T8:4.08  #End#
DebugCheckCond51 , T0:42 , T1:-1 , T2:4201.974 , T3:46693 , T4:0.74 , T5:3.72 , T6:136993 , T7:0.85 , T8:4.15  #End#
DebugCheckCond51 , T0:43 , T1:-1 , T2:4275.712 , T3:43325 , T4:0.77 , T5:3.48 , T6:132472 , T7:0.86 , T8:4.1  #End#
DebugCheckCond51 , T0:-1 , T1:0 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:-1 , T1:1 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:-1 , T1:2 , T2:4357.572 , T3:42434 , T4:0.78 , T5:3.44 , T6:127823 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:-1 , T1:3 , T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98  #End#
DebugCheckCond51 , T0:-1 , T1:4 , T2:4323.745 , T3:42155 , T4:0.78 , T5:3.43 , T6:126835 , T7:0.89 , T8:3.98  #End#



LowScoreRank0 , T0:6130 , T1:
Cond:4887, Score:1393.245, sT:42614, sP:3.4374, sB:0.7833, nT:128397, nP:3.9783, nB:0.8887
Cond:4833, Score:1393.188, sT:42630, sP:3.4354, sB:0.7821, nT:128388, nP:3.9758, nB:0.8891
Cond:1973, Score:1393.028, sT:42697, sP:3.4357, sB:0.7807, nT:128664, nP:3.9764, nB:0.8884
Cond:6903, Score:1392.973, sT:42639, sP:3.4383, sB:0.7832, nT:128461, nP:3.9791, nB:0.888
Cond:1077, Score:1392.777, sT:42647, sP:3.4362, sB:0.7815, nT:128465, nP:3.9763, nB:0.889
Cond:1515, Score:1392.556, sT:42581, sP:3.4383, sB:0.7842, nT:128292, nP:3.9795, nB:0.888
Cond:6469, Score:1392.353, sT:42673, sP:3.4377, sB:0.782, nT:128606, nP:3.9797, nB:0.8874
Cond:6695, Score:1392.129, sT:42601, sP:3.4368, sB:0.7825, nT:128282, nP:3.978, nB:0.8889
Cond:3265, Score:1392.024, sT:42594, sP:3.4363, sB:0.7823, nT:128281, nP:3.9767, nB:0.8888
Cond:6697, Score:1391.859, sT:42621, sP:3.4376, sB:0.7819, nT:128411, nP:3.9786, nB:0.8887
Cond:5303, Score:1391.817, sT:42619, sP:3.4366, sB:0.7824, nT:128410, nP:3.9782, nB:0.8875
Cond:8130, Score:1391.811, sT:42604, sP:3.4356, sB:0.7823, nT:128323, nP:3.9756, nB:0.8876
Cond:6693, Score:1391.736, sT:42586, sP:3.4374, sB:0.7823, nT:128257, nP:3.9787, nB:0.8894
Cond:7581, Score:1391.629, sT:42684, sP:3.4385, sB:0.7808, nT:128511, nP:3.9797, nB:0.8887
Cond:7789, Score:1391.545, sT:42734, sP:3.4429, sB:0.7817, nT:128360, nP:3.9793, nB:0.8881
Cond:1993, Score:1391.43, sT:42568, sP:3.4376, sB:0.7828, nT:128213, nP:3.9781, nB:0.8887
Cond:4677, Score:1391.401, sT:42600, sP:3.4366, sB:0.7815, nT:128312, nP:3.9781, nB:0.8892
Cond:8144, Score:1391.386, sT:42611, sP:3.4358, sB:0.7819, nT:128341, nP:3.9756, nB:0.8874
Cond:2411, Score:1391.259, sT:42655, sP:3.4375, sB:0.7811, nT:128554, nP:3.9809, nB:0.888
Cond:7337, Score:1391.23, sT:42647, sP:3.4377, sB:0.7809, nT:128463, nP:3.979, nB:0.8886
Cond:1977, Score:1391.228, sT:42660, sP:3.4376, sB:0.7782, nT:128625, nP:3.9798, nB:0.8919
Cond:7545, Score:1391.122, sT:42595, sP:3.4378, sB:0.7817, nT:128303, nP:3.9787, nB:0.8891
Cond:7095, Score:1390.995, sT:42873, sP:3.4397, sB:0.7779, nT:129439, nP:3.9831, nB:0.8845
Cond:3299, Score:1390.938, sT:42571, sP:3.4376, sB:0.7821, nT:128227, nP:3.978, nB:0.8889
Cond:3321, Score:1390.93, sT:42620, sP:3.4375, sB:0.7815, nT:128435, nP:3.9792, nB:0.8879
Cond:3150, Score:1390.801, sT:42605, sP:3.4365, sB:0.7811, nT:128365, nP:3.9771, nB:0.8883
Cond:1457, Score:1390.763, sT:42575, sP:3.437, sB:0.7823, nT:128208, nP:3.9778, nB:0.8881
Cond:2203, Score:1390.758, sT:42594, sP:3.4369, sB:0.7818, nT:128293, nP:3.9783, nB:0.8881
Cond:3160, Score:1390.726, sT:42670, sP:3.4379, sB:0.7809, nT:128526, nP:3.9792, nB:0.8871
Cond:6467, Score:1390.724, sT:42623, sP:3.4383, sB:0.7817, nT:128417, nP:3.98, nB:0.8877 , T2:4887,4833,1973,6903,1077,1515,6469,6695,3265,6697,5303,8130,6693,7581,7789,1993,4677,8144,2411,7337,1977,7545,7095,3299,3321,3150,1457,2203,3160,6467,  #End#
LowScoreRank1 , T0:6130 , T1:
Cond:4887, Score:2430.155, sT:42614, sP:3.4374, sB:0.7833, nT:128397, nP:3.9783, nB:0.8887
Cond:4833, Score:2430.135, sT:42630, sP:3.4354, sB:0.7821, nT:128388, nP:3.9758, nB:0.8891
Cond:1973, Score:2430.121, sT:42697, sP:3.4357, sB:0.7807, nT:128664, nP:3.9764, nB:0.8884
Cond:6903, Score:2429.724, sT:42639, sP:3.4383, sB:0.7832, nT:128461, nP:3.9791, nB:0.888
Cond:1077, Score:2429.505, sT:42647, sP:3.4362, sB:0.7815, nT:128465, nP:3.9763, nB:0.889
Cond:1515, Score:2428.791, sT:42581, sP:3.4383, sB:0.7842, nT:128292, nP:3.9795, nB:0.888
Cond:6469, Score:2428.784, sT:42673, sP:3.4377, sB:0.782, nT:128606, nP:3.9797, nB:0.8874
Cond:6695, Score:2428.164, sT:42601, sP:3.4368, sB:0.7825, nT:128282, nP:3.978, nB:0.8889
Cond:3265, Score:2427.983, sT:42594, sP:3.4363, sB:0.7823, nT:128281, nP:3.9767, nB:0.8888
Cond:6697, Score:2427.811, sT:42621, sP:3.4376, sB:0.7819, nT:128411, nP:3.9786, nB:0.8887
Cond:5303, Score:2427.664, sT:42619, sP:3.4366, sB:0.7824, nT:128410, nP:3.9782, nB:0.8875
Cond:8130, Score:2427.606, sT:42604, sP:3.4356, sB:0.7823, nT:128323, nP:3.9756, nB:0.8876
Cond:7581, Score:2427.581, sT:42684, sP:3.4385, sB:0.7808, nT:128511, nP:3.9797, nB:0.8887
Cond:6693, Score:2427.474, sT:42586, sP:3.4374, sB:0.7823, nT:128257, nP:3.9787, nB:0.8894
Cond:7789, Score:2427.375, sT:42734, sP:3.4429, sB:0.7817, nT:128360, nP:3.9793, nB:0.8881
Cond:7095, Score:2427.174, sT:42873, sP:3.4397, sB:0.7779, nT:129439, nP:3.9831, nB:0.8845
Cond:1977, Score:2427.143, sT:42660, sP:3.4376, sB:0.7782, nT:128625, nP:3.9798, nB:0.8919
Cond:4677, Score:2426.961, sT:42600, sP:3.4366, sB:0.7815, nT:128312, nP:3.9781, nB:0.8892
Cond:8144, Score:2426.893, sT:42611, sP:3.4358, sB:0.7819, nT:128341, nP:3.9756, nB:0.8874
Cond:2411, Score:2426.876, sT:42655, sP:3.4375, sB:0.7811, nT:128554, nP:3.9809, nB:0.888
Cond:1993, Score:2426.855, sT:42568, sP:3.4376, sB:0.7828, nT:128213, nP:3.9781, nB:0.8887
Cond:7337, Score:2426.808, sT:42647, sP:3.4377, sB:0.7809, nT:128463, nP:3.979, nB:0.8886
Cond:3327, Score:2426.674, sT:43115, sP:3.4444, sB:0.7759, nT:130020, nP:3.982, nB:0.8798
Cond:7545, Score:2426.457, sT:42595, sP:3.4378, sB:0.7817, nT:128303, nP:3.9787, nB:0.8891
Cond:3321, Score:2426.186, sT:42620, sP:3.4375, sB:0.7815, nT:128435, nP:3.9792, nB:0.8879
Cond:3299, Score:2426.045, sT:42571, sP:3.4376, sB:0.7821, nT:128227, nP:3.978, nB:0.8889
Cond:3150, Score:2425.941, sT:42605, sP:3.4365, sB:0.7811, nT:128365, nP:3.9771, nB:0.8883
Cond:3160, Score:2425.935, sT:42670, sP:3.4379, sB:0.7809, nT:128526, nP:3.9792, nB:0.8871
Cond:3093, Score:2425.83, sT:42782, sP:3.4348, sB:0.7779, nT:129014, nP:3.9763, nB:0.8849
Cond:6467, Score:2425.804, sT:42623, sP:3.4383, sB:0.7817, nT:128417, nP:3.98, nB:0.8877 , T2:4887,4833,1973,6903,1077,1515,6469,6695,3265,6697,5303,8130,7581,6693,7789,7095,1977,4677,8144,2411,1993,7337,3327,7545,3321,3299,3150,3160,3093,6467,  #End#
LowScoreRank2 , T0:6130 , T1:
Cond:1973, Score:4242.48, sT:42697, sP:3.4357, sB:0.7807, nT:128664, nP:3.9764, nB:0.8884
Cond:4833, Score:4242.035, sT:42630, sP:3.4354, sB:0.7821, nT:128388, nP:3.9758, nB:0.8891
Cond:4887, Score:4241.935, sT:42614, sP:3.4374, sB:0.7833, nT:128397, nP:3.9783, nB:0.8887
Cond:6903, Score:4241.26, sT:42639, sP:3.4383, sB:0.7832, nT:128461, nP:3.9791, nB:0.888
Cond:1077, Score:4241.091, sT:42647, sP:3.4362, sB:0.7815, nT:128465, nP:3.9763, nB:0.889
Cond:6469, Score:4239.866, sT:42673, sP:3.4377, sB:0.782, nT:128606, nP:3.9797, nB:0.8874
Cond:1515, Score:4239.27, sT:42581, sP:3.4383, sB:0.7842, nT:128292, nP:3.9795, nB:0.888
Cond:3327, Score:4238.476, sT:43115, sP:3.4444, sB:0.7759, nT:130020, nP:3.982, nB:0.8798
Cond:7095, Score:4238.395, sT:42873, sP:3.4397, sB:0.7779, nT:129439, nP:3.9831, nB:0.8845
Cond:6695, Score:4238.38, sT:42601, sP:3.4368, sB:0.7825, nT:128282, nP:3.978, nB:0.8889
Cond:3265, Score:4238.068, sT:42594, sP:3.4363, sB:0.7823, nT:128281, nP:3.9767, nB:0.8888
Cond:6697, Score:4237.973, sT:42621, sP:3.4376, sB:0.7819, nT:128411, nP:3.9786, nB:0.8887
Cond:7581, Score:4237.865, sT:42684, sP:3.4385, sB:0.7808, nT:128511, nP:3.9797, nB:0.8887
Cond:5303, Score:4237.589, sT:42619, sP:3.4366, sB:0.7824, nT:128410, nP:3.9782, nB:0.8875
Cond:1977, Score:4237.57, sT:42660, sP:3.4376, sB:0.7782, nT:128625, nP:3.9798, nB:0.8919
Cond:8130, Score:4237.399, sT:42604, sP:3.4356, sB:0.7823, nT:128323, nP:3.9756, nB:0.8876
Cond:7789, Score:4237.388, sT:42734, sP:3.4429, sB:0.7817, nT:128360, nP:3.9793, nB:0.8881
Cond:6693, Score:4237.169, sT:42586, sP:3.4374, sB:0.7823, nT:128257, nP:3.9787, nB:0.8894
Cond:2411, Score:4236.535, sT:42655, sP:3.4375, sB:0.7811, nT:128554, nP:3.9809, nB:0.888
Cond:4677, Score:4236.4, sT:42600, sP:3.4366, sB:0.7815, nT:128312, nP:3.9781, nB:0.8892
Cond:7337, Score:4236.385, sT:42647, sP:3.4377, sB:0.7809, nT:128463, nP:3.979, nB:0.8886
Cond:8144, Score:4236.206, sT:42611, sP:3.4358, sB:0.7819, nT:128341, nP:3.9756, nB:0.8874
Cond:1993, Score:4235.939, sT:42568, sP:3.4376, sB:0.7828, nT:128213, nP:3.9781, nB:0.8887
Cond:7545, Score:4235.49, sT:42595, sP:3.4378, sB:0.7817, nT:128303, nP:3.9787, nB:0.8891
Cond:3093, Score:4235.48, sT:42782, sP:3.4348, sB:0.7779, nT:129014, nP:3.9763, nB:0.8849
Cond:3119, Score:4235.257, sT:42768, sP:3.4391, sB:0.7791, nT:128917, nP:3.9776, nB:0.8854
Cond:3321, Score:4235.129, sT:42620, sP:3.4375, sB:0.7815, nT:128435, nP:3.9792, nB:0.8879
Cond:3160, Score:4234.872, sT:42670, sP:3.4379, sB:0.7809, nT:128526, nP:3.9792, nB:0.8871
Cond:3150, Score:4234.668, sT:42605, sP:3.4365, sB:0.7811, nT:128365, nP:3.9771, nB:0.8883
Cond:3299, Score:4234.611, sT:42571, sP:3.4376, sB:0.7821, nT:128227, nP:3.978, nB:0.8889 , T2:1973,4833,4887,6903,1077,6469,1515,3327,7095,6695,3265,6697,7581,5303,1977,8130,7789,6693,2411,4677,7337,8144,1993,7545,3093,3119,3321,3160,3150,3299,  #End#
LowScoreRank3 , T0:6130 , T1:
Cond:1973, Score:7412.04, sT:42697, sP:3.4357, sB:0.7807, nT:128664, nP:3.9764, nB:0.8884
Cond:4833, Score:7410.437, sT:42630, sP:3.4354, sB:0.7821, nT:128388, nP:3.9758, nB:0.8891
Cond:4887, Score:7410.029, sT:42614, sP:3.4374, sB:0.7833, nT:128397, nP:3.9783, nB:0.8887
Cond:1077, Score:7409.063, sT:42647, sP:3.4362, sB:0.7815, nT:128465, nP:3.9763, nB:0.889
Cond:6903, Score:7408.983, sT:42639, sP:3.4383, sB:0.7832, nT:128461, nP:3.9791, nB:0.888
Cond:3327, Score:7408.571, sT:43115, sP:3.4444, sB:0.7759, nT:130020, nP:3.982, nB:0.8798
Cond:6469, Score:7406.986, sT:42673, sP:3.4377, sB:0.782, nT:128606, nP:3.9797, nB:0.8874
Cond:7095, Score:7406.775, sT:42873, sP:3.4397, sB:0.7779, nT:129439, nP:3.9831, nB:0.8845
Cond:1515, Score:7404.876, sT:42581, sP:3.4383, sB:0.7842, nT:128292, nP:3.9795, nB:0.888
Cond:1977, Score:7403.976, sT:42660, sP:3.4376, sB:0.7782, nT:128625, nP:3.9798, nB:0.8919
Cond:6695, Score:7403.673, sT:42601, sP:3.4368, sB:0.7825, nT:128282, nP:3.978, nB:0.8889
Cond:7581, Score:7403.655, sT:42684, sP:3.4385, sB:0.7808, nT:128511, nP:3.9797, nB:0.8887
Cond:6697, Score:7403.334, sT:42621, sP:3.4376, sB:0.7819, nT:128411, nP:3.9786, nB:0.8887
Cond:3265, Score:7403.139, sT:42594, sP:3.4363, sB:0.7823, nT:128281, nP:3.9767, nB:0.8888
Cond:7672, Score:7403.139, sT:43345, sP:3.4222, sB:0.764, nT:131235, nP:3.9663, nB:0.8733
Cond:7789, Score:7402.59, sT:42734, sP:3.4429, sB:0.7817, nT:128360, nP:3.9793, nB:0.8881
Cond:5303, Score:7402.438, sT:42619, sP:3.4366, sB:0.7824, nT:128410, nP:3.9782, nB:0.8875
Cond:8130, Score:7401.953, sT:42604, sP:3.4356, sB:0.7823, nT:128323, nP:3.9756, nB:0.8876
Cond:1961, Score:7401.583, sT:43159, sP:3.4497, sB:0.7628, nT:130864, nP:4.0127, nB:0.897
Cond:6693, Score:7401.55, sT:42586, sP:3.4374, sB:0.7823, nT:128257, nP:3.9787, nB:0.8894
Cond:2411, Score:7401.167, sT:42655, sP:3.4375, sB:0.7811, nT:128554, nP:3.9809, nB:0.888
Cond:3164, Score:7401.111, sT:44119, sP:3.433, sB:0.7424, nT:135116, nP:3.9887, nB:0.8699
Cond:7337, Score:7400.845, sT:42647, sP:3.4377, sB:0.7809, nT:128463, nP:3.979, nB:0.8886
Cond:3093, Score:7400.675, sT:42782, sP:3.4348, sB:0.7779, nT:129014, nP:3.9763, nB:0.8849
Cond:4677, Score:7400.427, sT:42600, sP:3.4366, sB:0.7815, nT:128312, nP:3.9781, nB:0.8892
Cond:3119, Score:7400.036, sT:42768, sP:3.4391, sB:0.7791, nT:128917, nP:3.9776, nB:0.8854
Cond:8144, Score:7399.958, sT:42611, sP:3.4358, sB:0.7819, nT:128341, nP:3.9756, nB:0.8874
Cond:1993, Score:7399.14, sT:42568, sP:3.4376, sB:0.7828, nT:128213, nP:3.9781, nB:0.8887
Cond:7545, Score:7398.785, sT:42595, sP:3.4378, sB:0.7817, nT:128303, nP:3.9787, nB:0.8891
Cond:3321, Score:7398.355, sT:42620, sP:3.4375, sB:0.7815, nT:128435, nP:3.9792, nB:0.8879 , T2:1973,4833,4887,1077,6903,3327,6469,7095,1515,1977,6695,7581,6697,3265,7672,7789,5303,8130,1961,6693,2411,3164,7337,3093,4677,3119,8144,1993,7545,3321,  #End#
LowScoreRank0 , T0:1395 , T1:
Cond:6903, Score:1382.803, sT:42958, sP:3.4952, sB:0.7982, nT:130530, nP:4.1007, nB:0.8722
Cond:4887, Score:1382.701, sT:42932, sP:3.4943, sB:0.7981, nT:130460, nP:4.0999, nB:0.8727
Cond:4833, Score:1382.603, sT:42950, sP:3.4922, sB:0.7969, nT:130451, nP:4.0975, nB:0.873
Cond:1973, Score:1382.252, sT:43015, sP:3.4925, sB:0.7952, nT:130727, nP:4.0978, nB:0.8725
Cond:6469, Score:1382.091, sT:42992, sP:3.4946, sB:0.7969, nT:130675, nP:4.1012, nB:0.8716
Cond:1077, Score:1381.922, sT:42965, sP:3.4931, sB:0.796, nT:130528, nP:4.0978, nB:0.873
Cond:1515, Score:1381.919, sT:42905, sP:3.4955, sB:0.7988, nT:130373, nP:4.1016, nB:0.8721
Cond:6695, Score:1381.593, sT:42919, sP:3.4937, sB:0.7973, nT:130345, nP:4.0997, nB:0.8729
Cond:3265, Score:1381.463, sT:42914, sP:3.4931, sB:0.7971, nT:130344, nP:4.0984, nB:0.8727
Cond:8130, Score:1381.262, sT:42922, sP:3.4925, sB:0.7971, nT:130386, nP:4.0973, nB:0.8716
Cond:7545, Score:1381.194, sT:42911, sP:3.495, sB:0.7971, nT:130368, nP:4.1004, nB:0.8732
Cond:7581, Score:1381.149, sT:43002, sP:3.4953, sB:0.7956, nT:130574, nP:4.1011, nB:0.8727
Cond:6693, Score:1381.142, sT:42904, sP:3.4943, sB:0.7971, nT:130320, nP:4.1004, nB:0.8733
Cond:6697, Score:1381.108, sT:42940, sP:3.4944, sB:0.7965, nT:130475, nP:4.1001, nB:0.8726
Cond:7789, Score:1381.086, sT:43052, sP:3.4996, sB:0.7965, nT:130423, nP:4.1009, nB:0.8721
Cond:4677, Score:1380.969, sT:42918, sP:3.4935, sB:0.7964, nT:130375, nP:4.0998, nB:0.8732
Cond:8144, Score:1380.943, sT:42929, sP:3.4927, sB:0.7968, nT:130404, nP:4.0973, nB:0.8714
Cond:1993, Score:1380.932, sT:42886, sP:3.4945, sB:0.7977, nT:130276, nP:4.0999, nB:0.8726
Cond:5303, Score:1380.929, sT:42940, sP:3.4937, sB:0.797, nT:130488, nP:4.0999, nB:0.8712
Cond:1977, Score:1380.911, sT:42977, sP:3.4944, sB:0.7932, nT:130688, nP:4.1012, nB:0.8759
Cond:3321, Score:1380.877, sT:42939, sP:3.4942, sB:0.7967, nT:130498, nP:4.1008, nB:0.8719
Cond:7337, Score:1380.723, sT:42965, sP:3.4946, sB:0.7957, nT:130528, nP:4.1005, nB:0.8726
Cond:621, Score:1380.718, sT:43052, sP:3.5007, sB:0.796, nT:130677, nP:4.1051, nB:0.8722
Cond:3160, Score:1380.618, sT:42985, sP:3.4942, sB:0.796, nT:130589, nP:4.1006, nB:0.8711
Cond:3327, Score:1380.55, sT:43435, sP:3.5008, sB:0.7907, nT:132095, nP:4.102, nB:0.8644
Cond:6467, Score:1380.481, sT:42942, sP:3.4952, sB:0.7966, nT:130486, nP:4.1016, nB:0.8719
Cond:3299, Score:1380.402, sT:42889, sP:3.4945, sB:0.7969, nT:130290, nP:4.0998, nB:0.8729
Cond:3150, Score:1380.362, sT:42922, sP:3.4934, sB:0.796, nT:130428, nP:4.0987, nB:0.8723
Cond:1457, Score:1380.317, sT:42893, sP:3.494, sB:0.7972, nT:130271, nP:4.0996, nB:0.8721
Cond:6885, Score:1380.279, sT:42955, sP:3.4949, sB:0.7967, nT:130546, nP:4.1019, nB:0.8708 , T2:6903,4887,4833,1973,6469,1077,1515,6695,3265,8130,7545,7581,6693,6697,7789,4677,8144,1993,5303,1977,3321,7337,621,3160,3327,6467,3299,3150,1457,6885,  #End#
LowScoreRank1 , T0:1395 , T1:
Cond:6903, Score:2411.903, sT:42958, sP:3.4952, sB:0.7982, nT:130530, nP:4.1007, nB:0.8722
Cond:4887, Score:2411.678, sT:42932, sP:3.4943, sB:0.7981, nT:130460, nP:4.0999, nB:0.8727
Cond:4833, Score:2411.581, sT:42950, sP:3.4922, sB:0.7969, nT:130451, nP:4.0975, nB:0.873
Cond:1973, Score:2411.248, sT:43015, sP:3.4925, sB:0.7952, nT:130727, nP:4.0978, nB:0.8725
Cond:6469, Score:2410.802, sT:42992, sP:3.4946, sB:0.7969, nT:130675, nP:4.1012, nB:0.8716
Cond:1077, Score:2410.495, sT:42965, sP:3.4931, sB:0.796, nT:130528, nP:4.0978, nB:0.873
Cond:1515, Score:2410.182, sT:42905, sP:3.4955, sB:0.7988, nT:130373, nP:4.1016, nB:0.8721
Cond:6695, Score:2409.702, sT:42919, sP:3.4937, sB:0.7973, nT:130345, nP:4.0997, nB:0.8729
Cond:3265, Score:2409.476, sT:42914, sP:3.4931, sB:0.7971, nT:130344, nP:4.0984, nB:0.8727
Cond:3327, Score:2409.418, sT:43435, sP:3.5008, sB:0.7907, nT:132095, nP:4.102, nB:0.8644
Cond:7581, Score:2409.211, sT:43002, sP:3.4953, sB:0.7956, nT:130574, nP:4.1011, nB:0.8727
Cond:8130, Score:2409.119, sT:42922, sP:3.4925, sB:0.7971, nT:130386, nP:4.0973, nB:0.8716
Cond:1977, Score:2409.045, sT:42977, sP:3.4944, sB:0.7932, nT:130688, nP:4.1012, nB:0.8759
Cond:7789, Score:2409.045, sT:43052, sP:3.4996, sB:0.7965, nT:130423, nP:4.1009, nB:0.8721
Cond:7545, Score:2409.035, sT:42911, sP:3.495, sB:0.7971, nT:130368, nP:4.1004, nB:0.8732
Cond:6697, Score:2408.977, sT:42940, sP:3.4944, sB:0.7965, nT:130475, nP:4.1001, nB:0.8726
Cond:6693, Score:2408.908, sT:42904, sP:3.4943, sB:0.7971, nT:130320, nP:4.1004, nB:0.8733
Cond:4677, Score:2408.676, sT:42918, sP:3.4935, sB:0.7964, nT:130375, nP:4.0998, nB:0.8732
Cond:5303, Score:2408.596, sT:42940, sP:3.4937, sB:0.797, nT:130488, nP:4.0999, nB:0.8712
Cond:8144, Score:2408.587, sT:42929, sP:3.4927, sB:0.7968, nT:130404, nP:4.0973, nB:0.8714
Cond:621, Score:2408.551, sT:43052, sP:3.5007, sB:0.796, nT:130677, nP:4.1051, nB:0.8722
Cond:3321, Score:2408.546, sT:42939, sP:3.4942, sB:0.7967, nT:130498, nP:4.1008, nB:0.8719
Cond:1993, Score:2408.451, sT:42886, sP:3.4945, sB:0.7977, nT:130276, nP:4.0999, nB:0.8726
Cond:7337, Score:2408.395, sT:42965, sP:3.4946, sB:0.7957, nT:130528, nP:4.1005, nB:0.8726
Cond:3160, Score:2408.196, sT:42985, sP:3.4942, sB:0.796, nT:130589, nP:4.1006, nB:0.8711
Cond:7095, Score:2407.874, sT:43201, sP:3.4969, sB:0.7925, nT:131553, nP:4.1044, nB:0.8677
Cond:6467, Score:2407.857, sT:42942, sP:3.4952, sB:0.7966, nT:130486, nP:4.1016, nB:0.8719
Cond:3150, Score:2407.642, sT:42922, sP:3.4934, sB:0.796, nT:130428, nP:4.0987, nB:0.8723
Cond:3299, Score:2407.583, sT:42889, sP:3.4945, sB:0.7969, nT:130290, nP:4.0998, nB:0.8729
Cond:6885, Score:2407.504, sT:42955, sP:3.4949, sB:0.7967, nT:130546, nP:4.1019, nB:0.8708 , T2:6903,4887,4833,1973,6469,1077,1515,6695,3265,3327,7581,8130,1977,7789,7545,6697,6693,4677,5303,8144,621,3321,1993,7337,3160,7095,6467,3150,3299,6885,  #End#
LowScoreRank2 , T0:1395 , T1:
Cond:6903, Score:4210.034, sT:42958, sP:3.4952, sB:0.7982, nT:130530, nP:4.1007, nB:0.8722
Cond:4887, Score:4209.562, sT:42932, sP:3.4943, sB:0.7981, nT:130460, nP:4.0999, nB:0.8727
Cond:4833, Score:4209.52, sT:42950, sP:3.4922, sB:0.7969, nT:130451, nP:4.0975, nB:0.873
Cond:1973, Score:4209.429, sT:43015, sP:3.4925, sB:0.7952, nT:130727, nP:4.0978, nB:0.8725
Cond:6469, Score:4208.362, sT:42992, sP:3.4946, sB:0.7969, nT:130675, nP:4.1012, nB:0.8716
Cond:3327, Score:4208.226, sT:43435, sP:3.5008, sB:0.7907, nT:132095, nP:4.102, nB:0.8644
Cond:1077, Score:4207.804, sT:42965, sP:3.4931, sB:0.796, nT:130528, nP:4.0978, nB:0.873
Cond:1515, Score:4206.719, sT:42905, sP:3.4955, sB:0.7988, nT:130373, nP:4.1016, nB:0.8721
Cond:6695, Score:4206.034, sT:42919, sP:3.4937, sB:0.7973, nT:130345, nP:4.0997, nB:0.8729
Cond:1977, Score:4205.828, sT:42977, sP:3.4944, sB:0.7932, nT:130688, nP:4.1012, nB:0.8759
Cond:7581, Score:4205.673, sT:43002, sP:3.4953, sB:0.7956, nT:130574, nP:4.1011, nB:0.8727
Cond:3265, Score:4205.638, sT:42914, sP:3.4931, sB:0.7971, nT:130344, nP:4.0984, nB:0.8727
Cond:7789, Score:4205.269, sT:43052, sP:3.4996, sB:0.7965, nT:130423, nP:4.1009, nB:0.8721
Cond:8130, Score:4205.007, sT:42922, sP:3.4925, sB:0.7971, nT:130386, nP:4.0973, nB:0.8716
Cond:6697, Score:4204.983, sT:42940, sP:3.4944, sB:0.7965, nT:130475, nP:4.1001, nB:0.8726
Cond:7545, Score:4204.923, sT:42911, sP:3.495, sB:0.7971, nT:130368, nP:4.1004, nB:0.8732
Cond:621, Score:4204.677, sT:43052, sP:3.5007, sB:0.796, nT:130677, nP:4.1051, nB:0.8722
Cond:6693, Score:4204.637, sT:42904, sP:3.4943, sB:0.7971, nT:130320, nP:4.1004, nB:0.8733
Cond:7095, Score:4204.575, sT:43201, sP:3.4969, sB:0.7925, nT:131553, nP:4.1044, nB:0.8677
Cond:4677, Score:4204.352, sT:42918, sP:3.4935, sB:0.7964, nT:130375, nP:4.0998, nB:0.8732
Cond:5303, Score:4204.197, sT:42940, sP:3.4937, sB:0.797, nT:130488, nP:4.0999, nB:0.8712
Cond:3321, Score:4204.182, sT:42939, sP:3.4942, sB:0.7967, nT:130498, nP:4.1008, nB:0.8719
Cond:7337, Score:4204.12, sT:42965, sP:3.4946, sB:0.7957, nT:130528, nP:4.1005, nB:0.8726
Cond:8144, Score:4204.12, sT:42929, sP:3.4927, sB:0.7968, nT:130404, nP:4.0973, nB:0.8714
Cond:3160, Score:4203.744, sT:42985, sP:3.4942, sB:0.796, nT:130589, nP:4.1006, nB:0.8711
Cond:1993, Score:4203.683, sT:42886, sP:3.4945, sB:0.7977, nT:130276, nP:4.0999, nB:0.8726
Cond:6467, Score:4202.983, sT:42942, sP:3.4952, sB:0.7966, nT:130486, nP:4.1016, nB:0.8719
Cond:3150, Score:4202.593, sT:42922, sP:3.4934, sB:0.796, nT:130428, nP:4.0987, nB:0.8723
Cond:3093, Score:4202.484, sT:43098, sP:3.4915, sB:0.7924, nT:131077, nP:4.0974, nB:0.869
Cond:3119, Score:4202.484, sT:43087, sP:3.4959, sB:0.7937, nT:130986, nP:4.0989, nB:0.8694 , T2:6903,4887,4833,1973,6469,3327,1077,1515,6695,1977,7581,3265,7789,8130,6697,7545,621,6693,7095,4677,5303,3321,7337,8144,3160,1993,6467,3150,3093,3119,  #End#
LowScoreRank3 , T0:1395 , T1:
Cond:3327, Score:7355.562, sT:43435, sP:3.5008, sB:0.7907, nT:132095, nP:4.102, nB:0.8644
Cond:6903, Score:7354.287, sT:42958, sP:3.4952, sB:0.7982, nT:130530, nP:4.1007, nB:0.8722
Cond:1973, Score:7354.174, sT:43015, sP:3.4925, sB:0.7952, nT:130727, nP:4.0978, nB:0.8725
Cond:3164, Score:7353.5, sT:44443, sP:3.4873, sB:0.7578, nT:137213, nP:4.1061, nB:0.8546
Cond:4833, Score:7353.469, sT:42950, sP:3.4922, sB:0.7969, nT:130451, nP:4.0975, nB:0.873
Cond:4887, Score:7353.322, sT:42932, sP:3.4943, sB:0.7981, nT:130460, nP:4.0999, nB:0.8727
Cond:6469, Score:7351.805, sT:42992, sP:3.4946, sB:0.7969, nT:130675, nP:4.1012, nB:0.8716
Cond:1077, Score:7350.789, sT:42965, sP:3.4931, sB:0.796, nT:130528, nP:4.0978, nB:0.873
Cond:1977, Score:7348.321, sT:42977, sP:3.4944, sB:0.7932, nT:130688, nP:4.1012, nB:0.8759
Cond:1961, Score:7348.131, sT:43477, sP:3.5057, sB:0.7779, nT:132949, nP:4.1316, nB:0.8807
Cond:1515, Score:7347.95, sT:42905, sP:3.4955, sB:0.7988, nT:130373, nP:4.1016, nB:0.8721
Cond:7095, Score:7347.524, sT:43201, sP:3.4969, sB:0.7925, nT:131553, nP:4.1044, nB:0.8677
Cond:7581, Score:7347.253, sT:43002, sP:3.4953, sB:0.7956, nT:130574, nP:4.1011, nB:0.8727
Cond:6695, Score:7347.018, sT:42919, sP:3.4937, sB:0.7973, nT:130345, nP:4.0997, nB:0.8729
Cond:3265, Score:7346.327, sT:42914, sP:3.4931, sB:0.7971, nT:130344, nP:4.0984, nB:0.8727
Cond:7789, Score:7346.323, sT:43052, sP:3.4996, sB:0.7965, nT:130423, nP:4.1009, nB:0.8721
Cond:621, Score:7345.781, sT:43052, sP:3.5007, sB:0.796, nT:130677, nP:4.1051, nB:0.8722
Cond:6697, Score:7345.563, sT:42940, sP:3.4944, sB:0.7965, nT:130475, nP:4.1001, nB:0.8726
Cond:8130, Score:7345.21, sT:42922, sP:3.4925, sB:0.7971, nT:130386, nP:4.0973, nB:0.8716
Cond:7545, Score:7345.175, sT:42911, sP:3.495, sB:0.7971, nT:130368, nP:4.1004, nB:0.8732
Cond:6693, Score:7344.559, sT:42904, sP:3.4943, sB:0.7971, nT:130320, nP:4.1004, nB:0.8733
Cond:7337, Score:7344.322, sT:42965, sP:3.4946, sB:0.7957, nT:130528, nP:4.1005, nB:0.8726
Cond:4677, Score:7344.276, sT:42918, sP:3.4935, sB:0.7964, nT:130375, nP:4.0998, nB:0.8732
Cond:3321, Score:7344.079, sT:42939, sP:3.4942, sB:0.7967, nT:130498, nP:4.1008, nB:0.8719
Cond:5303, Score:7343.978, sT:42940, sP:3.4937, sB:0.797, nT:130488, nP:4.0999, nB:0.8712
Cond:8144, Score:7343.733, sT:42929, sP:3.4927, sB:0.7968, nT:130404, nP:4.0973, nB:0.8714
Cond:3160, Score:7343.614, sT:42985, sP:3.4942, sB:0.796, nT:130589, nP:4.1006, nB:0.8711
Cond:7321, Score:7343.614, sT:43459, sP:3.5, sB:0.788, nT:132556, nP:4.1101, nB:0.863
Cond:3093, Score:7342.87, sT:43098, sP:3.4915, sB:0.7924, nT:131077, nP:4.0974, nB:0.869
Cond:3119, Score:7342.623, sT:43087, sP:3.4959, sB:0.7937, nT:130986, nP:4.0989, nB:0.8694 , T2:3327,6903,1973,3164,4833,4887,6469,1077,1977,1961,1515,7095,7581,6695,3265,7789,621,6697,8130,7545,6693,7337,4677,3321,5303,8144,3160,7321,3093,3119,  #End#
LowScoreRank0 , T0:65 , T1:
Cond:1077, Score:1384.019, sT:42608, sP:3.4961, sB:0.8161, nT:129054, nP:4.1229, nB:0.8652
Cond:1973, Score:1383.992, sT:42657, sP:3.4956, sB:0.815, nT:129253, nP:4.1227, nB:0.8646
Cond:4833, Score:1383.343, sT:42592, sP:3.4953, sB:0.8157, nT:128978, nP:4.1225, nB:0.8652
Cond:4887, Score:1383.222, sT:42576, sP:3.4973, sB:0.8167, nT:128990, nP:4.1249, nB:0.8648
Cond:6469, Score:1383.193, sT:42637, sP:3.4975, sB:0.816, nT:129205, nP:4.1262, nB:0.8637
Cond:6903, Score:1383.042, sT:42603, sP:3.4982, sB:0.8165, nT:129060, nP:4.1257, nB:0.8643
Cond:3265, Score:1382.191, sT:42556, sP:3.4962, sB:0.8159, nT:128873, nP:4.1235, nB:0.8649
Cond:1515, Score:1382.153, sT:42550, sP:3.4984, sB:0.8171, nT:128903, nP:4.1267, nB:0.8642
Cond:6695, Score:1382.118, sT:42563, sP:3.4967, sB:0.8159, nT:128875, nP:4.1247, nB:0.865
Cond:8130, Score:1381.834, sT:42566, sP:3.4955, sB:0.8157, nT:128912, nP:4.1224, nB:0.8638
Cond:1977, Score:1381.797, sT:42621, sP:3.4975, sB:0.8121, nT:129217, nP:4.1262, nB:0.8681
Cond:8144, Score:1381.707, sT:42573, sP:3.4958, sB:0.8156, nT:128931, nP:4.1224, nB:0.8636
Cond:6693, Score:1381.693, sT:42548, sP:3.4974, sB:0.8157, nT:128850, nP:4.1255, nB:0.8655
Cond:7581, Score:1381.519, sT:42647, sP:3.4983, sB:0.814, nT:129104, nP:4.1261, nB:0.8648
Cond:4677, Score:1381.477, sT:42562, sP:3.4966, sB:0.815, nT:128905, nP:4.1248, nB:0.8653
Cond:5303, Score:1381.475, sT:42584, sP:3.4967, sB:0.8156, nT:129018, nP:4.1249, nB:0.8633
Cond:1993, Score:1381.474, sT:42530, sP:3.4976, sB:0.8163, nT:128805, nP:4.125, nB:0.8648
Cond:7789, Score:1381.453, sT:42697, sP:3.5026, sB:0.8149, nT:128953, nP:4.126, nB:0.8642
Cond:3321, Score:1381.399, sT:42583, sP:3.4973, sB:0.8153, nT:129028, nP:4.1258, nB:0.864
Cond:7337, Score:1381.268, sT:42610, sP:3.4975, sB:0.8142, nT:129058, nP:4.1255, nB:0.8648
Cond:621, Score:1381.25, sT:42695, sP:3.5038, sB:0.8146, nT:129205, nP:4.1301, nB:0.8643
Cond:3160, Score:1381.248, sT:42629, sP:3.4972, sB:0.8146, nT:129119, nP:4.1256, nB:0.8633
Cond:6697, Score:1381.222, sT:42584, sP:3.4974, sB:0.8147, nT:129005, nP:4.1252, nB:0.8647
Cond:2276, Score:1381.204, sT:42579, sP:3.4967, sB:0.8153, nT:128984, nP:4.1252, nB:0.8637
Cond:3093, Score:1381.202, sT:42741, sP:3.4945, sB:0.8119, nT:129603, nP:4.1223, nB:0.8611
Cond:6467, Score:1381.194, sT:42586, sP:3.4983, sB:0.8154, nT:129015, nP:4.1267, nB:0.864
Cond:7545, Score:1381.169, sT:42557, sP:3.4978, sB:0.8151, nT:128898, nP:4.1254, nB:0.8653
Cond:3150, Score:1380.898, sT:42566, sP:3.4964, sB:0.8146, nT:128958, nP:4.1237, nB:0.8644
Cond:3299, Score:1380.887, sT:42533, sP:3.4976, sB:0.8155, nT:128820, nP:4.1249, nB:0.865
Cond:1457, Score:1380.843, sT:42537, sP:3.497, sB:0.8158, nT:128801, nP:4.1246, nB:0.8642 , T2:1077,1973,4833,4887,6469,6903,3265,1515,6695,8130,1977,8144,6693,7581,4677,5303,1993,7789,3321,7337,621,3160,6697,2276,3093,6467,7545,3150,3299,1457,  #End#
LowScoreRank1 , T0:65 , T1:
Cond:1973, Score:2411.811, sT:42657, sP:3.4956, sB:0.815, nT:129253, nP:4.1227, nB:0.8646
Cond:1077, Score:2411.673, sT:42608, sP:3.4961, sB:0.8161, nT:129054, nP:4.1229, nB:0.8652
Cond:4833, Score:2410.445, sT:42592, sP:3.4953, sB:0.8157, nT:128978, nP:4.1225, nB:0.8652
Cond:6469, Score:2410.287, sT:42637, sP:3.4975, sB:0.816, nT:129205, nP:4.1262, nB:0.8637
Cond:4887, Score:2410.168, sT:42576, sP:3.4973, sB:0.8167, nT:128990, nP:4.1249, nB:0.8648
Cond:6903, Score:2409.915, sT:42603, sP:3.4982, sB:0.8165, nT:129060, nP:4.1257, nB:0.8643
Cond:3265, Score:2408.319, sT:42556, sP:3.4962, sB:0.8159, nT:128873, nP:4.1235, nB:0.8649
Cond:6695, Score:2408.201, sT:42563, sP:3.4967, sB:0.8159, nT:128875, nP:4.1247, nB:0.865
Cond:1515, Score:2408.185, sT:42550, sP:3.4984, sB:0.8171, nT:128903, nP:4.1267, nB:0.8642
Cond:1977, Score:2408.16, sT:42621, sP:3.4975, sB:0.8121, nT:129217, nP:4.1262, nB:0.8681
Cond:8130, Score:2407.7, sT:42566, sP:3.4955, sB:0.8157, nT:128912, nP:4.1224, nB:0.8638
Cond:8144, Score:2407.497, sT:42573, sP:3.4958, sB:0.8156, nT:128931, nP:4.1224, nB:0.8636
Cond:6693, Score:2407.455, sT:42548, sP:3.4974, sB:0.8157, nT:128850, nP:4.1255, nB:0.8655
Cond:7581, Score:2407.45, sT:42647, sP:3.4983, sB:0.814, nT:129104, nP:4.1261, nB:0.8648
Cond:7789, Score:2407.281, sT:42697, sP:3.5026, sB:0.8149, nT:128953, nP:4.126, nB:0.8642
Cond:3093, Score:2407.227, sT:42741, sP:3.4945, sB:0.8119, nT:129603, nP:4.1223, nB:0.8611
Cond:4677, Score:2407.145, sT:42562, sP:3.4966, sB:0.815, nT:128905, nP:4.1248, nB:0.8653
Cond:5303, Score:2407.131, sT:42584, sP:3.4967, sB:0.8156, nT:129018, nP:4.1249, nB:0.8633
Cond:621, Score:2407.063, sT:42695, sP:3.5038, sB:0.8146, nT:129205, nP:4.1301, nB:0.8643
Cond:3321, Score:2407.041, sT:42583, sP:3.4973, sB:0.8153, nT:129028, nP:4.1258, nB:0.864
Cond:1993, Score:2406.984, sT:42530, sP:3.4976, sB:0.8163, nT:128805, nP:4.125, nB:0.8648
Cond:7337, Score:2406.936, sT:42610, sP:3.4975, sB:0.8142, nT:129058, nP:4.1255, nB:0.8648
Cond:3160, Score:2406.882, sT:42629, sP:3.4972, sB:0.8146, nT:129119, nP:4.1256, nB:0.8633
Cond:6697, Score:2406.775, sT:42584, sP:3.4974, sB:0.8147, nT:129005, nP:4.1252, nB:0.8647
Cond:6467, Score:2406.677, sT:42586, sP:3.4983, sB:0.8154, nT:129015, nP:4.1267, nB:0.864
Cond:2276, Score:2406.659, sT:42579, sP:3.4967, sB:0.8153, nT:128984, nP:4.1252, nB:0.8637
Cond:7545, Score:2406.6, sT:42557, sP:3.4978, sB:0.8151, nT:128898, nP:4.1254, nB:0.8653
Cond:3327, Score:2406.569, sT:43087, sP:3.5035, sB:0.8081, nT:130622, nP:4.1267, nB:0.8565
Cond:3150, Score:2406.159, sT:42566, sP:3.4964, sB:0.8146, nT:128958, nP:4.1237, nB:0.8644
Cond:4679, Score:2406.05, sT:42578, sP:3.4974, sB:0.8142, nT:128999, nP:4.1252, nB:0.865 , T2:1973,1077,4833,6469,4887,6903,3265,6695,1515,1977,8130,8144,6693,7581,7789,3093,4677,5303,621,3321,1993,7337,3160,6697,6467,2276,7545,3327,3150,4679,  #End#
LowScoreRank2 , T0:65 , T1:
Cond:1973, Score:4206.062, sT:42657, sP:3.4956, sB:0.815, nT:129253, nP:4.1227, nB:0.8646
Cond:1077, Score:4205.494, sT:42608, sP:3.4961, sB:0.8161, nT:129054, nP:4.1229, nB:0.8652
Cond:4833, Score:4203.267, sT:42592, sP:3.4953, sB:0.8157, nT:128978, nP:4.1225, nB:0.8652
Cond:6469, Score:4203.174, sT:42637, sP:3.4975, sB:0.816, nT:129205, nP:4.1262, nB:0.8637
Cond:4887, Score:4202.671, sT:42576, sP:3.4973, sB:0.8167, nT:128990, nP:4.1249, nB:0.8648
Cond:6903, Score:4202.335, sT:42603, sP:3.4982, sB:0.8165, nT:129060, nP:4.1257, nB:0.8643
Cond:1977, Score:4200.008, sT:42621, sP:3.4975, sB:0.8121, nT:129217, nP:4.1262, nB:0.8681
Cond:3265, Score:4199.353, sT:42556, sP:3.4962, sB:0.8159, nT:128873, nP:4.1235, nB:0.8649
Cond:6695, Score:4199.161, sT:42563, sP:3.4967, sB:0.8159, nT:128875, nP:4.1247, nB:0.865
Cond:3327, Score:4199.102, sT:43087, sP:3.5035, sB:0.8081, nT:130622, nP:4.1267, nB:0.8565
Cond:1515, Score:4199.002, sT:42550, sP:3.4984, sB:0.8171, nT:128903, nP:4.1267, nB:0.8642
Cond:3093, Score:4198.555, sT:42741, sP:3.4945, sB:0.8119, nT:129603, nP:4.1223, nB:0.8611
Cond:7581, Score:4198.361, sT:42647, sP:3.4983, sB:0.814, nT:129104, nP:4.1261, nB:0.8648
Cond:8130, Score:4198.278, sT:42566, sP:3.4955, sB:0.8157, nT:128912, nP:4.1224, nB:0.8638
Cond:7789, Score:4197.957, sT:42697, sP:3.5026, sB:0.8149, nT:128953, nP:4.126, nB:0.8642
Cond:8144, Score:4197.954, sT:42573, sP:3.4958, sB:0.8156, nT:128931, nP:4.1224, nB:0.8636
Cond:6693, Score:4197.854, sT:42548, sP:3.4974, sB:0.8157, nT:128850, nP:4.1255, nB:0.8655
Cond:621, Score:4197.83, sT:42695, sP:3.5038, sB:0.8146, nT:129205, nP:4.1301, nB:0.8643
Cond:4677, Score:4197.429, sT:42562, sP:3.4966, sB:0.815, nT:128905, nP:4.1248, nB:0.8653
Cond:5303, Score:4197.389, sT:42584, sP:3.4967, sB:0.8156, nT:129018, nP:4.1249, nB:0.8633
Cond:7337, Score:4197.335, sT:42610, sP:3.4975, sB:0.8142, nT:129058, nP:4.1255, nB:0.8648
Cond:3321, Score:4197.304, sT:42583, sP:3.4973, sB:0.8153, nT:129028, nP:4.1258, nB:0.864
Cond:3160, Score:4197.207, sT:42629, sP:3.4972, sB:0.8146, nT:129119, nP:4.1256, nB:0.8633
Cond:6697, Score:4196.914, sT:42584, sP:3.4974, sB:0.8147, nT:129005, nP:4.1252, nB:0.8647
Cond:1993, Score:4196.874, sT:42530, sP:3.4976, sB:0.8163, nT:128805, nP:4.125, nB:0.8648
Cond:7095, Score:4196.703, sT:42848, sP:3.4995, sB:0.8104, nT:130082, nP:4.1292, nB:0.8599
Cond:6467, Score:4196.656, sT:42586, sP:3.4983, sB:0.8154, nT:129015, nP:4.1267, nB:0.864
Cond:2276, Score:4196.564, sT:42579, sP:3.4967, sB:0.8153, nT:128984, nP:4.1252, nB:0.8637
Cond:7545, Score:4196.464, sT:42557, sP:3.4978, sB:0.8151, nT:128898, nP:4.1254, nB:0.8653
Cond:3164, Score:4195.826, sT:44082, sP:3.4906, sB:0.7768, nT:135732, nP:4.1302, nB:0.8471 , T2:1973,1077,4833,6469,4887,6903,1977,3265,6695,3327,1515,3093,7581,8130,7789,8144,6693,621,4677,5303,7337,3321,3160,6697,1993,7095,6467,2276,7545,3164,  #End#
LowScoreRank3 , T0:65 , T1:
Cond:3164, Score:7341.279, sT:44082, sP:3.4906, sB:0.7768, nT:135732, nP:4.1302, nB:0.8471
Cond:1973, Score:7340.638, sT:42657, sP:3.4956, sB:0.815, nT:129253, nP:4.1227, nB:0.8646
Cond:1077, Score:7339.072, sT:42608, sP:3.4961, sB:0.8161, nT:129054, nP:4.1229, nB:0.8652
Cond:6469, Score:7335.195, sT:42637, sP:3.4975, sB:0.816, nT:129205, nP:4.1262, nB:0.8637
Cond:4833, Score:7335.034, sT:42592, sP:3.4953, sB:0.8157, nT:128978, nP:4.1225, nB:0.8652
Cond:4887, Score:7333.798, sT:42576, sP:3.4973, sB:0.8167, nT:128990, nP:4.1249, nB:0.8648
Cond:6903, Score:7333.393, sT:42603, sP:3.4982, sB:0.8165, nT:129060, nP:4.1257, nB:0.8643
Cond:472, Score:7333.114, sT:44451, sP:3.4855, sB:0.7682, nT:135728, nP:4.1063, nB:0.8431
Cond:3327, Score:7332.305, sT:43087, sP:3.5035, sB:0.8081, nT:130622, nP:4.1267, nB:0.8565
Cond:1977, Score:7330.63, sT:42621, sP:3.4975, sB:0.8121, nT:129217, nP:4.1262, nB:0.8681
Cond:3444, Score:7330.243, sT:43556, sP:3.4658, sB:0.7795, nT:131614, nP:4.0861, nB:0.861
Cond:7672, Score:7329.144, sT:43317, sP:3.4814, sB:0.7968, nT:131829, nP:4.1102, nB:0.8498
Cond:1961, Score:7328.997, sT:43120, sP:3.509, sB:0.7962, nT:131476, nP:4.1566, nB:0.8731
Cond:3093, Score:7328.396, sT:42741, sP:3.4945, sB:0.8119, nT:129603, nP:4.1223, nB:0.8611
Cond:3265, Score:7327.843, sT:42556, sP:3.4962, sB:0.8159, nT:128873, nP:4.1235, nB:0.8649
Cond:6695, Score:7327.529, sT:42563, sP:3.4967, sB:0.8159, nT:128875, nP:4.1247, nB:0.865
Cond:1515, Score:7327.026, sT:42550, sP:3.4984, sB:0.8171, nT:128903, nP:4.1267, nB:0.8642
Cond:7581, Score:7327.025, sT:42647, sP:3.4983, sB:0.814, nT:129104, nP:4.1261, nB:0.8648
Cond:7095, Score:7326.387, sT:42848, sP:3.4995, sB:0.8104, nT:130082, nP:4.1292, nB:0.8599
Cond:621, Score:7326.341, sT:42695, sP:3.5038, sB:0.8146, nT:129205, nP:4.1301, nB:0.8643
Cond:7789, Score:7326.103, sT:42697, sP:3.5026, sB:0.8149, nT:128953, nP:4.126, nB:0.8642
Cond:8130, Score:7325.974, sT:42566, sP:3.4955, sB:0.8157, nT:128912, nP:4.1224, nB:0.8638
Cond:7450, Score:7325.715, sT:43809, sP:3.4988, sB:0.7882, nT:134750, nP:4.1367, nB:0.8424
Cond:8144, Score:7325.461, sT:42573, sP:3.4958, sB:0.8156, nT:128931, nP:4.1224, nB:0.8636
Cond:6693, Score:7325.238, sT:42548, sP:3.4974, sB:0.8157, nT:128850, nP:4.1255, nB:0.8655
Cond:1061, Score:7325.041, sT:43137, sP:3.4868, sB:0.8011, nT:130836, nP:4.1168, nB:0.8554
Cond:7337, Score:7325.01, sT:42610, sP:3.4975, sB:0.8142, nT:129058, nP:4.1255, nB:0.8648
Cond:3160, Score:7324.726, sT:42629, sP:3.4972, sB:0.8146, nT:129119, nP:4.1256, nB:0.8633
Cond:4677, Score:7324.7, sT:42562, sP:3.4966, sB:0.815, nT:128905, nP:4.1248, nB:0.8653
Cond:5303, Score:7324.603, sT:42584, sP:3.4967, sB:0.8156, nT:129018, nP:4.1249, nB:0.8633 , T2:3164,1973,1077,6469,4833,4887,6903,472,3327,1977,3444,7672,1961,3093,3265,6695,1515,7581,7095,621,7789,8130,7450,8144,6693,1061,7337,3160,4677,5303,  #End#
LowScoreRank0 , T0:275 , T1:
Cond:4887, Score:1373.76, sT:43268, sP:3.4843, sB:0.7822, nT:131802, nP:4.1017, nB:0.8675
Cond:6903, Score:1373.749, sT:43294, sP:3.4852, sB:0.7822, nT:131872, nP:4.1025, nB:0.867
Cond:1973, Score:1373.498, sT:43351, sP:3.4825, sB:0.7796, nT:132069, nP:4.0996, nB:0.8672
Cond:4833, Score:1373.402, sT:43285, sP:3.4823, sB:0.7808, nT:131793, nP:4.0993, nB:0.8678
Cond:1077, Score:1373.25, sT:43301, sP:3.4831, sB:0.7804, nT:131870, nP:4.0997, nB:0.8678
Cond:6469, Score:1373.046, sT:43328, sP:3.4846, sB:0.781, nT:132017, nP:4.103, nB:0.8663
Cond:1515, Score:1372.824, sT:43241, sP:3.4855, sB:0.7828, nT:131715, nP:4.1034, nB:0.8668
Cond:6695, Score:1372.587, sT:43255, sP:3.4837, sB:0.7814, nT:131687, nP:4.1015, nB:0.8676
Cond:8130, Score:1372.296, sT:43258, sP:3.4825, sB:0.7812, nT:131728, nP:4.0992, nB:0.8664
Cond:6693, Score:1372.207, sT:43240, sP:3.4843, sB:0.7812, nT:131662, nP:4.1022, nB:0.8681
Cond:3265, Score:1372.174, sT:43249, sP:3.4832, sB:0.7809, nT:131686, nP:4.1002, nB:0.8675
Cond:7789, Score:1372.105, sT:43388, sP:3.4896, sB:0.7806, nT:131765, nP:4.1027, nB:0.8669
Cond:7581, Score:1372.1, sT:43338, sP:3.4853, sB:0.7797, nT:131916, nP:4.1029, nB:0.8674
Cond:6697, Score:1371.981, sT:43276, sP:3.4844, sB:0.7805, nT:131817, nP:4.1019, nB:0.8673
Cond:5303, Score:1371.967, sT:43276, sP:3.4837, sB:0.7811, nT:131830, nP:4.1017, nB:0.866
Cond:4677, Score:1371.955, sT:43254, sP:3.4835, sB:0.7805, nT:131717, nP:4.1016, nB:0.8679
Cond:1993, Score:1371.901, sT:43222, sP:3.4845, sB:0.7817, nT:131618, nP:4.1017, nB:0.8674
Cond:8144, Score:1371.868, sT:43265, sP:3.4827, sB:0.7808, nT:131746, nP:4.0992, nB:0.8662
Cond:1977, Score:1371.859, sT:43313, sP:3.4844, sB:0.7773, nT:132030, nP:4.103, nB:0.8706
Cond:621, Score:1371.759, sT:43388, sP:3.4906, sB:0.7801, nT:132019, nP:4.1068, nB:0.867
Cond:7337, Score:1371.75, sT:43301, sP:3.4846, sB:0.7798, nT:131870, nP:4.1023, nB:0.8674
Cond:3321, Score:1371.748, sT:43275, sP:3.4842, sB:0.7807, nT:131840, nP:4.1026, nB:0.8666
Cond:7545, Score:1371.655, sT:43249, sP:3.4847, sB:0.7806, nT:131710, nP:4.1022, nB:0.8679
Cond:3160, Score:1371.632, sT:43321, sP:3.4842, sB:0.7801, nT:131931, nP:4.1024, nB:0.8659
Cond:6467, Score:1371.456, sT:43278, sP:3.4852, sB:0.7807, nT:131828, nP:4.1034, nB:0.8666
Cond:3299, Score:1371.403, sT:43225, sP:3.4845, sB:0.781, nT:131632, nP:4.1016, nB:0.8676
Cond:3327, Score:1371.382, sT:43770, sP:3.4908, sB:0.7748, nT:133437, nP:4.1038, nB:0.8593
Cond:3150, Score:1371.338, sT:43258, sP:3.4834, sB:0.7801, nT:131770, nP:4.1005, nB:0.867
Cond:1457, Score:1371.277, sT:43229, sP:3.484, sB:0.7812, nT:131613, nP:4.1014, nB:0.8669
Cond:4679, Score:1371.167, sT:43270, sP:3.4844, sB:0.7797, nT:131812, nP:4.102, nB:0.8675 , T2:4887,6903,1973,4833,1077,6469,1515,6695,8130,6693,3265,7789,7581,6697,5303,4677,1993,8144,1977,621,7337,3321,7545,3160,6467,3299,3327,3150,1457,4679,  #End#
LowScoreRank1 , T0:275 , T1:
Cond:6903, Score:2397.595, sT:43294, sP:3.4852, sB:0.7822, nT:131872, nP:4.1025, nB:0.867
Cond:4887, Score:2397.565, sT:43268, sP:3.4843, sB:0.7822, nT:131802, nP:4.1017, nB:0.8675
Cond:1973, Score:2397.444, sT:43351, sP:3.4825, sB:0.7796, nT:132069, nP:4.0996, nB:0.8672
Cond:4833, Score:2397.024, sT:43285, sP:3.4823, sB:0.7808, nT:131793, nP:4.0993, nB:0.8678
Cond:1077, Score:2396.839, sT:43301, sP:3.4831, sB:0.7804, nT:131870, nP:4.0997, nB:0.8678
Cond:6469, Score:2396.503, sT:43328, sP:3.4846, sB:0.781, nT:132017, nP:4.103, nB:0.8663
Cond:1515, Score:2395.801, sT:43241, sP:3.4855, sB:0.7828, nT:131715, nP:4.1034, nB:0.8668
Cond:6695, Score:2395.472, sT:43255, sP:3.4837, sB:0.7814, nT:131687, nP:4.1015, nB:0.8676
Cond:8130, Score:2394.963, sT:43258, sP:3.4825, sB:0.7812, nT:131728, nP:4.0992, nB:0.8664
Cond:7581, Score:2394.905, sT:43338, sP:3.4853, sB:0.7797, nT:131916, nP:4.1029, nB:0.8674
Cond:3327, Score:2394.893, sT:43770, sP:3.4908, sB:0.7748, nT:133437, nP:4.1038, nB:0.8593
Cond:7789, Score:2394.86, sT:43388, sP:3.4896, sB:0.7806, nT:131765, nP:4.1027, nB:0.8669
Cond:6693, Score:2394.806, sT:43240, sP:3.4843, sB:0.7812, nT:131662, nP:4.1022, nB:0.8681
Cond:3265, Score:2394.768, sT:43249, sP:3.4832, sB:0.7809, nT:131686, nP:4.1002, nB:0.8675
Cond:1977, Score:2394.734, sT:43313, sP:3.4844, sB:0.7773, nT:132030, nP:4.103, nB:0.8706
Cond:6697, Score:2394.54, sT:43276, sP:3.4844, sB:0.7805, nT:131817, nP:4.1019, nB:0.8673
Cond:5303, Score:2394.445, sT:43276, sP:3.4837, sB:0.7811, nT:131830, nP:4.1017, nB:0.866
Cond:4677, Score:2394.433, sT:43254, sP:3.4835, sB:0.7805, nT:131717, nP:4.1016, nB:0.8679
Cond:621, Score:2394.403, sT:43388, sP:3.4906, sB:0.7801, nT:132019, nP:4.1068, nB:0.867
Cond:8144, Score:2394.243, sT:43265, sP:3.4827, sB:0.7808, nT:131746, nP:4.0992, nB:0.8662
Cond:7337, Score:2394.224, sT:43301, sP:3.4846, sB:0.7798, nT:131870, nP:4.1023, nB:0.8674
Cond:1993, Score:2394.187, sT:43222, sP:3.4845, sB:0.7817, nT:131618, nP:4.1017, nB:0.8674
Cond:3321, Score:2394.104, sT:43275, sP:3.4842, sB:0.7807, nT:131840, nP:4.1026, nB:0.8666
Cond:3160, Score:2394.002, sT:43321, sP:3.4842, sB:0.7801, nT:131931, nP:4.1024, nB:0.8659
Cond:7545, Score:2393.901, sT:43249, sP:3.4847, sB:0.7806, nT:131710, nP:4.1022, nB:0.8679
Cond:6467, Score:2393.593, sT:43278, sP:3.4852, sB:0.7807, nT:131828, nP:4.1034, nB:0.8666
Cond:3150, Score:2393.381, sT:43258, sP:3.4834, sB:0.7801, nT:131770, nP:4.1005, nB:0.867
Cond:3299, Score:2393.367, sT:43225, sP:3.4845, sB:0.781, nT:131632, nP:4.1016, nB:0.8676
Cond:3093, Score:2393.177, sT:43434, sP:3.4815, sB:0.7768, nT:132419, nP:4.0992, nB:0.8638
Cond:4679, Score:2393.15, sT:43270, sP:3.4844, sB:0.7797, nT:131812, nP:4.102, nB:0.8675 , T2:6903,4887,1973,4833,1077,6469,1515,6695,8130,7581,3327,7789,6693,3265,1977,6697,5303,4677,621,8144,7337,1993,3321,3160,7545,6467,3150,3299,3093,4679,  #End#
LowScoreRank2 , T0:275 , T1:
Cond:1973, Score:4187.913, sT:43351, sP:3.4825, sB:0.7796, nT:132069, nP:4.0996, nB:0.8672
Cond:6903, Score:4187.675, sT:43294, sP:3.4852, sB:0.7822, nT:131872, nP:4.1025, nB:0.867
Cond:4887, Score:4187.536, sT:43268, sP:3.4843, sB:0.7822, nT:131802, nP:4.1017, nB:0.8675
Cond:4833, Score:4186.735, sT:43285, sP:3.4823, sB:0.7808, nT:131793, nP:4.0993, nB:0.8678
Cond:1077, Score:4186.554, sT:43301, sP:3.4831, sB:0.7804, nT:131870, nP:4.0997, nB:0.8678
Cond:6469, Score:4186.001, sT:43328, sP:3.4846, sB:0.781, nT:132017, nP:4.103, nB:0.8663
Cond:3327, Score:4185.456, sT:43770, sP:3.4908, sB:0.7748, nT:133437, nP:4.1038, nB:0.8593
Cond:1515, Score:4184.227, sT:43241, sP:3.4855, sB:0.7828, nT:131715, nP:4.1034, nB:0.8668
Cond:6695, Score:4183.8, sT:43255, sP:3.4837, sB:0.7814, nT:131687, nP:4.1015, nB:0.8676
Cond:1977, Score:4183.452, sT:43313, sP:3.4844, sB:0.7773, nT:132030, nP:4.103, nB:0.8706
Cond:7581, Score:4183.3, sT:43338, sP:3.4853, sB:0.7797, nT:131916, nP:4.1029, nB:0.8674
Cond:7789, Score:4183.112, sT:43388, sP:3.4896, sB:0.7806, nT:131765, nP:4.1027, nB:0.8669
Cond:8130, Score:4182.905, sT:43258, sP:3.4825, sB:0.7812, nT:131728, nP:4.0992, nB:0.8664
Cond:6693, Score:4182.632, sT:43240, sP:3.4843, sB:0.7812, nT:131662, nP:4.1022, nB:0.8681
Cond:3265, Score:4182.598, sT:43249, sP:3.4832, sB:0.7809, nT:131686, nP:4.1002, nB:0.8675
Cond:621, Score:4182.583, sT:43388, sP:3.4906, sB:0.7801, nT:132019, nP:4.1068, nB:0.867
Cond:6697, Score:4182.392, sT:43276, sP:3.4844, sB:0.7805, nT:131817, nP:4.1019, nB:0.8673
Cond:5303, Score:4182.103, sT:43276, sP:3.4837, sB:0.7811, nT:131830, nP:4.1017, nB:0.866
Cond:4677, Score:4182.095, sT:43254, sP:3.4835, sB:0.7805, nT:131717, nP:4.1016, nB:0.8679
Cond:7337, Score:4181.993, sT:43301, sP:3.4846, sB:0.7798, nT:131870, nP:4.1023, nB:0.8674
Cond:8144, Score:4181.697, sT:43265, sP:3.4827, sB:0.7808, nT:131746, nP:4.0992, nB:0.8662
Cond:3321, Score:4181.581, sT:43275, sP:3.4842, sB:0.7807, nT:131840, nP:4.1026, nB:0.8666
Cond:3160, Score:4181.574, sT:43321, sP:3.4842, sB:0.7801, nT:131931, nP:4.1024, nB:0.8659
Cond:1993, Score:4181.402, sT:43222, sP:3.4845, sB:0.7817, nT:131618, nP:4.1017, nB:0.8674
Cond:7545, Score:4181.153, sT:43249, sP:3.4847, sB:0.7806, nT:131710, nP:4.1022, nB:0.8679
Cond:3093, Score:4180.932, sT:43434, sP:3.4815, sB:0.7768, nT:132419, nP:4.0992, nB:0.8638
Cond:6467, Score:4180.684, sT:43278, sP:3.4852, sB:0.7807, nT:131828, nP:4.1034, nB:0.8666
Cond:7095, Score:4180.367, sT:43539, sP:3.4867, sB:0.776, nT:132895, nP:4.1062, nB:0.8625
Cond:3150, Score:4180.301, sT:43258, sP:3.4834, sB:0.7801, nT:131770, nP:4.1005, nB:0.867
Cond:3119, Score:4180.058, sT:43422, sP:3.4859, sB:0.7778, nT:132328, nP:4.1007, nB:0.8642 , T2:1973,6903,4887,4833,1077,6469,3327,1515,6695,1977,7581,7789,8130,6693,3265,621,6697,5303,4677,7337,8144,3321,3160,1993,7545,3093,6467,7095,3150,3119,  #End#
LowScoreRank3 , T0:275 , T1:
Cond:1973, Score:7321.132, sT:43351, sP:3.4825, sB:0.7796, nT:132069, nP:4.0996, nB:0.8672
Cond:3327, Score:7320.34, sT:43770, sP:3.4908, sB:0.7748, nT:133437, nP:4.1038, nB:0.8593
Cond:6903, Score:7319.835, sT:43294, sP:3.4852, sB:0.7822, nT:131872, nP:4.1025, nB:0.867
Cond:3164, Score:7319.497, sT:44778, sP:3.4777, sB:0.7428, nT:138555, nP:4.1078, nB:0.8498
Cond:4887, Score:7319.443, sT:43268, sP:3.4843, sB:0.7822, nT:131802, nP:4.1017, nB:0.8675
Cond:4833, Score:7318.29, sT:43285, sP:3.4823, sB:0.7808, nT:131793, nP:4.0993, nB:0.8678
Cond:1077, Score:7318.224, sT:43301, sP:3.4831, sB:0.7804, nT:131870, nP:4.0997, nB:0.8678
Cond:6469, Score:7317.322, sT:43328, sP:3.4846, sB:0.781, nT:132017, nP:4.103, nB:0.8663
Cond:1977, Score:7313.822, sT:43313, sP:3.4844, sB:0.7773, nT:132030, nP:4.103, nB:0.8706
Cond:1961, Score:7313.393, sT:43812, sP:3.4958, sB:0.7622, nT:134291, nP:4.1331, nB:0.8755
Cond:1515, Score:7313.255, sT:43241, sP:3.4855, sB:0.7828, nT:131715, nP:4.1034, nB:0.8668
Cond:6695, Score:7312.763, sT:43255, sP:3.4837, sB:0.7814, nT:131687, nP:4.1015, nB:0.8676
Cond:7581, Score:7312.75, sT:43338, sP:3.4853, sB:0.7797, nT:131916, nP:4.1029, nB:0.8674
Cond:7789, Score:7312.204, sT:43388, sP:3.4896, sB:0.7806, nT:131765, nP:4.1027, nB:0.8669
Cond:621, Score:7311.771, sT:43388, sP:3.4906, sB:0.7801, nT:132019, nP:4.1068, nB:0.867
Cond:8130, Score:7311.193, sT:43258, sP:3.4825, sB:0.7812, nT:131728, nP:4.0992, nB:0.8664
Cond:6693, Score:7310.718, sT:43240, sP:3.4843, sB:0.7812, nT:131662, nP:4.1022, nB:0.8681
Cond:3265, Score:7310.714, sT:43249, sP:3.4832, sB:0.7809, nT:131686, nP:4.1002, nB:0.8675
Cond:6697, Score:7310.694, sT:43276, sP:3.4844, sB:0.7805, nT:131817, nP:4.1019, nB:0.8673
Cond:7337, Score:7310.262, sT:43301, sP:3.4846, sB:0.7798, nT:131870, nP:4.1023, nB:0.8674
Cond:4677, Score:7309.982, sT:43254, sP:3.4835, sB:0.7805, nT:131717, nP:4.1016, nB:0.8679
Cond:5303, Score:7309.974, sT:43276, sP:3.4837, sB:0.7811, nT:131830, nP:4.1017, nB:0.866
Cond:7095, Score:7309.875, sT:43539, sP:3.4867, sB:0.776, nT:132895, nP:4.1062, nB:0.8625
Cond:3093, Score:7309.763, sT:43434, sP:3.4815, sB:0.7768, nT:132419, nP:4.0992, nB:0.8638
Cond:3160, Score:7309.474, sT:43321, sP:3.4842, sB:0.7801, nT:131931, nP:4.1024, nB:0.8659
Cond:7672, Score:7309.398, sT:44005, sP:3.4687, sB:0.7624, nT:134650, nP:4.0876, nB:0.8526
Cond:3321, Score:7309.192, sT:43275, sP:3.4842, sB:0.7807, nT:131840, nP:4.1026, nB:0.8666
Cond:8144, Score:7309.167, sT:43265, sP:3.4827, sB:0.7808, nT:131746, nP:4.0992, nB:0.8662
Cond:7321, Score:7308.5, sT:43797, sP:3.4899, sB:0.7721, nT:133898, nP:4.1118, nB:0.8579
Cond:7545, Score:7308.312, sT:43249, sP:3.4847, sB:0.7806, nT:131710, nP:4.1022, nB:0.8679 , T2:1973,3327,6903,3164,4887,4833,1077,6469,1977,1961,1515,6695,7581,7789,621,8130,6693,3265,6697,7337,4677,5303,7095,3093,3160,7672,3321,8144,7321,7545,  #End#
LowScoreRank0 , T0:1575 , T1:
Cond:4887, Score:1374.466, sT:42857, sP:3.5013, sB:0.7909, nT:131448, nP:4.1172, nB:0.8736
Cond:6903, Score:1374.462, sT:42883, sP:3.5022, sB:0.7909, nT:131518, nP:4.118, nB:0.8731
Cond:4833, Score:1374.064, sT:42873, sP:3.4993, sB:0.7894, nT:131437, nP:4.1148, nB:0.874
Cond:6469, Score:1373.867, sT:42917, sP:3.5015, sB:0.7897, nT:131663, nP:4.1185, nB:0.8725
Cond:1515, Score:1373.587, sT:42830, sP:3.5025, sB:0.7915, nT:131361, nP:4.1189, nB:0.873
Cond:6695, Score:1373.362, sT:42844, sP:3.5007, sB:0.7901, nT:131333, nP:4.117, nB:0.8738
Cond:7581, Score:1373.165, sT:42926, sP:3.5023, sB:0.7886, nT:131561, nP:4.1184, nB:0.8737
Cond:7789, Score:1373.106, sT:42976, sP:3.5066, sB:0.7895, nT:131410, nP:4.1182, nB:0.8731
Cond:3265, Score:1373.058, sT:42838, sP:3.5001, sB:0.7897, nT:131330, nP:4.1158, nB:0.8737
Cond:8144, Score:1372.996, sT:42855, sP:3.4996, sB:0.7898, nT:131379, nP:4.1149, nB:0.8725
Cond:1973, Score:1372.995, sT:42942, sP:3.4993, sB:0.7869, nT:131712, nP:4.1151, nB:0.8735
Cond:6693, Score:1372.905, sT:42829, sP:3.5013, sB:0.7899, nT:131308, nP:4.1178, nB:0.8742
Cond:6697, Score:1372.774, sT:42865, sP:3.5014, sB:0.7892, nT:131463, nP:4.1174, nB:0.8735
Cond:1077, Score:1372.766, sT:42892, sP:3.4999, sB:0.7878, nT:131513, nP:4.1152, nB:0.874
Cond:4677, Score:1372.741, sT:42843, sP:3.5005, sB:0.7892, nT:131363, nP:4.1171, nB:0.8741
Cond:5303, Score:1372.692, sT:42865, sP:3.5007, sB:0.7898, nT:131476, nP:4.1172, nB:0.8721
Cond:1977, Score:1372.685, sT:42902, sP:3.5014, sB:0.786, nT:131676, nP:4.1185, nB:0.8768
Cond:1588, Score:1372.671, sT:42924, sP:3.5009, sB:0.789, nT:131710, nP:4.1196, nB:0.8714
Cond:3327, Score:1372.628, sT:43359, sP:3.5076, sB:0.7837, nT:133081, nP:4.1191, nB:0.8655
Cond:1993, Score:1372.605, sT:42811, sP:3.5015, sB:0.7904, nT:131264, nP:4.1172, nB:0.8735
Cond:3321, Score:1372.541, sT:42864, sP:3.5012, sB:0.7894, nT:131486, nP:4.1181, nB:0.8728
Cond:621, Score:1372.518, sT:42977, sP:3.5076, sB:0.7888, nT:131665, nP:4.1223, nB:0.8731
Cond:7337, Score:1372.49, sT:42890, sP:3.5016, sB:0.7885, nT:131516, nP:4.1178, nB:0.8735
Cond:7545, Score:1372.44, sT:42838, sP:3.5017, sB:0.7893, nT:131356, nP:4.1177, nB:0.8741
Cond:8130, Score:1372.395, sT:42848, sP:3.4994, sB:0.7891, nT:131358, nP:4.115, nB:0.8729
Cond:5830, Score:1372.255, sT:42851, sP:3.5006, sB:0.7892, nT:131400, nP:4.1165, nB:0.8729
Cond:6467, Score:1372.238, sT:42867, sP:3.5022, sB:0.7894, nT:131474, nP:4.119, nB:0.8728
Cond:3299, Score:1372.18, sT:42814, sP:3.5015, sB:0.7897, nT:131278, nP:4.1171, nB:0.8738
Cond:3150, Score:1372.133, sT:42847, sP:3.5004, sB:0.7888, nT:131416, nP:4.116, nB:0.8732
Cond:4258, Score:1372.088, sT:42863, sP:3.501, sB:0.7892, nT:131463, nP:4.1192, nB:0.8727 , T2:4887,6903,4833,6469,1515,6695,7581,7789,3265,8144,1973,6693,6697,1077,4677,5303,1977,1588,3327,1993,3321,621,7337,7545,8130,5830,6467,3299,3150,4258,  #End#
LowScoreRank1 , T0:1575 , T1:
Cond:6903, Score:2398.103, sT:42883, sP:3.5022, sB:0.7909, nT:131518, nP:4.118, nB:0.8731
Cond:4887, Score:2398.06, sT:42857, sP:3.5013, sB:0.7909, nT:131448, nP:4.1172, nB:0.8736
Cond:4833, Score:2397.448, sT:42873, sP:3.4993, sB:0.7894, nT:131437, nP:4.1148, nB:0.874
Cond:6469, Score:2397.201, sT:42917, sP:3.5015, sB:0.7897, nT:131663, nP:4.1185, nB:0.8725
Cond:1515, Score:2396.399, sT:42830, sP:3.5025, sB:0.7915, nT:131361, nP:4.1189, nB:0.873
Cond:3327, Score:2396.332, sT:43359, sP:3.5076, sB:0.7837, nT:133081, nP:4.1191, nB:0.8655
Cond:6695, Score:2396.092, sT:42844, sP:3.5007, sB:0.7901, nT:131333, nP:4.117, nB:0.8738
Cond:7581, Score:2396.024, sT:42926, sP:3.5023, sB:0.7886, nT:131561, nP:4.1184, nB:0.8737
Cond:1973, Score:2395.894, sT:42942, sP:3.4993, sB:0.7869, nT:131712, nP:4.1151, nB:0.8735
Cond:7789, Score:2395.863, sT:42976, sP:3.5066, sB:0.7895, nT:131410, nP:4.1182, nB:0.8731
Cond:3265, Score:2395.57, sT:42838, sP:3.5001, sB:0.7897, nT:131330, nP:4.1158, nB:0.8737
Cond:8144, Score:2395.461, sT:42855, sP:3.4996, sB:0.7898, nT:131379, nP:4.1149, nB:0.8725
Cond:1977, Score:2395.444, sT:42902, sP:3.5014, sB:0.786, nT:131676, nP:4.1185, nB:0.8768
Cond:1077, Score:2395.314, sT:42892, sP:3.4999, sB:0.7878, nT:131513, nP:4.1152, nB:0.874
Cond:6693, Score:2395.286, sT:42829, sP:3.5013, sB:0.7899, nT:131308, nP:4.1178, nB:0.8742
Cond:1604, Score:2395.214, sT:43398, sP:3.5021, sB:0.7796, nT:133603, nP:4.1292, nB:0.8675
Cond:6697, Score:2395.19, sT:42865, sP:3.5014, sB:0.7892, nT:131463, nP:4.1174, nB:0.8735
Cond:1588, Score:2395.124, sT:42924, sP:3.5009, sB:0.789, nT:131710, nP:4.1196, nB:0.8714
Cond:4677, Score:2395.07, sT:42843, sP:3.5005, sB:0.7892, nT:131363, nP:4.1171, nB:0.8741
Cond:621, Score:2394.992, sT:42977, sP:3.5076, sB:0.7888, nT:131665, nP:4.1223, nB:0.8731
Cond:5303, Score:2394.974, sT:42865, sP:3.5007, sB:0.7898, nT:131476, nP:4.1172, nB:0.8721
Cond:7337, Score:2394.78, sT:42890, sP:3.5016, sB:0.7885, nT:131516, nP:4.1178, nB:0.8735
Cond:3321, Score:2394.756, sT:42864, sP:3.5012, sB:0.7894, nT:131486, nP:4.1181, nB:0.8728
Cond:1993, Score:2394.679, sT:42811, sP:3.5015, sB:0.7904, nT:131264, nP:4.1172, nB:0.8735
Cond:7545, Score:2394.538, sT:42838, sP:3.5017, sB:0.7893, nT:131356, nP:4.1177, nB:0.8741
Cond:8130, Score:2394.435, sT:42848, sP:3.4994, sB:0.7891, nT:131358, nP:4.115, nB:0.8729
Cond:6467, Score:2394.224, sT:42867, sP:3.5022, sB:0.7894, nT:131474, nP:4.119, nB:0.8728
Cond:5830, Score:2394.212, sT:42851, sP:3.5006, sB:0.7892, nT:131400, nP:4.1165, nB:0.8729
Cond:3160, Score:2394.034, sT:42911, sP:3.501, sB:0.7884, nT:131574, nP:4.118, nB:0.8721
Cond:3150, Score:2394.033, sT:42847, sP:3.5004, sB:0.7888, nT:131416, nP:4.116, nB:0.8732 , T2:6903,4887,4833,6469,1515,3327,6695,7581,1973,7789,3265,8144,1977,1077,6693,1604,6697,1588,4677,621,5303,7337,3321,1993,7545,8130,6467,5830,3160,3150,  #End#
LowScoreRank2 , T0:1575 , T1:
Cond:6903, Score:4187.316, sT:42883, sP:3.5022, sB:0.7909, nT:131518, nP:4.118, nB:0.8731
Cond:4887, Score:4187.154, sT:42857, sP:3.5013, sB:0.7909, nT:131448, nP:4.1172, nB:0.8736
Cond:3327, Score:4186.724, sT:43359, sP:3.5076, sB:0.7837, nT:133081, nP:4.1191, nB:0.8655
Cond:4833, Score:4186.238, sT:42873, sP:3.4993, sB:0.7894, nT:131437, nP:4.1148, nB:0.874
Cond:3444, Score:4186.14, sT:43827, sP:3.4702, sB:0.757, nT:133983, nP:4.0809, nB:0.871
Cond:6469, Score:4185.979, sT:42917, sP:3.5015, sB:0.7897, nT:131663, nP:4.1185, nB:0.8725
Cond:1604, Score:4185.606, sT:43398, sP:3.5021, sB:0.7796, nT:133603, nP:4.1292, nB:0.8675
Cond:1973, Score:4184.073, sT:42942, sP:3.4993, sB:0.7869, nT:131712, nP:4.1151, nB:0.8735
Cond:1515, Score:4184.03, sT:42830, sP:3.5025, sB:0.7915, nT:131361, nP:4.1189, nB:0.873
Cond:7581, Score:4184.006, sT:42926, sP:3.5023, sB:0.7886, nT:131561, nP:4.1184, nB:0.8737
Cond:6695, Score:4183.642, sT:42844, sP:3.5007, sB:0.7901, nT:131333, nP:4.117, nB:0.8738
Cond:7789, Score:4183.607, sT:42976, sP:3.5066, sB:0.7895, nT:131410, nP:4.1182, nB:0.8731
Cond:1977, Score:4183.452, sT:42902, sP:3.5014, sB:0.786, nT:131676, nP:4.1185, nB:0.8768
Cond:3265, Score:4182.746, sT:42838, sP:3.5001, sB:0.7897, nT:131330, nP:4.1158, nB:0.8737
Cond:1077, Score:4182.744, sT:42892, sP:3.4999, sB:0.7878, nT:131513, nP:4.1152, nB:0.874
Cond:8144, Score:4182.553, sT:42855, sP:3.4996, sB:0.7898, nT:131379, nP:4.1149, nB:0.8725
Cond:1588, Score:4182.37, sT:42924, sP:3.5009, sB:0.789, nT:131710, nP:4.1196, nB:0.8714
Cond:621, Score:4182.367, sT:42977, sP:3.5076, sB:0.7888, nT:131665, nP:4.1223, nB:0.8731
Cond:6697, Score:4182.287, sT:42865, sP:3.5014, sB:0.7892, nT:131463, nP:4.1174, nB:0.8735
Cond:6693, Score:4182.222, sT:42829, sP:3.5013, sB:0.7899, nT:131308, nP:4.1178, nB:0.8742
Cond:4677, Score:4181.967, sT:42843, sP:3.5005, sB:0.7892, nT:131363, nP:4.1171, nB:0.8741
Cond:5303, Score:4181.781, sT:42865, sP:3.5007, sB:0.7898, nT:131476, nP:4.1172, nB:0.8721
Cond:7337, Score:4181.717, sT:42890, sP:3.5016, sB:0.7885, nT:131516, nP:4.1178, nB:0.8735
Cond:3321, Score:4181.479, sT:42864, sP:3.5012, sB:0.7894, nT:131486, nP:4.1181, nB:0.8728
Cond:7545, Score:4181.024, sT:42838, sP:3.5017, sB:0.7893, nT:131356, nP:4.1177, nB:0.8741
Cond:1993, Score:4181.013, sT:42811, sP:3.5015, sB:0.7904, nT:131264, nP:4.1172, nB:0.8735
Cond:8130, Score:4180.801, sT:42848, sP:3.4994, sB:0.7891, nT:131358, nP:4.115, nB:0.8729
Cond:7672, Score:4180.717, sT:43588, sP:3.4855, sB:0.772, nT:134258, nP:4.1034, nB:0.859
Cond:6467, Score:4180.547, sT:42867, sP:3.5022, sB:0.7894, nT:131474, nP:4.119, nB:0.8728
Cond:7095, Score:4180.499, sT:43129, sP:3.5038, sB:0.7847, nT:132540, nP:4.1215, nB:0.8686 , T2:6903,4887,3327,4833,3444,6469,1604,1973,1515,7581,6695,7789,1977,3265,1077,8144,1588,621,6697,6693,4677,5303,7337,3321,7545,1993,8130,7672,6467,7095,  #End#
LowScoreRank3 , T0:1575 , T1:
Cond:3444, Score:7326.432, sT:43827, sP:3.4702, sB:0.757, nT:133983, nP:4.0809, nB:0.871
Cond:472, Score:7325.458, sT:44693, sP:3.4909, sB:0.7458, nT:137865, nP:4.1012, nB:0.8548
Cond:3164, Score:7321.256, sT:44358, sP:3.4947, sB:0.7513, nT:138167, nP:4.1231, nB:0.8565
Cond:3327, Score:7320.447, sT:43359, sP:3.5076, sB:0.7837, nT:133081, nP:4.1191, nB:0.8655
Cond:1604, Score:7319.986, sT:43398, sP:3.5021, sB:0.7796, nT:133603, nP:4.1292, nB:0.8675
Cond:6903, Score:7317.098, sT:42883, sP:3.5022, sB:0.7909, nT:131518, nP:4.118, nB:0.8731
Cond:4887, Score:7316.665, sT:42857, sP:3.5013, sB:0.7909, nT:131448, nP:4.1172, nB:0.8736
Cond:4833, Score:7315.329, sT:42873, sP:3.4993, sB:0.7894, nT:131437, nP:4.1148, nB:0.874
Cond:6469, Score:7315.186, sT:42917, sP:3.5015, sB:0.7897, nT:131663, nP:4.1185, nB:0.8725
Cond:7672, Score:7313.323, sT:43588, sP:3.4855, sB:0.772, nT:134258, nP:4.1034, nB:0.859
Cond:1973, Score:7312.51, sT:42942, sP:3.4993, sB:0.7869, nT:131712, nP:4.1151, nB:0.8735
Cond:7581, Score:7311.868, sT:42926, sP:3.5023, sB:0.7886, nT:131561, nP:4.1184, nB:0.8737
Cond:1977, Score:7311.725, sT:42902, sP:3.5014, sB:0.786, nT:131676, nP:4.1185, nB:0.8768
Cond:7789, Score:7310.943, sT:42976, sP:3.5066, sB:0.7895, nT:131410, nP:4.1182, nB:0.8731
Cond:1515, Score:7310.814, sT:42830, sP:3.5025, sB:0.7915, nT:131361, nP:4.1189, nB:0.873
Cond:1961, Score:7310.591, sT:43400, sP:3.5128, sB:0.7706, nT:133937, nP:4.1484, nB:0.8815
Cond:6695, Score:7310.389, sT:42844, sP:3.5007, sB:0.7901, nT:131333, nP:4.117, nB:0.8738
Cond:1077, Score:7309.629, sT:42892, sP:3.4999, sB:0.7878, nT:131513, nP:4.1152, nB:0.874
Cond:621, Score:7309.288, sT:42977, sP:3.5076, sB:0.7888, nT:131665, nP:4.1223, nB:0.8731
Cond:1588, Score:7308.909, sT:42924, sP:3.5009, sB:0.789, nT:131710, nP:4.1196, nB:0.8714
Cond:3265, Score:7308.854, sT:42838, sP:3.5001, sB:0.7897, nT:131330, nP:4.1158, nB:0.8737
Cond:8144, Score:7308.512, sT:42855, sP:3.4996, sB:0.7898, nT:131379, nP:4.1149, nB:0.8725
Cond:6697, Score:7308.415, sT:42865, sP:3.5014, sB:0.7892, nT:131463, nP:4.1174, nB:0.8735
Cond:7095, Score:7308.017, sT:43129, sP:3.5038, sB:0.7847, nT:132540, nP:4.1215, nB:0.8686
Cond:6693, Score:7307.889, sT:42829, sP:3.5013, sB:0.7899, nT:131308, nP:4.1178, nB:0.8742
Cond:7337, Score:7307.673, sT:42890, sP:3.5016, sB:0.7885, nT:131516, nP:4.1178, nB:0.8735
Cond:4677, Score:7307.66, sT:42843, sP:3.5005, sB:0.7892, nT:131363, nP:4.1171, nB:0.8741
Cond:5303, Score:7307.304, sT:42865, sP:3.5007, sB:0.7898, nT:131476, nP:4.1172, nB:0.8721
Cond:3321, Score:7306.917, sT:42864, sP:3.5012, sB:0.7894, nT:131486, nP:4.1181, nB:0.8728
Cond:3220, Score:7306.717, sT:42970, sP:3.4971, sB:0.7834, nT:131658, nP:4.1108, nB:0.875 , T2:3444,472,3164,3327,1604,6903,4887,4833,6469,7672,1973,7581,1977,7789,1515,1961,6695,1077,621,1588,3265,8144,6697,7095,6693,7337,4677,5303,3321,3220,  #End#
LowScoreRank0 , T0:6385 , T1:
Cond:4833, Score:1391.3, sT:37912, sP:3.2681, sB:0.8164, nT:114344, nP:3.8091, nB:0.9335
Cond:4887, Score:1391.271, sT:37896, sP:3.2703, sB:0.8177, nT:114353, nP:3.8119, nB:0.9331
Cond:6903, Score:1391.094, sT:37922, sP:3.2712, sB:0.8177, nT:114417, nP:3.813, nB:0.9323
Cond:1515, Score:1390.506, sT:37869, sP:3.2714, sB:0.8186, nT:114264, nP:3.8135, nB:0.9324
Cond:1973, Score:1390.501, sT:37977, sP:3.2689, sB:0.8143, nT:114620, nP:3.8101, nB:0.9327
Cond:1077, Score:1390.365, sT:37927, sP:3.2692, sB:0.8153, nT:114421, nP:3.8097, nB:0.9334
Cond:6469, Score:1390.352, sT:37956, sP:3.271, sB:0.8163, nT:114567, nP:3.814, nB:0.9317
Cond:6695, Score:1390.086, sT:37883, sP:3.2696, sB:0.8168, nT:114238, nP:3.8113, nB:0.9333
Cond:3265, Score:1390.028, sT:37876, sP:3.269, sB:0.8166, nT:114237, nP:3.8099, nB:0.9332
Cond:8130, Score:1389.838, sT:37886, sP:3.2683, sB:0.8166, nT:114279, nP:3.8088, nB:0.9319
Cond:6693, Score:1389.689, sT:37868, sP:3.2702, sB:0.8166, nT:114213, nP:3.8121, nB:0.9339
Cond:7581, Score:1389.621, sT:37947, sP:3.2701, sB:0.815, nT:114405, nP:3.8121, nB:0.9331
Cond:6808, Score:1389.545, sT:38470, sP:3.2643, sB:0.7998, nT:116581, nP:3.8057, nB:0.9275
Cond:1993, Score:1389.505, sT:37850, sP:3.2703, sB:0.8173, nT:114169, nP:3.8114, nB:0.9331
Cond:3150, Score:1389.465, sT:37882, sP:3.2696, sB:0.8161, nT:114298, nP:3.8105, nB:0.9329
Cond:6697, Score:1389.463, sT:37904, sP:3.2705, sB:0.8159, nT:114368, nP:3.8122, nB:0.9329
Cond:4677, Score:1389.394, sT:37882, sP:3.2694, sB:0.8158, nT:114268, nP:3.8115, nB:0.9336
Cond:8144, Score:1389.393, sT:37893, sP:3.2685, sB:0.8162, nT:114297, nP:3.8088, nB:0.9316
Cond:5303, Score:1389.371, sT:37904, sP:3.2697, sB:0.8165, nT:114381, nP:3.812, nB:0.9314
Cond:3321, Score:1389.19, sT:37903, sP:3.2702, sB:0.8161, nT:114391, nP:3.813, nB:0.9321
Cond:3160, Score:1389.172, sT:37938, sP:3.27, sB:0.8155, nT:114427, nP:3.811, nB:0.9314
Cond:1977, Score:1389.167, sT:37939, sP:3.2708, sB:0.8122, nT:114581, nP:3.8139, nB:0.9366
Cond:7545, Score:1389.017, sT:37877, sP:3.2707, sB:0.8159, nT:114261, nP:3.8122, nB:0.9336
Cond:7337, Score:1388.931, sT:37928, sP:3.2707, sB:0.8149, nT:114417, nP:3.8127, nB:0.9329
Cond:3299, Score:1388.914, sT:37853, sP:3.2703, sB:0.8164, nT:114183, nP:3.8113, nB:0.9334
Cond:1457, Score:1388.812, sT:37857, sP:3.2697, sB:0.8167, nT:114164, nP:3.811, nB:0.9325
Cond:6780, Score:1388.79, sT:38164, sP:3.2818, sB:0.8133, nT:115484, nP:3.8303, nB:0.9303
Cond:7672, Score:1388.725, sT:38527, sP:3.2545, sB:0.7989, nT:116595, nP:3.7957, nB:0.9207
Cond:6467, Score:1388.717, sT:37906, sP:3.2714, sB:0.816, nT:114379, nP:3.8139, nB:0.9321
Cond:4679, Score:1388.564, sT:37898, sP:3.2704, sB:0.8149, nT:114363, nP:3.8122, nB:0.9332 , T2:4833,4887,6903,1515,1973,1077,6469,6695,3265,8130,6693,7581,6808,1993,3150,6697,4677,8144,5303,3321,3160,1977,7545,7337,3299,1457,6780,7672,6467,4679,  #End#
LowScoreRank1 , T0:6385 , T1:
Cond:4833, Score:2412.823, sT:37912, sP:3.2681, sB:0.8164, nT:114344, nP:3.8091, nB:0.9335
Cond:4887, Score:2412.692, sT:37896, sP:3.2703, sB:0.8177, nT:114353, nP:3.8119, nB:0.9331
Cond:6903, Score:2412.429, sT:37922, sP:3.2712, sB:0.8177, nT:114417, nP:3.813, nB:0.9323
Cond:6808, Score:2412.359, sT:38470, sP:3.2643, sB:0.7998, nT:116581, nP:3.8057, nB:0.9275
Cond:1973, Score:2411.752, sT:37977, sP:3.2689, sB:0.8143, nT:114620, nP:3.8101, nB:0.9327
Cond:1077, Score:2411.315, sT:37927, sP:3.2692, sB:0.8153, nT:114421, nP:3.8097, nB:0.9334
Cond:6469, Score:2411.306, sT:37956, sP:3.271, sB:0.8163, nT:114567, nP:3.814, nB:0.9317
Cond:1515, Score:2411.211, sT:37869, sP:3.2714, sB:0.8186, nT:114264, nP:3.8135, nB:0.9324
Cond:7672, Score:2410.801, sT:38527, sP:3.2545, sB:0.7989, nT:116595, nP:3.7957, nB:0.9207
Cond:6695, Score:2410.586, sT:37883, sP:3.2696, sB:0.8168, nT:114238, nP:3.8113, nB:0.9333
Cond:3265, Score:2410.486, sT:37876, sP:3.269, sB:0.8166, nT:114237, nP:3.8099, nB:0.9332
Cond:6388, Score:2410.341, sT:38745, sP:3.305, sB:0.8111, nT:117675, nP:3.8981, nB:0.9276
Cond:8130, Score:2410.154, sT:37886, sP:3.2683, sB:0.8166, nT:114279, nP:3.8088, nB:0.9319
Cond:7581, Score:2410.029, sT:37947, sP:3.2701, sB:0.815, nT:114405, nP:3.8121, nB:0.9331
Cond:6693, Score:2409.891, sT:37868, sP:3.2702, sB:0.8166, nT:114213, nP:3.8121, nB:0.9339
Cond:6697, Score:2409.64, sT:37904, sP:3.2705, sB:0.8159, nT:114368, nP:3.8122, nB:0.9329
Cond:1977, Score:2409.575, sT:37939, sP:3.2708, sB:0.8122, nT:114581, nP:3.8139, nB:0.9366
Cond:3150, Score:2409.568, sT:37882, sP:3.2696, sB:0.8161, nT:114298, nP:3.8105, nB:0.9329
Cond:6780, Score:2409.485, sT:38164, sP:3.2818, sB:0.8133, nT:115484, nP:3.8303, nB:0.9303
Cond:1993, Score:2409.471, sT:37850, sP:3.2703, sB:0.8173, nT:114169, nP:3.8114, nB:0.9331
Cond:4677, Score:2409.451, sT:37882, sP:3.2694, sB:0.8158, nT:114268, nP:3.8115, nB:0.9336
Cond:8144, Score:2409.41, sT:37893, sP:3.2685, sB:0.8162, nT:114297, nP:3.8088, nB:0.9316
Cond:5303, Score:2409.408, sT:37904, sP:3.2697, sB:0.8165, nT:114381, nP:3.812, nB:0.9314
Cond:3160, Score:2409.182, sT:37938, sP:3.27, sB:0.8155, nT:114427, nP:3.811, nB:0.9314
Cond:3321, Score:2409.135, sT:37903, sP:3.2702, sB:0.8161, nT:114391, nP:3.813, nB:0.9321
Cond:7337, Score:2408.815, sT:37928, sP:3.2707, sB:0.8149, nT:114417, nP:3.8127, nB:0.9329
Cond:7545, Score:2408.788, sT:37877, sP:3.2707, sB:0.8159, nT:114261, nP:3.8122, nB:0.9336
Cond:3299, Score:2408.507, sT:37853, sP:3.2703, sB:0.8164, nT:114183, nP:3.8113, nB:0.9334
Cond:621, Score:2408.331, sT:38015, sP:3.2782, sB:0.8152, nT:114568, nP:3.8183, nB:0.9324
Cond:6467, Score:2408.319, sT:37906, sP:3.2714, sB:0.816, nT:114379, nP:3.8139, nB:0.9321 , T2:4833,4887,6903,6808,1973,1077,6469,1515,7672,6695,3265,6388,8130,7581,6693,6697,1977,3150,6780,1993,4677,8144,5303,3160,3321,7337,7545,3299,621,6467,  #End#
LowScoreRank2 , T0:6385 , T1:
Cond:6808, Score:4191.21, sT:38470, sP:3.2643, sB:0.7998, nT:116581, nP:3.8057, nB:0.9275
Cond:7672, Score:4188.255, sT:38527, sP:3.2545, sB:0.7989, nT:116595, nP:3.7957, nB:0.9207
Cond:6388, Score:4188.074, sT:38745, sP:3.305, sB:0.8111, nT:117675, nP:3.8981, nB:0.9276
Cond:4833, Score:4187.497, sT:37912, sP:3.2681, sB:0.8164, nT:114344, nP:3.8091, nB:0.9335
Cond:4887, Score:4187.131, sT:37896, sP:3.2703, sB:0.8177, nT:114353, nP:3.8119, nB:0.9331
Cond:6903, Score:4186.751, sT:37922, sP:3.2712, sB:0.8177, nT:114417, nP:3.813, nB:0.9323
Cond:1973, Score:4186.19, sT:37977, sP:3.2689, sB:0.8143, nT:114620, nP:3.8101, nB:0.9327
Cond:6469, Score:4185.091, sT:37956, sP:3.271, sB:0.8163, nT:114567, nP:3.814, nB:0.9317
Cond:1077, Score:4185.081, sT:37927, sP:3.2692, sB:0.8153, nT:114421, nP:3.8097, nB:0.9334
Cond:1515, Score:4184.291, sT:37869, sP:3.2714, sB:0.8186, nT:114264, nP:3.8135, nB:0.9324
Cond:6780, Score:4183.484, sT:38164, sP:3.2818, sB:0.8133, nT:115484, nP:3.8303, nB:0.9303
Cond:6695, Score:4183.386, sT:37883, sP:3.2696, sB:0.8168, nT:114238, nP:3.8113, nB:0.9333
Cond:3265, Score:4183.215, sT:37876, sP:3.269, sB:0.8166, nT:114237, nP:3.8099, nB:0.9332
Cond:7581, Score:4182.849, sT:37947, sP:3.2701, sB:0.815, nT:114405, nP:3.8121, nB:0.9331
Cond:1977, Score:4182.654, sT:37939, sP:3.2708, sB:0.8122, nT:114581, nP:3.8139, nB:0.9366
Cond:8130, Score:4182.634, sT:37886, sP:3.2683, sB:0.8166, nT:114279, nP:3.8088, nB:0.9319
Cond:6693, Score:4182.169, sT:37868, sP:3.2702, sB:0.8166, nT:114213, nP:3.8121, nB:0.9339
Cond:6697, Score:4181.979, sT:37904, sP:3.2705, sB:0.8159, nT:114368, nP:3.8122, nB:0.9329
Cond:3150, Score:4181.725, sT:37882, sP:3.2696, sB:0.8161, nT:114298, nP:3.8105, nB:0.9329
Cond:4677, Score:4181.532, sT:37882, sP:3.2694, sB:0.8158, nT:114268, nP:3.8115, nB:0.9336
Cond:5303, Score:4181.45, sT:37904, sP:3.2697, sB:0.8165, nT:114381, nP:3.812, nB:0.9314
Cond:8144, Score:4181.389, sT:37893, sP:3.2685, sB:0.8162, nT:114297, nP:3.8088, nB:0.9316
Cond:1993, Score:4181.267, sT:37850, sP:3.2703, sB:0.8173, nT:114169, nP:3.8114, nB:0.9331
Cond:3160, Score:4181.264, sT:37938, sP:3.27, sB:0.8155, nT:114427, nP:3.811, nB:0.9314
Cond:3327, Score:4181.058, sT:38344, sP:3.2758, sB:0.8094, nT:115782, nP:3.8152, nB:0.9229
Cond:3321, Score:4181.051, sT:37903, sP:3.2702, sB:0.8161, nT:114391, nP:3.813, nB:0.9321
Cond:7337, Score:4180.717, sT:37928, sP:3.2707, sB:0.8149, nT:114417, nP:3.8127, nB:0.9329
Cond:7545, Score:4180.367, sT:37877, sP:3.2707, sB:0.8159, nT:114261, nP:3.8122, nB:0.9336
Cond:621, Score:4180.206, sT:38015, sP:3.2782, sB:0.8152, nT:114568, nP:3.8183, nB:0.9324
Cond:7922, Score:4179.888, sT:37969, sP:3.2657, sB:0.8123, nT:114557, nP:3.8052, nB:0.931 , T2:6808,7672,6388,4833,4887,6903,1973,6469,1077,1515,6780,6695,3265,7581,1977,8130,6693,6697,3150,4677,5303,8144,1993,3160,3327,3321,7337,7545,621,7922,  #End#
LowScoreRank3 , T0:6385 , T1:
Cond:6808, Score:7287.305, sT:38470, sP:3.2643, sB:0.7998, nT:116581, nP:3.8057, nB:0.9275
Cond:6388, Score:7282.506, sT:38745, sP:3.305, sB:0.8111, nT:117675, nP:3.8981, nB:0.9276
Cond:7672, Score:7281.719, sT:38527, sP:3.2545, sB:0.7989, nT:116595, nP:3.7957, nB:0.9207
Cond:4833, Score:7272.944, sT:37912, sP:3.2681, sB:0.8164, nT:114344, nP:3.8091, nB:0.9335
Cond:3164, Score:7272.607, sT:39192, sP:3.2611, sB:0.7761, nT:119728, nP:3.8068, nB:0.9149
Cond:4887, Score:7272.071, sT:37896, sP:3.2703, sB:0.8177, nT:114353, nP:3.8119, nB:0.9331
Cond:1973, Score:7271.639, sT:37977, sP:3.2689, sB:0.8143, nT:114620, nP:3.8101, nB:0.9327
Cond:6903, Score:7271.541, sT:37922, sP:3.2712, sB:0.8177, nT:114417, nP:3.813, nB:0.9323
Cond:6469, Score:7269.164, sT:37956, sP:3.271, sB:0.8163, nT:114567, nP:3.814, nB:0.9317
Cond:6780, Score:7269.103, sT:38164, sP:3.2818, sB:0.8133, nT:115484, nP:3.8303, nB:0.9303
Cond:1077, Score:7269.098, sT:37927, sP:3.2692, sB:0.8153, nT:114421, nP:3.8097, nB:0.9334
Cond:472, Score:7268.037, sT:39523, sP:3.2556, sB:0.7689, nT:119632, nP:3.7871, nB:0.9104
Cond:1515, Score:7266.671, sT:37869, sP:3.2714, sB:0.8186, nT:114264, nP:3.8135, nB:0.9324
Cond:3327, Score:7265.945, sT:38344, sP:3.2758, sB:0.8094, nT:115782, nP:3.8152, nB:0.9229
Cond:1977, Score:7265.932, sT:37939, sP:3.2708, sB:0.8122, nT:114581, nP:3.8139, nB:0.9366
Cond:3444, Score:7265.889, sT:38782, sP:3.2397, sB:0.7808, nT:116557, nP:3.7773, nB:0.9266
Cond:1862, Score:7265.724, sT:38752, sP:3.2751, sB:0.7956, nT:116310, nP:3.8125, nB:0.9266
Cond:6695, Score:7265.402, sT:37883, sP:3.2696, sB:0.8168, nT:114238, nP:3.8113, nB:0.9333
Cond:7581, Score:7265.218, sT:37947, sP:3.2701, sB:0.815, nT:114405, nP:3.8121, nB:0.9331
Cond:3265, Score:7265.114, sT:37876, sP:3.269, sB:0.8166, nT:114237, nP:3.8099, nB:0.9332
Cond:8130, Score:7264.095, sT:37886, sP:3.2683, sB:0.8166, nT:114279, nP:3.8088, nB:0.9319
Cond:6697, Score:7263.376, sT:37904, sP:3.2705, sB:0.8159, nT:114368, nP:3.8122, nB:0.9329
Cond:6693, Score:7263.274, sT:37868, sP:3.2702, sB:0.8166, nT:114213, nP:3.8121, nB:0.9339
Cond:3234, Score:7262.872, sT:38535, sP:3.2844, sB:0.8021, nT:115947, nP:3.8203, nB:0.9294
Cond:3150, Score:7262.707, sT:37882, sP:3.2696, sB:0.8161, nT:114298, nP:3.8105, nB:0.9329
Cond:4677, Score:7262.388, sT:37882, sP:3.2694, sB:0.8158, nT:114268, nP:3.8115, nB:0.9336
Cond:3160, Score:7262.267, sT:37938, sP:3.27, sB:0.8155, nT:114427, nP:3.811, nB:0.9314
Cond:5303, Score:7262.235, sT:37904, sP:3.2697, sB:0.8165, nT:114381, nP:3.812, nB:0.9314
Cond:4970, Score:7262.144, sT:38570, sP:3.2712, sB:0.7996, nT:117152, nP:3.8162, nB:0.9185
Cond:8144, Score:7262.016, sT:37893, sP:3.2685, sB:0.8162, nT:114297, nP:3.8088, nB:0.9316 , T2:6808,6388,7672,4833,3164,4887,1973,6903,6469,6780,1077,472,1515,3327,1977,3444,1862,6695,7581,3265,8130,6697,6693,3234,3150,4677,3160,5303,4970,8144,  #End#
LowScoreRank0 , T0:2695 , T1:
Cond:6903, Score:1374.082, sT:42531, sP:3.5163, sB:0.7979, nT:130685, nP:4.1317, nB:0.8803
Cond:4887, Score:1374.078, sT:42505, sP:3.5154, sB:0.7979, nT:130615, nP:4.1309, nB:0.8808
Cond:6469, Score:1373.621, sT:42565, sP:3.5157, sB:0.7969, nT:130828, nP:4.1322, nB:0.8796
Cond:4833, Score:1373.593, sT:42517, sP:3.5136, sB:0.7965, nT:130588, nP:4.1289, nB:0.8812
Cond:1515, Score:1373.194, sT:42478, sP:3.5166, sB:0.7985, nT:130528, nP:4.1326, nB:0.8802
Cond:6695, Score:1372.875, sT:42492, sP:3.5148, sB:0.797, nT:130500, nP:4.1307, nB:0.881
Cond:3444, Score:1372.803, sT:43431, sP:3.4855, sB:0.7669, nT:133054, nP:4.0956, nB:0.88
Cond:7581, Score:1372.645, sT:42574, sP:3.5164, sB:0.7955, nT:130728, nP:4.1321, nB:0.8808
Cond:1604, Score:1372.641, sT:43020, sP:3.5165, sB:0.7884, nT:132695, nP:4.1434, nB:0.8746
Cond:3265, Score:1372.519, sT:42482, sP:3.5145, sB:0.7968, nT:130484, nP:4.1298, nB:0.8808
Cond:6693, Score:1372.498, sT:42477, sP:3.5155, sB:0.7969, nT:130475, nP:4.1315, nB:0.8814
Cond:7789, Score:1372.417, sT:42622, sP:3.5208, sB:0.7963, nT:130575, nP:4.132, nB:0.8802
Cond:4677, Score:1372.377, sT:42492, sP:3.5147, sB:0.7963, nT:130528, nP:4.1309, nB:0.8812
Cond:6697, Score:1372.368, sT:42512, sP:3.5155, sB:0.7962, nT:130627, nP:4.1312, nB:0.8807
Cond:1588, Score:1372.363, sT:42567, sP:3.5153, sB:0.7963, nT:130854, nP:4.1333, nB:0.8785
Cond:1077, Score:1372.316, sT:42532, sP:3.5145, sB:0.7952, nT:130637, nP:4.1296, nB:0.8811
Cond:3327, Score:1372.232, sT:43005, sP:3.5218, sB:0.7906, nT:132244, nP:4.1327, nB:0.8726
Cond:5303, Score:1372.222, sT:42513, sP:3.5148, sB:0.7967, nT:130643, nP:4.1309, nB:0.8793
Cond:1993, Score:1372.194, sT:42459, sP:3.5157, sB:0.7974, nT:130431, nP:4.1309, nB:0.8807
Cond:1977, Score:1372.174, sT:42550, sP:3.5155, sB:0.7929, nT:130843, nP:4.1322, nB:0.8839
Cond:7337, Score:1372.132, sT:42538, sP:3.5157, sB:0.7955, nT:130683, nP:4.1315, nB:0.8807
Cond:3321, Score:1372.11, sT:42512, sP:3.5153, sB:0.7964, nT:130653, nP:4.1318, nB:0.8799
Cond:3220, Score:1372.095, sT:42615, sP:3.5111, sB:0.7911, nT:130818, nP:4.1245, nB:0.8825
Cond:621, Score:1372.057, sT:42625, sP:3.5218, sB:0.7957, nT:130832, nP:4.136, nB:0.8803
Cond:8144, Score:1372.057, sT:42494, sP:3.514, sB:0.7964, nT:130519, nP:4.1292, nB:0.88
Cond:6467, Score:1372.053, sT:42515, sP:3.5164, sB:0.7966, nT:130640, nP:4.1327, nB:0.88
Cond:1973, Score:1371.973, sT:42577, sP:3.5137, sB:0.7938, nT:130836, nP:4.1294, nB:0.8805
Cond:8130, Score:1371.912, sT:42483, sP:3.5142, sB:0.7963, nT:130495, nP:4.1294, nB:0.8804
Cond:7545, Score:1371.877, sT:42486, sP:3.5159, sB:0.7962, nT:130523, nP:4.1314, nB:0.8812
Cond:3299, Score:1371.805, sT:42463, sP:3.5157, sB:0.7968, nT:130442, nP:4.1309, nB:0.8809 , T2:6903,4887,6469,4833,1515,6695,3444,7581,1604,3265,6693,7789,4677,6697,1588,1077,3327,5303,1993,1977,7337,3321,3220,621,8144,6467,1973,8130,7545,3299,  #End#
LowScoreRank1 , T0:2695 , T1:
Cond:3444, Score:2397.942, sT:43431, sP:3.4855, sB:0.7669, nT:133054, nP:4.0956, nB:0.88
Cond:6903, Score:2396.589, sT:42531, sP:3.5163, sB:0.7979, nT:130685, nP:4.1317, nB:0.8803
Cond:4887, Score:2396.531, sT:42505, sP:3.5154, sB:0.7979, nT:130615, nP:4.1309, nB:0.8808
Cond:6469, Score:2395.91, sT:42565, sP:3.5157, sB:0.7969, nT:130828, nP:4.1322, nB:0.8796
Cond:1604, Score:2395.83, sT:43020, sP:3.5165, sB:0.7884, nT:132695, nP:4.1434, nB:0.8746
Cond:4833, Score:2395.754, sT:42517, sP:3.5136, sB:0.7965, nT:130588, nP:4.1289, nB:0.8812
Cond:1515, Score:2394.861, sT:42478, sP:3.5166, sB:0.7985, nT:130528, nP:4.1326, nB:0.8802
Cond:3327, Score:2394.796, sT:43005, sP:3.5218, sB:0.7906, nT:132244, nP:4.1327, nB:0.8726
Cond:6695, Score:2394.394, sT:42492, sP:3.5148, sB:0.797, nT:130500, nP:4.1307, nB:0.881
Cond:7581, Score:2394.266, sT:42574, sP:3.5164, sB:0.7955, nT:130728, nP:4.1321, nB:0.8808
Cond:7789, Score:2393.812, sT:42622, sP:3.5208, sB:0.7963, nT:130575, nP:4.132, nB:0.8802
Cond:3265, Score:2393.757, sT:42482, sP:3.5145, sB:0.7968, nT:130484, nP:4.1298, nB:0.8808
Cond:6693, Score:2393.726, sT:42477, sP:3.5155, sB:0.7969, nT:130475, nP:4.1315, nB:0.8814
Cond:1588, Score:2393.705, sT:42567, sP:3.5153, sB:0.7963, nT:130854, nP:4.1333, nB:0.8785
Cond:1977, Score:2393.701, sT:42550, sP:3.5155, sB:0.7929, nT:130843, nP:4.1322, nB:0.8839
Cond:3220, Score:2393.658, sT:42615, sP:3.5111, sB:0.7911, nT:130818, nP:4.1245, nB:0.8825
Cond:6697, Score:2393.628, sT:42512, sP:3.5155, sB:0.7962, nT:130627, nP:4.1312, nB:0.8807
Cond:1077, Score:2393.623, sT:42532, sP:3.5145, sB:0.7952, nT:130637, nP:4.1296, nB:0.8811
Cond:4677, Score:2393.575, sT:42492, sP:3.5147, sB:0.7963, nT:130528, nP:4.1309, nB:0.8812
Cond:621, Score:2393.343, sT:42625, sP:3.5218, sB:0.7957, nT:130832, nP:4.136, nB:0.8803
Cond:5303, Score:2393.307, sT:42513, sP:3.5148, sB:0.7967, nT:130643, nP:4.1309, nB:0.8793
Cond:7337, Score:2393.304, sT:42538, sP:3.5157, sB:0.7955, nT:130683, nP:4.1315, nB:0.8807
Cond:1973, Score:2393.216, sT:42577, sP:3.5137, sB:0.7938, nT:130836, nP:4.1294, nB:0.8805
Cond:3321, Score:2393.149, sT:42512, sP:3.5153, sB:0.7964, nT:130653, nP:4.1318, nB:0.8799
Cond:1993, Score:2393.111, sT:42459, sP:3.5157, sB:0.7974, nT:130431, nP:4.1309, nB:0.8807
Cond:6467, Score:2393.042, sT:42515, sP:3.5164, sB:0.7966, nT:130640, nP:4.1327, nB:0.88
Cond:8144, Score:2392.972, sT:42494, sP:3.514, sB:0.7964, nT:130519, nP:4.1292, nB:0.88
Cond:472, Score:2392.913, sT:44217, sP:3.5089, sB:0.7573, nT:136533, nP:4.1184, nB:0.8666
Cond:7922, Score:2392.874, sT:42570, sP:3.5112, sB:0.7933, nT:130773, nP:4.1256, nB:0.88
Cond:8130, Score:2392.714, sT:42483, sP:3.5142, sB:0.7963, nT:130495, nP:4.1294, nB:0.8804 , T2:3444,6903,4887,6469,1604,4833,1515,3327,6695,7581,7789,3265,6693,1588,1977,3220,6697,1077,4677,621,5303,7337,1973,3321,1993,6467,8144,472,7922,8130,  #End#
LowScoreRank2 , T0:2695 , T1:
Cond:3444, Score:4191.821, sT:43431, sP:3.4855, sB:0.7669, nT:133054, nP:4.0956, nB:0.88
Cond:472, Score:4187.765, sT:44217, sP:3.5089, sB:0.7573, nT:136533, nP:4.1184, nB:0.8666
Cond:1604, Score:4184.963, sT:43020, sP:3.5165, sB:0.7884, nT:132695, nP:4.1434, nB:0.8746
Cond:6903, Score:4183.197, sT:42531, sP:3.5163, sB:0.7979, nT:130685, nP:4.1317, nB:0.8803
Cond:4887, Score:4183.009, sT:42505, sP:3.5154, sB:0.7979, nT:130615, nP:4.1309, nB:0.8808
Cond:3327, Score:4182.578, sT:43005, sP:3.5218, sB:0.7906, nT:132244, nP:4.1327, nB:0.8726
Cond:6469, Score:4182.23, sT:42565, sP:3.5157, sB:0.7969, nT:130828, nP:4.1322, nB:0.8796
Cond:4833, Score:4181.77, sT:42517, sP:3.5136, sB:0.7965, nT:130588, nP:4.1289, nB:0.8812
Cond:1515, Score:4179.869, sT:42478, sP:3.5166, sB:0.7985, nT:130528, nP:4.1326, nB:0.8802
Cond:7581, Score:4179.462, sT:42574, sP:3.5164, sB:0.7955, nT:130728, nP:4.1321, nB:0.8808
Cond:6695, Score:4179.208, sT:42492, sP:3.5148, sB:0.797, nT:130500, nP:4.1307, nB:0.881
Cond:3220, Score:4179.015, sT:42615, sP:3.5111, sB:0.7911, nT:130818, nP:4.1245, nB:0.8825
Cond:1977, Score:4178.933, sT:42550, sP:3.5155, sB:0.7929, nT:130843, nP:4.1322, nB:0.8839
Cond:7672, Score:4178.848, sT:43219, sP:3.4999, sB:0.7793, nT:133367, nP:4.1179, nB:0.8672
Cond:7789, Score:4178.554, sT:42622, sP:3.5208, sB:0.7963, nT:130575, nP:4.132, nB:0.8802
Cond:3164, Score:4178.439, sT:43976, sP:3.5095, sB:0.7594, nT:137223, nP:4.1378, nB:0.8644
Cond:1588, Score:4178.365, sT:42567, sP:3.5153, sB:0.7963, nT:130854, nP:4.1333, nB:0.8785
Cond:1077, Score:4178.22, sT:42532, sP:3.5145, sB:0.7952, nT:130637, nP:4.1296, nB:0.8811
Cond:6697, Score:4178.078, sT:42512, sP:3.5155, sB:0.7962, nT:130627, nP:4.1312, nB:0.8807
Cond:3265, Score:4178.071, sT:42482, sP:3.5145, sB:0.7968, nT:130484, nP:4.1298, nB:0.8808
Cond:621, Score:4178.024, sT:42625, sP:3.5218, sB:0.7957, nT:130832, nP:4.136, nB:0.8803
Cond:6693, Score:4178.023, sT:42477, sP:3.5155, sB:0.7969, nT:130475, nP:4.1315, nB:0.8814
Cond:4677, Score:4177.867, sT:42492, sP:3.5147, sB:0.7963, nT:130528, nP:4.1309, nB:0.8812
Cond:1973, Score:4177.848, sT:42577, sP:3.5137, sB:0.7938, nT:130836, nP:4.1294, nB:0.8805
Cond:7337, Score:4177.666, sT:42538, sP:3.5157, sB:0.7955, nT:130683, nP:4.1315, nB:0.8807
Cond:5303, Score:4177.404, sT:42513, sP:3.5148, sB:0.7967, nT:130643, nP:4.1309, nB:0.8793
Cond:3321, Score:4177.193, sT:42512, sP:3.5153, sB:0.7964, nT:130653, nP:4.1318, nB:0.8799
Cond:7922, Score:4177.188, sT:42570, sP:3.5112, sB:0.7933, nT:130773, nP:4.1256, nB:0.88
Cond:6467, Score:4176.994, sT:42515, sP:3.5164, sB:0.7966, nT:130640, nP:4.1327, nB:0.88
Cond:1993, Score:4176.801, sT:42459, sP:3.5157, sB:0.7974, nT:130431, nP:4.1309, nB:0.8807 , T2:3444,472,1604,6903,4887,3327,6469,4833,1515,7581,6695,3220,1977,7672,7789,3164,1588,1077,6697,3265,621,6693,4677,1973,7337,5303,3321,7922,6467,1993,  #End#
LowScoreRank3 , T0:2695 , T1:
Cond:472, Score:7334.643, sT:44217, sP:3.5089, sB:0.7573, nT:136533, nP:4.1184, nB:0.8666
Cond:3444, Score:7333.362, sT:43431, sP:3.4855, sB:0.7669, nT:133054, nP:4.0956, nB:0.88
Cond:3164, Score:7318.033, sT:43976, sP:3.5095, sB:0.7594, nT:137223, nP:4.1378, nB:0.8644
Cond:1604, Score:7315.876, sT:43020, sP:3.5165, sB:0.7884, nT:132695, nP:4.1434, nB:0.8746
Cond:3327, Score:7310.66, sT:43005, sP:3.5218, sB:0.7906, nT:132244, nP:4.1327, nB:0.8726
Cond:7672, Score:7307.454, sT:43219, sP:3.4999, sB:0.7793, nT:133367, nP:4.1179, nB:0.8672
Cond:6903, Score:7307.344, sT:42531, sP:3.5163, sB:0.7979, nT:130685, nP:4.1317, nB:0.8803
Cond:4887, Score:7306.864, sT:42505, sP:3.5154, sB:0.7979, nT:130615, nP:4.1309, nB:0.8808
Cond:6469, Score:7306.042, sT:42565, sP:3.5157, sB:0.7969, nT:130828, nP:4.1322, nB:0.8796
Cond:4833, Score:7304.905, sT:42517, sP:3.5136, sB:0.7965, nT:130588, nP:4.1289, nB:0.8812
Cond:1862, Score:7304.419, sT:43781, sP:3.5393, sB:0.7782, nT:133586, nP:4.1481, nB:0.8701
Cond:5904, Score:7303.5, sT:43514, sP:3.4803, sB:0.7629, nT:134056, nP:4.0865, nB:0.8669
Cond:3220, Score:7301.669, sT:42615, sP:3.5111, sB:0.7911, nT:130818, nP:4.1245, nB:0.8825
Cond:7581, Score:7301.373, sT:42574, sP:3.5164, sB:0.7955, nT:130728, nP:4.1321, nB:0.8808
Cond:1977, Score:7301.268, sT:42550, sP:3.5155, sB:0.7929, nT:130843, nP:4.1322, nB:0.8839
Cond:1515, Score:7300.986, sT:42478, sP:3.5166, sB:0.7985, nT:130528, nP:4.1326, nB:0.8802
Cond:1961, Score:7300.678, sT:43048, sP:3.5268, sB:0.7773, nT:133104, nP:4.162, nB:0.8886
Cond:6695, Score:7300.096, sT:42492, sP:3.5148, sB:0.797, nT:130500, nP:4.1307, nB:0.881
Cond:7450, Score:7299.617, sT:43723, sP:3.5168, sB:0.7698, nT:136327, nP:4.1432, nB:0.859
Cond:7789, Score:7299.563, sT:42622, sP:3.5208, sB:0.7963, nT:130575, nP:4.132, nB:0.8802
Cond:1061, Score:7299.268, sT:43032, sP:3.5063, sB:0.7829, nT:132306, nP:4.1253, nB:0.8736
Cond:1588, Score:7299.26, sT:42567, sP:3.5153, sB:0.7963, nT:130854, nP:4.1333, nB:0.8785
Cond:621, Score:7299.161, sT:42625, sP:3.5218, sB:0.7957, nT:130832, nP:4.136, nB:0.8803
Cond:1077, Score:7298.997, sT:42532, sP:3.5145, sB:0.7952, nT:130637, nP:4.1296, nB:0.8811
Cond:1973, Score:7298.944, sT:42577, sP:3.5137, sB:0.7938, nT:130836, nP:4.1294, nB:0.8805
Cond:6697, Score:7298.494, sT:42512, sP:3.5155, sB:0.7962, nT:130627, nP:4.1312, nB:0.8807
Cond:3265, Score:7298.067, sT:42482, sP:3.5145, sB:0.7968, nT:130484, nP:4.1298, nB:0.8808
Cond:7337, Score:7298.037, sT:42538, sP:3.5157, sB:0.7955, nT:130683, nP:4.1315, nB:0.8807
Cond:6693, Score:7297.995, sT:42477, sP:3.5155, sB:0.7969, nT:130475, nP:4.1315, nB:0.8814
Cond:4677, Score:7297.913, sT:42492, sP:3.5147, sB:0.7963, nT:130528, nP:4.1309, nB:0.8812 , T2:472,3444,3164,1604,3327,7672,6903,4887,6469,4833,1862,5904,3220,7581,1977,1515,1961,6695,7450,7789,1061,1588,621,1077,1973,6697,3265,7337,6693,4677,  #End#
LowScoreRank0 , T0:291 , T1:
Cond:1973, Score:1376.568, sT:42880, sP:3.4964, sB:0.8095, nT:129127, nP:4.1123, nB:0.8556
Cond:4887, Score:1376.515, sT:42798, sP:3.4981, sB:0.8119, nT:128860, nP:4.1145, nB:0.8558
Cond:6903, Score:1376.513, sT:42824, sP:3.4991, sB:0.8119, nT:128930, nP:4.1152, nB:0.8553
Cond:4833, Score:1376.15, sT:42815, sP:3.4961, sB:0.8104, nT:128851, nP:4.112, nB:0.8562
Cond:1077, Score:1376.026, sT:42831, sP:3.497, sB:0.8101, nT:128928, nP:4.1124, nB:0.8561
Cond:6469, Score:1375.63, sT:42859, sP:3.4984, sB:0.8104, nT:129075, nP:4.1158, nB:0.8547
Cond:1515, Score:1375.616, sT:42771, sP:3.4994, sB:0.8125, nT:128773, nP:4.1162, nB:0.8552
Cond:6695, Score:1375.404, sT:42785, sP:3.4976, sB:0.8111, nT:128745, nP:4.1142, nB:0.856
Cond:6693, Score:1374.938, sT:42770, sP:3.4982, sB:0.8109, nT:128720, nP:4.115, nB:0.8564
Cond:3265, Score:1374.937, sT:42779, sP:3.497, sB:0.8106, nT:128744, nP:4.113, nB:0.8558
Cond:7581, Score:1374.861, sT:42868, sP:3.4992, sB:0.8093, nT:128974, nP:4.1157, nB:0.8558
Cond:7789, Score:1374.81, sT:42918, sP:3.5035, sB:0.8102, nT:128823, nP:4.1155, nB:0.8552
Cond:6697, Score:1374.806, sT:42806, sP:3.4983, sB:0.8102, nT:128875, nP:4.1147, nB:0.8557
Cond:5303, Score:1374.772, sT:42806, sP:3.4975, sB:0.8108, nT:128888, nP:4.1144, nB:0.8543
Cond:621, Score:1374.761, sT:42918, sP:3.5046, sB:0.81, nT:129077, nP:4.1196, nB:0.8553
Cond:4677, Score:1374.677, sT:42784, sP:3.4974, sB:0.8101, nT:128775, nP:4.1143, nB:0.8563
Cond:1977, Score:1374.671, sT:42843, sP:3.4983, sB:0.8069, nT:129088, nP:4.1157, nB:0.8591
Cond:8130, Score:1374.653, sT:42788, sP:3.4964, sB:0.8105, nT:128786, nP:4.1119, nB:0.8547
Cond:1993, Score:1374.624, sT:42752, sP:3.4984, sB:0.8114, nT:128676, nP:4.1145, nB:0.8557
Cond:3321, Score:1374.615, sT:42805, sP:3.498, sB:0.8104, nT:128898, nP:4.1153, nB:0.855
Cond:8144, Score:1374.553, sT:42795, sP:3.4966, sB:0.8104, nT:128804, nP:4.1118, nB:0.8545
Cond:3160, Score:1374.529, sT:42850, sP:3.4979, sB:0.8098, nT:128989, nP:4.1152, nB:0.8543
Cond:7545, Score:1374.442, sT:42779, sP:3.4987, sB:0.8103, nT:128768, nP:4.115, nB:0.8563
Cond:7337, Score:1374.326, sT:42831, sP:3.4985, sB:0.8093, nT:128928, nP:4.1151, nB:0.8557
Cond:6467, Score:1374.292, sT:42808, sP:3.4991, sB:0.8104, nT:128886, nP:4.1162, nB:0.855
Cond:3327, Score:1374.211, sT:43300, sP:3.5047, sB:0.8042, nT:130495, nP:4.1164, nB:0.8476
Cond:3299, Score:1374.197, sT:42755, sP:3.4984, sB:0.8107, nT:128690, nP:4.1144, nB:0.856
Cond:1457, Score:1374.018, sT:42759, sP:3.4979, sB:0.8109, nT:128671, nP:4.1142, nB:0.8552
Cond:4679, Score:1373.927, sT:42800, sP:3.4982, sB:0.8093, nT:128870, nP:4.1147, nB:0.8559
Cond:5166, Score:1373.88, sT:42778, sP:3.4983, sB:0.8102, nT:128804, nP:4.1145, nB:0.8552 , T2:1973,4887,6903,4833,1077,6469,1515,6695,6693,3265,7581,7789,6697,5303,621,4677,1977,8130,1993,3321,8144,3160,7545,7337,6467,3327,3299,1457,4679,5166,  #End#
LowScoreRank1 , T0:291 , T1:
Cond:1973, Score:2399.008, sT:42880, sP:3.4964, sB:0.8095, nT:129127, nP:4.1123, nB:0.8556
Cond:6903, Score:2398.634, sT:42824, sP:3.4991, sB:0.8119, nT:128930, nP:4.1152, nB:0.8553
Cond:4887, Score:2398.586, sT:42798, sP:3.4981, sB:0.8119, nT:128860, nP:4.1145, nB:0.8558
Cond:4833, Score:2398.04, sT:42815, sP:3.4961, sB:0.8104, nT:128851, nP:4.112, nB:0.8562
Cond:1077, Score:2397.896, sT:42831, sP:3.497, sB:0.8101, nT:128928, nP:4.1124, nB:0.8561
Cond:6469, Score:2397.243, sT:42859, sP:3.4984, sB:0.8104, nT:129075, nP:4.1158, nB:0.8547
Cond:1515, Score:2396.892, sT:42771, sP:3.4994, sB:0.8125, nT:128773, nP:4.1162, nB:0.8552
Cond:6695, Score:2396.608, sT:42785, sP:3.4976, sB:0.8111, nT:128745, nP:4.1142, nB:0.856
Cond:3327, Score:2396.071, sT:43300, sP:3.5047, sB:0.8042, nT:130495, nP:4.1164, nB:0.8476
Cond:7581, Score:2395.946, sT:42868, sP:3.4992, sB:0.8093, nT:128974, nP:4.1157, nB:0.8558
Cond:1977, Score:2395.862, sT:42843, sP:3.4983, sB:0.8069, nT:129088, nP:4.1157, nB:0.8591
Cond:621, Score:2395.855, sT:42918, sP:3.5046, sB:0.81, nT:129077, nP:4.1196, nB:0.8553
Cond:7789, Score:2395.805, sT:42918, sP:3.5035, sB:0.8102, nT:128823, nP:4.1155, nB:0.8552
Cond:3265, Score:2395.803, sT:42779, sP:3.497, sB:0.8106, nT:128744, nP:4.113, nB:0.8558
Cond:6693, Score:2395.787, sT:42770, sP:3.4982, sB:0.8109, nT:128720, nP:4.115, nB:0.8564
Cond:6697, Score:2395.689, sT:42806, sP:3.4983, sB:0.8102, nT:128875, nP:4.1147, nB:0.8557
Cond:5303, Score:2395.557, sT:42806, sP:3.4975, sB:0.8108, nT:128888, nP:4.1144, nB:0.8543
Cond:4677, Score:2395.405, sT:42784, sP:3.4974, sB:0.8101, nT:128775, nP:4.1143, nB:0.8563
Cond:3321, Score:2395.327, sT:42805, sP:3.498, sB:0.8104, nT:128898, nP:4.1153, nB:0.855
Cond:8130, Score:2395.307, sT:42788, sP:3.4964, sB:0.8105, nT:128786, nP:4.1119, nB:0.8547
Cond:3160, Score:2395.276, sT:42850, sP:3.4979, sB:0.8098, nT:128989, nP:4.1152, nB:0.8543
Cond:1993, Score:2395.155, sT:42752, sP:3.4984, sB:0.8114, nT:128676, nP:4.1145, nB:0.8557
Cond:8144, Score:2395.151, sT:42795, sP:3.4966, sB:0.8104, nT:128804, nP:4.1118, nB:0.8545
Cond:7545, Score:2394.982, sT:42779, sP:3.4987, sB:0.8103, nT:128768, nP:4.115, nB:0.8563
Cond:7337, Score:2394.943, sT:42831, sP:3.4985, sB:0.8093, nT:128928, nP:4.1151, nB:0.8557
Cond:6467, Score:2394.762, sT:42808, sP:3.4991, sB:0.8104, nT:128886, nP:4.1162, nB:0.855
Cond:7095, Score:2394.735, sT:43069, sP:3.5007, sB:0.8061, nT:129953, nP:4.1189, nB:0.8509
Cond:3093, Score:2394.557, sT:42964, sP:3.4953, sB:0.8065, nT:129477, nP:4.1118, nB:0.8521
Cond:3299, Score:2394.46, sT:42755, sP:3.4984, sB:0.8107, nT:128690, nP:4.1144, nB:0.856
Cond:4679, Score:2394.188, sT:42800, sP:3.4982, sB:0.8093, nT:128870, nP:4.1147, nB:0.8559 , T2:1973,6903,4887,4833,1077,6469,1515,6695,3327,7581,1977,621,7789,3265,6693,6697,5303,4677,3321,8130,3160,1993,8144,7545,7337,6467,7095,3093,3299,4679,  #End#
LowScoreRank2 , T0:291 , T1:
Cond:1973, Score:4183.93, sT:42880, sP:3.4964, sB:0.8095, nT:129127, nP:4.1123, nB:0.8556
Cond:6903, Score:4182.793, sT:42824, sP:3.4991, sB:0.8119, nT:128930, nP:4.1152, nB:0.8553
Cond:4887, Score:4182.617, sT:42798, sP:3.4981, sB:0.8119, nT:128860, nP:4.1145, nB:0.8558
Cond:4833, Score:4181.819, sT:42815, sP:3.4961, sB:0.8104, nT:128851, nP:4.112, nB:0.8562
Cond:1077, Score:4181.698, sT:42831, sP:3.497, sB:0.8101, nT:128928, nP:4.1124, nB:0.8561
Cond:3327, Score:4180.855, sT:43300, sP:3.5047, sB:0.8042, nT:130495, nP:4.1164, nB:0.8476
Cond:6469, Score:4180.625, sT:42859, sP:3.4984, sB:0.8104, nT:129075, nP:4.1158, nB:0.8547
Cond:1515, Score:4179.443, sT:42771, sP:3.4994, sB:0.8125, nT:128773, nP:4.1162, nB:0.8552
Cond:6695, Score:4179.092, sT:42785, sP:3.4976, sB:0.8111, nT:128745, nP:4.1142, nB:0.856
Cond:1977, Score:4178.731, sT:42843, sP:3.4983, sB:0.8069, nT:129088, nP:4.1157, nB:0.8591
Cond:7581, Score:4178.436, sT:42868, sP:3.4992, sB:0.8093, nT:128974, nP:4.1157, nB:0.8558
Cond:621, Score:4178.416, sT:42918, sP:3.5046, sB:0.81, nT:129077, nP:4.1196, nB:0.8553
Cond:7789, Score:4178.082, sT:42918, sP:3.5035, sB:0.8102, nT:128823, nP:4.1155, nB:0.8552
Cond:6697, Score:4177.706, sT:42806, sP:3.4983, sB:0.8102, nT:128875, nP:4.1147, nB:0.8557
Cond:3265, Score:4177.705, sT:42779, sP:3.497, sB:0.8106, nT:128744, nP:4.113, nB:0.8558
Cond:6693, Score:4177.647, sT:42770, sP:3.4982, sB:0.8109, nT:128720, nP:4.115, nB:0.8564
Cond:7095, Score:4177.566, sT:43069, sP:3.5007, sB:0.8061, nT:129953, nP:4.1189, nB:0.8509
Cond:5303, Score:4177.349, sT:42806, sP:3.4975, sB:0.8108, nT:128888, nP:4.1144, nB:0.8543
Cond:4677, Score:4177.107, sT:42784, sP:3.4974, sB:0.8101, nT:128775, nP:4.1143, nB:0.8563
Cond:3160, Score:4177.106, sT:42850, sP:3.4979, sB:0.8098, nT:128989, nP:4.1152, nB:0.8543
Cond:3321, Score:4177.026, sT:42805, sP:3.498, sB:0.8104, nT:128898, nP:4.1153, nB:0.855
Cond:8130, Score:4176.839, sT:42788, sP:3.4964, sB:0.8105, nT:128786, nP:4.1119, nB:0.8547
Cond:3093, Score:4176.645, sT:42964, sP:3.4953, sB:0.8065, nT:129477, nP:4.1118, nB:0.8521
Cond:8144, Score:4176.595, sT:42795, sP:3.4966, sB:0.8104, nT:128804, nP:4.1118, nB:0.8545
Cond:7337, Score:4176.565, sT:42831, sP:3.4985, sB:0.8093, nT:128928, nP:4.1151, nB:0.8557
Cond:1993, Score:4176.396, sT:42752, sP:3.4984, sB:0.8114, nT:128676, nP:4.1145, nB:0.8557
Cond:7545, Score:4176.35, sT:42779, sP:3.4987, sB:0.8103, nT:128768, nP:4.115, nB:0.8563
Cond:6467, Score:4176.038, sT:42808, sP:3.4991, sB:0.8104, nT:128886, nP:4.1162, nB:0.855
Cond:3119, Score:4175.787, sT:42952, sP:3.4998, sB:0.8075, nT:129386, nP:4.1134, nB:0.8526
Cond:3299, Score:4175.273, sT:42755, sP:3.4984, sB:0.8107, nT:128690, nP:4.1144, nB:0.856 , T2:1973,6903,4887,4833,1077,3327,6469,1515,6695,1977,7581,621,7789,6697,3265,6693,7095,5303,4677,3160,3321,8130,3093,8144,7337,1993,7545,6467,3119,3299,  #End#
LowScoreRank3 , T0:291 , T1:
Cond:1973, Score:7302.289, sT:42880, sP:3.4964, sB:0.8095, nT:129127, nP:4.1123, nB:0.8556
Cond:3164, Score:7300.594, sT:44306, sP:3.4909, sB:0.7711, nT:135613, nP:4.12, nB:0.8384
Cond:3327, Score:7300.502, sT:43300, sP:3.5047, sB:0.8042, nT:130495, nP:4.1164, nB:0.8476
Cond:6903, Score:7299.45, sT:42824, sP:3.4991, sB:0.8119, nT:128930, nP:4.1152, nB:0.8553
Cond:4887, Score:7298.986, sT:42798, sP:3.4981, sB:0.8119, nT:128860, nP:4.1145, nB:0.8558
Cond:1077, Score:7297.876, sT:42831, sP:3.497, sB:0.8101, nT:128928, nP:4.1124, nB:0.8561
Cond:4833, Score:7297.859, sT:42815, sP:3.4961, sB:0.8104, nT:128851, nP:4.112, nB:0.8562
Cond:6469, Score:7296.123, sT:42859, sP:3.4984, sB:0.8104, nT:129075, nP:4.1158, nB:0.8547
Cond:1977, Score:7293.727, sT:42843, sP:3.4983, sB:0.8069, nT:129088, nP:4.1157, nB:0.8591
Cond:1961, Score:7293.126, sT:43342, sP:3.5097, sB:0.7913, nT:131349, nP:4.1463, nB:0.8642
Cond:7095, Score:7293.098, sT:43069, sP:3.5007, sB:0.8061, nT:129953, nP:4.1189, nB:0.8509
Cond:1515, Score:7293.058, sT:42771, sP:3.4994, sB:0.8125, nT:128773, nP:4.1162, nB:0.8552
Cond:6695, Score:7292.695, sT:42785, sP:3.4976, sB:0.8111, nT:128745, nP:4.1142, nB:0.856
Cond:621, Score:7292.623, sT:42918, sP:3.5046, sB:0.81, nT:129077, nP:4.1196, nB:0.8553
Cond:7581, Score:7292.419, sT:42868, sP:3.4992, sB:0.8093, nT:128974, nP:4.1157, nB:0.8558
Cond:7789, Score:7291.59, sT:42918, sP:3.5035, sB:0.8102, nT:128823, nP:4.1155, nB:0.8552
Cond:6697, Score:7290.662, sT:42806, sP:3.4983, sB:0.8102, nT:128875, nP:4.1147, nB:0.8557
Cond:3093, Score:7290.417, sT:42964, sP:3.4953, sB:0.8065, nT:129477, nP:4.1118, nB:0.8521
Cond:7321, Score:7290.358, sT:43331, sP:3.5033, sB:0.8016, nT:130956, nP:4.1246, nB:0.8462
Cond:3265, Score:7290.304, sT:42779, sP:3.497, sB:0.8106, nT:128744, nP:4.113, nB:0.8558
Cond:6693, Score:7290.15, sT:42770, sP:3.4982, sB:0.8109, nT:128720, nP:4.115, nB:0.8564
Cond:3160, Score:7289.821, sT:42850, sP:3.4979, sB:0.8098, nT:128989, nP:4.1152, nB:0.8543
Cond:5303, Score:7289.817, sT:42806, sP:3.4975, sB:0.8108, nT:128888, nP:4.1144, nB:0.8543
Cond:4677, Score:7289.435, sT:42784, sP:3.4974, sB:0.8101, nT:128775, nP:4.1143, nB:0.8563
Cond:3321, Score:7289.39, sT:42805, sP:3.498, sB:0.8104, nT:128898, nP:4.1153, nB:0.855
Cond:7337, Score:7288.947, sT:42831, sP:3.4985, sB:0.8093, nT:128928, nP:4.1151, nB:0.8557
Cond:8130, Score:7288.793, sT:42788, sP:3.4964, sB:0.8105, nT:128786, nP:4.1119, nB:0.8547
Cond:3119, Score:7288.723, sT:42952, sP:3.4998, sB:0.8075, nT:129386, nP:4.1134, nB:0.8526
Cond:8144, Score:7288.417, sT:42795, sP:3.4966, sB:0.8104, nT:128804, nP:4.1118, nB:0.8545
Cond:7545, Score:7288.076, sT:42779, sP:3.4987, sB:0.8103, nT:128768, nP:4.115, nB:0.8563 , T2:1973,3164,3327,6903,4887,1077,4833,6469,1977,1961,7095,1515,6695,621,7581,7789,6697,3093,7321,3265,6693,3160,5303,4677,3321,7337,8130,3119,8144,7545,  #End#
End , T0:00:48:05.0614313  #End#




////////////////////////////////////////////////////////////////////////////////////////////



LowScoreRank0 , T0:5041 , T1:
Cond:6130, Score:1389.13, sT:42563, sP:3.4374, sB:0.7811, nT:128183, nP:3.9782, nB:0.8881
Cond:6385, Score:1387.083, sT:37845, sP:3.2701, sB:0.8154, nT:114139, nP:3.8115, nB:0.9325
Cond:65, Score:1379.22, sT:42525, sP:3.4974, sB:0.8146, nT:128776, nP:4.1251, nB:0.8642
Cond:6128, Score:1379.067, sT:38912, sP:3.3395, sB:0.811, nT:112411, nP:3.761, nB:0.9222
Cond:1395, Score:1378.701, sT:42881, sP:3.4944, sB:0.796, nT:130246, nP:4.1, nB:0.8721
Cond:291, Score:1372.405, sT:42747, sP:3.4983, sB:0.8097, nT:128646, nP:4.1146, nB:0.8552
Cond:110, Score:1370.844, sT:41936, sP:3.4626, sB:0.792, nT:127897, nP:4.0855, nB:0.8868
Cond:8321, Score:1370.611, sT:40758, sP:3.501, sB:0.8096, nT:123611, nP:4.1113, nB:0.926
Cond:94, Score:1370.464, sT:40073, sP:3.4212, sB:0.8147, nT:121821, nP:4.0585, nB:0.9079
Cond:1575, Score:1370.368, sT:42806, sP:3.5014, sB:0.7887, nT:131234, nP:4.1174, nB:0.873
Cond:750, Score:1370.15, sT:38501, sP:3.3852, sB:0.831, nT:118380, nP:4.0302, nB:0.9229
Cond:2695, Score:1369.994, sT:42454, sP:3.5155, sB:0.7957, nT:130401, nP:4.1311, nB:0.8802
Cond:275, Score:1369.591, sT:43217, sP:3.4844, sB:0.78, nT:131588, nP:4.1018, nB:0.8668
Cond:7822, Score:1367.609, sT:42762, sP:3.4859, sB:0.7803, nT:130492, nP:4.1023, nB:0.8792
Cond:2485, Score:1366.659, sT:43016, sP:3.4889, sB:0.7792, nT:131652, nP:4.1041, nB:0.8694
Cond:7598, Score:1366.476, sT:42106, sP:3.4917, sB:0.7909, nT:128542, nP:4.1075, nB:0.8878
Cond:5358, Score:1365.778, sT:42943, sP:3.4837, sB:0.7773, nT:131168, nP:4.1005, nB:0.8728
Cond:8046, Score:1364.977, sT:43068, sP:3.4824, sB:0.7744, nT:131585, nP:4.0988, nB:0.8707
Cond:677, Score:1364.856, sT:43243, sP:3.4839, sB:0.7746, nT:132203, nP:4.1018, nB:0.8644
Cond:5134, Score:1364.674, sT:42551, sP:3.4885, sB:0.7825, nT:129864, nP:4.1048, nB:0.8802
Cond:8368, Score:1364.599, sT:42812, sP:3.4653, sB:0.7728, nT:130680, nP:4.0741, nB:0.8728
Cond:7150, Score:1364.084, sT:41231, sP:3.5085, sB:0.8054, nT:126368, nP:4.1256, nB:0.9011
Cond:6122, Score:1364.08, sT:41963, sP:3.4522, sB:0.7867, nT:128329, nP:4.084, nB:0.8783
Cond:5, Score:1364.07, sT:43275, sP:3.4825, sB:0.7729, nT:132275, nP:4.1005, nB:0.8642
Cond:2693, Score:1363.965, sT:42989, sP:3.4931, sB:0.7794, nT:131614, nP:4.1101, nB:0.8681
Cond:7838, Score:1363.909, sT:43147, sP:3.4837, sB:0.7727, nT:131864, nP:4.101, nB:0.8695
Cond:49, Score:1363.828, sT:43040, sP:3.4888, sB:0.7838, nT:130999, nP:4.113, nB:0.862
Cond:768, Score:1363.816, sT:40400, sP:3.5151, sB:0.8629, nT:114304, nP:4.1022, nB:0.8924
Cond:7390, Score:1363.328, sT:42650, sP:3.4922, sB:0.7817, nT:130381, nP:4.111, nB:0.8769
Cond:1646, Score:1362.716, sT:39299, sP:3.4103, sB:0.8098, nT:121338, nP:4.0439, nB:0.9185 , T2:6130,6385,65,6128,1395,291,110,8321,94,1575,750,2695,275,7822,2485,7598,5358,8046,677,5134,8368,7150,6122,5,2693,7838,49,768,7390,1646,  #End#
LowScoreRank1 , T0:5041 , T1:
Cond:6130, Score:2422.866, sT:42563, sP:3.4374, sB:0.7811, nT:128183, nP:3.9782, nB:0.8881
Cond:6385, Score:2405.298, sT:37845, sP:3.2701, sB:0.8154, nT:114139, nP:3.8115, nB:0.9325
Cond:1395, Score:2404.587, sT:42881, sP:3.4944, sB:0.796, nT:130246, nP:4.1, nB:0.8721
Cond:65, Score:2403.077, sT:42525, sP:3.4974, sB:0.8146, nT:128776, nP:4.1251, nB:0.8642
Cond:6128, Score:2392.254, sT:38912, sP:3.3395, sB:0.811, nT:112411, nP:3.761, nB:0.9222
Cond:291, Score:2391.312, sT:42747, sP:3.4983, sB:0.8097, nT:128646, nP:4.1146, nB:0.8552
Cond:1575, Score:2390.802, sT:42806, sP:3.5014, sB:0.7887, nT:131234, nP:4.1174, nB:0.873
Cond:275, Score:2390.18, sT:43217, sP:3.4844, sB:0.78, nT:131588, nP:4.1018, nB:0.8668
Cond:2695, Score:2389.3, sT:42454, sP:3.5155, sB:0.7957, nT:130401, nP:4.1311, nB:0.8802
Cond:110, Score:2389.08, sT:41936, sP:3.4626, sB:0.792, nT:127897, nP:4.0855, nB:0.8868
Cond:7822, Score:2386.08, sT:42762, sP:3.4859, sB:0.7803, nT:130492, nP:4.1023, nB:0.8792
Cond:8321, Score:2385.66, sT:40758, sP:3.501, sB:0.8096, nT:123611, nP:4.1113, nB:0.926
Cond:2485, Score:2385.041, sT:43016, sP:3.4889, sB:0.7792, nT:131652, nP:4.1041, nB:0.8694
Cond:5358, Score:2383.343, sT:42943, sP:3.4837, sB:0.7773, nT:131168, nP:4.1005, nB:0.8728
Cond:94, Score:2382.467, sT:40073, sP:3.4212, sB:0.8147, nT:121821, nP:4.0585, nB:0.9079
Cond:677, Score:2382.422, sT:43243, sP:3.4839, sB:0.7746, nT:132203, nP:4.1018, nB:0.8644
Cond:8046, Score:2382.357, sT:43068, sP:3.4824, sB:0.7744, nT:131585, nP:4.0988, nB:0.8707
Cond:7598, Score:2382.179, sT:42106, sP:3.4917, sB:0.7909, nT:128542, nP:4.1075, nB:0.8878
Cond:5, Score:2381.186, sT:43275, sP:3.4825, sB:0.7729, nT:132275, nP:4.1005, nB:0.8642
Cond:8368, Score:2381.102, sT:42812, sP:3.4653, sB:0.7728, nT:130680, nP:4.0741, nB:0.8728
Cond:7838, Score:2380.754, sT:43147, sP:3.4837, sB:0.7727, nT:131864, nP:4.101, nB:0.8695
Cond:5134, Score:2380.329, sT:42551, sP:3.4885, sB:0.7825, nT:129864, nP:4.1048, nB:0.8802
Cond:2693, Score:2380.225, sT:42989, sP:3.4931, sB:0.7794, nT:131614, nP:4.1101, nB:0.8681
Cond:49, Score:2379.226, sT:43040, sP:3.4888, sB:0.7838, nT:130999, nP:4.113, nB:0.862
Cond:2277, Score:2378.356, sT:43083, sP:3.4886, sB:0.7749, nT:131888, nP:4.1032, nB:0.8672
Cond:7390, Score:2378.278, sT:42650, sP:3.4922, sB:0.7817, nT:130381, nP:4.111, nB:0.8769
Cond:7, Score:2377.91, sT:43257, sP:3.4829, sB:0.7714, nT:132219, nP:4.1012, nB:0.8646
Cond:7614, Score:2377.818, sT:42969, sP:3.4859, sB:0.7746, nT:131279, nP:4.1036, nB:0.8722
Cond:750, Score:2377.807, sT:38501, sP:3.3852, sB:0.831, nT:118380, nP:4.0302, nB:0.9229
Cond:5150, Score:2377.6, sT:43083, sP:3.4837, sB:0.7727, nT:131617, nP:4.1021, nB:0.8697 , T2:6130,6385,1395,65,6128,291,1575,275,2695,110,7822,8321,2485,5358,94,677,8046,7598,5,8368,7838,5134,2693,49,2277,7390,7,7614,750,5150,  #End#
LowScoreRank2 , T0:5041 , T1:
Cond:6130, Score:4229.016, sT:42563, sP:3.4374, sB:0.7811, nT:128183, nP:3.9782, nB:0.8881
Cond:1395, Score:4196.984, sT:42881, sP:3.4944, sB:0.796, nT:130246, nP:4.1, nB:0.8721
Cond:65, Score:4190.098, sT:42525, sP:3.4974, sB:0.8146, nT:128776, nP:4.1251, nB:0.8642
Cond:275, Score:4174.445, sT:43217, sP:3.4844, sB:0.78, nT:131588, nP:4.1018, nB:0.8668
Cond:1575, Score:4174.291, sT:42806, sP:3.5014, sB:0.7887, nT:131234, nP:4.1174, nB:0.873
Cond:6385, Score:4174.071, sT:37845, sP:3.2701, sB:0.8154, nT:114139, nP:3.8115, nB:0.9325
Cond:2695, Score:4170.194, sT:42454, sP:3.5155, sB:0.7957, nT:130401, nP:4.1311, nB:0.8802
Cond:291, Score:4169.736, sT:42747, sP:3.4983, sB:0.8097, nT:128646, nP:4.1146, nB:0.8552
Cond:110, Score:4166.805, sT:41936, sP:3.4626, sB:0.792, nT:127897, nP:4.0855, nB:0.8868
Cond:7822, Score:4166.185, sT:42762, sP:3.4859, sB:0.7803, nT:130492, nP:4.1023, nB:0.8792
Cond:2485, Score:4165.467, sT:43016, sP:3.4889, sB:0.7792, nT:131652, nP:4.1041, nB:0.8694
Cond:5358, Score:4162.211, sT:42943, sP:3.4837, sB:0.7773, nT:131168, nP:4.1005, nB:0.8728
Cond:677, Score:4161.807, sT:43243, sP:3.4839, sB:0.7746, nT:132203, nP:4.1018, nB:0.8644
Cond:8046, Score:4161.208, sT:43068, sP:3.4824, sB:0.7744, nT:131585, nP:4.0988, nB:0.8707
Cond:5, Score:4159.883, sT:43275, sP:3.4825, sB:0.7729, nT:132275, nP:4.1005, nB:0.8642
Cond:7838, Score:4158.866, sT:43147, sP:3.4837, sB:0.7727, nT:131864, nP:4.101, nB:0.8695
Cond:8368, Score:4157.974, sT:42812, sP:3.4653, sB:0.7728, nT:130680, nP:4.0741, nB:0.8728
Cond:2693, Score:4156.854, sT:42989, sP:3.4931, sB:0.7794, nT:131614, nP:4.1101, nB:0.8681
Cond:7598, Score:4156.018, sT:42106, sP:3.4917, sB:0.7909, nT:128542, nP:4.1075, nB:0.8878
Cond:8321, Score:4155.57, sT:40758, sP:3.501, sB:0.8096, nT:123611, nP:4.1113, nB:0.926
Cond:5134, Score:4155.042, sT:42551, sP:3.4885, sB:0.7825, nT:129864, nP:4.1048, nB:0.8802
Cond:2277, Score:4154.324, sT:43083, sP:3.4886, sB:0.7749, nT:131888, nP:4.1032, nB:0.8672
Cond:7, Score:4154.207, sT:43257, sP:3.4829, sB:0.7714, nT:132219, nP:4.1012, nB:0.8646
Cond:49, Score:4153.743, sT:43040, sP:3.4888, sB:0.7838, nT:130999, nP:4.113, nB:0.862
Cond:3566, Score:4153.212, sT:43125, sP:3.4818, sB:0.7716, nT:131862, nP:4.0996, nB:0.8681
Cond:513, Score:4153.129, sT:43243, sP:3.4843, sB:0.773, nT:131994, nP:4.1042, nB:0.8643
Cond:5150, Score:4152.994, sT:43083, sP:3.4837, sB:0.7727, nT:131617, nP:4.1021, nB:0.8697
Cond:7614, Score:4152.871, sT:42969, sP:3.4859, sB:0.7746, nT:131279, nP:4.1036, nB:0.8722
Cond:6128, Score:4152.678, sT:38912, sP:3.3395, sB:0.811, nT:112411, nP:3.761, nB:0.9222
Cond:1573, Score:4152.468, sT:43136, sP:3.4872, sB:0.7752, nT:131959, nP:4.105, nB:0.8643 , T2:6130,1395,65,275,1575,6385,2695,291,110,7822,2485,5358,677,8046,5,7838,8368,2693,7598,8321,5134,2277,7,49,3566,513,5150,7614,6128,1573,  #End#
LowScoreRank3 , T0:5041 , T1:
Cond:6130, Score:7387.118, sT:42563, sP:3.4374, sB:0.7811, nT:128183, nP:3.9782, nB:0.8881
Cond:1395, Score:7330.999, sT:42881, sP:3.4944, sB:0.796, nT:130246, nP:4.1, nB:0.8721
Cond:65, Score:7311.492, sT:42525, sP:3.4974, sB:0.8146, nT:128776, nP:4.1251, nB:0.8642
Cond:275, Score:7296.221, sT:43217, sP:3.4844, sB:0.78, nT:131588, nP:4.1018, nB:0.8668
Cond:1575, Score:7293.854, sT:42806, sP:3.5014, sB:0.7887, nT:131234, nP:4.1174, nB:0.873
Cond:2695, Score:7284.14, sT:42454, sP:3.5155, sB:0.7957, nT:130401, nP:4.1311, nB:0.8802
Cond:2485, Score:7280.58, sT:43016, sP:3.4889, sB:0.7792, nT:131652, nP:4.1041, nB:0.8694
Cond:7822, Score:7279.896, sT:42762, sP:3.4859, sB:0.7803, nT:130492, nP:4.1023, nB:0.8792
Cond:291, Score:7276.159, sT:42747, sP:3.4983, sB:0.8097, nT:128646, nP:4.1146, nB:0.8552
Cond:677, Score:7275.768, sT:43243, sP:3.4839, sB:0.7746, nT:132203, nP:4.1018, nB:0.8644
Cond:5358, Score:7274.363, sT:42943, sP:3.4837, sB:0.7773, nT:131168, nP:4.1005, nB:0.8728
Cond:8046, Score:7273.874, sT:43068, sP:3.4824, sB:0.7744, nT:131585, nP:4.0988, nB:0.8707
Cond:110, Score:7272.907, sT:41936, sP:3.4626, sB:0.792, nT:127897, nP:4.0855, nB:0.8868
Cond:5, Score:7272.817, sT:43275, sP:3.4825, sB:0.7729, nT:132275, nP:4.1005, nB:0.8642
Cond:7838, Score:7270.582, sT:43147, sP:3.4837, sB:0.7727, nT:131864, nP:4.101, nB:0.8695
Cond:8368, Score:7266.394, sT:42812, sP:3.4653, sB:0.7728, nT:130680, nP:4.0741, nB:0.8728
Cond:2693, Score:7265.177, sT:42989, sP:3.4931, sB:0.7794, nT:131614, nP:4.1101, nB:0.8681
Cond:7, Score:7262.976, sT:43257, sP:3.4829, sB:0.7714, nT:132219, nP:4.1012, nB:0.8646
Cond:2277, Score:7262.041, sT:43083, sP:3.4886, sB:0.7749, nT:131888, nP:4.1032, nB:0.8672
Cond:3566, Score:7260.603, sT:43125, sP:3.4818, sB:0.7716, nT:131862, nP:4.0996, nB:0.8681
Cond:513, Score:7260.356, sT:43243, sP:3.4843, sB:0.773, nT:131994, nP:4.1042, nB:0.8643
Cond:3, Score:7260.264, sT:43310, sP:3.4817, sB:0.7692, nT:132388, nP:4.099, nB:0.864
Cond:5150, Score:7259.68, sT:43083, sP:3.4837, sB:0.7727, nT:131617, nP:4.1021, nB:0.8697
Cond:1573, Score:7258.62, sT:43136, sP:3.4872, sB:0.7752, nT:131959, nP:4.105, nB:0.8643
Cond:7614, Score:7258.584, sT:42969, sP:3.4859, sB:0.7746, nT:131279, nP:4.1036, nB:0.8722
Cond:5134, Score:7258.5, sT:42551, sP:3.4885, sB:0.7825, nT:129864, nP:4.1048, nB:0.8802
Cond:675, Score:7258.337, sT:43271, sP:3.4827, sB:0.7704, nT:132278, nP:4.1005, nB:0.8639
Cond:1141, Score:7257.863, sT:43264, sP:3.4813, sB:0.7707, nT:132214, nP:4.0987, nB:0.8632
Cond:1266, Score:7257.604, sT:43156, sP:3.4873, sB:0.7722, nT:131940, nP:4.104, nB:0.8676
Cond:49, Score:7257.279, sT:43040, sP:3.4888, sB:0.7838, nT:130999, nP:4.113, nB:0.862 , T2:6130,1395,65,275,1575,2695,2485,7822,291,677,5358,8046,110,5,7838,8368,2693,7,2277,3566,513,3,5150,1573,7614,5134,675,1141,1266,49,  #End#
LowScoreRank0 , T0:4833 , T1:
Cond:6130, Score:1390.134, sT:42348, sP:3.4393, sB:0.7858, nT:127365, nP:3.9794, nB:0.892
Cond:6385, Score:1387.841, sT:37625, sP:3.2713, sB:0.8204, nT:113303, nP:3.8119, nB:0.9371
Cond:6128, Score:1380.038, sT:38694, sP:3.341, sB:0.8158, nT:111591, nP:3.7608, nB:0.9271
Cond:65, Score:1379.432, sT:42303, sP:3.4998, sB:0.8191, nT:127940, nP:4.1275, nB:0.8679
Cond:1395, Score:1379.212, sT:42663, sP:3.4964, sB:0.8006, nT:129410, nP:4.1023, nB:0.8757
Cond:291, Score:1372.037, sT:42527, sP:3.5004, sB:0.8136, nT:127810, nP:4.117, nB:0.8587
Cond:94, Score:1371.872, sT:39877, sP:3.4228, sB:0.8205, nT:121084, nP:4.0598, nB:0.9106
Cond:1575, Score:1370.824, sT:42586, sP:3.5036, sB:0.7932, nT:130401, nP:4.1197, nB:0.8767
Cond:2695, Score:1370.226, sT:42236, sP:3.5179, sB:0.8002, nT:129564, nP:4.1337, nB:0.8838
Cond:110, Score:1370.181, sT:41727, sP:3.4644, sB:0.7959, nT:127109, nP:4.0873, nB:0.8893
Cond:275, Score:1369.594, sT:42998, sP:3.4865, sB:0.784, nT:130752, nP:4.1041, nB:0.8704
Cond:750, Score:1369.59, sT:38309, sP:3.3873, sB:0.835, nT:117652, nP:4.0319, nB:0.9259
Cond:8321, Score:1368.646, sT:40536, sP:3.5031, sB:0.8124, nT:122770, nP:4.1135, nB:0.9295
Cond:7822, Score:1367.881, sT:42542, sP:3.488, sB:0.7846, nT:129656, nP:4.1045, nB:0.8829
Cond:2485, Score:1366.643, sT:42794, sP:3.4912, sB:0.7834, nT:130818, nP:4.1064, nB:0.8728
Cond:7598, Score:1366.414, sT:41885, sP:3.494, sB:0.7951, nT:127706, nP:4.1098, nB:0.8915
Cond:5358, Score:1365.927, sT:42723, sP:3.4859, sB:0.7815, nT:130332, nP:4.1027, nB:0.8764
Cond:8046, Score:1365.1, sT:42848, sP:3.4846, sB:0.7786, nT:130749, nP:4.101, nB:0.8742
Cond:677, Score:1365.044, sT:43023, sP:3.486, sB:0.7788, nT:131367, nP:4.104, nB:0.8679
Cond:5134, Score:1364.875, sT:42331, sP:3.4908, sB:0.7868, nT:129031, nP:4.107, nB:0.8839
Cond:768, Score:1364.817, sT:40189, sP:3.5171, sB:0.8685, nT:113617, nP:4.1044, nB:0.8956
Cond:8368, Score:1364.724, sT:42592, sP:3.4674, sB:0.777, nT:129844, nP:4.0761, nB:0.8763
Cond:2693, Score:1364.438, sT:42766, sP:3.4955, sB:0.7839, nT:130770, nP:4.1125, nB:0.8719
Cond:49, Score:1364.377, sT:42819, sP:3.491, sB:0.7884, nT:130163, nP:4.1153, nB:0.8656
Cond:5, Score:1364.258, sT:43055, sP:3.4846, sB:0.7771, nT:131439, nP:4.1028, nB:0.8677
Cond:7838, Score:1364.055, sT:42927, sP:3.4859, sB:0.7769, nT:131028, nP:4.1032, nB:0.873
Cond:6122, Score:1363.767, sT:41750, sP:3.4533, sB:0.7904, nT:127525, nP:4.0856, nB:0.8815
Cond:7390, Score:1363.507, sT:42431, sP:3.4944, sB:0.786, nT:129544, nP:4.1133, nB:0.8805
Cond:7166, Score:1362.781, sT:42057, sP:3.499, sB:0.7917, nT:128436, nP:4.1194, nB:0.8869
Cond:305, Score:1362.705, sT:43044, sP:3.4858, sB:0.7832, nT:131149, nP:4.1073, nB:0.8584 , T2:6130,6385,6128,65,1395,291,94,1575,2695,110,275,750,8321,7822,2485,7598,5358,8046,677,5134,768,8368,2693,49,5,7838,6122,7390,7166,305,  #End#
LowScoreRank1 , T0:4833 , T1:
Cond:6130, Score:2423.855, sT:42348, sP:3.4393, sB:0.7858, nT:127365, nP:3.9794, nB:0.892
Cond:6385, Score:2405.764, sT:37625, sP:3.2713, sB:0.8204, nT:113303, nP:3.8119, nB:0.9371
Cond:1395, Score:2404.712, sT:42663, sP:3.4964, sB:0.8006, nT:129410, nP:4.1023, nB:0.8757
Cond:65, Score:2402.686, sT:42303, sP:3.4998, sB:0.8191, nT:127940, nP:4.1275, nB:0.8679
Cond:6128, Score:2393.134, sT:38694, sP:3.341, sB:0.8158, nT:111591, nP:3.7608, nB:0.9271
Cond:1575, Score:2390.845, sT:42586, sP:3.5036, sB:0.7932, nT:130401, nP:4.1197, nB:0.8767
Cond:291, Score:2389.932, sT:42527, sP:3.5004, sB:0.8136, nT:127810, nP:4.117, nB:0.8587
Cond:275, Score:2389.45, sT:42998, sP:3.4865, sB:0.784, nT:130752, nP:4.1041, nB:0.8704
Cond:2695, Score:2388.941, sT:42236, sP:3.5179, sB:0.8002, nT:129564, nP:4.1337, nB:0.8838
Cond:110, Score:2387.173, sT:41727, sP:3.4644, sB:0.7959, nT:127109, nP:4.0873, nB:0.8893
Cond:7822, Score:2385.804, sT:42542, sP:3.488, sB:0.7846, nT:129656, nP:4.1045, nB:0.8829
Cond:2485, Score:2384.263, sT:42794, sP:3.4912, sB:0.7834, nT:130818, nP:4.1064, nB:0.8728
Cond:94, Score:2384.114, sT:39877, sP:3.4228, sB:0.8205, nT:121084, nP:4.0598, nB:0.9106
Cond:5358, Score:2382.858, sT:42723, sP:3.4859, sB:0.7815, nT:130332, nP:4.1027, nB:0.8764
Cond:677, Score:2382.007, sT:43023, sP:3.486, sB:0.7788, nT:131367, nP:4.104, nB:0.8679
Cond:8046, Score:2381.823, sT:42848, sP:3.4846, sB:0.7786, nT:130749, nP:4.101, nB:0.8742
Cond:8321, Score:2381.499, sT:40536, sP:3.5031, sB:0.8124, nT:122770, nP:4.1135, nB:0.9295
Cond:7598, Score:2381.314, sT:41885, sP:3.494, sB:0.7951, nT:127706, nP:4.1098, nB:0.8915
Cond:5, Score:2380.77, sT:43055, sP:3.4846, sB:0.7771, nT:131439, nP:4.1028, nB:0.8677
Cond:8368, Score:2380.569, sT:42592, sP:3.4674, sB:0.777, nT:129844, nP:4.0761, nB:0.8763
Cond:2693, Score:2380.296, sT:42766, sP:3.4955, sB:0.7839, nT:130770, nP:4.1125, nB:0.8719
Cond:7838, Score:2380.263, sT:42927, sP:3.4859, sB:0.7769, nT:131028, nP:4.1032, nB:0.873
Cond:5134, Score:2379.929, sT:42331, sP:3.4908, sB:0.7868, nT:129031, nP:4.107, nB:0.8839
Cond:49, Score:2379.426, sT:42819, sP:3.491, sB:0.7884, nT:130163, nP:4.1153, nB:0.8656
Cond:7390, Score:2377.837, sT:42431, sP:3.4944, sB:0.786, nT:129544, nP:4.1133, nB:0.8805
Cond:2277, Score:2377.716, sT:42865, sP:3.4907, sB:0.7789, nT:131054, nP:4.1054, nB:0.8708
Cond:7, Score:2377.383, sT:43037, sP:3.485, sB:0.7756, nT:131383, nP:4.1035, nB:0.868
Cond:7614, Score:2377.345, sT:42749, sP:3.4881, sB:0.7788, nT:130443, nP:4.1059, nB:0.8758
Cond:305, Score:2377.264, sT:43044, sP:3.4858, sB:0.7832, nT:131149, nP:4.1073, nB:0.8584
Cond:3566, Score:2377.223, sT:42905, sP:3.4839, sB:0.7758, nT:131026, nP:4.1018, nB:0.8717 , T2:6130,6385,1395,65,6128,1575,291,275,2695,110,7822,2485,94,5358,677,8046,8321,7598,5,8368,2693,7838,5134,49,7390,2277,7,7614,305,3566,  #End#
LowScoreRank2 , T0:4833 , T1:
Cond:6130, Score:4229.404, sT:42348, sP:3.4393, sB:0.7858, nT:127365, nP:3.9794, nB:0.892
Cond:1395, Score:4195.856, sT:42663, sP:3.4964, sB:0.8006, nT:129410, nP:4.1023, nB:0.8757
Cond:65, Score:4188.082, sT:42303, sP:3.4998, sB:0.8191, nT:127940, nP:4.1275, nB:0.8679
Cond:6385, Score:4173.397, sT:37625, sP:3.2713, sB:0.8204, nT:113303, nP:3.8119, nB:0.9371
Cond:1575, Score:4173.042, sT:42586, sP:3.5036, sB:0.7932, nT:130401, nP:4.1197, nB:0.8767
Cond:275, Score:4171.88, sT:42998, sP:3.4865, sB:0.784, nT:130752, nP:4.1041, nB:0.8704
Cond:2695, Score:4168.23, sT:42236, sP:3.5179, sB:0.8002, nT:129564, nP:4.1337, nB:0.8838
Cond:291, Score:4166.035, sT:42527, sP:3.5004, sB:0.8136, nT:127810, nP:4.117, nB:0.8587
Cond:7822, Score:4164.383, sT:42542, sP:3.488, sB:0.7846, nT:129656, nP:4.1045, nB:0.8829
Cond:2485, Score:4162.788, sT:42794, sP:3.4912, sB:0.7834, nT:130818, nP:4.1064, nB:0.8728
Cond:110, Score:4162.161, sT:41727, sP:3.4644, sB:0.7959, nT:127109, nP:4.0873, nB:0.8893
Cond:5358, Score:4160.052, sT:42723, sP:3.4859, sB:0.7815, nT:130332, nP:4.1027, nB:0.8764
Cond:677, Score:4159.774, sT:43023, sP:3.486, sB:0.7788, nT:131367, nP:4.104, nB:0.8679
Cond:8046, Score:4158.963, sT:42848, sP:3.4846, sB:0.7786, nT:130749, nP:4.101, nB:0.8742
Cond:5, Score:4157.849, sT:43055, sP:3.4846, sB:0.7771, nT:131439, nP:4.1028, nB:0.8677
Cond:7838, Score:4156.698, sT:42927, sP:3.4859, sB:0.7769, nT:131028, nP:4.1032, nB:0.873
Cond:8368, Score:4155.723, sT:42592, sP:3.4674, sB:0.777, nT:129844, nP:4.0761, nB:0.8763
Cond:2693, Score:4155.654, sT:42766, sP:3.4955, sB:0.7839, nT:130770, nP:4.1125, nB:0.8719
Cond:7598, Score:4153.181, sT:41885, sP:3.494, sB:0.7951, nT:127706, nP:4.1098, nB:0.8915
Cond:5134, Score:4153.028, sT:42331, sP:3.4908, sB:0.7868, nT:129031, nP:4.107, nB:0.8839
Cond:6128, Score:4152.8, sT:38694, sP:3.341, sB:0.8158, nT:111591, nP:3.7608, nB:0.9271
Cond:49, Score:4152.76, sT:42819, sP:3.491, sB:0.7884, nT:130163, nP:4.1153, nB:0.8656
Cond:7, Score:4151.973, sT:43037, sP:3.485, sB:0.7756, nT:131383, nP:4.1035, nB:0.868
Cond:2277, Score:4151.923, sT:42865, sP:3.4907, sB:0.7789, nT:131054, nP:4.1054, nB:0.8708
Cond:3566, Score:4151.337, sT:42905, sP:3.4839, sB:0.7758, nT:131026, nP:4.1018, nB:0.8717
Cond:513, Score:4151.056, sT:43023, sP:3.4865, sB:0.7772, nT:131158, nP:4.1064, nB:0.8678
Cond:5150, Score:4151.004, sT:42863, sP:3.4859, sB:0.7769, nT:130781, nP:4.1043, nB:0.8733
Cond:7614, Score:4150.735, sT:42749, sP:3.4881, sB:0.7788, nT:130443, nP:4.1059, nB:0.8758
Cond:305, Score:4150.315, sT:43044, sP:3.4858, sB:0.7832, nT:131149, nP:4.1073, nB:0.8584
Cond:3, Score:4150.035, sT:43090, sP:3.4838, sB:0.7733, nT:131552, nP:4.1012, nB:0.8675 , T2:6130,1395,65,6385,1575,275,2695,291,7822,2485,110,5358,677,8046,5,7838,8368,2693,7598,5134,6128,49,7,2277,3566,513,5150,7614,305,3,  #End#
LowScoreRank3 , T0:4833 , T1:
Cond:6130, Score:7385.442, sT:42348, sP:3.4393, sB:0.7858, nT:127365, nP:3.9794, nB:0.892
Cond:1395, Score:7326.66, sT:42663, sP:3.4964, sB:0.8006, nT:129410, nP:4.1023, nB:0.8757
Cond:65, Score:7305.634, sT:42303, sP:3.4998, sB:0.8191, nT:127940, nP:4.1275, nB:0.8679
Cond:275, Score:7289.468, sT:42998, sP:3.4865, sB:0.784, nT:130752, nP:4.1041, nB:0.8704
Cond:1575, Score:7289.346, sT:42586, sP:3.5036, sB:0.7932, nT:130401, nP:4.1197, nB:0.8767
Cond:2695, Score:7278.358, sT:42236, sP:3.5179, sB:0.8002, nT:129564, nP:4.1337, nB:0.8838
Cond:7822, Score:7274.426, sT:42542, sP:3.488, sB:0.7846, nT:129656, nP:4.1045, nB:0.8829
Cond:2485, Score:7273.578, sT:42794, sP:3.4912, sB:0.7834, nT:130818, nP:4.1064, nB:0.8728
Cond:677, Score:7269.917, sT:43023, sP:3.486, sB:0.7788, nT:131367, nP:4.104, nB:0.8679
Cond:5358, Score:7268.288, sT:42723, sP:3.4859, sB:0.7815, nT:130332, nP:4.1027, nB:0.8764
Cond:8046, Score:7267.64, sT:42848, sP:3.4846, sB:0.7786, nT:130749, nP:4.101, nB:0.8742
Cond:291, Score:7267.426, sT:42527, sP:3.5004, sB:0.8136, nT:127810, nP:4.117, nB:0.8587
Cond:5, Score:7266.961, sT:43055, sP:3.4846, sB:0.7771, nT:131439, nP:4.1028, nB:0.8677
Cond:7838, Score:7264.488, sT:42927, sP:3.4859, sB:0.7769, nT:131028, nP:4.1032, nB:0.873
Cond:110, Score:7262.487, sT:41727, sP:3.4644, sB:0.7959, nT:127109, nP:4.0873, nB:0.8893
Cond:2693, Score:7260.749, sT:42766, sP:3.4955, sB:0.7839, nT:130770, nP:4.1125, nB:0.8719
Cond:8368, Score:7260.139, sT:42592, sP:3.4674, sB:0.777, nT:129844, nP:4.0761, nB:0.8763
Cond:7, Score:7256.76, sT:43037, sP:3.485, sB:0.7756, nT:131383, nP:4.1035, nB:0.868
Cond:2277, Score:7255.588, sT:42865, sP:3.4907, sB:0.7789, nT:131054, nP:4.1054, nB:0.8708
Cond:3566, Score:7255.031, sT:42905, sP:3.4839, sB:0.7758, nT:131026, nP:4.1018, nB:0.8717
Cond:513, Score:7254.438, sT:43023, sP:3.4865, sB:0.7772, nT:131158, nP:4.1064, nB:0.8678
Cond:3, Score:7254.093, sT:43090, sP:3.4838, sB:0.7733, nT:131552, nP:4.1012, nB:0.8675
Cond:5150, Score:7253.907, sT:42863, sP:3.4859, sB:0.7769, nT:130781, nP:4.1043, nB:0.8733
Cond:49, Score:7253.22, sT:42819, sP:3.491, sB:0.7884, nT:130163, nP:4.1153, nB:0.8656
Cond:5134, Score:7252.67, sT:42331, sP:3.4908, sB:0.7868, nT:129031, nP:4.107, nB:0.8839
Cond:7614, Score:7252.551, sT:42749, sP:3.4881, sB:0.7788, nT:130443, nP:4.1059, nB:0.8758
Cond:675, Score:7252.112, sT:43051, sP:3.4848, sB:0.7745, nT:131442, nP:4.1027, nB:0.8674
Cond:1141, Score:7251.301, sT:43045, sP:3.4833, sB:0.7747, nT:131378, nP:4.1009, nB:0.8667
Cond:305, Score:7251.294, sT:43044, sP:3.4858, sB:0.7832, nT:131149, nP:4.1073, nB:0.8584
Cond:5582, Score:7250.941, sT:42977, sP:3.483, sB:0.7738, nT:131231, nP:4.1003, nB:0.87 , T2:6130,1395,65,275,1575,2695,7822,2485,677,5358,8046,291,5,7838,110,2693,8368,7,2277,3566,513,3,5150,49,5134,7614,675,1141,305,5582,  #End#
LowScoreRank0 , T0:6903 , T1:
Cond:6130, Score:1389.504, sT:42343, sP:3.443, sB:0.7871, nT:127399, nP:3.9832, nB:0.8908
Cond:6385, Score:1387.248, sT:37621, sP:3.2752, sB:0.822, nT:113337, nP:3.8164, nB:0.9358
Cond:1395, Score:1378.807, sT:42657, sP:3.5003, sB:0.802, nT:129450, nP:4.1061, nB:0.8747
Cond:65, Score:1378.649, sT:42300, sP:3.5035, sB:0.8201, nT:127983, nP:4.1313, nB:0.8668
Cond:6128, Score:1378.32, sT:38682, sP:3.3451, sB:0.8167, nT:111603, nP:3.765, nB:0.9254
Cond:291, Score:1371.98, sT:42522, sP:3.5042, sB:0.8153, nT:127850, nP:4.1208, nB:0.8577
Cond:1575, Score:1370.92, sT:42582, sP:3.5073, sB:0.795, nT:130443, nP:4.1234, nB:0.8757
Cond:110, Score:1370.757, sT:41718, sP:3.4665, sB:0.7979, nT:127081, nP:4.0903, nB:0.8886
Cond:2695, Score:1370.336, sT:42236, sP:3.5214, sB:0.8019, nT:129622, nP:4.1371, nB:0.8827
Cond:8321, Score:1369.825, sT:40546, sP:3.5066, sB:0.8148, nT:122849, nP:4.117, nB:0.9287
Cond:275, Score:1369.579, sT:42993, sP:3.4902, sB:0.7857, nT:130792, nP:4.1078, nB:0.8694
Cond:7822, Score:1367.624, sT:42537, sP:3.4918, sB:0.7861, nT:129696, nP:4.1083, nB:0.8819
Cond:94, Score:1367.243, sT:39849, sP:3.4246, sB:0.8181, nT:120992, nP:4.0621, nB:0.9092
Cond:2485, Score:1367.058, sT:42791, sP:3.4948, sB:0.7854, nT:130862, nP:4.1101, nB:0.8719
Cond:750, Score:1365.959, sT:38272, sP:3.3897, sB:0.834, nT:117545, nP:4.0348, nB:0.9245
Cond:5358, Score:1365.811, sT:42718, sP:3.4894, sB:0.7829, nT:130365, nP:4.1064, nB:0.8757
Cond:7598, Score:1365.342, sT:41881, sP:3.4978, sB:0.7958, nT:127746, nP:4.1137, nB:0.8905
Cond:8046, Score:1364.881, sT:42844, sP:3.4883, sB:0.7801, nT:130789, nP:4.1048, nB:0.8732
Cond:677, Score:1364.827, sT:43019, sP:3.4897, sB:0.7803, nT:131407, nP:4.1078, nB:0.8669
Cond:768, Score:1364.809, sT:40193, sP:3.5201, sB:0.8701, nT:113673, nP:4.1079, nB:0.8942
Cond:5134, Score:1364.624, sT:42324, sP:3.4943, sB:0.788, nT:129060, nP:4.1108, nB:0.8834
Cond:8368, Score:1364.565, sT:42588, sP:3.4711, sB:0.7785, nT:129884, nP:4.0799, nB:0.8754
Cond:2693, Score:1364.466, sT:42766, sP:3.499, sB:0.7856, nT:130822, nP:4.116, nB:0.8707
Cond:6122, Score:1364.166, sT:41742, sP:3.4572, sB:0.7924, nT:127555, nP:4.0894, nB:0.8809
Cond:49, Score:1364.16, sT:42815, sP:3.4947, sB:0.7899, nT:130203, nP:4.1191, nB:0.8646
Cond:5, Score:1364.055, sT:43051, sP:3.4883, sB:0.7786, nT:131479, nP:4.1065, nB:0.8667
Cond:7838, Score:1363.801, sT:42923, sP:3.4896, sB:0.7783, nT:131068, nP:4.107, nB:0.8721
Cond:7390, Score:1363.213, sT:42426, sP:3.4982, sB:0.7874, nT:129584, nP:4.1171, nB:0.8796
Cond:2277, Score:1362.885, sT:42860, sP:3.4944, sB:0.7808, nT:131094, nP:4.1092, nB:0.8698
Cond:7150, Score:1362.848, sT:41007, sP:3.5138, sB:0.8102, nT:125567, nP:4.1313, nB:0.9038 , T2:6130,6385,1395,65,6128,291,1575,110,2695,8321,275,7822,94,2485,750,5358,7598,8046,677,768,5134,8368,2693,6122,49,5,7838,7390,2277,7150,  #End#
LowScoreRank1 , T0:6903 , T1:
Cond:6130, Score:2422.675, sT:42343, sP:3.443, sB:0.7871, nT:127399, nP:3.9832, nB:0.8908
Cond:6385, Score:2404.645, sT:37621, sP:3.2752, sB:0.822, nT:113337, nP:3.8164, nB:0.9358
Cond:1395, Score:2403.933, sT:42657, sP:3.5003, sB:0.802, nT:129450, nP:4.1061, nB:0.8747
Cond:65, Score:2401.266, sT:42300, sP:3.5035, sB:0.8201, nT:127983, nP:4.1313, nB:0.8668
Cond:1575, Score:2390.924, sT:42582, sP:3.5073, sB:0.795, nT:130443, nP:4.1234, nB:0.8757
Cond:6128, Score:2390.06, sT:38682, sP:3.3451, sB:0.8167, nT:111603, nP:3.765, nB:0.9254
Cond:291, Score:2389.75, sT:42522, sP:3.5042, sB:0.8153, nT:127850, nP:4.1208, nB:0.8577
Cond:275, Score:2389.339, sT:42993, sP:3.4902, sB:0.7857, nT:130792, nP:4.1078, nB:0.8694
Cond:2695, Score:2389.06, sT:42236, sP:3.5214, sB:0.8019, nT:129622, nP:4.1371, nB:0.8827
Cond:110, Score:2388.041, sT:41718, sP:3.4665, sB:0.7979, nT:127081, nP:4.0903, nB:0.8886
Cond:7822, Score:2385.277, sT:42537, sP:3.4918, sB:0.7861, nT:129696, nP:4.1083, nB:0.8819
Cond:2485, Score:2384.895, sT:42791, sP:3.4948, sB:0.7854, nT:130862, nP:4.1101, nB:0.8719
Cond:8321, Score:2383.49, sT:40546, sP:3.5066, sB:0.8148, nT:122849, nP:4.117, nB:0.9287
Cond:5358, Score:2382.587, sT:42718, sP:3.4894, sB:0.7829, nT:130365, nP:4.1064, nB:0.8757
Cond:677, Score:2381.551, sT:43019, sP:3.4897, sB:0.7803, nT:131407, nP:4.1078, nB:0.8669
Cond:8046, Score:2381.364, sT:42844, sP:3.4883, sB:0.7801, nT:130789, nP:4.1048, nB:0.8732
Cond:5, Score:2380.338, sT:43051, sP:3.4883, sB:0.7786, nT:131479, nP:4.1065, nB:0.8667
Cond:2693, Score:2380.262, sT:42766, sP:3.499, sB:0.7856, nT:130822, nP:4.116, nB:0.8707
Cond:8368, Score:2380.217, sT:42588, sP:3.4711, sB:0.7785, nT:129884, nP:4.0799, nB:0.8754
Cond:7838, Score:2379.748, sT:42923, sP:3.4896, sB:0.7783, nT:131068, nP:4.107, nB:0.8721
Cond:5134, Score:2379.434, sT:42324, sP:3.4943, sB:0.788, nT:129060, nP:4.1108, nB:0.8834
Cond:7598, Score:2379.403, sT:41881, sP:3.4978, sB:0.7958, nT:127746, nP:4.1137, nB:0.8905
Cond:49, Score:2378.971, sT:42815, sP:3.4947, sB:0.7899, nT:130203, nP:4.1191, nB:0.8646
Cond:2277, Score:2377.934, sT:42860, sP:3.4944, sB:0.7808, nT:131094, nP:4.1092, nB:0.8698
Cond:5150, Score:2377.829, sT:42857, sP:3.4894, sB:0.7787, nT:130810, nP:4.108, nB:0.8728
Cond:7390, Score:2377.255, sT:42426, sP:3.4982, sB:0.7874, nT:129584, nP:4.1171, nB:0.8796
Cond:7, Score:2377.071, sT:43033, sP:3.4887, sB:0.7771, nT:131423, nP:4.1072, nB:0.8671
Cond:3566, Score:2377.014, sT:42899, sP:3.4876, sB:0.7774, nT:131063, nP:4.1055, nB:0.8708
Cond:6122, Score:2376.735, sT:41742, sP:3.4572, sB:0.7924, nT:127555, nP:4.0894, nB:0.8809
Cond:7614, Score:2376.713, sT:42745, sP:3.4918, sB:0.7802, nT:130483, nP:4.1097, nB:0.8748 , T2:6130,6385,1395,65,1575,6128,291,275,2695,110,7822,2485,8321,5358,677,8046,5,2693,8368,7838,5134,7598,49,2277,5150,7390,7,3566,6122,7614,  #End#
LowScoreRank2 , T0:6903 , T1:
Cond:6130, Score:4227.206, sT:42343, sP:3.443, sB:0.7871, nT:127399, nP:3.9832, nB:0.8908
Cond:1395, Score:4194.37, sT:42657, sP:3.5003, sB:0.802, nT:129450, nP:4.1061, nB:0.8747
Cond:65, Score:4185.511, sT:42300, sP:3.5035, sB:0.8201, nT:127983, nP:4.1313, nB:0.8668
Cond:1575, Score:4173.028, sT:42582, sP:3.5073, sB:0.795, nT:130443, nP:4.1234, nB:0.8757
Cond:275, Score:4171.536, sT:42993, sP:3.4902, sB:0.7857, nT:130792, nP:4.1078, nB:0.8694
Cond:6385, Score:4171.298, sT:37621, sP:3.2752, sB:0.822, nT:113337, nP:3.8164, nB:0.9358
Cond:2695, Score:4168.311, sT:42236, sP:3.5214, sB:0.8019, nT:129622, nP:4.1371, nB:0.8827
Cond:291, Score:4165.573, sT:42522, sP:3.5042, sB:0.8153, nT:127850, nP:4.1208, nB:0.8577
Cond:2485, Score:4163.732, sT:42791, sP:3.4948, sB:0.7854, nT:130862, nP:4.1101, nB:0.8719
Cond:110, Score:4163.433, sT:41718, sP:3.4665, sB:0.7979, nT:127081, nP:4.0903, nB:0.8886
Cond:7822, Score:4163.33, sT:42537, sP:3.4918, sB:0.7861, nT:129696, nP:4.1083, nB:0.8819
Cond:5358, Score:4159.461, sT:42718, sP:3.4894, sB:0.7829, nT:130365, nP:4.1064, nB:0.8757
Cond:677, Score:4158.841, sT:43019, sP:3.4897, sB:0.7803, nT:131407, nP:4.1078, nB:0.8669
Cond:8046, Score:4158.026, sT:42844, sP:3.4883, sB:0.7801, nT:130789, nP:4.1048, nB:0.8732
Cond:5, Score:4156.959, sT:43051, sP:3.4883, sB:0.7786, nT:131479, nP:4.1065, nB:0.8667
Cond:7838, Score:4155.676, sT:42923, sP:3.4896, sB:0.7783, nT:131068, nP:4.107, nB:0.8721
Cond:2693, Score:4155.451, sT:42766, sP:3.499, sB:0.7856, nT:130822, nP:4.116, nB:0.8707
Cond:8368, Score:4154.979, sT:42588, sP:3.4711, sB:0.7785, nT:129884, nP:4.0799, nB:0.8754
Cond:2277, Score:4152.137, sT:42860, sP:3.4944, sB:0.7808, nT:131094, nP:4.1092, nB:0.8698
Cond:5134, Score:4152.065, sT:42324, sP:3.4943, sB:0.788, nT:129060, nP:4.1108, nB:0.8834
Cond:5150, Score:4151.949, sT:42857, sP:3.4894, sB:0.7787, nT:130810, nP:4.108, nB:0.8728
Cond:49, Score:4151.833, sT:42815, sP:3.4947, sB:0.7899, nT:130203, nP:4.1191, nB:0.8646
Cond:7, Score:4151.298, sT:43033, sP:3.4887, sB:0.7771, nT:131423, nP:4.1072, nB:0.8671
Cond:3566, Score:4150.829, sT:42899, sP:3.4876, sB:0.7774, nT:131063, nP:4.1055, nB:0.8708
Cond:8321, Score:4150.383, sT:40546, sP:3.5066, sB:0.8148, nT:122849, nP:4.117, nB:0.9287
Cond:513, Score:4149.892, sT:43019, sP:3.4901, sB:0.7786, nT:131198, nP:4.1102, nB:0.8668
Cond:7598, Score:4149.775, sT:41881, sP:3.4978, sB:0.7958, nT:127746, nP:4.1137, nB:0.8905
Cond:7614, Score:4149.505, sT:42745, sP:3.4918, sB:0.7802, nT:130483, nP:4.1097, nB:0.8748
Cond:1573, Score:4149.46, sT:42912, sP:3.493, sB:0.7809, nT:131163, nP:4.111, nB:0.8668
Cond:3, Score:4149.154, sT:43086, sP:3.4875, sB:0.7748, nT:131592, nP:4.1049, nB:0.8665 , T2:6130,1395,65,1575,275,6385,2695,291,2485,110,7822,5358,677,8046,5,7838,2693,8368,2277,5134,5150,49,7,3566,8321,513,7598,7614,1573,3,  #End#
LowScoreRank3 , T0:6903 , T1:
Cond:6130, Score:7381.361, sT:42343, sP:3.443, sB:0.7871, nT:127399, nP:3.9832, nB:0.8908
Cond:1395, Score:7323.848, sT:42657, sP:3.5003, sB:0.802, nT:129450, nP:4.1061, nB:0.8747
Cond:65, Score:7300.983, sT:42300, sP:3.5035, sB:0.8201, nT:127983, nP:4.1313, nB:0.8668
Cond:1575, Score:7289.058, sT:42582, sP:3.5073, sB:0.795, nT:130443, nP:4.1234, nB:0.8757
Cond:275, Score:7288.606, sT:42993, sP:3.4902, sB:0.7857, nT:130792, nP:4.1078, nB:0.8694
Cond:2695, Score:7278.283, sT:42236, sP:3.5214, sB:0.8019, nT:129622, nP:4.1371, nB:0.8827
Cond:2485, Score:7274.95, sT:42791, sP:3.4948, sB:0.7854, nT:130862, nP:4.1101, nB:0.8719
Cond:7822, Score:7272.355, sT:42537, sP:3.4918, sB:0.7861, nT:129696, nP:4.1083, nB:0.8819
Cond:677, Score:7268.05, sT:43019, sP:3.4897, sB:0.7803, nT:131407, nP:4.1078, nB:0.8669
Cond:5358, Score:7267.049, sT:42718, sP:3.4894, sB:0.7829, nT:130365, nP:4.1064, nB:0.8757
Cond:291, Score:7266.371, sT:42522, sP:3.5042, sB:0.8153, nT:127850, nP:4.1208, nB:0.8577
Cond:8046, Score:7265.768, sT:42844, sP:3.4883, sB:0.7801, nT:130789, nP:4.1048, nB:0.8732
Cond:5, Score:7265.172, sT:43051, sP:3.4883, sB:0.7786, nT:131479, nP:4.1065, nB:0.8667
Cond:110, Score:7264.282, sT:41718, sP:3.4665, sB:0.7979, nT:127081, nP:4.0903, nB:0.8886
Cond:7838, Score:7262.492, sT:42923, sP:3.4896, sB:0.7783, nT:131068, nP:4.107, nB:0.8721
Cond:2693, Score:7260.149, sT:42766, sP:3.499, sB:0.7856, nT:130822, nP:4.116, nB:0.8707
Cond:8368, Score:7258.616, sT:42588, sP:3.4711, sB:0.7785, nT:129884, nP:4.0799, nB:0.8754
Cond:2277, Score:7255.672, sT:42860, sP:3.4944, sB:0.7808, nT:131094, nP:4.1092, nB:0.8698
Cond:7, Score:7255.358, sT:43033, sP:3.4887, sB:0.7771, nT:131423, nP:4.1072, nB:0.8671
Cond:5150, Score:7255.313, sT:42857, sP:3.4894, sB:0.7787, nT:130810, nP:4.108, nB:0.8728
Cond:3566, Score:7253.898, sT:42899, sP:3.4876, sB:0.7774, nT:131063, nP:4.1055, nB:0.8708
Cond:3, Score:7252.318, sT:43086, sP:3.4875, sB:0.7748, nT:131592, nP:4.1049, nB:0.8665
Cond:513, Score:7252.179, sT:43019, sP:3.4901, sB:0.7786, nT:131198, nP:4.1102, nB:0.8668
Cond:49, Score:7251.369, sT:42815, sP:3.4947, sB:0.7899, nT:130203, nP:4.1191, nB:0.8646
Cond:1573, Score:7250.831, sT:42912, sP:3.493, sB:0.7809, nT:131163, nP:4.111, nB:0.8668
Cond:5134, Score:7250.818, sT:42324, sP:3.4943, sB:0.788, nT:129060, nP:4.1108, nB:0.8834
Cond:675, Score:7250.264, sT:43047, sP:3.4885, sB:0.776, nT:131482, nP:4.1065, nB:0.8664
Cond:7614, Score:7250.181, sT:42745, sP:3.4918, sB:0.7802, nT:130483, nP:4.1097, nB:0.8748
Cond:1266, Score:7249.967, sT:42931, sP:3.4932, sB:0.778, nT:131149, nP:4.1099, nB:0.87
Cond:1141, Score:7249.856, sT:43040, sP:3.4871, sB:0.7763, nT:131418, nP:4.1046, nB:0.8657 , T2:6130,1395,65,1575,275,2695,2485,7822,677,5358,291,8046,5,110,7838,2693,8368,2277,7,5150,3566,3,513,49,1573,5134,675,7614,1266,1141,  #End#
LowScoreRank0 , T0:4887 , T1:
Cond:6130, Score:1389.791, sT:42318, sP:3.442, sB:0.7872, nT:127336, nP:3.9824, nB:0.8915
Cond:6385, Score:1387.379, sT:37595, sP:3.2742, sB:0.822, nT:113274, nP:3.8153, nB:0.9365
Cond:6128, Score:1379.324, sT:38665, sP:3.344, sB:0.8171, nT:111563, nP:3.7641, nB:0.9265
Cond:65, Score:1378.887, sT:42273, sP:3.5026, sB:0.8203, nT:127914, nP:4.1305, nB:0.8674
Cond:1395, Score:1378.825, sT:42631, sP:3.4993, sB:0.802, nT:129381, nP:4.1053, nB:0.8752
Cond:291, Score:1371.888, sT:42496, sP:3.5032, sB:0.8152, nT:127781, nP:4.12, nB:0.8582
Cond:110, Score:1370.902, sT:41708, sP:3.4659, sB:0.7977, nT:127061, nP:4.0898, nB:0.8892
Cond:1575, Score:1370.819, sT:42556, sP:3.5064, sB:0.7949, nT:130374, nP:4.1226, nB:0.8762
Cond:2695, Score:1370.247, sT:42210, sP:3.5204, sB:0.8018, nT:129553, nP:4.1363, nB:0.8832
Cond:94, Score:1369.867, sT:39851, sP:3.4242, sB:0.82, nT:121008, nP:4.0619, nB:0.9101
Cond:275, Score:1369.504, sT:42967, sP:3.4892, sB:0.7856, nT:130723, nP:4.107, nB:0.8699
Cond:8321, Score:1368.659, sT:40520, sP:3.5058, sB:0.8139, nT:122788, nP:4.1162, nB:0.9288
Cond:750, Score:1367.887, sT:38271, sP:3.3892, sB:0.8353, nT:117559, nP:4.0344, nB:0.9253
Cond:7822, Score:1367.556, sT:42512, sP:3.4908, sB:0.786, nT:129627, nP:4.1075, nB:0.8824
Cond:2485, Score:1366.962, sT:42765, sP:3.4939, sB:0.7853, nT:130793, nP:4.1093, nB:0.8724
Cond:5358, Score:1366.022, sT:42692, sP:3.4885, sB:0.7831, nT:130296, nP:4.1056, nB:0.8762
Cond:7598, Score:1366.003, sT:41854, sP:3.4968, sB:0.7964, nT:127677, nP:4.1129, nB:0.8911
Cond:8046, Score:1364.871, sT:42818, sP:3.4873, sB:0.78, nT:130720, nP:4.104, nB:0.8738
Cond:5134, Score:1364.776, sT:42299, sP:3.493, sB:0.7882, nT:128983, nP:4.1099, nB:0.8837
Cond:768, Score:1364.775, sT:40171, sP:3.519, sB:0.8698, nT:113609, nP:4.1067, nB:0.895
Cond:677, Score:1364.755, sT:42993, sP:3.4887, sB:0.7802, nT:131338, nP:4.107, nB:0.8674
Cond:2693, Score:1364.453, sT:42740, sP:3.498, sB:0.7855, nT:130753, nP:4.1152, nB:0.8713
Cond:8368, Score:1364.423, sT:42562, sP:3.4701, sB:0.7784, nT:129811, nP:4.079, nB:0.8758
Cond:49, Score:1364.062, sT:42789, sP:3.4938, sB:0.7898, nT:130134, nP:4.1183, nB:0.8651
Cond:5, Score:1364.048, sT:43025, sP:3.4873, sB:0.7785, nT:131410, nP:4.1057, nB:0.8673
Cond:7150, Score:1363.803, sT:40994, sP:3.5129, sB:0.8107, nT:125543, nP:4.1303, nB:0.9045
Cond:7838, Score:1363.727, sT:42897, sP:3.4886, sB:0.7782, nT:130999, nP:4.1062, nB:0.8726
Cond:7390, Score:1363.191, sT:42400, sP:3.4971, sB:0.7874, nT:129516, nP:4.1163, nB:0.88
Cond:5150, Score:1362.814, sT:42831, sP:3.4884, sB:0.7786, nT:130741, nP:4.1072, nB:0.8734
Cond:2277, Score:1362.792, sT:42834, sP:3.4935, sB:0.7807, nT:131025, nP:4.1084, nB:0.8703 , T2:6130,6385,6128,65,1395,291,110,1575,2695,94,275,8321,750,7822,2485,5358,7598,8046,5134,768,677,2693,8368,49,5,7150,7838,7390,5150,2277,  #End#
LowScoreRank1 , T0:4887 , T1:
Cond:6130, Score:2423.132, sT:42318, sP:3.442, sB:0.7872, nT:127336, nP:3.9824, nB:0.8915
Cond:6385, Score:2404.823, sT:37595, sP:3.2742, sB:0.822, nT:113274, nP:3.8153, nB:0.9365
Cond:1395, Score:2403.913, sT:42631, sP:3.4993, sB:0.802, nT:129381, nP:4.1053, nB:0.8752
Cond:65, Score:2401.624, sT:42273, sP:3.5026, sB:0.8203, nT:127914, nP:4.1305, nB:0.8674
Cond:6128, Score:2391.772, sT:38665, sP:3.344, sB:0.8171, nT:111563, nP:3.7641, nB:0.9265
Cond:1575, Score:2390.702, sT:42556, sP:3.5064, sB:0.7949, nT:130374, nP:4.1226, nB:0.8762
Cond:291, Score:2389.543, sT:42496, sP:3.5032, sB:0.8152, nT:127781, nP:4.12, nB:0.8582
Cond:275, Score:2389.161, sT:42967, sP:3.4892, sB:0.7856, nT:130723, nP:4.107, nB:0.8699
Cond:2695, Score:2388.858, sT:42210, sP:3.5204, sB:0.8018, nT:129553, nP:4.1363, nB:0.8832
Cond:110, Score:2388.299, sT:41708, sP:3.4659, sB:0.7977, nT:127061, nP:4.0898, nB:0.8892
Cond:7822, Score:2385.113, sT:42512, sP:3.4908, sB:0.786, nT:129627, nP:4.1075, nB:0.8824
Cond:2485, Score:2384.682, sT:42765, sP:3.4939, sB:0.7853, nT:130793, nP:4.1093, nB:0.8724
Cond:5358, Score:2382.896, sT:42692, sP:3.4885, sB:0.7831, nT:130296, nP:4.1056, nB:0.8762
Cond:8321, Score:2381.434, sT:40520, sP:3.5058, sB:0.8139, nT:122788, nP:4.1162, nB:0.9288
Cond:677, Score:2381.379, sT:42993, sP:3.4887, sB:0.7802, nT:131338, nP:4.107, nB:0.8674
Cond:8046, Score:2381.303, sT:42818, sP:3.4873, sB:0.78, nT:130720, nP:4.104, nB:0.8738
Cond:94, Score:2380.553, sT:39851, sP:3.4242, sB:0.82, nT:121008, nP:4.0619, nB:0.9101
Cond:7598, Score:2380.482, sT:41854, sP:3.4968, sB:0.7964, nT:127677, nP:4.1129, nB:0.8911
Cond:5, Score:2380.285, sT:43025, sP:3.4873, sB:0.7785, nT:131410, nP:4.1057, nB:0.8673
Cond:2693, Score:2380.198, sT:42740, sP:3.498, sB:0.7855, nT:130753, nP:4.1152, nB:0.8713
Cond:8368, Score:2379.917, sT:42562, sP:3.4701, sB:0.7784, nT:129811, nP:4.079, nB:0.8758
Cond:5134, Score:2379.627, sT:42299, sP:3.493, sB:0.7882, nT:128983, nP:4.1099, nB:0.8837
Cond:7838, Score:2379.574, sT:42897, sP:3.4886, sB:0.7782, nT:130999, nP:4.1062, nB:0.8726
Cond:49, Score:2378.754, sT:42789, sP:3.4938, sB:0.7898, nT:130134, nP:4.1183, nB:0.8651
Cond:5150, Score:2377.769, sT:42831, sP:3.4884, sB:0.7786, nT:130741, nP:4.1072, nB:0.8734
Cond:2277, Score:2377.726, sT:42834, sP:3.4935, sB:0.7807, nT:131025, nP:4.1084, nB:0.8703
Cond:7390, Score:2377.162, sT:42400, sP:3.4971, sB:0.7874, nT:129516, nP:4.1163, nB:0.88
Cond:3566, Score:2377.099, sT:42876, sP:3.4864, sB:0.7773, nT:130994, nP:4.1047, nB:0.8714
Cond:7, Score:2376.901, sT:43007, sP:3.4877, sB:0.777, nT:131354, nP:4.1064, nB:0.8676
Cond:7614, Score:2376.824, sT:42719, sP:3.4908, sB:0.7802, nT:130414, nP:4.1089, nB:0.8754 , T2:6130,6385,1395,65,6128,1575,291,275,2695,110,7822,2485,5358,8321,677,8046,94,7598,5,2693,8368,5134,7838,49,5150,2277,7390,3566,7,7614,  #End#
LowScoreRank2 , T0:4887 , T1:
Cond:6130, Score:4227.925, sT:42318, sP:3.442, sB:0.7872, nT:127336, nP:3.9824, nB:0.8915
Cond:1395, Score:4194.246, sT:42631, sP:3.4993, sB:0.802, nT:129381, nP:4.1053, nB:0.8752
Cond:65, Score:4186.038, sT:42273, sP:3.5026, sB:0.8203, nT:127914, nP:4.1305, nB:0.8674
Cond:1575, Score:4172.561, sT:42556, sP:3.5064, sB:0.7949, nT:130374, nP:4.1226, nB:0.8762
Cond:6385, Score:4171.524, sT:37595, sP:3.2742, sB:0.822, nT:113274, nP:3.8153, nB:0.9365
Cond:275, Score:4171.146, sT:42967, sP:3.4892, sB:0.7856, nT:130723, nP:4.107, nB:0.8699
Cond:2695, Score:4167.878, sT:42210, sP:3.5204, sB:0.8018, nT:129553, nP:4.1363, nB:0.8832
Cond:291, Score:4165.13, sT:42496, sP:3.5032, sB:0.8152, nT:127781, nP:4.12, nB:0.8582
Cond:110, Score:4163.896, sT:41708, sP:3.4659, sB:0.7977, nT:127061, nP:4.0898, nB:0.8892
Cond:2485, Score:4163.283, sT:42765, sP:3.4939, sB:0.7853, nT:130793, nP:4.1093, nB:0.8724
Cond:7822, Score:4162.965, sT:42512, sP:3.4908, sB:0.786, nT:129627, nP:4.1075, nB:0.8824
Cond:5358, Score:4159.901, sT:42692, sP:3.4885, sB:0.7831, nT:130296, nP:4.1056, nB:0.8762
Cond:677, Score:4158.464, sT:42993, sP:3.4887, sB:0.7802, nT:131338, nP:4.107, nB:0.8674
Cond:8046, Score:4157.847, sT:42818, sP:3.4873, sB:0.78, nT:130720, nP:4.104, nB:0.8738
Cond:5, Score:4156.794, sT:43025, sP:3.4873, sB:0.7785, nT:131410, nP:4.1057, nB:0.8673
Cond:7838, Score:4155.293, sT:42897, sP:3.4886, sB:0.7782, nT:130999, nP:4.1062, nB:0.8726
Cond:2693, Score:4155.267, sT:42740, sP:3.498, sB:0.7855, nT:130753, nP:4.1152, nB:0.8713
Cond:8368, Score:4154.367, sT:42562, sP:3.4701, sB:0.7784, nT:129811, nP:4.079, nB:0.8758
Cond:5134, Score:4152.277, sT:42299, sP:3.493, sB:0.7882, nT:128983, nP:4.1099, nB:0.8837
Cond:5150, Score:4151.77, sT:42831, sP:3.4884, sB:0.7786, nT:130741, nP:4.1072, nB:0.8734
Cond:2277, Score:4151.696, sT:42834, sP:3.4935, sB:0.7807, nT:131025, nP:4.1084, nB:0.8703
Cond:7598, Score:4151.529, sT:41854, sP:3.4968, sB:0.7964, nT:127677, nP:4.1129, nB:0.8911
Cond:49, Score:4151.376, sT:42789, sP:3.4938, sB:0.7898, nT:130134, nP:4.1183, nB:0.8651
Cond:7, Score:4150.924, sT:43007, sP:3.4877, sB:0.777, nT:131354, nP:4.1064, nB:0.8676
Cond:3566, Score:4150.908, sT:42876, sP:3.4864, sB:0.7773, nT:130994, nP:4.1047, nB:0.8714
Cond:6128, Score:4150.227, sT:38665, sP:3.344, sB:0.8171, nT:111563, nP:3.7641, nB:0.9265
Cond:513, Score:4149.666, sT:42993, sP:3.4892, sB:0.7785, nT:131129, nP:4.1094, nB:0.8674
Cond:7614, Score:4149.618, sT:42719, sP:3.4908, sB:0.7802, nT:130414, nP:4.1089, nB:0.8754
Cond:1573, Score:4149.021, sT:42886, sP:3.4921, sB:0.7808, nT:131094, nP:4.1102, nB:0.8673
Cond:3, Score:4148.784, sT:43060, sP:3.4865, sB:0.7747, nT:131523, nP:4.1041, nB:0.867 , T2:6130,1395,65,1575,6385,275,2695,291,110,2485,7822,5358,677,8046,5,7838,2693,8368,5134,5150,2277,7598,49,7,3566,6128,513,7614,1573,3,  #End#
LowScoreRank3 , T0:4887 , T1:
Cond:6130, Score:7382.483, sT:42318, sP:3.442, sB:0.7872, nT:127336, nP:3.9824, nB:0.8915
Cond:1395, Score:7323.478, sT:42631, sP:3.4993, sB:0.802, nT:129381, nP:4.1053, nB:0.8752
Cond:65, Score:7301.732, sT:42273, sP:3.5026, sB:0.8203, nT:127914, nP:4.1305, nB:0.8674
Cond:1575, Score:7288.105, sT:42556, sP:3.5064, sB:0.7949, nT:130374, nP:4.1226, nB:0.8762
Cond:275, Score:7287.787, sT:42967, sP:3.4892, sB:0.7856, nT:130723, nP:4.107, nB:0.8699
Cond:2695, Score:7277.386, sT:42210, sP:3.5204, sB:0.8018, nT:129553, nP:4.1363, nB:0.8832
Cond:2485, Score:7274.03, sT:42765, sP:3.4939, sB:0.7853, nT:130793, nP:4.1093, nB:0.8724
Cond:7822, Score:7271.579, sT:42512, sP:3.4908, sB:0.786, nT:129627, nP:4.1075, nB:0.8824
Cond:5358, Score:7267.644, sT:42692, sP:3.4885, sB:0.7831, nT:130296, nP:4.1056, nB:0.8762
Cond:677, Score:7267.254, sT:42993, sP:3.4887, sB:0.7802, nT:131338, nP:4.107, nB:0.8674
Cond:291, Score:7265.458, sT:42496, sP:3.5032, sB:0.8152, nT:127781, nP:4.12, nB:0.8582
Cond:8046, Score:7265.328, sT:42818, sP:3.4873, sB:0.78, nT:130720, nP:4.104, nB:0.8738
Cond:110, Score:7265.112, sT:41708, sP:3.4659, sB:0.7977, nT:127061, nP:4.0898, nB:0.8892
Cond:5, Score:7264.759, sT:43025, sP:3.4873, sB:0.7785, nT:131410, nP:4.1057, nB:0.8673
Cond:7838, Score:7261.686, sT:42897, sP:3.4886, sB:0.7782, nT:130999, nP:4.1062, nB:0.8726
Cond:2693, Score:7259.7, sT:42740, sP:3.498, sB:0.7855, nT:130753, nP:4.1152, nB:0.8713
Cond:8368, Score:7257.391, sT:42562, sP:3.4701, sB:0.7784, nT:129811, nP:4.079, nB:0.8758
Cond:5150, Score:7254.876, sT:42831, sP:3.4884, sB:0.7786, nT:130741, nP:4.1072, nB:0.8734
Cond:2277, Score:7254.768, sT:42834, sP:3.4935, sB:0.7807, nT:131025, nP:4.1084, nB:0.8703
Cond:7, Score:7254.567, sT:43007, sP:3.4877, sB:0.777, nT:131354, nP:4.1064, nB:0.8676
Cond:3566, Score:7253.914, sT:42876, sP:3.4864, sB:0.7773, nT:130994, nP:4.1047, nB:0.8714
Cond:513, Score:7251.661, sT:42993, sP:3.4892, sB:0.7785, nT:131129, nP:4.1094, nB:0.8674
Cond:3, Score:7251.536, sT:43060, sP:3.4865, sB:0.7747, nT:131523, nP:4.1041, nB:0.867
Cond:5134, Score:7250.969, sT:42299, sP:3.493, sB:0.7882, nT:128983, nP:4.1099, nB:0.8837
Cond:49, Score:7250.435, sT:42789, sP:3.4938, sB:0.7898, nT:130134, nP:4.1183, nB:0.8651
Cond:7614, Score:7250.239, sT:42719, sP:3.4908, sB:0.7802, nT:130414, nP:4.1089, nB:0.8754
Cond:1266, Score:7249.993, sT:42906, sP:3.4923, sB:0.778, nT:131078, nP:4.1091, nB:0.8706
Cond:1573, Score:7249.93, sT:42886, sP:3.4921, sB:0.7808, nT:131094, nP:4.1102, nB:0.8673
Cond:675, Score:7249.758, sT:43021, sP:3.4876, sB:0.7759, nT:131413, nP:4.1057, nB:0.867
Cond:1141, Score:7249.581, sT:43014, sP:3.4861, sB:0.7763, nT:131349, nP:4.1038, nB:0.8662 , T2:6130,1395,65,1575,275,2695,2485,7822,5358,677,291,8046,110,5,7838,2693,8368,5150,2277,7,3566,513,3,5134,49,7614,1266,1573,675,1141,  #End#
LowScoreRank0 , T0:6469 , T1:
Cond:6130, Score:1388.874, sT:42377, sP:3.4422, sB:0.7859, nT:127544, nP:3.9838, nB:0.8901
Cond:6385, Score:1386.519, sT:37655, sP:3.2748, sB:0.8206, nT:113487, nP:3.8173, nB:0.9351
Cond:65, Score:1378.932, sT:42334, sP:3.5027, sB:0.8197, nT:128128, nP:4.1318, nB:0.8662
Cond:6128, Score:1378.63, sT:38718, sP:3.344, sB:0.8162, nT:111739, nP:3.766, nB:0.9248
Cond:1395, Score:1378.189, sT:42691, sP:3.4995, sB:0.8008, nT:129595, nP:4.1066, nB:0.874
Cond:110, Score:1371.451, sT:41747, sP:3.4669, sB:0.7983, nT:127205, nP:4.091, nB:0.888
Cond:291, Score:1371.205, sT:42557, sP:3.5033, sB:0.8139, nT:127995, nP:4.1213, nB:0.857
Cond:1575, Score:1370.296, sT:42616, sP:3.5065, sB:0.7938, nT:130588, nP:4.1239, nB:0.875
Cond:2695, Score:1369.928, sT:42270, sP:3.5206, sB:0.8009, nT:129765, nP:4.1376, nB:0.882
Cond:8321, Score:1369.223, sT:40573, sP:3.5056, sB:0.8138, nT:122968, nP:4.1176, nB:0.9278
Cond:275, Score:1368.932, sT:43027, sP:3.4894, sB:0.7845, nT:130937, nP:4.1083, nB:0.8687
Cond:94, Score:1367.684, sT:39874, sP:3.425, sB:0.8183, nT:121089, nP:4.0628, nB:0.9087
Cond:750, Score:1367.36, sT:38295, sP:3.3895, sB:0.835, nT:117648, nP:4.0356, nB:0.9241
Cond:7822, Score:1366.997, sT:42571, sP:3.491, sB:0.7849, nT:129841, nP:4.1088, nB:0.8812
Cond:2485, Score:1366.416, sT:42825, sP:3.494, sB:0.7842, nT:131007, nP:4.1106, nB:0.8712
Cond:5358, Score:1364.862, sT:42753, sP:3.4888, sB:0.7816, nT:130517, nP:4.107, nB:0.8747
Cond:7598, Score:1364.648, sT:41915, sP:3.497, sB:0.7945, nT:127891, nP:4.1142, nB:0.8898
Cond:8046, Score:1364.299, sT:42878, sP:3.4875, sB:0.7789, nT:130934, nP:4.1053, nB:0.8726
Cond:677, Score:1364.248, sT:43053, sP:3.4889, sB:0.7791, nT:131552, nP:4.1082, nB:0.8663
Cond:8368, Score:1363.924, sT:42622, sP:3.4703, sB:0.7773, nT:130029, nP:4.0804, nB:0.8747
Cond:2693, Score:1363.822, sT:42800, sP:3.4982, sB:0.7844, nT:130966, nP:4.1165, nB:0.87
Cond:49, Score:1363.533, sT:42849, sP:3.4939, sB:0.7887, nT:130348, nP:4.1195, nB:0.8639
Cond:5, Score:1363.461, sT:43085, sP:3.4875, sB:0.7774, nT:131624, nP:4.107, nB:0.8661
Cond:768, Score:1363.458, sT:40233, sP:3.5183, sB:0.8677, nT:113808, nP:4.1085, nB:0.8935
Cond:6122, Score:1363.423, sT:41777, sP:3.4573, sB:0.7913, nT:127691, nP:4.0898, nB:0.8801
Cond:5134, Score:1363.408, sT:42360, sP:3.4937, sB:0.7865, nT:129216, nP:4.1113, nB:0.8822
Cond:7838, Score:1363.161, sT:42957, sP:3.4888, sB:0.7771, nT:131213, nP:4.1074, nB:0.8714
Cond:4338, Score:1362.854, sT:40615, sP:3.3881, sB:0.7865, nT:118574, nP:3.8468, nB:0.8923
Cond:7390, Score:1362.409, sT:42458, sP:3.4973, sB:0.7861, nT:129727, nP:4.1176, nB:0.8788
Cond:2277, Score:1362.229, sT:42894, sP:3.4937, sB:0.7796, nT:131239, nP:4.1096, nB:0.8691 , T2:6130,6385,65,6128,1395,110,291,1575,2695,8321,275,94,750,7822,2485,5358,7598,8046,677,8368,2693,49,5,768,6122,5134,7838,4338,7390,2277,  #End#
LowScoreRank1 , T0:6469 , T1:
Cond:6130, Score:2421.714, sT:42377, sP:3.4422, sB:0.7859, nT:127544, nP:3.9838, nB:0.8901
Cond:6385, Score:2403.541, sT:37655, sP:3.2748, sB:0.8206, nT:113487, nP:3.8173, nB:0.9351
Cond:1395, Score:2402.988, sT:42691, sP:3.4995, sB:0.8008, nT:129595, nP:4.1066, nB:0.874
Cond:65, Score:2401.864, sT:42334, sP:3.5027, sB:0.8197, nT:128128, nP:4.1318, nB:0.8662
Cond:6128, Score:2390.712, sT:38718, sP:3.344, sB:0.8162, nT:111739, nP:3.766, nB:0.9248
Cond:1575, Score:2389.967, sT:42616, sP:3.5065, sB:0.7938, nT:130588, nP:4.1239, nB:0.875
Cond:110, Score:2389.313, sT:41747, sP:3.4669, sB:0.7983, nT:127205, nP:4.091, nB:0.888
Cond:291, Score:2388.539, sT:42557, sP:3.5033, sB:0.8139, nT:127995, nP:4.1213, nB:0.857
Cond:2695, Score:2388.472, sT:42270, sP:3.5206, sB:0.8009, nT:129765, nP:4.1376, nB:0.882
Cond:275, Score:2388.341, sT:43027, sP:3.4894, sB:0.7845, nT:130937, nP:4.1083, nB:0.8687
Cond:7822, Score:2384.317, sT:42571, sP:3.491, sB:0.7849, nT:129841, nP:4.1088, nB:0.8812
Cond:2485, Score:2383.907, sT:42825, sP:3.494, sB:0.7842, nT:131007, nP:4.1106, nB:0.8712
Cond:8321, Score:2382.54, sT:40573, sP:3.5056, sB:0.8138, nT:122968, nP:4.1176, nB:0.9278
Cond:5358, Score:2381.062, sT:42753, sP:3.4888, sB:0.7816, nT:130517, nP:4.107, nB:0.8747
Cond:677, Score:2380.676, sT:43053, sP:3.4889, sB:0.7791, nT:131552, nP:4.1082, nB:0.8663
Cond:8046, Score:2380.483, sT:42878, sP:3.4875, sB:0.7789, nT:130934, nP:4.1053, nB:0.8726
Cond:5, Score:2379.437, sT:43085, sP:3.4875, sB:0.7774, nT:131624, nP:4.107, nB:0.8661
Cond:2693, Score:2379.269, sT:42800, sP:3.4982, sB:0.7844, nT:130966, nP:4.1165, nB:0.87
Cond:8368, Score:2379.232, sT:42622, sP:3.4703, sB:0.7773, nT:130029, nP:4.0804, nB:0.8747
Cond:7838, Score:2378.765, sT:42957, sP:3.4888, sB:0.7771, nT:131213, nP:4.1074, nB:0.8714
Cond:7598, Score:2378.333, sT:41915, sP:3.497, sB:0.7945, nT:127891, nP:4.1142, nB:0.8898
Cond:49, Score:2378.009, sT:42849, sP:3.4939, sB:0.7887, nT:130348, nP:4.1195, nB:0.8639
Cond:5134, Score:2377.45, sT:42360, sP:3.4937, sB:0.7865, nT:129216, nP:4.1113, nB:0.8822
Cond:2277, Score:2376.922, sT:42894, sP:3.4937, sB:0.7796, nT:131239, nP:4.1096, nB:0.8691
Cond:94, Score:2376.851, sT:39874, sP:3.425, sB:0.8183, nT:121089, nP:4.0628, nB:0.9087
Cond:7, Score:2376.052, sT:43067, sP:3.4879, sB:0.7759, nT:131568, nP:4.1077, nB:0.8664
Cond:7390, Score:2375.982, sT:42458, sP:3.4973, sB:0.7861, nT:129727, nP:4.1176, nB:0.8788
Cond:7614, Score:2375.862, sT:42779, sP:3.491, sB:0.779, nT:130628, nP:4.1101, nB:0.8742
Cond:5150, Score:2375.775, sT:42893, sP:3.4888, sB:0.7772, nT:130966, nP:4.1085, nB:0.8716
Cond:513, Score:2375.764, sT:43053, sP:3.4894, sB:0.7775, nT:131343, nP:4.1106, nB:0.8662 , T2:6130,6385,1395,65,6128,1575,110,291,2695,275,7822,2485,8321,5358,677,8046,5,2693,8368,7838,7598,49,5134,2277,94,7,7390,7614,5150,513,  #End#
LowScoreRank2 , T0:6469 , T1:
Cond:6130, Score:4225.768, sT:42377, sP:3.4422, sB:0.7859, nT:127544, nP:3.9838, nB:0.8901
Cond:1395, Score:4192.954, sT:42691, sP:3.4995, sB:0.8008, nT:129595, nP:4.1066, nB:0.874
Cond:65, Score:4186.737, sT:42334, sP:3.5027, sB:0.8197, nT:128128, nP:4.1318, nB:0.8662
Cond:1575, Score:4171.59, sT:42616, sP:3.5065, sB:0.7938, nT:130588, nP:4.1239, nB:0.875
Cond:275, Score:4170.026, sT:43027, sP:3.4894, sB:0.7845, nT:130937, nP:4.1083, nB:0.8687
Cond:6385, Score:4169.666, sT:37655, sP:3.2748, sB:0.8206, nT:113487, nP:3.8173, nB:0.9351
Cond:2695, Score:4167.504, sT:42270, sP:3.5206, sB:0.8009, nT:129765, nP:4.1376, nB:0.882
Cond:110, Score:4165.764, sT:41747, sP:3.4669, sB:0.7983, nT:127205, nP:4.091, nB:0.888
Cond:291, Score:4163.706, sT:42557, sP:3.5033, sB:0.8139, nT:127995, nP:4.1213, nB:0.857
Cond:2485, Score:4162.239, sT:42825, sP:3.494, sB:0.7842, nT:131007, nP:4.1106, nB:0.8712
Cond:7822, Score:4161.888, sT:42571, sP:3.491, sB:0.7849, nT:129841, nP:4.1088, nB:0.8812
Cond:677, Score:4157.553, sT:43053, sP:3.4889, sB:0.7791, nT:131552, nP:4.1082, nB:0.8663
Cond:5358, Score:4157.03, sT:42753, sP:3.4888, sB:0.7816, nT:130517, nP:4.107, nB:0.8747
Cond:8046, Score:4156.727, sT:42878, sP:3.4875, sB:0.7789, nT:130934, nP:4.1053, nB:0.8726
Cond:5, Score:4155.623, sT:43085, sP:3.4875, sB:0.7774, nT:131624, nP:4.107, nB:0.8661
Cond:7838, Score:4154.193, sT:42957, sP:3.4888, sB:0.7771, nT:131213, nP:4.1074, nB:0.8714
Cond:2693, Score:4153.949, sT:42800, sP:3.4982, sB:0.7844, nT:130966, nP:4.1165, nB:0.87
Cond:8368, Score:4153.494, sT:42622, sP:3.4703, sB:0.7773, nT:130029, nP:4.0804, nB:0.8747
Cond:2277, Score:4150.604, sT:42894, sP:3.4937, sB:0.7796, nT:131239, nP:4.1096, nB:0.8691
Cond:49, Score:4150.385, sT:42849, sP:3.4939, sB:0.7887, nT:130348, nP:4.1195, nB:0.8639
Cond:7, Score:4149.751, sT:43067, sP:3.4879, sB:0.7759, nT:131568, nP:4.1077, nB:0.8664
Cond:8321, Score:4148.901, sT:40573, sP:3.5056, sB:0.8138, nT:122968, nP:4.1176, nB:0.9278
Cond:5134, Score:4148.846, sT:42360, sP:3.4937, sB:0.7865, nT:129216, nP:4.1113, nB:0.8822
Cond:513, Score:4148.845, sT:43053, sP:3.4894, sB:0.7775, nT:131343, nP:4.1106, nB:0.8662
Cond:3566, Score:4148.777, sT:42934, sP:3.4869, sB:0.7761, nT:131211, nP:4.1061, nB:0.87
Cond:6128, Score:4148.634, sT:38718, sP:3.344, sB:0.8162, nT:111739, nP:3.766, nB:0.9248
Cond:5150, Score:4148.601, sT:42893, sP:3.4888, sB:0.7772, nT:130966, nP:4.1085, nB:0.8716
Cond:7614, Score:4148.26, sT:42779, sP:3.491, sB:0.779, nT:130628, nP:4.1101, nB:0.8742
Cond:7598, Score:4148.151, sT:41915, sP:3.497, sB:0.7945, nT:127891, nP:4.1142, nB:0.8898
Cond:1573, Score:4147.934, sT:42946, sP:3.4922, sB:0.7797, nT:131308, nP:4.1115, nB:0.8661 , T2:6130,1395,65,1575,275,6385,2695,110,291,2485,7822,677,5358,8046,5,7838,2693,8368,2277,49,7,8321,5134,513,3566,6128,5150,7614,7598,1573,  #End#
LowScoreRank3 , T0:6469 , T1:
Cond:6130, Score:7379.273, sT:42377, sP:3.4422, sB:0.7859, nT:127544, nP:3.9838, nB:0.8901
Cond:1395, Score:7321.787, sT:42691, sP:3.4995, sB:0.8008, nT:129595, nP:4.1066, nB:0.874
Cond:65, Score:7303.444, sT:42334, sP:3.5027, sB:0.8197, nT:128128, nP:4.1318, nB:0.8662
Cond:1575, Score:7286.956, sT:42616, sP:3.5065, sB:0.7938, nT:130588, nP:4.1239, nB:0.875
Cond:275, Score:7286.376, sT:43027, sP:3.4894, sB:0.7845, nT:130937, nP:4.1083, nB:0.8687
Cond:2695, Score:7277.255, sT:42270, sP:3.5206, sB:0.8009, nT:129765, nP:4.1376, nB:0.882
Cond:2485, Score:7272.751, sT:42825, sP:3.494, sB:0.7842, nT:131007, nP:4.1106, nB:0.8712
Cond:7822, Score:7270.25, sT:42571, sP:3.491, sB:0.7849, nT:129841, nP:4.1088, nB:0.8812
Cond:110, Score:7268.549, sT:41747, sP:3.4669, sB:0.7983, nT:127205, nP:4.091, nB:0.888
Cond:677, Score:7266.22, sT:43053, sP:3.4889, sB:0.7791, nT:131552, nP:4.1082, nB:0.8663
Cond:8046, Score:7263.919, sT:42878, sP:3.4875, sB:0.7789, nT:130934, nP:4.1053, nB:0.8726
Cond:291, Score:7263.545, sT:42557, sP:3.5033, sB:0.8139, nT:127995, nP:4.1213, nB:0.857
Cond:5, Score:7263.256, sT:43085, sP:3.4875, sB:0.7774, nT:131624, nP:4.107, nB:0.8661
Cond:5358, Score:7263.21, sT:42753, sP:3.4888, sB:0.7816, nT:130517, nP:4.107, nB:0.8747
Cond:7838, Score:7260.311, sT:42957, sP:3.4888, sB:0.7771, nT:131213, nP:4.1074, nB:0.8714
Cond:2693, Score:7257.931, sT:42800, sP:3.4982, sB:0.7844, nT:130966, nP:4.1165, nB:0.87
Cond:8368, Score:7256.435, sT:42622, sP:3.4703, sB:0.7773, nT:130029, nP:4.0804, nB:0.8747
Cond:2277, Score:7253.407, sT:42894, sP:3.4937, sB:0.7796, nT:131239, nP:4.1096, nB:0.8691
Cond:7, Score:7253.061, sT:43067, sP:3.4879, sB:0.7759, nT:131568, nP:4.1077, nB:0.8664
Cond:513, Score:7250.759, sT:43053, sP:3.4894, sB:0.7775, nT:131343, nP:4.1106, nB:0.8662
Cond:3566, Score:7250.732, sT:42934, sP:3.4869, sB:0.7761, nT:131211, nP:4.1061, nB:0.87
Cond:3, Score:7249.998, sT:43120, sP:3.4867, sB:0.7736, nT:131737, nP:4.1054, nB:0.8658
Cond:5150, Score:7249.885, sT:42893, sP:3.4888, sB:0.7772, nT:130966, nP:4.1085, nB:0.8716
Cond:49, Score:7249.25, sT:42849, sP:3.4939, sB:0.7887, nT:130348, nP:4.1195, nB:0.8639
Cond:1266, Score:7249.063, sT:42964, sP:3.4924, sB:0.7771, nT:131290, nP:4.1104, nB:0.8693
Cond:1573, Score:7248.572, sT:42946, sP:3.4922, sB:0.7797, nT:131308, nP:4.1115, nB:0.8661
Cond:7614, Score:7248.429, sT:42779, sP:3.491, sB:0.779, nT:130628, nP:4.1101, nB:0.8742
Cond:675, Score:7248.335, sT:43081, sP:3.4877, sB:0.7748, nT:131627, nP:4.107, nB:0.8658
Cond:1141, Score:7248.062, sT:43074, sP:3.4863, sB:0.7752, nT:131563, nP:4.1051, nB:0.865
Cond:5582, Score:7247.071, sT:43006, sP:3.486, sB:0.7741, nT:131416, nP:4.1045, nB:0.8684 , T2:6130,1395,65,1575,275,2695,2485,7822,110,677,8046,291,5,5358,7838,2693,8368,2277,7,513,3566,3,5150,49,1266,1573,7614,675,1141,5582,  #End#
LowScoreRank0 , T0:8130 , T1:
Cond:6130, Score:1389.416, sT:42313, sP:3.4399, sB:0.787, nT:127278, nP:3.9794, nB:0.8904
Cond:6385, Score:1387.1, sT:37590, sP:3.2719, sB:0.8218, nT:113216, nP:3.8118, nB:0.9353
Cond:6128, Score:1379.461, sT:38661, sP:3.3417, sB:0.8172, nT:111515, nP:3.7605, nB:0.9254
Cond:1395, Score:1378.425, sT:42626, sP:3.4973, sB:0.8018, nT:129323, nP:4.1023, nB:0.8741
Cond:65, Score:1377.966, sT:42268, sP:3.5005, sB:0.8196, nT:127851, nP:4.1277, nB:0.8663
Cond:110, Score:1371.416, sT:41710, sP:3.4641, sB:0.7982, nT:127055, nP:4.0871, nB:0.8881
Cond:291, Score:1371.174, sT:42491, sP:3.5012, sB:0.8147, nT:127723, nP:4.1171, nB:0.8571
Cond:94, Score:1370.798, sT:39864, sP:3.4222, sB:0.8205, nT:121058, nP:4.0592, nB:0.909
Cond:1575, Score:1369.79, sT:42552, sP:3.5042, sB:0.7939, nT:130299, nP:4.1201, nB:0.8755
Cond:750, Score:1369.515, sT:38292, sP:3.3872, sB:0.8362, nT:117624, nP:4.0313, nB:0.9243
Cond:2695, Score:1369.481, sT:42191, sP:3.519, sB:0.8013, nT:129444, nP:4.1345, nB:0.883
Cond:275, Score:1369.084, sT:42962, sP:3.4872, sB:0.7854, nT:130665, nP:4.1041, nB:0.8688
Cond:8321, Score:1368.933, sT:40515, sP:3.5037, sB:0.8143, nT:122730, nP:4.1133, nB:0.9278
Cond:7822, Score:1367.158, sT:42507, sP:3.4887, sB:0.7858, nT:129569, nP:4.1046, nB:0.8813
Cond:2485, Score:1366.02, sT:42763, sP:3.4916, sB:0.7844, nT:130729, nP:4.1065, nB:0.8715
Cond:7598, Score:1365.84, sT:41850, sP:3.4947, sB:0.7964, nT:127619, nP:4.1099, nB:0.89
Cond:5358, Score:1365.209, sT:42688, sP:3.4866, sB:0.7827, nT:130245, nP:4.1028, nB:0.8748
Cond:2693, Score:1364.52, sT:42731, sP:3.4961, sB:0.7856, nT:130674, nP:4.1128, nB:0.8708
Cond:8046, Score:1364.468, sT:42813, sP:3.4852, sB:0.7798, nT:130662, nP:4.1011, nB:0.8727
Cond:677, Score:1364.396, sT:42988, sP:3.4867, sB:0.78, nT:131280, nP:4.1041, nB:0.8664
Cond:5134, Score:1364.169, sT:42296, sP:3.4914, sB:0.788, nT:128944, nP:4.1071, nB:0.8823
Cond:8368, Score:1364.088, sT:42557, sP:3.468, sB:0.7782, nT:129757, nP:4.0762, nB:0.8748
Cond:5, Score:1363.624, sT:43020, sP:3.4853, sB:0.7783, nT:131352, nP:4.1028, nB:0.8662
Cond:7838, Score:1363.304, sT:42892, sP:3.4866, sB:0.778, nT:130941, nP:4.1033, nB:0.8715
Cond:49, Score:1363.266, sT:42785, sP:3.4917, sB:0.7892, nT:130076, nP:4.1154, nB:0.864
Cond:6122, Score:1363.004, sT:41718, sP:3.4544, sB:0.7916, nT:127456, nP:4.0856, nB:0.8798
Cond:768, Score:1362.927, sT:40157, sP:3.5176, sB:0.8684, nT:113538, nP:4.1043, nB:0.894
Cond:7390, Score:1362.837, sT:42395, sP:3.4951, sB:0.7872, nT:129458, nP:4.1134, nB:0.879
Cond:1662, Score:1362.442, sT:41087, sP:3.4484, sB:0.793, nT:126204, nP:4.0735, nB:0.8935
Cond:1646, Score:1362.317, sT:39084, sP:3.4126, sB:0.8151, nT:120541, nP:4.0453, nB:0.9203 , T2:6130,6385,6128,1395,65,110,291,94,1575,750,2695,275,8321,7822,2485,7598,5358,2693,8046,677,5134,8368,5,7838,49,6122,768,7390,1662,1646,  #End#
LowScoreRank1 , T0:8130 , T1:
Cond:6130, Score:2422.411, sT:42313, sP:3.4399, sB:0.787, nT:127278, nP:3.9794, nB:0.8904
Cond:6385, Score:2404.268, sT:37590, sP:3.2719, sB:0.8218, nT:113216, nP:3.8118, nB:0.9353
Cond:1395, Score:2403.149, sT:42626, sP:3.4973, sB:0.8018, nT:129323, nP:4.1023, nB:0.8741
Cond:65, Score:2399.968, sT:42268, sP:3.5005, sB:0.8196, nT:127851, nP:4.1277, nB:0.8663
Cond:6128, Score:2391.942, sT:38661, sP:3.3417, sB:0.8172, nT:111515, nP:3.7605, nB:0.9254
Cond:110, Score:2389.137, sT:41710, sP:3.4641, sB:0.7982, nT:127055, nP:4.0871, nB:0.8881
Cond:1575, Score:2388.876, sT:42552, sP:3.5042, sB:0.7939, nT:130299, nP:4.1201, nB:0.8755
Cond:275, Score:2388.363, sT:42962, sP:3.4872, sB:0.7854, nT:130665, nP:4.1041, nB:0.8688
Cond:291, Score:2388.244, sT:42491, sP:3.5012, sB:0.8147, nT:127723, nP:4.1171, nB:0.8571
Cond:2695, Score:2387.455, sT:42191, sP:3.519, sB:0.8013, nT:129444, nP:4.1345, nB:0.883
Cond:7822, Score:2384.353, sT:42507, sP:3.4887, sB:0.7858, nT:129569, nP:4.1046, nB:0.8813
Cond:2485, Score:2383.006, sT:42763, sP:3.4916, sB:0.7844, nT:130729, nP:4.1065, nB:0.8715
Cond:94, Score:2382.16, sT:39864, sP:3.4222, sB:0.8205, nT:121058, nP:4.0592, nB:0.909
Cond:8321, Score:2381.823, sT:40515, sP:3.5037, sB:0.8143, nT:122730, nP:4.1133, nB:0.9278
Cond:5358, Score:2381.415, sT:42688, sP:3.4866, sB:0.7827, nT:130245, nP:4.1028, nB:0.8748
Cond:677, Score:2380.691, sT:42988, sP:3.4867, sB:0.78, nT:131280, nP:4.1041, nB:0.8664
Cond:8046, Score:2380.535, sT:42813, sP:3.4852, sB:0.7798, nT:130662, nP:4.1011, nB:0.8727
Cond:2693, Score:2380.239, sT:42731, sP:3.4961, sB:0.7856, nT:130674, nP:4.1128, nB:0.8708
Cond:7598, Score:2380.124, sT:41850, sP:3.4947, sB:0.7964, nT:127619, nP:4.1099, nB:0.89
Cond:5, Score:2379.479, sT:43020, sP:3.4853, sB:0.7783, nT:131352, nP:4.1028, nB:0.8662
Cond:8368, Score:2379.271, sT:42557, sP:3.468, sB:0.7782, nT:129757, nP:4.0762, nB:0.8748
Cond:7838, Score:2378.77, sT:42892, sP:3.4866, sB:0.778, nT:130941, nP:4.1033, nB:0.8715
Cond:5134, Score:2378.507, sT:42296, sP:3.4914, sB:0.788, nT:128944, nP:4.1071, nB:0.8823
Cond:49, Score:2377.316, sT:42785, sP:3.4917, sB:0.7892, nT:130076, nP:4.1154, nB:0.864
Cond:2277, Score:2376.591, sT:42830, sP:3.4913, sB:0.7802, nT:130965, nP:4.1055, nB:0.8693
Cond:7390, Score:2376.481, sT:42395, sP:3.4951, sB:0.7872, nT:129458, nP:4.1134, nB:0.879
Cond:7, Score:2376.094, sT:43002, sP:3.4857, sB:0.7768, nT:131296, nP:4.1035, nB:0.8665
Cond:7614, Score:2376.022, sT:42714, sP:3.4888, sB:0.78, nT:130356, nP:4.106, nB:0.8743
Cond:750, Score:2375.842, sT:38292, sP:3.3872, sB:0.8362, nT:117624, nP:4.0313, nB:0.9243
Cond:513, Score:2375.812, sT:42988, sP:3.4871, sB:0.7784, nT:131071, nP:4.1065, nB:0.8663 , T2:6130,6385,1395,65,6128,110,1575,275,291,2695,7822,2485,94,8321,5358,677,8046,2693,7598,5,8368,7838,5134,49,2277,7390,7,7614,750,513,  #End#
LowScoreRank2 , T0:8130 , T1:
Cond:6130, Score:4226.547, sT:42313, sP:3.4399, sB:0.787, nT:127278, nP:3.9794, nB:0.8904
Cond:1395, Score:4192.796, sT:42626, sP:3.4973, sB:0.8018, nT:129323, nP:4.1023, nB:0.8741
Cond:65, Score:4183.059, sT:42268, sP:3.5005, sB:0.8196, nT:127851, nP:4.1277, nB:0.8663
Cond:6385, Score:4170.436, sT:37590, sP:3.2719, sB:0.8218, nT:113216, nP:3.8118, nB:0.9353
Cond:275, Score:4169.634, sT:42962, sP:3.4872, sB:0.7854, nT:130665, nP:4.1041, nB:0.8688
Cond:1575, Score:4169.317, sT:42552, sP:3.5042, sB:0.7939, nT:130299, nP:4.1201, nB:0.8755
Cond:2695, Score:4165.308, sT:42191, sP:3.519, sB:0.8013, nT:129444, nP:4.1345, nB:0.883
Cond:110, Score:4165.255, sT:41710, sP:3.4641, sB:0.7982, nT:127055, nP:4.0871, nB:0.8881
Cond:291, Score:4162.768, sT:42491, sP:3.5012, sB:0.8147, nT:127723, nP:4.1171, nB:0.8571
Cond:7822, Score:4161.518, sT:42507, sP:3.4887, sB:0.7858, nT:129569, nP:4.1046, nB:0.8813
Cond:2485, Score:4160.296, sT:42763, sP:3.4916, sB:0.7844, nT:130729, nP:4.1065, nB:0.8715
Cond:5358, Score:4157.201, sT:42688, sP:3.4866, sB:0.7827, nT:130245, nP:4.1028, nB:0.8748
Cond:677, Score:4157.15, sT:42988, sP:3.4867, sB:0.78, nT:131280, nP:4.1041, nB:0.8664
Cond:8046, Score:4156.386, sT:42813, sP:3.4852, sB:0.7798, nT:130662, nP:4.1011, nB:0.8727
Cond:5, Score:4155.269, sT:43020, sP:3.4853, sB:0.7783, nT:131352, nP:4.1028, nB:0.8662
Cond:2693, Score:4155.205, sT:42731, sP:3.4961, sB:0.7856, nT:130674, nP:4.1128, nB:0.8708
Cond:7838, Score:4153.772, sT:42892, sP:3.4866, sB:0.778, nT:130941, nP:4.1033, nB:0.8715
Cond:8368, Score:4153.129, sT:42557, sP:3.468, sB:0.7782, nT:129757, nP:4.0762, nB:0.8748
Cond:7598, Score:4150.774, sT:41850, sP:3.4947, sB:0.7964, nT:127619, nP:4.1099, nB:0.89
Cond:6128, Score:4150.4, sT:38661, sP:3.3417, sB:0.8172, nT:111515, nP:3.7605, nB:0.9254
Cond:5134, Score:4150.213, sT:42296, sP:3.4914, sB:0.788, nT:128944, nP:4.1071, nB:0.8823
Cond:2277, Score:4149.622, sT:42830, sP:3.4913, sB:0.7802, nT:130965, nP:4.1055, nB:0.8693
Cond:7, Score:4149.397, sT:43002, sP:3.4857, sB:0.7768, nT:131296, nP:4.1035, nB:0.8665
Cond:49, Score:4148.777, sT:42785, sP:3.4917, sB:0.7892, nT:130076, nP:4.1154, nB:0.864
Cond:3566, Score:4148.503, sT:42870, sP:3.4846, sB:0.777, nT:130939, nP:4.1019, nB:0.8701
Cond:513, Score:4148.499, sT:42988, sP:3.4871, sB:0.7784, nT:131071, nP:4.1065, nB:0.8663
Cond:5150, Score:4148.17, sT:42828, sP:3.4866, sB:0.7781, nT:130694, nP:4.1044, nB:0.8717
Cond:7614, Score:4148.101, sT:42714, sP:3.4888, sB:0.78, nT:130356, nP:4.106, nB:0.8743
Cond:3, Score:4147.464, sT:43055, sP:3.4845, sB:0.7745, nT:131465, nP:4.1012, nB:0.866
Cond:8321, Score:4147.28, sT:40515, sP:3.5037, sB:0.8143, nT:122730, nP:4.1133, nB:0.9278 , T2:6130,1395,65,6385,275,1575,2695,110,291,7822,2485,5358,677,8046,5,2693,7838,8368,7598,6128,5134,2277,7,49,3566,513,5150,7614,3,8321,  #End#
LowScoreRank3 , T0:8130 , T1:
Cond:6130, Score:7379.866, sT:42313, sP:3.4399, sB:0.787, nT:127278, nP:3.9794, nB:0.8904
Cond:1395, Score:7320.737, sT:42626, sP:3.4973, sB:0.8018, nT:129323, nP:4.1023, nB:0.8741
Cond:65, Score:7296.371, sT:42268, sP:3.5005, sB:0.8196, nT:127851, nP:4.1277, nB:0.8663
Cond:275, Score:7284.935, sT:42962, sP:3.4872, sB:0.7854, nT:130665, nP:4.1041, nB:0.8688
Cond:1575, Score:7282.332, sT:42552, sP:3.5042, sB:0.7939, nT:130299, nP:4.1201, nB:0.8755
Cond:2695, Score:7272.683, sT:42191, sP:3.519, sB:0.8013, nT:129444, nP:4.1345, nB:0.883
Cond:7822, Score:7268.84, sT:42507, sP:3.4887, sB:0.7858, nT:129569, nP:4.1046, nB:0.8813
Cond:2485, Score:7268.701, sT:42763, sP:3.4916, sB:0.7844, nT:130729, nP:4.1065, nB:0.8715
Cond:110, Score:7267.306, sT:41710, sP:3.4641, sB:0.7982, nT:127055, nP:4.0871, nB:0.8881
Cond:677, Score:7264.76, sT:42988, sP:3.4867, sB:0.78, nT:131280, nP:4.1041, nB:0.8664
Cond:5358, Score:7262.724, sT:42688, sP:3.4866, sB:0.7827, nT:130245, nP:4.1028, nB:0.8748
Cond:8046, Score:7262.564, sT:42813, sP:3.4852, sB:0.7798, nT:130662, nP:4.1011, nB:0.8727
Cond:5, Score:7261.884, sT:43020, sP:3.4853, sB:0.7783, nT:131352, nP:4.1028, nB:0.8662
Cond:291, Score:7261.162, sT:42491, sP:3.5012, sB:0.8147, nT:127723, nP:4.1171, nB:0.8571
Cond:2693, Score:7259.354, sT:42731, sP:3.4961, sB:0.7856, nT:130674, nP:4.1128, nB:0.8708
Cond:7838, Score:7258.817, sT:42892, sP:3.4866, sB:0.778, nT:130941, nP:4.1033, nB:0.8715
Cond:8368, Score:7255.033, sT:42557, sP:3.468, sB:0.7782, nT:129757, nP:4.0762, nB:0.8748
Cond:7, Score:7251.69, sT:43002, sP:3.4857, sB:0.7768, nT:131296, nP:4.1035, nB:0.8665
Cond:2277, Score:7250.978, sT:42830, sP:3.4913, sB:0.7802, nT:130965, nP:4.1055, nB:0.8693
Cond:3566, Score:7249.498, sT:42870, sP:3.4846, sB:0.777, nT:130939, nP:4.1019, nB:0.8701
Cond:513, Score:7249.397, sT:42988, sP:3.4871, sB:0.7784, nT:131071, nP:4.1065, nB:0.8663
Cond:3, Score:7249.031, sT:43055, sP:3.4845, sB:0.7745, nT:131465, nP:4.1012, nB:0.866
Cond:5150, Score:7248.374, sT:42828, sP:3.4866, sB:0.7781, nT:130694, nP:4.1044, nB:0.8717
Cond:7614, Score:7247.38, sT:42714, sP:3.4888, sB:0.78, nT:130356, nP:4.106, nB:0.8743
Cond:5134, Score:7247.172, sT:42296, sP:3.4914, sB:0.788, nT:128944, nP:4.1071, nB:0.8823
Cond:675, Score:7246.975, sT:43016, sP:3.4855, sB:0.7757, nT:131355, nP:4.1028, nB:0.8659
Cond:1141, Score:7246.63, sT:43009, sP:3.4841, sB:0.7761, nT:131291, nP:4.101, nB:0.8651
Cond:1266, Score:7245.933, sT:42899, sP:3.4902, sB:0.7774, nT:131014, nP:4.1063, nB:0.8698
Cond:5582, Score:7245.797, sT:42942, sP:3.4837, sB:0.775, nT:131144, nP:4.1004, nB:0.8685
Cond:49, Score:7245.738, sT:42785, sP:3.4917, sB:0.7892, nT:130076, nP:4.1154, nB:0.864 , T2:6130,1395,65,275,1575,2695,7822,2485,110,677,5358,8046,5,291,2693,7838,8368,7,2277,3566,513,3,5150,7614,5134,675,1141,1266,5582,49,  #End#
LowScoreRank0 , T0:1973 , T1:
Cond:6130, Score:1388.804, sT:42409, sP:3.44, sB:0.7838, nT:127629, nP:3.9803, nB:0.8908
Cond:6385, Score:1385.667, sT:37684, sP:3.2725, sB:0.8176, nT:113567, nP:3.8133, nB:0.9356
Cond:65, Score:1378.946, sT:42362, sP:3.5006, sB:0.8179, nT:128203, nP:4.128, nB:0.8667
Cond:6128, Score:1378.613, sT:38758, sP:3.342, sB:0.8135, nT:111865, nP:3.7622, nB:0.9258
Cond:1395, Score:1377.539, sT:42722, sP:3.4973, sB:0.7983, nT:129674, nP:4.1029, nB:0.8745
Cond:110, Score:1371.759, sT:41795, sP:3.464, sB:0.7961, nT:127358, nP:4.0879, nB:0.8886
Cond:291, Score:1371.292, sT:42586, sP:3.5012, sB:0.8121, nT:128074, nP:4.1175, nB:0.8576
Cond:94, Score:1370.932, sT:39933, sP:3.4225, sB:0.8185, nT:121320, nP:4.0604, nB:0.9096
Cond:1575, Score:1368.627, sT:42649, sP:3.5041, sB:0.7902, nT:130664, nP:4.1203, nB:0.8756
Cond:275, Score:1368.546, sT:43058, sP:3.4872, sB:0.7823, nT:131016, nP:4.1047, nB:0.8692
Cond:750, Score:1367.863, sT:38367, sP:3.3869, sB:0.8325, nT:117891, nP:4.0324, nB:0.9241
Cond:2695, Score:1367.495, sT:42290, sP:3.5184, sB:0.797, nT:129800, nP:4.1346, nB:0.8825
Cond:7822, Score:1366.577, sT:42603, sP:3.4887, sB:0.7826, nT:129920, nP:4.1051, nB:0.8817
Cond:2485, Score:1365.316, sT:42858, sP:3.4917, sB:0.7812, nT:131082, nP:4.107, nB:0.8718
Cond:7598, Score:1365.055, sT:41945, sP:3.4947, sB:0.793, nT:127970, nP:4.1104, nB:0.8903
Cond:5358, Score:1364.704, sT:42784, sP:3.4866, sB:0.7796, nT:130596, nP:4.1033, nB:0.8752
Cond:8321, Score:1364.393, sT:40580, sP:3.5038, sB:0.8081, nT:122955, nP:4.1131, nB:0.9275
Cond:8046, Score:1363.928, sT:42909, sP:3.4853, sB:0.7767, nT:131013, nP:4.1016, nB:0.8731
Cond:677, Score:1363.856, sT:43084, sP:3.4867, sB:0.7769, nT:131631, nP:4.1046, nB:0.8668
Cond:8368, Score:1363.74, sT:42652, sP:3.4681, sB:0.7753, nT:130107, nP:4.0768, nB:0.8752
Cond:5134, Score:1363.709, sT:42392, sP:3.4914, sB:0.7849, nT:129295, nP:4.1076, nB:0.8827
Cond:49, Score:1363.418, sT:42879, sP:3.4918, sB:0.7868, nT:130425, nP:4.1159, nB:0.8644
Cond:768, Score:1363.193, sT:40255, sP:3.5171, sB:0.8652, nT:113843, nP:4.1048, nB:0.8953
Cond:5, Score:1363.078, sT:43116, sP:3.4853, sB:0.7752, nT:131703, nP:4.1033, nB:0.8666
Cond:7838, Score:1362.773, sT:42988, sP:3.4866, sB:0.7749, nT:131292, nP:4.1038, nB:0.8719
Cond:1662, Score:1362.637, sT:41173, sP:3.4484, sB:0.7906, nT:126524, nP:4.0742, nB:0.8941
Cond:6122, Score:1362.375, sT:41815, sP:3.4544, sB:0.7883, nT:127807, nP:4.0862, nB:0.8802
Cond:2693, Score:1362.202, sT:42828, sP:3.4957, sB:0.7811, nT:131029, nP:4.113, nB:0.8704
Cond:7390, Score:1362.199, sT:42491, sP:3.4951, sB:0.784, nT:129809, nP:4.1139, nB:0.8793
Cond:766, Score:1361.977, sT:40667, sP:3.4351, sB:0.7958, nT:124578, nP:4.0643, nB:0.9008 , T2:6130,6385,65,6128,1395,110,291,94,1575,275,750,2695,7822,2485,7598,5358,8321,8046,677,8368,5134,49,768,5,7838,1662,6122,2693,7390,766,  #End#
LowScoreRank1 , T0:1973 , T1:
Cond:6130, Score:2421.797, sT:42409, sP:3.44, sB:0.7838, nT:127629, nP:3.9803, nB:0.8908
Cond:6385, Score:2402.3, sT:37684, sP:3.2725, sB:0.8176, nT:113567, nP:3.8133, nB:0.9356
Cond:1395, Score:2402.062, sT:42722, sP:3.4973, sB:0.7983, nT:129674, nP:4.1029, nB:0.8745
Cond:65, Score:2402.06, sT:42362, sP:3.5006, sB:0.8179, nT:128203, nP:4.128, nB:0.8667
Cond:6128, Score:2390.962, sT:38758, sP:3.342, sB:0.8135, nT:111865, nP:3.7622, nB:0.9258
Cond:110, Score:2390.101, sT:41795, sP:3.464, sB:0.7961, nT:127358, nP:4.0879, nB:0.8886
Cond:291, Score:2388.869, sT:42586, sP:3.5012, sB:0.8121, nT:128074, nP:4.1175, nB:0.8576
Cond:275, Score:2387.862, sT:43058, sP:3.4872, sB:0.7823, nT:131016, nP:4.1047, nB:0.8692
Cond:1575, Score:2387.312, sT:42649, sP:3.5041, sB:0.7902, nT:130664, nP:4.1203, nB:0.8756
Cond:2695, Score:2384.453, sT:42290, sP:3.5184, sB:0.797, nT:129800, nP:4.1346, nB:0.8825
Cond:7822, Score:2383.784, sT:42603, sP:3.4887, sB:0.7826, nT:129920, nP:4.1051, nB:0.8817
Cond:94, Score:2382.724, sT:39933, sP:3.4225, sB:0.8185, nT:121320, nP:4.0604, nB:0.9096
Cond:2485, Score:2382.218, sT:42858, sP:3.4917, sB:0.7812, nT:131082, nP:4.107, nB:0.8718
Cond:5358, Score:2380.974, sT:42784, sP:3.4866, sB:0.7796, nT:130596, nP:4.1033, nB:0.8752
Cond:677, Score:2380.186, sT:43084, sP:3.4867, sB:0.7769, nT:131631, nP:4.1046, nB:0.8668
Cond:8046, Score:2380.032, sT:42909, sP:3.4853, sB:0.7767, nT:131013, nP:4.1016, nB:0.8731
Cond:7598, Score:2379.207, sT:41945, sP:3.4947, sB:0.793, nT:127970, nP:4.1104, nB:0.8903
Cond:8368, Score:2379.096, sT:42652, sP:3.4681, sB:0.7753, nT:130107, nP:4.0768, nB:0.8752
Cond:5, Score:2378.964, sT:43116, sP:3.4853, sB:0.7752, nT:131703, nP:4.1033, nB:0.8666
Cond:7838, Score:2378.283, sT:42988, sP:3.4866, sB:0.7749, nT:131292, nP:4.1038, nB:0.8719
Cond:5134, Score:2378.148, sT:42392, sP:3.4914, sB:0.7849, nT:129295, nP:4.1076, nB:0.8827
Cond:49, Score:2377.988, sT:42879, sP:3.4918, sB:0.7868, nT:130425, nP:4.1159, nB:0.8644
Cond:2693, Score:2376.665, sT:42828, sP:3.4957, sB:0.7811, nT:131029, nP:4.113, nB:0.8704
Cond:7390, Score:2375.81, sT:42491, sP:3.4951, sB:0.784, nT:129809, nP:4.1139, nB:0.8793
Cond:2277, Score:2375.802, sT:42926, sP:3.4914, sB:0.777, nT:131318, nP:4.106, nB:0.8696
Cond:305, Score:2375.586, sT:43104, sP:3.4865, sB:0.7814, nT:131413, nP:4.1079, nB:0.8573
Cond:7614, Score:2375.569, sT:42810, sP:3.4888, sB:0.7769, nT:130707, nP:4.1065, nB:0.8747
Cond:7, Score:2375.556, sT:43098, sP:3.4857, sB:0.7737, nT:131647, nP:4.1041, nB:0.8669
Cond:1266, Score:2375.36, sT:42991, sP:3.4903, sB:0.7752, nT:131340, nP:4.1071, nB:0.8703
Cond:3566, Score:2375.299, sT:42966, sP:3.4846, sB:0.7739, nT:131290, nP:4.1024, nB:0.8705 , T2:6130,6385,1395,65,6128,110,291,275,1575,2695,7822,94,2485,5358,677,8046,7598,8368,5,7838,5134,49,2693,7390,2277,305,7614,7,1266,3566,  #End#
LowScoreRank2 , T0:1973 , T1:
Cond:6130, Score:4226.272, sT:42409, sP:3.44, sB:0.7838, nT:127629, nP:3.9803, nB:0.8908
Cond:1395, Score:4191.701, sT:42722, sP:3.4973, sB:0.7983, nT:129674, nP:4.1029, nB:0.8745
Cond:65, Score:4187.38, sT:42362, sP:3.5006, sB:0.8179, nT:128203, nP:4.128, nB:0.8667
Cond:275, Score:4169.531, sT:43058, sP:3.4872, sB:0.7823, nT:131016, nP:4.1047, nB:0.8692
Cond:6385, Score:4167.921, sT:37684, sP:3.2725, sB:0.8176, nT:113567, nP:3.8133, nB:0.9356
Cond:110, Score:4167.579, sT:41795, sP:3.464, sB:0.7961, nT:127358, nP:4.0879, nB:0.8886
Cond:1575, Score:4167.402, sT:42649, sP:3.5041, sB:0.7902, nT:130664, nP:4.1203, nB:0.8756
Cond:291, Score:4164.596, sT:42586, sP:3.5012, sB:0.8121, nT:128074, nP:4.1175, nB:0.8576
Cond:7822, Score:4161.309, sT:42603, sP:3.4887, sB:0.7826, nT:129920, nP:4.1051, nB:0.8817
Cond:2695, Score:4160.879, sT:42290, sP:3.5184, sB:0.797, nT:129800, nP:4.1346, nB:0.8825
Cond:2485, Score:4159.694, sT:42858, sP:3.4917, sB:0.7812, nT:131082, nP:4.107, nB:0.8718
Cond:5358, Score:4157.205, sT:42784, sP:3.4866, sB:0.7796, nT:130596, nP:4.1033, nB:0.8752
Cond:677, Score:4157.039, sT:43084, sP:3.4867, sB:0.7769, nT:131631, nP:4.1046, nB:0.8668
Cond:8046, Score:4156.282, sT:42909, sP:3.4853, sB:0.7767, nT:131013, nP:4.1016, nB:0.8731
Cond:5, Score:4155.141, sT:43116, sP:3.4853, sB:0.7752, nT:131703, nP:4.1033, nB:0.8666
Cond:7838, Score:4153.693, sT:42988, sP:3.4866, sB:0.7749, nT:131292, nP:4.1038, nB:0.8719
Cond:8368, Score:4153.583, sT:42652, sP:3.4681, sB:0.7753, nT:130107, nP:4.0768, nB:0.8752
Cond:49, Score:4150.663, sT:42879, sP:3.4918, sB:0.7868, nT:130425, nP:4.1159, nB:0.8644
Cond:5134, Score:4150.362, sT:42392, sP:3.4914, sB:0.7849, nT:129295, nP:4.1076, nB:0.8827
Cond:7598, Score:4149.965, sT:41945, sP:3.4947, sB:0.793, nT:127970, nP:4.1104, nB:0.8903
Cond:2693, Score:4149.789, sT:42828, sP:3.4957, sB:0.7811, nT:131029, nP:4.113, nB:0.8704
Cond:6128, Score:4149.554, sT:38758, sP:3.342, sB:0.8135, nT:111865, nP:3.7622, nB:0.9258
Cond:7, Score:4149.225, sT:43098, sP:3.4857, sB:0.7737, nT:131647, nP:4.1041, nB:0.8669
Cond:2277, Score:4149.019, sT:42926, sP:3.4914, sB:0.777, nT:131318, nP:4.106, nB:0.8696
Cond:1266, Score:4148.527, sT:42991, sP:3.4903, sB:0.7752, nT:131340, nP:4.1071, nB:0.8703
Cond:3566, Score:4148.415, sT:42966, sP:3.4846, sB:0.7739, nT:131290, nP:4.1024, nB:0.8705
Cond:513, Score:4148.274, sT:43083, sP:3.4872, sB:0.7753, nT:131422, nP:4.107, nB:0.8667
Cond:5150, Score:4148.104, sT:42924, sP:3.4866, sB:0.775, nT:131045, nP:4.1049, nB:0.8721
Cond:7614, Score:4148.082, sT:42810, sP:3.4888, sB:0.7769, nT:130707, nP:4.1065, nB:0.8747
Cond:305, Score:4147.818, sT:43104, sP:3.4865, sB:0.7814, nT:131413, nP:4.1079, nB:0.8573 , T2:6130,1395,65,275,6385,110,1575,291,7822,2695,2485,5358,677,8046,5,7838,8368,49,5134,7598,2693,6128,7,2277,1266,3566,513,5150,7614,305,  #End#
LowScoreRank3 , T0:1973 , T1:
Cond:6130, Score:7380.782, sT:42409, sP:3.44, sB:0.7838, nT:127629, nP:3.9803, nB:0.8908
Cond:1395, Score:7320.233, sT:42722, sP:3.4973, sB:0.7983, nT:129674, nP:4.1029, nB:0.8745
Cond:65, Score:7305.096, sT:42362, sP:3.5006, sB:0.8179, nT:128203, nP:4.128, nB:0.8667
Cond:275, Score:7286.109, sT:43058, sP:3.4872, sB:0.7823, nT:131016, nP:4.1047, nB:0.8692
Cond:1575, Score:7280.42, sT:42649, sP:3.5041, sB:0.7902, nT:130664, nP:4.1203, nB:0.8756
Cond:110, Score:7272.487, sT:41795, sP:3.464, sB:0.7961, nT:127358, nP:4.0879, nB:0.8886
Cond:7822, Score:7269.851, sT:42603, sP:3.4887, sB:0.7826, nT:129920, nP:4.1051, nB:0.8817
Cond:2485, Score:7269.01, sT:42858, sP:3.4917, sB:0.7812, nT:131082, nP:4.107, nB:0.8718
Cond:2695, Score:7266.368, sT:42290, sP:3.5184, sB:0.797, nT:129800, nP:4.1346, nB:0.8825
Cond:677, Score:7265.92, sT:43084, sP:3.4867, sB:0.7769, nT:131631, nP:4.1046, nB:0.8668
Cond:291, Score:7265.646, sT:42586, sP:3.5012, sB:0.8121, nT:128074, nP:4.1175, nB:0.8576
Cond:5358, Score:7264.09, sT:42784, sP:3.4866, sB:0.7796, nT:130596, nP:4.1033, nB:0.8752
Cond:8046, Score:7263.743, sT:42909, sP:3.4853, sB:0.7767, nT:131013, nP:4.1016, nB:0.8731
Cond:5, Score:7263.013, sT:43116, sP:3.4853, sB:0.7752, nT:131703, nP:4.1033, nB:0.8666
Cond:7838, Score:7260.035, sT:42988, sP:3.4866, sB:0.7749, nT:131292, nP:4.1038, nB:0.8719
Cond:8368, Score:7257.159, sT:42652, sP:3.4681, sB:0.7753, nT:130107, nP:4.0768, nB:0.8752
Cond:7, Score:7252.74, sT:43098, sP:3.4857, sB:0.7737, nT:131647, nP:4.1041, nB:0.8669
Cond:2693, Score:7251.34, sT:42828, sP:3.4957, sB:0.7811, nT:131029, nP:4.113, nB:0.8704
Cond:2277, Score:7251.287, sT:42926, sP:3.4914, sB:0.777, nT:131318, nP:4.106, nB:0.8696
Cond:1266, Score:7250.9, sT:42991, sP:3.4903, sB:0.7752, nT:131340, nP:4.1071, nB:0.8703
Cond:3566, Score:7250.7, sT:42966, sP:3.4846, sB:0.7739, nT:131290, nP:4.1024, nB:0.8705
Cond:513, Score:7250.356, sT:43083, sP:3.4872, sB:0.7753, nT:131422, nP:4.107, nB:0.8667
Cond:49, Score:7250.287, sT:42879, sP:3.4918, sB:0.7868, nT:130425, nP:4.1159, nB:0.8644
Cond:3, Score:7250.037, sT:43151, sP:3.4845, sB:0.7714, nT:131816, nP:4.1018, nB:0.8664
Cond:5150, Score:7249.614, sT:42924, sP:3.4866, sB:0.775, nT:131045, nP:4.1049, nB:0.8721
Cond:2099, Score:7249.315, sT:43014, sP:3.4908, sB:0.7755, nT:131567, nP:4.1058, nB:0.8675
Cond:5134, Score:7248.794, sT:42392, sP:3.4914, sB:0.7849, nT:129295, nP:4.1076, nB:0.8827
Cond:7614, Score:7248.703, sT:42810, sP:3.4888, sB:0.7769, nT:130707, nP:4.1065, nB:0.8747
Cond:675, Score:7248.077, sT:43112, sP:3.4855, sB:0.7726, nT:131706, nP:4.1033, nB:0.8663
Cond:305, Score:7247.693, sT:43104, sP:3.4865, sB:0.7814, nT:131413, nP:4.1079, nB:0.8573 , T2:6130,1395,65,275,1575,110,7822,2485,2695,677,291,5358,8046,5,7838,8368,7,2693,2277,1266,3566,513,49,3,5150,2099,5134,7614,675,305,  #End#
LowScoreRank0 , T0:8144 , T1:
Cond:6130, Score:1389.221, sT:42334, sP:3.4396, sB:0.7861, nT:127330, nP:3.9789, nB:0.8906
Cond:6385, Score:1386.808, sT:37611, sP:3.2716, sB:0.8207, nT:113268, nP:3.8113, nB:0.9355
Cond:6128, Score:1379.376, sT:38683, sP:3.3413, sB:0.8163, nT:111567, nP:3.7601, nB:0.9256
Cond:65, Score:1378.605, sT:42289, sP:3.5002, sB:0.8195, nT:127905, nP:4.1271, nB:0.8665
Cond:1395, Score:1378.243, sT:42647, sP:3.4969, sB:0.8009, nT:129375, nP:4.1018, nB:0.8743
Cond:291, Score:1371.21, sT:42512, sP:3.5008, sB:0.814, nT:127775, nP:4.1165, nB:0.8573
Cond:110, Score:1371.072, sT:41732, sP:3.4636, sB:0.7971, nT:127105, nP:4.0866, nB:0.8883
Cond:1575, Score:1370.607, sT:42573, sP:3.5039, sB:0.7941, nT:130355, nP:4.1195, nB:0.8755
Cond:2695, Score:1369.521, sT:42216, sP:3.5183, sB:0.8007, nT:129503, nP:4.1338, nB:0.8828
Cond:94, Score:1369.337, sT:39882, sP:3.4219, sB:0.8183, nT:121102, nP:4.0588, nB:0.9093
Cond:275, Score:1368.888, sT:42983, sP:3.4868, sB:0.7845, nT:130717, nP:4.1036, nB:0.869
Cond:750, Score:1368.372, sT:38312, sP:3.3868, sB:0.8342, nT:117668, nP:4.0308, nB:0.9246
Cond:8321, Score:1367.941, sT:40538, sP:3.5031, sB:0.8126, nT:122787, nP:4.1126, nB:0.9277
Cond:7822, Score:1366.685, sT:42528, sP:3.4882, sB:0.7846, nT:129621, nP:4.1041, nB:0.8815
Cond:2485, Score:1366.554, sT:42783, sP:3.4914, sB:0.7843, nT:130784, nP:4.1059, nB:0.8716
Cond:7598, Score:1365.189, sT:41870, sP:3.4943, sB:0.7951, nT:127671, nP:4.1094, nB:0.8901
Cond:5358, Score:1364.948, sT:42710, sP:3.4861, sB:0.7817, nT:130297, nP:4.1023, nB:0.875
Cond:768, Score:1364.632, sT:40184, sP:3.5167, sB:0.869, nT:113596, nP:4.1036, nB:0.8944
Cond:8046, Score:1364.189, sT:42834, sP:3.4849, sB:0.7789, nT:130714, nP:4.1006, nB:0.8728
Cond:677, Score:1364.132, sT:43009, sP:3.4863, sB:0.7791, nT:131332, nP:4.1036, nB:0.8665
Cond:2693, Score:1363.905, sT:42754, sP:3.4957, sB:0.7845, nT:130738, nP:4.112, nB:0.8705
Cond:8368, Score:1363.812, sT:42578, sP:3.4677, sB:0.7773, nT:129809, nP:4.0757, nB:0.8749
Cond:5134, Score:1363.797, sT:42318, sP:3.491, sB:0.7869, nT:128996, nP:4.1066, nB:0.8825
Cond:49, Score:1363.461, sT:42805, sP:3.4914, sB:0.7887, nT:130128, nP:4.1148, nB:0.8642
Cond:5, Score:1363.339, sT:43041, sP:3.485, sB:0.7774, nT:131404, nP:4.1023, nB:0.8663
Cond:7838, Score:1363.209, sT:42913, sP:3.4862, sB:0.7772, nT:130993, nP:4.1028, nB:0.8717
Cond:6122, Score:1362.743, sT:41740, sP:3.454, sB:0.7906, nT:127508, nP:4.0851, nB:0.88
Cond:1662, Score:1362.405, sT:41107, sP:3.4481, sB:0.7922, nT:126254, nP:4.073, nB:0.8938
Cond:7390, Score:1362.362, sT:42416, sP:3.4946, sB:0.786, nT:129509, nP:4.1129, nB:0.8792
Cond:2277, Score:1362.361, sT:42849, sP:3.4911, sB:0.7798, nT:131018, nP:4.105, nB:0.8694 , T2:6130,6385,6128,65,1395,291,110,1575,2695,94,275,750,8321,7822,2485,7598,5358,768,8046,677,2693,8368,5134,49,5,7838,6122,1662,7390,2277,  #End#
LowScoreRank1 , T0:8144 , T1:
Cond:6130, Score:2422.17, sT:42334, sP:3.4396, sB:0.7861, nT:127330, nP:3.9789, nB:0.8906
Cond:6385, Score:2403.873, sT:37611, sP:3.2716, sB:0.8207, nT:113268, nP:3.8113, nB:0.9355
Cond:1395, Score:2402.929, sT:42647, sP:3.4969, sB:0.8009, nT:129375, nP:4.1018, nB:0.8743
Cond:65, Score:2401.149, sT:42289, sP:3.5002, sB:0.8195, nT:127905, nP:4.1271, nB:0.8665
Cond:6128, Score:2391.895, sT:38683, sP:3.3413, sB:0.8163, nT:111567, nP:3.7601, nB:0.9256
Cond:1575, Score:2390.349, sT:42573, sP:3.5039, sB:0.7941, nT:130355, nP:4.1195, nB:0.8755
Cond:110, Score:2388.643, sT:41732, sP:3.4636, sB:0.7971, nT:127105, nP:4.0866, nB:0.8883
Cond:291, Score:2388.395, sT:42512, sP:3.5008, sB:0.814, nT:127775, nP:4.1165, nB:0.8573
Cond:275, Score:2388.116, sT:42983, sP:3.4868, sB:0.7845, nT:130717, nP:4.1036, nB:0.869
Cond:2695, Score:2387.601, sT:42216, sP:3.5183, sB:0.8007, nT:129503, nP:4.1338, nB:0.8828
Cond:2485, Score:2384, sT:42783, sP:3.4914, sB:0.7843, nT:130784, nP:4.1059, nB:0.8716
Cond:7822, Score:2383.635, sT:42528, sP:3.4882, sB:0.7846, nT:129621, nP:4.1041, nB:0.8815
Cond:5358, Score:2381.06, sT:42710, sP:3.4861, sB:0.7817, nT:130297, nP:4.1023, nB:0.875
Cond:677, Score:2380.322, sT:43009, sP:3.4863, sB:0.7791, nT:131332, nP:4.1036, nB:0.8665
Cond:8321, Score:2380.221, sT:40538, sP:3.5031, sB:0.8126, nT:122787, nP:4.1126, nB:0.9277
Cond:8046, Score:2380.141, sT:42834, sP:3.4849, sB:0.7789, nT:130714, nP:4.1006, nB:0.8728
Cond:94, Score:2379.765, sT:39882, sP:3.4219, sB:0.8183, nT:121102, nP:4.0588, nB:0.9093
Cond:2693, Score:2379.264, sT:42754, sP:3.4957, sB:0.7845, nT:130738, nP:4.112, nB:0.8705
Cond:7598, Score:2379.098, sT:41870, sP:3.4943, sB:0.7951, nT:127671, nP:4.1094, nB:0.8901
Cond:5, Score:2379.074, sT:43041, sP:3.485, sB:0.7774, nT:131404, nP:4.1023, nB:0.8663
Cond:8368, Score:2378.884, sT:42578, sP:3.4677, sB:0.7773, nT:129809, nP:4.0757, nB:0.8749
Cond:7838, Score:2378.697, sT:42913, sP:3.4862, sB:0.7772, nT:130993, nP:4.1028, nB:0.8717
Cond:5134, Score:2377.965, sT:42318, sP:3.491, sB:0.7869, nT:128996, nP:4.1066, nB:0.8825
Cond:49, Score:2377.737, sT:42805, sP:3.4914, sB:0.7887, nT:130128, nP:4.1148, nB:0.8642
Cond:2277, Score:2376.994, sT:42849, sP:3.4911, sB:0.7798, nT:131018, nP:4.105, nB:0.8694
Cond:7, Score:2375.839, sT:43023, sP:3.4853, sB:0.7759, nT:131348, nP:4.103, nB:0.8667
Cond:7390, Score:2375.762, sT:42416, sP:3.4946, sB:0.786, nT:129509, nP:4.1129, nB:0.8792
Cond:305, Score:2375.582, sT:43030, sP:3.4861, sB:0.7835, nT:131114, nP:4.1069, nB:0.857
Cond:3566, Score:2375.508, sT:42891, sP:3.4843, sB:0.7761, nT:130991, nP:4.1014, nB:0.8703
Cond:513, Score:2375.408, sT:43009, sP:3.4868, sB:0.7775, nT:131123, nP:4.106, nB:0.8664 , T2:6130,6385,1395,65,6128,1575,110,291,275,2695,2485,7822,5358,677,8321,8046,94,2693,7598,5,8368,7838,5134,49,2277,7,7390,305,3566,513,  #End#
LowScoreRank2 , T0:8144 , T1:
Cond:6130, Score:4226.3, sT:42334, sP:3.4396, sB:0.7861, nT:127330, nP:3.9789, nB:0.8906
Cond:1395, Score:4192.581, sT:42647, sP:3.4969, sB:0.8009, nT:129375, nP:4.1018, nB:0.8743
Cond:65, Score:4185.234, sT:42289, sP:3.5002, sB:0.8195, nT:127905, nP:4.1271, nB:0.8665
Cond:1575, Score:4171.97, sT:42573, sP:3.5039, sB:0.7941, nT:130355, nP:4.1195, nB:0.8755
Cond:6385, Score:4169.944, sT:37611, sP:3.2716, sB:0.8207, nT:113268, nP:3.8113, nB:0.9355
Cond:275, Score:4169.372, sT:42983, sP:3.4868, sB:0.7845, nT:130717, nP:4.1036, nB:0.869
Cond:2695, Score:4165.698, sT:42216, sP:3.5183, sB:0.8007, nT:129503, nP:4.1338, nB:0.8828
Cond:110, Score:4164.576, sT:41732, sP:3.4636, sB:0.7971, nT:127105, nP:4.0866, nB:0.8883
Cond:291, Score:4163.186, sT:42512, sP:3.5008, sB:0.814, nT:127775, nP:4.1165, nB:0.8573
Cond:2485, Score:4162.141, sT:42783, sP:3.4914, sB:0.7843, nT:130784, nP:4.1059, nB:0.8716
Cond:7822, Score:4160.454, sT:42528, sP:3.4882, sB:0.7846, nT:129621, nP:4.1041, nB:0.8815
Cond:5358, Score:4156.758, sT:42710, sP:3.4861, sB:0.7817, nT:130297, nP:4.1023, nB:0.875
Cond:677, Score:4156.668, sT:43009, sP:3.4863, sB:0.7791, nT:131332, nP:4.1036, nB:0.8665
Cond:8046, Score:4155.861, sT:42834, sP:3.4849, sB:0.7789, nT:130714, nP:4.1006, nB:0.8728
Cond:5, Score:4154.726, sT:43041, sP:3.485, sB:0.7774, nT:131404, nP:4.1023, nB:0.8663
Cond:7838, Score:4153.805, sT:42913, sP:3.4862, sB:0.7772, nT:130993, nP:4.1028, nB:0.8717
Cond:2693, Score:4153.672, sT:42754, sP:3.4957, sB:0.7845, nT:130738, nP:4.112, nB:0.8705
Cond:8368, Score:4152.617, sT:42578, sP:3.4677, sB:0.7773, nT:129809, nP:4.0757, nB:0.8749
Cond:6128, Score:4150.494, sT:38683, sP:3.3413, sB:0.8163, nT:111567, nP:3.7601, nB:0.9256
Cond:2277, Score:4150.453, sT:42849, sP:3.4911, sB:0.7798, nT:131018, nP:4.105, nB:0.8694
Cond:49, Score:4149.652, sT:42805, sP:3.4914, sB:0.7887, nT:130128, nP:4.1148, nB:0.8642
Cond:5134, Score:4149.453, sT:42318, sP:3.491, sB:0.7869, nT:128996, nP:4.1066, nB:0.8825
Cond:7598, Score:4149.174, sT:41870, sP:3.4943, sB:0.7951, nT:127671, nP:4.1094, nB:0.8901
Cond:7, Score:4149.12, sT:43023, sP:3.4853, sB:0.7759, nT:131348, nP:4.103, nB:0.8667
Cond:3566, Score:4148.178, sT:42891, sP:3.4843, sB:0.7761, nT:130991, nP:4.1014, nB:0.8703
Cond:513, Score:4147.957, sT:43009, sP:3.4868, sB:0.7775, nT:131123, nP:4.106, nB:0.8664
Cond:1266, Score:4147.509, sT:42920, sP:3.4898, sB:0.7771, nT:131069, nP:4.1058, nB:0.8698
Cond:5150, Score:4147.41, sT:42850, sP:3.4861, sB:0.777, nT:130746, nP:4.1039, nB:0.8719
Cond:1573, Score:4147.231, sT:42902, sP:3.4897, sB:0.7797, nT:131088, nP:4.1068, nB:0.8664
Cond:305, Score:4147.214, sT:43030, sP:3.4861, sB:0.7835, nT:131114, nP:4.1069, nB:0.857 , T2:6130,1395,65,1575,6385,275,2695,110,291,2485,7822,5358,677,8046,5,7838,2693,8368,6128,2277,49,5134,7598,7,3566,513,1266,5150,1573,305,  #End#
LowScoreRank3 , T0:8144 , T1:
Cond:6130, Score:7379.736, sT:42334, sP:3.4396, sB:0.7861, nT:127330, nP:3.9789, nB:0.8906
Cond:1395, Score:7320.656, sT:42647, sP:3.4969, sB:0.8009, nT:129375, nP:4.1018, nB:0.8743
Cond:65, Score:7300.369, sT:42289, sP:3.5002, sB:0.8195, nT:127905, nP:4.1271, nB:0.8665
Cond:1575, Score:7287.11, sT:42573, sP:3.5039, sB:0.7941, nT:130355, nP:4.1195, nB:0.8755
Cond:275, Score:7284.772, sT:42983, sP:3.4868, sB:0.7845, nT:130717, nP:4.1036, nB:0.869
Cond:2695, Score:7273.595, sT:42216, sP:3.5183, sB:0.8007, nT:129503, nP:4.1338, nB:0.8828
Cond:2485, Score:7272.115, sT:42783, sP:3.4914, sB:0.7843, nT:130784, nP:4.1059, nB:0.8716
Cond:7822, Score:7267.313, sT:42528, sP:3.4882, sB:0.7846, nT:129621, nP:4.1041, nB:0.8815
Cond:110, Score:7266.439, sT:41732, sP:3.4636, sB:0.7971, nT:127105, nP:4.0866, nB:0.8883
Cond:677, Score:7264.201, sT:43009, sP:3.4863, sB:0.7791, nT:131332, nP:4.1036, nB:0.8665
Cond:5358, Score:7262.258, sT:42710, sP:3.4861, sB:0.7817, nT:130297, nP:4.1023, nB:0.875
Cond:291, Score:7262.16, sT:42512, sP:3.5008, sB:0.814, nT:127775, nP:4.1165, nB:0.8573
Cond:8046, Score:7261.933, sT:42834, sP:3.4849, sB:0.7789, nT:130714, nP:4.1006, nB:0.8728
Cond:5, Score:7261.22, sT:43041, sP:3.485, sB:0.7774, nT:131404, nP:4.1023, nB:0.8663
Cond:7838, Score:7259.157, sT:42913, sP:3.4862, sB:0.7772, nT:130993, nP:4.1028, nB:0.8717
Cond:2693, Score:7256.973, sT:42754, sP:3.4957, sB:0.7845, nT:130738, nP:4.112, nB:0.8705
Cond:8368, Score:7254.426, sT:42578, sP:3.4677, sB:0.7773, nT:129809, nP:4.0757, nB:0.8749
Cond:2277, Score:7252.65, sT:42849, sP:3.4911, sB:0.7798, nT:131018, nP:4.105, nB:0.8694
Cond:7, Score:7251.499, sT:43023, sP:3.4853, sB:0.7759, nT:131348, nP:4.103, nB:0.8667
Cond:3566, Score:7249.227, sT:42891, sP:3.4843, sB:0.7761, nT:130991, nP:4.1014, nB:0.8703
Cond:513, Score:7248.735, sT:43009, sP:3.4868, sB:0.7775, nT:131123, nP:4.106, nB:0.8664
Cond:3, Score:7248.452, sT:43076, sP:3.4841, sB:0.7736, nT:131517, nP:4.1007, nB:0.8661
Cond:1266, Score:7248.134, sT:42920, sP:3.4898, sB:0.7771, nT:131069, nP:4.1058, nB:0.8698
Cond:49, Score:7247.509, sT:42805, sP:3.4914, sB:0.7887, nT:130128, nP:4.1148, nB:0.8642
Cond:5150, Score:7247.367, sT:42850, sP:3.4861, sB:0.777, nT:130746, nP:4.1039, nB:0.8719
Cond:1573, Score:7246.887, sT:42902, sP:3.4897, sB:0.7797, nT:131088, nP:4.1068, nB:0.8664
Cond:1141, Score:7246.506, sT:43030, sP:3.4837, sB:0.7752, nT:131343, nP:4.1004, nB:0.8653
Cond:675, Score:7246.304, sT:43037, sP:3.4852, sB:0.7748, nT:131407, nP:4.1023, nB:0.866
Cond:5134, Score:7246.166, sT:42318, sP:3.491, sB:0.7869, nT:128996, nP:4.1066, nB:0.8825
Cond:7614, Score:7245.994, sT:42736, sP:3.4884, sB:0.7789, nT:130408, nP:4.1054, nB:0.8744 , T2:6130,1395,65,1575,275,2695,2485,7822,110,677,5358,291,8046,5,7838,2693,8368,2277,7,3566,513,3,1266,49,5150,1573,1141,675,5134,7614,  #End#
End , T0:00:34:13.3078338  #End#





 */
