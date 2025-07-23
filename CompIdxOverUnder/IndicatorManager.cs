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

        public IndicatorManager(BarHistory bars, CompIdxParameters parameters)
        {
            CustInd = new Customci(bars, 1, parameters.RsiShort, parameters.RsiLong, parameters.MomRsi, parameters.CompIdxSmaShort, parameters.CompIdxSmaLong);
            CompIdxSmaShort = new SMA(CustInd, parameters.CompIdxSmaShort);
            CompIdxSmaLong = new SMA(CustInd, parameters.CompIdxSmaLong);
            // ... init other indicators
            SmaVolShort = new SMA(bars.Volume, parameters.VolumeShort);
            SmaVolLong = new SMA(bars.Volume, parameters.VolumeLong);
        }
    }
}