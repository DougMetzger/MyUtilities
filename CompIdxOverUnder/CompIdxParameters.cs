using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WealthLab.Core;

namespace CompIdxOverUnderDriver
{
    public class CompIdxParameters
    {
        public int RsiShort { get; set; }
        public int RsiLong { get; set; }
        public int MomRsi { get; set; }
        public double OverSold { get; set; }
        public double OverBought { get; set; }
        public int StopLoss { get; set; }
        public int ProfitTarget { get; set; }
        public int CompIdxSmaShort { get; set; }
        public int CompIdxSmaLong { get; set; }
        public int VolumeLong { get; set; }
        public int VolumeShort { get; set; }
        public int TreasholdBuyPct { get; set; }
        public double Reversal { get; set; }
        public double EnableDebug { get; set; }

        public static CompIdxParameters FromStrategyParameters(ParameterList parameters)
        {
            return new CompIdxParameters
            {
                RsiShort = parameters[0].AsInt,
                RsiLong = parameters[1].AsInt,
                MomRsi = parameters[2].AsInt,
                OverSold = parameters[3].AsDouble,
                OverBought = parameters[4].AsDouble,
                StopLoss = parameters[5].AsInt,
                ProfitTarget = parameters[6].AsInt,
                CompIdxSmaShort = parameters[7].AsInt,
                CompIdxSmaLong = parameters[8].AsInt,
                VolumeLong = parameters[9].AsInt,
                VolumeShort = parameters[10].AsInt,
                TreasholdBuyPct = parameters[11].AsInt,
                Reversal = parameters[12].AsDouble,
                EnableDebug = parameters[13].AsDouble,           };        
        }
    }
}
