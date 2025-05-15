using System;

namespace CSharp_sample
{
	class MainExec
	{
		static void Main(string[] args)
		{
			string execType = args[0];
			try {
				if (execType == "1") EveryDayExec.ExecBasic();
				if (execType == "2") MinitesExec.ExecBasic();
				if (execType == "3") Tools.SetOldData();
				if (execType == "4") Tools.SetJapanOldData();
				if (execType == "5") Tools.DataChecker();
				if (execType == "6") Tools.IsBuyList();
				if (execType == "7") Tools.CodeDataChecker();
				if (execType == "8") RequestBasic.TestRequest();
				if (execType == "9") RequestBasic.RequestWallet();
				if (execType == "10") Condtions.SaveBuyInfo();
				if (execType == "11") Condtions.BenefitSum();
				if (execType == "12") Condtions.SaveJapanCond();
				//if (execType == "13") Condtions.SaveAllCond();
				if (execType == "14") Tools.AddNewPro500();
				if (execType == "15") Tools.GetAllResponseSymbol();
				if (execType == "16") Tools.CheckDateBenefitLoss();
				if (execType == "17") Tools.TestExec();
				if (execType == "18") Tools.BuyCheck();
				if (execType == "19") EveryDayExec.SetDayMemo(DateTime.Today);
				if (execType == "20") Tools.CheckJapanInfo();
				if (execType == "21") Tools.LowPriceCheck();
				if (execType == "22") RequestBasic.RequestRanking();
				if (execType == "23") Tools.OldCheck();
				if (execType == "24") Condtions.SaveCond51All();
				if (execType == "25") Condtions.SaveBenefitAll();
				if (execType == "26") Condtions.CheckCond51All();
				if (execType == "27") Condtions.DebugCheckCond51Score();
			} catch (Exception e) {
				CsvControll.ErrorLog("Exception", execType, e.Message, "");
			}
		}
	}
}
