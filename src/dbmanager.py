import os
from confighelper import ConfigHelper
from sqlalchemy import create_engine
from sqlalchemy import Boolean, Column, DateTime, Integer
from sqlalchemy.orm import DeclarativeBase
from sqlalchemy.orm import scoped_session, sessionmaker
from sqlalchemy.sql import func


confighelper = ConfigHelper()


class Base(DeclarativeBase):
    pass


class User(Base):

    __tablename__ = 'users'

    id = Column(Integer, primary_key=True, autoincrement=True)
    telegram_id = Column(Integer, unique=True, nullable=False)
    is_active = Column(Boolean, nullable=False, default=0)
    created_on = Column(DateTime, nullable=False, server_default=func.now())


    def __init__(self, telegram_id=None, is_active=None):
        super().__init__()
        if telegram_id is not None:
            self.telegram_id = telegram_id
        if is_active is not None:
            self.is_active = is_active


    def __repr__(self):
        return f"<User(id={self.id}, telegram_id={self.telegram_id}, is_active={self.is_active}, created_on={self.created_on})>"


class DBManager:

    def __init__(self, confighelper: ConfigHelper):
        db_path = confighelper.get_db_path()
        self._engine = create_engine(f"sqlite:///{db_path}")
        # Create the database if it doesn't exist.
        if not os.path.exists(db_path):
            Base.metadata.create_all(self._engine)


    def new_session(self):
        return scoped_session(sessionmaker(bind=self._engine))
