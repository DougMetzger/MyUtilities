//using CompIdxOverUnderDriver;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using WealthLab.Backtest;
using WealthLab.Core;
using WealthLab.Indicators;
using WealthLab.MyIndicators;

namespace CompIdxOverUnderDriver;

public class CompIdxOUDriver : UserStrategyBase
{
    public CompIdxOUDriver()
    {

        AddParameter("RsiShort", ParameterType.Int32, 5, 3, 12, 1);         //0
        AddParameter("RsiLong", ParameterType.Int32, 15, 14, 21, 1);        //1
        AddParameter("MomRsi", ParameterType.Int32, 17, 6, 21, 1);          //2
        AddParameter("OverSold", ParameterType.Double, 30, 10, 30, 2);      //3
        AddParameter("OverBought", ParameterType.Double, 83, 50, 85, 2);    //4
        AddParameter("StopLoss", ParameterType.Int32, 6, 3, 12, 1);         //5
        AddParameter("ProfitTarget", ParameterType.Int32, 20, 6, 50, 2);    //6 
        AddParameter("CompIdxSMA_Short", ParameterType.Int32, 13, 7, 21, 1); //7
        AddParameter("CompIdxSMA_Long", ParameterType.Int32, 39, 24, 50, 1); //8
        AddParameter("VolumeLong", ParameterType.Int32, 14, 4, 21, 1);       //9 
        AddParameter("VolumeShort", ParameterType.Int32, 1, 1, 3, 1);        //10
        AddParameter("TreasholdBuyPct", ParameterType.Int32, 50, 4, 80, 2);  //11
        AddParameter("Reversal", ParameterType.Double, 8, 1, 40, 1);         //12
        AddParameter("Enable Debug", ParameterType.Double, 1, 1, 2, 1);      //13  //Boolean not allowed in AddParameter.  1 = True and 2 = False;  

    }

    private CompIdxParameters compIdxParams;
    private IndicatorManager indicatorManager;
//       private DetrendIndicatorManager detrendIndicatorManager; 
    private TradeEntryManager tradeEntryManager;
    private readonly ReviewCurrentPosition reviewCurrPos;
    private TradeExitManager exitManager;
    private IDetrendIndicatorProvider detrendIndicatorManager;


    private TradeEntryManager entryMgr;
    private TradeExitManager exitMgr;

    private bool[] volSpikeFlags;

    private int numRsiShort;
    private int numRsiLong;
    private int numMomRsi;

    private int compIdxShort;
    private int compIdxLong;

    private double overSoldLevel;
    private double overBoughtLevel;

    private double sellAtStopLossPct;
    private double sellAtProfitPct;

    private int numBarsVolShort;
    private int numBarsVolLong;
    private int thresholdPct;           // Exceed a percent threashold of volume spike to trigger purchase

    private readonly SMA smaVolShort;
    private readonly SMA smaVolLong;

    private double reversal;            // Used for negative divergence processing 

    private SMA sma40;                  // Weekly
    private SMA sma50;                  //Daily
    private SMA sma100;                 //Daily	
    private SMA sma200;                 //Daily

    private readonly int avoidTradingViolation = 1;
    int exitBar = 0;

    private bool noTradeViolation = true;

    private bool enableDebugLogging = true;
    private double enableDebugging = 1;   // True = 1 and False = 2
    private bool processExternalPreferredValues = false;                                      // 

    private string dateStr;

    private double highAmt;             // for multi-symbol processing, BarsSinceLastExit doesn't work. Stop Loss managed in program.
    private readonly int currentOpenPosIdx;
    private readonly double sellAtProfit;
    private readonly double sellAtStopLoss;

    private readonly bool issueStopLoss = false;
    private readonly bool takeProfit = false;

    PeakTroughCalculator _ptc;
    private PeakTrough pt = null;
    private PeakTrough pt2 = null;

    private bool bearishDivergence = false;
    private bool bullishDivergence = false;
    string divergenceIdentifier = string.Empty;

    string symbol = string.Empty;

