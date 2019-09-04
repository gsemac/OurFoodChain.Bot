-- Add support for Favorites.
CREATE TABLE IF NOT EXISTS Favorites(user_id INTEGER, species_id INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id), UNIQUE(user_id, species_id));