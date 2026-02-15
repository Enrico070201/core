// ============================================================================
// FullAutoBot v3.3 - Vollautomatischer cTrader Trading Bot
// ============================================================================
// Nur 2 Parameter: Timeframe + Markt (Symbol)
// Alles andere wird automatisch berechnet und angepasst.
//
// v3.3 Profitabilitäts-Upgrade:
//   - Daily Loss Limit: Tageshandel stoppt nach X% Verlust (kein Revenge-Trading)
//   - Max Daily Trades: Übertrading-Schutz (max 6/Tag)
//   - Session-Qualitäts-System: Granularer Risiko-Multiplikator pro Handelszeit
//   - Min R:R Enforcement: Trades unter 1.8:1 Risk/Reward werden abgelehnt
//   - Zweiter Partial Close: +50% bei 4R (stufenweise Gewinnmitnahme)
//   - Early-Exit bei Schwäche: Raus wenn 3+ Gegen-Signale bei kleinem Gewinn
//   - Montag-Morgen-Filter: Kein Trading vor 10 UTC am Montag
//   - Freitag ab 18 UTC: Keine neuen Trades (statt 20)
//   - Pyramiding verschärft: Score 10+, 2 ATR Gewinn, nur in Trend-Regimes
//   - Stale-Trade verbessert: Auch leichte Verlierer nach langer Zeit schließen
//   - Tages-P&L-basierte Risikoanpassung (schützt Gewinntage)
//   - TP leicht erhöht für besseres R:R
//
// v3.2 Fixes:
//   - Warm-up Phase: Erste 10 Trades nur 50% Risiko
//   - Signal-Selektivität: minScore +1, Diff 3, Cooldown min 2
//   - Risk-Cap: Score-Multiplikatoren gedämpft, Max 2.0x Basis
//   - Partial Close Fix: Einmal 40% statt wiederholte 25%
//
// v3 Verbesserungen:
//   - Flexibler HTF-Filter statt binärem Block (Seitwärts +1, Gegen +Sperre)
//   - Bollinger-Squeeze-Breakout auch in Konsolidierung erlaubt
//   - Adaptive RSI-Schwellen je nach Trendrichtung
//   - Session-Qualitäts-Bonus (London/NY Overlap)
//   - Stärkere ADX-Gewichtung bei hoher Trendstärke
//   - Konfluenz-Boost: Volle Zeitebenen-Übereinstimmung = +2
//   - Break-Even bei 1.5R (nicht 1.0R - verhindert Ausstoppung)
//   - Progressiver Trailing: enger je höher der Gewinn
//   - Partial Close 40% bei 2.5R (einmalig), Rest trailing lassen
//   - Anti-Martingale: 1 Verlust = normal, erst ab 2 bremsen
//   - ADX-gewichtetes Risiko für Trend-Überzeugung
// ============================================================================

