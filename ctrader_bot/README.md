# cTrader Bot - Automatisches Trading System

Ein vollautomatischer Trading-Bot mit verschiedenen auswählbaren Strategien für den Handel auf Forex-Märkten. Das System ist modular aufgebaut und bietet professionelles Risk Management.

## 🎯 Features

- **5 vorgefertigte Trading-Strategien**
  - Trend Following (Moving Average Crossover)
  - Mean Reversion (Bollinger Bands + RSI)
  - Breakout (Support/Resistance Breakouts)
  - Grid Trading (Automatisches Grid-System)
  - RSI Strategy (RSI mit Divergenz-Erkennung)

- **Professionelles Risk Management**
  - Automatische Position Sizing
  - Stop Loss und Take Profit
  - Trailing Stops
  - Tägliche Verlustlimits
  - Risk/Reward Management

- **Vollautomatisch**
  - Automatische Marktanalyse
  - Automatische Trade-Ausführung
  - Automatisches Position Management
  - Nur Strategieauswahl ist manuell

- **Demo & Backtesting**
  - Integrierter Market Data Simulator
  - Backtesting-Funktionen
  - Performance-Analyse

## 📁 Projektstruktur

```
ctrader_bot/
├── strategies/              # Trading-Strategien
│   ├── base_strategy.py    # Basis-Klasse für Strategien
│   ├── trend_following.py  # Trend-Following-Strategie
│   ├── mean_reversion.py   # Mean-Reversion-Strategie
│   ├── breakout.py         # Breakout-Strategie
│   ├── grid_trading.py     # Grid-Trading-Strategie
│   └── rsi_strategy.py     # RSI-Strategie
│
├── utils/                   # Utility-Module
│   ├── indicators.py       # Technische Indikatoren
│   └── risk_manager.py     # Risk Management System
│
├── bot.py                   # Haupt-Bot-Klasse
├── config.py               # Konfiguration & Strategie-Definitionen
├── market_data_simulator.py # Marktdaten-Simulator
├── run_bot.py              # Interaktiver Bot-Runner
├── demo_quick_start.py     # Quick-Start Demo
└── README.md               # Diese Datei
```

## 🚀 Quick Start

### 1. Installation

Keine zusätzlichen Abhängigkeiten außer Python 3.7+ und NumPy:

```bash
pip install numpy
```

### 2. Quick Demo ausführen

Schnelle Demo mit RSI-Strategie:

```bash
cd /home/user/core/ctrader_bot
python3 demo_quick_start.py
```

### 3. Alle Strategien testen

Vergleiche alle Strategien in verschiedenen Marktbedingungen:

```bash
python3 demo_quick_start.py all
```

### 4. Spezifische Strategie testen

```bash
python3 demo_quick_start.py trend_following mixed
python3 demo_quick_start.py mean_reversion ranging
python3 demo_quick_start.py breakout volatile
```

### 5. Interaktiver Modus

Starte den Bot interaktiv mit Strategieauswahl:

```bash
python3 run_bot.py
```

Der Bot führt dich durch:
1. Strategieauswahl
2. Konfiguration (Symbol, Timeframe, Kapital, Risiko)
3. Modus-Auswahl (Demo oder Live)

## 📊 Verfügbare Strategien

### 1. Trend Following
**Geeignet für:** Trending Markets
**Beschreibung:** Folgt Markttrends mit Moving Average Crossover

**Funktionsweise:**
- Nutzt schnellen und langsamen Moving Average
- Kaufsignal: Schneller MA kreuzt langsamen MA nach oben
- Verkaufssignal: Schneller MA kreuzt langsamen MA nach unten
- Trend-Filter mit langfristigem MA

**Parameter:**
- `fast_ma_period`: 10 (Schneller MA)
- `slow_ma_period`: 30 (Langsamer MA)
- `trend_filter_period`: 200 (Trend-Filter)
- `use_ema`: True (EMA statt SMA)

### 2. Mean Reversion
**Geeignet für:** Ranging Markets
**Beschreibung:** Handelt bei extremen Abweichungen vom Durchschnitt

**Funktionsweise:**
- Nutzt Bollinger Bänder zur Identifikation von Extremen
- RSI zur Bestätigung von Überverkauft/Überkauft
- Kauft bei unterem Band + RSI < 30
- Verkauft bei oberem Band + RSI > 70
- Exit zum mittleren Band

**Parameter:**
- `bb_period`: 20 (Bollinger Band Periode)
- `bb_std_dev`: 2.0 (Standardabweichungen)
- `rsi_period`: 14
- `rsi_oversold`: 30
- `rsi_overbought`: 70