    private VolumeSpikeAnalyzer analyzer; // in your class

    internal static Dictionary<string, Dictionary<string, double>> paramTable;

    private int detrendPeriod = 7;

    public override void BacktestBegin()
    {
        enableDebugLogging = true; // Set to false if you want to disable debug logging.
        processExternalPreferredValues = false; // Set to false if you want to use parameters from Strategy GUI only.

        if (enableDebugLogging)
        {
            WLLogger.LogAction = msg => WriteToDebugLog(msg);
            WLLogger.Write("Logger initialized in BacktestBegin");
        }
        //        var loader = new PreferredValueLoader("Weekly Consolidated NSDQ 2024 12 31 Particle Swarm", "2025 06 04 Weeky Detrend", enableDebugLogging);
        if (processExternalPreferredValues == true)
        {
            var loader = new PreferredValueLoader("vs 2022 v4", "2025 My Strategy Development", enableDebugLogging);
        }
    }

    public override void Initialize(BarHistory bars)
    {

        if (processExternalPreferredValues == true)
        {
            LoadParametersfromFile(bars.Symbol); // Load parameters from external file
            if (enableDebugLogging)
            {
                WLLogger.Write("Load Preferred Values from External File");
            }
        }
        else
        {
            compIdxParams = CompIdxParameters.FromStrategyParameters(Parameters); // Load parameters from Strategy GUI
            LoadParameters(bars); // Load parameters from Strategy GUI
            if (enableDebugLogging == true)
            {
                WLLogger.Write("Return From: Load Preferred Values from WealthLab Strategy Screen");
                WLLogger.Write($"Symbol: + {bars.Symbol} + numRSIShort: {numRsiShort}");
                WLLogger.Write($"Symbol: + {bars.Symbol} + reversal {reversal}");
            }
        }

        // Load CompIdx, SMAs, and Volume bar arrays
        indicatorManager = new IndicatorManager(bars,
                                                numRsiShort,
                                                numRsiLong,
                                                numMomRsi,
                                                compIdxShort,
                                                compIdxLong,
                                                numBarsVolShort,
                                                numBarsVolLong);

        // Plot Custom Indicator and Volume 
        indicatorManager.PlotCustIndVolume(this, overSoldLevel, overBoughtLevel);

        // Determine if Spike in Volume  
        analyzer = new VolumeSpikeAnalyzer(indicatorManager.SmaVolShort,
                                            indicatorManager.SmaVolLong,
                                            numBarsVolShort,
                                            numBarsVolLong,
                                            thresholdPct,
                                            enableDebugLogging);

        // Plot Volume Spikes
        volSpikeFlags = analyzer.Analyze(bars);

        // Plot Price SMAs.
        PlotPriceSma(bars);

        //           detrendIndicatorManager = new DetrendIndicatorManager(bars, detrendPeriod);
        //        IDetrendIndicatorProvider detrendProvider = new DetrendIndicatorManager(bars, detrendPeriod);
        detrendIndicatorManager = new DetrendIndicatorManager(bars, detrendPeriod);
            
        detrendIndicatorManager.PlotDetrendIndicator(this);


        StartIndex = compIdxLong;
    }


