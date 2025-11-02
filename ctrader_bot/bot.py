"""
Haupt-Bot-Klasse für automatisches Trading
"""

import logging
import time
from typing import Dict, Optional, List
from datetime import datetime
import json

from .strategies import (
    BaseStrategy,
    TrendFollowingStrategy,
    MeanReversionStrategy,
    BreakoutStrategy,
    GridTradingStrategy,
    RSIStrategy
)
from .utils.risk_manager import RiskManager


class TradingBot:
    """
    Automatischer Trading Bot mit verschiedenen Strategien
    """

    def __init__(
        self,
        strategy_name: str,
        symbol: str = "EURUSD",
        timeframe: str = "H1",
        initial_balance: float = 10000.0,
        config: Optional[Dict] = None
    ):
        """
        Initialisiert den Trading Bot

        Args:
            strategy_name: Name der zu verwendenden Strategie
            symbol: Handelssymbol
            timeframe: Zeitrahmen
            initial_balance: Startkapital
            config: Optionale Konfiguration für Strategie und Risk Management
        """
        # Logging Setup
        self._setup_logging()

        self.symbol = symbol
        self.timeframe = timeframe
        self.config = config or {}

        # Initialisiere Risk Manager
        self.risk_manager = RiskManager(
            account_balance=initial_balance,
            max_risk_per_trade=self.config.get('max_risk_per_trade', 0.02),
            max_position_size=self.config.get('max_position_size', 0.1),
            max_daily_loss=self.config.get('max_daily_loss', 0.05),
            use_trailing_stop=self.config.get('use_trailing_stop', True)
        )

        # Initialisiere Strategie
        self.strategy = self._create_strategy(strategy_name)

        # Bot-Status
        self.is_running = False
        self.last_update = None

        # Trade History
        self.trade_history: List[Dict] = []

        self.logger.info(
            f"Trading Bot initialisiert: {strategy_name} auf {symbol} ({timeframe}), "
            f"Startkapital: {initial_balance:.2f}"
        )

    def _setup_logging(self):
        """
        Richtet Logging ein
        """
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
            handlers=[
                logging.FileHandler('/home/user/core/ctrader_bot/trading_bot.log'),
                logging.StreamHandler()
            ]
        )
        self.logger = logging.getLogger('TradingBot')

    def _create_strategy(self, strategy_name: str) -> BaseStrategy:
        """
        Erstellt die gewählte Strategie

        Args:
            strategy_name: Name der Strategie

        Returns:
            Instanz der Strategie

        Raises:
            ValueError: Wenn Strategie nicht existiert
        """
        strategies = {
            'trend_following': TrendFollowingStrategy,
            'mean_reversion': MeanReversionStrategy,
            'breakout': BreakoutStrategy,
            'grid_trading': GridTradingStrategy,
            'rsi': RSIStrategy
        }

        strategy_class = strategies.get(strategy_name.lower())

        if not strategy_class:
            available = ', '.join(strategies.keys())
            raise ValueError(
                f"Strategie '{strategy_name}' nicht gefunden. "
                f"Verfügbare Strategien: {available}"
            )

        # Erstelle Strategie mit Config
        strategy_config = self.config.get('strategy_params', {})
        return strategy_class(
            symbol=self.symbol,
            timeframe=self.timeframe,
            **strategy_config
        )

    def analyze_market(self, market_data: Dict) -> Optional[Dict]:
        """
        Analysiert Marktdaten und generiert Trading-Signal

        Args:
            market_data: Dictionary mit OHLCV-Daten

        Returns:
            Dictionary mit Trade-Informationen oder None
        """
        # Prüfe ob Trading erlaubt ist
        if not self.risk_manager.can_trade():
            self.logger.warning("Trading pausiert: Tagesverlust-Limit erreicht")
            return None

        # Hole Signal von Strategie
        signal, params = self.strategy.analyze(market_data)

        if signal == 'HOLD':
            return None

        current_price = market_data['close'][-1]

        # Verarbeite Signale
        if signal in ['BUY', 'SELL']:
            return self._process_entry_signal(signal, current_price, params, market_data)

        elif signal == 'CLOSE':
            return self._process_exit_signal(current_price)

        return None

    def _process_entry_signal(
        self,
        signal: str,
        current_price: float,
        params: Dict,
        market_data: Dict
    ) -> Optional[Dict]:
        """
        Verarbeitet Entry-Signal

        Args:
            signal: 'BUY' oder 'SELL'
            current_price: Aktueller Preis
            params: Signal-Parameter
            market_data: Marktdaten

        Returns:
            Trade-Dictionary
        """
        if self.strategy.position is not None:
            self.logger.warning(f"Position bereits offen: {self.strategy.position}")
            return None

        direction = 'long' if signal == 'BUY' else 'short'

        # Berechne Stop Loss
        atr = params.get('atr', 0.001)
        stop_loss_multiplier = params.get('stop_loss_multiplier', 2.0)

        stop_loss = self.risk_manager.calculate_stop_loss(
            entry_price=current_price,
            direction=direction,
            atr=atr,
            multiplier=stop_loss_multiplier
        )

        # Berechne Take Profit
        take_profit_ratio = params.get('take_profit_ratio', 2.0)
        take_profit = self.risk_manager.calculate_take_profit(
            entry_price=current_price,
            stop_loss=stop_loss,
            direction=direction,
            risk_reward_ratio=take_profit_ratio
        )

        # Berechne Positionsgröße
        position_size = self.risk_manager.calculate_position_size(
            entry_price=current_price,
            stop_loss=stop_loss
        )

        if position_size == 0:
            self.logger.warning("Positionsgröße ist 0, Trade wird übersprungen")
            return None

        # Erstelle Trade
        trade = {
            'timestamp': datetime.now().isoformat(),
            'action': 'OPEN',
            'direction': direction,
            'symbol': self.symbol,
            'entry_price': current_price,
            'position_size': position_size,
            'stop_loss': stop_loss,
            'take_profit': take_profit,
            'strategy': self.strategy.name
        }

        # Setze Position in Strategie
        self.strategy.set_position(direction, current_price, position_size)

        self.logger.info(
            f"Trade eröffnet: {direction.upper()} {position_size} Lots @ {current_price:.5f}, "
            f"SL: {stop_loss:.5f}, TP: {take_profit:.5f}"
        )

        return trade

    def _process_exit_signal(self, current_price: float) -> Optional[Dict]:
        """
        Verarbeitet Exit-Signal

        Args:
            current_price: Aktueller Preis

        Returns:
            Trade-Dictionary
        """
        if self.strategy.position is None:
            return None

        # Berechne Profit/Loss
        pnl = self.strategy.close_position(current_price)

        # Aktualisiere Risk Manager
        self.risk_manager.update_balance(pnl)

        # Erstelle Exit-Trade
        trade = {
            'timestamp': datetime.now().isoformat(),
            'action': 'CLOSE',
            'direction': self.strategy.position,
            'symbol': self.symbol,
            'exit_price': current_price,
            'pnl': pnl,
            'strategy': self.strategy.name,
            'balance': self.risk_manager.account_balance
        }

        self.trade_history.append(trade)

        self.logger.info(
            f"Position geschlossen @ {current_price:.5f}, "
            f"PnL: {pnl:+.2f}, Balance: {self.risk_manager.account_balance:.2f}"
        )

        return trade

    def get_status(self) -> Dict:
        """
        Gibt den aktuellen Bot-Status zurück

        Returns:
            Status-Dictionary
        """
        return {
            'bot': {
                'running': self.is_running,
                'last_update': self.last_update,
                'symbol': self.symbol,
                'timeframe': self.timeframe
            },
            'strategy': self.strategy.get_status(),
            'risk_management': self.risk_manager.get_statistics(),
            'trade_history_count': len(self.trade_history)
        }

    def save_state(self, filepath: str = '/home/user/core/ctrader_bot/bot_state.json'):
        """
        Speichert Bot-Status in Datei

        Args:
            filepath: Pfad zur Ausgabedatei
        """
        state = {
            'status': self.get_status(),
            'trade_history': self.trade_history,
            'timestamp': datetime.now().isoformat()
        }

        with open(filepath, 'w') as f:
            json.dump(state, f, indent=2)

        self.logger.info(f"Status gespeichert: {filepath}")

    def load_state(self, filepath: str = '/home/user/core/ctrader_bot/bot_state.json'):
        """
        Lädt Bot-Status aus Datei

        Args:
            filepath: Pfad zur Eingabedatei
        """
        try:
            with open(filepath, 'r') as f:
                state = json.load(f)

            self.trade_history = state.get('trade_history', [])
            self.logger.info(f"Status geladen: {filepath}")

        except FileNotFoundError:
            self.logger.warning(f"Status-Datei nicht gefunden: {filepath}")

    def reset_daily_stats(self):
        """
        Setzt tägliche Statistiken zurück (sollte täglich aufgerufen werden)
        """
        self.risk_manager.reset_daily_stats()
        self.logger.info("Tägliche Statistiken zurückgesetzt")

    def __str__(self):
        return (
            f"TradingBot(strategy={self.strategy.name}, "
            f"symbol={self.symbol}, "
            f"balance={self.risk_manager.account_balance:.2f})"
        )
