using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;

namespace CSharp_sample
{

	public class RequestParam
	{
		public bool isTest;
		public REQUEST_TYPE requestType;

		// ���������E���E�������
		public string symbol = "0"; // �����R�[�h
		public int exchange = 0;// �s��R�[�h ���؂��炢�����g��񂩁H

		// ��������
		public string side = "0"; // �����敪(1:��,2:��)
		public int cashMargin = 0; // �M�p�敪(2:�V�K,3:�ԍ�)
		public int qty = 0; // ��������
		public int frontOrderType = 0; // ���s����(10:���s,20:�w�l,30:�t�w�l)
		public int price = 0; // �������i(���s�Ȃ�0)
		public int expireDay = 0; // �����L������(yyyyMMdd�`���B�{���Ȃ�0)

		// ��������p
		public string orderId = ""; // �����ԍ� sendorder�̃��X�|���X�Ŏ󂯎��OrderID

		// �����o�^
		private List<object> symbols = new List<object>(); // �����̃��X�g

		public RequestParam(REQUEST_TYPE requestType)
		{
			this.isTest = RequestBasic.isTest;
			this.requestType = requestType;
		}

		// �����Z�b�g(���������E���E�������) exchange�͎s��R�[�h
		public void SetSymbol(int symbol, int exchange)
		{
			if (requestType == REQUEST_TYPE.REGISTER) {
				symbols.Add(new { Symbol = symbol.ToString().PadLeft(4, '0'), Exchange = exchange });
			} else {
				this.symbol = symbol.ToString();
				this.exchange = exchange;
			}
		}
		// ��������
		public void SetOrder(bool isBuy, int qty, int price, int expireDay)
		{
			this.side = isBuy ? "2" : "1";
			this.cashMargin = isBuy ? 2 : 3;
			this.qty = qty;
			this.frontOrderType = price == 0 ? 10 : 20;
			this.price = price;
			this.expireDay = expireDay;
		}
		// �������
		public void SetOrderCancel(string orderId) { this.orderId = orderId; }





		public string GetSymbol()
		{
			//return symbol.ToString().PadLeft(4, '0'); 
			return symbol;
		}

		public string GetUrl()
		{
			string url = "http://localhost:1808" + (isTest ? "1" : "0") + "/kabusapi/";
			url += requestUrls[requestType];
			if (requestType == REQUEST_TYPE.BOARD || requestType == REQUEST_TYPE.SYMBOL) {
				url += "/" + GetSymbol() + "@" + exchange; // �����R�[�h [�����R�[�h]@[�s��R�[�h]
			}
			if (!querySolidParams.ContainsKey(requestType)) return url;

			var builder = new UriBuilder(url);
			var param = System.Web.HttpUtility.ParseQueryString(builder.Query);
			foreach (KeyValuePair<string, string> pair in querySolidParams[requestType]) {
				param[pair.Key] = pair.Value;
			}
			builder.Query = param.ToString();

			return builder.ToString();
		}

		public object GetRequestBody()
		{
			if (requestType == REQUEST_TYPE.TOKEN) {
				return new {
					// todo
					APIPassword = isTest ? Secret.TestPass : Secret.Pass,
				};
			} else if (requestType == REQUEST_TYPE.SENDORDER) {
				return new {
					Password = Secret.SendPass, // �����p�X���[�h
					Symbol = GetSymbol(), // �����R�[�h
					Exchange = exchange,// �s��R�[�h ���؂��炢�����g��񂩁H
					SecurityType = 1, // ���i���
					Side = side, // �����敪(1:��,2:��)
					CashMargin = cashMargin, // �M�p�敪(2:�V�K,3:�ԍ�)
					MarginTradeType = 1, // �M�p����敪(1:���x�M�p)
					DelivType = side == "1" ? 2 : 0, // ��n�敪(0:�w��Ȃ�,2:���a���,3	:au�}�l�[�R�l�N�g)
					AccountType = 4, // �������(2:���,4	:����)
					Qty = qty, // ��������
					ClosePositionOrder = 0, // ���Ϗ���
					Price = price, // �������i(���s�Ȃ�0)
					ExpireDay = expireDay, // �����L������(yyyyMMdd�`���B�{���Ȃ�0)
					FrontOrderType = frontOrderType, // ���s����(10:���s,20:�w�l,30:�t�w�l)
				};
			} else if (requestType == REQUEST_TYPE.CANCELORDER) {
				return new {
					OrderId = orderId, // �����ԍ� sendorder�̃��X�|���X�Ŏ󂯎��OrderID
					Password = Secret.SendPass, // �����p�X���[�h
				};
			} else if (requestType == REQUEST_TYPE.REGISTER) {
				object[] tmp = new object[symbols.Count];
				for (int i = 0; i < symbols.Count; i++) tmp[i] = symbols[i];
				return new { Symbols = tmp, }; // �o�^��������̃��X�g
			}
			return null;
		}