    public override void Execute(BarHistory bars, int idx)
    {

        if (enableDebugLogging == true)
        {
            WLLogger.Write($"Execute");
        }
        if (enableDebugLogging == true)
        {
            WLLogger.Write($"Symbol: + {bars.Symbol} + numRSIShort: {numRsiShort}");
            WLLogger.Write($"Symbol: + {bars.Symbol} + reversal {reversal}");
        }



        divergenceIdentifier = DivergenceHelper.DivergenceIdentifier(this,
                                                                    bars,
                                                                    idx,
                                                                    indicatorManager.CustInd,
                                                                    reversal,
                                                                    indicatorManager.CustInd.PaneTag,
                                                                    enableDebugLogging);

        if (divergenceIdentifier == "E")
        {
            bearishDivergence = true;   // E = Bearish Divergence
            bullishDivergence = false;  // Reset bullish divergence 
        }
        else if (divergenceIdentifier == "U")
        {
            bullishDivergence = true;   // U = Bullish Divergence
            bearishDivergence = false;  // Reset bearish divergence  
        }
        else
        {
            bearishDivergence = false;
            bullishDivergence = false;
        }

        // Draw an upward arrow in Volume Pane if volume spike occures. 

        //       var drawVolumeSpikeMarker = new VolumeSpikeAnalyzer.DrawVolumeSpikeMarker(this, idx);


        analyzer.DrawVolumeSpike(this, idx);

        // Determine what trades already exist for symbol at three levels: crossover Oversold, crossover Midpoint, crossover OverBought.  Logic in "TradeEntryManager" allows multiple trades, but only one trade per level. 
        var review = new ReviewCurrentPosition(enableDebugLogging);
        review.Analyze(HasOpenPosition); // Determine if there is an open position for the symbol at the following levels: Oversold, Midpoint, and OverBought.  If so, set flags to true.

        // Determine if Trade Violation exists: For example, if no open position exist, then check the last closed position to see if it was closed within the last "avoidTradingViolation" bars.      
        //          dateStr = bars.DateTimes[idx].ToString("MM-dd-yyyy");
        noTradeViolation = true;
        var violationCheck = new TradeViolationUtil(this,
                                                    bars,
                                                    idx,
                                                    avoidTradingViolation,
                                                    enableDebugLogging);
        if (!violationCheck.NoTradeViolation)
        {
            noTradeViolation = false;
        }

        // Buy stocks that meet criteria
        if (noTradeViolation)
        {
            this.tradeEntryManager = new TradeEntryManager(
                                                        bars,
                                                        indicatorManager.CustInd,
                                                        volSpikeFlags,
                                                        overSoldLevel,
                                                        overBoughtLevel,
                                                        review.OverSoldPositionExists,
                                                        review.OverBoughtPositionExists,
                                                        review.MidpointPositionExists,
                                                        enableDebugLogging,
                                                        (bh, tt, ot, qty, limit, desc) => PlaceTrade(bh, tt, ot, qty, (int)limit, desc),
                                                        this,
                                                        detrendIndicatorManager);

            // this.tradeEntryManager.Evaluate(idx);
        
        } 
        
        if (LastOpenPosition != null)           
        {
             this.exitManager = new TradeExitManager(
               //                                    this,
                                                   bars,
                                                   enableDebugLogging,
                                                   bearishDivergence,
                                                   indicatorManager.CustInd,
                                                   overSoldLevel,
                                                   overBoughtLevel,
                                                   sellAtStopLossPct,
                                                   sellAtProfitPct,
                                                   () => OpenPositions,
                                                   (pos, type, qty, reason) => ClosePosition(pos, type, qty, reason));

            //  this.exitManager.CloseTrades(idx);
        }

//           ExecuteSessionOpen(bars, idx, bars.Open[idx]);
    }

    public override void ExecuteSessionOpen(BarHistory bars, int idx, double sessionOpenPrice)
    {
        if (noTradeViolation)
        {
            this.tradeEntryManager.Evaluate(idx);
        }

        if (LastOpenPosition != null)
        {
            this.exitManager.CloseTrades(idx);
        }

        
    }
      

    public interface IDetrendIndicatorProvider
    {
        bool IsDetrendNegative(int idx);
        void PlotDetrendIndicator(UserStrategyBase strategy);
                   
    }

    private bool HasOpenPosition(string entrySignalName)
    {
        if (LastOpenPosition == null)
        {
            return false;
        }

        return OpenPositions.Any(p => p.EntrySignalName == entrySignalName);
    }   

