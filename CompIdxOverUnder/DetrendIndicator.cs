using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using WealthLab.Backtest;
using WealthLab.Core;
using WealthLab.Indicators;
using WealthLab.MyIndicators;
using static CompIdxOverUnderDriver.CompIdxOUDriver;

namespace CompIdxOverUnderDriver
{
    public class DetrendIndicatorManager : IDetrendIndicatorProvider
    {
        public SMA smaDetrendShort { get; private set; }
        public SMA smaDetrendLong { get; private set; }
        public TimeSeries smaDetrendSpread { get; private set; }

        private readonly BarHistory bars;
        private readonly int period;    


        public int numDetrendShort = 1;
        public int numDetrendSpread = 1;

        public DetrendIndicatorManager(BarHistory bars, int detrendPeriod)
        {
            this.bars = bars;
            this.period = detrendPeriod;

            InitializeDetrendIndicators();
        }

        private void InitializeDetrendIndicators()
        {
              
                smaDetrendShort = new SMA(bars.Close, numDetrendShort);
                smaDetrendLong = new SMA(bars.Close, period);
                smaDetrendSpread = smaDetrendShort - smaDetrendLong;
        }

        public bool IsDetrendNegative(int idx)
        {
            return smaDetrendSpread[idx] < 0; 
        }   

        public void PlotDetrendIndicator(UserStrategyBase strategy)
        {
            strategy.PlotTimeSeries(smaDetrendSpread,"Detrend Indicator", "Detrend Indicator", WLColor.Blue, PlotStyle.Line, false);
            strategy.DrawHorzLine(0, WLColor.Black, 1, LineStyle.Solid, "Detrend Indicator");

            strategy.DrawHeaderText("Detrend Indicator", WLColor.Coral, 17, "Detrend Indicator", true);
        }
    }
}