		public string DebugLog()
		{

			return "PARAM Url:" + GetUrl() + ", symbol:" + GetSymbol() + ", qty:" + qty.ToString() + ", price:" + price.ToString() + ", side:" + side.ToString();
		}

		private static Dictionary<REQUEST_TYPE, string> requestUrls = new Dictionary<REQUEST_TYPE, string>() {
			{ REQUEST_TYPE.TOKEN, "token" }, { REQUEST_TYPE.SENDORDER, "sendorder" },
			{ REQUEST_TYPE.CANCELORDER, "cancelorder" }, { REQUEST_TYPE.WALLET, "wallet/margin" },
			{ REQUEST_TYPE.BOARD, "board" }, { REQUEST_TYPE.SYMBOL, "symbol" },
			{ REQUEST_TYPE.ORDERS, "orders" }, { REQUEST_TYPE.POSITIONS, "positions" },
			{ REQUEST_TYPE.REGISTER, "register" },{ REQUEST_TYPE.UNREGISTERALL, "unregister/all" },
			{ REQUEST_TYPE.RANKING, "ranking" },
		};
		// ����s�v
		private static Dictionary<REQUEST_TYPE, Dictionary<string, string>> querySolidParams = new Dictionary<REQUEST_TYPE, Dictionary<string, string>>() {
			{ REQUEST_TYPE.RANKING, new Dictionary<string, string>(){ { "Type", "2" }, { "ExchangeDivision", "T" } } },
		};
	}

