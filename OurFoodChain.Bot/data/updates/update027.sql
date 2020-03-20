-- Add a column for storing zone flags.
ALTER TABLE Zones ADD COLUMN flags INTEGER;
-- Add a table for storing zone aliases.
CREATE TABLE IF NOT EXISTS ZoneAliases(zone_id INTEGER, alias TEXT, FOREIGN KEY(zone_id) REFERENCES Zones(zone_id), UNIQUE(zone_id, alias));
-- Add a table for storing zone fields.
CREATE TABLE IF NOT EXISTS ZoneFields(zone_id INTEGER, name TEXT, value TEXT, FOREIGN KEY(zone_id) REFERENCES Zones(zone_id), UNIQUE(zone_id, name));