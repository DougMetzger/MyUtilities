using System;
using System.Reflection;
using WealthLab.Backtest;
using WealthLab.Core;
using WealthLab.Indicators;
using WealthLab.MyIndicators;
using System.Linq;
using System.Reflection.Metadata.Ecma335;


namespace CompIdxOverUnderDriver
{
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
        private TradeEntryManager tradeEntryManager;
        private readonly ReviewCurrentPosition reviewCurrPos;
        private TradeExitManager exitManager;
       

        private bool[] volSpikeFlags;

 //       private readonly Customci custInd;

               
 //       private CompIdxParameters _parameters;

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
        
        private bool enableDebugLogging = false;
        private double enableDebugging = 1;   // True = 1 and False = 2     

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
        private bool bullishDivergence   = false;
        string divergenceIdentifier = string.Empty;
               

        //       private StrategyParamaters config;

        public override void Initialize(BarHistory bars)
        {
            // Load parameters from Strategy GUI
            compIdxParams = CompIdxParameters.FromStrategyParameters(Parameters);

            // Load Strategy parameters (AddParameter) for processing.
            LoadParameters();

            WLLogger.LogAction = msg => WriteToDebugLog(msg);
            if (enableDebugLogging == true)
            {
                WLLogger.Write($"Initialize");
            }

            // Load CompIdx, SMAs, and Volume bar arrays
            indicatorManager = new IndicatorManager(bars, compIdxParams);

            PlotIndicators();                     

            // Determine if Spike in Volume  
            var analyzer = new VolumeSpikeAnalyzer(
                                                    indicatorManager.SmaVolShort,
                                                    indicatorManager.SmaVolLong,
                                                    numBarsVolShort,
                                                    numBarsVolLong,
                                                    thresholdPct,
                                                    enableDebugLogging);

            volSpikeFlags = analyzer.Analyze(bars);

            PlotPriceSma(bars);
            
            StartIndex = compIdxLong;
        }


        public override void Execute(BarHistory bars, int idx)
        {            
   
            if (enableDebugLogging == true)
            {
                WLLogger.Write($"Execute");
            }

   //         GraphDivergences(bars, idx);      

            divergenceIdentifier = DivergenceHelper.DivergenceIdentifier(this, bars, idx, indicatorManager.CustInd, reversal, indicatorManager.CustInd.PaneTag, enableDebugLogging);

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


            // Draw an upward arrow for volume spikes.            
            if (idx >= Math.Max(numBarsVolShort, numBarsVolLong))
            {
                if (volSpikeFlags[idx] == true)
                {
                    DrawText("↑", idx, indicatorManager.SmaVolShort[idx], WLColor.Blue, 30, "Volume Spikes");
                }
            }            

            // Determine what trades already exist for symbol 
            var review = new ReviewCurrentPosition(enableDebugLogging);
            review.Analyze(); // If degugging enabled, this will write to the debug log, "Start of Analysis"
            review.OverSoldPositionExists   = HasOpenPosition("Buy @ OverSold");   
            review.OverBoughtPositionExists = HasOpenPosition("Buy @ OverBought");        
            review.MidpointPositionExists   = HasOpenPosition("Buy @ MidPoint");
            

            // Determine if Trade Violation
            dateStr = bars.DateTimes[idx].ToString("MM-dd-yyyy");
            noTradeViolation = true;           
   //         DetermineIfTradeViolation(bars, idx);

            var violationCheck = new TradeViolationUtil(this, bars, idx, avoidTradingViolation, enableDebugLogging);

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
                                                            (bh, tt, ot, qty, limit, desc) => PlaceTrade(bh, tt, ot, qty, (int)limit, desc));