### 3. Breakout
**Geeignet für:** Volatile Markets
**Beschreibung:** Handelt bei Ausbrüchen aus Konsolidierungsphasen

**Funktionsweise:**
- Identifiziert Support und Resistance Levels
- Wartet auf Breakout mit hohem Volumen
- Kauft bei Breakout über Resistance
- Verkauft bei Breakout unter Support
- Volumen-Bestätigung erforderlich

**Parameter:**
- `lookback_period`: 20 (Periode für Highs/Lows)
- `breakout_threshold`: 0.0001 (Minimaler Breakout)
- `volume_multiplier`: 1.5 (Volumen-Multiplikator)
- `consolidation_threshold`: 0.5%

### 4. Grid Trading
**Geeignet für:** Sideways Markets
**Beschreibung:** Platziert Orders in einem gleichmäßigen Raster

**Funktionsweise:**
- Erstellt Grid um aktuellen Preis
- Buy-Orders unterhalb, Sell-Orders oberhalb
- Automatische Take-Profit-Orders
- Re-Initialisierung bei zu großer Drift
- Maximale Anzahl gleichzeitiger Positionen

**Parameter:**
- `grid_levels`: 5 (Grid-Ebenen pro Seite)
- `grid_spacing`: 0.0010 (10 Pips Abstand)
- `take_profit_pips`: 0.0015 (15 Pips TP)
- `max_positions`: 3

### 5. RSI Strategy
**Geeignet für:** All Market Conditions
**Beschreibung:** RSI-basiert mit Divergenz-Erkennung

**Funktionsweise:**
- Primärer Indikator: RSI (14)
- MACD zur Bestätigung
- Divergenz-Erkennung (Bullish/Bearish)
- Multiple Exit-Kriterien
- Adaptive zu verschiedenen Marktbedingungen

**Parameter:**
- `rsi_period`: 14
- `oversold_level`: 30
- `overbought_level`: 70
- `extreme_oversold`: 20
- `extreme_overbought`: 80
- `use_divergence`: True

## ⚙️ Konfiguration

### Risk Management

Standard-Einstellungen in `config.py`:

```python
DEFAULT_RISK_CONFIG = {
    'max_risk_per_trade': 0.02,      # 2% Risiko pro Trade
    'max_position_size': 0.1,        # 10% max. Position
    'max_daily_loss': 0.05,          # 5% max. Tagesverlust
    'use_trailing_stop': True,       # Trailing Stop aktiviert
}
```

### Eigene Konfiguration erstellen

```python
from config import create_custom_config

config = create_custom_config(
    strategy_name='rsi',
    symbol='GBPUSD',
    timeframe='M15',
    initial_balance=5000.0,
    risk_per_trade=0.01,  # 1% Risiko
    # Strategie-spezifische Parameter überschreiben:
    rsi_period=21,
    oversold_level=25
)
```

## 🔧 Programmatische Verwendung

### Einfaches Beispiel

```python
from bot import TradingBot
from market_data_simulator import create_sample_data
from config import create_custom_config

# Konfiguration
config = create_custom_config(
    strategy_name='trend_following',
    symbol='EURUSD',
    timeframe='H1',
    initial_balance=10000.0
)

# Bot erstellen
bot = TradingBot(
    strategy_name='trend_following',
    symbol='EURUSD',
    timeframe='H1',
    initial_balance=10000.0,
    config=config
)

# Marktdaten simulieren
simulator = create_sample_data(
    symbol='EURUSD',
    bars=100,
    market_type='trending_up'
)

# Trading-Loop
for i in range(100):
    # Generiere Marktdaten
    simulator.generate_next_candle()
    market_data = simulator.get_market_data()

    # Analysiere & Trade
    trade = bot.analyze_market(market_data)

    if trade:
        print(f"Trade: {trade['action']} @ {trade.get('entry_price', 'N/A')}")

# Statistiken anzeigen
stats = bot.risk_manager.get_statistics()
print(f"Final Balance: {stats['account_balance']:.2f}")
print(f"Win Rate: {stats['win_rate']:.2f}%")
```

### Eigene Strategie erstellen

```python
from strategies.base_strategy import BaseStrategy
from utils.indicators import Indicators

class MyCustomStrategy(BaseStrategy):
    def __init__(self, symbol="EURUSD", timeframe="H1", my_param=10):
        super().__init__("My Strategy", symbol, timeframe)
        self.my_param = my_param

    def analyze(self, market_data):
        closes = market_data.get('close', [])

        # Deine Logik hier
        sma = Indicators.sma(closes, self.my_param)
        current_price = closes[-1]

        if current_price > sma:
            return ('BUY', {'atr': 0.001})
        elif current_price < sma:
            return ('SELL', {'atr': 0.001})

        return ('HOLD', None)

    def get_parameters(self):
        return {
            'strategy': 'My Custom Strategy',
            'my_param': self.my_param
        }
```

