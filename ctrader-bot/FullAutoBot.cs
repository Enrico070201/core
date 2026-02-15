// ============================================================================
// FullAutoBot v4.3 - Vollautomatischer cTrader Trading Bot
// ============================================================================
// 10 Parameter für volle Kontrolle - Rest wird automatisch berechnet.
//
// PARAMETER:
//   1. Timeframe          - Chart-Zeitrahmen (M1 bis Monthly)
//   2. Markt (Symbol)     - Handelsinstrument (EURUSD, GBPJPY, etc.)
//   3. Strategie-Modus    - 1=Konservativ, 2=Normal, 3=Aggressiv
//   4. Basis-Risiko %     - Risiko pro Trade (0=Auto)
//   5. Max Drawdown %     - Maximaler Drawdown bis Stop (0=Auto)
//   6. Max Tagesverlust % - Tägliches Verlustlimit (0=Auto)
//   7. Max Positionen     - Gleichzeitig offene Trades (0=Auto)
//   8. Max Trades/Tag     - Übertrading-Schutz (0=Auto)
//   9. Session Start UTC  - Beginn der Handelszeit (-1=Auto: 8 UTC)
//  10. Session Ende UTC   - Ende der Handelszeit (-1=Auto: 20 UTC)
//
// STRATEGIE-MODUS:
//   Konservativ: Score+2, Cooldown x1.5, Risiko x0.7, R:R min 2.5, max 1 Trade/Richtung
//   Normal:      Standard-Werte (ausgewogen)
//   Aggressiv:   Score-1, Cooldown x0.7, Risiko x1.3, R:R min 1.5, max 3 Trades/Richtung
//
// v4.3 Benutzer-Parameter:
//   - Von 2 auf 10 konfigurierbare Parameter erweitert
//   - Strategie-Modus (Konservativ/Normal/Aggressiv) steuert Selektivität
//   - Risiko, Drawdown, Tagesverlust individuell einstellbar
//   - Max Positionen und Trades/Tag konfigurierbar
//   - Session-Zeiten frei wählbar (nicht mehr fix 8-20 UTC)
//   - 0 / -1 = Automatik (Timeframe-basierte Berechnung bleibt aktiv)
//
// v4.2 Neue Parameter & Features:
//   ENTRY-QUALITÄT:
//   - Volumen-Bestätigung: Tick-Volume vs 20-Bar Durchschnitt (Score +2/+3 oder -1)
//   - Marktstruktur-Erkennung: HH/HL (Aufwärts) + LL/LH (Abwärts) Tracking (Score +2)
//   - Break of Structure (BoS): Strukturbruch als starkes Signal (Score +3)
//   - Momentum-Filter: Min. Momentum nötig (30% ATR oder MACD-Zuwachs) (Score +1/-2)
//   - Volatilitäts-Regime: Niedrig/Normal/Hoch/Extrem Klassifizierung
//     → Extrem blockiert neue Trades, Niedrig = Score -1, Hoch = Score +1
//   RISIKO-MANAGEMENT:
//   - Progressiver Equity-Schutz: 2-Stufen DD (35%/65% vom MaxDD)
//     → Stufe 1: 50% Risikoreduktion, Stufe 2: 75% Reduktion
//   - Richtungslimit: Max 2 Trades in gleicher Richtung (kein Klumpenrisiko)
//   EXIT-MANAGEMENT:
//   - Maximale Haltezeit: Harter Timeout pro Timeframe (Scalp 120, Intra 60, Swing 40)
//   - Session-Ende Auto-Close: Schließt kleine Positionen N Min vor Session-Ende
//   QUALITÄTSFILTER:
//   - Regime-Bestätigungs-Bars: Konfigurierbar (2-3 je nach TF, vorher fix 2)
//   - Konfigurierbarer Momentum-Schwellenwert pro Timeframe
//
// v4.1 Signalqualität + Haltedauer:
//   - minScore massiv erhöht: StarkerTrend 9, MittlererTrend 10, SchwacherTrend 11, Konso 12
//   - Score-Differenz 4 (vorher 3): Richtung muss absolut eindeutig sein
//   - ADX-Fallend-Filter: +2 auf minScore wenn Trend nachlässt
//   - HTF Seitwärts Aufschlag: +2 statt +1
//   - Cooldown minimum 3 Bars (Scalping 4)
//   - Break-Even erst bei 2.5R (vorher 1.5R)
//   - Partial #1: 30% bei 3.5R (vorher 40% bei 2.5R)
//   - Partial #2: 40% bei 6R (vorher 50% bei 4R)
//   - Trailing Start bei 3R (vorher 2R), Faktor 1.2 statt 1.0
//   - Trailing im StarkerTrend: 1.5x Raum (vorher 1.3x)
//   - Early-Exit fast deaktiviert: Nur noch bei 4+ Gegen-Signalen inkl. ADX
//   - Reversal-Exit: 3 Signale nötig (vorher 2), höhere Mindestgewinne
//   - Stale-Trade: Im Trend 1.5x mehr Geduld, alle Werte deutlich erhöht
//   - TP-Multiplier: StarkerTrend 2.2x, MittlererTrend 1.6x, A+ 2.0x
//   - Min R:R: 2.0:1 (vorher 1.8:1)
//   - TP-Basis erhöht: Intraday 4.5, Swing 5.0, Positions 6.5 ATR
//
// v4.0 Mustererkennung & Profitabilitäts-Upgrade:
//   - Morning Star / Evening Star (3-Kerzen Umkehr, Gewicht 4)
//   - Three White Soldiers / Three Black Crows (Continuation, Gewicht 3)
//   - Inside Bar Breakout (Konsolidierungs-Ausbruch, Gewicht 2)
//   - Doji an S/R Levels (Unsicherheit an Key-Levels, Gewicht 2)
//   - Tweezer Top/Bottom (Doppel-Kerzen Umkehr, Gewicht 2)
//   - Double Top/Bottom (Chart-Muster, Gewicht 3)
//   - Support/Resistance aus Swing High/Low (50-Bar Lookback)
//   - ADX-Dynamik: Steigend = Trend verstärkt sich (+2), Fallend = schwächt ab (-1)
//   - S/R als Score-Bonus: Nah am Support = Buy-Bonus, nah am Widerstand = Sell-Bonus
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
        // BENUTZER-PARAMETER (10 Stück - Rest wird automatisch berechnet)
        // =====================================================================

        // --- GRUNDEINSTELLUNGEN ---

        [Parameter("1. Timeframe", DefaultValue = "Hour", Group = "Grundeinstellungen")]
        public TimeFrame BotTimeframe { get; set; }

        [Parameter("2. Markt (Symbol)", DefaultValue = "EURUSD", Group = "Grundeinstellungen")]
        public string MarktSymbol { get; set; }

        [Parameter("3. Strategie-Modus", DefaultValue = 2, MinValue = 1, MaxValue = 3, Group = "Grundeinstellungen")]
        public int StrategieModus { get; set; }
        // 1 = Konservativ (wenige Trades, hohe Qualität)
        // 2 = Normal (ausgewogen)
        // 3 = Aggressiv (mehr Trades, niedrigere Schwellen)

        // --- RISIKO ---

        [Parameter("4. Basis-Risiko %", DefaultValue = 0.0, MinValue = 0.0, MaxValue = 5.0, Step = 0.1, Group = "Risiko")]
        public double ParamBasisRisiko { get; set; }
        // 0.0 = Automatisch (vom Timeframe berechnet)

        [Parameter("5. Max Drawdown %", DefaultValue = 0.0, MinValue = 0.0, MaxValue = 25.0, Step = 0.5, Group = "Risiko")]
        public double ParamMaxDrawdown { get; set; }
        // 0.0 = Automatisch

        [Parameter("6. Max Tagesverlust %", DefaultValue = 0.0, MinValue = 0.0, MaxValue = 10.0, Step = 0.5, Group = "Risiko")]
        public double ParamMaxTagesverlust { get; set; }
        // 0.0 = Automatisch

        // --- TRADE-MANAGEMENT ---

        [Parameter("7. Max offene Positionen", DefaultValue = 0, MinValue = 0, MaxValue = 5, Group = "Trade-Management")]
        public int ParamMaxPositionen { get; set; }
        // 0 = Automatisch (2)

        [Parameter("8. Max Trades pro Tag", DefaultValue = 0, MinValue = 0, MaxValue = 20, Group = "Trade-Management")]
        public int ParamMaxTagesTrades { get; set; }
        // 0 = Automatisch (6)

        // --- ZEITFILTER ---

        [Parameter("9. Session Start (UTC)", DefaultValue = -1, MinValue = -1, MaxValue = 23, Group = "Zeitfilter")]
        public int ParamSessionStart { get; set; }
        // -1 = Automatisch (8 UTC)

        [Parameter("10. Session Ende (UTC)", DefaultValue = -1, MinValue = -1, MaxValue = 23, Group = "Zeitfilter")]
        public int ParamSessionEnde { get; set; }
        // -1 = Automatisch (20 UTC)

        // --- INTERNE SESSION-WERTE (aus Parametern oder Automatik) ---
        private int _sessionStartStunde;
        private int _sessionEndStunde;
        private int _maxDailyTrades;

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
        // MaxDailyTrades: Über Parameter oder automatisch (default 6)

        // Session-Qualität: Multiplikator basierend auf Handelszeit
        private double _sessionQualitaet; // 0.0 - 1.0

        // Min Risk:Reward Ratio
        private double _minRiskReward;

        // === v4.2: NEUE PARAMETER ===

        // Volumen-Bestätigung
        private Bars _tickVolumeBars; // Referenz auf aktuelle Bars (Tick Volume)

        // Marktstruktur: HH/HL/LL/LH Tracking
        private double _letzterSwingHigh;
        private double _letzterSwingLow;
        private double _vorLetzterSwingHigh;
        private double _vorLetzterSwingLow;

        // Volatilitäts-Regime Klassifizierung
        private VolatilitaetsRegime _aktuellesVolRegime;

        // Max Haltezeit (in Minuten, berechnet aus Timeframe)
        private int _maxHaltezeitBars;

        // Session-Ende Auto-Close (Minuten vor Session-Ende)
        private int _sessionEndeVorlaufMinuten;

        // Max Trades in gleicher Richtung
        private int _maxTradesGleicheRichtung;

        // Regime-Bestätigungs-Bars
        private int _regimeBestaetigungsBars;

        // Momentum-Schwelle (MACD Histogram % über Signal)
        private double _minMomentumSchwelle;

        // Equity-Schutz: Progressive DD-Stufen
        private double _ddStufe1Prozent;  // Ab wann Stufe 1 greift
        private double _ddStufe1Reduktion; // Risikoreduktion in Stufe 1
        private double _ddStufe2Prozent;  // Ab wann Stufe 2 greift
        private double _ddStufe2Reduktion; // Risikoreduktion in Stufe 2

        private const string BotLabel = "FullAutoBot";

        // =====================================================================
        // INITIALISIERUNG
        // =====================================================================

        protected override void OnStart()
        {
            Print("=== FullAutoBot v4.3 gestartet ===");
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
            UebernehmeBenuzerParameter();
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

            // v4.2: Neue Variablen initialisieren
            _tickVolumeBars = _marktBars; // Volumen kommt von den Markt-Bars
            _letzterSwingHigh = 0;
            _letzterSwingLow = double.MaxValue;
            _vorLetzterSwingHigh = 0;
            _vorLetzterSwingLow = double.MaxValue;
            _aktuellesVolRegime = VolatilitaetsRegime.Normal;

            // Events registrieren
            _marktBars.BarOpened += OnBarOpened;
            Positions.Closed += OnPositionClosed;

            Print("MaxHalte={0}Bars | SessionClose={1}Min | RegimeBest={2}Bars | MinMomentum={3:F2}",
                _maxHaltezeitBars, _sessionEndeVorlaufMinuten,
                _regimeBestaetigungsBars, _minMomentumSchwelle);
            Print("DD-Schutz: Stufe1 ab {0:F1}% (x{1:F2}) | Stufe2 ab {2:F1}% (x{3:F2})",
                _ddStufe1Prozent, _ddStufe1Reduktion, _ddStufe2Prozent, _ddStufe2Reduktion);
        }

        // =====================================================================
        // BENUTZER-PARAMETER ÜBERNEHMEN (nach Timeframe-Adaption)
        // =====================================================================

        private void UebernehmeBenuzerParameter()
        {
            // Risiko: User-Wert > 0 überschreibt Automatik
            if (ParamBasisRisiko > 0)
                _baseRiskPercent = ParamBasisRisiko;

            if (ParamMaxDrawdown > 0)
                _maxDrawdownPercent = ParamMaxDrawdown;

            if (ParamMaxTagesverlust > 0)
                _dailyLossLimitPercent = ParamMaxTagesverlust;

            // Trade-Management: 0 = Automatik
            if (ParamMaxPositionen > 0)
                _maxOpenPositions = ParamMaxPositionen;

            _maxDailyTrades = ParamMaxTagesTrades > 0 ? ParamMaxTagesTrades : 6;

            // Zeitfilter: -1 = Automatik (8-20 UTC)
            _sessionStartStunde = ParamSessionStart >= 0 ? ParamSessionStart : 8;
            _sessionEndStunde = ParamSessionEnde >= 0 ? ParamSessionEnde : 20;

            // Validierung: Start < Ende
            if (_sessionStartStunde >= _sessionEndStunde)
            {
                Print("WARNUNG: Session-Start ({0}) >= Ende ({1}) - verwende Default 8-20",
                    _sessionStartStunde, _sessionEndStunde);
                _sessionStartStunde = 8;
                _sessionEndStunde = 20;
            }

            // DD-Stufen neu berechnen (falls MaxDD geändert wurde)
            _ddStufe1Prozent = _maxDrawdownPercent * 0.35;
            _ddStufe1Reduktion = 0.5;
            _ddStufe2Prozent = _maxDrawdownPercent * 0.65;
            _ddStufe2Reduktion = 0.25;

            // Min R:R basiert auf Strategie-Modus
            _minRiskReward = 2.0;

            // === STRATEGIE-MODUS anwenden ===
            WendeStrategieModusAn();

            Print("Parameter: Risiko={0:F2}% | MaxDD={1:F1}% | Tagesverlust={2:F1}% | Pos={3} | Trades/Tag={4}",
                _baseRiskPercent, _maxDrawdownPercent, _dailyLossLimitPercent,
                _maxOpenPositions, _maxDailyTrades);
            Print("Session: {0}:00-{1}:00 UTC | Strategie: {2}",
                _sessionStartStunde, _sessionEndStunde,
                StrategieModus == 1 ? "Konservativ" : (StrategieModus == 3 ? "Aggressiv" : "Normal"));
        }

        // =====================================================================
        // STRATEGIE-MODUS: Konservativ / Normal / Aggressiv
        // =====================================================================

        // Interner Strategie-Score-Offset (wird auf minScore addiert/subtrahiert)
        private int _strategieScoreOffset;
        // Interner Cooldown-Multiplikator
        private double _strategieCooldownMultiplier;
        // Interner Risiko-Multiplikator
        private double _strategieRisikoMultiplier;

        private void WendeStrategieModusAn()
        {
            switch (StrategieModus)
            {
                case 1: // KONSERVATIV: Wenige Trades, hohe Qualität, geringes Risiko
                    _strategieScoreOffset = 2;        // +2 auf alle minScores
                    _strategieCooldownMultiplier = 1.5; // 50% längerer Cooldown
                    _strategieRisikoMultiplier = 0.7;   // 30% weniger Risiko
                    _minRiskReward = 2.5;               // Min 2.5:1 R:R
                    _maxTradesGleicheRichtung = 1;      // Nur 1 Trade pro Richtung
                    Print("MODUS: Konservativ - Score+2, Cooldown x1.5, Risiko x0.7, R:R min 2.5:1");
                    break;

                case 3: // AGGRESSIV: Mehr Trades, niedrigere Schwellen, höheres Risiko
                    _strategieScoreOffset = -1;       // -1 auf minScores
                    _strategieCooldownMultiplier = 0.7; // 30% kürzerer Cooldown
                    _strategieRisikoMultiplier = 1.3;   // 30% mehr Risiko
                    _minRiskReward = 1.5;               // Min 1.5:1 R:R
                    _maxTradesGleicheRichtung = 3;      // Bis zu 3 in gleicher Richtung
                    Print("MODUS: Aggressiv - Score-1, Cooldown x0.7, Risiko x1.3, R:R min 1.5:1");
                    break;

                default: // NORMAL: Ausgewogen (Standard)
                    _strategieScoreOffset = 0;
                    _strategieCooldownMultiplier = 1.0;
                    _strategieRisikoMultiplier = 1.0;
                    _minRiskReward = 2.0;
                    // _maxTradesGleicheRichtung bleibt wie vom Timeframe gesetzt
                    Print("MODUS: Normal - Standardwerte");
                    break;
            }

            // Risiko-Multiplikator anwenden
            _baseRiskPercent *= _strategieRisikoMultiplier;

            // Cooldown anpassen
            _signalCooldown = Math.Max(2, (int)(_signalCooldown * _strategieCooldownMultiplier));
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
                _maxOpenPositions = 2; _signalCooldown = 4; _staleTradeBarCount = 50;
                _dailyLossLimitPercent = 2.0;
                _maxHaltezeitBars = 120;         // 10h bei M5
                _sessionEndeVorlaufMinuten = 30; // 30 Min vor Session-Ende raus
                _maxTradesGleicheRichtung = 2;
                _regimeBestaetigungsBars = 3;
                _minMomentumSchwelle = 0.15;     // 15% MACD-Histogram über Vorgänger
            }
            else if (tfMinuten <= 30)
            {
                // Intraday: Sniper-Modus
                _emaFastPeriod = 10; _emaMediumPeriod = 25; _emaSlowPeriod = 50;
                _rsiPeriod = 14; _atrPeriod = 14; _adxPeriod = 14;
                _bollingerPeriod = 20; _bollingerStdDev = 2.0;
                _atrMultiplierSL = 1.8; _atrMultiplierTP = 4.5; _trailingAtrMultiplier = 1.5;
                _baseRiskPercent = 1.2; _maxDrawdownPercent = 8.0;
                _maxOpenPositions = 2; _signalCooldown = 3; _staleTradeBarCount = 40;
                _dailyLossLimitPercent = 2.5;
                _maxHaltezeitBars = 60;          // 30h bei M30
                _sessionEndeVorlaufMinuten = 45;
                _maxTradesGleicheRichtung = 2;
                _regimeBestaetigungsBars = 3;
                _minMomentumSchwelle = 0.10;
            }
            else if (tfMinuten <= 240)
            {
                // Swing: großes R:R, wenige Setups
                _emaFastPeriod = 12; _emaMediumPeriod = 26; _emaSlowPeriod = 50;
                _rsiPeriod = 14; _atrPeriod = 14; _adxPeriod = 14;
                _bollingerPeriod = 20; _bollingerStdDev = 2.0;
                _atrMultiplierSL = 2.0; _atrMultiplierTP = 5.0; _trailingAtrMultiplier = 2.0;
                _baseRiskPercent = 1.6; _maxDrawdownPercent = 10.0;
                _maxOpenPositions = 2; _signalCooldown = 3; _staleTradeBarCount = 30;
                _dailyLossLimitPercent = 3.0;
                _maxHaltezeitBars = 40;          // Swing: mehr Geduld
                _sessionEndeVorlaufMinuten = 0;  // Swing ignoriert Session-Ende
                _maxTradesGleicheRichtung = 2;
                _regimeBestaetigungsBars = 2;
                _minMomentumSchwelle = 0.08;
            }
            else
            {
                // Positions: maximales R:R
                _emaFastPeriod = 10; _emaMediumPeriod = 21; _emaSlowPeriod = 50;
                _rsiPeriod = 14; _atrPeriod = 20; _adxPeriod = 14;
                _bollingerPeriod = 20; _bollingerStdDev = 2.0;
                _atrMultiplierSL = 2.5; _atrMultiplierTP = 6.5; _trailingAtrMultiplier = 2.5;
                _baseRiskPercent = 2.2; _maxDrawdownPercent = 12.0;
                _maxOpenPositions = 2; _signalCooldown = 3; _staleTradeBarCount = 25;
                _dailyLossLimitPercent = 3.5;
                _maxHaltezeitBars = 30;          // Positions: sehr geduldig
                _sessionEndeVorlaufMinuten = 0;  // Positions ignoriert Session-Ende
                _maxTradesGleicheRichtung = 2;
                _regimeBestaetigungsBars = 2;
                _minMomentumSchwelle = 0.05;
            }

            // Equity-Schutz: Gleich für alle Timeframes
            _ddStufe1Prozent = _maxDrawdownPercent * 0.35;  // 35% des Max-DD
            _ddStufe1Reduktion = 0.5;                       // 50% weniger Risiko
            _ddStufe2Prozent = _maxDrawdownPercent * 0.65;  // 65% des Max-DD
            _ddStufe2Reduktion = 0.25;                      // 75% weniger Risiko
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

            // v4.2: Volatilitäts-Regime klassifizieren
            KlassifiziereVolatilitaetsRegime();

            // v4.2: Marktstruktur aktualisieren
            AktualisiereMarktStruktur();

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

            // v4.2: Session-Ende Auto-Close (nur für Intraday/Scalping)
            if (_sessionEndeVorlaufMinuten > 0)
                PruefeSessionEndeAutoClose();

            // v4.2: Regime-Cooldown mit konfigurierbaren Bestätigungs-Bars
            if (_barsSeitRegimeWechsel < _regimeBestaetigungsBars)
            {
                Print("Regime-Cooldown: {0}/{1} Bars seit Wechsel - warte",
                    _barsSeitRegimeWechsel, _regimeBestaetigungsBars);
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

            // 14. Support/Resistance Levels
            ErkenneUnterstuetzungWiderstand(analyse);

            // 15. Double Top/Bottom
            ErkenneDoubleTopBottom(analyse);

            // 16. ADX-Dynamik (steigend/fallend)
            ErkenneAdxDynamik(analyse);

            // 17. v4.2: Volumen-Bestätigung
            AnalysiereVolumen(analyse);

            // 18. v4.2: Marktstruktur (HH/HL/LL/LH)
            AnalysiereMarktStruktur(analyse);

            // 19. v4.2: Volatilitäts-Regime
            analyse.VolRegime = _aktuellesVolRegime;

            // 20. v4.2: Momentum-Filter
            AnalysiereMomentum(analyse);

            // 21. Volle Konfluenz: HTF + EMA + 200 EMA alle in gleicher Richtung
            analyse.VolleKonfluenzBuy = analyse.HtfTrend == TrendRichtung.Aufwaerts
                && analyse.EmaSignal == TrendRichtung.Aufwaerts && analyse.UeberEma200;
            analyse.VolleKonfluenzSell = analyse.HtfTrend == TrendRichtung.Abwaerts
                && analyse.EmaSignal == TrendRichtung.Abwaerts && !analyse.UeberEma200;

            // 22. Markt-Regime
            analyse.Regime = _aktuellesRegime;

            // 23. Gesamtsignal berechnen
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

            // === ERWEITERTE MUSTER (v4) ===

            double close3 = _marktBars.ClosePrices.Last(3);
            double open3 = _marktBars.OpenPrices.Last(3);
            double high2 = _marktBars.HighPrices.Last(2);
            double low2 = _marktBars.LowPrices.Last(2);
            double high3 = _marktBars.HighPrices.Last(3);
            double low3 = _marktBars.LowPrices.Last(3);

            double koerper2 = Math.Abs(close2 - open2);
            double koerper3 = Math.Abs(close3 - open3);
            double range2 = high2 - low2;

            // --- MORNING STAR (3-Kerzen Umkehr bullish) ---
            // Bar 3: große bearish Kerze
            // Bar 2: kleine Kerze (Doji-artig, Unsicherheit)
            // Bar 1: große bullish Kerze, schließt über Mitte von Bar 3
            bool bar3Bearish = close3 < open3 && koerper3 > (high3 - low3) * 0.5;
            bool bar2Klein = range2 > 0 && koerper2 < range2 * 0.3;
            bool bar1BullishStark = close1 > open1 && koerper > gesamtRange * 0.5
                && close1 > (open3 + close3) / 2.0;
            analyse.MorningStar = bar3Bearish && bar2Klein && bar1BullishStark;

            // --- EVENING STAR (3-Kerzen Umkehr bearish) ---
            bool bar3Bullish = close3 > open3 && koerper3 > (high3 - low3) * 0.5;
            bool bar1BearishStark = close1 < open1 && koerper > gesamtRange * 0.5
                && close1 < (open3 + close3) / 2.0;
            analyse.EveningStar = bar3Bullish && bar2Klein && bar1BearishStark;

            // --- THREE WHITE SOLDIERS (3 bullish Kerzen mit steigendem Close) ---
            bool drei_bullish = close1 > open1 && close2 > open2 && close3 > open3;
            bool steigend = close1 > close2 && close2 > close3;
            bool kleine_dochte = gesamtRange > 0 && oberDocht < koerper * 0.3;
            analyse.ThreeWhiteSoldiers = drei_bullish && steigend && kleine_dochte
                && koerper > gesamtRange * 0.5 && koerper2 > range2 * 0.5;

            // --- THREE BLACK CROWS (3 bearish Kerzen mit fallendem Close) ---
            bool drei_bearish = close1 < open1 && close2 < open2 && close3 < open3;
            bool fallend = close1 < close2 && close2 < close3;
            bool kleine_unter_dochte = gesamtRange > 0 && unterDocht < koerper * 0.3;
            analyse.ThreeBlackCrows = drei_bearish && fallend && kleine_unter_dochte
                && koerper > gesamtRange * 0.5 && koerper2 > range2 * 0.5;

            // --- INSIDE BAR BREAKOUT ---
            // Bar 2 Range enthält Bar 1 komplett (Inside Bar)
            // → Breakout-Richtung zeigt Continuation/Reversal
            bool insideBar = high1 <= high2 && low1 >= low2;
            if (_marktBars.ClosePrices.Count > 3)
            {
                double prevClose = _marktBars.ClosePrices.Last(3);
                // Wurde die vorherige Bar zur Inside Bar? Bricht die aktuelle aus?
                bool prevInsideBar = high2 <= high3 && low2 >= low3;
                if (prevInsideBar)
                {
                    analyse.BullishInsideBarBreakout = close1 > high2;
                    analyse.BearishInsideBarBreakout = close1 < low2;
                }
            }

            // --- DOJI (Unsicherheits-Kerze) an Key-Levels ---
            bool istDoji = gesamtRange > 0 && koerper < gesamtRange * 0.1;
            if (istDoji)
            {
                analyse.DojiAnUnterstuetzung = analyse.PreisNahUnteremBand;
                analyse.DojiAnWiderstand = analyse.PreisNahOberemBand;
            }

            // --- TWEEZER TOP/BOTTOM ---
            // Zwei Kerzen mit nahezu identischem High (Top) oder Low (Bottom)
            double toleranz = gesamtRange * 0.1;
            if (toleranz > 0)
            {
                // Tweezer Bottom: Ähnliche Lows, Bar 1 bullish
                analyse.BullishTweezerBottom = Math.Abs(low1 - low2) < toleranz
                    && close1 > open1 && close2 < open2;

                // Tweezer Top: Ähnliche Highs, Bar 1 bearish
                analyse.BearishTweezerTop = Math.Abs(high1 - high2) < toleranz
                    && close1 < open1 && close2 > open2;
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
        // SUPPORT/RESISTANCE ERKENNUNG (v4: Swing High/Low basiert)
        // =====================================================================

        private void ErkenneUnterstuetzungWiderstand(MarktAnalyse analyse)
        {
            // Swing Highs/Lows der letzten 50 Bars finden
            int lookback = 50;
            int swingLen = 3; // 3 Bars links + 3 rechts = Swing-Punkt
            double close = _marktBars.ClosePrices.Last(1);
            double atr = _atr.Result.Last(1);
            double toleranz = atr * 0.5; // S/R Zone statt exakter Preis

            double naechsteUnterstuetzung = 0;
            double naechsterWiderstand = double.MaxValue;

            if (_marktBars.HighPrices.Count < lookback + swingLen + 1)
            {
                analyse.NaechsteUnterstuetzung = 0;
                analyse.NaechsterWiderstand = 0;
                return;
            }

            for (int i = swingLen + 1; i < lookback; i++)
            {
                // Swing High: Höher als N Bars links und rechts
                bool istSwingHigh = true;
                bool istSwingLow = true;
                double high_i = _marktBars.HighPrices.Last(i);
                double low_i = _marktBars.LowPrices.Last(i);

                for (int j = 1; j <= swingLen; j++)
                {
                    if (_marktBars.HighPrices.Last(i - j) >= high_i ||
                        _marktBars.HighPrices.Last(i + j) >= high_i)
                        istSwingHigh = false;

                    if (_marktBars.LowPrices.Last(i - j) <= low_i ||
                        _marktBars.LowPrices.Last(i + j) <= low_i)
                        istSwingLow = false;
                }

                // Nächster Widerstand (über aktuellem Preis)
                if (istSwingHigh && high_i > close && high_i < naechsterWiderstand)
                    naechsterWiderstand = high_i;

                // Nächste Unterstützung (unter aktuellem Preis)
                if (istSwingLow && low_i < close && low_i > naechsteUnterstuetzung)
                    naechsteUnterstuetzung = low_i;
            }

            analyse.NaechsteUnterstuetzung = naechsteUnterstuetzung;
            analyse.NaechsterWiderstand = naechsterWiderstand == double.MaxValue ? 0 : naechsterWiderstand;

            // Nah an S/R Level? (innerhalb 0.5 ATR)
            if (naechsteUnterstuetzung > 0)
                analyse.PreisNahUnterstuetzung = (close - naechsteUnterstuetzung) < toleranz;
            if (naechsterWiderstand < double.MaxValue)
                analyse.PreisNahWiderstand = (naechsterWiderstand - close) < toleranz;
        }

        // =====================================================================
        // DOUBLE TOP/BOTTOM ERKENNUNG (v4)
        // =====================================================================

        private void ErkenneDoubleTopBottom(MarktAnalyse analyse)
        {
            int lookback = 40;
            int swingLen = 3;
            double close = _marktBars.ClosePrices.Last(1);
            double atr = _atr.Result.Last(1);
            double toleranz = atr * 0.3; // Wie nah müssen die Tops/Bottoms sein

            if (_marktBars.HighPrices.Count < lookback + swingLen + 1)
                return;

            // Sammle Swing Highs und Swing Lows
            var swingHighs = new List<double>();
            var swingLows = new List<double>();

            for (int i = swingLen + 1; i < lookback; i++)
            {
                bool istSwingHigh = true;
                bool istSwingLow = true;
                double high_i = _marktBars.HighPrices.Last(i);
                double low_i = _marktBars.LowPrices.Last(i);

                for (int j = 1; j <= swingLen; j++)
                {
                    if (_marktBars.HighPrices.Last(i - j) >= high_i ||
                        _marktBars.HighPrices.Last(i + j) >= high_i)
                        istSwingHigh = false;

                    if (_marktBars.LowPrices.Last(i - j) <= low_i ||
                        _marktBars.LowPrices.Last(i + j) <= low_i)
                        istSwingLow = false;
                }

                if (istSwingHigh) swingHighs.Add(high_i);
                if (istSwingLow) swingLows.Add(low_i);
            }

            // Double Top: Zwei nahe beieinanderliegende Swing Highs, Preis fällt darunter
            for (int i = 0; i < swingHighs.Count - 1; i++)
            {
                if (Math.Abs(swingHighs[i] - swingHighs[i + 1]) < toleranz
                    && close < swingHighs[i] - atr * 0.5)
                {
                    analyse.DoubleTopErkannt = true;
                    break;
                }
            }

            // Double Bottom: Zwei nahe Swing Lows, Preis steigt darüber
            for (int i = 0; i < swingLows.Count - 1; i++)
            {
                if (Math.Abs(swingLows[i] - swingLows[i + 1]) < toleranz
                    && close > swingLows[i] + atr * 0.5)
                {
                    analyse.DoubleBottomErkannt = true;
                    break;
                }
            }
        }

        // =====================================================================
        // ADX-DYNAMIK ERKENNUNG (v4: Steigend = Trend verstärkt sich)
        // =====================================================================

        private void ErkenneAdxDynamik(MarktAnalyse analyse)
        {
            if (_adx.ADX.Count < 4) return;

            double adx1 = _adx.ADX.Last(1);
            double adx2 = _adx.ADX.Last(2);
            double adx3 = _adx.ADX.Last(3);

            // ADX steigend: Trend wird stärker (2 aufeinanderfolgende Anstiege)
            analyse.AdxSteigend = adx1 > adx2 && adx2 > adx3;
            // ADX fallend: Trend wird schwächer
            analyse.AdxFallend = adx1 < adx2 && adx2 < adx3;
        }

        // =====================================================================
        // v4.2: VOLUMEN-BESTÄTIGUNG
        // =====================================================================

        private void AnalysiereVolumen(MarktAnalyse analyse)
        {
            // Tick-Volume als Proxy für echtes Volumen (FX hat kein zentrales Volumen)
            if (_tickVolumeBars.TickVolumes.Count < 22) return;

            double aktuellesVolumen = _tickVolumeBars.TickVolumes.Last(1);

            // Durchschnitt der letzten 20 Bars
            double summeVol = 0;
            for (int i = 2; i <= 21; i++)
                summeVol += _tickVolumeBars.TickVolumes.Last(i);
            double durchschnittVol = summeVol / 20.0;

            if (durchschnittVol > 0)
            {
                analyse.VolumenRatio = aktuellesVolumen / durchschnittVol;
                analyse.VolumenUeberDurchschnitt = analyse.VolumenRatio >= 1.2; // 20% über Durchschnitt
            }
        }

        // =====================================================================
        // v4.2: MARKTSTRUKTUR-ERKENNUNG (HH/HL/LL/LH)
        // =====================================================================

        private void AktualisiereMarktStruktur()
        {
            // Swing Points der letzten 30 Bars ermitteln
            int lookback = 30;
            int swingLen = 3;

            if (_marktBars.HighPrices.Count < lookback + swingLen + 1)
                return;

            // Die zwei letzten Swing Highs und Swing Lows finden
            var recentHighs = new List<double>();
            var recentLows = new List<double>();

            for (int i = swingLen + 1; i < lookback && (recentHighs.Count < 2 || recentLows.Count < 2); i++)
            {
                bool istSwingHigh = true;
                bool istSwingLow = true;
                double high_i = _marktBars.HighPrices.Last(i);
                double low_i = _marktBars.LowPrices.Last(i);

                for (int j = 1; j <= swingLen; j++)
                {
                    if (_marktBars.HighPrices.Last(i - j) >= high_i ||
                        _marktBars.HighPrices.Last(i + j) >= high_i)
                        istSwingHigh = false;

                    if (_marktBars.LowPrices.Last(i - j) <= low_i ||
                        _marktBars.LowPrices.Last(i + j) <= low_i)
                        istSwingLow = false;
                }

                if (istSwingHigh && recentHighs.Count < 2) recentHighs.Add(high_i);
                if (istSwingLow && recentLows.Count < 2) recentLows.Add(low_i);
            }

            // Struktur aktualisieren
            if (recentHighs.Count >= 2)
            {
                _letzterSwingHigh = recentHighs[0];
                _vorLetzterSwingHigh = recentHighs[1];
            }
            if (recentLows.Count >= 2)
            {
                _letzterSwingLow = recentLows[0];
                _vorLetzterSwingLow = recentLows[1];
            }
        }

        private void AnalysiereMarktStruktur(MarktAnalyse analyse)
        {
            if (_letzterSwingHigh == 0 || _vorLetzterSwingHigh == 0) return;
            if (_letzterSwingLow == double.MaxValue || _vorLetzterSwingLow == double.MaxValue) return;

            double close = _marktBars.ClosePrices.Last(1);

            // Higher High + Higher Low = Aufwärtstrend intakt
            analyse.HigherHighs = _letzterSwingHigh > _vorLetzterSwingHigh
                && _letzterSwingLow > _vorLetzterSwingLow;

            // Lower Low + Lower High = Abwärtstrend intakt
            analyse.LowerLows = _letzterSwingLow < _vorLetzterSwingLow
                && _letzterSwingHigh < _vorLetzterSwingHigh;

            // Break of Structure: Preis durchbricht letzten Swing-Punkt
            analyse.StrukturBruchBullish = close > _letzterSwingHigh && analyse.LowerLows;
            analyse.StrukturBruchBearish = close < _letzterSwingLow && analyse.HigherHighs;
        }

        // =====================================================================
        // v4.2: VOLATILITÄTS-REGIME KLASSIFIZIERUNG
        // =====================================================================

        private void KlassifiziereVolatilitaetsRegime()
        {
            // ATR als Prozent des Preises - universell über alle Instrumente
            double close = _marktBars.ClosePrices.Last(1);
            if (close <= 0) return;

            double atrProzent = (_atr.Result.Last(1) / close) * 100;

            // Dynamische Schwellen basierend auf historischem Durchschnitt
            // Benutze Vol-Ratio als Zusatz-Indikator
            if (atrProzent > 3.0 || _volatilitaetsRatio > 2.5)
                _aktuellesVolRegime = VolatilitaetsRegime.Extrem;
            else if (atrProzent > 1.5 || _volatilitaetsRatio > 1.5)
                _aktuellesVolRegime = VolatilitaetsRegime.Hoch;
            else if (atrProzent < 0.05 || _volatilitaetsRatio < 0.5)
                _aktuellesVolRegime = VolatilitaetsRegime.Niedrig;
            else
                _aktuellesVolRegime = VolatilitaetsRegime.Normal;
        }

        // =====================================================================
        // v4.2: MOMENTUM-FILTER
        // =====================================================================

        private void AnalysiereMomentum(MarktAnalyse analyse)
        {
            // Momentum = Wie stark ist die aktuelle Bewegung relativ zur ATR
            double close = _marktBars.ClosePrices.Last(1);
            double prevClose = _marktBars.ClosePrices.Last(2);
            double atr = _atr.Result.Last(1);

            if (atr <= 0) return;

            // Absolute Bewegung relativ zur ATR (0-100+)
            double bewegung = Math.Abs(close - prevClose);
            analyse.MomentumStaerke = (bewegung / atr) * 100;

            // MACD Momentum: Histogram muss in Richtung des Signals zunehmen
            double histAktuell = Math.Abs(_macd.Histogram.Last(1));
            double histVorher = Math.Abs(_macd.Histogram.Last(2));

            bool macdMomentumOk = histVorher > 0
                ? (histAktuell / histVorher - 1.0) >= _minMomentumSchwelle
                : histAktuell > 0;

            // Momentum ausreichend wenn: Bewegung > 30% ATR ODER MACD nimmt zu
            analyse.MomentumAusreichend = analyse.MomentumStaerke >= 30 || macdMomentumOk;
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

            // Kerzenformationen Basis (Gewicht: 2-3)
            if (analyse.BullishEngulfing) buyScore += 3;
            if (analyse.BullishPinBar) buyScore += 2;
            if (analyse.StarkeMomentumKerze && analyse.IstBullishKerze) buyScore += 2;
            else if (analyse.IstBullishKerze && analyse.KerzenKoerper > analyse.UntererDocht) buyScore += 1;

            // Erweiterte Kerzenmuster (v4, Gewicht: 2-4)
            if (analyse.MorningStar) buyScore += 4;                   // Starkes Umkehrmuster
            if (analyse.ThreeWhiteSoldiers) buyScore += 3;            // Starke Continuation
            if (analyse.BullishInsideBarBreakout) buyScore += 2;      // Breakout-Signal
            if (analyse.DojiAnUnterstuetzung) buyScore += 2;          // Unsicherheit am Support
            if (analyse.BullishTweezerBottom) buyScore += 2;          // Doppelboden-Kerze

            // Chart-Muster (v4, Gewicht: 3)
            if (analyse.DoubleBottomErkannt) buyScore += 3;           // Double Bottom Breakout

            // Support/Resistance (v4, Gewicht: 1-2)
            if (analyse.PreisNahUnterstuetzung) buyScore += 2;        // Bounce am Support
            if (analyse.PreisNahWiderstand) buyScore -= 1;            // Nahe am Widerstand = Risiko

            // ADX-Dynamik (v4, Gewicht: 1-2)
            if (analyse.AdxSteigend && analyse.IstTrendStark) buyScore += 2;  // Trend verstärkt sich
            if (analyse.AdxFallend && analyse.Trendstaerke > 25) buyScore -= 1; // Trend schwächt ab

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

            // Erweiterte Kerzenmuster Sell (v4, Gewicht: 2-4)
            if (analyse.EveningStar) sellScore += 4;
            if (analyse.ThreeBlackCrows) sellScore += 3;
            if (analyse.BearishInsideBarBreakout) sellScore += 2;
            if (analyse.DojiAnWiderstand) sellScore += 2;
            if (analyse.BearishTweezerTop) sellScore += 2;

            // Chart-Muster Sell (v4, Gewicht: 3)
            if (analyse.DoubleTopErkannt) sellScore += 3;

            // S/R Sell (v4)
            if (analyse.PreisNahWiderstand) sellScore += 2;
            if (analyse.PreisNahUnterstuetzung) sellScore -= 1;

            // ADX-Dynamik Sell (v4)
            if (analyse.AdxSteigend && analyse.IstTrendStark) sellScore += 2;
            if (analyse.AdxFallend && analyse.Trendstaerke > 25) sellScore -= 1;

            if (analyse.Regime == MarktRegime.StarkerTrend && analyse.EmaSignal == TrendRichtung.Abwaerts)
                sellScore += 1;

            if (analyse.DreiBarsAbwaerts) sellScore += 1;
            if (analyse.CloseImUnterenDrittel) sellScore += 1;
            if (analyse.EmaPullbackBounceSell) sellScore += 2;
            if (analyse.AtrExpandiert) sellScore += 1;
            if (analyse.VolleKonfluenzSell) sellScore += 2;

            // === v4.2: NEUE SCORING-PARAMETER ===

            // Volumen-Bestätigung (Gewicht: 2) - Volumen über Durchschnitt = Überzeugung
            if (analyse.VolumenUeberDurchschnitt)
            {
                buyScore += 2;
                sellScore += 2;
                // Extra-Bonus bei starkem Volumen (>1.5x)
                if (analyse.VolumenRatio >= 1.5)
                {
                    buyScore += 1;
                    sellScore += 1;
                }
            }
            else if (analyse.VolumenRatio < 0.7 && analyse.VolumenRatio > 0)
            {
                // Schwaches Volumen = wenig Überzeugung
                buyScore -= 1;
                sellScore -= 1;
            }

            // Marktstruktur (Gewicht: 2-3) - HH/HL oder LL/LH bestätigt Trendrichtung
            if (analyse.HigherHighs) buyScore += 2;    // Aufwärtsstruktur intakt
            if (analyse.LowerLows) sellScore += 2;     // Abwärtsstruktur intakt
            if (analyse.HigherHighs) sellScore -= 1;   // Gegen Aufwärtsstruktur = Risiko
            if (analyse.LowerLows) buyScore -= 1;      // Gegen Abwärtsstruktur = Risiko

            // Break of Structure (Gewicht: 3) - Strukturbruch = starkes Signal
            if (analyse.StrukturBruchBullish) buyScore += 3;
            if (analyse.StrukturBruchBearish) sellScore += 3;

            // Momentum-Filter (Gewicht: 1-2) - Mindest-Momentum als Qualitätsfilter
            if (analyse.MomentumAusreichend)
            {
                buyScore += 1;
                sellScore += 1;
            }
            else
            {
                // Kein Momentum = Signal ist schwach
                buyScore -= 2;
                sellScore -= 2;
            }

            // Volatilitäts-Regime Bonus/Malus (Gewicht: 1)
            if (analyse.VolRegime == VolatilitaetsRegime.Hoch)
            {
                // Hohe Vol = größere Moves möglich aber auch riskanter
                buyScore += 1;
                sellScore += 1;
            }
            else if (analyse.VolRegime == VolatilitaetsRegime.Niedrig)
            {
                // Niedrige Vol = wenig Bewegung erwartet
                buyScore -= 1;
                sellScore -= 1;
            }

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

            // Regime-adaptive Schwellenwerte (v4.1: deutlich höher - nur A+ Setups)
            int minScore;
            switch (analyse.Regime)
            {
                case MarktRegime.StarkerTrend: minScore = 9; break;   // vorher 7
                case MarktRegime.MittlererTrend: minScore = 10; break; // vorher 7
                case MarktRegime.Konsolidierung:
                    // Konsolidierung: NUR Bollinger-Squeeze-Breakouts erlaubt
                    if (!analyse.BollingerSqueeze)
                        return;
                    minScore = 12; // vorher 9 - Nur absolute Ausnahme-Setups
                    break;
                default: minScore = 11; break; // SchwacherTrend (vorher 8)
            }

            // Strategie-Modus Offset: Konservativ +2, Normal +0, Aggressiv -1
            minScore = Math.Max(5, minScore + _strategieScoreOffset);

            // ADX darf nicht fallend sein (Trend schwächt sich ab = schlechter Einstieg)
            if (analyse.AdxFallend && analyse.Regime != MarktRegime.Konsolidierung)
            {
                minScore += 2; // Noch strengere Anforderung wenn Trend nachlässt
            }

            // HTF-Filter: Flexibel statt binärer Block
            // Mit HTF: +0 | Seitwärts: +1 | Gegen HTF: gesperrt
            int htfBuyAufschlag = 0;
            int htfSellAufschlag = 0;

            if (analyse.HtfTrend == TrendRichtung.Seitwaerts)
            {
                htfBuyAufschlag = 2; // vorher 1 - ohne klaren HTF-Trend viel schwieriger
                htfSellAufschlag = 2;
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

            // Score-Differenz: Richtung muss absolut klar sein (v4.1: diff 4)
            if (buyScore >= buyMinScore && buyScore > sellScore + 4)
            {
                analyse.Signal = SignalTyp.Buy;
                analyse.SignalScore = buyScore;
                string muster = ErkanntesMusterString(analyse, true);
                Print("BUY Score:{0}/{1} (Sell:{2}) | HTF:{3} | RSI:{4:F0} | ADX:{5:F0}{6} | Regime:{7} {8}",
                    buyScore, buyMinScore, sellScore, analyse.HtfTrend, analyse.RsiWert, analyse.Trendstaerke,
                    analyse.AdxSteigend ? "↑" : (analyse.AdxFallend ? "↓" : ""),
                    analyse.Regime, muster);
            }
            else if (sellScore >= sellMinScore && sellScore > buyScore + 4)
            {
                analyse.Signal = SignalTyp.Sell;
                analyse.SignalScore = sellScore;
                string musterSell = ErkanntesMusterString(analyse, false);
                Print("SELL Score:{0}/{1} (Buy:{2}) | HTF:{3} | RSI:{4:F0} | ADX:{5:F0}{6} | Regime:{7} {8}",
                    sellScore, sellMinScore, buyScore, analyse.HtfTrend, analyse.RsiWert, analyse.Trendstaerke,
                    analyse.AdxSteigend ? "↑" : (analyse.AdxFallend ? "↓" : ""),
                    analyse.Regime, musterSell);
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

            // v4.2: Richtungslimit prüfen - max N Trades in gleicher Richtung
            TradeType geplant = analyse.Signal == SignalTyp.Buy ? TradeType.Buy : TradeType.Sell;
            int gleicheRichtung = offene.Count(p => p.TradeType == geplant);
            if (gleicheRichtung >= _maxTradesGleicheRichtung)
            {
                Print("RICHTUNGSLIMIT: Bereits {0} {1}-Trades offen (max {2})",
                    gleicheRichtung, geplant, _maxTradesGleicheRichtung);
                return;
            }

            // v4.2: Extremes Volatilitäts-Regime blockiert neue Trades
            if (_aktuellesVolRegime == VolatilitaetsRegime.Extrem)
            {
                Print("VOL-REGIME EXTREM: Kein neuer Trade bei extremer Volatilität");
                return;
            }

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

            // Dynamische TP-Extension: Regime-, Score- und Vol-basiert (v4.1: höher)
            double tpMultiplier = 1.0;
            if (analyse.Regime == MarktRegime.StarkerTrend)
                tpMultiplier = 2.2; // vorher 1.8 - Trend laufen lassen
            else if (analyse.Regime == MarktRegime.MittlererTrend)
                tpMultiplier = 1.6; // vorher 1.3

            // A+ Setups bekommen mindestens 1.8x TP (vorher 1.4)
            if (analyse.SignalScore >= 14)
                tpMultiplier = Math.Max(tpMultiplier, 2.0);
            else if (analyse.SignalScore >= 11)
                tpMultiplier = Math.Max(tpMultiplier, 1.8);

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

            // 4. v4.2: Progressiver Equity-Schutz (ersetzt alte stufenlose DD-Skalierung)
            _currentDrawdown = _peakBalance > 0 ? ((_peakBalance - Account.Balance) / _peakBalance) * 100 : 0;
            if (_currentDrawdown >= _ddStufe2Prozent)
            {
                risk *= _ddStufe2Reduktion;
                Print("DD-SCHUTZ Stufe 2: DD {0:F1}% >= {1:F1}% -> Risiko x{2:F2}",
                    _currentDrawdown, _ddStufe2Prozent, _ddStufe2Reduktion);
            }
            else if (_currentDrawdown >= _ddStufe1Prozent)
            {
                risk *= _ddStufe1Reduktion;
                Print("DD-SCHUTZ Stufe 1: DD {0:F1}% >= {1:F1}% -> Risiko x{2:F2}",
                    _currentDrawdown, _ddStufe1Prozent, _ddStufe1Reduktion);
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

                // 1. BREAK-EVEN: erst bei 2.5R (v4.1: vorher 1.5R - war zu früh, Trade braucht Raum)
                if (position.Pips > slPips * 2.5 && !IstBreakEven(position))
                {
                    SetzeBreakEven(position);
                }

                // 2a. PARTIAL CLOSE #1: 30% bei 3.5R sichern (vorher 40% bei 2.5R)
                if (position.Pips > slPips * 3.5
                    && position.VolumeInUnits > _marktSymbol.VolumeInUnitsMin * 2
                    && !_partialClosedPositions.Contains(position.Id))
                {
                    double closeVolume = _marktSymbol.NormalizeVolumeInUnits(
                        position.VolumeInUnits * 0.30, RoundingMode.Down);
                    if (closeVolume >= _marktSymbol.VolumeInUnitsMin)
                    {
                        ClosePosition(position, closeVolume);
                        _partialClosedPositions.Add(position.Id);
                        Print("PARTIAL #1: 30% bei +{0:F1} Pips (3.5R)", position.Pips);
                    }
                }

                // 2b. PARTIAL CLOSE #2: Weitere 30% bei 6R (vorher 50% bei 4R)
                if (position.Pips > slPips * 6.0
                    && position.VolumeInUnits > _marktSymbol.VolumeInUnitsMin * 2
                    && _partialClosedPositions.Contains(position.Id)
                    && !_secondPartialClosedPositions.Contains(position.Id))
                {
                    double closeVolume = _marktSymbol.NormalizeVolumeInUnits(
                        position.VolumeInUnits * 0.40, RoundingMode.Down);
                    if (closeVolume >= _marktSymbol.VolumeInUnitsMin)
                    {
                        ClosePosition(position, closeVolume);
                        _secondPartialClosedPositions.Add(position.Id);
                        Print("PARTIAL #2: 40% bei +{0:F1} Pips (6R) - Rest läuft weiter", position.Pips);
                    }
                }

                // 2c. EARLY-EXIT komplett entschärft: Nur noch bei EXTREMER Schwäche
                // (v4.1: Nur bei 1.5-2.5R Profit UND alle 4 Gegen-Signale + ADX fallend)
                if (position.Pips > slPips * 1.5 && position.Pips < slPips * 2.5
                    && !_partialClosedPositions.Contains(position.Id))
                {
                    int gegenSignale = 0;
                    double rsi = _rsi.Result.Last(1);

                    // RSI extrem gegen Position
                    if (position.TradeType == TradeType.Buy && rsi > 78) gegenSignale++;
                    if (position.TradeType == TradeType.Sell && rsi < 22) gegenSignale++;

                    // MACD deutlich gegen Position
                    if (position.TradeType == TradeType.Buy
                        && _macd.Histogram.Last(1) < 0 && _macd.Histogram.Last(2) > 0) gegenSignale++;
                    if (position.TradeType == TradeType.Sell
                        && _macd.Histogram.Last(1) > 0 && _macd.Histogram.Last(2) < 0) gegenSignale++;

                    // EMA-Kreuzung gegen Position
                    double fast = _emaFast.Result.Last(1);
                    double med = _emaMedium.Result.Last(1);
                    if (position.TradeType == TradeType.Buy && fast < med) gegenSignale++;
                    if (position.TradeType == TradeType.Sell && fast > med) gegenSignale++;

                    // ADX fallend = Trend löst sich auf
                    if (_adx.ADX.Last(1) < _adx.ADX.Last(2) && _adx.ADX.Last(2) < _adx.ADX.Last(3))
                        gegenSignale++;

                    // Nur bei 4+ Gegen-Signalen raus (vorher 3)
                    if (gegenSignale >= 4)
                    {
                        Print("EARLY-EXIT: {0} Gegen-Signale bei +{1:F1} Pips - sichere Gewinn",
                            gegenSignale, position.Pips);
                        ClosePosition(position);
                        continue;
                    }
                }

                // 3. PROGRESSIVER TRAILING STOP: Viel weiter - Trade laufen lassen
                double trailingStart = slPips * 3.0; // vorher 2.0R
                if (position.Pips > trailingStart)
                {
                    double profitR = position.Pips / slPips;

                    // Trailing-Distanz: Weiter als vorher, erst bei sehr hohem Profit enger
                    double trailingFaktor;
                    if (profitR > 8.0)
                        trailingFaktor = 0.7;  // 8R+: enger, großen Gewinn schützen
                    else if (profitR > 5.0)
                        trailingFaktor = 0.85; // 5-8R: etwas enger
                    else
                        trailingFaktor = 1.2;  // 3-5R: weit - Trade Raum geben (vorher 1.0)

                    double trailingDistanz = AtrZuPips(atr * _trailingAtrMultiplier * trailingFaktor);

                    // Im starken Trend: deutlich mehr Raum lassen
                    if (_aktuellesRegime == MarktRegime.StarkerTrend)
                        trailingDistanz *= 1.5; // vorher 1.3

                    // Vol-Expansion: Breiterer Trail (mehr Noise)
                    if (_volatilitaetsRatio > 1.3)
                        trailingDistanz *= 1.2;  // vorher 1.15
                    else if (_volatilitaetsRatio < 0.75)
                        trailingDistanz *= 0.9;  // vorher 0.85

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

                // v4.2: MAXIMALE HALTEZEIT - harter Timeout
                if (_maxHaltezeitBars > 0 && barsOffen >= _maxHaltezeitBars)
                {
                    Print("MAX-HALTEZEIT: {0} Bars ({1} max) bei {2:F1} Pips - schließe",
                        barsOffen, _maxHaltezeitBars, position.Pips);
                    ClosePosition(position);
                    continue;
                }

                // STALE TRADE: Position geht nirgendwohin (v4.1: viel geduldiger)
                double atrPipsStale = AtrZuPips(_atr.Result.Last(1));
                // Flach: kaum Bewegung nach vielen Bars - aber im Trend mehr Geduld
                int staleGeduld = _aktuellesRegime == MarktRegime.StarkerTrend
                    || _aktuellesRegime == MarktRegime.MittlererTrend
                    ? (int)(_staleTradeBarCount * 1.5)
                    : _staleTradeBarCount;
                if (barsOffen >= staleGeduld && Math.Abs(position.Pips) < atrPipsStale * 0.3)
                {
                    Print("STALE TRADE geschlossen nach {0} Bars bei {1:F1} Pips (flach)", barsOffen, position.Pips);
                    ClosePosition(position);
                    continue;
                }
                // Lange im Minus: Nach 2x Stale-Count und immer noch negativ
                if (barsOffen >= (int)(_staleTradeBarCount * 2.0) && position.Pips < 0 && position.Pips > -atrPipsStale)
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

                // EMA-Kreuzung gegen Position (nur mit Mindestgewinn von 2.5R)
                double fast = _emaFast.Result.Last(1);
                double medium = _emaMedium.Result.Last(1);
                double fastVorher = _emaFast.Result.Last(2);
                double mediumVorher = _emaMedium.Result.Last(2);

                bool emaBearishCross = fastVorher >= mediumVorher && fast < medium;
                bool emaBullishCross = fastVorher <= mediumVorher && fast > medium;

                if (position.TradeType == TradeType.Buy && emaBearishCross && position.Pips > atrPipsReversal * 2.5)
                    reversalZaehler++;
                if (position.TradeType == TradeType.Sell && emaBullishCross && position.Pips > atrPipsReversal * 2.5)
                    reversalZaehler++;

                // MACD Umkehr gegen Position (nur mit deutlichem Gewinn von 3R)
                if (position.TradeType == TradeType.Buy && _macd.Histogram.Last(1) < 0
                    && _macd.Histogram.Last(2) > 0 && position.Pips > atrPipsReversal * 3.0)
                {
                    reversalZaehler++;
                }
                if (position.TradeType == TradeType.Sell && _macd.Histogram.Last(1) > 0
                    && _macd.Histogram.Last(2) < 0 && position.Pips > atrPipsReversal * 3.0)
                {
                    reversalZaehler++;
                }

                // v4.1: Nur schließen wenn mindestens 3 Reversal-Indikatoren feuern (vorher 2)
                if (reversalZaehler >= 3)
                {
                    Print("REVERSAL-EXIT ({0} Signale) bei {1:F1} Pips (RSI:{2:F0})",
                        reversalZaehler, position.Pips, rsi);
                    ClosePosition(position);
                }
            }
        }

        // =====================================================================
        // v4.2: SESSION-ENDE AUTO-CLOSE
        // =====================================================================

        private void PruefeSessionEndeAutoClose()
        {
            int stunde = Server.Time.Hour;
            int minute = Server.Time.Minute;
            int sessionEndStunde = _sessionEndStunde;

            // Berechne Minuten bis Session-Ende
            int minutenBisEnde = (sessionEndStunde - stunde) * 60 - minute;

            if (minutenBisEnde > 0 && minutenBisEnde <= _sessionEndeVorlaufMinuten)
            {
                var positionen = Positions.FindAll(BotLabel, _marktSymbol.Name);
                foreach (var pos in positionen)
                {
                    // Nur Positionen schließen die wenig im Gewinn sind (große Runner laufen lassen)
                    double atrPips = AtrZuPips(_atr.Result.Last(1));
                    if (pos.Pips < atrPips * 2.0) // Weniger als 2 ATR Gewinn
                    {
                        Print("SESSION-ENDE: Schließe {0} bei {1:F1} Pips ({2} Min bis Session-Ende)",
                            pos.TradeType, pos.Pips, minutenBisEnde);
                        ClosePosition(pos);
                    }
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
            if (_dailyTradeCount >= _maxDailyTrades)
            {
                Print("MAX DAILY TRADES: {0}/{1} Trades heute - genug für heute", _dailyTradeCount, _maxDailyTrades);
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
            // Session-Zeiten aus Benutzer-Parametern (Default: 8-20 UTC)
            return stunde >= _sessionStartStunde && stunde <= _sessionEndStunde;
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

            // Session-Rand: Erste und letzte Stunde der User-Session
            if (stunde == _sessionStartStunde || stunde == _sessionEndStunde)
                return 0.65;

            // Außerhalb Kern aber innerhalb Session
            if (stunde >= _sessionStartStunde && stunde <= _sessionEndStunde)
                return 0.75;

            // Alles andere
            return 0.5;
        }

        // =====================================================================
        // CLEANUP
        // =====================================================================

        protected override void OnStop()
        {
            int total = _totalWins + _totalLosses;
            Print("=== FullAutoBot v4.3 gestoppt ===");
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
            Print("Vol-Regime: {0} | Struktur: SH={1:F5} SL={2:F5} | DD-Stufen: {3:F1}%/{4:F1}%",
                _aktuellesVolRegime, _letzterSwingHigh, _letzterSwingLow,
                _ddStufe1Prozent, _ddStufe2Prozent);

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

        private string ErkanntesMusterString(MarktAnalyse a, bool isBuy)
        {
            var muster = new List<string>();
            if (isBuy)
            {
                if (a.MorningStar) muster.Add("MorningStar");
                if (a.ThreeWhiteSoldiers) muster.Add("3WS");
                if (a.BullishInsideBarBreakout) muster.Add("InsideBreakout");
                if (a.BullishEngulfing) muster.Add("BullEngulf");
                if (a.BullishPinBar) muster.Add("PinBar");
                if (a.BullishTweezerBottom) muster.Add("TweezerBot");
                if (a.DojiAnUnterstuetzung) muster.Add("DojiSupport");
                if (a.DoubleBottomErkannt) muster.Add("DoubleBottom");
                if (a.PreisNahUnterstuetzung) muster.Add("@Support");
                if (a.RsiBullishDivergenz) muster.Add("RSI-Div");
                if (a.HigherHighs) muster.Add("HH/HL");
                if (a.StrukturBruchBullish) muster.Add("BoS↑");
                if (a.VolumenUeberDurchschnitt) muster.Add("Vol+" + (a.VolumenRatio >= 1.5 ? "+" : ""));
                if (a.MomentumAusreichend) muster.Add("Mom✓");
            }
            else
            {
                if (a.EveningStar) muster.Add("EveningStar");
                if (a.ThreeBlackCrows) muster.Add("3BC");
                if (a.BearishInsideBarBreakout) muster.Add("InsideBreakout");
                if (a.BearishEngulfing) muster.Add("BearEngulf");
                if (a.BearishPinBar) muster.Add("PinBar");
                if (a.BearishTweezerTop) muster.Add("TweezerTop");
                if (a.DojiAnWiderstand) muster.Add("DojiResist");
                if (a.DoubleTopErkannt) muster.Add("DoubleTop");
                if (a.PreisNahWiderstand) muster.Add("@Resist");
                if (a.RsiBearishDivergenz) muster.Add("RSI-Div");
                if (a.LowerLows) muster.Add("LL/LH");
                if (a.StrukturBruchBearish) muster.Add("BoS↓");
                if (a.VolumenUeberDurchschnitt) muster.Add("Vol+" + (a.VolumenRatio >= 1.5 ? "+" : ""));
                if (a.MomentumAusreichend) muster.Add("Mom✓");
            }
            return muster.Count > 0 ? "| " + string.Join(", ", muster) : "";
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

            // Kerzenformationen (Basis)
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

            // Erweiterte Kerzenmuster (v4)
            public bool MorningStar { get; set; }
            public bool EveningStar { get; set; }
            public bool ThreeWhiteSoldiers { get; set; }
            public bool ThreeBlackCrows { get; set; }
            public bool BullishInsideBarBreakout { get; set; }
            public bool BearishInsideBarBreakout { get; set; }
            public bool DojiAnUnterstuetzung { get; set; }
            public bool DojiAnWiderstand { get; set; }
            public bool BullishTweezerBottom { get; set; }
            public bool BearishTweezerTop { get; set; }

            // Chart-Muster (v4)
            public bool DoubleBottomErkannt { get; set; }
            public bool DoubleTopErkannt { get; set; }

            // Support/Resistance
            public double NaechsteUnterstuetzung { get; set; }
            public double NaechsterWiderstand { get; set; }
            public bool PreisNahUnterstuetzung { get; set; }
            public bool PreisNahWiderstand { get; set; }

            // ADX-Dynamik
            public bool AdxSteigend { get; set; }
            public bool AdxFallend { get; set; }

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

            // v4.2: Volumen-Bestätigung
            public bool VolumenUeberDurchschnitt { get; set; }
            public double VolumenRatio { get; set; } // Aktuell / Durchschnitt

            // v4.2: Marktstruktur
            public bool HigherHighs { get; set; }  // HH + HL = Aufwärtstrend intakt
            public bool LowerLows { get; set; }    // LL + LH = Abwärtstrend intakt
            public bool StrukturBruchBullish { get; set; } // Break of Structure nach oben
            public bool StrukturBruchBearish { get; set; } // Break of Structure nach unten

            // v4.2: Volatilitäts-Regime
            public VolatilitaetsRegime VolRegime { get; set; }

            // v4.2: Momentum-Stärke
            public double MomentumStaerke { get; set; } // Wie stark ist die Bewegung (0-100)
            public bool MomentumAusreichend { get; set; }
        }

        // v4.2: Volatilitäts-Regime Enum
        private enum VolatilitaetsRegime { Niedrig, Normal, Hoch, Extrem }
    }
}