                this.tradeEntryManager.Evaluate(idx);
            }

            // If there is no open trades, exit.
            if (LastOpenPosition != null)           
            {
  //             CheckForStopLoss(bars, idx);

  //              CheckForProfitTaking(bars, idx);

               
                
    //           WriteToDebugLog($"issueStopLoss: " + issueStopLoss);
               

                this.exitManager = new TradeExitManager(
                                                       bars,
                                                       enableDebugLogging,
                                                       bearishDivergence,
                                                       indicatorManager.CustInd,
                                                       overSoldLevel,
                                                       overBoughtLevel,
                                                       sellAtStopLossPct,
                                                       sellAtProfitPct,
                                                     //  issueStopLoss,
                                                     //  takeProfit,
                                                       () => OpenPositions,
                                                       () => LastOpenPosition,
                                                       (pos, type, qty, reason) => ClosePosition(pos, type, qty, reason)
                                              );

                this.exitManager.CloseTrades(idx);
            }
        }

        private bool HasOpenPosition(string entrySignalName)
        {
            if (LastOpenPosition == null)
            {
                return false;
            }

            return OpenPositions.Any(p => p.EntrySignalName == entrySignalName);
        }   


        private void LoadParameters()
        {
            if (enableDebugLogging == true)
            {
               WLLogger.Write($"LoadParameters");
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

            enableDebugging = compIdxParams.EnableDebug;                    

            if (enableDebugging == 1)
            {
                enableDebugLogging = true;
            }
            else
            {
                enableDebugLogging = false;
            }

            //			WriteToDebugLog($"enableDebugging: " + enableDebugging);  

        }
                
        private void PlotIndicators()
        {
            if (enableDebugLogging == true)
            {
               WLLogger.Write($"PlotIndicators");
            }

            PlotIndicator(indicatorManager.CustInd, new WLColor(0, 0, 0));
            indicatorManager.CustInd.MassageColors = true;
            PlotIndicator(indicatorManager.CompIdxSmaShort, WLColor.Blue, PlotStyle.Line, false, "Custom Composite Indicator");
            PlotIndicator(indicatorManager.CompIdxSmaLong, WLColor.Red, PlotStyle.Line, false, "Custom Composite Indicator");
            DrawHeaderText("Custom Composite Indicator", WLColor.Coral, 17, "Custom Composite Indicator", true);

            PlotIndicator(indicatorManager.SmaVolShort, WLColor.Blue, PlotStyle.Line, false, "Volume Spikes");
            PlotIndicator(indicatorManager.SmaVolLong, WLColor.Red, PlotStyle.Line, false, "Volume Spikes");
            DrawHorzLine(overSoldLevel, WLColor.Black, 1, LineStyle.Solid, "Custom Composite Indicator");
            DrawHorzLine(overBoughtLevel, WLColor.Black, 1, LineStyle.Solid, "Custom Composite Indicator");
            DrawHeaderText("VolumeSpikes", WLColor.Coral, 17, "Volume Spikes", true);
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

        /*       private void DrawVolumeSpikeMarker(int idx)
               {

                   if (enableDebugLogging == true)
                   {
                      WLLogger.Write($"DrawVolumeSpikeMarker");
                   }

                   DrawText("↑", idx, indicatorManager.SmaVolShort[idx], WLColor.Blue, 30, "Volume Spikes");
               }  */

        private void GraphDivergences(BarHistory bars, int index)
        {
            if (enableDebugLogging == true)
            {
                WriteToDebugLog($"GraphDivergences");
            }

            // Graph peaks and troughs to identify divergences
            //
            //create the PeakTroughCalculator for an Indicator or TimeSeries

            PeakTroughReversalType reversalType = PeakTroughReversalType.Point;
            _ptc = new PeakTroughCalculator(indicatorManager.CustInd, reversal, reversalType);  //_ptc: peak loss calculator  

            //start after at least 2 peaks and 2 troughs
            pt = _ptc.PeakTroughs[3];                                            //pt1: peakTrough 1 
            StartIndex = pt.DetectedAtIndex;                                     //pt2: peakTrough 2 

            //		PeakTrough pt = null;
            pt2 = null;

            if (_ptc.Divergence(index, bars.High, out pt, out pt2) == DivergenceType.Bearish)
            {
                WLColor bearClr = WLColor.Red;
                DrawBarAnnotation(TextShape.ArrowDown, index, true, WLColor.Gold, 36);
                DrawLine(pt.XIndex, pt.YValue, pt2.XIndex, pt2.YValue, bearClr, 4, default, indicatorManager.CustInd.PaneTag);
                DrawLine(pt.XIndex, bars.High[pt.XIndex], pt2.XIndex, bars.High[pt2.XIndex], bearClr, 4, default, "Price");
            }
            else if (_ptc.Divergence(index, bars.Low, out pt, out pt2) == DivergenceType.Bullish)
            {
                WLColor bullClr = WLColor.Green;
                DrawBarAnnotation(TextShape.ArrowUp, index, false, WLColor.Gold, 36);
                DrawLine(pt.XIndex, pt.YValue, pt2.XIndex, pt2.YValue, bullClr, 4, default, indicatorManager.CustInd.PaneTag);
                DrawLine(pt.XIndex, bars.Low[pt.XIndex], pt2.XIndex, bars.Low[pt2.XIndex], bullClr, 4, default, "Price");
            }

        }

   /*     private void DetermineIfTradeViolation(BarHistory bars, int index)
        {            
            if (enableDebugLogging == true)
            {
               WLLogger.Write($"DetermineIfTradeViolation");
            }
            //
            //  The following logic is necessary because WealthLab's BarsSinceLastOpen does not work for datasets (multi symbols).  
            //
            if (LastOpenPosition == null)
            {
                if (bars.Scale != HistoryScale.Weekly)
                {
                    Position lastClosed = GetPositions()
                    .Where(p => !p.IsOpen)
                    .OrderByDescending(p => p.ExitBar)
                    .FirstOrDefault();

                    if (lastClosed != null)
                    {
                        exitBar = lastClosed.ExitBar;
                       WLLogger.Write("Symbol: " + bars.Symbol + " Last closed position exited at exit bar: " + exitBar + " current bar: " + index + " dated: " + dateStr);
                        if ((index - exitBar) > avoidTradingViolation)
                        {
                            noTradeViolation = true;
                            if (enableDebugLogging == true)
                            {
                               WLLogger.Write("Last Symbol - No Trade Violation: " + noTradeViolation + " Days since last close = " + (index - exitBar));
                            }
                        }
                        else
                        {
                            noTradeViolation = false;
                            if (enableDebugLogging == true)
                            {
                               WLLogger.Write("Last Symbol - ***** Trade Violation: ***** " + noTradeViolation + " Days since last close = " + (index - exitBar));
                            }
                        }
                    }
                    else
                    {
                        if (enableDebugLogging == true)
                        {
                           WLLogger.Write("Symbol: " + bars.Symbol + " No closed positions found for this symbol " + " on bar " + index + " dated: " + dateStr);
                        }
                    }
                }

                if (enableDebugLogging == true)
                {
                   WLLogger.Write("Symbol: " + bars.Symbol + " Weekly Processing: No Trade Violation: " + noTradeViolation + " on bar " + index + " dated: " + dateStr);
                }
            }
            else
            {
                if (enableDebugLogging == true)
                {
                   WLLogger.Write("Symbol: " + bars.Symbol + " has open position(s) " + " on bar " + index + " dated: " + dateStr);
                }

            }

        }*/

 /*       private void CheckForStopLoss(BarHistory bars, int index)
        {
            //
            //  If Stop Loss is recongized on any open position, close all open position and exit.
            //	

            issueStopLoss = false;

            foreach (Position openPos in OpenPositions)
            {
                highAmt = openPos.EntryPrice;
                currentOpenPosIdx = openPos.EntryBar;

                while (currentOpenPosIdx <= (index))
                {
                    if (bars.Close[currentOpenPosIdx] > highAmt)
                    {
                        highAmt = bars.Close[currentOpenPosIdx];
                    }

                    currentOpenPosIdx++;
                }

                sellAtStopLoss = (highAmt * (1 - (sellAtStopLossPct / 100)));

                if (bars.Close[index] <= sellAtStopLoss)
                {
                    issueStopLoss = true;

                    if (enableDebugLogging == true)
                    {
                        WLLogger.Write("Closed because of Stop Loss");
                    }

                    break;
                }
            }
            
        }*/

 /*    private void CheckForProfitTaking(BarHistory bars, int index)
        {
            //
            //  If any position reaches its profit goal, close all open position and exit.
            //	
            sellAtProfit = (sellAtProfitPct / 100.0) + 1.0;

            takeProfit = false;
           
            foreach (Position openPos in OpenPositions)
            {
                if (bars.Close[index] >= (openPos.EntryPrice * sellAtProfit))
                {
                    takeProfit = true;

                    if (enableDebugLogging == true)
                    {
                        WLLogger.Write("Closed because Profit Target Reached: " + (index + 1));
                    }

                    return;
                }
            }

        }*/


/*        private void CloseTrades(BarHistory bars, int index)
        {
            if  (enableDebugLogging == true)
            {
               WLLogger.Write($"CloseTrades");
            }
            //  If there is no open trades, exit.
            if  (LastOpenPosition == null)
            {
                return;
            }

            //          
            //  If bearish divergence exist, close all open position and exit.
            //		

            if  (_ptc.Divergence(index, bars.High, out pt, out pt2) == DivergenceType.Bearish)
            {
                foreach (Position openPos in OpenPositions)
                {
                    ClosePosition(openPos, OrderType.Market, 0, "Sell Bearish Divergence");

                    if (enableDebugLogging == true)
                    {
                       WLLogger.Write("Closed positions because of Negative Divergence: " + (index + 1));
                    }
                }

                return;
            }
            //
            //  If CompIdx crosses below Overbought, close all open position and exit.
            //	
            if (indicatorManager.CustInd.CrossesUnder(overBoughtLevel, index))
            {
                foreach (Position openPos in OpenPositions)
                {
                    ClosePosition(openPos, OrderType.Market, 0, "Sold @ OverBought");

                    if (enableDebugLogging == true)
                    {
                       WLLogger.Write("Closed Crossed Under OverBought Level: " + (index + 1));
                    }
                }
                return;
            }
            //
            //  If CompIdx crosses below OverSold, close all open position and exit.
            //	
            if (indicatorManager.CustInd.CrossesUnder(overSoldLevel, index))
            {
                foreach (Position openPos in OpenPositions)
                {
                    ClosePosition(openPos, OrderType.Market, 0, "Sold At OverSold");

                    if (enableDebugLogging == true)
                    {
                       WLLogger.Write("Closed Crossed Under OverSold Level: " + (index + 1));
                    }
                }
                return;
            }
            //
            //  If Stop Loss is recongized on any open position, close all open position and exit.
            //	
            foreach (Position openPos in OpenPositions)
            {
                highAmt = openPos.EntryPrice;
                currentOpenPosIdx = openPos.EntryBar;

                while (currentOpenPosIdx <= (index))
                {
                    if (bars.Close[currentOpenPosIdx] > highAmt)
                    {
                        highAmt = bars.Close[currentOpenPosIdx];
                    }

                    currentOpenPosIdx++;
                }

                sellAtStopLoss = (highAmt * (1 - (sellAtStopLossPct / 100)));

                if (bars.Close[index] <= sellAtStopLoss)
                {
                    ClosePosition(openPos, OrderType.Market, 0, "Sell at " + sellAtStopLossPct + "% Trailing Loss");
                    if (enableDebugLogging == true)
                    {
                       WLLogger.Write("Closed because of Stop Loss: " + (index + 1));
                    }
                }
            }
            //
            //  If any position reaches its profit goal, close all open position and exit.
            //	
            sellAtProfit = (sellAtProfitPct / 100.0) + 1.0;
            foreach (Position openPos in OpenPositions)
            {
                if (bars.Close[index] >= (openPos.EntryPrice * sellAtProfit))
                {
                    ClosePosition(openPos, OrderType.Market, 0, "Sell at " + sellAtProfitPct + "% profit target");
                    if (enableDebugLogging == true)
                    {
                       WLLogger.Write("Closed because Profit Target Reached: " + (index + 1));
                    }
                }
            }

        }*/
    }
}
