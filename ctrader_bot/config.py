"""
Konfigurationsdatei für den cTrader Bot
"""

# Verfügbare Strategien
AVAILABLE_STRATEGIES = {
    'trend_following': {
        'name': 'Trend Following',
        'description': 'Folgt Markttrends mit Moving Average Crossover',
        'best_for': 'Trending Markets',
        'params': {
            'fast_ma_period': 10,
            'slow_ma_period': 30,
            'use_ema': True,
            'trend_filter_period': 200
        }
    },
    'mean_reversion': {
        'name': 'Mean Reversion',
        'description': 'Handelt bei Abweichungen vom Durchschnitt',
        'best_for': 'Ranging Markets',
        'params': {
            'bb_period': 20,
            'bb_std_dev': 2.0,
            'rsi_period': 14,
            'rsi_oversold': 30.0,
            'rsi_overbought': 70.0
        }
    },
    'breakout': {
        'name': 'Breakout',
        'description': 'Handelt bei Ausbrüchen aus Konsolidierungsphasen',
        'best_for': 'Volatile Markets',
        'params': {
            'lookback_period': 20,
            'breakout_threshold': 0.0001,
            'volume_multiplier': 1.5,
            'consolidation_threshold': 0.5
        }
    },
    'grid_trading': {
        'name': 'Grid Trading',
        'description': 'Platziert Orders in einem gleichmäßigen Raster',
        'best_for': 'Sideways Markets',
        'params': {
            'grid_levels': 5,
            'grid_spacing': 0.0010,
            'take_profit_pips': 0.0015,
            'max_positions': 3
        }
    },
    'rsi': {
        'name': 'RSI Strategy',
        'description': 'Handelt basierend auf RSI-Signalen und Divergenzen',
        'best_for': 'All Market Conditions',
        'params': {
            'rsi_period': 14,
            'oversold_level': 30.0,
            'overbought_level': 70.0,
            'extreme_oversold': 20.0,
            'extreme_overbought': 80.0,
            'use_divergence': True
        }
    }
}

# Standard Risk Management Einstellungen
DEFAULT_RISK_CONFIG = {
    'max_risk_per_trade': 0.02,      # 2% Risiko pro Trade
    'max_position_size': 0.1,        # 10% des Kontos als maximale Position
    'max_daily_loss': 0.05,          # 5% maximaler Tagesverlust
    'use_trailing_stop': True,       # Trailing Stop verwenden
}

# Standard Bot-Einstellungen
DEFAULT_BOT_CONFIG = {
    'symbol': 'EURUSD',
    'timeframe': 'H1',              # M1, M5, M15, H1, H4, D1
    'initial_balance': 10000.0,
    'update_interval': 60,          # Sekunden zwischen Updates
}

# Trading-Zeitfenster (optional)
TRADING_HOURS = {
    'enabled': False,               # Zeitfenster-Beschränkung aktivieren
    'start_hour': 8,                # Handelsstart (UTC)
    'end_hour': 18,                 # Handelsende (UTC)
    'trading_days': [0, 1, 2, 3, 4] # 0=Montag, 4=Freitag
}


def get_strategy_config(strategy_name: str) -> dict:
    """
    Gibt die Konfiguration für eine bestimmte Strategie zurück

    Args:
        strategy_name: Name der Strategie

    Returns:
        Dictionary mit Strategiekonfiguration
    """
    strategy = AVAILABLE_STRATEGIES.get(strategy_name.lower())

    if not strategy:
        raise ValueError(
            f"Strategie '{strategy_name}' nicht gefunden. "
            f"Verfügbare Strategien: {', '.join(AVAILABLE_STRATEGIES.keys())}"
        )

    return {
        'strategy_params': strategy['params'],
        **DEFAULT_RISK_CONFIG
    }


def list_strategies():
    """
    Zeigt alle verfügbaren Strategien an
    """
    print("\n=== Verfügbare Trading-Strategien ===\n")

    for key, strategy in AVAILABLE_STRATEGIES.items():
        print(f"📊 {strategy['name']} ({key})")
        print(f"   Beschreibung: {strategy['description']}")
        print(f"   Geeignet für: {strategy['best_for']}")
        print(f"   Parameter: {len(strategy['params'])} konfigurierbare Optionen")
        print()


def create_custom_config(
    strategy_name: str,
    symbol: str = 'EURUSD',
    timeframe: str = 'H1',
    initial_balance: float = 10000.0,
    risk_per_trade: float = 0.02,
    **kwargs
) -> dict:
    """
    Erstellt eine benutzerdefinierte Konfiguration

    Args:
        strategy_name: Name der Strategie
        symbol: Handelssymbol
        timeframe: Zeitrahmen
        initial_balance: Startkapital
        risk_per_trade: Risiko pro Trade (Dezimalzahl)
        **kwargs: Zusätzliche Parameter

    Returns:
        Vollständige Bot-Konfiguration
    """
    base_config = get_strategy_config(strategy_name)

    config = {
        'symbol': symbol,
        'timeframe': timeframe,
        'initial_balance': initial_balance,
        'max_risk_per_trade': risk_per_trade,
        **base_config
    }

    # Überschreibe mit benutzerdefinierten Parametern
    config.update(kwargs)

    return config
