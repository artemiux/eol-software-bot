import logging
import os
import sys
from telegram import Update
from telegram.ext import ApplicationBuilder, ContextTypes, CommandHandler
from telegram.ext.filters import UpdateFilter

# Change the current directory to the location of the script.
# It ensures that all relative paths are resolved correctly.
os.chdir(os.path.dirname(__file__))

# This lines are necessary to be able to import modules from the 'src' directory.
src_path = os.path.join(os.path.dirname(__file__), 'src')
sys.path.append(src_path)

import logginghelper
from confighelper import ConfigHelper
from dbmanager import DBManager, User


confighelper = ConfigHelper()

logginghelper.setup_env(confighelper)
logginghelper.setup_logging(confighelper, logger_name=__name__)
logger = logging.getLogger(__name__)


class DevelopmentEnvironmentFilter(UpdateFilter):
    def __init__(self, config: dict):
        super().__init__()
        if config:
            self.config = config
            self.environment = os.getenv('EOL_BOT_ENVIRONMENT')
            self.logger = logging.getLogger(__name__)


    def filter(self, update: Update) -> bool:
        self.logger.info(f"User {update.effective_chat.id} typed {update.message.text}")
        if self.environment == 'Development'\
                and update.effective_chat.id != self.config['bot']['admin_chat_id']:
            self.logger.warning(f"User {update.effective_chat.id} was ignored due to development environment")
            return False
        return True


async def error_handler(update: object, context: ContextTypes.DEFAULT_TYPE) -> None:
    logger.error(f"Exception while handling an update:", exc_info=context.error)


async def start(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    text = "Welcome! To start receiving EOL reports, type /subscribe. You can cancel your subscription at any time."
    await context.bot.send_message(chat_id=update.effective_chat.id, text=text)


async def subscribe(update: Update, context: ContextTypes.DEFAULT_TYPE, db: DBManager) -> None:
    session = db.new_session()
    user=User(update.effective_chat.id, True)
    try:
        db_user = session.query(User).filter_by(telegram_id=user.telegram_id).first()
        if not db_user:
            session.add(user)
        else:
            db_user.is_active = True
        session.commit()
        await context.bot.send_message(
            chat_id=update.effective_chat.id,
            text='You have subscribed!'
        )
    except Exception as e:
        session.rollback()
        logger.error(f"Exception while subscribing user {update.effective_chat.id}: {e}")
        await context.bot.send_message(
            chat_id=update.effective_chat.id,
            text='Something went wrong. Please try again later.'
        )
    finally:
        session.remove()

    logger.info(f"User {update.effective_chat.id} subscribed")


async def unsubscribe(update: Update, context: ContextTypes.DEFAULT_TYPE, db: DBManager) -> None:
    session = db.new_session()
    user=User(update.effective_chat.id, False)
    try:
        db_user = session.query(User).filter_by(telegram_id=user.telegram_id).first()
        if not db_user:
            session.add(user)
        else:
            db_user.is_active = False
        session.commit()
        await context.bot.send_message(
            chat_id=update.effective_chat.id,
            text='You have unsubscribed!'
        )
    except Exception as e:
        session.rollback()
        logger.error(f"Exception while unsubscribing user {update.effective_chat.id}: {e}")
        await context.bot.send_message(
            chat_id=update.effective_chat.id,
            text='Something went wrong. Please try again later.'
        )
    finally:
        session.remove()

    logger.info(f"User {update.effective_chat.id} ubsubscribed")


def main():
    application = ApplicationBuilder()\
        .token(confighelper.config['bot']['api_token']).build()

    application.add_error_handler(error_handler)

    db = DBManager(confighelper)
    start_handler = CommandHandler(
        command='start',
        callback=start,
        filters=DevelopmentEnvironmentFilter(confighelper.config),
        has_args=False
    )
    subscribe_handler = CommandHandler(
        command='subscribe',
        callback=lambda update, context: subscribe(update, context, db),
        filters=DevelopmentEnvironmentFilter(confighelper.config),
        has_args=False
    )
    unsubscribe_handler = CommandHandler(
        command='unsubscribe',
        callback=lambda update, context: unsubscribe(update, context, db),
        filters=DevelopmentEnvironmentFilter(confighelper.config),
        has_args=False
    )
    application.add_handler(start_handler)
    application.add_handler(subscribe_handler)
    application.add_handler(unsubscribe_handler)

    logger.info("EOL bot has started")
    application.run_polling(allowed_updates=Update.ALL_TYPES, timeout=30)
    logger.info("EOL bot has stopped")


if __name__ == '__main__':
    main()
