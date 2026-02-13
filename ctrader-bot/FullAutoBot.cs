// ============================================================================
// FullAutoBot - Vollautomatischer cTrader Trading Bot
// ============================================================================
// Nur 2 Parameter: Timeframe + Markt (Symbol)
// Alles andere wird automatisch berechnet und angepasst:
//   - Trend-Erkennung (Multi-EMA + ADX)
//   - Einstiegssignale (RSI + MACD + Bollinger Bands)
//   - Positionsgröße (ATR-basiertes Risikomanagement)
//   - Stop-Loss & Take-Profit (dynamisch via ATR)
//   - Trailing Stop (automatische Gewinnabsicherung)
//   - Volatilitätsfilter (handelt nicht bei zu niedriger/hoher Vola)
//   - Session-Filter (handelt nur in aktiven Marktzeiten)
//   - Max Drawdown Schutz
// ============================================================================

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None, AddIndicators = true)]
    public class FullAutoBot : Robot
    {
        // =====================================================================
        // NUR DIESE 2 PARAMETER - ALLES ANDERE IST AUTOMATISCH
        // =====================================================================

        [Parameter("Timeframe", DefaultValue = "Hour1")]
        public TimeFrame BotTimeframe { get; set; }

        [Parameter("Markt (Symbol)", DefaultValue = "EURUSD")]
        public string MarktSymbol { get; set; }

        // =====================================================================
        // AUTOMATISCH BERECHNETE INTERNE VARIABLEN
        // =====================================================================

        // Indikatoren
        private ExponentialMovingAverage _emaFast;
        private ExponentialMovingAverage _emaMedium;
        private ExponentialMovingAverage _emaSlow;
        private RelativeStrengthIndex _rsi;
        private MacdCrossOver _macd;
        private BollingerBands _bollingerBands;
        private AverageTrueRange _atr;
        private DirectionalMovementSystem _adx;

        // Higher Timeframe Trend
        private Bars _higherTimeframeBars;
        private ExponentialMovingAverage _htfEmaFast;
        private ExponentialMovingAverage _htfEmaSlow;

        // Markt-Referenz
        private Symbol _marktSymbol;
        private Bars _marktBars;

        // Risikomanagement
        private double _riskPercent;
        private double _maxDrawdownPercent;
        private double _initialBalance;
        private int _maxOpenPositions;
        private int _consecutiveLosses;
        private double _currentDrawdown;

        // Adaptive Parameter
        private int _emaFastPeriod;
        private int _emaMediumPeriod;
        private int _emaSlowPeriod;
        private int _rsiPeriod;
        private int _atrPeriod;
        private int _adxPeriod;
        private int _bollingerPeriod;
        private double _bollingerStdDev;
        private double _atrMultiplierSL;
        private double _atrMultiplierTP;
        private double _trailingAtrMultiplier;

        // Signal-Tracking
        private int _signalCooldown;
        private DateTime _lastTradeTime;
        private const string BotLabel = "FullAutoBot";

        // =====================================================================
        // INITIALISIERUNG
        // =====================================================================

        protected override void OnStart()
        {
            Print("=== FullAutoBot wird gestartet ===");
            Print("Markt: {0} | Timeframe: {1}", MarktSymbol, BotTimeframe);

            // Symbol laden
            _marktSymbol = Symbols.GetSymbol(MarktSymbol);
            if (_marktSymbol == null)
            {
                Print("FEHLER: Symbol {0} nicht gefunden!", MarktSymbol);
                Stop();
                return;
            }

            // Bars für den gewählten Timeframe laden
            _marktBars = MarketData.GetBars(BotTimeframe, _marktSymbol.Name);

            // Parameter automatisch an Timeframe anpassen
            AdaptiereParameterAnTimeframe();

            // Indikatoren initialisieren
            InitialisiereIndikatoren();

            // Higher Timeframe für Trendbestätigung
            InitialisiereHigherTimeframe();

            // Risikomanagement initialisieren
            _initialBalance = Account.Balance;
            _consecutiveLosses = 0;
            _currentDrawdown = 0;
            _lastTradeTime = DateTime.MinValue;

            // Auf neue Bars reagieren
            _marktBars.BarOpened += OnBarOpened;

            Print("Bot erfolgreich initialisiert.");
            Print("Risiko pro Trade: {0:F1}% | Max Drawdown: {1:F1}%", _riskPercent, _maxDrawdownPercent);
            Print("EMA: {0}/{1}/{2} | RSI: {3} | ATR: {4}", _emaFastPeriod, _emaMediumPeriod, _emaSlowPeriod, _rsiPeriod, _atrPeriod);
            Print("SL-Multiplikator: {0:F1}x ATR | TP-Multiplikator: {1:F1}x ATR", _atrMultiplierSL, _atrMultiplierTP);
        }

        // =====================================================================
        // PARAMETER AUTOMATISCH AN TIMEFRAME ANPASSEN
        // =====================================================================

        private void AdaptiereParameterAnTimeframe()
        {
            // Timeframe in Minuten umrechnen für adaptive Berechnung
            int tfMinuten = TimeframeZuMinuten(BotTimeframe);

            if (tfMinuten <= 5)
            {
                // M1 - M5: Scalping-Modus
                _emaFastPeriod = 8;
                _emaMediumPeriod = 21;
                _emaSlowPeriod = 55;
                _rsiPeriod = 10;
                _atrPeriod = 14;
                _adxPeriod = 14;
                _bollingerPeriod = 20;
                _bollingerStdDev = 2.0;
                _atrMultiplierSL = 1.5;
                _atrMultiplierTP = 2.0;
                _trailingAtrMultiplier = 1.0;
                _riskPercent = 0.5;
                _maxDrawdownPercent = 5.0;
                _maxOpenPositions = 2;
                _signalCooldown = 3;
            }
            else if (tfMinuten <= 30)
            {
                // M15 - M30: Intraday-Modus
                _emaFastPeriod = 10;
                _emaMediumPeriod = 25;
                _emaSlowPeriod = 50;
                _rsiPeriod = 14;
                _atrPeriod = 14;
                _adxPeriod = 14;
                _bollingerPeriod = 20;
                _bollingerStdDev = 2.0;
                _atrMultiplierSL = 1.8;
                _atrMultiplierTP = 2.5;
                _trailingAtrMultiplier = 1.2;
                _riskPercent = 0.75;
                _maxDrawdownPercent = 6.0;
                _maxOpenPositions = 3;
                _signalCooldown = 2;
            }
            else if (tfMinuten <= 240)
            {
                // H1 - H4: Swing-Modus
                _emaFastPeriod = 12;
                _emaMediumPeriod = 26;
                _emaSlowPeriod = 50;
                _rsiPeriod = 14;
                _atrPeriod = 14;
                _adxPeriod = 14;
                _bollingerPeriod = 20;
                _bollingerStdDev = 2.0;
                _atrMultiplierSL = 2.0;
                _atrMultiplierTP = 3.0;
                _trailingAtrMultiplier = 1.5;
                _riskPercent = 1.0;
                _maxDrawdownPercent = 8.0;
                _maxOpenPositions = 3;
                _signalCooldown = 1;
            }
            else
            {
                // Daily+: Positions-Modus
                _emaFastPeriod = 10;
                _emaMediumPeriod = 21;
                _emaSlowPeriod = 50;
                _rsiPeriod = 14;
                _atrPeriod = 20;
                _adxPeriod = 14;
                _bollingerPeriod = 20;
                _bollingerStdDev = 2.0;
                _atrMultiplierSL = 2.5;
                _atrMultiplierTP = 4.0;
                _trailingAtrMultiplier = 2.0;
                _riskPercent = 1.5;
                _maxDrawdownPercent = 10.0;
                _maxOpenPositions = 4;
                _signalCooldown = 1;
            }
        }

        // =====================================================================
        // INDIKATOREN INITIALISIEREN
        // =====================================================================

        private void InitialisiereIndikatoren()
        {
            var closeSeries = _marktBars.ClosePrices;

            _emaFast = Indicators.ExponentialMovingAverage(closeSeries, _emaFastPeriod);
            _emaMedium = Indicators.ExponentialMovingAverage(closeSeries, _emaMediumPeriod);
            _emaSlow = Indicators.ExponentialMovingAverage(closeSeries, _emaSlowPeriod);
            _rsi = Indicators.RelativeStrengthIndex(closeSeries, _rsiPeriod);
            _macd = Indicators.MacdCrossOver(closeSeries, 12, 26, 9);
            _bollingerBands = Indicators.BollingerBands(closeSeries, _bollingerPeriod, _bollingerStdDev, MovingAverageType.Exponential);
            _atr = Indicators.AverageTrueRange(_marktBars, _atrPeriod, MovingAverageType.Exponential);
            _adx = Indicators.DirectionalMovementSystem(_marktBars, _adxPeriod);
        }

        // =====================================================================
        // HIGHER TIMEFRAME TREND
        // =====================================================================

        private void InitialisiereHigherTimeframe()
        {
            TimeFrame htf = ErmittleHigherTimeframe(BotTimeframe);
            _higherTimeframeBars = MarketData.GetBars(htf, _marktSymbol.Name);
            _htfEmaFast = Indicators.ExponentialMovingAverage(_higherTimeframeBars.ClosePrices, 12);
            _htfEmaSlow = Indicators.ExponentialMovingAverage(_higherTimeframeBars.ClosePrices, 26);
            Print("Higher Timeframe für Trend: {0}", htf);
        }

        private TimeFrame ErmittleHigherTimeframe(TimeFrame tf)
        {
            int minuten = TimeframeZuMinuten(tf);

            if (minuten <= 5) return TimeFrame.Hour1;
            if (minuten <= 15) return TimeFrame.Hour4;
            if (minuten <= 60) return TimeFrame.Daily;
            if (minuten <= 240) return TimeFrame.Weekly;
            return TimeFrame.Monthly;
        }

        // =====================================================================
        // HAUPTLOGIK - BEI JEDER NEUEN BAR
        // =====================================================================

        private void OnBarOpened(BarOpenedEventArgs args)
        {
            // Sicherheitschecks
            if (!DarfHandeln())
                return;

            // Bestehende Positionen verwalten (Trailing Stop)
            VerwalteBestehendePositionen();

            // Aktuelle Werte auslesen
            int index = _marktBars.ClosePrices.Count - 2; // Letzte abgeschlossene Bar
            if (index < _emaSlowPeriod + 10)
                return;

            // Marktanalyse durchführen
            var analyse = AnalysiereMarkt(index);

            // Handelssignale prüfen und ausführen
            if (analyse.Signal != SignalTyp.Kein)
            {
                FuehreTradeAus(analyse);
            }
        }

        // =====================================================================
        // MARKTANALYSE - VOLLAUTOMATISCH
        // =====================================================================

        private MarktAnalyse AnalysiereMarkt(int index)
        {
            var analyse = new MarktAnalyse();

            // 1. Higher Timeframe Trend bestimmen
            analyse.HtfTrend = ErmittleHTFTrend();

            // 2. Aktueller Trend via EMAs
            analyse.EmaSignal = ErmittleEmaTrend(index);

            // 3. Trendstärke via ADX
            analyse.Trendstaerke = _adx.ADX.Last(1);
            analyse.IstTrendStark = analyse.Trendstaerke > 20;

            // 4. RSI-Analyse
            analyse.RsiWert = _rsi.Result.Last(1);
            analyse.RsiUeberkauft = analyse.RsiWert > 70;
            analyse.RsiUeberverkauft = analyse.RsiWert < 30;
            analyse.RsiNeutral = analyse.RsiWert > 40 && analyse.RsiWert < 60;

            // 5. MACD-Analyse
            analyse.MacdHistogramm = _macd.Histogram.Last(1);
            analyse.MacdHistogrammVorher = _macd.Histogram.Last(2);
            analyse.MacdBullishCross = analyse.MacdHistogramm > 0 && analyse.MacdHistogrammVorher <= 0;
            analyse.MacdBearishCross = analyse.MacdHistogramm < 0 && analyse.MacdHistogrammVorher >= 0;

            // 6. Bollinger Bands Analyse
            double close = _marktBars.ClosePrices.Last(1);
            analyse.PreisNahOberemBand = close >= _bollingerBands.Top.Last(1) * 0.998;
            analyse.PreisNahUnteremBand = close <= _bollingerBands.Bottom.Last(1) * 1.002;
            analyse.BollingerBreite = (_bollingerBands.Top.Last(1) - _bollingerBands.Bottom.Last(1)) / _bollingerBands.Main.Last(1);

            // 7. Volatilitätsanalyse via ATR
            analyse.AtrWert = _atr.Result.Last(1);
            analyse.AtrProzent = (analyse.AtrWert / close) * 100;

            // Volatilitätsfilter: Nicht handeln bei extrem niedriger oder hoher Vola
            analyse.VolatilitaetOk = analyse.AtrProzent > 0.02 && analyse.AtrProzent < 2.0;

            // 8. Kerzenformation der letzten Bar
            double open = _marktBars.OpenPrices.Last(1);
            double high = _marktBars.HighPrices.Last(1);
            double low = _marktBars.LowPrices.Last(1);
            analyse.IstBullishKerze = close > open;
            analyse.IstBearishKerze = close < open;
            analyse.KerzenKoerper = Math.Abs(close - open);
            analyse.ObererDocht = high - Math.Max(close, open);
            analyse.UntererDocht = Math.Min(close, open) - low;

            // 9. Gesamtsignal berechnen
            analyse.Signal = BerechneGesamtSignal(analyse);

            return analyse;
        }

        // =====================================================================
        // TREND-ERKENNUNG
        // =====================================================================

        private TrendRichtung ErmittleHTFTrend()
        {
            if (_higherTimeframeBars.ClosePrices.Count < 30)
                return TrendRichtung.Seitwaerts;

            double htfFast = _htfEmaFast.Result.Last(1);
            double htfSlow = _htfEmaSlow.Result.Last(1);
            double htfClose = _higherTimeframeBars.ClosePrices.Last(1);

            if (htfFast > htfSlow && htfClose > htfFast)
                return TrendRichtung.Aufwaerts;
            if (htfFast < htfSlow && htfClose < htfFast)
                return TrendRichtung.Abwaerts;
            return TrendRichtung.Seitwaerts;
        }

        private TrendRichtung ErmittleEmaTrend(int index)
        {
            double fast = _emaFast.Result.Last(1);
            double medium = _emaMedium.Result.Last(1);
            double slow = _emaSlow.Result.Last(1);

            // Klarer Aufwärtstrend: Fast > Medium > Slow
            if (fast > medium && medium > slow)
                return TrendRichtung.Aufwaerts;

            // Klarer Abwärtstrend: Fast < Medium < Slow
            if (fast < medium && medium < slow)
                return TrendRichtung.Abwaerts;

            return TrendRichtung.Seitwaerts;
        }

        // =====================================================================
        // SIGNAL-BERECHNUNG (SCORING SYSTEM)
        // =====================================================================

        private SignalTyp BerechneGesamtSignal(MarktAnalyse analyse)
        {
            // Volatilitätsfilter
            if (!analyse.VolatilitaetOk)
                return SignalTyp.Kein;

            int buyScore = 0;
            int sellScore = 0;

            // --- BUY SCORING ---

            // Higher Timeframe Trend (stärkstes Signal)
            if (analyse.HtfTrend == TrendRichtung.Aufwaerts) buyScore += 3;
            if (analyse.HtfTrend == TrendRichtung.Abwaerts) buyScore -= 2;

            // EMA Trend
            if (analyse.EmaSignal == TrendRichtung.Aufwaerts) buyScore += 2;

            // ADX Trendstärke
            if (analyse.IstTrendStark && analyse.EmaSignal == TrendRichtung.Aufwaerts) buyScore += 1;

            // RSI
            if (analyse.RsiUeberverkauft) buyScore += 2;
            if (analyse.RsiWert < 45 && analyse.RsiWert > 30) buyScore += 1;
            if (analyse.RsiUeberkauft) buyScore -= 2;

            // MACD
            if (analyse.MacdBullishCross) buyScore += 2;
            if (analyse.MacdHistogramm > 0 && analyse.MacdHistogramm > analyse.MacdHistogrammVorher) buyScore += 1;

            // Bollinger Bands
            if (analyse.PreisNahUnteremBand) buyScore += 1;

            // Bullische Kerze
            if (analyse.IstBullishKerze && analyse.KerzenKoerper > analyse.UntererDocht) buyScore += 1;

            // --- SELL SCORING ---

            // Higher Timeframe Trend
            if (analyse.HtfTrend == TrendRichtung.Abwaerts) sellScore += 3;
            if (analyse.HtfTrend == TrendRichtung.Aufwaerts) sellScore -= 2;

            // EMA Trend
            if (analyse.EmaSignal == TrendRichtung.Abwaerts) sellScore += 2;

            // ADX Trendstärke
            if (analyse.IstTrendStark && analyse.EmaSignal == TrendRichtung.Abwaerts) sellScore += 1;

            // RSI
            if (analyse.RsiUeberkauft) sellScore += 2;
            if (analyse.RsiWert > 55 && analyse.RsiWert < 70) sellScore += 1;
            if (analyse.RsiUeberverkauft) sellScore -= 2;

            // MACD
            if (analyse.MacdBearishCross) sellScore += 2;
            if (analyse.MacdHistogramm < 0 && analyse.MacdHistogramm < analyse.MacdHistogrammVorher) sellScore += 1;

            // Bollinger Bands
            if (analyse.PreisNahOberemBand) sellScore += 1;

            // Bärische Kerze
            if (analyse.IstBearishKerze && analyse.KerzenKoerper > analyse.ObererDocht) sellScore += 1;

            // --- ENTSCHEIDUNG ---
            int minScore = 5; // Mindestens 5 Punkte für ein Signal

            if (buyScore >= minScore && buyScore > sellScore + 2)
            {
                Print("BUY Signal - Score: {0} (Sell: {1}) | HTF: {2} | RSI: {3:F1} | ADX: {4:F1}",
                    buyScore, sellScore, analyse.HtfTrend, analyse.RsiWert, analyse.Trendstaerke);
                return SignalTyp.Buy;
            }

            if (sellScore >= minScore && sellScore > buyScore + 2)
            {
                Print("SELL Signal - Score: {0} (Buy: {1}) | HTF: {2} | RSI: {3:F1} | ADX: {4:F1}",
                    sellScore, buyScore, analyse.HtfTrend, analyse.RsiWert, analyse.Trendstaerke);
                return SignalTyp.Sell;
            }

            return SignalTyp.Kein;
        }

        // =====================================================================
        // TRADE AUSFÜHREN
        // =====================================================================

        private void FuehreTradeAus(MarktAnalyse analyse)
        {
            // Prüfen ob bereits max Positionen offen
            var offenePositionen = Positions.FindAll(BotLabel, _marktSymbol.Name);
            if (offenePositionen.Length >= _maxOpenPositions)
            {
                Print("Max offene Positionen ({0}) erreicht - kein neuer Trade", _maxOpenPositions);
                return;
            }

            // Cooldown prüfen
            if ((Server.Time - _lastTradeTime).TotalMinutes < _signalCooldown * TimeframeZuMinuten(BotTimeframe))
            {
                Print("Signal-Cooldown aktiv - kein neuer Trade");
                return;
            }

            // ATR-basierte Level berechnen
            double atr = analyse.AtrWert;
            double stopLossPips = AtrZuPips(atr * _atrMultiplierSL);
            double takeProfitPips = AtrZuPips(atr * _atrMultiplierTP);

            // Minimale SL/TP-Distanz sicherstellen
            double minPips = _marktSymbol.Spread * 3;
            stopLossPips = Math.Max(stopLossPips, minPips);
            takeProfitPips = Math.Max(takeProfitPips, minPips);

            // Positionsgröße berechnen (risiko-basiert)
            double positionsGroesse = BerechnePositionsGroesse(stopLossPips);
            if (positionsGroesse < _marktSymbol.VolumeInUnitsMin)
            {
                Print("Berechnete Position zu klein - kein Trade");
                return;
            }

            // Adaptive Risikoanpassung bei Verlustserie
            if (_consecutiveLosses >= 3)
            {
                positionsGroesse *= 0.5;
                Print("Verlustserie ({0}) - Positionsgröße halbiert", _consecutiveLosses);
            }

            // Volume normalisieren
            positionsGroesse = _marktSymbol.NormalizeVolumeInUnits(positionsGroesse, RoundingMode.Down);

            if (positionsGroesse < _marktSymbol.VolumeInUnitsMin)
                return;

            // Trade platzieren
            TradeType richtung = analyse.Signal == SignalTyp.Buy ? TradeType.Buy : TradeType.Sell;

            var result = ExecuteMarketOrder(
                richtung,
                _marktSymbol.Name,
                positionsGroesse,
                BotLabel,
                stopLossPips,
                takeProfitPips
            );

            if (result.IsSuccessful)
            {
                _lastTradeTime = Server.Time;
                Print("Trade eröffnet: {0} {1:F0} Einheiten {2} | SL: {3:F1} Pips | TP: {4:F1} Pips",
                    richtung, positionsGroesse, _marktSymbol.Name, stopLossPips, takeProfitPips);
            }
            else
            {
                Print("Trade fehlgeschlagen: {0}", result.Error);
            }
        }

        // =====================================================================
        // POSITIONSGRÖSSE BERECHNEN
        // =====================================================================

        private double BerechnePositionsGroesse(double stopLossPips)
        {
            // Risikobetrag = Kontostand * Risikoprozent
            double risikoBetrag = Account.Balance * (_riskPercent / 100.0);

            // Pip-Wert berechnen
            double pipValue = _marktSymbol.PipValue;

            if (pipValue <= 0 || stopLossPips <= 0)
                return _marktSymbol.VolumeInUnitsMin;

            // Positionsgröße = Risikobetrag / (SL in Pips * Pip-Wert)
            double volume = risikoBetrag / (stopLossPips * pipValue);

            return volume;
        }

        // =====================================================================
        // TRAILING STOP - BESTEHENDE POSITIONEN VERWALTEN
        // =====================================================================

        private void VerwalteBestehendePositionen()
        {
            var positionen = Positions.FindAll(BotLabel, _marktSymbol.Name);

            foreach (var position in positionen)
            {
                double atr = _atr.Result.Last(1);
                double trailingDistanzPips = AtrZuPips(atr * _trailingAtrMultiplier);

                // Trailing Stop nur aktivieren, wenn Position im Gewinn ist
                if (position.Pips > trailingDistanzPips)
                {
                    double neuerSL;

                    if (position.TradeType == TradeType.Buy)
                    {
                        neuerSL = _marktSymbol.Bid - (trailingDistanzPips * _marktSymbol.PipSize);
                        if (position.StopLoss == null || neuerSL > position.StopLoss)
                        {
                            ModifyPosition(position, neuerSL, position.TakeProfit);
                        }
                    }
                    else
                    {
                        neuerSL = _marktSymbol.Ask + (trailingDistanzPips * _marktSymbol.PipSize);
                        if (position.StopLoss == null || neuerSL < position.StopLoss)
                        {
                            ModifyPosition(position, neuerSL, position.TakeProfit);
                        }
                    }
                }
            }
        }

        // =====================================================================
        // SICHERHEITSCHECKS
        // =====================================================================

        private bool DarfHandeln()
        {
            // Max Drawdown Check
            _currentDrawdown = ((_initialBalance - Account.Balance) / _initialBalance) * 100;
            if (_currentDrawdown >= _maxDrawdownPercent)
            {
                Print("MAX DRAWDOWN erreicht ({0:F1}%) - Bot pausiert!", _currentDrawdown);
                return false;
            }

            // Session-Filter: Nur während aktiver Marktzeiten handeln
            if (!IstAktiveHandelszeit())
                return false;

            // Wochenend-Filter
            if (Server.Time.DayOfWeek == DayOfWeek.Saturday || Server.Time.DayOfWeek == DayOfWeek.Sunday)
                return false;

            return true;
        }

        private bool IstAktiveHandelszeit()
        {
            int stunde = Server.Time.Hour;

            // Forex/Indizes: Haupthandelszeiten (London + New York overlap)
            // UTC 07:00 - 21:00
            return stunde >= 7 && stunde <= 21;
        }

        // =====================================================================
        // POSITIONS-EVENTS
        // =====================================================================

        protected override void OnStop()
        {
            Print("=== FullAutoBot wird gestoppt ===");
            Print("Endgültiger Kontostand: {0:F2} | Drawdown: {1:F1}%", Account.Balance, _currentDrawdown);
            _marktBars.BarOpened -= OnBarOpened;
        }

        // =====================================================================
        // HILFSFUNKTIONEN
        // =====================================================================

        private double AtrZuPips(double atrWert)
        {
            return atrWert / _marktSymbol.PipSize;
        }

        private int TimeframeZuMinuten(TimeFrame tf)
        {
            if (tf == TimeFrame.Minute) return 1;
            if (tf == TimeFrame.Minute2) return 2;
            if (tf == TimeFrame.Minute3) return 3;
            if (tf == TimeFrame.Minute4) return 4;
            if (tf == TimeFrame.Minute5) return 5;
            if (tf == TimeFrame.Minute6) return 6;
            if (tf == TimeFrame.Minute7) return 7;
            if (tf == TimeFrame.Minute8) return 8;
            if (tf == TimeFrame.Minute9) return 9;
            if (tf == TimeFrame.Minute10) return 10;
            if (tf == TimeFrame.Minute15) return 15;
            if (tf == TimeFrame.Minute20) return 20;
            if (tf == TimeFrame.Minute30) return 30;
            if (tf == TimeFrame.Minute45) return 45;
            if (tf == TimeFrame.Hour) return 60;
            if (tf == TimeFrame.Hour2) return 120;
            if (tf == TimeFrame.Hour3) return 180;
            if (tf == TimeFrame.Hour4) return 240;
            if (tf == TimeFrame.Hour6) return 360;
            if (tf == TimeFrame.Hour8) return 480;
            if (tf == TimeFrame.Hour12) return 720;
            if (tf == TimeFrame.Daily) return 1440;
            if (tf == TimeFrame.Day2) return 2880;
            if (tf == TimeFrame.Day3) return 4320;
            if (tf == TimeFrame.Weekly) return 10080;
            if (tf == TimeFrame.Monthly) return 43200;
            return 60; // Default: H1
        }

        // =====================================================================
        // DATENSTRUKTUREN
        // =====================================================================

        private enum TrendRichtung
        {
            Aufwaerts,
            Abwaerts,
            Seitwaerts
        }

        private enum SignalTyp
        {
            Kein,
            Buy,
            Sell
        }

        private class MarktAnalyse
        {
            // Trend
            public TrendRichtung HtfTrend { get; set; }
            public TrendRichtung EmaSignal { get; set; }
            public double Trendstaerke { get; set; }
            public bool IstTrendStark { get; set; }

            // RSI
            public double RsiWert { get; set; }
            public bool RsiUeberkauft { get; set; }
            public bool RsiUeberverkauft { get; set; }
            public bool RsiNeutral { get; set; }

            // MACD
            public double MacdHistogramm { get; set; }
            public double MacdHistogrammVorher { get; set; }
            public bool MacdBullishCross { get; set; }
            public bool MacdBearishCross { get; set; }

            // Bollinger Bands
            public bool PreisNahOberemBand { get; set; }
            public bool PreisNahUnteremBand { get; set; }
            public double BollingerBreite { get; set; }

            // Volatilität
            public double AtrWert { get; set; }
            public double AtrProzent { get; set; }
            public bool VolatilitaetOk { get; set; }

            // Kerzenformation
            public bool IstBullishKerze { get; set; }
            public bool IstBearishKerze { get; set; }
            public double KerzenKoerper { get; set; }
            public double ObererDocht { get; set; }
            public double UntererDocht { get; set; }

            // Ergebnis
            public SignalTyp Signal { get; set; }
        }
    }
}