	// �g�[�N���擾
	public class ResponseToken
	{
		public string resultCode;
		public string token;
	}
	// ����/�������
	public class ResponseOrder
	{
		public int Result; // ���ʃR�[�h 0�������B����ȊO�̓G���[�R�[�h�B
		public string OrderId; // ��t�����ԍ�
	}
	// ����]��(�M�p)
	public class ResponseWallet
	{
		public double? MarginAccountWallet; // �M�p�V�K�\�z
		public double? DepositkeepRate; // �ۏ؋��ێ���
		public double? ConsignmentDepositRate; // �ϑ��ۏ؋���
		public double? CashOfConsignmentDepositRate; // �����ϑ��ۏ؋���
	}
	// �������E���
	public class ResponseBoard
	{
		public string Symbol; // �����R�[�h
		public string SymbolName; // ������
		public int Exchange; // �s��R�[�h�������E�敨�E�I�v�V���������̏ꍇ�̂�
		public string ExchangeName; // �s�ꖼ�́������E�敨�E�I�v�V���������̏ꍇ�̂�
		public double CurrentPrice; // ���l
		public string CurrentPriceTime; // ���l����
		public string CurrentPriceChangeStatus; // ���l�O�l��r
		public int CurrentPriceStatus; // ���l�X�e�[�^�X
		public double CalcPrice; // �v�Z�p���l
		public double PreviousClose; // �O���I�l
		public string PreviousCloseTime; // �O���I�l���t
		public double ChangePreviousClose; // �O����(CurrentPrice-PreviousClose)
		public double ChangePreviousClosePer; // ������((CurrentPrice/PreviousClose-1)*100)
		public double OpeningPrice; // �n�l
		public string OpeningPriceTime; // �n�l����
		public double HighPrice; // ���l
		public string HighPriceTime; // ���l����
		public double LowPrice; // ���l
		public string LowPriceTime; // ���l����
		public double TradingVolume; // �������������E�敨�E�I�v�V���������̏ꍇ�̂�
		public string TradingVolumeTime; // �����������������E�敨�E�I�v�V���������̏ꍇ�̂�
		public double VWAP; // ���������d���ω��i�iVWAP�j�������E�敨�E�I�v�V���������̏ꍇ�̂�
		public double TradingValue; // ��������������E�敨�E�I�v�V���������̏ꍇ�̂�
		public double BidQty; // �ŗǔ��C�z���� ���@�������E�敨�E�I�v�V���������̏ꍇ�̂�
		public double BidPrice; // �ŗǔ��C�z�l�i ���@�������E�敨�E�I�v�V���������̏ꍇ�̂�
		public string BidTime; // �ŗǔ��C�z���� ���@�����������̏ꍇ�̂�
		public string BidSign; // �ŗǔ��C�z�t���O ���@�������E�敨�E�I�v�V���������̏ꍇ�̂�
		public double MarketOrderSellQty; // �����s���ʁ����������̏ꍇ�̂�
		public SellBuy Sell1; // ���C�z����1�{��
		public SellBuy Sell2; // ���C�z����2�{��
		public SellBuy Sell3; // ���C�z����3�{��
		public SellBuy Sell4; // ���C�z����4�{��
		public SellBuy Sell5; // ���C�z����5�{��
		public SellBuy Sell6; // ���C�z����6�{��
		public SellBuy Sell7; // ���C�z����7�{��
		public SellBuy Sell8; // ���C�z����8�{��
		public SellBuy Sell9; // ���C�z����9�{��
		public SellBuy Sell10; // ���C�z����10�{��
		public double AskQty; // �ŗǔ��C�z���� ���@�������E�敨�E�I�v�V���������̏ꍇ�̂�
		public double AskPrice; // �ŗǔ��C�z�l�i ���@�������E�敨�E�I�v�V���������̏ꍇ�̂�
		public string AskTime; // �ŗǔ��C�z���� ���@�����������̏ꍇ�̂�
		public string AskSign; // �ŗǔ��C�z�t���O ���@�������E�敨�E�I�v�V���������̏ꍇ�̂�
		public double MarketOrderBuyQty; // �����s���ʁ����������̏ꍇ�̂�
		public SellBuy Buy1; // ���C�z����1�{��
		public SellBuy Buy2; // ���C�z����2�{��
		public SellBuy Buy3; // ���C�z����3�{��
		public SellBuy Buy4; // ���C�z����4�{��
		public SellBuy Buy5; // ���C�z����5�{��
		public SellBuy Buy6; // ���C�z����6�{��
		public SellBuy Buy7; // ���C�z����7�{��
		public SellBuy Buy8; // ���C�z����8�{��
		public SellBuy Buy9; // ���C�z����9�{��
		public SellBuy Buy10; // ���C�z����10�{��
		public double OverSellQty; // OVER�C�z���ʁ����������̏ꍇ�̂�
		public double UnderBuyQty; // UNDER�C�z���ʁ����������̏ꍇ�̂�
		public double TotalMarketValue; //  �������z�����������̏ꍇ�̂�
		public double ClearingPrice; // ���Z�l���敨�����̏ꍇ�̂�
		public double IV; // �C���v���C�h�E�{���e�B���e�B���I�v�V�������������ʂ��̏ꍇ�̂�
		public double Gamma; // �K���}���I�v�V�������������ʂ��̏ꍇ�̂�
		public double Theta; // �Z�[�^���I�v�V�������������ʂ��̏ꍇ�̂�
		public double Vega; // �x�K���I�v�V�������������ʂ��̏ꍇ�̂�
		public double Delta; // �f���^���I�v�V�������������ʂ��̏ꍇ�̂�
		public int SecurityType; // �������

	}
	public class SellBuy
	{
		public string Time; // ���������������̏ꍇ�̂�
		public string Sign; // �C�z�t���O�������E�敨�E�I�v�V���������̏ꍇ�̂�
		public double Price; // �l�i�������E�敨�E�I�v�V���������̏ꍇ�̂�
		public double Qty; // ���ʁ������E�敨�E�I�v�V���������̏ꍇ�̂�
	}
	// �������
	public class ResponseSymbol
	{
		public string Symbol; // �����R�[�h
		public string SymbolName; // ������
		public string DisplayName; // �������́������E�敨�E�I�v�V���������̏ꍇ�̂�
		public int Exchange; // �s��R�[�h�������E�敨�E�I�v�V���������̏ꍇ�̂�
		public string ExchangeName; // �s�ꖼ�́������E�敨�E�I�v�V���������̏ꍇ�̂�
		public string BisCategory; // �Ǝ�R�[�h�������������̏ꍇ�̂�
		public double TotalMarketValue; // �������z�����������̏ꍇ�̂� �ǉ����o�̓t���O�Ffalse�̏ꍇ�Anull
		public double TotalStocks; // ���s�ς݊������i�犔�j�����������̏ꍇ�̂ݒǉ����o�̓t���O�Ffalse�̏ꍇ�Anull
		public double TradingUnit; // �����P�ʁ������E�敨�E�I�v�V���������̏ꍇ�̂�
		public int FiscalYearEndBasic; // ���Z���������������̏ꍇ�̂� �ǉ����o�̓t���O�Ffalse�̏ꍇ�Anull
		public string PriceRangeGroup; // �Ēl�O���[�v�������E�敨�E�I�v�V���������̏ꍇ�̂݁��e�Ēl�R�[�h���Ή����鏤�i�͈ȉ��ƂȂ�܂��B
		public bool KCMarginBuy; // ��ʐM�p�����t���O��true�̂Ƃ��A��ʐM�p(����) �܂��͈�ʐM�p(�f�C�g��) �������\�����������̏ꍇ�̂�
		public bool KCMarginSell; // ��ʐM�p�����t���O��true�̂Ƃ��A��ʐM�p(����) �܂��͈�ʐM�p(�f�C�g��) �������\�����������̏ꍇ�̂�
		public bool MarginBuy; // ���x�M�p�����t���O��true�̂Ƃ����x�M�p�����\�����������̏ꍇ�̂�
		public bool MarginSell; // ���x�M�p�����t���O��true�̂Ƃ����x�M�p�����\�����������̏ꍇ�̂�
		public double UpperLimit; // �l������������E�敨�E�I�v�V���������̏ꍇ�̂�
		public double LowerLimit; // �l�������������E�敨�E�I�v�V���������̏ꍇ�̂�
	}
	// �������Ɖ�
	public class ResponseOrders
	{
		public string ID; // �����ԍ�
		/* 1:�ҋ@�i�����ҋ@�j, 2:�������i�������M���j,3:�����ρi�����ρE�����ρj,4:����������M��, 5:�I���i�����G���[�E����ρE�S���E�����E�����؂�j*/
		public int State; // ��ԁ�OrderState�Ɠ���ł���
		public int OrderState; // ������ԁ�State�Ɠ���ł���
		public int OrdType; // ���s����
		public string RecvTime; // �󒍓���
		public string Symbol; // �����R�[�h
		public string SymbolName; // ������
		public int Exchange; // �s��R�[�h
		public string ExchangeName; // �s�ꖼ
		public int TimeInForce; // �L�����ԏ������敨�E�I�v�V���������̏ꍇ�̂�
		public double Price; // �l�i
		public double OrderQty; // �������ʁ����������؂�Ǝ����̏ꍇ�AOrderQty�̓[���ɂȂ�܂���B�������؂�Ǝ����̊m�F���@�Ƃ��ẮADetails��RecType�i3: �����؂�A7: �����j�ɂĂ��m�F���������B
		public double CumQty; // ��萔��
		public string Side; // �����敪(1:��,2:��)
		public int CashMargin; // ����敪
		public int AccountType; // �������
		public int DelivType; // ��n�敪
		public int ExpireDay; // �����L������yyyyMMdd�`��
		public int MarginTradeType; // �M�p����敪���M�p�𒍕������ۂɕ\������܂��B
		public double MarginPremium; // �v���~�A�������i���������ʁ{���ϐ��ʁj�~�P��������v���~�A�����Ƃ��Čv�Z����܂��B
									 //���M�p�𒍕������ۂɕ\������܂��B�����x�M�p��/���A��ʁi�����j���A��ʁi�f�C�g���j���̏ꍇ�́ANone�ƕԂ���܂��B
									 //��ʁi�����j���A��ʁi�f�C�g���j���̏ꍇ�́A�v���~�A����= 0�̏ꍇ�A0�i�[���j�ƕԂ���܂��B
		public object[] Details; // �����ڍ�

	}
	// �c���Ɖ�
	public class ResponsePositions
	{
		public string ExecutionID; // ���ԍ�����������ł́Anull���Ԃ�܂��B
		public int AccountType; // �������
		public string Symbol; // �����R�[�h
		public string SymbolName; // ������
		public int Exchange; // �s��R�[�h
		public string ExchangeName; // �s�ꖼ
		public int SecurityType; // ������ʁ��敨�E�I�v�V���������̏ꍇ�̂�
		public int ExecutionDay; // �����i���ʓ��j���M�p�E�敨�E�I�v�V�����̏ꍇ�̂݁���������ł́Anull���Ԃ�܂��B
		public double Price; // �l�i
		public double LeavesQty; // �c���ʁi�ۗL���ʁj
		public double HoldQty; // �S�����ʁi�ԍς̂��߂ɍS������Ă��鐔�ʁj
		public string Side; // �����敪
		public double Expenses; // ���o��M�p�E�敨�E�I�v�V�����̏ꍇ�̂�
		public double Commission; // �萔�����M�p�E�敨�E�I�v�V�����̏ꍇ�̂�
		public double CommissionTax; // �萔������Ł��M�p�E�敨�E�I�v�V�����̏ꍇ�̂�
		public int ExpireDay; // �ԍϊ������M�p�E�敨�E�I�v�V�����̏ꍇ�̂�
		public int MarginTradeType; // �M�p����敪���M�p�̏ꍇ�̂�
		public double CurrentPrice; // ���ݒl �ǉ����o�̓t���O�Ffalse�̏ꍇ�Anull
		public double Valuation; // �]�����z �ǉ����o�̓t���O�Ffalse�̏ꍇ�Anull
		public double ProfitLoss; // �]�����v�z �ǉ����o�̓t���O�Ffalse�̏ꍇ�Anull
		public double ProfitLossRate; // �]�����v�� �ǉ����o�̓t���O�Ffalse�̏ꍇ�Anull �����炭�}�C�i�X�̃p�[�Z���g�ŕ\���Ă�Ǝv����
	}
	// �o�^����
	public class ResponseRegister
	{
		public RegistInfo[] RegistList; // ���ݓo�^����Ă�������̃��X�g
	}
	public class RegistInfo
	{
		public string Symbol; // �����R�[�h
		public int Exchange; // �s��R�[�h
	}
	// �ڍ׃����L���O
	public class ResponseRanking {
		public int Type; // ���
		public string ExchangeDivision; // �s��
		public RankingInfo[] Ranking; // �����L���O
	}
	public class RankingInfo{
		public int No; // ���� �������L���O���œ������ʂ��ԋp�����ꍇ������܂��i10�ʂ�2���Ȃǁj
		public string Trend; // �g�����h
		/*
			��`�l ���e
			0	�Ώۃf�[�^����
			1	�ߋ�10�c�Ɠ����20�ʈȏ�㏸
			2	�ߋ�10�c�Ɠ����1�`19�ʏ㏸
			3	�ߋ�10�c�Ɠ��ƕς�炸
			4	�ߋ�10�c�Ɠ����1�`19�ʉ���
			5	�ߋ�10�c�Ɠ����20�ʈȏ㉺��
		*/
		public double AverageRanking; // ���Ϗ��� ��100�ʈȉ��́u999�v�ƂȂ�܂�
		public string Symbol; // �����R�[�h
		public string SymbolName; // ��������
		public double CurrentPrice; // ���ݒl
		public double ChangeRatio; // �O����
		public double ChangePercentage; // �������i%�j
		public string CurrentPriceTime; // ���� HH:mm �����t�͕Ԃ��܂���
		public double TradingVolume; // ������ ��������犔�P�ʂŕ\������ ���S���̈ʂ��l�̌ܓ�
		public double Turnover; // ������� ���������S���~�P�ʂŕ\������ ���\���~�̈ʂ��l�̌ܓ�
		public string ExchangeName; // �s�ꖼ
		public string CategoryName; // �Ǝ햼
	}

	public class ClosePosition
	{
		public ClosePosition(string HoldID = "", int Qty = 0)
		{
			this.HoldID = HoldID;
			this.Qty = Qty;
		}
		public string HoldID; // �ԍό���ID
		public int Qty; // �ԍό��ʐ���
	}


}
