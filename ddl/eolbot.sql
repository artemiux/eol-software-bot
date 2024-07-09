CREATE TABLE users (
    id          INTEGER  PRIMARY KEY AUTOINCREMENT,
    telegram_id INTEGER  UNIQUE
                         NOT NULL,
    is_active   INTEGER  DEFAULT (0)
                         NOT NULL
                         CHECK (is_active IN (0, 1)),
    created_on  DATETIME NOT NULL
                         DEFAULT (datetime('now'))
);
