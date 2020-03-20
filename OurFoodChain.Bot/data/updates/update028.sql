-- Add a table for storing zone aliases.
CREATE TABLE IF NOT EXISTS ZoneAliases(zone_id INTEGER, alias TEXT, FOREIGN KEY(zone_id) REFERENCES Zones(zone_id), UNIQUE(zone_id, alias));