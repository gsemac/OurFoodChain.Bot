-- Zone types are now stored in the database.
-- This allows users to create custom zone types as they see fit.
CREATE TABLE IF NOT EXISTS ZoneTypes(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, icon TEXT, color TEXT, description TEXT, flags INTEGER, UNIQUE(name));

-- Add default zone types.
INSERT OR IGNORE INTO ZoneTypes(name, icon, color) VALUES("terrestrial", "🌳", "#1f8b4c");
INSERT OR IGNORE INTO ZoneTypes(name, icon, color) VALUES("aquatic", "🌊", "#3498db");

-- Add a column for storing the id of the type of the zone (deprecating the "type" column).
ALTER TABLE Zones ADD COLUMN type_id INTEGER REFERENCES ZoneTypes(id);

-- Update the type_id value for all existing zones.
UPDATE Zones SET type_id = (SELECT id FROM ZoneTypes WHERE ZoneTypes.name = Zones.type);

-- Zones can now contain nested zones.
ALTER TABLE Zones ADD COLUMN parent_id INTEGER REFERENCES Zones(id);

-- Zones can now have their own icons independent of their zone type.
ALTER TABLE Zones ADD COLUMN icon TEXT;