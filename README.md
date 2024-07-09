# EOL Software Bot

This is the source code for the Telegram bot [@eol_software_bot](https://t.me/eol_software_bot) that tracks changes to software end-of-life (EOL) dates and sends a report on upcoming events every Monday. It supports hundreds of products.

## Quick Start

Send the command `/subscribe` to the bot [@eol_software_bot](https://t.me/eol_software_bot).

## How it works

The data source is a local copy of the [@release-data](https://github.com/endoflife-date/release-data) repository. First, synchronization is performed, and then the json files in the [releases](https://github.com/endoflife-date/release-data/tree/main/releases) directory are parsed.

## Self-installation

If you have a Debian-compatible operating system, you can simply run the following commands and follow the instructions on the screen:

```
chmod +x install.Debian.sh
./install.Debian.sh
```

Support for other operating systems is not currently planned, but you can use [install.Debian.sh](install.Debian.sh) as a sample to create your own.

## Preparing the development environment

1. Install all missing packages:

```
apt update
apt -y install git python3 rsync sqlite3
```

2. Create a virtual environment and install all dependencies:

```
python3 -m venv .venv
.venv/bin/python3 -m pip install -r requirements.txt
```

3. Clone [@release-data](https://github.com/endoflife-date/release-data) repository:

```
git clone https://github.com/endoflife-date/release-data var/release-data
```

4. Create your own bot configuration:

```
cp config.yaml.default config.yaml
```

5. Add your bot's token and your Telegram ID to `config.yaml`

6. Activate the development environment:

```
cp .env.development.example .env
```
