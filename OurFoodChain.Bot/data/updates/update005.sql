﻿CREATE TABLE IF NOT EXISTS Predates(species_id INTEGER, eats_id INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id), FOREIGN KEY(eats_id) REFERENCES Species(id), PRIMARY KEY(species_id, eats_id));