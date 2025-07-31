# EOL Software Bot

This is the source code for the Telegram bot [@eol_software_bot](https://t.me/eol_software_bot) that tracks changes to software end-of-life (EOL) dates and sends a report on upcoming events every Monday. It supports hundreds of products.

## Quick Start

Send the command `/subscribe` to the bot [@eol_software_bot](https://t.me/eol_software_bot).

## How it works

The data source is a local copy of the [@release-data](https://github.com/endoflife-date/release-data) repository. First, synchronization is performed, and then the json files in the [releases](https://github.com/endoflife-date/release-data/tree/main/releases) directory are parsed.
