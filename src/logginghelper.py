import logging
import os
from confighelper import ConfigHelper
from dotenv import load_dotenv
from telegramhandler import TelegramHandler


def setup_env(confighelper: ConfigHelper) -> None:
    dotenv_path = os.path.join(confighelper.get_base_dir(), '.env')
    if os.path.exists(dotenv_path):
        load_dotenv(dotenv_path=dotenv_path)


def setup_logging(confighelper: ConfigHelper, logger_name: str) -> None:
    logger = logging.getLogger(logger_name)
    logger.setLevel(logging.DEBUG) # Minimal level for all handlers.

    formatter = logging.Formatter(confighelper.config['logs']['format'])

    stream_handler = logging.StreamHandler()
    stream_handler.setFormatter(formatter)
    logger.addHandler(stream_handler)

    if os.getenv('EOL_BOT_ENVIRONMENT') == 'Development':
        stream_handler.setLevel(logging.DEBUG)
    else:
        stream_handler.setLevel(logging.INFO)

        file_handler = logging.FileHandler(confighelper.get_log_path())
        file_handler.setFormatter(formatter)
        file_handler.setLevel(logging.INFO)
        logger.addHandler(file_handler)

        if confighelper.config['bot']['admin_chat_id']:
            telegram_handler = TelegramHandler(confighelper.config['bot']['api_token'],
                                               confighelper.config['bot']['admin_chat_id'],
                                               pause_between_messages=60)
            telegram_handler.setFormatter(formatter)
            telegram_handler.setLevel(logging.ERROR)
            logger.addHandler(telegram_handler)
