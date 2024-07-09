import asyncio
import datetime
import logging
import logginghelper
import os
from confighelper import ConfigHelper
from dbmanager import DBManager, User
from report import Report
from telegram import Bot
from telegram.error import Forbidden
from telegram.constants import ParseMode


"""
Sends reports to all subscribers.
"""


confighelper = ConfigHelper()

logginghelper.setup_env(confighelper)
logginghelper.setup_logging(confighelper, logger_name=__name__)
logger = logging.getLogger(__name__)

db = DBManager(confighelper)


def unsubscribe_user(telegram_id) -> None:
    session = db.new_session()
    try:
        db_user = session.query(User).filter_by(telegram_id=telegram_id).first()
        if db_user:
            db_user.is_active = False
            session.commit()
    except Exception as e:
        session.rollback()
        logger.error(f"Exception while unsubscribing user {telegram_id}: {e}")
        raise e
    finally:
        session.remove()


async def send_message(bot, telegram_id, text) -> None:
    try:
        await bot.send_message(telegram_id, text, disable_web_page_preview=True, parse_mode=ParseMode.HTML)
        logger.debug(f"Report was sent to {telegram_id}")
    except Forbidden:
        unsubscribe_user(telegram_id)
        logger.info(f"User {telegram_id} was unsubscribed because they'd blocked the bot")
    except Exception as e:
        logger.error(f"Exception while sending a report to user {telegram_id}: {e}")


async def send(recipients, text) -> None:
    logger.info(f"A new report will be sent to {len(recipients)} users")

    bot = Bot(confighelper.config['bot']['api_token'])

    # Send messages asynchronously.
    max_concurrent_messages = confighelper.config['bot']['max_concurrent_messages']
    tasks = []
    for recipient in recipients:
        task = asyncio.create_task(
            send_message(bot, recipient.telegram_id, text)
        )
        tasks.append(task)
        # Do not send more than `max_concurrent_messages` messages at once.
        if len(tasks) >= max_concurrent_messages:
            logger.debug('max_concurrent_messages reached, waiting for tasks')
            await asyncio.wait(tasks)
            tasks = []

    # Send remaining messages if there are any.
    if tasks:
        logger.debug('Waiting for remaining tasks')
        await asyncio.wait(tasks)

    await bot.shutdown()
    logger.info("Done")


def main():
    report_start_date = datetime.date.today()
    report_end_date = report_start_date + datetime.timedelta(days=confighelper.config['report']['days_to_cover'])
    try:
        report = Report(report_start_date, report_end_date, logger)
        logger.debug(f"Report: {report}")
    except Exception as e:
        logger.error(f"Exception while creating a report: {e}")
        return

    session = db.new_session()
    report_recipients = []
    try:
        if os.getenv('EOL_BOT_ENVIRONMENT') == 'Development':
            if not confighelper.config['bot']['admin_chat_id']:
                raise Exception('You must set `admin_chat_id` in config.yaml if development environment is enabled')
            admin_recipient = session.query(User)\
                .filter_by(telegram_id=confighelper.config['bot']['admin_chat_id']).first()
            if not admin_recipient:
                raise Exception('Development environment requires the user specified in `admin_chat_id` to exist in the database')
            report_recipients.append(admin_recipient)
        else:
            report_recipients = session.query(User).filter_by(is_active=True).all()
    except Exception as e:
        logger.error(f"Exception while getting recipients: {e}")
        return
    finally:
        session.remove()

    try:
        asyncio.run(
            send(report_recipients, str(report))
        )
    except Exception as e:
        logger.error(f"Exception while sending a report: {e}")


if __name__ == '__main__':
    main()
