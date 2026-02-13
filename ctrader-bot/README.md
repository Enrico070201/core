# FullAutoBot - Vollautomatischer cTrader Trading Bot

## Konzept

Nur **2 Parameter** einstellen - der Rest wird automatisch berechnet:

| Parameter | Beschreibung | Beispiel |
|-----------|-------------|---------|
| **Timeframe** | Chart-Zeitrahmen | `Hour1`, `Minute15`, `Daily` |
| **Markt (Symbol)** | Handelsinstrument | `EURUSD`, `GBPUSD`, `US30` |

## Was der Bot automatisch macht

### Analyse
- **Multi-Timeframe Trend**: Erkennt den übergeordneten Trend via Higher-Timeframe EMAs
- **EMA-Crossover** (Fast/Medium/Slow): Bestimmt lokalen Trend
- **ADX**: Misst Trendstärke, handelt nur bei ausreichendem Trend
- **RSI**: Erkennt überkaufte/überverkaufte Bereiche
- **MACD**: Erkennt Momentum-Wechsel via Histogramm-Crossover
- **Bollinger Bands**: Erkennt Extremzonen und Volatilität
- **ATR**: Misst Volatilität für dynamische SL/TP-Berechnung
- **Kerzenformationen**: Bestätigt Signale durch Preis-Action

### Risikomanagement
- **Automatische Positionsgröße**: Basierend auf ATR und Kontogröße
- **Dynamischer Stop-Loss**: ATR-basiert, passt sich der Volatilität an
- **Dynamischer Take-Profit**: ATR-basiert mit günstigem Risiko/Reward-Verhältnis
- **Trailing Stop**: Sichert Gewinne automatisch ab
- **Max-Drawdown-Schutz**: Pausiert den Bot bei zu hohen Verlusten
- **Verlustserie-Erkennung**: Halbiert Positionsgrößen nach 3 Verlusten
- **Session-Filter**: Handelt nur während aktiver Marktzeiten (07-21 UTC)
- **Wochenend-Filter**: Keine Trades am Wochenende

### Adaptive Parameter je Timeframe

| Timeframe | Modus | Risiko/Trade | Max Drawdown | SL (ATR-x) | TP (ATR-x) |
|-----------|-------|-------------|-------------|------------|------------|
| M1-M5 | Scalping | 0.5% | 5% | 1.5x | 2.0x |
| M15-M30 | Intraday | 0.75% | 6% | 1.8x | 2.5x |
| H1-H4 | Swing | 1.0% | 8% | 2.0x | 3.0x |
| Daily+ | Position | 1.5% | 10% | 2.5x | 4.0x |

### Signal-Scoring-System

Der Bot vergibt Punkte für jedes bestätigte Signal:

| Faktor | Max Punkte |
|--------|-----------|
| Higher-Timeframe Trend | +3 |
| EMA Trend-Richtung | +2 |
| ADX Trendstärke | +1 |
| RSI Extreme/Zone | +2 |
| MACD Crossover/Momentum | +2 |
| Bollinger Band Berührung | +1 |
| Kerzenformation | +1 |

**Mindestens 5 Punkte** erforderlich für ein Handelssignal.
Der Buy-Score muss den Sell-Score um mindestens 2 Punkte übertreffen (und umgekehrt).

## Installation in cTrader

1. Öffne **cTrader Desktop**
2. Gehe zu **Automate** (unten links)
3. Klicke auf **New cBot**
4. Lösche den Standardcode und füge den Inhalt von `FullAutoBot.cs` ein
5. Klicke auf **Build**
6. Gehe zum Chart und wähle den gewünschten Markt
7. Drag & Drop den Bot auf den Chart
8. Stelle **Timeframe** und **Markt** ein
9. Starte den Bot

## Backtesting

1. In cTrader: **Automate** -> **Backtesting**
2. Wähle den **FullAutoBot**
3. Stelle Timeframe und Symbol ein
4. Wähle den Zeitraum
5. Klicke auf **Start**

## Hinweis

Dieser Bot ist ein algorithmisches Handelswerkzeug. Vergangene Performance garantiert keine zukünftigen Ergebnisse. Teste immer zuerst auf einem **Demo-Konto** bevor du echtes Geld einsetzt.
