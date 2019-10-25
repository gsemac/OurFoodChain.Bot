-- Initializes the Trophies table for storing records of earned trophies.
CREATE TABLE IF NOT EXISTS Trophies(user_id INTEGER, trophy_name TEXT, timestamp INTEGER, UNIQUE(user_id, trophy_name));