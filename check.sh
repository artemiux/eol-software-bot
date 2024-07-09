#!/usr/bin/bash

systemctl stop eolbot.service

export EOL_BOT_ENVIRONMENT=Development
sudo -E -u eolbot .venv/bin/python3 src/update.py
sudo -E -u eolbot .venv/bin/python3 src/send.py
sudo -E -u eolbot .venv/bin/python3 main.py

echo -e "\n\033[33mDo not forget to start the service!\033[0m\n"
