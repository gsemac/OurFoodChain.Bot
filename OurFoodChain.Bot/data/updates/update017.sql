-- Adds support for relationships.
CREATE TABLE IF NOT EXISTS Relationships(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, description TEXT, UNIQUE(name));
CREATE TABLE IF NOT EXISTS SpeciesRelationships(species1_id INTEGER, species2_id INTEGER, relationship_id INTEGER, FOREIGN KEY(species1_id) REFERENCES Species(id), FOREIGN KEY(species2_id) REFERENCES Species(id), FOREIGN KEY(relationship_id) REFERENCES Relationships(id), UNIQUE(species1_id, species2_id));
-- Insert default relationships.
INSERT OR IGNORE INTO Relationships(name, description) VALUES("parasitism", "A relationship where one organism lives in or on another, receiving benefits at the expense of the host.");
INSERT OR IGNORE INTO Relationships(name, description) VALUES("mutualism", "A relationship where both organisms benefit from interacting with the other.");
INSERT OR IGNORE INTO Relationships(name, description) VALUES("commensalism", "A relationship where one organism benefits from interacting with another, while the other organism is unaffected.");