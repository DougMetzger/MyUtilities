using System;
using System.IO;

namespace StraategyHarness
{
    internal class ConsoleApp
    {
        static void Main(string[] args)
        {
            var datasets = File.ReadAllLines("datasets.txt");
            foreach (var name in datasets)
            {
                var bars = HistoryLoader.GetHistory("TechSector", BarScale.Daily);
                var strat = new CompIdxOU();
                var bt = new Backtester(strat, bars);
                BacktestResult result = bt.Run();
                Console.WriteLine($"Final PV for TechSector: {result.PortfolioValue}");


                strat.Initialize(bars);
                strat.Execute(bars);
                PVLogger.Save(name, strat); // Optional: custom PV save routine
            }

        }
        public static class PVLogger
        {
            public static void Save(string datasetName, double pv)
            {
                File.AppendAllText("pv_log.txt", $"{datasetName}: {pv}\n");
            }
        }
    }
}