    private void LoadParameters(BarHistory bars)
    {
        if (enableDebugLogging == true)
        {
           WLLogger.Write($"LoadParameters from WealthLab Strategy Screen");
        }

        numRsiShort = compIdxParams.RsiShort;           
        numRsiLong = compIdxParams.RsiLong;
        numMomRsi = compIdxParams.MomRsi;;

        overSoldLevel = compIdxParams.OverSold;
        overBoughtLevel = compIdxParams.OverBought;

        sellAtStopLossPct = compIdxParams.StopLoss;
        sellAtProfitPct = compIdxParams.ProfitTarget;

        compIdxShort = compIdxParams.CompIdxSmaShort;
        compIdxLong = compIdxParams.CompIdxSmaLong;

        numBarsVolLong = compIdxParams.VolumeLong;
        numBarsVolShort = compIdxParams.VolumeShort;
        thresholdPct = compIdxParams.TreasholdBuyPct;

        reversal = compIdxParams.Reversal;

        if (enableDebugLogging == true)
        {
            WLLogger.Write($"Symbol: + {bars.Symbol} + numRSIShort: {numRsiShort}");
            WLLogger.Write($"Symbol: + {bars.Symbol} + reversal {reversal}");
        }







        /*           enableDebugging = compIdxParams.EnableDebug;                    

                   if (enableDebugging == 1)
                   {
                       enableDebugLogging = true;
                   }
                   else
                   {
                       enableDebugLogging = false;
                   }*/

    }

    private void LoadParametersfromFile(string symbol)
    {
        if (enableDebugLogging == true)
        {
            WLLogger.Write($"LoadParameters From External File");
        }

        // RSI and Momentum
        numRsiShort = (int)PreferredValueLoader.Get(symbol, "RsiShort", 12);
        numRsiLong =  (int)PreferredValueLoader.Get(symbol, "RsiLong", 14);
        numMomRsi = (int)PreferredValueLoader.Get(symbol, "MomRsi", 19);

        // Overbought/Oversold levels
        overSoldLevel = PreferredValueLoader.Get(symbol, "OverSold", 30);
        overBoughtLevel = PreferredValueLoader.Get(symbol, "OverBought", 85);

        // Exit logic
        sellAtStopLossPct = PreferredValueLoader.Get(symbol, "StopLoss", 10);
        sellAtProfitPct = PreferredValueLoader.Get(symbol, "ProfitTarget", 50);

        // Comparison Index SMA parameters
        compIdxShort = (int)PreferredValueLoader.Get(symbol, "CompIdxSMA_Short", 13);
        compIdxLong = (int)PreferredValueLoader.Get(symbol, "CompIdxSMA_Long", 39);

        // Volume thresholds
        numBarsVolLong = (int)PreferredValueLoader.Get(symbol, "VolumeLong", 4);
        numBarsVolShort = (int)PreferredValueLoader.Get(symbol, "VolumeShort", 1);

        // Buy threshold and reversal detection
        thresholdPct = (int)PreferredValueLoader.Get(symbol, "TreasholdBuyPct", 4);
        reversal = PreferredValueLoader.Get(symbol, "Reversal", 40);

        // Debug flag
        enableDebugging = PreferredValueLoader.Get(symbol, "Enable Debug", 0);
//           enableDebugLogging = (enableDebugging == 1);
/*           if (enableDebugging == 1)
        {
            enableDebugLogging = true;
        }
        else
        {
            enableDebugLogging = false;
        }*/
    }


    private void PlotPriceSma(BarHistory bars)
    {
        if (enableDebugLogging == true)
        {
           WLLogger.Write($"PlotPriceSma");
        }

        if (bars.Scale == HistoryScale.Weekly)
        {
            sma40 = new SMA(bars.Close, 40);
            PlotIndicator(sma40, WLColor.Blue, PlotStyle.Line, false, "Price");
        }
        else
        {
            sma50 = new SMA(bars.Close, 50);
            PlotIndicator(sma50, WLColor.Blue, PlotStyle.Line, false, "Price");
            sma100 = new SMA(bars.Close, 100);
            PlotIndicator(sma100, WLColor.Red, PlotStyle.Line, false, "Price");
            sma200 = new SMA(bars.Close, 200);
            PlotIndicator(sma200, WLColor.Black, PlotStyle.Line, false, "Price");
        }
    }


}
