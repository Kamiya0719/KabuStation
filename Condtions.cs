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
		private static readonly int[] ConfirmAnds = new int[7] {
			2806, // T2:4353.954 , T3:43200 , T4:0.76 , T5:3.43 , T6:130514 , T7:0.88 , T8:3.96
			981, // T2:4512.765 , T3:50017 , T4:0.64 , T5:3.21 , T6:153685 , T7:0.79 , T8:3.78
			5100, // T2:4393.302 , T3:43836 , T4:0.76 , T5:3.43 , T6:131573 , T7:0.88 , T8:3.97
			2808, // T2:4605.062 , T3:45670 , T4:0.76 , T5:3.41 , T6:139284 , T7:0.88 , T8:3.95
			126, // T2:4352.487 , T3:44426 , T4:0.75 , T5:3.43 , T6:134711 , T7:0.85 , T8:3.96
			8354, // T2:4217.273 , T3:46612 , T4:0.71 , T5:3.49 , T6:142942 , T7:0.8 , T8:4.08
			8344, // T2:4201.974 , T3:46693 , T4:0.74 , T5:3.72 , T6:136993 , T7:0.85 , T8:4.15
			//6130, // T2:4275.712 , T3:43325 , T4:0.77 , T5:3.48 , T6:132472 , T7:0.86 , T8:4.1

			//4999, // T2:4355.432 , T3:42864 , T4:0.77 , T5:3.42 , T6:128905 , T7:0.88 , T8:3.97
			//1474, // T2:4364.727 , T3:42786 , T4:0.78 , T5:3.43 , T6:128873 , T7:0.88 , T8:3.97
			//1664, // T2:4406.172 , T3:42903 , T4:0.78 , T5:3.43 , T6:130376 , T7:0.88 , T8:3.98
			//2786, // T2:4365.229 , T3:42737 , T4:0.78 , T5:3.44 , T6:129242 , T7:0.88 , T8:3.98

			//1682, // T2:4366.456 , T3:42592 , T4:0.78 , T5:3.44 , T6:128285 , T7:0.89 , T8:3.98
			//1282, // T2:4365.501 , T3:42593 , T4:0.78 , T5:3.44 , T6:128255 , T7:0.89 , T8:3.98
			//4656, // T2:4363.196 , T3:42595 , T4:0.78 , T5:3.44 , T6:128261 , T7:0.89 , T8:3.98
			//7612, // T2:4356.833 , T3:42584 , T4:0.78 , T5:3.44 , T6:128254 , T7:0.89 , T8:3.98
			//2804, // T2:4370.598 , T3:42609 , T4:0.78 , T5:3.44 , T6:128370 , T7:0.89 , T8:3.98
			//4356, // T2:4368.806 , T3:42586 , T4:0.78 , T5:3.44 , T6:128286 , T7:0.89 , T8:3.98
			/*
			//8412, // T2:4365.294 , T3:42567 , T4:0.78 , T5:3.44 , T6:128188 , T7:0.89 , T8:3.98
			//6170, // T2:4362.189 , T3:42574 , T4:0.78 , T5:3.44 , T6:128217 , T7:0.89 , T8:3.98
			//8372, // T2:4362.984 , T3:42568 , T4:0.78 , T5:3.44 , T6:128195 , T7:0.89 , T8:3.98
			//8408, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//5958, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//6218, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//8476, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//4476, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//8444, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//6270, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//8478, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//6202, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//8426, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//8026, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//8428, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//4462, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//222, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//206, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//6168, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//6186, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//862, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//2862, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//8430, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//5324, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//2830, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			//8376, // T2:4363.221 , T3:42563 , T4:0.78 , T5:3.44 , T6:128183 , T7:0.89 , T8:3.98
			*/
		};
		private static readonly int[] ConfirmOrs = new int[8] {
			2322, // T3:4182.689 , T4:40694 , T5:0.78 , T6:3.47 , T7:123741 , T8:0.89 , T9:4.01
			//845, // T3:4368.953 , T4:42503 , T5:0.78 , T6:3.43 , T7:128041 , T8:0.89 , T9:3.98
			66, // T3:4177.842 , T4:38963 , T5:0.79 , T6:3.38 , T7:121832 , T8:0.9 , T9:3.97
			1065, // T3:4338.98 , T4:41286 , T5:0.79 , T6:3.42 , T7:123811 , T8:0.91 , T9:3.95
			//5983, // T3:4358.209 , T4:42481 , T5:0.78 , T6:3.44 , T7:128062 , T8:0.89 , T9:3.98
			6851, // T3:4351.649 , T4:40931 , T5:0.81 , T6:3.45 , T7:121676 , T8:0.92 , T9:3.97
			//4524, // T3:4369.842 , T4:42480 , T5:0.78 , T6:3.44 , T7:127727 , T8:0.89 , T9:3.97
			1618, // T3:4395.805 , T4:41365 , T5:0.83 , T6:3.34 , T7:121683 , T8:0.86 , T9:3.89
			7436, // T3:4233.446 , T4:37492 , T5:0.85 , T6:3.37 , T7:104561 , T8:0.96 , T9:3.79

			//52, // T3:4324.182 , T4:42176 , T5:0.78 , T6:3.43 , T7:126887 , T8:0.89 , T9:3.98
			5388, // T3:4322.379 , T4:42183 , T5:0.78 , T6:3.45 , T7:126578 , T8:0.89 , T9:3.99
			//619, // T3:4378.074 , T4:42278 , T5:0.79 , T6:3.43 , T7:127348 , T8:0.89 , T9:3.97
			7599, // T3:4376.87 , T4:42214 , T5:0.79 , T6:3.44 , T7:126844 , T8:0.89 , T9:3.97
			//6717, // T3:4337.832 , T4:42370 , T5:0.78 , T6:3.44 , T7:127541 , T8:0.89 , T9:3.98
			//5041, // T3:4349.89 , T4:42286 , T5:0.79 , T6:3.44 , T7:127170 , T8:0.89 , T9:3.98
			//528, // T3:4358.366 , T4:42454 , T5:0.78 , T6:3.44 , T7:127875 , T8:0.89 , T9:3.98

			//706, // T3:4364.015 , T4:42583 , T5:0.78 , T6:3.44 , T7:128235 , T8:0.89 , T9:3.98
			//7404, // T3:4364.015 , T4:42583 , T5:0.78 , T6:3.44 , T7:128235 , T8:0.89 , T9:3.98
			//5164, // T3:4364.015 , T4:42583 , T5:0.78 , T6:3.44 , T7:128235 , T8:0.89 , T9:3.98
			//7196, // T3:4364.015 , T4:42583 , T5:0.78 , T6:3.44 , T7:128235 , T8:0.89 , T9:3.98
			//8395, // T3:4364.836 , T4:42582 , T5:0.78 , T6:3.44 , T7:128234 , T8:0.89 , T9:3.98
			//8167, // T3:4367.655 , T4:42559 , T5:0.78 , T6:3.44 , T7:128167 , T8:0.89 , T9:3.98
			//8375, // T3:4364.699 , T4:42578 , T5:0.78 , T6:3.44 , T7:128217 , T8:0.89 , T9:3.98

			/*
			//6159, // 仮  T0:65692, T1:0.6371, T2:5.4129 #End#, T0:202915, T1:0.5783, T2:6.848  #End#
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
			AllTrueCondIdx
		};
		private static readonly int[] KouhoOrs = new int[] {
			//AllTrueCondIdx-1

		};
		private const int AllCond51Num = 3754886; // 2000日*2500銘柄
		private const double AllCond51Ratio = -0.000912;
		private const double PeriodPow = 0.7;
		private const int NoSkipRatio = 6;
		/** 51条件の全検証 */
		public static void CheckCond51All()
		{
			int[] confirmAnds =  ConfirmAnds;
			int[] confirmOrs = ConfirmOrs;

			if(IsAndCheck) {
				for (int i = 1; i < confirmAnds.Length; i++) {
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
				for (int r = 1; r < 4; r++) {
					string result = ""; string result2 = ""; int max = 10;
					foreach (KeyValuePair<int, double> b in (r==0?scores: r == 1 ? scores2 : r == 2 ? scores3 : scores4).OrderByDescending(c => c.Value)) {
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
Cond:65, Score:2361.056, sT:42006, sP:3.4239, sB:0.8138, nT:129098, nP:4.0755, nB:0.8411
Cond:737, Score:2340.285, sT:40116, sP:3.4223, sB:0.8532, nT:121995, nP:4.0807, nB:0.8269
Cond:2695, Score:2340.102, sT:41786, sP:3.4497, sB:0.802, nT:130328, nP:4.09, nB:0.8627
Cond:1395, Score:2337.465, sT:42376, sP:3.4202, sB:0.7899, nT:130602, nP:4.0503, nB:0.8476
Cond:6130, Score:2334.64, sT:42197, sP:3.3724, sB:0.775, nT:128763, nP:3.932, nB:0.8629
Cond:291, Score:2333.631, sT:42189, sP:3.4239, sB:0.8013, nT:129005, nP:4.0642, nB:0.8309
Cond:1575, Score:2333.461, sT:42227, sP:3.4312, sB:0.79, nT:131404, nP:4.0718, nB:0.8521
Cond:2485, Score:2322.579, sT:42479, sP:3.4165, sB:0.7786, nT:131925, nP:4.0566, nB:0.8477
Cond:6494, Score:2320.868, sT:40625, sP:3.44, sB:0.8098, nT:126542, nP:4.0816, nB:0.8806
Cond:2693, Score:2320.497, sT:42438, sP:3.4218, sB:0.78, nT:131838, nP:4.0634, nB:0.8466 , T3:65,737,2695,1395,6130,291,1575,2485,6494,2693,  #End#
LowScoreRank2 , T0:-1 , T1:1 , T2:
Cond:65, Score:4083.662, sT:42006, sP:3.4239, sB:0.8138, nT:129098, nP:4.0755, nB:0.8411
Cond:2695, Score:4049.754, sT:41786, sP:3.4497, sB:0.802, nT:130328, nP:4.09, nB:0.8627
Cond:1395, Score:4046.792, sT:42376, sP:3.4202, sB:0.7899, nT:130602, nP:4.0503, nB:0.8476
Cond:6130, Score:4042.313, sT:42197, sP:3.3724, sB:0.775, nT:128763, nP:3.932, nB:0.8629
Cond:1575, Score:4040.256, sT:42227, sP:3.4312, sB:0.79, nT:131404, nP:4.0718, nB:0.8521
Cond:291, Score:4036.868, sT:42189, sP:3.4239, sB:0.8013, nT:129005, nP:4.0642, nB:0.8309
Cond:737, Score:4034.549, sT:40116, sP:3.4223, sB:0.8532, nT:121995, nP:4.0807, nB:0.8269
Cond:2485, Score:4022.84, sT:42479, sP:3.4165, sB:0.7786, nT:131925, nP:4.0566, nB:0.8477
Cond:2693, Score:4018.919, sT:42438, sP:3.4218, sB:0.78, nT:131838, nP:4.0634, nB:0.8466
Cond:49, Score:4016.738, sT:42529, sP:3.4153, sB:0.781, nT:131360, nP:4.0635, nB:0.8387 , T3:65,2695,1395,6130,1575,291,737,2485,2693,49,  #End#
LowScoreRank3 , T0:-1 , T1:1 , T2:
Cond:65, Score:7067.589, sT:42006, sP:3.4239, sB:0.8138, nT:129098, nP:4.0755, nB:0.8411
Cond:2695, Score:7013.173, sT:41786, sP:3.4497, sB:0.802, nT:130328, nP:4.09, nB:0.8627
Cond:1395, Score:7010.701, sT:42376, sP:3.4202, sB:0.7899, nT:130602, nP:4.0503, nB:0.8476
Cond:6130, Score:7003.644, sT:42197, sP:3.3724, sB:0.775, nT:128763, nP:3.932, nB:0.8629
Cond:1575, Score:7000.166, sT:42227, sP:3.4312, sB:0.79, nT:131404, nP:4.0718, nB:0.8521
Cond:291, Score:6987.673, sT:42189, sP:3.4239, sB:0.8013, nT:129005, nP:4.0642, nB:0.8309
Cond:2485, Score:6972.457, sT:42479, sP:3.4165, sB:0.7786, nT:131925, nP:4.0566, nB:0.8477
Cond:2693, Score:6965.114, sT:42438, sP:3.4218, sB:0.78, nT:131838, nP:4.0634, nB:0.8466
Cond:49, Score:6960.067, sT:42529, sP:3.4153, sB:0.781, nT:131360, nP:4.0635, nB:0.8387
Cond:737, Score:6959.614, sT:40116, sP:3.4223, sB:0.8532, nT:121995, nP:4.0807, nB:0.8269 , T3:65,2695,1395,6130,1575,291,2485,2693,49,737,  #End#

LowScoreRank1 , T0:2806 , T1:1 , T2:
Cond:65, Score:2352.284, sT:42831, sP:3.4078, sB:0.7934, nT:132214, nP:4.0573, nB:0.8257
Cond:2695, Score:2333.944, sT:42625, sP:3.4321, sB:0.7824, nT:133462, nP:4.0713, nB:0.8471
Cond:737, Score:2332.336, sT:40933, sP:3.4064, sB:0.8318, nT:125084, nP:4.0619, nB:0.8112
Cond:1395, Score:2332.129, sT:43209, sP:3.404, sB:0.7715, nT:133732, nP:4.0326, nB:0.8323
Cond:291, Score:2328.715, sT:43015, sP:3.4079, sB:0.7829, nT:132132, nP:4.046, nB:0.8159
Cond:1575, Score:2328.124, sT:43061, sP:3.4146, sB:0.7714, nT:134538, nP:4.0536, nB:0.8369
Cond:6130, Score:2327.976, sT:43020, sP:3.3576, sB:0.7566, nT:131839, nP:3.9161, nB:0.8474
Cond:2485, Score:2316.461, sT:43313, sP:3.4003, sB:0.76, nT:135059, nP:4.0388, nB:0.8327
Cond:2693, Score:2314.277, sT:43272, sP:3.4055, sB:0.7613, nT:134972, nP:4.0455, nB:0.8316
Cond:110, Score:2314.057, sT:42301, sP:3.3777, sB:0.7695, nT:131291, nP:4.025, nB:0.8505 , T3:65,2695,737,1395,291,1575,6130,2485,2693,110,  #End#
LowScoreRank2 , T0:2806 , T1:1 , T2:
Cond:65, Score:4073.176, sT:42831, sP:3.4078, sB:0.7934, nT:132214, nP:4.0573, nB:0.8257
Cond:2695, Score:4043.783, sT:42625, sP:3.4321, sB:0.7824, nT:133462, nP:4.0713, nB:0.8471
Cond:1395, Score:4042.132, sT:43209, sP:3.404, sB:0.7715, nT:133732, nP:4.0326, nB:0.8323
Cond:1575, Score:4035.615, sT:43061, sP:3.4146, sB:0.7714, nT:134538, nP:4.0536, nB:0.8369
Cond:6130, Score:4035.358, sT:43020, sP:3.3576, sB:0.7566, nT:131839, nP:3.9161, nB:0.8474
Cond:291, Score:4032.92, sT:43015, sP:3.4079, sB:0.7829, nT:132132, nP:4.046, nB:0.8159
Cond:737, Score:4025.602, sT:40933, sP:3.4064, sB:0.8318, nT:125084, nP:4.0619, nB:0.8112
Cond:2485, Score:4016.824, sT:43313, sP:3.4003, sB:0.76, nT:135059, nP:4.0388, nB:0.8327
Cond:2693, Score:4012.729, sT:43272, sP:3.4055, sB:0.7613, nT:134972, nP:4.0455, nB:0.8316
Cond:49, Score:4009.126, sT:43366, sP:3.399, sB:0.7619, nT:134493, nP:4.0456, nB:0.8238 , T3:65,2695,1395,1575,6130,291,737,2485,2693,49,  #End#
LowScoreRank3 , T0:2806 , T1:1 , T2:
Cond:65, Score:7057.618, sT:42831, sP:3.4078, sB:0.7934, nT:132214, nP:4.0573, nB:0.8257
Cond:2695, Score:7011.009, sT:42625, sP:3.4321, sB:0.7824, nT:133462, nP:4.0713, nB:0.8471
Cond:1395, Score:7010.619, sT:43209, sP:3.404, sB:0.7715, nT:133732, nP:4.0326, nB:0.8323
Cond:1575, Score:7000.15, sT:43061, sP:3.4146, sB:0.7714, nT:134538, nP:4.0536, nB:0.8369
Cond:6130, Score:6999.596, sT:43020, sP:3.3576, sB:0.7566, nT:131839, nP:3.9161, nB:0.8474
Cond:291, Score:6988.788, sT:43015, sP:3.4079, sB:0.7829, nT:132132, nP:4.046, nB:0.8159
Cond:2485, Score:6970.031, sT:43313, sP:3.4003, sB:0.76, nT:135059, nP:4.0388, nB:0.8327
Cond:2693, Score:6962.389, sT:43272, sP:3.4055, sB:0.7613, nT:134972, nP:4.0455, nB:0.8316
Cond:275, Score:6955.81, sT:43550, sP:3.3946, sB:0.7548, nT:135092, nP:4.0347, nB:0.8279
Cond:49, Score:6954.911, sT:43366, sP:3.399, sB:0.7619, nT:134493, nP:4.0456, nB:0.8238 , T3:65,2695,1395,1575,6130,291,2485,2693,275,49,  #End#


//////////////////////////////////////////////////////////////////////////////////////////

LowScoreRank1 , T0:-1 , T1:1 , T2:
Cond:52, Score:2316.992, sT:43257, sP:3.4199, sB:0.7627, nT:134252, nP:4.049, nB:0.842
Cond:6717, Score:2308.15, sT:43060, sP:3.4043, sB:0.7608, nT:133608, nP:4.0463, nB:0.8398
Cond:7149, Score:2304.46, sT:43163, sP:3.4054, sB:0.7585, nT:133900, nP:4.0452, nB:0.8363
Cond:3327, Score:2304.084, sT:43552, sP:3.4121, sB:0.7538, nT:135096, nP:4.049, nB:0.8313
Cond:6941, Score:2303.128, sT:42924, sP:3.406, sB:0.7616, nT:133163, nP:4.0464, nB:0.8395
Cond:6697, Score:2302.024, sT:42916, sP:3.4075, sB:0.7612, nT:133170, nP:4.0484, nB:0.8404
Cond:6903, Score:2301.239, sT:42910, sP:3.4079, sB:0.7614, nT:133168, nP:4.0486, nB:0.8393
Cond:5041, Score:2300.645, sT:43186, sP:3.4011, sB:0.7552, nT:134087, nP:4.0413, nB:0.8363
Cond:5135, Score:2300.62, sT:43061, sP:3.4058, sB:0.7586, nT:133606, nP:4.0463, nB:0.8369
Cond:4833, Score:2300.597, sT:42916, sP:3.4042, sB:0.7599, nT:133141, nP:4.0444, nB:0.8403 , T3:52,6717,7149,3327,6941,6697,6903,5041,5135,4833, 
LowScoreRank2 , T0:-1 , T1:1 , T2:
Cond:52, Score:4017.489, sT:43257, sP:3.4199, sB:0.7627, nT:134252, nP:4.049, nB:0.842
Cond:6717, Score:4001.087, sT:43060, sP:3.4043, sB:0.7608, nT:133608, nP:4.0463, nB:0.8398
Cond:3327, Score:3996.29, sT:43552, sP:3.4121, sB:0.7538, nT:135096, nP:4.049, nB:0.8313
Cond:7149, Score:3995.112, sT:43163, sP:3.4054, sB:0.7585, nT:133900, nP:4.0452, nB:0.8363
Cond:6941, Score:3991.683, sT:42924, sP:3.406, sB:0.7616, nT:133163, nP:4.0464, nB:0.8395
Cond:6697, Score:3989.828, sT:42916, sP:3.4075, sB:0.7612, nT:133170, nP:4.0484, nB:0.8404
Cond:6907, Score:3989.72, sT:43456, sP:3.4107, sB:0.7541, nT:134835, nP:4.0517, nB:0.8308
Cond:6493, Score:3989.007, sT:43462, sP:3.4009, sB:0.7514, nT:134988, nP:4.0445, nB:0.8313
Cond:5041, Score:3988.884, sT:43186, sP:3.4011, sB:0.7552, nT:134087, nP:4.0413, nB:0.8363
Cond:6903, Score:3988.381, sT:42910, sP:3.4079, sB:0.7614, nT:133168, nP:4.0486, nB:0.8393 , T3:52,6717,3327,7149,6941,6697,6907,6493,5041,6903, 
LowScoreRank3 , T0:-1 , T1:1 , T2:
Cond:52, Score:6970.713, sT:43257, sP:3.4199, sB:0.7627, nT:134252, nP:4.049, nB:0.842
Cond:6717, Score:6940.388, sT:43060, sP:3.4043, sB:0.7608, nT:133608, nP:4.0463, nB:0.8398
Cond:3327, Score:6935.972, sT:43552, sP:3.4121, sB:0.7538, nT:135096, nP:4.049, nB:0.8313
Cond:7149, Score:6930.755, sT:43163, sP:3.4054, sB:0.7585, nT:133900, nP:4.0452, nB:0.8363
Cond:6907, Score:6923.708, sT:43456, sP:3.4107, sB:0.7541, nT:134835, nP:4.0517, nB:0.8308
Cond:5119, Score:6923.163, sT:44320, sP:3.4186, sB:0.7401, nT:138160, nP:4.0614, nB:0.8187
Cond:6493, Score:6922.981, sT:43462, sP:3.4009, sB:0.7514, nT:134988, nP:4.0445, nB:0.8313
Cond:6941, Score:6922.861, sT:42924, sP:3.406, sB:0.7616, nT:133163, nP:4.0464, nB:0.8395
Cond:5041, Score:6920.633, sT:43186, sP:3.4011, sB:0.7552, nT:134087, nP:4.0413, nB:0.8363
Cond:6697, Score:6919.751, sT:42916, sP:3.4075, sB:0.7612, nT:133170, nP:4.0484, nB:0.8404 , T3:52,6717,3327,7149,6907,5119,6493,6941,5041,6697, 
LowScoreRank1 , T0:2322 , T1:1 , T2:
Cond:1604, Score:2250.66, sT:42205, sP:3.4179, sB:0.7529, nT:132350, nP:4.0819, nB:0.8385
Cond:472, Score:2239.353, sT:43784, sP:3.3968, sB:0.7171, nT:137181, nP:4.0416, nB:0.8246
Cond:3012, Score:2235.766, sT:42707, sP:3.452, sB:0.745, nT:133134, nP:4.1187, nB:0.8371
Cond:486, Score:2227.004, sT:41633, sP:3.4332, sB:0.7586, nT:130141, nP:4.0899, nB:0.8337
Cond:3444, Score:2226.92, sT:42496, sP:3.3786, sB:0.728, nT:132209, nP:4.0269, nB:0.8414
Cond:1862, Score:2220.533, sT:43185, sP:3.4484, sB:0.7297, nT:133075, nP:4.0849, nB:0.8355
Cond:52, Score:2218.072, sT:41302, sP:3.4499, sB:0.7595, nT:129576, nP:4.0879, nB:0.8424
Cond:3220, Score:2212.086, sT:41211, sP:3.42, sB:0.7536, nT:128873, nP:4.0736, nB:0.8433
Cond:24, Score:2210.045, sT:43573, sP:3.4103, sB:0.7121, nT:135894, nP:4.0545, nB:0.8227
Cond:3234, Score:2209.633, sT:42188, sP:3.4542, sB:0.7429, nT:131633, nP:4.1034, nB:0.8353 , T3:1604,472,3012,486,3444,1862,52,3220,24,3234, 
LowScoreRank2 , T0:2322 , T1:1 , T2:
Cond:1604, Score:3898.787, sT:42205, sP:3.4179, sB:0.7529, nT:132350, nP:4.0819, nB:0.8385
Cond:472, Score:3887.859, sT:43784, sP:3.3968, sB:0.7171, nT:137181, nP:4.0416, nB:0.8246
Cond:3012, Score:3875.152, sT:42707, sP:3.452, sB:0.745, nT:133134, nP:4.1187, nB:0.8371
Cond:3444, Score:3860.003, sT:42496, sP:3.3786, sB:0.728, nT:132209, nP:4.0269, nB:0.8414
Cond:486, Score:3854.349, sT:41633, sP:3.4332, sB:0.7586, nT:130141, nP:4.0899, nB:0.8337
Cond:1862, Score:3850.88, sT:43185, sP:3.4484, sB:0.7297, nT:133075, nP:4.0849, nB:0.8355
Cond:488, Score:3842.835, sT:47614, sP:3.443, sB:0.6504, nT:151827, nP:4.0963, nB:0.795
Cond:52, Score:3838.289, sT:41302, sP:3.4499, sB:0.7595, nT:129576, nP:4.0879, nB:0.8424
Cond:24, Score:3835.849, sT:43573, sP:3.4103, sB:0.7121, nT:135894, nP:4.0545, nB:0.8227
Cond:3234, Score:3827.77, sT:42188, sP:3.4542, sB:0.7429, nT:131633, nP:4.1034, nB:0.8353 , T3:1604,472,3012,3444,486,1862,488,52,24,3234, 
LowScoreRank3 , T0:2322 , T1:1 , T2:
Cond:1604, Score:6758.467, sT:42205, sP:3.4179, sB:0.7529, nT:132350, nP:4.0819, nB:0.8385
Cond:472, Score:6754.621, sT:43784, sP:3.3968, sB:0.7171, nT:137181, nP:4.0416, nB:0.8246
Cond:3012, Score:6721.211, sT:42707, sP:3.452, sB:0.745, nT:133134, nP:4.1187, nB:0.8371
Cond:488, Score:6712.825, sT:47614, sP:3.443, sB:0.6504, nT:151827, nP:4.0963, nB:0.795
Cond:3444, Score:6695.285, sT:42496, sP:3.3786, sB:0.728, nT:132209, nP:4.0269, nB:0.8414
Cond:1862, Score:6682.742, sT:43185, sP:3.4484, sB:0.7297, nT:133075, nP:4.0849, nB:0.8355
Cond:486, Score:6675.391, sT:41633, sP:3.4332, sB:0.7586, nT:130141, nP:4.0899, nB:0.8337
Cond:24, Score:6662.267, sT:43573, sP:3.4103, sB:0.7121, nT:135894, nP:4.0545, nB:0.8227
Cond:52, Score:6646.595, sT:41302, sP:3.4499, sB:0.7595, nT:129576, nP:4.0879, nB:0.8424
Cond:696, Score:6645.779, sT:45255, sP:3.3827, sB:0.6718, nT:142389, nP:4.026, nB:0.8092 , T3:1604,472,3012,488,3444,1862,486,24,52,696, 
LowScoreRank1 , T0:66 , T1:1 , T2:
Cond:52, Score:2170.594, sT:39011, sP:3.3656, sB:0.7596, nT:126658, nP:4.0589, nB:0.8487
Cond:68, Score:2160.637, sT:45703, sP:3.4948, sB:0.6697, nT:141585, nP:4.0837, nB:0.8147
Cond:80, Score:2149.845, sT:45184, sP:3.425, sB:0.6819, nT:133902, nP:4.0253, nB:0.786
Cond:82, Score:2149.018, sT:55936, sP:3.7887, sB:0.6012, nT:161979, nP:4.1403, nB:0.7304
Cond:306, Score:2129.354, sT:43445, sP:3.509, sB:0.6963, nT:132910, nP:4.0741, nB:0.824
Cond:724, Score:2106.273, sT:39608, sP:3.3756, sB:0.7183, nT:129611, nP:4.0668, nB:0.8398
Cond:528, Score:2103.733, sT:38041, sP:3.3281, sB:0.7435, nT:125164, nP:4.0511, nB:0.8341
Cond:1202, Score:2083.389, sT:40362, sP:3.3789, sB:0.7004, nT:130539, nP:4.0578, nB:0.8245
Cond:530, Score:2081.293, sT:50040, sP:3.6222, sB:0.6029, nT:155159, nP:4.1372, nB:0.7688
Cond:514, Score:2078.341, sT:39209, sP:3.4048, sB:0.7202, nT:128167, nP:4.0829, nB:0.8349 , T3:52,68,80,82,306,724,528,1202,530,514, 
LowScoreRank2 , T0:66 , T1:1 , T2:
Cond:82, Score:3774.609, sT:55936, sP:3.7887, sB:0.6012, nT:161979, nP:4.1403, nB:0.7304
Cond:68, Score:3761.317, sT:45703, sP:3.4948, sB:0.6697, nT:141585, nP:4.0837, nB:0.8147
Cond:52, Score:3748.41, sT:39011, sP:3.3656, sB:0.7596, nT:126658, nP:4.0589, nB:0.8487
Cond:80, Score:3733.697, sT:45184, sP:3.425, sB:0.6819, nT:133902, nP:4.0253, nB:0.786
Cond:306, Score:3695.258, sT:43445, sP:3.509, sB:0.6963, nT:132910, nP:4.0741, nB:0.824
Cond:724, Score:3642.941, sT:39608, sP:3.3756, sB:0.7183, nT:129611, nP:4.0668, nB:0.8398
Cond:530, Score:3642.576, sT:50040, sP:3.6222, sB:0.6029, nT:155159, nP:4.1372, nB:0.7688
Cond:528, Score:3629.495, sT:38041, sP:3.3281, sB:0.7435, nT:125164, nP:4.0511, nB:0.8341
Cond:1634, Score:3613.577, sT:47951, sP:3.4827, sB:0.5902, nT:158515, nP:4.17, nB:0.7855
Cond:1202, Score:3606.074, sT:40362, sP:3.3789, sB:0.7004, nT:130539, nP:4.0578, nB:0.8245 , T3:82,68,52,80,306,724,530,528,1634,1202, 
LowScoreRank3 , T0:66 , T1:1 , T2:
Cond:82, Score:6633.927, sT:55936, sP:3.7887, sB:0.6012, nT:161979, nP:4.1403, nB:0.7304
Cond:68, Score:6552.41, sT:45703, sP:3.4948, sB:0.6697, nT:141585, nP:4.0837, nB:0.8147
Cond:80, Score:6488.448, sT:45184, sP:3.425, sB:0.6819, nT:133902, nP:4.0253, nB:0.786
Cond:52, Score:6477.93, sT:39011, sP:3.3656, sB:0.7596, nT:126658, nP:4.0589, nB:0.8487
Cond:306, Score:6417.036, sT:43445, sP:3.509, sB:0.6963, nT:132910, nP:4.0741, nB:0.824
Cond:530, Score:6379.614, sT:50040, sP:3.6222, sB:0.6029, nT:155159, nP:4.1372, nB:0.7688
Cond:84, Score:6333.084, sT:60327, sP:3.9428, sB:0.5402, nT:184874, nP:4.3747, nB:0.6697
Cond:1634, Score:6327.938, sT:47951, sP:3.4827, sB:0.5902, nT:158515, nP:4.17, nB:0.7855
Cond:740, Score:6311.988, sT:48600, sP:3.5122, sB:0.5924, nT:155825, nP:4.1515, nB:0.7759
Cond:724, Score:6305.524, sT:39608, sP:3.3756, sB:0.7183, nT:129611, nP:4.0668, nB:0.8398 , T3:82,68,80,52,306,530,84,1634,740,724, 
LowScoreRank1 , T0:1065 , T1:1 , T2:
Cond:52, Score:2302.445, sT:41574, sP:3.3983, sB:0.7741, nT:128850, nP:4.0304, nB:0.875
Cond:6717, Score:2288.705, sT:41361, sP:3.3821, sB:0.7703, nT:128190, nP:4.0276, nB:0.8723
Cond:3327, Score:2285.399, sT:41866, sP:3.3907, sB:0.7631, nT:129697, nP:4.0305, nB:0.8634
Cond:6941, Score:2284.089, sT:41222, sP:3.3839, sB:0.7713, nT:127736, nP:4.0277, nB:0.8725
Cond:7149, Score:2284.053, sT:41464, sP:3.3832, sB:0.7675, nT:128486, nP:4.0263, nB:0.8684
Cond:1977, Score:2283.356, sT:41666, sP:3.3857, sB:0.7636, nT:129116, nP:4.0262, nB:0.8677
Cond:6697, Score:2283.245, sT:41216, sP:3.3853, sB:0.7709, nT:127746, nP:4.0297, nB:0.8736
Cond:1081, Score:2282.671, sT:41483, sP:3.3855, sB:0.7684, nT:128477, nP:4.0249, nB:0.8642
Cond:6903, Score:2282.608, sT:41218, sP:3.386, sB:0.7711, nT:127762, nP:4.03, nB:0.8724
Cond:6907, Score:2281.53, sT:41775, sP:3.3888, sB:0.7633, nT:129450, nP:4.0333, nB:0.8624 , T3:52,6717,3327,6941,7149,1977,6697,1081,6903,6907, 
LowScoreRank2 , T0:1065 , T1:1 , T2:
Cond:52, Score:3985.269, sT:41574, sP:3.3983, sB:0.7741, nT:128850, nP:4.0304, nB:0.875
Cond:6717, Score:3960.421, sT:41361, sP:3.3821, sB:0.7703, nT:128190, nP:4.0276, nB:0.8723
Cond:1961, Score:3957.33, sT:43587, sP:3.4263, sB:0.7383, nT:135985, nP:4.0814, nB:0.8485
Cond:3327, Score:3957.077, sT:41866, sP:3.3907, sB:0.7631, nT:129697, nP:4.0305, nB:0.8634
Cond:7149, Score:3952.828, sT:41464, sP:3.3832, sB:0.7675, nT:128486, nP:4.0263, nB:0.8684
Cond:1977, Score:3952.811, sT:41666, sP:3.3857, sB:0.7636, nT:129116, nP:4.0262, nB:0.8677
Cond:6941, Score:3951.718, sT:41222, sP:3.3839, sB:0.7713, nT:127736, nP:4.0277, nB:0.8725
Cond:6697, Score:3950.331, sT:41216, sP:3.3853, sB:0.7709, nT:127746, nP:4.0297, nB:0.8736
Cond:1081, Score:3950.227, sT:41483, sP:3.3855, sB:0.7684, nT:128477, nP:4.0249, nB:0.8642
Cond:6907, Score:3949.866, sT:41775, sP:3.3888, sB:0.7633, nT:129450, nP:4.0333, nB:0.8624 , T3:52,6717,1961,3327,7149,1977,6941,6697,1081,6907, 
LowScoreRank3 , T0:1065 , T1:1 , T2:
Cond:52, Score:6902.719, sT:41574, sP:3.3983, sB:0.7741, nT:128850, nP:4.0304, nB:0.875
Cond:1049, Score:6873.329, sT:46712, sP:3.5121, sB:0.716, nT:147083, nP:4.1904, nB:0.7809
Cond:1961, Score:6872.877, sT:43587, sP:3.4263, sB:0.7383, nT:135985, nP:4.0814, nB:0.8485
Cond:6717, Score:6857.834, sT:41361, sP:3.3821, sB:0.7703, nT:128190, nP:4.0276, nB:0.8723
Cond:3327, Score:6856.156, sT:41866, sP:3.3907, sB:0.7631, nT:129697, nP:4.0305, nB:0.8634
Cond:1977, Score:6847.512, sT:41666, sP:3.3857, sB:0.7636, nT:129116, nP:4.0262, nB:0.8677
Cond:7149, Score:6845.473, sT:41464, sP:3.3832, sB:0.7675, nT:128486, nP:4.0263, nB:0.8684
Cond:6907, Score:6842.775, sT:41775, sP:3.3888, sB:0.7633, nT:129450, nP:4.0333, nB:0.8624
Cond:6941, Score:6841.519, sT:41222, sP:3.3839, sB:0.7713, nT:127736, nP:4.0277, nB:0.8725
Cond:1081, Score:6840.592, sT:41483, sP:3.3855, sB:0.7684, nT:128477, nP:4.0249, nB:0.8642 , T3:52,1049,1961,6717,3327,1977,7149,6907,6941,1081, 
LowScoreRank1 , T0:6851 , T1:1 , T2:
Cond:6907, Score:2312.223, sT:41382, sP:3.4211, sB:0.7947, nT:127001, nP:4.0526, nB:0.8627
Cond:52, Score:2308.381, sT:40828, sP:3.4356, sB:0.8031, nT:125214, nP:4.0543, nB:0.8748
Cond:6699, Score:2305.957, sT:40841, sP:3.418, sB:0.8007, nT:125169, nP:4.0495, nB:0.8692
Cond:6697, Score:2304.916, sT:40641, sP:3.4193, sB:0.8029, nT:124690, nP:4.0496, nB:0.8737
Cond:3547, Score:2303.906, sT:40691, sP:3.4219, sB:0.8011, nT:124908, nP:4.053, nB:0.8756
Cond:1515, Score:2303.841, sT:40777, sP:3.4265, sB:0.8012, nT:125061, nP:4.0581, nB:0.8735
Cond:6681, Score:2303.424, sT:41918, sP:3.4171, sB:0.7815, nT:129387, nP:4.0599, nB:0.8517
Cond:7339, Score:2302.83, sT:40860, sP:3.4183, sB:0.7984, nT:125250, nP:4.0486, nB:0.8701
Cond:5115, Score:2302.457, sT:40704, sP:3.4214, sB:0.8008, nT:124966, nP:4.0525, nB:0.8733
Cond:7321, Score:2301.529, sT:41459, sP:3.4262, sB:0.7899, nT:127749, nP:4.0627, nB:0.8583 , T3:6907,52,6699,6697,3547,1515,6681,7339,5115,7321, 
LowScoreRank2 , T0:6851 , T1:1 , T2:
Cond:6907, Score:3998.464, sT:41382, sP:3.4211, sB:0.7947, nT:127001, nP:4.0526, nB:0.8627
Cond:52, Score:3989.345, sT:40828, sP:3.4356, sB:0.8031, nT:125214, nP:4.0543, nB:0.8748
Cond:6681, Score:3986.447, sT:41918, sP:3.4171, sB:0.7815, nT:129387, nP:4.0599, nB:0.8517
Cond:6699, Score:3984.871, sT:40841, sP:3.418, sB:0.8007, nT:125169, nP:4.0495, nB:0.8692
Cond:6697, Score:3982.318, sT:40641, sP:3.4193, sB:0.8029, nT:124690, nP:4.0496, nB:0.8737
Cond:1515, Score:3981.179, sT:40777, sP:3.4265, sB:0.8012, nT:125061, nP:4.0581, nB:0.8735
Cond:3547, Score:3981.067, sT:40691, sP:3.4219, sB:0.8011, nT:124908, nP:4.053, nB:0.8756
Cond:7321, Score:3980.736, sT:41459, sP:3.4262, sB:0.7899, nT:127749, nP:4.0627, nB:0.8583
Cond:7339, Score:3979.766, sT:40860, sP:3.4183, sB:0.7984, nT:125250, nP:4.0486, nB:0.8701
Cond:5115, Score:3978.538, sT:40704, sP:3.4214, sB:0.8008, nT:124966, nP:4.0525, nB:0.8733 , T3:6907,52,6681,6699,6697,1515,3547,7321,7339,5115, 
LowScoreRank3 , T0:6851 , T1:1 , T2:
Cond:6907, Score:6918.951, sT:41382, sP:3.4211, sB:0.7947, nT:127001, nP:4.0526, nB:0.8627
Cond:6681, Score:6903.753, sT:41918, sP:3.4171, sB:0.7815, nT:129387, nP:4.0599, nB:0.8517
Cond:52, Score:6898.893, sT:40828, sP:3.4356, sB:0.8031, nT:125214, nP:4.0543, nB:0.8748
Cond:6699, Score:6890.644, sT:40841, sP:3.418, sB:0.8007, nT:125169, nP:4.0495, nB:0.8692
Cond:7321, Score:6889.635, sT:41459, sP:3.4262, sB:0.7899, nT:127749, nP:4.0627, nB:0.8583
Cond:6697, Score:6884.944, sT:40641, sP:3.4193, sB:0.8029, nT:124690, nP:4.0496, nB:0.8737
Cond:1515, Score:6884.213, sT:40777, sP:3.4265, sB:0.8012, nT:125061, nP:4.0581, nB:0.8735
Cond:3547, Score:6883.649, sT:40691, sP:3.4219, sB:0.8011, nT:124908, nP:4.053, nB:0.8756
Cond:7339, Score:6882.343, sT:40860, sP:3.4183, sB:0.7984, nT:125250, nP:4.0486, nB:0.8701
Cond:5115, Score:6879.226, sT:40704, sP:3.4214, sB:0.8008, nT:124966, nP:4.0525, nB:0.8733 , T3:6907,6681,52,6699,7321,6697,1515,3547,7339,5115, 
LowScoreRank1 , T0:1618 , T1:1 , T2:
Cond:722, Score:2368.046, sT:41693, sP:3.3033, sB:0.8108, nT:127102, nP:3.9768, nB:0.8115
Cond:706, Score:2365.89, sT:41245, sP:3.2873, sB:0.8203, nT:124212, nP:3.954, nB:0.8056
Cond:736, Score:2354.751, sT:41375, sP:3.2857, sB:0.8122, nT:124819, nP:3.9549, nB:0.8042
Cond:1602, Score:2327.815, sT:41662, sP:3.3448, sB:0.7931, nT:126372, nP:3.9756, nB:0.833
Cond:52, Score:2317.639, sT:41904, sP:3.3171, sB:0.7921, nT:126701, nP:3.9726, nB:0.7972
Cond:708, Score:2313.697, sT:41577, sP:3.301, sB:0.7943, nT:125321, nP:3.9568, nB:0.797
Cond:720, Score:2305.276, sT:41074, sP:3.2849, sB:0.8001, nT:123151, nP:3.9523, nB:0.7942
Cond:690, Score:2299.342, sT:41070, sP:3.287, sB:0.7986, nT:123126, nP:3.9524, nB:0.7919
Cond:710, Score:2297.159, sT:42684, sP:3.3089, sB:0.7686, nT:129159, nP:3.9733, nB:0.7885
Cond:1586, Score:2296.641, sT:41248, sP:3.3024, sB:0.7918, nT:123849, nP:3.9545, nB:0.8037 , T3:722,706,736,1602,52,708,720,690,710,1586, 
LowScoreRank2 , T0:1618 , T1:1 , T2:
Cond:722, Score:4091.641, sT:41693, sP:3.3033, sB:0.8108, nT:127102, nP:3.9768, nB:0.8115
Cond:706, Score:4083.791, sT:41245, sP:3.2873, sB:0.8203, nT:124212, nP:3.954, nB:0.8056
Cond:736, Score:4065.693, sT:41375, sP:3.2857, sB:0.8122, nT:124819, nP:3.9549, nB:0.8042
Cond:1602, Score:4024.06, sT:41662, sP:3.3448, sB:0.7931, nT:126372, nP:3.9756, nB:0.833
Cond:52, Score:4005.22, sT:41904, sP:3.3171, sB:0.7921, nT:126701, nP:3.9726, nB:0.7972
Cond:708, Score:3996.405, sT:41577, sP:3.301, sB:0.7943, nT:125321, nP:3.9568, nB:0.797
Cond:720, Score:3978.396, sT:41074, sP:3.2849, sB:0.8001, nT:123151, nP:3.9523, nB:0.7942
Cond:710, Score:3974.31, sT:42684, sP:3.3089, sB:0.7686, nT:129159, nP:3.9733, nB:0.7885
Cond:690, Score:3968.101, sT:41070, sP:3.287, sB:0.7986, nT:123126, nP:3.9524, nB:0.7919
Cond:1586, Score:3965.55, sT:41248, sP:3.3024, sB:0.7918, nT:123849, nP:3.9545, nB:0.8037 , T3:722,706,736,1602,52,708,720,710,690,1586, 
LowScoreRank3 , T0:1618 , T1:1 , T2:
Cond:722, Score:7074.134, sT:41693, sP:3.3033, sB:0.8108, nT:127102, nP:3.9768, nB:0.8115
Cond:706, Score:7053.289, sT:41245, sP:3.2873, sB:0.8203, nT:124212, nP:3.954, nB:0.8056
Cond:736, Score:7024.012, sT:41375, sP:3.2857, sB:0.8122, nT:124819, nP:3.9549, nB:0.8042
Cond:1602, Score:6960.7, sT:41662, sP:3.3448, sB:0.7931, nT:126372, nP:3.9756, nB:0.833
Cond:52, Score:6925.829, sT:41904, sP:3.3171, sB:0.7921, nT:126701, nP:3.9726, nB:0.7972
Cond:708, Score:6907.088, sT:41577, sP:3.301, sB:0.7943, nT:125321, nP:3.9568, nB:0.797
Cond:710, Score:6880.179, sT:42684, sP:3.3089, sB:0.7686, nT:129159, nP:3.9733, nB:0.7885
Cond:720, Score:6869.909, sT:41074, sP:3.2849, sB:0.8001, nT:123151, nP:3.9523, nB:0.7942
Cond:690, Score:6852.036, sT:41070, sP:3.287, sB:0.7986, nT:123126, nP:3.9524, nB:0.7919
Cond:1586, Score:6851.341, sT:41248, sP:3.3024, sB:0.7918, nT:123849, nP:3.9545, nB:0.8037 , T3:722,706,736,1602,52,708,710,720,690,1586, 
LowScoreRank1 , T0:7436 , T1:1 , T2:
Cond:7644, Score:2301.862, sT:39456, sP:3.3002, sB:0.8031, nT:115950, nP:3.8031, nB:0.8789
Cond:7212, Score:2290.348, sT:39492, sP:3.3339, sB:0.8049, nT:116114, nP:3.8733, nB:0.8828
Cond:7420, Score:2283.355, sT:37019, sP:3.27, sB:0.8366, nT:104987, nP:3.7105, nB:0.9174
Cond:4958, Score:2263.874, sT:40080, sP:3.3116, sB:0.7831, nT:118233, nP:3.8325, nB:0.8522
Cond:7196, Score:2259.339, sT:35229, sP:3.2488, sB:0.8626, nT:96879, nP:3.6345, nB:0.939
Cond:6988, Score:2250.322, sT:36654, sP:3.2877, sB:0.8374, nT:102901, nP:3.7226, nB:0.9117
Cond:4734, Score:2250.167, sT:37779, sP:3.2802, sB:0.8117, nT:107795, nP:3.7397, nB:0.8935
Cond:7450, Score:2228.463, sT:38474, sP:3.2779, sB:0.7902, nT:111287, nP:3.7525, nB:0.8693
Cond:6766, Score:2227.699, sT:38915, sP:3.3025, sB:0.7892, nT:112945, nP:3.7868, nB:0.8577
Cond:6750, Score:2225.403, sT:35770, sP:3.2313, sB:0.8337, nT:98506, nP:3.5999, nB:0.9137 , T3:7644,7212,7420,4958,7196,6988,4734,7450,6766,6750, 
LowScoreRank2 , T0:7436 , T1:1 , T2:
Cond:7644, Score:3968.221, sT:39456, sP:3.3002, sB:0.8031, nT:115950, nP:3.8031, nB:0.8789
Cond:7212, Score:3948.446, sT:39492, sP:3.3339, sB:0.8049, nT:116114, nP:3.8733, nB:0.8828
Cond:7420, Score:3921.102, sT:37019, sP:3.27, sB:0.8366, nT:104987, nP:3.7105, nB:0.9174
Cond:4958, Score:3905.774, sT:40080, sP:3.3116, sB:0.7831, nT:118233, nP:3.8325, nB:0.8522
Cond:4734, Score:3868.597, sT:37779, sP:3.2802, sB:0.8117, nT:107795, nP:3.7397, nB:0.8935
Cond:7196, Score:3867.666, sT:35229, sP:3.2488, sB:0.8626, nT:96879, nP:3.6345, nB:0.939
Cond:6988, Score:3861.337, sT:36654, sP:3.2877, sB:0.8374, nT:102901, nP:3.7226, nB:0.9117
Cond:7004, Score:3848.844, sT:42034, sP:3.444, sB:0.7571, nT:128686, nP:4.0931, nB:0.8303
Cond:7868, Score:3845.078, sT:42800, sP:3.3511, sB:0.7202, nT:132147, nP:3.9377, nB:0.8164
Cond:6766, Score:3836.463, sT:38915, sP:3.3025, sB:0.7892, nT:112945, nP:3.7868, nB:0.8577 , T3:7644,7212,7420,4958,4734,7196,6988,7004,7868,6766, 
LowScoreRank3 , T0:7436 , T1:1 , T2:
Cond:7644, Score:6845.001, sT:39456, sP:3.3002, sB:0.8031, nT:115950, nP:3.8031, nB:0.8789
Cond:7212, Score:6811.016, sT:39492, sP:3.3339, sB:0.8049, nT:116114, nP:3.8733, nB:0.8828
Cond:4958, Score:6742.551, sT:40080, sP:3.3116, sB:0.7831, nT:118233, nP:3.8325, nB:0.8522
Cond:7420, Score:6737.286, sT:37019, sP:3.27, sB:0.8366, nT:104987, nP:3.7105, nB:0.9174
Cond:7868, Score:6669.676, sT:42800, sP:3.3511, sB:0.7202, nT:132147, nP:3.9377, nB:0.8164
Cond:7004, Score:6665.462, sT:42034, sP:3.444, sB:0.7571, nT:128686, nP:4.0931, nB:0.8303
Cond:4734, Score:6654.845, sT:37779, sP:3.2802, sB:0.8117, nT:107795, nP:3.7397, nB:0.8935
Cond:6988, Score:6629.288, sT:36654, sP:3.2877, sB:0.8374, nT:102901, nP:3.7226, nB:0.9117
Cond:7660, Score:6624.768, sT:47209, sP:3.4761, sB:0.6688, nT:154559, nP:4.2252, nB:0.7473
Cond:7196, Score:6624.336, sT:35229, sP:3.2488, sB:0.8626, nT:96879, nP:3.6345, nB:0.939 , T3:7644,7212,4958,7420,7868,7004,4734,6988,7660,7196, 
LowScoreRank1 , T0:5388 , T1:1 , T2:
Cond:3372, Score:2289.404, sT:42787, sP:3.4071, sB:0.7582, nT:132608, nP:4.0488, nB:0.8403
Cond:3150, Score:2288.863, sT:42364, sP:3.4175, sB:0.7669, nT:131105, nP:4.0617, nB:0.8464
Cond:3148, Score:2285.387, sT:42504, sP:3.4141, sB:0.7624, nT:131536, nP:4.0579, nB:0.8445
Cond:7880, Score:2283.956, sT:43019, sP:3.4008, sB:0.7523, nT:133511, nP:4.0421, nB:0.8324
Cond:7434, Score:2280.598, sT:42385, sP:3.4166, sB:0.7639, nT:131148, nP:4.0635, nB:0.8428
Cond:52, Score:2280.214, sT:42479, sP:3.4371, sB:0.7656, nT:131207, nP:4.0738, nB:0.8439
Cond:3596, Score:2280.144, sT:43119, sP:3.3972, sB:0.7477, nT:133832, nP:4.0377, nB:0.8326
Cond:7642, Score:2279.981, sT:42510, sP:3.4163, sB:0.7617, nT:131686, nP:4.064, nB:0.8403
Cond:7450, Score:2279.605, sT:43952, sP:3.4141, sB:0.7367, nT:138255, nP:4.0702, nB:0.8211
Cond:5390, Score:2278.49, sT:42791, sP:3.4091, sB:0.7551, nT:132761, nP:4.0522, nB:0.8353 , T3:3372,3150,3148,7880,7434,52,3596,7642,7450,5390, 
LowScoreRank2 , T0:5388 , T1:1 , T2:
Cond:3372, Score:3967.412, sT:42787, sP:3.4071, sB:0.7582, nT:132608, nP:4.0488, nB:0.8403
Cond:3150, Score:3964.164, sT:42364, sP:3.4175, sB:0.7669, nT:131105, nP:4.0617, nB:0.8464
Cond:7880, Score:3959.092, sT:43019, sP:3.4008, sB:0.7523, nT:133511, nP:4.0421, nB:0.8324
Cond:3148, Score:3958.963, sT:42504, sP:3.4141, sB:0.7624, nT:131536, nP:4.0579, nB:0.8445
Cond:7450, Score:3957.341, sT:43952, sP:3.4141, sB:0.7367, nT:138255, nP:4.0702, nB:0.8211
Cond:3596, Score:3953.247, sT:43119, sP:3.3972, sB:0.7477, nT:133832, nP:4.0377, nB:0.8326
Cond:7434, Score:3949.893, sT:42385, sP:3.4166, sB:0.7639, nT:131148, nP:4.0635, nB:0.8428
Cond:52, Score:3949.566, sT:42479, sP:3.4371, sB:0.7656, nT:131207, nP:4.0738, nB:0.8439
Cond:7642, Score:3949.507, sT:42510, sP:3.4163, sB:0.7617, nT:131686, nP:4.064, nB:0.8403
Cond:5390, Score:3948.524, sT:42791, sP:3.4091, sB:0.7551, nT:132761, nP:4.0522, nB:0.8353 , T3:3372,3150,7880,3148,7450,3596,7434,52,7642,5390, 
LowScoreRank3 , T0:5388 , T1:1 , T2:
Cond:3372, Score:6879.926, sT:42787, sP:3.4071, sB:0.7582, nT:132608, nP:4.0488, nB:0.8403
Cond:7450, Score:6874.619, sT:43952, sP:3.4141, sB:0.7367, nT:138255, nP:4.0702, nB:0.8211
Cond:3150, Score:6870.269, sT:42364, sP:3.4175, sB:0.7669, nT:131105, nP:4.0617, nB:0.8464
Cond:7880, Score:6867.453, sT:43019, sP:3.4008, sB:0.7523, nT:133511, nP:4.0421, nB:0.8324
Cond:3148, Score:6862.679, sT:42504, sP:3.4141, sB:0.7624, nT:131536, nP:4.0579, nB:0.8445
Cond:3596, Score:6858.652, sT:43119, sP:3.3972, sB:0.7477, nT:133832, nP:4.0377, nB:0.8326
Cond:7672, Score:6851.267, sT:43485, sP:3.3912, sB:0.7397, nT:135313, nP:4.0348, nB:0.8246
Cond:3164, Score:6850.048, sT:44597, sP:3.3996, sB:0.7162, nT:140620, nP:4.0542, nB:0.8184
Cond:7658, Score:6848.542, sT:43705, sP:3.4159, sB:0.74, nT:137215, nP:4.083, nB:0.8203
Cond:5390, Score:6847.223, sT:42791, sP:3.4091, sB:0.7551, nT:132761, nP:4.0522, nB:0.8353 , T3:3372,7450,3150,7880,3148,3596,7672,3164,7658,5390, 


 */
