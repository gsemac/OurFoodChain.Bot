-- Add a table for storing records of zone changes.
CREATE TABLE IF NOT EXISTS ZoneRecords(species_id INTEGER, zone_id INTEGER, reason TEXT, timestamp INTEGER, record_type INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id), FOREIGN KEY(zone_id) REFERENCES Zones(id));

-- Add records for all existing species-zone relationships.
INSERT INTO ZoneRecords(species_id, zone_id, reason, timestamp, record_type) SELECT species_id, zone_id, "" as reason, timestamp, '1' AS record_type FROM SpeciesZones