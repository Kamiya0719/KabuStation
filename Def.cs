using System.Collections.Generic;

namespace CSharp_sample
{
	class Def
	{

		// �g�����v���[�h todo ���z�����half�ɂ��邩�� ���؂̓I�[�o�[�A�b�v���̂�(�S��) �w���̓I�[�o�[�_�E�����̂݁H
		public const bool TranpMode = false;
		public const bool SubTranpMode = true;
		public const int JScoreOverUp = 10;
		public const int JScoreOverDown = 20;
		public const int JScoreNotGet = -10;
		// �I�[�o�[�_�E��(5������+��)�ƃI�[�o�[�A�b�v(4���㏸)�ɂ��Ă̒�` 
		// �I�[�o�[�_�E������CodeDaily������(13��50���Ƃ��Ɉ�x�����H�t���O�ŊǗ����邩) ���ɂ�������㏑���A�Ȃ��Ȃ�new


		// todo Sp�n�̊�w����x�^�ł�
		public const int SpBuyBasePricew = 300000;
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


		public const int TypePro = 1;
		public const int TypeSp = 2;

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


	}
}
