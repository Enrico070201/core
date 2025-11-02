"""
Technische Indikatoren für Trading-Strategien
"""

import numpy as np
from typing import List, Tuple


class Indicators:
    """
    Sammlung von technischen Indikatoren für die Marktanalyse
    """

    @staticmethod
    def sma(prices: List[float], period: int) -> float:
        """
        Simple Moving Average (SMA)

        Args:
            prices: Liste von Preisen
            period: Anzahl der Perioden

        Returns:
            SMA-Wert
        """
        if len(prices) < period:
            return 0.0
        return sum(prices[-period:]) / period

    @staticmethod
    def ema(prices: List[float], period: int) -> float:
        """
        Exponential Moving Average (EMA)

        Args:
            prices: Liste von Preisen
            period: Anzahl der Perioden

        Returns:
            EMA-Wert
        """
        if len(prices) < period:
            return 0.0

        prices_array = np.array(prices)
        multiplier = 2 / (period + 1)

        # Beginne mit SMA
        ema = np.mean(prices_array[:period])

        # Berechne EMA für verbleibende Werte
        for price in prices_array[period:]:
            ema = (price * multiplier) + (ema * (1 - multiplier))

        return float(ema)

    @staticmethod
    def rsi(prices: List[float], period: int = 14) -> float:
        """
        Relative Strength Index (RSI)

        Args:
            prices: Liste von Preisen
            period: RSI-Periode (Standard: 14)

        Returns:
            RSI-Wert (0-100)
        """
        if len(prices) < period + 1:
            return 50.0  # Neutraler Wert

        # Berechne Preisänderungen
        deltas = np.diff(prices)

        # Trenne Gewinne und Verluste
        gains = np.where(deltas > 0, deltas, 0)
        losses = np.where(deltas < 0, -deltas, 0)

        # Durchschnittliche Gewinne und Verluste
        avg_gain = np.mean(gains[-period:])
        avg_loss = np.mean(losses[-period:])

        if avg_loss == 0:
            return 100.0

        rs = avg_gain / avg_loss
        rsi = 100 - (100 / (1 + rs))

        return float(rsi)

    @staticmethod
    def bollinger_bands(prices: List[float], period: int = 20, std_dev: float = 2.0) -> Tuple[float, float, float]:
        """
        Bollinger Bänder

        Args:
            prices: Liste von Preisen
            period: Anzahl der Perioden
            std_dev: Anzahl der Standardabweichungen

        Returns:
            Tuple (upper_band, middle_band, lower_band)
        """
        if len(prices) < period:
            return (0.0, 0.0, 0.0)

        recent_prices = prices[-period:]
        middle = np.mean(recent_prices)
        std = np.std(recent_prices)

        upper = middle + (std_dev * std)
        lower = middle - (std_dev * std)

        return (float(upper), float(middle), float(lower))

    @staticmethod
    def macd(prices: List[float], fast: int = 12, slow: int = 26, signal: int = 9) -> Tuple[float, float, float]:
        """
        Moving Average Convergence Divergence (MACD)

        Args:
            prices: Liste von Preisen
            fast: Schnelle EMA-Periode
            slow: Langsame EMA-Periode
            signal: Signal-Linien-Periode

        Returns:
            Tuple (macd_line, signal_line, histogram)
        """
        if len(prices) < slow:
            return (0.0, 0.0, 0.0)

        ema_fast = Indicators.ema(prices, fast)
        ema_slow = Indicators.ema(prices, slow)

        macd_line = ema_fast - ema_slow

        # Vereinfachte Signal-Linie (sollte EMA von MACD sein)
        signal_line = macd_line * 0.9  # Approximation

        histogram = macd_line - signal_line

        return (macd_line, signal_line, histogram)

    @staticmethod
    def atr(highs: List[float], lows: List[float], closes: List[float], period: int = 14) -> float:
        """
        Average True Range (ATR) - Volatilitätsindikator

        Args:
            highs: Liste von Hochs
            lows: Liste von Tiefs
            closes: Liste von Schlusskursen
            period: Anzahl der Perioden

        Returns:
            ATR-Wert
        """
        if len(highs) < period + 1 or len(lows) < period + 1 or len(closes) < period + 1:
            return 0.0

        true_ranges = []
        for i in range(1, len(closes)):
            high_low = highs[i] - lows[i]
            high_close = abs(highs[i] - closes[i-1])
            low_close = abs(lows[i] - closes[i-1])

            tr = max(high_low, high_close, low_close)
            true_ranges.append(tr)

        if len(true_ranges) < period:
            return 0.0

        atr = np.mean(true_ranges[-period:])
        return float(atr)

    @staticmethod
    def stochastic(highs: List[float], lows: List[float], closes: List[float], period: int = 14) -> Tuple[float, float]:
        """
        Stochastic Oscillator

        Args:
            highs: Liste von Hochs
            lows: Liste von Tiefs
            closes: Liste von Schlusskursen
            period: Anzahl der Perioden

        Returns:
            Tuple (%K, %D)
        """
        if len(highs) < period or len(lows) < period or len(closes) < period:
            return (50.0, 50.0)

        highest_high = max(highs[-period:])
        lowest_low = min(lows[-period:])
        current_close = closes[-1]

        if highest_high == lowest_low:
            k = 50.0
        else:
            k = 100 * (current_close - lowest_low) / (highest_high - lowest_low)

        # %D ist der gleitende Durchschnitt von %K
        d = k * 0.9  # Vereinfachte Berechnung

        return (float(k), float(d))
