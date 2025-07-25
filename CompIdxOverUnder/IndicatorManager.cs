using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    }
}