using System;

namespace CSharp_sample
{
	class MainExec
	{
		static void Main(string[] args)
		{
			DateTime now = DateTime.Now;
			string execType = args[0];
			string param1 = args.Length >= 2 ? args[1] : "";
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
				//if (execType == "12") Condtions.SaveJapanCond();
				//if (execType == "13") Condtions.SaveAllCond();
				if (execType == "14") Tools.AddNewPro500();
				if (execType == "15") Tools.GetAllResponseSymbol();
				if (execType == "16") Tools.CheckDateBenefitLoss();
				if (execType == "17") Tools.TestExec(param1);
				if (execType == "18") Tools.BuyCheck();
				if (execType == "19") EveryDayExec.SetDayMemo(DateTime.Today);
				if (execType == "20") Tools.CheckJapanInfo();
				if (execType == "21") Tools.LowPriceCheck();
				if (execType == "22") RequestBasic.RequestRanking();
				if (execType == "23") Tools.OldCheck();
				if (execType == "24") Condtions.SaveCond51All();
				if (execType == "25") Condtions.SaveBenefitAll();
				if (execType == "26") Condtions.Check51Rmv(param1);
				if (execType == "27") Condtions.Check51One(param1);
				if (execType == "28") Tools.CheckRanking();
				if (execType == "29") Tools.CheckRankingBenfitAll();
				//if (execType == "30") Condtions.Aaa();
				if (execType == "31") Tools.SaveJapanScoreMulti(DateTime.Parse("2025/05/29"), DateTime.Parse("2025/06/03"));
				if (execType == "32") Tools.CheckLossSell();
				if (execType == "33") Condtions.Check51Alter(param1);
				if (execType == "34") CsvControll.FileTest();

			} catch (Exception e) {
				Console.WriteLine(e);
				CsvControll.ErrorLog("Exception", execType, e.Message, "");
			} finally{
				CsvControll.FlushSymbolLog();
				Common.DebugInfo("End", (DateTime.Now - now));
			}
		}
	}
}
