using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WealthLab.Backtest;
using WealthLab.Core;
using WealthLab.Indicators;
using WealthLab.MyIndicators;

namespace CompIdxOverUnderDriver
{
    public class IndicatorManager
    {
        public Customci CustInd { get; private set; }
        public SMA CompIdxSmaShort { get; private set; }
        public SMA CompIdxSmaLong { get; private set; }

        public SMA SmaVolShort { get; private set; }
        public SMA SmaVolLong { get; private set; }

        public IndicatorManager(BarHistory bars, int numRsiShort, int numRsiLong, int numMomRsi, int CompIdxShort, int CompIdxLong, int numBarsVolShort, int numBarsVolLong)
        {
            CustInd = new Customci(bars, 1, numRsiShort, numRsiLong, numMomRsi, CompIdxShort, CompIdxLong);
            CompIdxSmaShort = new SMA(CustInd, CompIdxShort);
            CompIdxSmaLong = new SMA(CustInd, CompIdxLong);
            // ... init other indicators
            SmaVolShort = new SMA(bars.Volume, numBarsVolShort);
            SmaVolLong = new SMA(bars.Volume, numBarsVolLong);
        }



        public void PlotCustIndVolume(UserStrategyBase strategy, double overSoldLevel, double overBoughtLevel)
        {
            // Composite Indicator visuals
            strategy.PlotIndicator(CustInd, new WLColor(0, 0, 0));
            CustInd.MassageColors = true;

            strategy.PlotIndicator(CompIdxSmaShort, WLColor.Blue, PlotStyle.Line, false, "Custom Composite Indicator");
            strategy.PlotIndicator(CompIdxSmaLong, WLColor.Red, PlotStyle.Line, false, "Custom Composite Indicator");
            strategy.DrawHeaderText("Custom Composite Indicator", WLColor.Coral, 17, "Custom Composite Indicator", true);

            // Volume Spike visuals
            strategy.PlotIndicator(SmaVolShort, WLColor.Blue, PlotStyle.Line, false, "Volume Spikes");
            strategy.PlotIndicator(SmaVolLong, WLColor.Red, PlotStyle.Line, false, "Volume Spikes");

            // Threshold visuals
            strategy.DrawHorzLine(overSoldLevel, WLColor.Black, 1, LineStyle.Solid, "Custom Composite Indicator");
            strategy.DrawHorzLine(overBoughtLevel, WLColor.Black, 1, LineStyle.Solid, "Custom Composite Indicator");

            strategy.DrawHeaderText("VolumeSpikes", WLColor.Coral, 17, "Volume Spikes", true);
        }
    }
}