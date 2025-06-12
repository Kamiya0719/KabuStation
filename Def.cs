using System.Collections.Generic;

namespace CSharp_sample
{
	public enum TimeIdx
	{
		T0000,
		T1525,
		T1520,
		T1515,
		T1500,
		T1420,
		T0900,
	}

	class Def
	{
		// �v��500�̔����� ���̓�����ёO�����͔������T����
		public const string Pro500Day = "2025/06/18";
		// ��\�ƂȂ���� �ŐV�܂ł��邱�Ƃ��K�{ �v��500�ɂ��Ȃ���΂Ȃ�Ȃ�
		public const string CapitalSymbol = "1417";

		// �g�����v���[�h todo ���z�����half�ɂ��邩�� ���؂̓I�[�o�[�A�b�v���̂�(�S��) �w���̓I�[�o�[�_�E�����̂݁H
		public const bool TranpMode = false;
		public const bool SubTranpMode = false;
		public const int JScoreOverUp = 10;
		public const int JScoreOverDown = 20;
		public const int JScoreNotGet = -10;
		// �I�[�o�[�_�E��(5������+��)�ƃI�[�o�[�A�b�v(4���㏸)�ɂ��Ă̒�` 
		// �I�[�o�[�_�E������CodeDaily������(13��50���Ƃ��Ɉ�x�����H�t���O�ŊǗ����邩) ���ɂ�������㏑���A�Ȃ��Ȃ�new


		// todo Sp�n�̊�w����x�^�ł�
		public const int SpBuyBasePricew = 150000;
		public static readonly Dictionary<string, int> SpBuyInfo = new Dictionary<string, int>() {
			{ "1435", 0 },
			{ "2193", 0 },
			{ "2930", 137 },
			//{ "3071", 99 }, ����
			{ "3656", 121 },
			{ "4571", 0 },
			{ "4586", 64 },
			{ "4591", 0 },
			//{ "4596", 46 }, ����
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


		public const int TypePro = 1; // �v��500�ʏ�M�p����
		public const int TypeSp = 2; // ��z������
		public const int TypeKara = 3; // �󔄂�

		// �ő�w���z55�����Ƃ肠�����^�̏��
		public const double BuyMax = 1.1;
		// �Œ�w���������z(�O�����[�ɔ������Ƃ��Ƃ�)
		public const int BuyLowestPrice = 100000;

		// JScore�ɉ����čw�����ɔ{���������� 4�Ȃ甃��Ȃ�
		public static readonly double[] BuyJScoreRatio = new double[5] {
			1.0, 0.7, 0.5, 0.3, 0
		};
		// ���؃��C��(���o���σX�R�A,{����%,�O����-%})
		public static readonly double[,] LossCutRatio = new double[5, 2] {
			//{6, 3.5},{3.5, 1.5},{3, 1},{2, 0.5},{0.5, 0},
			{6, 3.5},{3.5, 1.5},{3, 1},{2, 0.5},{0.5, 0},
		};
		// todo
		public static readonly double[,] LossCutRatioHalf = new double[5, 2] {
			{5, 2.5},{2.5, 1},{2, 0.5},{1, 0},{0, 0},
		};

		// �������z���ς�����Ƃ��A�L�����Z�����Ē������Ȃ������̍��̊���
		public const double CancelDiff = 1.003;
		public const double CancelDiffNum = 1.1;



		// ����̈����������̏��(�ۏ؋��ɑ΂��銄��) 800��*0.1=80
		public const double BuyMaxPriceCode = 0.115;
		// ����̍w�����(�ۏ؋��ɑ΂��銄��) 800��*0.3=240
		public const double BuyMaxPriceDay = 0.33;
		// ����̍w�����(�ۏ؋��ɑ΂��銄��)�ł�����������ȉ��Ȃ炱��ɕ␳ 800��*0.43=320
		public const double BuyMaxPriceDaySub = 0.45;
		// ��T�Ԃ̍w�����
		public const double BuyMaxPriceWeek = 0.75;
		// ��T�Ԃ̍w������ł�����������ȉ��Ȃ炱��ɕ␳(800��*0.8=640��)
		public const double BuyMaxPriceWeekSub = 0.8;
		// �S�̂̍w�����
		public const double BuyMaxPriceAll = 2.15;
		// ���z����{��
		public static readonly Dictionary<int, double> idealSellRatio = new Dictionary<int, double>() {
			{ 30, 1.02 }, { 20, 1.03 }, { 10, 1.04 }, { 5, 1.07 }
		};
		public static readonly Dictionary<int, double> idealSellRatioHalf = new Dictionary<int, double>() {
			{ 30, 1.01 }, { 20, 1.02 }, { 10, 1.03 }, { 5, 1.04 }
		};




		/*
		 			if (now.Hour == 15) {
				if (now.Minute >= 25) return 1;
				if (now.Minute >= 20) return 2;
				if (now.Minute >= 15) return 3;
				return 4;
			}
			if (now.Hour == 14 && now.Minute >= 20) return 5;
			return 6;
		 
		 */
	}

	/**
	 1435,1,1,100,151,0,0,True,False,0,0,0,0,20251231,2,False
1928,1,1,100,3291,200,3519,False,False,0,0,0,200,20260131,1,False
2331,1,1,100,1005.5,900,1052,False,False,0,0,0,900,20260331,1,False
2501,1,1,100,7326,100,7935,False,False,0,0,0,100,20251231,1,False
2930,1,1,100,143,700,146,True,False,0,0,0,700,20260228,2,False
3046,1,10,100,8450,100,9210,False,False,0,0,0,100,20250831,1,False
3139,1,5,100,3530,200,3575,False,False,0,0,0,200,20251130,1,False
3591,1,1,100,4900,100,5276,False,False,0,0,0,100,20260331,1,False
3656,1,1,100,115,0,0,True,False,0,0,0,0,20251231,2,False
4323,1,1,100,1913,500,2017,False,False,0,0,0,500,20260331,1,False
4571,1,1,100,129,0,0,True,False,0,0,0,0,20260331,2,False
4586,1,1,100,61,3300,63,False,False,0,0,0,3300,20251231,2,False
4591,1,1,100,92,0,0,True,False,0,0,0,0,20260331,2,False
4829,1,1,100,115,0,0,True,False,0,0,0,0,20260531,2,False
4883,1,1,100,76,2500,80,True,False,0,0,0,2500,20251231,2,False
5233,1,1,100,3644,0,0,True,False,0,0,0,0,20260331,1,False
5261,1,10,100,5030,0,0,True,False,0,0,0,0,20260331,1,False
6046,1,1,100,120,0,0,True,False,0,0,0,0,20250930,2,False
6054,1,1,100,146,0,0,True,False,0,0,0,0,20251231,2,False
6072,1,1,100,163,0,0,True,False,0,0,0,0,20260331,2,False
6093,1,1,100,137,0,0,True,False,0,0,0,0,20260228,2,False
6740,1,1,100,15,9000,18,False,False,0,0,0,9000,20260331,2,False
7545,1,1,100,2070,400,2228,False,False,0,0,0,400,20260220,1,False
7550,1,1,100,7768,100,8389,False,False,0,0,0,100,20260331,1,False
8202,1,1,100,165,0,0,True,False,0,0,0,0,20251231,2,False
8566,1,10,100,5200,0,0,True,False,0,0,0,0,20260331,1,False
8929,1,1,100,1796,0,0,True,False,0,0,0,0,20251231,1,False
8946,1,1,100,92,0,0,True,False,0,0,0,0,20251231,2,False

	 
	 */
}
