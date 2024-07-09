import html
import logging
import requests
import time


"""
A logger handler that can be used to send log messages to a Telegram chat.
"""


class TelegramHandler(logging.Handler):

    def __init__(self, token, chat_id, level=None, pause_between_messages=0):
        super().__init__()
        self.telegram_api_url = f"https://api.telegram.org/bot{token}/sendMessage"
        self.chat_id = chat_id
        if level:
            self.setLevel(level)
        self.pause_between_messages = pause_between_messages
        self._last_record_time = None


    def emit(self, record) -> None:
        if self.pause_between_messages > 0:
            current_time = int(time.time())
            if self._last_record_time \
                and (current_time - self._last_record_time) < self.pause_between_messages:
                return
            self._last_record_time = current_time
        log_entry = '<pre>%s</pre>' % html.escape(self.format(record))
        # Telegram Bot API allows only 4096 characters in one message.
        log_entry = log_entry[-4096:]
        payload = {
            'chat_id': self.chat_id,
            'text': log_entry,
            'parse_mode': 'HTML'
        }
        try:
            requests.post(self.telegram_api_url, json=payload)
        except Exception as e:
            print(e)
