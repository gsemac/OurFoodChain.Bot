-- The following used to occur when a gotchi was created or accessed, so the table didn't exist if no users used the gotchi features.
-- Since gotchi features have been extended since this was added (battles, trades, etc.), it's safer to ensure that the table always exists.
-- This is especially important, since some updates (such as #18 and #19) assume that the table already exists.
CREATE TABLE IF NOT EXISTS Gotchi(id INTEGER PRIMARY KEY AUTOINCREMENT, species_id INTEGER, name TEXT, owner_id INTEGER, fed_ts INTEGER, born_ts INTEGER, died_ts INTEGER, evolved_ts INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id));
ALTER TABLE Gotchi ADD COLUMN level INTEGER;
ALTER TABLE Gotchi ADD COLUMN exp REAL;