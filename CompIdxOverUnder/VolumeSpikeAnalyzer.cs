using CompIdxOverUnderDriver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WealthLab.Core;
using WealthLab.Indicators;
using WealthLab.Backtest;   
using WealthLab.MyIndicators;

namespace CompIdxOverUnderDriver
{

    public class VolumeSpikeAnalyzer
    {
        private readonly SMA smaVolShort;
        private readonly SMA smaVolLong;
        private readonly int numBarsVolShort;
        private readonly int numBarsVolLong;
        private readonly double thresholdPct;
        private readonly bool enableDebugLogging;

        #pragma warning disable IDE0290
        public VolumeSpikeAnalyzer(
            SMA smaVolShort,
            SMA smaVolLong,
            int numBarsVolShort,
            int numBarsVolLong,
            double thresholdPct,
            bool enableDebugLogging = false)
        {
            this.smaVolShort = smaVolShort;
            this.smaVolLong = smaVolLong;
            this.numBarsVolShort = numBarsVolShort;
            this.numBarsVolLong = numBarsVolLong;
            this.thresholdPct = thresholdPct;
            this.enableDebugLogging = enableDebugLogging;
        }

        public bool[] Analyze(BarHistory bars)
        {
           if (enableDebugLogging)
            {
                WLLogger.Write("VolumSpikeAnalyzer");  // Or your custom logger
            }

            bool[] volSpikeFlags = new bool[bars.Count];
            int startBar = Math.Max(numBarsVolShort, numBarsVolLong);

            for (int bar = startBar; bar < bars.Count; bar++)
            {
                if (smaVolShort[bar] > smaVolLong[bar])
                {
                    double pctDiff = Math.Abs(smaVolShort[bar] - smaVolLong[bar]) / smaVolLong[bar];
                    volSpikeFlags[bar] = pctDiff > (thresholdPct / 100.0);

                    // Optional detailed logging
                    /*
                    WLLogger.Write($"Symbol: {bars.Symbol} Bar: {bar} SMA Short: {smaVolShort[bar]:F3}, " +
                                   $"SMA Long: {smaVolLong[bar]:F3}, Diff%: {pctDiff:F3}, Flag: {volSpikeFlags[bar]}");
                    */
                }
            }

            return volSpikeFlags;
        }
    }
}
