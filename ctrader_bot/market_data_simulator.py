"""
Market Data Simulator für Backtesting und Demo-Modus
"""

import random
import numpy as np
from typing import Dict, List
from datetime import datetime, timedelta


class MarketDataSimulator:
    """
    Simuliert Marktdaten für Backtesting und Demo-Trading
    """

    def __init__(
        self,
        symbol: str = "EURUSD",
        initial_price: float = 1.1000,
        volatility: float = 0.0010,
        trend_strength: float = 0.0
    ):
        """
        Initialisiert den Market Data Simulator

        Args:
            symbol: Handelssymbol
            initial_price: Startpreis
            volatility: Volatilität (Standardabweichung der Returns)
            trend_strength: Trendstärke (-1.0 bis 1.0, 0 = kein Trend)
        """
        self.symbol = symbol
        self.current_price = initial_price
        self.volatility = volatility
        self.trend_strength = trend_strength

        self.price_history: List[float] = [initial_price]
        self.high_history: List[float] = [initial_price]
        self.low_history: List[float] = [initial_price]
        self.volume_history: List[float] = [1000000]

        self.current_time = datetime.now()

    def generate_next_candle(self) -> Dict:
        """
        Generiert die nächste Kerze (OHLC + Volume)

        Returns:
            Dictionary mit OHLC-Daten
        """
        # Generiere Preisbewegung
        random_return = np.random.normal(self.trend_strength * 0.0001, self.volatility)
        new_price = self.current_price * (1 + random_return)

        # Generiere High und Low
        intrabar_volatility = self.volatility * 0.5
        high = new_price * (1 + abs(np.random.normal(0, intrabar_volatility)))
        low = new_price * (1 - abs(np.random.normal(0, intrabar_volatility)))

        # Stelle sicher, dass High/Low sinnvoll sind
        high = max(high, self.current_price, new_price)
        low = min(low, self.current_price, new_price)

        # Generiere Volumen (variiert zufällig)
        base_volume = 1000000
        volume = base_volume * random.uniform(0.5, 2.0)

        # Aktualisiere History
        self.price_history.append(new_price)
        self.high_history.append(high)
        self.low_history.append(low)
        self.volume_history.append(volume)

        # Begrenze History-Größe
        max_history = 500
        if len(self.price_history) > max_history:
            self.price_history.pop(0)
            self.high_history.pop(0)
            self.low_history.pop(0)
            self.volume_history.pop(0)

        # Aktualisiere aktuellen Preis und Zeit
        self.current_price = new_price
        self.current_time += timedelta(hours=1)  # 1 Stunde pro Kerze

        return {
            'timestamp': self.current_time,
            'open': self.price_history[-2] if len(self.price_history) > 1 else new_price,
            'high': high,
            'low': low,
            'close': new_price,
            'volume': volume
        }

    def get_market_data(self) -> Dict:
        """
        Gibt vollständige Marktdaten zurück

        Returns:
            Dictionary mit Listen von OHLCV-Daten
        """
        return {
            'close': self.price_history.copy(),
            'high': self.high_history.copy(),
            'low': self.low_history.copy(),
            'volume': self.volume_history.copy(),
            'current_price': self.current_price
        }

    def set_trend(self, trend_strength: float):
        """
        Setzt die Trendstärke

        Args:
            trend_strength: -1.0 (Abwärtstrend) bis 1.0 (Aufwärtstrend)
        """
        self.trend_strength = max(-1.0, min(1.0, trend_strength))

    def set_volatility(self, volatility: float):
        """
        Setzt die Volatilität

        Args:
            volatility: Volatilität als Standardabweichung
        """
        self.volatility = max(0.0001, volatility)

    def create_trending_market(self, bars: int = 100, trend_direction: str = 'up'):
        """
        Erstellt einen trendigen Markt

        Args:
            bars: Anzahl der zu generierenden Bars
            trend_direction: 'up' oder 'down'
        """
        self.trend_strength = 0.7 if trend_direction == 'up' else -0.7

        for _ in range(bars):
            self.generate_next_candle()

    def create_ranging_market(self, bars: int = 100):
        """
        Erstellt einen seitwärts laufenden Markt

        Args:
            bars: Anzahl der zu generierenden Bars
        """
        self.trend_strength = 0.0
        self.volatility = 0.0005  # Niedrigere Volatilität für Range

        for _ in range(bars):
            self.generate_next_candle()

    def create_volatile_market(self, bars: int = 100):
        """
        Erstellt einen volatilen Markt

        Args:
            bars: Anzahl der zu generierenden Bars
        """
        self.trend_strength = 0.0
        self.volatility = 0.0020  # Höhere Volatilität

        for _ in range(bars):
            self.generate_next_candle()


def create_sample_data(
    symbol: str = "EURUSD",
    bars: int = 200,
    market_type: str = 'mixed'
) -> MarketDataSimulator:
    """
    Erstellt Beispiel-Marktdaten

    Args:
        symbol: Handelssymbol
        bars: Anzahl der Bars
        market_type: 'trending_up', 'trending_down', 'ranging', 'volatile', 'mixed'

    Returns:
        MarketDataSimulator mit generierten Daten
    """
    simulator = MarketDataSimulator(symbol=symbol)

    if market_type == 'trending_up':
        simulator.create_trending_market(bars, 'up')

    elif market_type == 'trending_down':
        simulator.create_trending_market(bars, 'down')

    elif market_type == 'ranging':
        simulator.create_ranging_market(bars)

    elif market_type == 'volatile':
        simulator.create_volatile_market(bars)

    elif market_type == 'mixed':
        # Erstelle gemischte Marktbedingungen
        segments = bars // 4
        simulator.create_trending_market(segments, 'up')
        simulator.create_ranging_market(segments)
        simulator.create_trending_market(segments, 'down')
        simulator.create_volatile_market(segments)

    return simulator
