#!/usr/bin/bash

set -e

BOT_USER=eolbot
BOT_USER_UID=5678
BOT_DATA_VOLUME=eolbot_data
BOT_LOG_DIR=/var/log/eolbot

if [ ! -f '/etc/debian_version' ]; then
    echo "This script supports only Debian-based Linux distributions."
    exit 1
fi

if ! which docker >/dev/null 2>&1; then
    echo "Docker is not installed."
    exit 1
fi

echo Creating user $BOT_USER...
adduser -u $BOT_USER_UID --disabled-password --disabled-login --gecos "" $BOT_USER

echo Creating $BOT_DATA_VOLUME volume in Docker...
docker volume create $BOT_DATA_VOLUME >/dev/null
chown $BOT_USER:$BOT_USER /var/lib/docker/volumes/$BOT_DATA_VOLUME/_data

echo Creating $BOT_LOG_DIR for logs...
mkdir -p $BOT_LOG_DIR
chown $BOT_USER:$BOT_USER $BOT_LOG_DIR
chmod 750 $BOT_LOG_DIR

echo Configuring Logrotate...
cp etc/logrotate.d/* /etc/logrotate.d/

echo Done