## 📈 Technische Indikatoren

Verfügbare Indikatoren in `utils/indicators.py`:

- **SMA** - Simple Moving Average
- **EMA** - Exponential Moving Average
- **RSI** - Relative Strength Index
- **MACD** - Moving Average Convergence Divergence
- **Bollinger Bands** - Volatilitätsbänder
- **ATR** - Average True Range
- **Stochastic** - Stochastic Oscillator

### Verwendung

```python
from utils.indicators import Indicators

# SMA
sma = Indicators.sma(prices, period=20)

# RSI
rsi = Indicators.rsi(prices, period=14)

# Bollinger Bands
upper, middle, lower = Indicators.bollinger_bands(prices, period=20, std_dev=2.0)

# ATR
atr = Indicators.atr(highs, lows, closes, period=14)
```

## 🎮 Demo-Modus

Der Demo-Modus simuliert realistische Marktbedingungen:

```python
from market_data_simulator import create_sample_data

# Trending Market
simulator = create_sample_data(
    symbol='EURUSD',
    bars=200,
    market_type='trending_up'
)

# Ranging Market
simulator = create_sample_data(
    symbol='EURUSD',
    bars=200,
    market_type='ranging'
)

# Volatile Market
simulator = create_sample_data(
    symbol='EURUSD',
    bars=200,
    market_type='volatile'
)

# Mixed Conditions
simulator = create_sample_data(
    symbol='EURUSD',
    bars=200,
    market_type='mixed'
)
```

## 📊 Performance-Analyse

Der Bot speichert automatisch:

- **Trade History** - Alle ausgeführten Trades
- **Risk Statistics** - Win Rate, P/L, etc.
- **Bot State** - Aktueller Zustand (in `bot_state.json`)

### Statistiken abrufen

```python
# Bot-Status
status = bot.get_status()
print(status)

# Risk-Statistiken
stats = bot.risk_manager.get_statistics()
print(f"Win Rate: {stats['win_rate']:.2f}%")
print(f"Total Trades: {stats['total_trades']}")
print(f"Balance: {stats['account_balance']:.2f}")

# Trade History
for trade in bot.trade_history:
    print(trade)
```

## ⚠️ Wichtige Hinweise

### Für Live-Trading

Diese Implementation ist ein Framework. Für echtes Live-Trading benötigst du:

1. **cTrader API Integration**
   - cTrader Open API Credentials
   - WebSocket-Verbindung für Echtzeitdaten
   - Order-Management über API

2. **Zusätzliche Sicherheitsmaßnahmen**
   - API-Rate-Limiting
   - Verbindungsüberwachung
   - Error Handling für Netzwerkprobleme
   - Order-Bestätigung

3. **Regulierung & Compliance**
   - Broker-Zulassung
   - Risiko-Offenlegung
   - Logging für Audits

### Risk Management

- **Teste immer zuerst im Demo-Modus**
- **Starte mit kleinen Positionsgrößen**
- **Verwende angemessene Stop Losses**
- **Überwache den Bot regelmäßig**
- **Verstehe die Strategien vollständig**

## 🔍 Troubleshooting

### Bot führt keine Trades aus

- Prüfe ob genug historische Daten vorhanden sind
- Überprüfe Strategie-Parameter
- Aktiviere Debug-Logging

### Performance ist schlecht

- Teste Strategie in verschiedenen Marktbedingungen
- Optimiere Parameter (Vorsicht: Overfitting!)
- Prüfe ob Strategie zum aktuellen Markt passt

### Fehler beim Start

```bash
# Logging aktivieren
import logging
logging.basicConfig(level=logging.DEBUG)
```

## 📝 Lizenz

Siehe Haupt-Repository-Lizenz (GPLv3)

## 🤝 Beitragen

Verbesserungen und neue Strategien sind willkommen!

## ⚡ Nächste Schritte

1. **Teste verschiedene Strategien im Demo-Modus**
2. **Optimiere Parameter für deine bevorzugten Märkte**
3. **Erstelle eigene Strategien**
4. **Implementiere cTrader API für Live-Trading**
5. **Erweitere mit weiteren Indikatoren**

---

**Disclaimer:** Trading birgt Risiken. Diese Software dient nur zu Bildungszwecken. Nutze sie auf eigene Gefahr. Keine Garantie für Gewinne.
