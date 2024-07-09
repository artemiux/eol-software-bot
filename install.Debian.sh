#!/usr/bin/bash

set -e

USER=eolbot
BOT_LOG=/var/log/eolbot.log
INSTALL_DIR=/opt/EOLSoftwareBot

if [ ! -f '/etc/debian_version' ]; then
    echo "This script supports only Debian-based Linux distributions."
    exit 1
fi

apt update
apt install git python3 rsync sqlite3

mkdir -v $INSTALL_DIR \
    && echo "$INSTALL_DIR directory created successfully"

echo Creating user $USER...
adduser \
    --system \
    --home $INSTALL_DIR --no-create-home \
    --disabled-password --disabled-login \
    --group \
    $USER

echo Copying files...
rsync -av --exclude=".git*" --exclude="install.*" ./* $INSTALL_DIR/

echo Cloning the `release-data` repository...
git clone https://github.com/endoflife-date/release-data.git "$INSTALL_DIR/var/release-data"

echo Creating a virtual environment...
/usr/bin/python3 -m venv "$INSTALL_DIR/.venv" \
    && "$INSTALL_DIR/.venv/bin/python3" -m pip install -r "$INSTALL_DIR/requirements.txt"

echo Creating an empty database...
if [ -f "$INSTALL_DIR/var/eolbot.db" ]; then
    echo -e "\033[33mDatabase $INSTALL_DIR/var/eolbot.db already exists. Omitted creating.\033[0m"
else
    /usr/bin/sqlite3 -echo "$INSTALL_DIR/var/eolbot.db" ".databases" \
        && /usr/bin/sqlite3 -echo "$INSTALL_DIR/var/eolbot.db" < "$INSTALL_DIR/ddl/eolbot.sql"
fi

echo Setting filesystem permissions...
touch $BOT_LOG
chown root:$USER $BOT_LOG
chmod 660 $BOT_LOG
chown -R $USER:$USER "$INSTALL_DIR/var"
chmod 755 "$INSTALL_DIR/check.sh"

echo Creating symlinks...
ln -v -s "$INSTALL_DIR/etc/cron.d/eolbot" /etc/cron.d/
ln -v -s "$INSTALL_DIR/etc/logrotate.d/eolbot" /etc/logrotate.d/
ln -v -s "$INSTALL_DIR/etc/systemd/system/eolbot.service" /etc/systemd/system/

echo -e "\n\033[33mTo complete the installation do the following:\033[0m\n"
echo -e "1) Copy $INSTALL_DIR/config.yaml.default to $INSTALL_DIR/config.yaml"
echo -e "2) Add your bot's API token to $INSTALL_DIR/config.yaml"
echo -e "3) Make sure all the parts of the bot work well by running:"
echo -e "\tsudo -u eolbot /opt/EOLSoftwareBot/.venv/bin/python3 /opt/EOLSoftwareBot/src/update.py"
echo -e "\tsudo -u eolbot /opt/EOLSoftwareBot/.venv/bin/python3 /opt/EOLSoftwareBot/src/send.py"
echo -e "4) Run the bot:\n\tsudo -u eolbot /opt/EOLSoftwareBot/.venv/bin/python3 /opt/EOLSoftwareBot/main.py"
echo -e "5) If it works well, enable and start the bot service:"
echo -e "\tsystemctl enable eolbot.service\n\tsystemctl start eolbot.service\n"