using System;
using System.Collections.Generic;
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

        [Parameter("Timeframe", DefaultValue = "Hour")]
        public TimeFrame BotTimeframe { get; set; }

        [Parameter("Markt (Symbol)", DefaultValue = "EURUSD")]
        public string MarktSymbol { get; set; }

        // =====================================================================
        // INDIKATOREN
        // =====================================================================

        private ExponentialMovingAverage _emaFast;
        private ExponentialMovingAverage _emaMedium;
        private ExponentialMovingAverage _emaSlow;
        private ExponentialMovingAverage _ema200;
        private RelativeStrengthIndex _rsi;
        private MacdCrossOver _macd;
        private BollingerBands _bollingerBands;
        private AverageTrueRange _atr;
        private DirectionalMovementSystem _adx;

        // Higher Timeframe
        private Bars _higherTimeframeBars;
        private ExponentialMovingAverage _htfEmaFast;
        private ExponentialMovingAverage _htfEmaSlow;
        private AverageTrueRange _htfAtr;

        // Markt-Referenz
        private Symbol _marktSymbol;
        private Bars _marktBars;

        // =====================================================================
        // PERFORMANCE-TRACKING (für Kelly-Criterion & adaptive Größen)
        // =====================================================================

        private readonly List<double> _tradeResultsPips = new List<double>();
        private int _totalWins;
        private int _totalLosses;
        private double _summeGewinne;
        private double _summeVerluste;
        private int _consecutiveWins;
        private int _consecutiveLosses;
        private double _peakBalance;
        private double _initialBalance;
        private double _currentDrawdown;

        // =====================================================================
        // ADAPTIVE PARAMETER
        // =====================================================================

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
        private double _baseRiskPercent;
        private double _maxDrawdownPercent;
        private int _maxOpenPositions;
        private int _signalCooldown;
        private int _staleTradeBarCount;

        // Signal-Tracking
        private DateTime _lastTradeTime;
        private double _lastSpread;
        private double _avgSpread;
        private int _spreadSampleCount;
        private MarktRegime _aktuellesRegime;

        // =====================================================================
        // ADAPTIVE EVENT-TRACKING
        // =====================================================================

        // Rolling Performance: Letzte N Trades statt nur "consecutive"
        private readonly Queue<double> _rollendeErgebnissePips = new Queue<double>();
        private readonly Queue<TradeType> _rollendeRichtungen = new Queue<TradeType>();
        private readonly Queue<bool> _rollendeErfolge = new Queue<bool>();
        private const int RollendesFenster = 12;
        private double _rollendeErwartung;
        private double _rollendeWinRate;

        // Volatilitäts-Regime: ATR(5) vs ATR(14) Ratio
        private AverageTrueRange _atrKurz;
        private double _volatilitaetsRatio; // >1 = expandierend, <1 = kontrahierend

        // Regime-Wechsel-Tracking
        private MarktRegime _vorherigesRegime;
        private int _barsSeitRegimeWechsel;
        private int _regimeWechselLetzten20Bars;
        private readonly Queue<bool> _regimeWechselHistorie = new Queue<bool>();

        // Richtungs-Bias: Lernt aus letzten Trade-Ergebnissen pro Richtung
        private int _recentBuyWins;
        private int _recentBuyLosses;
        private int _recentSellWins;
        private int _recentSellLosses;

        // Spread-Volatilitäts-Tracking
        private double _spreadVolatilitaet;
        private double _vorherigerAvgSpread;

        // Warm-up Tracking: Erste Trades mit reduziertem Risiko
        private int _totalTradeCount;
        private const int WarmUpTrades = 10;

        // Partial-Close Tracking: Verhindert wiederholte Micro-Closes
        private readonly HashSet<int> _partialClosedPositions = new HashSet<int>();
        private readonly HashSet<int> _secondPartialClosedPositions = new HashSet<int>();

        // Daily P&L Tracking: Tägliches Verlustlimit
        private double _dailyStartBalance;
        private DateTime _dailyResetDate;
        private double _dailyLossLimitPercent;
        private double _dailyProfitPercent;
        private int _dailyTradeCount;
        private const int MaxDailyTrades = 6; // Übertrading-Schutz

        // Session-Qualität: Multiplikator basierend auf Handelszeit
        private double _sessionQualitaet; // 0.0 - 1.0

        // Min Risk:Reward Ratio
        private double _minRiskReward;

        private const string BotLabel = "FullAutoBot";

        // =====================================================================
        // INITIALISIERUNG
        // =====================================================================

        protected override void OnStart()
        {
            Print("=== FullAutoBot v3.3 gestartet ===");
            Print("Markt: {0} | Timeframe: {1}", MarktSymbol, BotTimeframe);

            _marktSymbol = Symbols.GetSymbol(MarktSymbol);
            if (_marktSymbol == null)
            {
                Print("FEHLER: Symbol {0} nicht gefunden!", MarktSymbol);
                Stop();
                return;
            }

            _marktBars = MarketData.GetBars(BotTimeframe, _marktSymbol.Name);

            AdaptiereParameterAnTimeframe();
            InitialisiereIndikatoren();
            InitialisiereHigherTimeframe();

            // Performance-Tracking initialisieren
            _initialBalance = Account.Balance;
            _peakBalance = Account.Balance;
            _consecutiveLosses = 0;
            _consecutiveWins = 0;
            _totalWins = 0;
            _totalLosses = 0;
            _summeGewinne = 0;
            _summeVerluste = 0;
            _lastTradeTime = DateTime.MinValue;
            _avgSpread = _marktSymbol.Spread;
            _spreadSampleCount = 1;
            _aktuellesRegime = MarktRegime.Unbekannt;
            _vorherigesRegime = MarktRegime.Unbekannt;
            _barsSeitRegimeWechsel = 99;
            _regimeWechselLetzten20Bars = 0;
            _volatilitaetsRatio = 1.0;
            _rollendeErwartung = 0;
            _rollendeWinRate = 0.5;
            _spreadVolatilitaet = 0;
            _vorherigerAvgSpread = _marktSymbol.Spread;
            _totalTradeCount = 0;
            _dailyStartBalance = Account.Balance;
            _dailyResetDate = Server.Time.Date;
            _dailyProfitPercent = 0;
            _dailyTradeCount = 0;
            _sessionQualitaet = 1.0;
            _minRiskReward = 1.8; // Mindestens 1.8:1 R:R sonst kein Trade

            // Events registrieren
            _marktBars.BarOpened += OnBarOpened;
            Positions.Closed += OnPositionClosed;

            Print("Bot initialisiert | Basis-Risiko: {0:F2}% | Max Drawdown: {1:F1}%",
                _baseRiskPercent, _maxDrawdownPercent);
        }

        // =====================================================================
        // PARAMETER AUTOMATISCH AN TIMEFRAME ANPASSEN
        // =====================================================================

        private void AdaptiereParameterAnTimeframe()
        {
            int tfMinuten = TimeframeZuMinuten(BotTimeframe);

            if (tfMinuten <= 5)
            {
                // Scalping: wenige Trades, gutes R:R
                _emaFastPeriod = 8; _emaMediumPeriod = 21; _emaSlowPeriod = 55;
                _rsiPeriod = 10; _atrPeriod = 14; _adxPeriod = 14;
                _bollingerPeriod = 20; _bollingerStdDev = 2.0;
                _atrMultiplierSL = 1.5; _atrMultiplierTP = 3.0; _trailingAtrMultiplier = 1.0;
                _baseRiskPercent = 0.8; _maxDrawdownPercent = 6.0;
                _maxOpenPositions = 2; _signalCooldown = 3; _staleTradeBarCount = 30;
                _dailyLossLimitPercent = 2.0;
            }
            else if (tfMinuten <= 30)
            {
                // Intraday: Sniper-Modus
                _emaFastPeriod = 10; _emaMediumPeriod = 25; _emaSlowPeriod = 50;
                _rsiPeriod = 14; _atrPeriod = 14; _adxPeriod = 14;
                _bollingerPeriod = 20; _bollingerStdDev = 2.0;
                _atrMultiplierSL = 1.8; _atrMultiplierTP = 3.8; _trailingAtrMultiplier = 1.2;
                _baseRiskPercent = 1.2; _maxDrawdownPercent = 8.0;
                _maxOpenPositions = 2; _signalCooldown = 2; _staleTradeBarCount = 25;
                _dailyLossLimitPercent = 2.5;
            }
            else if (tfMinuten <= 240)
            {
                // Swing: großes R:R, wenige Setups
                _emaFastPeriod = 12; _emaMediumPeriod = 26; _emaSlowPeriod = 50;
                _rsiPeriod = 14; _atrPeriod = 14; _adxPeriod = 14;
                _bollingerPeriod = 20; _bollingerStdDev = 2.0;
                _atrMultiplierSL = 2.0; _atrMultiplierTP = 4.2; _trailingAtrMultiplier = 1.5;
                _baseRiskPercent = 1.6; _maxDrawdownPercent = 10.0;
                _maxOpenPositions = 2; _signalCooldown = 2; _staleTradeBarCount = 18;
                _dailyLossLimitPercent = 3.0;
            }
            else
            {
                // Positions: maximales R:R
                _emaFastPeriod = 10; _emaMediumPeriod = 21; _emaSlowPeriod = 50;
                _rsiPeriod = 14; _atrPeriod = 20; _adxPeriod = 14;
                _bollingerPeriod = 20; _bollingerStdDev = 2.0;
                _atrMultiplierSL = 2.5; _atrMultiplierTP = 5.5; _trailingAtrMultiplier = 2.0;
                _baseRiskPercent = 2.2; _maxDrawdownPercent = 12.0;
                _maxOpenPositions = 2; _signalCooldown = 2; _staleTradeBarCount = 14;
                _dailyLossLimitPercent = 3.5;
            }
        }

        // =====================================================================
        // INDIKATOREN INITIALISIEREN
        // =====================================================================

        private void InitialisiereIndikatoren()
        {
            var close = _marktBars.ClosePrices;
            _emaFast = Indicators.ExponentialMovingAverage(close, _emaFastPeriod);
            _emaMedium = Indicators.ExponentialMovingAverage(close, _emaMediumPeriod);
            _emaSlow = Indicators.ExponentialMovingAverage(close, _emaSlowPeriod);
            _ema200 = Indicators.ExponentialMovingAverage(close, 200);
            _rsi = Indicators.RelativeStrengthIndex(close, _rsiPeriod);
            _macd = Indicators.MacdCrossOver(close, 12, 26, 9);
            _bollingerBands = Indicators.BollingerBands(close, _bollingerPeriod, _bollingerStdDev, MovingAverageType.Exponential);
            _atr = Indicators.AverageTrueRange(_marktBars, _atrPeriod, MovingAverageType.Exponential);
            _atrKurz = Indicators.AverageTrueRange(_marktBars, 5, MovingAverageType.Exponential);
            _adx = Indicators.DirectionalMovementSystem(_marktBars, _adxPeriod);
        }

        private void InitialisiereHigherTimeframe()
        {
            TimeFrame htf = ErmittleHigherTimeframe(BotTimeframe);
            _higherTimeframeBars = MarketData.GetBars(htf, _marktSymbol.Name);
            _htfEmaFast = Indicators.ExponentialMovingAverage(_higherTimeframeBars.ClosePrices, 12);
            _htfEmaSlow = Indicators.ExponentialMovingAverage(_higherTimeframeBars.ClosePrices, 26);
            _htfAtr = Indicators.AverageTrueRange(_higherTimeframeBars, 14, MovingAverageType.Exponential);
            Print("Higher Timeframe: {0}", htf);
        }

        private TimeFrame ErmittleHigherTimeframe(TimeFrame tf)
        {
            int minuten = TimeframeZuMinuten(tf);
            if (minuten <= 5) return TimeFrame.Hour;
            if (minuten <= 15) return TimeFrame.Hour4;
            if (minuten <= 60) return TimeFrame.Daily;
            if (minuten <= 240) return TimeFrame.Weekly;
            return TimeFrame.Monthly;
        }

        // =====================================================================
        // EVENT: POSITION GESCHLOSSEN - Performance-Tracking
        // =====================================================================

        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var pos = args.Position;
            if (pos.Label != BotLabel || pos.SymbolName != _marktSymbol.Name)
                return;

            double pips = pos.Pips;
            _tradeResultsPips.Add(pips);
            _totalTradeCount++;
            _dailyTradeCount++;

            bool istGewinn = pips > 0;

            if (istGewinn)
            {
                _totalWins++;
                _summeGewinne += pips;
                _consecutiveWins++;
                _consecutiveLosses = 0;
            }
            else
            {
                _totalLosses++;
                _summeVerluste += Math.Abs(pips);
                _consecutiveLosses++;
                _consecutiveWins = 0;
            }

            // Rolling Performance Window: Letzte N Trades
            _rollendeErgebnissePips.Enqueue(pips);
            _rollendeRichtungen.Enqueue(pos.TradeType);
            _rollendeErfolge.Enqueue(istGewinn);
            while (_rollendeErgebnissePips.Count > RollendesFenster)
            {
                _rollendeErgebnissePips.Dequeue();
                var altRichtung = _rollendeRichtungen.Dequeue();
                var altErfolg = _rollendeErfolge.Dequeue();
                // Richtungs-Tracking rückbauen
                if (altRichtung == TradeType.Buy)
                { if (altErfolg) _recentBuyWins--; else _recentBuyLosses--; }
                else
                { if (altErfolg) _recentSellWins--; else _recentSellLosses--; }
            }

            // Richtungs-Tracking aufbauen
            if (pos.TradeType == TradeType.Buy)
            { if (istGewinn) _recentBuyWins++; else _recentBuyLosses++; }
            else
            { if (istGewinn) _recentSellWins++; else _recentSellLosses++; }

            // Rolling Expectancy berechnen
            AktualisiereRollendeErwartung();

            Print("{0} {1:F1} Pips | Streak: {2}{3} | Roll-WR: {4:F0}% | Roll-Exp: {5:F1} | WR: {6:F1}%",
                istGewinn ? "WIN +" : "LOSS", pips,
                istGewinn ? _consecutiveWins : _consecutiveLosses,
                istGewinn ? "W" : "L",
                _rollendeWinRate * 100, _rollendeErwartung, WinRate() * 100);

            // Partial-Close-Tracking aufräumen
            _partialClosedPositions.Remove(pos.Id);
            _secondPartialClosedPositions.Remove(pos.Id);

            // Peak-Balance aktualisieren
            if (Account.Balance > _peakBalance)
                _peakBalance = Account.Balance;
        }

        // =====================================================================
        // HAUPTLOGIK - BEI JEDER NEUEN BAR
        // =====================================================================

        private void OnBarOpened(BarOpenedEventArgs args)
        {
            // Spread-Tracking aktualisieren
            AktualisiereSpreadTracking();

            // Volatilitäts-Regime aktualisieren
            AktualisiereVolatilitaetsRegime();

            // Session-Qualität aktualisieren
            _sessionQualitaet = BerechneSessionQualitaet();

            // Sicherheitschecks
            if (!DarfHandeln())
                return;

            // Markt-Regime erkennen mit Wechsel-Tracking
            var neuesRegime = ErkenneMarktRegime();
            bool regimeGewechselt = neuesRegime != _aktuellesRegime && _aktuellesRegime != MarktRegime.Unbekannt;

            _regimeWechselHistorie.Enqueue(regimeGewechselt);
            while (_regimeWechselHistorie.Count > 20)
                _regimeWechselHistorie.Dequeue();
            _regimeWechselLetzten20Bars = _regimeWechselHistorie.Count(x => x);

            if (regimeGewechselt)
            {
                _vorherigesRegime = _aktuellesRegime;
                _barsSeitRegimeWechsel = 0;
                Print("REGIME-WECHSEL: {0} -> {1} | Wechsel/20: {2}",
                    _vorherigesRegime, neuesRegime, _regimeWechselLetzten20Bars);
            }
            else
            {
                _barsSeitRegimeWechsel++;
            }
            _aktuellesRegime = neuesRegime;

            // Bestehende Positionen aktiv verwalten
            VerwalteBestehendePositionen();

            // Stale Trades prüfen und schließen
            PruefeStaleTradesUndReversals();

            // Regime-Cooldown: Nach Wechsel 2 Bars warten (neues Regime muss sich bestätigen)
            if (_barsSeitRegimeWechsel < 2)
            {
                Print("Regime-Cooldown: {0} Bars seit Wechsel - warte", _barsSeitRegimeWechsel);
                return;
            }

            // Chop-Filter: Zu viele Regime-Wechsel = unentschlossener Markt = raushalten
            if (_regimeWechselLetzten20Bars >= 5)
            {
                Print("CHOP erkannt: {0} Regime-Wechsel in 20 Bars - kein Trade", _regimeWechselLetzten20Bars);
                return;
            }

            // Aktuelle Werte auslesen
            int index = _marktBars.ClosePrices.Count - 2;
            if (index < 210) // 200 EMA + Puffer
                return;

            // Gap-Detection: Warnung bei großen Gaps
            if (ErkenneGap())
            {
                Print("GAP erkannt - überspringe diese Bar");
                return;
            }

            // Spread-Spike-Filter
            if (IstSpreadZuHoch())
            {
                Print("Spread-Spike erkannt ({0:F1} vs Avg {1:F1}) - kein neuer Trade",
                    _marktSymbol.Spread, _avgSpread);
                return;
            }

            // Marktanalyse durchführen
            var analyse = AnalysiereMarkt(index);

            // Pyramiding: Prüfe ob bestehende Position verstärkt werden kann
            if (analyse.Signal != SignalTyp.Kein)
            {
                var offene = Positions.FindAll(BotLabel, _marktSymbol.Name);
                bool bereitsInRichtung = offene.Any(p =>
                    (analyse.Signal == SignalTyp.Buy && p.TradeType == TradeType.Buy) ||
                    (analyse.Signal == SignalTyp.Sell && p.TradeType == TradeType.Sell));

                if (bereitsInRichtung && offene.Length < _maxOpenPositions)
                {
                    // Pyramiding nur wenn bestehende Position deutlich im Gewinn + starkes Signal
                    var bestehende = offene.First(p =>
                        (analyse.Signal == SignalTyp.Buy && p.TradeType == TradeType.Buy) ||
                        (analyse.Signal == SignalTyp.Sell && p.TradeType == TradeType.Sell));

                    // Verschärft: Min 2 ATR Gewinn + Score 10+ + nur in Trend-Regimes
                    bool trendRegime = _aktuellesRegime == MarktRegime.StarkerTrend
                        || _aktuellesRegime == MarktRegime.MittlererTrend;
                    if (bestehende.Pips > AtrZuPips(_atr.Result.Last(1) * 2.0)
                        && analyse.SignalScore >= 10
                        && trendRegime)
                    {
                        Print("PYRAMIDING: Bestehende Pos +{0:F1} Pips, Score {1}, Regime {2} - verstärke",
                            bestehende.Pips, analyse.SignalScore, _aktuellesRegime);
                        FuehreTradeAus(analyse, true);
                    }
                }
                else if (!bereitsInRichtung)
                {
                    FuehreTradeAus(analyse, false);
                }
            }
        }

        // =====================================================================
        // MARKT-REGIME-ERKENNUNG
        // =====================================================================

        private MarktRegime ErkenneMarktRegime()
        {
            double adx = _adx.ADX.Last(1);
            double bbBreite = 0;
            double bbMain = _bollingerBands.Main.Last(1);
            if (bbMain > 0)
                bbBreite = (_bollingerBands.Top.Last(1) - _bollingerBands.Bottom.Last(1)) / bbMain;

            // ADX-basierte Regime-Erkennung
            if (adx > 30 && bbBreite > 0.02)
                return MarktRegime.StarkerTrend;
            if (adx > 20)
                return MarktRegime.MittlererTrend;
            if (adx < 15 && bbBreite < 0.01)
                return MarktRegime.Konsolidierung;
            return MarktRegime.SchwacherTrend;
        }

        // =====================================================================
        // MARKTANALYSE - VOLLAUTOMATISCH
        // =====================================================================

        private MarktAnalyse AnalysiereMarkt(int index)
        {
            var analyse = new MarktAnalyse();

            // 1. Higher Timeframe Trend
            analyse.HtfTrend = ErmittleHTFTrend();

            // 2. EMA Trend
            analyse.EmaSignal = ErmittleEmaTrend(index);

            // 3. 200 EMA Langfristtrend
            double close = _marktBars.ClosePrices.Last(1);
            analyse.UeberEma200 = close > _ema200.Result.Last(1);

            // 4. ADX Trendstärke + DI-Richtung
            analyse.Trendstaerke = _adx.ADX.Last(1);
            analyse.IstTrendStark = analyse.Trendstaerke > 20;
            analyse.DiPlus = _adx.DIPlus.Last(1);
            analyse.DiMinus = _adx.DIMinus.Last(1);

            // 5. RSI mit Divergenz-Erkennung
            analyse.RsiWert = _rsi.Result.Last(1);
            analyse.RsiVorher = _rsi.Result.Last(2);
            analyse.RsiUeberkauft = analyse.RsiWert > 70;
            analyse.RsiUeberverkauft = analyse.RsiWert < 30;
            analyse.RsiBullishDivergenz = ErkenneRsiBullishDivergenz();
            analyse.RsiBearishDivergenz = ErkenneRsiBearishDivergenz();

            // 6. MACD mit Momentum-Stärke
            analyse.MacdHistogramm = _macd.Histogram.Last(1);
            analyse.MacdHistogrammVorher = _macd.Histogram.Last(2);
            analyse.MacdHistogrammVorVorher = _macd.Histogram.Last(3);
            analyse.MacdBullishCross = analyse.MacdHistogramm > 0 && analyse.MacdHistogrammVorher <= 0;
            analyse.MacdBearishCross = analyse.MacdHistogramm < 0 && analyse.MacdHistogrammVorher >= 0;
            // Momentum nimmt zu?
            analyse.MacdMomentumSteigt = Math.Abs(analyse.MacdHistogramm) > Math.Abs(analyse.MacdHistogrammVorher)
                && Math.Abs(analyse.MacdHistogrammVorher) > Math.Abs(analyse.MacdHistogrammVorVorher);

            // 7. Bollinger Bands
            analyse.PreisNahOberemBand = close >= _bollingerBands.Top.Last(1) * 0.998;
            analyse.PreisNahUnteremBand = close <= _bollingerBands.Bottom.Last(1) * 1.002;
            double bbMain = _bollingerBands.Main.Last(1);
            analyse.BollingerBreite = bbMain > 0 ? (_bollingerBands.Top.Last(1) - _bollingerBands.Bottom.Last(1)) / bbMain : 0;
            // Squeeze: Bollinger wird eng -> Ausbruch erwartet
            double bbBreiteVorher = 0;
            double bbMainVorher = _bollingerBands.Main.Last(5);
            if (bbMainVorher > 0)
                bbBreiteVorher = (_bollingerBands.Top.Last(5) - _bollingerBands.Bottom.Last(5)) / bbMainVorher;
            analyse.BollingerSqueeze = analyse.BollingerBreite < bbBreiteVorher * 0.7;

            // 8. ATR / Volatilität
            analyse.AtrWert = _atr.Result.Last(1);
            analyse.AtrProzent = close > 0 ? (analyse.AtrWert / close) * 100 : 0;
            analyse.VolatilitaetOk = analyse.AtrProzent > 0.02 && analyse.AtrProzent < 2.0;

            // 9. Kerzenformationen (erweitert)
            AnalysiereKerzenFormationen(analyse);

            // 10. 3-Bar Momentum Konsistenz
            double close2m = _marktBars.ClosePrices.Last(2);
            double close3m = _marktBars.ClosePrices.Last(3);
            analyse.DreiBarsAufwaerts = close > close2m && close2m > close3m;
            analyse.DreiBarsAbwaerts = close < close2m && close2m < close3m;

            // 11. Close-Position innerhalb der Bar-Range
            double high1r = _marktBars.HighPrices.Last(1);
            double low1r = _marktBars.LowPrices.Last(1);
            double range1r = high1r - low1r;
            if (range1r > 0)
            {
                double closeRelativ = (close - low1r) / range1r;
                analyse.CloseImOberenDrittel = closeRelativ > 0.7;
                analyse.CloseImUnterenDrittel = closeRelativ < 0.3;
            }

            // 12. EMA-Pullback-Bounce: Preis kam zum Medium-EMA und prallt ab
            double emaMed = _emaMedium.Result.Last(1);
            double prevLow = _marktBars.LowPrices.Last(2);
            double prevHigh = _marktBars.HighPrices.Last(2);
            // Buy: Vorherige Bar berührte/durchstach Medium EMA von oben, aktuelle schließt darüber
            analyse.EmaPullbackBounceBuy = prevLow <= emaMed * 1.001 && close > emaMed
                && analyse.EmaSignal == TrendRichtung.Aufwaerts;
            // Sell: Vorherige Bar berührte Medium EMA von unten, aktuelle schließt darunter
            analyse.EmaPullbackBounceSell = prevHigh >= emaMed * 0.999 && close < emaMed
                && analyse.EmaSignal == TrendRichtung.Abwaerts;

            // 13. ATR expandiert (Markt bewegt sich, Ausbruch)
            double atrVorher3 = _atr.Result.Last(3);
            analyse.AtrExpandiert = atrVorher3 > 0 && analyse.AtrWert > atrVorher3 * 1.1;

            // 14. Volle Konfluenz: HTF + EMA + 200 EMA alle in gleicher Richtung
            analyse.VolleKonfluenzBuy = analyse.HtfTrend == TrendRichtung.Aufwaerts
                && analyse.EmaSignal == TrendRichtung.Aufwaerts && analyse.UeberEma200;
            analyse.VolleKonfluenzSell = analyse.HtfTrend == TrendRichtung.Abwaerts
                && analyse.EmaSignal == TrendRichtung.Abwaerts && !analyse.UeberEma200;

            // 15. Markt-Regime
            analyse.Regime = _aktuellesRegime;

            // 16. Gesamtsignal berechnen
            BerechneGesamtSignal(analyse);

            return analyse;
        }

        // =====================================================================
        // KERZENFORMATIONEN (ERWEITERT)
        // =====================================================================

        private void AnalysiereKerzenFormationen(MarktAnalyse analyse)
        {
            double close1 = _marktBars.ClosePrices.Last(1);
            double open1 = _marktBars.OpenPrices.Last(1);
            double high1 = _marktBars.HighPrices.Last(1);
            double low1 = _marktBars.LowPrices.Last(1);
            double close2 = _marktBars.ClosePrices.Last(2);
            double open2 = _marktBars.OpenPrices.Last(2);

            double koerper = Math.Abs(close1 - open1);
            double oberDocht = high1 - Math.Max(close1, open1);
            double unterDocht = Math.Min(close1, open1) - low1;
            double gesamtRange = high1 - low1;

            analyse.IstBullishKerze = close1 > open1;
            analyse.IstBearishKerze = close1 < open1;
            analyse.KerzenKoerper = koerper;
            analyse.ObererDocht = oberDocht;
            analyse.UntererDocht = unterDocht;

            // Engulfing Pattern
            analyse.BullishEngulfing = close1 > open1 && close2 < open2
                && close1 > open2 && open1 < close2;
            analyse.BearishEngulfing = close1 < open1 && close2 > open2
                && close1 < open2 && open1 > close2;

            // Pin Bar / Hammer
            if (gesamtRange > 0)
            {
                analyse.BullishPinBar = unterDocht > koerper * 2.0 && oberDocht < koerper * 0.5
                    && unterDocht > gesamtRange * 0.6;
                analyse.BearishPinBar = oberDocht > koerper * 2.0 && unterDocht < koerper * 0.5
                    && oberDocht > gesamtRange * 0.6;
            }

            // Starke Momentum-Kerze (großer Körper, kleine Dochte)
            if (gesamtRange > 0)
            {
                analyse.StarkeMomentumKerze = koerper > gesamtRange * 0.75;
            }
        }

        // =====================================================================
        // RSI DIVERGENZ-ERKENNUNG
        // =====================================================================

        private bool ErkenneRsiBullishDivergenz()
        {
            if (_marktBars.LowPrices.Count < 15 || _rsi.Result.Count < 15)
                return false;

            // Preis macht tieferes Tief, RSI macht höheres Tief
            double preisLow1 = _marktBars.LowPrices.Last(1);
            double preisLow5 = _marktBars.LowPrices.Minimum(10);
            double rsi1 = _rsi.Result.Last(1);

            // Finde RSI am Preistief
            double rsiAmTief = double.MaxValue;
            for (int i = 2; i <= 10; i++)
            {
                if (_marktBars.LowPrices.Last(i) <= preisLow5 * 1.001)
                {
                    rsiAmTief = Math.Min(rsiAmTief, _rsi.Result.Last(i));
                    break;
                }
            }

            return preisLow1 <= preisLow5 * 1.001 && rsi1 > rsiAmTief + 3 && rsi1 < 40;
        }

        private bool ErkenneRsiBearishDivergenz()
        {
            if (_marktBars.HighPrices.Count < 15 || _rsi.Result.Count < 15)
                return false;

            double preisHigh1 = _marktBars.HighPrices.Last(1);
            double preisHigh5 = _marktBars.HighPrices.Maximum(10);
            double rsi1 = _rsi.Result.Last(1);

            double rsiAmHoch = double.MinValue;
            for (int i = 2; i <= 10; i++)
            {
                if (_marktBars.HighPrices.Last(i) >= preisHigh5 * 0.999)
                {
                    rsiAmHoch = Math.Max(rsiAmHoch, _rsi.Result.Last(i));
                    break;
                }
            }

            return preisHigh1 >= preisHigh5 * 0.999 && rsi1 < rsiAmHoch - 3 && rsi1 > 60;
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

            if (fast > medium && medium > slow)
                return TrendRichtung.Aufwaerts;
            if (fast < medium && medium < slow)
                return TrendRichtung.Abwaerts;
            return TrendRichtung.Seitwaerts;
        }

        // =====================================================================
        // SIGNAL-BERECHNUNG (VERBESSERTES SCORING)
        // =====================================================================

        private void BerechneGesamtSignal(MarktAnalyse analyse)
        {
            analyse.Signal = SignalTyp.Kein;
            analyse.SignalScore = 0;

            if (!analyse.VolatilitaetOk)
                return;

            int buyScore = 0;
            int sellScore = 0;

            // --- GEWICHTETES BUY SCORING ---

            // Higher Timeframe Trend (Gewicht: 3)
            if (analyse.HtfTrend == TrendRichtung.Aufwaerts) buyScore += 3;
            if (analyse.HtfTrend == TrendRichtung.Abwaerts) buyScore -= 3;

            // 200 EMA Langfrist-Filter (Gewicht: 2)
            if (analyse.UeberEma200) buyScore += 2;
            else buyScore -= 1;

            // EMA Trend (Gewicht: 2)
            if (analyse.EmaSignal == TrendRichtung.Aufwaerts) buyScore += 2;

            // ADX + DI Richtung (Gewicht: 2-3, extra für starke Trends)
            if (analyse.IstTrendStark && analyse.DiPlus > analyse.DiMinus)
            {
                buyScore += 2;
                if (analyse.Trendstaerke > 30) buyScore += 1; // Bonus für sehr starken Trend
            }

            // RSI Zone - ADAPTIV an Trend (im Aufwärtstrend: Pullbacks = Kaufchance)
            double buyOversold = analyse.EmaSignal == TrendRichtung.Aufwaerts ? 40 : 30;
            double buyOverbought = analyse.EmaSignal == TrendRichtung.Aufwaerts ? 80 : 70;
            if (analyse.RsiWert < buyOversold) buyScore += 2;
            else if (analyse.RsiWert < 50 && analyse.RsiWert > buyOversold) buyScore += 1;
            if (analyse.RsiWert > buyOverbought) buyScore -= 3;

            // RSI Divergenz (Gewicht: 3 - stark!)
            if (analyse.RsiBullishDivergenz) buyScore += 3;

            // MACD (Gewicht: 2)
            if (analyse.MacdBullishCross) buyScore += 2;
            if (analyse.MacdHistogramm > 0 && analyse.MacdMomentumSteigt) buyScore += 1;

            // Bollinger (Gewicht: 1-2)
            if (analyse.PreisNahUnteremBand) buyScore += 1;
            if (analyse.BollingerSqueeze && analyse.EmaSignal == TrendRichtung.Aufwaerts) buyScore += 2;

            // Kerzenformationen (Gewicht: 2-3)
            if (analyse.BullishEngulfing) buyScore += 3;
            if (analyse.BullishPinBar) buyScore += 2;
            if (analyse.StarkeMomentumKerze && analyse.IstBullishKerze) buyScore += 2;
            else if (analyse.IstBullishKerze && analyse.KerzenKoerper > analyse.UntererDocht) buyScore += 1;

            // Regime-Bonus
            if (analyse.Regime == MarktRegime.StarkerTrend && analyse.EmaSignal == TrendRichtung.Aufwaerts)
                buyScore += 1;

            // 3-Bar Momentum Konsistenz (Gewicht: 1)
            if (analyse.DreiBarsAufwaerts) buyScore += 1;

            // Close im oberen Drittel der Bar = bullische Überzeugung (Gewicht: 1)
            if (analyse.CloseImOberenDrittel) buyScore += 1;

            // EMA-Pullback-Bounce = qualitativ hochwertiger Einstieg (Gewicht: 2)
            if (analyse.EmaPullbackBounceBuy) buyScore += 2;

            // ATR expandiert = Markt bestätigt Bewegung (Gewicht: 1)
            if (analyse.AtrExpandiert) buyScore += 1;

            // Volle Konfluenz = alle Zeitebenen einig (Gewicht: 2 - stark!)
            if (analyse.VolleKonfluenzBuy) buyScore += 2;

            // --- GEWICHTETES SELL SCORING ---

            if (analyse.HtfTrend == TrendRichtung.Abwaerts) sellScore += 3;
            if (analyse.HtfTrend == TrendRichtung.Aufwaerts) sellScore -= 3;

            if (!analyse.UeberEma200) sellScore += 2;
            else sellScore -= 1;

            if (analyse.EmaSignal == TrendRichtung.Abwaerts) sellScore += 2;

            if (analyse.IstTrendStark && analyse.DiMinus > analyse.DiPlus)
            {
                sellScore += 2;
                if (analyse.Trendstaerke > 30) sellScore += 1;
            }

            // RSI adaptiv für Sell (im Abwärtstrend: Bounces = Verkaufschance)
            double sellOverbought = analyse.EmaSignal == TrendRichtung.Abwaerts ? 60 : 70;
            double sellOversold = analyse.EmaSignal == TrendRichtung.Abwaerts ? 20 : 30;
            if (analyse.RsiWert > sellOverbought) sellScore += 2;
            else if (analyse.RsiWert > 50 && analyse.RsiWert < sellOverbought) sellScore += 1;
            if (analyse.RsiWert < sellOversold) sellScore -= 3;

            if (analyse.RsiBearishDivergenz) sellScore += 3;

            if (analyse.MacdBearishCross) sellScore += 2;
            if (analyse.MacdHistogramm < 0 && analyse.MacdMomentumSteigt) sellScore += 1;

            if (analyse.PreisNahOberemBand) sellScore += 1;
            if (analyse.BollingerSqueeze && analyse.EmaSignal == TrendRichtung.Abwaerts) sellScore += 2;

            if (analyse.BearishEngulfing) sellScore += 3;
            if (analyse.BearishPinBar) sellScore += 2;
            if (analyse.StarkeMomentumKerze && analyse.IstBearishKerze) sellScore += 2;
            else if (analyse.IstBearishKerze && analyse.KerzenKoerper > analyse.ObererDocht) sellScore += 1;

            if (analyse.Regime == MarktRegime.StarkerTrend && analyse.EmaSignal == TrendRichtung.Abwaerts)
                sellScore += 1;

            if (analyse.DreiBarsAbwaerts) sellScore += 1;
            if (analyse.CloseImUnterenDrittel) sellScore += 1;
            if (analyse.EmaPullbackBounceSell) sellScore += 2;
            if (analyse.AtrExpandiert) sellScore += 1;
            if (analyse.VolleKonfluenzSell) sellScore += 2;

            // --- SESSION-QUALITÄTS-BONUS (v3.3: granular statt flat +1) ---
            if (_sessionQualitaet >= 0.9)
            {
                buyScore += 2;  // Overlap/London-Kern: +2
                sellScore += 2;
            }
            else if (_sessionQualitaet >= 0.75)
            {
                buyScore += 1;  // London Open/NY: +1
                sellScore += 1;
            }
            // Unter 0.75: Kein Bonus (schlechtere Sessions müssen über Signal-Qualität kompensieren)

            // --- RICHTUNGS-BIAS: Lernt aus letzten Ergebnissen ---
            // Wenn Buys in letzter Zeit gut laufen -> Buy-Bonus
            int recentBuyTotal = _recentBuyWins + _recentBuyLosses;
            int recentSellTotal = _recentSellWins + _recentSellLosses;
            if (recentBuyTotal >= 3)
            {
                double buyWR = (double)_recentBuyWins / recentBuyTotal;
                if (buyWR >= 0.7) buyScore += 1;      // Buys laufen gut
                else if (buyWR <= 0.3) buyScore -= 1;  // Buys versagen
            }
            if (recentSellTotal >= 3)
            {
                double sellWR = (double)_recentSellWins / recentSellTotal;
                if (sellWR >= 0.7) sellScore += 1;
                else if (sellWR <= 0.3) sellScore -= 1;
            }

            // --- VOLATILITÄTS-EXPANSION-BONUS ---
            // Expandierende Volatilität = Markt bewegt sich = bessere Trade-Chance
            if (_volatilitaetsRatio > 1.3)
            {
                buyScore += 1;
                sellScore += 1;
            }

            // --- ENTSCHEIDUNG ---
            // v3: Flexibler HTF-Filter + Squeeze-Breakout in Konsolidierung

            // Regime-adaptive Schwellenwerte (v3.2: +1 überall - weniger Trades, bessere Qualität)
            int minScore;
            switch (analyse.Regime)
            {
                case MarktRegime.StarkerTrend: minScore = 7; break;   // vorher 6
                case MarktRegime.MittlererTrend: minScore = 7; break;  // vorher 6
                case MarktRegime.Konsolidierung:
                    // Konsolidierung: NUR Bollinger-Squeeze-Breakouts erlaubt
                    if (!analyse.BollingerSqueeze)
                        return;
                    minScore = 9; // vorher 8 - Muss absolut überzeugend sein
                    break;
                default: minScore = 8; break; // SchwacherTrend (vorher 7)
            }

            // HTF-Filter: Flexibel statt binärer Block
            // Mit HTF: +0 | Seitwärts: +1 | Gegen HTF: gesperrt
            int htfBuyAufschlag = 0;
            int htfSellAufschlag = 0;

            if (analyse.HtfTrend == TrendRichtung.Seitwaerts)
            {
                htfBuyAufschlag = 1;
                htfSellAufschlag = 1;
            }
            else if (analyse.HtfTrend == TrendRichtung.Aufwaerts)
            {
                htfSellAufschlag = 99; // Sell gegen HTF = gesperrt
            }
            else // Abwaerts
            {
                htfBuyAufschlag = 99; // Buy gegen HTF = gesperrt
            }

            int buyMinScore = minScore + htfBuyAufschlag;
            int sellMinScore = minScore + htfSellAufschlag;

            // Score-Differenz: Richtung muss klar sein (v3.2: diff 3 statt 2)
            if (buyScore >= buyMinScore && buyScore > sellScore + 3)
            {
                analyse.Signal = SignalTyp.Buy;
                analyse.SignalScore = buyScore;
                Print("BUY Score:{0}/{1} (Sell:{2}) | HTF:{3} | RSI:{4:F0} | ADX:{5:F0} | Regime:{6}",
                    buyScore, buyMinScore, sellScore, analyse.HtfTrend, analyse.RsiWert, analyse.Trendstaerke, analyse.Regime);
            }
            else if (sellScore >= sellMinScore && sellScore > buyScore + 3)
            {
                analyse.Signal = SignalTyp.Sell;
                analyse.SignalScore = sellScore;
                Print("SELL Score:{0}/{1} (Buy:{2}) | HTF:{3} | RSI:{4:F0} | ADX:{5:F0} | Regime:{6}",
                    sellScore, sellMinScore, buyScore, analyse.HtfTrend, analyse.RsiWert, analyse.Trendstaerke, analyse.Regime);
            }
        }

        // =====================================================================
        // TRADE AUSFÜHREN (VERBESSERT)
        // =====================================================================

        private void FuehreTradeAus(MarktAnalyse analyse, bool istPyramide)
        {
            var offene = Positions.FindAll(BotLabel, _marktSymbol.Name);
            if (offene.Length >= _maxOpenPositions)
                return;

            // Cooldown prüfen (kürzer bei Pyramide)
            double cooldownMinuten = _signalCooldown * TimeframeZuMinuten(BotTimeframe);
            if (istPyramide) cooldownMinuten *= 0.5;
            if ((Server.Time - _lastTradeTime).TotalMinutes < cooldownMinuten)
                return;

            // ATR-basierte Level mit Vol-Ratio-Anpassung
            double atr = analyse.AtrWert;

            // Volatilitäts-adaptive SL/TP-Multiplikatoren
            // Expandierende Vol (>1.2): Breiterer SL (Rauschen), höherer TP (größere Moves)
            // Kontrahierende Vol (<0.8): Engerer SL, kleinerer TP
            double volAnpassungSL = 1.0;
            double volAnpassungTP = 1.0;
            if (_volatilitaetsRatio > 1.3)
            {
                volAnpassungSL = 1.15;  // 15% breiterer SL bei Vol-Expansion
                volAnpassungTP = 1.25;  // 25% größeres TP (größere Moves erwartet)
            }
            else if (_volatilitaetsRatio < 0.75)
            {
                volAnpassungSL = 0.85;  // 15% engerer SL bei Vol-Kontraktion
                volAnpassungTP = 0.85;  // 15% kleineres TP
            }

            double stopLossPips = AtrZuPips(atr * _atrMultiplierSL * volAnpassungSL);
            double takeProfitPips = AtrZuPips(atr * _atrMultiplierTP * volAnpassungTP);

            // Dynamische TP-Extension: Regime-, Score- und Vol-basiert
            double tpMultiplier = 1.0;
            if (analyse.Regime == MarktRegime.StarkerTrend)
                tpMultiplier = 1.8;
            else if (analyse.Regime == MarktRegime.MittlererTrend)
                tpMultiplier = 1.3;

            // A+ Setups bekommen mindestens 1.4x TP
            if (analyse.SignalScore >= 10)
                tpMultiplier = Math.Max(tpMultiplier, 1.4);

            if (tpMultiplier > 1.0)
            {
                takeProfitPips *= tpMultiplier;
            }

            Print("SL:{0:F1} TP:{1:F1} | VolRatio:{2:F2} | SL-Adj:{3:F2} TP-Adj:{4:F2} | TPx:{5:F1}",
                stopLossPips, takeProfitPips, _volatilitaetsRatio,
                volAnpassungSL, volAnpassungTP, tpMultiplier);

            // Minimale Distanz
            double minPips = _marktSymbol.Spread * 3;
            stopLossPips = Math.Max(stopLossPips, minPips);
            takeProfitPips = Math.Max(takeProfitPips, minPips);

            // Min R:R Enforcement: Trade ablehnen wenn Risk/Reward zu schlecht
            double riskReward = takeProfitPips / stopLossPips;
            if (riskReward < _minRiskReward)
            {
                Print("R:R ABGELEHNT: {0:F2}:1 < min {1:F1}:1 (SL:{2:F1} TP:{3:F1})",
                    riskReward, _minRiskReward, stopLossPips, takeProfitPips);
                return;
            }

            // Positionsgröße berechnen (Kelly-basiert + adaptive Anpassung)
            double riskPercent = BerechneAdaptivesRisiko(analyse);
            double positionsGroesse = BerechnePositionsGroesse(stopLossPips, riskPercent);

            if (positionsGroesse < _marktSymbol.VolumeInUnitsMin)
                return;

            // Pyramide: halbe Größe (kontrolliert nachlegen)
            if (istPyramide)
                positionsGroesse *= 0.5;

            positionsGroesse = _marktSymbol.NormalizeVolumeInUnits(positionsGroesse, RoundingMode.Down);
            if (positionsGroesse < _marktSymbol.VolumeInUnitsMin)
                return;

            TradeType richtung = analyse.Signal == SignalTyp.Buy ? TradeType.Buy : TradeType.Sell;

            var result = ExecuteMarketOrder(
                richtung, _marktSymbol.Name, positionsGroesse,
                BotLabel, stopLossPips, takeProfitPips);

            if (result.IsSuccessful)
            {
                _lastTradeTime = Server.Time;
                string typ = istPyramide ? "PYRAMIDE" : "NEU";
                Print("{0} Trade: {1} {2:F0} Einheiten | SL:{3:F1} TP:{4:F1} Pips | Risiko:{5:F2}%",
                    typ, richtung, positionsGroesse, stopLossPips, takeProfitPips, riskPercent);
            }
        }

        // =====================================================================
        // POSITIONSGRÖ?E: KELLY-CRITERION + ADAPTIVES RISIKO
        // =====================================================================

        private double BerechneAdaptivesRisiko(MarktAnalyse analyse)
        {
            double risk = _baseRiskPercent;

            // === WARM-UP PHASE: Erste 10 Trades nur 50% Risiko ===
            // Verhindert massive Verluste am Start wenn noch keine Daten vorhanden
            if (_totalTradeCount < WarmUpTrades)
            {
                risk *= 0.5;
                Print("WARM-UP: Trade {0}/{1} - halbes Risiko ({2:F2}%)",
                    _totalTradeCount + 1, WarmUpTrades, risk);
            }

            // Kelly-Criterion wenn genug Daten vorhanden (min 20 Trades)
            if (_totalWins + _totalLosses >= 20)
            {
                double kellyRisk = BerechneKellyRisiko();
                // Halbes Kelly als Sicherheitsnetz
                risk = Math.Min(risk * 1.5, kellyRisk);
            }

            // === ADAPTIVE RISIKO-SKALIERUNG (v3.2: Gedämpft) ===

            // 1. Rolling Expectancy: Statt nur Streak, gesamte letzte Performance
            if (_rollendeErgebnissePips.Count >= 5)
            {
                if (_rollendeErwartung > 5.0)
                    risk *= 1.2;   // Bot läuft gut: etwas mehr (vorher 1.3)
                else if (_rollendeErwartung > 0)
                    risk *= 1.05;  // Leicht positiv: minimal (vorher 1.1)
                else if (_rollendeErwartung < -5.0)
                    risk *= 0.5;   // Bot verliert: stark bremsen
                else if (_rollendeErwartung < 0)
                    risk *= 0.7;   // Leicht negativ: etwas bremsen
            }

            // 2. Consecutive als Zusatz-Sicherung (Streak-Breaker)
            if (_consecutiveWins >= 4)
                risk *= 1.1;   // Heißer Lauf (vorher 1.2)
            if (_consecutiveLosses >= 3)
                risk *= 0.4;   // Kalter Lauf - sofort bremsen
            else if (_consecutiveLosses >= 2)
                risk *= 0.65;  // Warnung

            // 3. Rolling WinRate Anpassung
            if (_rollendeErgebnissePips.Count >= 5)
            {
                if (_rollendeWinRate > 0.7)
                    risk *= 1.1;  // Hohe Trefferquote (vorher 1.15)
                else if (_rollendeWinRate < 0.35)
                    risk *= 0.6;  // Niedrige Trefferquote
            }

            // 4. Drawdown-Skalierung: ab 50% des Max-DD bremsen
            _currentDrawdown = _peakBalance > 0 ? ((_peakBalance - Account.Balance) / _peakBalance) * 100 : 0;
            if (_currentDrawdown > _maxDrawdownPercent * 0.5)
            {
                double ddFaktor = 1.0 - ((_currentDrawdown - _maxDrawdownPercent * 0.5) / (_maxDrawdownPercent * 0.5));
                risk *= Math.Max(0.2, ddFaktor);
            }

            // 5. Signal-Score-Bonus: Gedämpft - max 1.5x statt 2.0x
            if (analyse.SignalScore >= 12) risk *= 1.5;
            else if (analyse.SignalScore >= 10) risk *= 1.3;
            else if (analyse.SignalScore >= 8) risk *= 1.15;

            // 6. ADX-Trendstärke-Gewichtung
            if (analyse.Trendstaerke > 35)
                risk *= 1.1;   // Vorher 1.15
            else if (analyse.Trendstaerke < 20)
                risk *= 0.85;

            // 7. Regime-Anpassung
            if (analyse.Regime == MarktRegime.StarkerTrend) risk *= 1.15; // Vorher 1.2

            // 8. Volatilitäts-Regime: Bei extremer Vol runterfahren (Spikes = gefährlich)
            if (_volatilitaetsRatio > 2.0)
                risk *= 0.7; // Extreme Expansion: Vorsicht
            else if (_volatilitaetsRatio < 0.5)
                risk *= 0.8; // Extreme Kontraktion: Wenig Bewegung erwartet

            // 9. Session-Qualität: Außerhalb der Kernzeiten weniger riskieren
            if (_sessionQualitaet < 0.75)
                risk *= 0.7;  // Schlechte Session: 30% weniger Risiko
            else if (_sessionQualitaet < 0.9)
                risk *= 0.85; // Mittlere Session: 15% weniger

            // 10. Tages-P&L: Nach gutem Tag konservativer (Gewinne schützen)
            if (_dailyProfitPercent > _dailyLossLimitPercent * 1.5)
                risk *= 0.7; // Sehr guter Tag: Gewinne sichern
            else if (_dailyProfitPercent < -_dailyLossLimitPercent * 0.5)
                risk *= 0.75; // Halbes Tageslimit verloren: bremsen

            // Harte Grenzen: max 2.0x Basis
            return Math.Max(0.15, Math.Min(risk, _baseRiskPercent * 2.0));
        }

        private double BerechneKellyRisiko()
        {
            double wr = WinRate();
            double avgWin = _totalWins > 0 ? _summeGewinne / _totalWins : 1;
            double avgLoss = _totalLosses > 0 ? _summeVerluste / _totalLosses : 1;

            if (avgLoss <= 0) return _baseRiskPercent;

            double payoffRatio = avgWin / avgLoss;

            // Kelly-Formel: f = W - (1-W)/R
            double kelly = wr - ((1.0 - wr) / payoffRatio);

            // Halbes Kelly (bewährt sicher), minimal 0.15%
            return Math.Max(0.15, kelly * 100.0 * 0.5);
        }

        private double BerechnePositionsGroesse(double stopLossPips, double riskPercent)
        {
            double risikoBetrag = Account.Balance * (riskPercent / 100.0);
            double pipValue = _marktSymbol.PipValue;

            if (pipValue <= 0 || stopLossPips <= 0)
                return _marktSymbol.VolumeInUnitsMin;

            return risikoBetrag / (stopLossPips * pipValue);
        }

        // =====================================================================
        // POSITIONS-VERWALTUNG (MASSIV VERBESSERT)
        // =====================================================================

        private void VerwalteBestehendePositionen()
        {
            var positionen = Positions.FindAll(BotLabel, _marktSymbol.Name);
            double atr = _atr.Result.Last(1);

            foreach (var position in positionen)
            {
                double atrPips = AtrZuPips(atr);
                double slPips = AtrZuPips(atr * _atrMultiplierSL);

                // 1. BREAK-EVEN: bei 1.5R (nicht früher - Markt braucht Raum zum Atmen)
                if (position.Pips > slPips * 1.5 && !IstBreakEven(position))
                {
                    SetzeBreakEven(position);
                }

                // 2a. PARTIAL CLOSE #1: 40% bei 2.5R sichern
                if (position.Pips > slPips * 2.5
                    && position.VolumeInUnits > _marktSymbol.VolumeInUnitsMin * 2
                    && !_partialClosedPositions.Contains(position.Id))
                {
                    double closeVolume = _marktSymbol.NormalizeVolumeInUnits(
                        position.VolumeInUnits * 0.40, RoundingMode.Down);
                    if (closeVolume >= _marktSymbol.VolumeInUnitsMin)
                    {
                        ClosePosition(position, closeVolume);
                        _partialClosedPositions.Add(position.Id);
                        Print("PARTIAL #1: 40% bei +{0:F1} Pips (2.5R)", position.Pips);
                    }
                }

                // 2b. PARTIAL CLOSE #2: Weitere 30% bei 4R - Großteil der Gewinne sichern
                if (position.Pips > slPips * 4.0
                    && position.VolumeInUnits > _marktSymbol.VolumeInUnitsMin * 2
                    && _partialClosedPositions.Contains(position.Id)
                    && !_secondPartialClosedPositions.Contains(position.Id))
                {
                    double closeVolume = _marktSymbol.NormalizeVolumeInUnits(
                        position.VolumeInUnits * 0.50, RoundingMode.Down);
                    if (closeVolume >= _marktSymbol.VolumeInUnitsMin)
                    {
                        ClosePosition(position, closeVolume);
                        _secondPartialClosedPositions.Add(position.Id);
                        Print("PARTIAL #2: 50% bei +{0:F1} Pips (4R) - Rest läuft weiter", position.Pips);
                    }
                }

                // 2c. EARLY-EXIT bei Schwäche: Position in leichtem Profit aber Momentum kippt
                if (position.Pips > slPips * 0.5 && position.Pips < slPips * 1.2
                    && !_partialClosedPositions.Contains(position.Id))
                {
                    int gegenSignale = 0;
                    double rsi = _rsi.Result.Last(1);

                    // RSI dreht gegen Position
                    if (position.TradeType == TradeType.Buy && rsi > 70) gegenSignale++;
                    if (position.TradeType == TradeType.Sell && rsi < 30) gegenSignale++;

                    // MACD dreht gegen Position
                    if (position.TradeType == TradeType.Buy
                        && _macd.Histogram.Last(1) < 0 && _macd.Histogram.Last(2) > 0) gegenSignale++;
                    if (position.TradeType == TradeType.Sell
                        && _macd.Histogram.Last(1) > 0 && _macd.Histogram.Last(2) < 0) gegenSignale++;

                    // EMA-Kreuzung gegen Position
                    double fast = _emaFast.Result.Last(1);
                    double med = _emaMedium.Result.Last(1);
                    if (position.TradeType == TradeType.Buy && fast < med) gegenSignale++;
                    if (position.TradeType == TradeType.Sell && fast > med) gegenSignale++;

                    // Bei 3 Gegen-Signalen: Lieber mit kleinem Gewinn raus als Verlust riskieren
                    if (gegenSignale >= 3)
                    {
                        Print("EARLY-EXIT: {0} Gegen-Signale bei +{1:F1} Pips - sichere Gewinn",
                            gegenSignale, position.Pips);
                        ClosePosition(position);
                        continue;
                    }
                }

                // 3. PROGRESSIVER TRAILING STOP: Enger je höher der Gewinn
                double trailingStart = slPips * 2.0;
                if (position.Pips > trailingStart)
                {
                    double profitR = position.Pips / slPips;

                    // Trailing-Distanz verkürzt sich mit steigendem Gewinn
                    double trailingFaktor;
                    if (profitR > 5.0)
                        trailingFaktor = 0.6;  // 5R+: eng, Gewinn sichern
                    else if (profitR > 3.5)
                        trailingFaktor = 0.8;  // 3.5-5R: mittel
                    else
                        trailingFaktor = 1.0;  // 2-3.5R: normal

                    double trailingDistanz = AtrZuPips(atr * _trailingAtrMultiplier * trailingFaktor);

                    // Im starken Trend: etwas mehr Raum lassen
                    if (_aktuellesRegime == MarktRegime.StarkerTrend)
                        trailingDistanz *= 1.3;

                    // Vol-Expansion: Breiterer Trail (mehr Noise)
                    if (_volatilitaetsRatio > 1.3)
                        trailingDistanz *= 1.15;
                    else if (_volatilitaetsRatio < 0.75)
                        trailingDistanz *= 0.85;

                    double neuerSL;
                    if (position.TradeType == TradeType.Buy)
                    {
                        neuerSL = _marktSymbol.Bid - (trailingDistanz * _marktSymbol.PipSize);
                        if (position.StopLoss == null || neuerSL > position.StopLoss)
                            position.ModifyStopLossPrice(neuerSL);
                    }
                    else
                    {
                        neuerSL = _marktSymbol.Ask + (trailingDistanz * _marktSymbol.PipSize);
                        if (position.StopLoss == null || neuerSL < position.StopLoss)
                            position.ModifyStopLossPrice(neuerSL);
                    }
                }
            }
        }

        private bool IstBreakEven(Position position)
        {
            if (position.StopLoss == null) return false;
            double diff = Math.Abs(position.EntryPrice - position.StopLoss.Value);
            return diff < _marktSymbol.PipSize * 3; // Innerhalb von 3 Pips = Break-Even
        }

        private void SetzeBreakEven(Position position)
        {
            // SL auf Entry + 1 Pip setzen (leichter Gewinn garantiert)
            double bePriceOffset = _marktSymbol.PipSize * 1;
            double bePrice;

            if (position.TradeType == TradeType.Buy)
                bePrice = position.EntryPrice + bePriceOffset;
            else
                bePrice = position.EntryPrice - bePriceOffset;

            if (position.StopLoss == null ||
                (position.TradeType == TradeType.Buy && bePrice > position.StopLoss) ||
                (position.TradeType == TradeType.Sell && bePrice < position.StopLoss))
            {
                position.ModifyStopLossPrice(bePrice);
                Print("BREAK-EVEN gesetzt bei +{0:F1} Pips", position.Pips);
            }
        }

        // =====================================================================
        // STALE-TRADE-TIMEOUT & AKTIVER REVERSAL-EXIT
        // =====================================================================

        private void PruefeStaleTradesUndReversals()
        {
            var positionen = Positions.FindAll(BotLabel, _marktSymbol.Name);

            foreach (var position in positionen)
            {
                // Wie viele Bars ist die Position schon offen?
                int barsOffen = (int)((Server.Time - position.EntryTime).TotalMinutes / TimeframeZuMinuten(BotTimeframe));

                // STALE TRADE: Position geht nirgendwohin
                double atrPipsStale = AtrZuPips(_atr.Result.Last(1));
                // Flach: kaum Bewegung nach vielen Bars
                if (barsOffen >= _staleTradeBarCount && Math.Abs(position.Pips) < atrPipsStale * 0.3)
                {
                    Print("STALE TRADE geschlossen nach {0} Bars bei {1:F1} Pips (flach)", barsOffen, position.Pips);
                    ClosePosition(position);
                    continue;
                }
                // Lange im Minus: Nach 1.5x Stale-Count und immer noch negativ → Kapital freigeben
                if (barsOffen >= (int)(_staleTradeBarCount * 1.5) && position.Pips < 0 && position.Pips > -atrPipsStale)
                {
                    Print("STALE-LOSS geschlossen nach {0} Bars bei {1:F1} Pips (kleiner Verlust)", barsOffen, position.Pips);
                    ClosePosition(position);
                    continue;
                }

                // REVERSAL-EXIT: Nur bei starken Umkehrsignalen + genug Gewinn
                // Zähle wie viele Reversal-Indikatoren gleichzeitig feuern
                int reversalZaehler = 0;
                double atrPipsReversal = AtrZuPips(_atr.Result.Last(1));
                double rsi = _rsi.Result.Last(1);

                // RSI extrem gegen Position (nur bei wirklich extremen Werten)
                if (position.TradeType == TradeType.Buy && rsi > 85)
                    reversalZaehler++;
                if (position.TradeType == TradeType.Sell && rsi < 15)
                    reversalZaehler++;

                // EMA-Kreuzung gegen Position (nur mit Mindestgewinn von 1.5R)
                double fast = _emaFast.Result.Last(1);
                double medium = _emaMedium.Result.Last(1);
                double fastVorher = _emaFast.Result.Last(2);
                double mediumVorher = _emaMedium.Result.Last(2);

                bool emaBearishCross = fastVorher >= mediumVorher && fast < medium;
                bool emaBullishCross = fastVorher <= mediumVorher && fast > medium;

                if (position.TradeType == TradeType.Buy && emaBearishCross && position.Pips > atrPipsReversal * 1.5)
                    reversalZaehler++;
                if (position.TradeType == TradeType.Sell && emaBullishCross && position.Pips > atrPipsReversal * 1.5)
                    reversalZaehler++;

                // MACD Umkehr gegen Position (nur mit deutlichem Gewinn von 2R)
                if (position.TradeType == TradeType.Buy && _macd.Histogram.Last(1) < 0
                    && _macd.Histogram.Last(2) > 0 && position.Pips > atrPipsReversal * 2.0)
                {
                    reversalZaehler++;
                }
                if (position.TradeType == TradeType.Sell && _macd.Histogram.Last(1) > 0
                    && _macd.Histogram.Last(2) < 0 && position.Pips > atrPipsReversal * 2.0)
                {
                    reversalZaehler++;
                }

                // Nur schließen wenn mindestens 2 Reversal-Indikatoren gleichzeitig feuern
                if (reversalZaehler >= 2)
                {
                    Print("REVERSAL-EXIT ({0} Signale) bei {1:F1} Pips (RSI:{2:F0})",
                        reversalZaehler, position.Pips, rsi);
                    ClosePosition(position);
                }
            }
        }

        // =====================================================================
        // EREIGNIS-ERKENNUNG: GAP, SPREAD-SPIKE
        // =====================================================================

        private bool ErkenneGap()
        {
            if (_marktBars.ClosePrices.Count < 3)
                return false;

            double vorherigerClose = _marktBars.ClosePrices.Last(2);
            double aktuellerOpen = _marktBars.OpenPrices.Last(1);
            double atr = _atr.Result.Last(2);

            if (atr <= 0) return false;

            double gapGroesse = Math.Abs(aktuellerOpen - vorherigerClose);
            return gapGroesse > atr * 2.0;
        }

        private void AktualisiereSpreadTracking()
        {
            double spread = _marktSymbol.Spread;
            _spreadSampleCount++;
            _avgSpread = _avgSpread + (spread - _avgSpread) / Math.Min(_spreadSampleCount, 100);
            _lastSpread = spread;
        }

        private bool IstSpreadZuHoch()
        {
            if (_spreadSampleCount < 10) return false;
            return _marktSymbol.Spread > _avgSpread * 3.0;
        }

        // =====================================================================
        // SICHERHEITSCHECKS
        // =====================================================================

        private bool DarfHandeln()
        {
            // Daily Reset: Neuer Tag = neue Zähler
            if (Server.Time.Date != _dailyResetDate)
            {
                double tagesPnl = (Account.Balance - _dailyStartBalance) / _dailyStartBalance * 100;
                if (_dailyTradeCount > 0)
                    Print("TAGES-ABSCHLUSS: PnL {0:+0.00;-0.00}% | Trades: {1}", tagesPnl, _dailyTradeCount);
                _dailyStartBalance = Account.Balance;
                _dailyResetDate = Server.Time.Date;
                _dailyTradeCount = 0;
            }

            // Daily P&L berechnen
            _dailyProfitPercent = _dailyStartBalance > 0
                ? (Account.Balance - _dailyStartBalance) / _dailyStartBalance * 100 : 0;

            _currentDrawdown = _peakBalance > 0 ? ((_peakBalance - Account.Balance) / _peakBalance) * 100 : 0;
            if (_currentDrawdown >= _maxDrawdownPercent)
            {
                Print("MAX DRAWDOWN {0:F1}% erreicht - STOP!", _currentDrawdown);
                return false;
            }

            // Daily Loss Limit: Heute genug verloren → Pause bis morgen
            if (_dailyProfitPercent <= -_dailyLossLimitPercent)
            {
                Print("DAILY LOSS LIMIT: {0:F2}% heute verloren (Limit: {1:F1}%) - Pause bis morgen",
                    _dailyProfitPercent, _dailyLossLimitPercent);
                return false;
            }

            // Übertrading-Schutz: Max Trades pro Tag
            if (_dailyTradeCount >= MaxDailyTrades)
            {
                Print("MAX DAILY TRADES: {0} Trades heute - genug für heute", _dailyTradeCount);
                return false;
            }

            if (!IstAktiveHandelszeit())
                return false;

            if (Server.Time.DayOfWeek == DayOfWeek.Saturday || Server.Time.DayOfWeek == DayOfWeek.Sunday)
                return false;

            // Freitag ab 18 Uhr: Keine neuen Trades (Gap-Risiko + abnehmende Liquidität)
            if (Server.Time.DayOfWeek == DayOfWeek.Friday && Server.Time.Hour >= 18)
                return false;

            // Montag-Morgen: Erste 2 Stunden = choppy nach Weekend → kein Neueinstieg
            if (Server.Time.DayOfWeek == DayOfWeek.Monday && Server.Time.Hour < 10)
                return false;

            return true;
        }

        private bool IstAktiveHandelszeit()
        {
            int stunde = Server.Time.Hour;
            // Nur Hauptsessions: London + NY (beste Liquidität)
            return stunde >= 8 && stunde <= 20;
        }

        private double BerechneSessionQualitaet()
        {
            int stunde = Server.Time.Hour;

            // London/NY Overlap (13-16 UTC): Beste Liquidität = bestes Trading
            if (stunde >= 13 && stunde <= 16)
                return 1.0;

            // London Kern (9-12 UTC): Gute Liquidität
            if (stunde >= 9 && stunde <= 12)
                return 0.9;

            // NY Kern (14-17 UTC) - teilweise Overlap
            if (stunde >= 14 && stunde <= 17)
                return 0.85;

            // London Open (8 UTC): Volatiler, aber Breakout-Chancen
            if (stunde == 8)
                return 0.75;

            // NY Nachmittag (18-20 UTC): Abnehmende Liquidität
            if (stunde >= 18 && stunde <= 20)
                return 0.6;

            // Alles andere (sollte durch IstAktiveHandelszeit gefiltert werden)
            return 0.5;
        }

        // =====================================================================
        // CLEANUP
        // =====================================================================

        protected override void OnStop()
        {
            int total = _totalWins + _totalLosses;
            Print("=== FullAutoBot v3.3 gestoppt ===");
            Print("Trades: {0} | Wins: {1} | Losses: {2} | WR: {3:F1}%",
                total, _totalWins, _totalLosses, WinRate() * 100);
            if (_totalWins > 0 && _totalLosses > 0)
            {
                double avgW = _summeGewinne / _totalWins;
                double avgL = _summeVerluste / _totalLosses;
                Print("Avg Win: +{0:F1} Pips | Avg Loss: -{1:F1} Pips | Payoff: {2:F2}",
                    avgW, avgL, avgW / Math.Max(avgL, 0.01));
            }
            Print("Balance: {0:F2} | Peak: {1:F2} | Drawdown: {2:F1}%",
                Account.Balance, _peakBalance, _currentDrawdown);
            Print("Rolling Exp: {0:F1} | Rolling WR: {1:F0}% | Vol-Ratio: {2:F2}",
                _rollendeErwartung, _rollendeWinRate * 100, _volatilitaetsRatio);
            Print("Buy WR: {0}/{1} | Sell WR: {2}/{3}",
                _recentBuyWins, _recentBuyWins + _recentBuyLosses,
                _recentSellWins, _recentSellWins + _recentSellLosses);
            Print("Heute: PnL {0:+0.00;-0.00}% | Trades: {1} | Session-Q: {2:F2}",
                _dailyProfitPercent, _dailyTradeCount, _sessionQualitaet);

            _marktBars.BarOpened -= OnBarOpened;
            Positions.Closed -= OnPositionClosed;
        }

        // =====================================================================
        // ADAPTIVE METHODEN
        // =====================================================================

        private void AktualisiereVolatilitaetsRegime()
        {
            double atrKurz = _atrKurz.Result.Last(1);
            double atrLang = _atr.Result.Last(1);

            if (atrLang > 0)
                _volatilitaetsRatio = atrKurz / atrLang;
            else
                _volatilitaetsRatio = 1.0;

            // Spread-Volatilität tracken (wie stark schwankt der Spread)
            double spreadDiff = Math.Abs(_avgSpread - _vorherigerAvgSpread);
            _spreadVolatilitaet = _spreadVolatilitaet * 0.9 + spreadDiff * 0.1;
            _vorherigerAvgSpread = _avgSpread;
        }

        private void AktualisiereRollendeErwartung()
        {
            if (_rollendeErgebnissePips.Count == 0)
            {
                _rollendeErwartung = 0;
                _rollendeWinRate = 0.5;
                return;
            }

            _rollendeErwartung = _rollendeErgebnissePips.Average();
            _rollendeWinRate = (double)_rollendeErfolge.Count(x => x) / _rollendeErfolge.Count;
        }

        // =====================================================================
        // HILFSFUNKTIONEN
        // =====================================================================

        private double WinRate()
        {
            int total = _totalWins + _totalLosses;
            return total > 0 ? (double)_totalWins / total : 0.5;
        }

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
            return 60;
        }

        // =====================================================================
        // DATENSTRUKTUREN
        // =====================================================================

        private enum TrendRichtung { Aufwaerts, Abwaerts, Seitwaerts }
        private enum SignalTyp { Kein, Buy, Sell }
        private enum MarktRegime { Unbekannt, StarkerTrend, MittlererTrend, SchwacherTrend, Konsolidierung }

        private class MarktAnalyse
        {
            // Trend
            public TrendRichtung HtfTrend { get; set; }
            public TrendRichtung EmaSignal { get; set; }
            public bool UeberEma200 { get; set; }
            public double Trendstaerke { get; set; }
            public bool IstTrendStark { get; set; }
            public double DiPlus { get; set; }
            public double DiMinus { get; set; }

            // RSI
            public double RsiWert { get; set; }
            public double RsiVorher { get; set; }
            public bool RsiUeberkauft { get; set; }
            public bool RsiUeberverkauft { get; set; }
            public bool RsiBullishDivergenz { get; set; }
            public bool RsiBearishDivergenz { get; set; }

            // MACD
            public double MacdHistogramm { get; set; }
            public double MacdHistogrammVorher { get; set; }
            public double MacdHistogrammVorVorher { get; set; }
            public bool MacdBullishCross { get; set; }
            public bool MacdBearishCross { get; set; }
            public bool MacdMomentumSteigt { get; set; }

            // Bollinger Bands
            public bool PreisNahOberemBand { get; set; }
            public bool PreisNahUnteremBand { get; set; }
            public double BollingerBreite { get; set; }
            public bool BollingerSqueeze { get; set; }

            // Volatilität
            public double AtrWert { get; set; }
            public double AtrProzent { get; set; }
            public bool VolatilitaetOk { get; set; }

            // Kerzenformationen
            public bool IstBullishKerze { get; set; }
            public bool IstBearishKerze { get; set; }
            public double KerzenKoerper { get; set; }
            public double ObererDocht { get; set; }
            public double UntererDocht { get; set; }
            public bool BullishEngulfing { get; set; }
            public bool BearishEngulfing { get; set; }
            public bool BullishPinBar { get; set; }
            public bool BearishPinBar { get; set; }
            public bool StarkeMomentumKerze { get; set; }

            // Momentum & Konfluenz
            public bool DreiBarsAufwaerts { get; set; }
            public bool DreiBarsAbwaerts { get; set; }
            public bool CloseImOberenDrittel { get; set; }
            public bool CloseImUnterenDrittel { get; set; }
            public bool EmaPullbackBounceBuy { get; set; }
            public bool EmaPullbackBounceSell { get; set; }
            public bool AtrExpandiert { get; set; }
            public bool VolleKonfluenzBuy { get; set; }
            public bool VolleKonfluenzSell { get; set; }

            // Regime + Signal
            public MarktRegime Regime { get; set; }
            public SignalTyp Signal { get; set; }
            public int SignalScore { get; set; }
        }
    }
}
