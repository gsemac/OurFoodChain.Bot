BEGIN TRANSACTION;

-- Create the TaxaRanks ranks table and populate it with default values.

CREATE TABLE TaxaRanks(id INTEGER PRIMARY KEY AUTOINCREMENT, parent_id INTEGER, name TEXT, FOREIGN KEY(parent_id) REFERENCES TaxaRanks(id) ON DELETE SET NULL, UNIQUE(name));

INSERT INTO TaxaRanks(name) VALUES("domain");
INSERT INTO TaxaRanks(parent_id, name) SELECT last_insert_rowid(), "kingdom";
INSERT INTO TaxaRanks(parent_id, name) SELECT last_insert_rowid(), "phylum";
INSERT INTO TaxaRanks(parent_id, name) SELECT last_insert_rowid(), "class";
INSERT INTO TaxaRanks(parent_id, name) SELECT last_insert_rowid(), "order";
INSERT INTO TaxaRanks(parent_id, name) SELECT last_insert_rowid(), "family";
INSERT INTO TaxaRanks(parent_id, name) SELECT last_insert_rowid(), "genus";
INSERT INTO TaxaRanks(parent_id, name) SELECT last_insert_rowid(), "species";

-- Copy users from the Species and Picture tables into the Users table.
-- Species are ordered by ID so that the newest usernames are preserved (not replaced) when inserting.

CREATE TABLE Users(id INTEGER PRIMARY KEY AUTOINCREMENT, user_id INTEGER, name TEXT, UNIQUE(user_id));

INSERT OR REPLACE INTO Users(user_id, name) SELECT user_id, owner FROM Species GROUP BY owner ORDER BY id ASC;
INSERT INTO Users(name) SELECT artist FROM Picture WHERE artist NOT IN (SELECT name FROM Users) AND artist IS NOT NULL AND artist != "" GROUP BY artist;

-- Rename the Gallery table to "Galleries" and Picture to "Pictures" for consistency.

CREATE TABLE Galleries(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, UNIQUE(name));
CREATE TABLE Pictures(id INTEGER PRIMARY KEY AUTOINCREMENT, url TEXT, name TEXT, description TEXT, user_id INTEGER, timestamp INTEGER, FOREIGN KEY(user_id) REFERENCES Users(id) ON DELETE SET NULL, UNIQUE(url));
CREATE TABLE GalleryPictures(gallery_id INTEGER NOT NULL, picture_id INTEGER NOT NULL, FOREIGN KEY(gallery_id) REFERENCES Galleries(id) ON DELETE CASCADE, FOREIGN KEY(picture_id) REFERENCES Pictures(id) ON DELETE CASCADE, UNIQUE(gallery_id, picture_id));

INSERT INTO Galleries SELECT * FROM Gallery;
INSERT OR IGNORE INTO Pictures(id, url, name, description, user_id) SELECT Picture.id, url, Picture.name, description, Users.id FROM Picture, Users WHERE Picture.artist = Users.name;
INSERT INTO GalleryPictures(gallery_id, picture_id) SELECT Picture.gallery_id, Pictures.id FROM Picture, Pictures WHERE Picture.url = Pictures.url;

DROP TABLE Picture;
DROP TABLE Gallery;

-- Create taxa-related tables.

CREATE TABLE Taxa(id INTEGER PRIMARY KEY AUTOINCREMENT, parent_id INTEGER, rank_id INTEGER, rank_order INTEGER, user_id INTEGER, common_name_id INTEGER, picture_id INTEGER, name TEXT, description TEXT, timestamp INTEGER, FOREIGN KEY(parent_id) REFERENCES Taxa(id) ON DELETE SET NULL, FOREIGN KEY(rank_id) REFERENCES TaxaRanks(id) ON DELETE SET NULL, FOREIGN KEY(user_id) REFERENCES Users(id) ON DELETE SET NULL, UNIQUE(name, parent_id));
CREATE TABLE TaxaCommonNames(id INTEGER PRIMARY KEY AUTOINCREMENT, taxon_id INTEGER, name TEXT, timestamp INTEGER, FOREIGN KEY(taxon_id) REFERENCES Taxa(id) ON DELETE CASCADE, UNIQUE(taxon_id, name));
CREATE TABLE TaxaPictures(taxon_id INTEGER NOT NULL, picture_id INTEGER NOT NULL, FOREIGN KEY(taxon_id) REFERENCES Taxa(id) ON DELETE CASCADE, FOREIGN KEY(picture_id) REFERENCES Pictures(id) ON DELETE CASCADE, UNIQUE(taxon_id, picture_id));

-- Require unique names for all taxa except for species.
-- Because WHERE clauses for partial indexes cannot contain subqueries, this is provided as a constant.

CREATE UNIQUE INDEX Idx_Taxa_RankIdName ON Taxa(rank_id, name) WHERE rank_id != 8;

-- Copy taxa into the Taxa tables.

INSERT INTO Taxa(name, description, rank_id) SELECT Domain.name, description, TaxaRanks.id FROM Domain, TaxaRanks WHERE TaxaRanks.name = "domain";
INSERT INTO Taxa(name, description, rank_id) SELECT Kingdom.name, description, TaxaRanks.id FROM Kingdom, TaxaRanks WHERE TaxaRanks.name = "kingdom";
INSERT INTO Taxa(name, description, rank_id) SELECT Phylum.name, description, TaxaRanks.id FROM Phylum, TaxaRanks WHERE TaxaRanks.name = "phylum";
INSERT INTO Taxa(name, description, rank_id) SELECT Class.name, description, TaxaRanks.id FROM Class, TaxaRanks WHERE TaxaRanks.name = "class";
INSERT INTO Taxa(name, description, rank_id) SELECT Ord.name, description, TaxaRanks.id FROM Ord, TaxaRanks WHERE TaxaRanks.name = "order";
INSERT INTO Taxa(name, description, rank_id) SELECT Family.name, description, TaxaRanks.id FROM Family, TaxaRanks WHERE TaxaRanks.name = "family";
INSERT INTO Taxa(name, description, rank_id) SELECT Genus.name, description, TaxaRanks.id FROM Genus, TaxaRanks WHERE TaxaRanks.name = "genus";

INSERT INTO Taxa(
  name, description, parent_id, rank_id, 
  user_id, timestamp
) 
SELECT 
  Species.name, 
  Species.description, 
  Taxa.id AS parent_id, 
  TaxaRanks.id AS rank_id, 
  Users.id AS user_id, 
  Species.timestamp 
FROM 
  Species, 
  TaxaRanks, 
  Taxa, 
  Users 
WHERE 
  TaxaRanks.name = "species" 
  AND Taxa.name = (
    SELECT 
      Genus.name 
    FROM 
      Genus 
    WHERE 
      Genus.id = Species.genus_id
  ) 
  AND (
    Species.user_id = Users.user_id 
    OR Species.owner = Users.name
  ) 
GROUP BY 
  Species.id 
ORDER BY 
  user_id;

INSERT OR IGNORE INTO Pictures(url) SELECT pics FROM Domain WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url) SELECT pics FROM Kingdom WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url) SELECT pics FROM Phylum WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url) SELECT pics FROM Class WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url) SELECT pics FROM Ord WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url) SELECT pics FROM Family WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url) SELECT pics FROM Genus WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url) SELECT pics FROM Species WHERE pics IS NOT NULL AND pics != "";

INSERT INTO TaxaPictures(taxon_id, picture_id) SELECT Taxa.id, Pictures.id FROM Taxa, Pictures, Domain WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "domain") AND Pictures.url = Domain.pics AND taxa.name = Domain.name;
INSERT INTO TaxaPictures(taxon_id, picture_id) SELECT Taxa.id, Pictures.id FROM Taxa, Pictures, Kingdom WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "kingdom") AND Pictures.url = Kingdom.pics AND taxa.name = Kingdom.name;
INSERT INTO TaxaPictures(taxon_id, picture_id) SELECT Taxa.id, Pictures.id FROM Taxa, Pictures, Phylum WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "phylum") AND Pictures.url = Phylum.pics AND taxa.name = Phylum.name;
INSERT INTO TaxaPictures(taxon_id, picture_id) SELECT Taxa.id, Pictures.id FROM Taxa, Pictures, Class WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "class") AND Pictures.url = Class.pics AND taxa.name = Class.name;
INSERT INTO TaxaPictures(taxon_id, picture_id) SELECT Taxa.id, Pictures.id FROM Taxa, Pictures, Ord WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "order") AND Pictures.url = Ord.pics AND taxa.name = Ord.name;
INSERT INTO TaxaPictures(taxon_id, picture_id) SELECT Taxa.id, Pictures.id FROM Taxa, Pictures, Family WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "family") AND Pictures.url = Family.pics AND taxa.name = Family.name;
INSERT INTO TaxaPictures(taxon_id, picture_id) SELECT Taxa.id, Pictures.id FROM Taxa, Pictures, Genus WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "genus") AND Pictures.url = Genus.pics AND taxa.name = Genus.name;
INSERT INTO TaxaPictures(taxon_id, picture_id) SELECT Taxa.id, Pictures.id FROM Taxa, Pictures, Species WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "species") AND Pictures.url = species.pics AND taxa.name = species.name AND taxa.timestamp = species.timestamp;

INSERT INTO TaxaCommonNames(taxon_id, name) SELECT Taxa.id, common_name FROM Taxa, Domain WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "domain") AND Domain.name = Taxa.name AND common_name IS NOT NULL AND common_name != "";
INSERT INTO TaxaCommonNames(taxon_id, name) SELECT Taxa.id, common_name FROM Taxa, Kingdom WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "kingdom") AND Kingdom.name = Taxa.name AND common_name IS NOT NULL AND common_name != "";
INSERT INTO TaxaCommonNames(taxon_id, name) SELECT Taxa.id, common_name FROM Taxa, Phylum WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "phylum") AND Phylum.name = Taxa.name AND common_name IS NOT NULL AND common_name != "";
INSERT INTO TaxaCommonNames(taxon_id, name) SELECT Taxa.id, common_name FROM Taxa, Class WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "class") AND Class.name = Taxa.name AND common_name IS NOT NULL AND common_name != "";
INSERT INTO TaxaCommonNames(taxon_id, name) SELECT Taxa.id, common_name FROM Taxa, Ord WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "order") AND Ord.name = Taxa.name AND common_name IS NOT NULL AND common_name != "";
INSERT INTO TaxaCommonNames(taxon_id, name) SELECT Taxa.id, common_name FROM Taxa, Family WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "family") AND Family.name = Taxa.name AND common_name IS NOT NULL AND common_name != "";
INSERT INTO TaxaCommonNames(taxon_id, name) SELECT Taxa.id, common_name FROM Taxa, Genus WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "genus") AND Genus.name = Taxa.name AND common_name IS NOT NULL AND common_name != "";
INSERT INTO TaxaCommonNames(taxon_id, name) SELECT Taxa.id, common_name FROM Taxa, Species WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "species") AND Species.name = Taxa.name AND species.timestamp = Taxa.timestamp AND common_name IS NOT NULL AND common_name != "";

UPDATE Taxa SET picture_id = (SELECT Pictures.id FROM Pictures, Domain WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "domain") AND url = Domain.pics AND Domain.name = Taxa.name);
UPDATE Taxa SET picture_id = (SELECT Pictures.id FROM Pictures, Kingdom WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "kingdom") AND url = Kingdom.pics AND Kingdom.name = Taxa.name);
UPDATE Taxa SET picture_id = (SELECT Pictures.id FROM Pictures, Phylum WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "phylum") AND url = Phylum.pics AND Phylum.name = Taxa.name);
UPDATE Taxa SET picture_id = (SELECT Pictures.id FROM Pictures, Class WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "class") AND url = Class.pics AND Class.name = Taxa.name);
UPDATE Taxa SET picture_id = (SELECT Pictures.id FROM Pictures, Ord WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "order") AND url = Ord.pics AND Ord.name = Taxa.name);
UPDATE Taxa SET picture_id = (SELECT Pictures.id FROM Pictures, Family WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "family") AND url = Family.pics AND Family.name = Taxa.name);
UPDATE Taxa SET picture_id = (SELECT Pictures.id FROM Pictures, Genus WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "genus") AND url = Genus.pics AND Genus.name = Taxa.name);
UPDATE Taxa SET picture_id = (SELECT Pictures.id FROM Pictures, Species WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "species") AND url = Species.pics AND Species.name = Taxa.name AND Species.timestamp = Taxa.timestamp);

UPDATE Taxa SET common_name_id = (SELECT TaxaCommonNames.id FROM TaxaCommonNames, Domain WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "domain") AND TaxaCommonNames.taxon_id = Taxa.id AND Domain.name = Taxa.name AND TaxaCommonNames.name = Domain.common_name);
UPDATE Taxa SET common_name_id = (SELECT TaxaCommonNames.id FROM TaxaCommonNames, Kingdom WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "kingdom") AND TaxaCommonNames.taxon_id = Taxa.id AND Kingdom.name = Taxa.name AND TaxaCommonNames.name = Kingdom.common_name);
UPDATE Taxa SET common_name_id = (SELECT TaxaCommonNames.id FROM TaxaCommonNames, Phylum WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "phylum") AND TaxaCommonNames.taxon_id = Taxa.id AND Phylum.name = Taxa.name AND TaxaCommonNames.name = Phylum.common_name);
UPDATE Taxa SET common_name_id = (SELECT TaxaCommonNames.id FROM TaxaCommonNames, Class WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "class") AND TaxaCommonNames.taxon_id = Taxa.id AND Class.name = Taxa.name AND TaxaCommonNames.name = Class.common_name);
UPDATE Taxa SET common_name_id = (SELECT TaxaCommonNames.id FROM TaxaCommonNames, Ord WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "order") AND TaxaCommonNames.taxon_id = Taxa.id AND Ord.name = Taxa.name AND TaxaCommonNames.name = Ord.common_name);
UPDATE Taxa SET common_name_id = (SELECT TaxaCommonNames.id FROM TaxaCommonNames, Family WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "family") AND TaxaCommonNames.taxon_id = Taxa.id AND Family.name = Taxa.name AND TaxaCommonNames.name = Family.common_name);
UPDATE Taxa SET common_name_id = (SELECT TaxaCommonNames.id FROM TaxaCommonNames, Genus WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "genus") AND TaxaCommonNames.taxon_id = Taxa.id AND Genus.name = Taxa.name AND TaxaCommonNames.name = Genus.common_name);
UPDATE Taxa SET common_name_id = (SELECT TaxaCommonNames.id FROM TaxaCommonNames, Species WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "species") AND TaxaCommonNames.taxon_id = Taxa.id AND Species.name = Taxa.name AND Species.timestamp = Taxa.timestamp AND TaxaCommonNames.name = Species.common_name);

UPDATE Taxa SET parent_id = (SELECT Taxa2.id FROM Taxa AS Taxa2 WHERE Taxa2.rank_id = (SELECT id FROM TaxaRanks WHERE name = "domain") AND Taxa2.name = (SELECT Domain.name FROM Kingdom, Domain WHERE Domain.id = Kingdom.domain_id AND Kingdom.name = Taxa.name)) WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "kingdom");
UPDATE Taxa SET parent_id = (SELECT Taxa2.id FROM Taxa AS Taxa2 WHERE Taxa2.rank_id = (SELECT id FROM TaxaRanks WHERE name = "kingdom") AND Taxa2.name = (SELECT Kingdom.name FROM Phylum, Kingdom WHERE Kingdom.id = Phylum.kingdom_id AND Phylum.name = Taxa.name)) WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "phylum");
UPDATE Taxa SET parent_id = (SELECT Taxa2.id FROM Taxa AS Taxa2 WHERE Taxa2.rank_id = (SELECT id FROM TaxaRanks WHERE name = "phylum") AND Taxa2.name = (SELECT Phylum.name FROM Class, Phylum WHERE Phylum.id = Class.phylum_id AND Class.name = Taxa.name)) WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "class");
UPDATE Taxa SET parent_id = (SELECT Taxa2.id FROM Taxa AS Taxa2 WHERE Taxa2.rank_id = (SELECT id FROM TaxaRanks WHERE name = "class") AND Taxa2.name = (SELECT Class.name FROM Ord, Class WHERE Class.id = Ord.class_id AND Ord.name = Taxa.name)) WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "order");
UPDATE Taxa SET parent_id = (SELECT Taxa2.id FROM Taxa AS Taxa2 WHERE Taxa2.rank_id = (SELECT id FROM TaxaRanks WHERE name = "order") AND Taxa2.name = (SELECT Ord.name FROM Family, Ord WHERE Ord.id = Family.order_id AND Family.name = Taxa.name)) WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "family");
UPDATE Taxa SET parent_id = (SELECT Taxa2.id FROM Taxa AS Taxa2 WHERE Taxa2.rank_id = (SELECT id FROM TaxaRanks WHERE name = "family") AND Taxa2.name = (SELECT Family.name FROM Genus, Family WHERE Family.id = Genus.family_id AND Genus.name = Taxa.name)) WHERE Taxa.rank_id = (SELECT id FROM TaxaRanks WHERE name = "genus");

-- Move species image relationships outside of Galleries and into TaxaPictures.

INSERT 
OR IGNORE INTO TaxaPictures 
SELECT 
  Taxa.id, 
  Pictures.id 
FROM 
  Taxa, 
  Pictures 
WHERE 
  Taxa.rank_id = (
    SELECT 
      id 
    FROM 
      TaxaRanks 
    WHERE 
      name = "species"
  ) 
  AND Pictures.id IN (
    SELECT 
      picture_id 
    FROM 
      GalleryPictures 
    WHERE 
      gallery_id IN (
        SELECT 
          Galleries.id 
        FROM 
          Galleries 
        WHERE 
          name = (
            "species" || (
              SELECT 
                Species.id 
              FROM 
                Species 
              WHERE 
                Species.name = Taxa.name 
                AND Species.timestamp = Taxa.timestamp
            )
          )
      )
  );

DELETE FROM Galleries WHERE name IN (SELECT "species" || id FROM Species);

-- Move common names from SpeciesCommonNames to TaxaCommonNames.

INSERT OR IGNORE INTO TaxaCommonNames(taxon_id, name) SELECT Taxa.id, SpeciesCommonNames.name FROM SpeciesCommonNames, Taxa WHERE SpeciesCommonNames.species_id = (SELECT Species.id FROM Species WHERE Species.name = Taxa.name AND Species.timestamp = Taxa.timestamp);

DROP TABLE SpeciesCommonNames;

-- Recreate tables that have a foreign key to the Species table to refer to the Taxa table instead.

-- Recreate the Ancestors table.

CREATE TABLE Ancestors2(species_id INTEGER, ancestor_id INTEGER, FOREIGN KEY(species_id) REFERENCES Taxa(id) ON DELETE CASCADE, FOREIGN KEY(ancestor_id) REFERENCES Taxa(id) ON DELETE CASCADE, UNIQUE(species_id, ancestor_id));

INSERT INTO Ancestors2 
SELECT 
  Taxa2.id AS species_id, 
  Taxa.id AS ancestor_id 
FROM 
  Taxa, 
  Taxa AS Taxa2, 
  Ancestors 
WHERE 
  Ancestors.ancestor_id = (
    SELECT 
      Species.id 
    FROM 
      Species 
    WHERE 
      Species.name = Taxa.name 
      AND Species.timestamp = Taxa.timestamp
  ) 
  AND Ancestors.species_id = (
    SELECT 
      Species.id 
    FROM 
      Species 
    WHERE 
      Species.name = Taxa2.name 
      AND Species.timestamp = Taxa2.timestamp
  );

DROP TABLE Ancestors;

ALTER TABLE Ancestors2 RENAME TO Ancestors;

-- Recreate the Extinctions table.

CREATE TABLE Extinctions2(species_id INTEGER, reason TEXT, timestamp INTEGER, FOREIGN KEY(species_id) REFERENCES Taxa(id) ON DELETE CASCADE, UNIQUE(species_id));

INSERT INTO Extinctions2 SELECT Taxa.id, Extinctions.reason, Extinctions.timestamp FROM Taxa, Extinctions WHERE (Extinctions.species_id = (SELECT Species.id FROM Species WHERE Species.name = Taxa.name AND Species.timestamp = Taxa.timestamp));

DROP TABLE Extinctions;

ALTER TABLE Extinctions2 RENAME TO Extinctions;

-- Recreate the Favorites table.

INSERT OR IGNORE INTO Users(user_id) SELECT user_id FROM Favorites;

CREATE TABLE Favorites2(user_id INTEGER, species_id INTEGER, FOREIGN KEY(species_id) REFERENCES Taxa(id) ON DELETE CASCADE, FOREIGN KEY(user_id) REFERENCES Users(id) ON DELETE CASCADE, UNIQUE(user_id, species_id));

INSERT INTO Favorites2 SELECT Users.id, Taxa.id FROM Favorites, Users, Taxa WHERE Favorites.species_id = (SELECT Species.id FROM Species WHERE Species.name = Taxa.name AND Species.timestamp = Taxa.timestamp) AND Favorites.user_id = Users.user_id;

DROP TABLE Favorites;

ALTER TABLE Favorites2 RENAME TO Favorites;

-- Recreate the Gotchi table.

INSERT OR IGNORE INTO Users(user_id) SELECT owner_id FROM Gotchi;

CREATE TABLE Gotchis(id INTEGER PRIMARY KEY AUTOINCREMENT, species_id INTEGER, user_id INTEGER, name TEXT, exp REAL, fed_ts INTEGER, born_ts INTEGER, evolved_ts INTEGER, viewed_ts INTEGER, training_ts INTEGER, training_left INTEGER, FOREIGN KEY(species_id) REFERENCES Taxa(id) ON DELETE CASCADE, FOREIGN KEY(user_id) REFERENCES Users(id) ON DELETE CASCADE, UNIQUE(user_id, name));

INSERT INTO Gotchis(species_id, user_id, name, exp, fed_ts, born_ts, evolved_ts, viewed_ts, training_ts, training_left) SELECT Taxa.id, Users.id, Gotchi.name, exp, fed_ts, born_ts, evolved_ts, viewed_ts, training_ts, training_left FROM Taxa, Users, Gotchi WHERE (Gotchi.species_id = (SELECT Species.id FROM Species WHERE Species.name = Taxa.name AND Species.timestamp = Taxa.timestamp)) AND Gotchi.owner_id = Users.user_id;

-- Recreate the GotchiUsers table.

INSERT OR IGNORE INTO Users(user_id) SELECT user_id FROM GotchiUser;

CREATE TABLE GotchiUsers(user_id INTEGER, g INTEGER, gotchi_limit INTEGER, primary_gotchi_id INTEGER, FOREIGN KEY(user_id) REFERENCES Users(id) ON DELETE CASCADE, FOREIGN KEY(primary_gotchi_id) REFERENCES Gotchis(id) ON DELETE SET NULL, UNIQUE(user_id));

INSERT INTO GotchiUsers SELECT Users.id, g, gotchi_limit, Gotchis.id FROM Users, GotchiUser, Gotchis WHERE (primary_gotchi_id = (SELECT Gotchi.id FROM Gotchi WHERE Gotchi.name = Gotchis.name AND Gotchi.born_ts = Gotchis.born_ts)) AND GotchiUser.user_id = Users.user_id;
INSERT INTO GotchiUsers(user_id, g, gotchi_limit) SELECT Users.id, g, gotchi_limit FROM Users, GotchiUser WHERE GotchiUser.user_id = Users.user_id AND Users.id NOT IN (SELECT user_id FROM GotchiUsers);

DROP TABLE GotchiUser;
DROP TABLE Gotchi;

-- Recreate the Predates table.

CREATE TABLE Predators(species_id INTEGER, prey_id INTEGER, notes TEXT, timestamp INTEGER, FOREIGN KEY(species_id) REFERENCES Taxa(id) ON DELETE CASCADE, FOREIGN KEY(prey_id) REFERENCES Taxa(id) ON DELETE CASCADE, UNIQUE(species_id, prey_id));

INSERT INTO Predators(species_id, prey_id, notes)
SELECT 
  Taxa2.id AS species_id, 
  Taxa.id AS prey_id,
   Predates.notes
FROM 
  Taxa, 
  Taxa AS Taxa2, 
  Predates 
WHERE 
  Predates.species_id = (
    SELECT 
      Species.id 
    FROM 
      Species 
    WHERE 
      Species.name = Taxa.name 
      AND Species.timestamp = Taxa.timestamp
  ) 
  AND Predates.eats_id = (
    SELECT 
      Species.id 
    FROM 
      Species 
    WHERE 
      Species.name = Taxa2.name 
      AND Species.timestamp = Taxa2.timestamp
  );

DROP TABLE Predates;

-- Recreate the SpeciesRelationships table.

CREATE TABLE SpeciesRelationships2(species1_id INTEGER, species2_id INTEGER, relationship_id INTEGER, notes TEXT, timestamp INTEGER, FOREIGN KEY(species1_id) REFERENCES Taxa(id) ON DELETE CASCADE, FOREIGN KEY(species2_id) REFERENCES Taxa(id) ON DELETE CASCADE, FOREIGN KEY(relationship_id) REFERENCES Relationships(id) ON DELETE CASCADE, UNIQUE(species1_id, species2_id, relationship_id));

INSERT INTO SpeciesRelationships2(species1_id, species2_id, relationship_id)
SELECT 
  Taxa2.id AS species_id, 
  Taxa.id AS prey_id,
  relationship_id
FROM 
  Taxa, 
  Taxa AS Taxa2,
  SpeciesRelationships 
WHERE 
  SpeciesRelationships.species1_id = (
    SELECT 
      Species.id 
    FROM 
      Species 
    WHERE 
      Species.name = Taxa.name 
      AND Species.timestamp = Taxa.timestamp
  ) 
  AND SpeciesRelationships.species2_id = (
    SELECT 
      Species.id 
    FROM 
      Species 
    WHERE 
      Species.name = Taxa2.name 
      AND Species.timestamp = Taxa2.timestamp
  );

DROP TABLE SpeciesRelationships;

ALTER TABLE SpeciesRelationships2 RENAME TO SpeciesRelationships;

-- Recreate the SpeciesRoles table.

CREATE TABLE SpeciesRoles2(species_id INTEGER, role_id INTEGER, notes TEXT, timestamp INTEGER, FOREIGN KEY(species_id) REFERENCES Taxa(id) ON DELETE CASCADE, FOREIGN KEY(role_id) REFERENCES Roles(id) ON DELETE CASCADE, UNIQUE(species_id, role_id));

INSERT INTO SpeciesRoles2(species_id, role_id, notes) SELECT Taxa.id, SpeciesRoles.role_id, SpeciesRoles.notes FROM SpeciesRoles, Taxa WHERE SpeciesRoles.species_id = (SELECT Species.id FROM Species WHERE Species.name = Taxa.name AND Species.timestamp = Taxa.timestamp);

DROP TABLE SpeciesRoles;

ALTER TABLE SpeciesRoles2 RENAME TO SpeciesRoles;

-- Recreate the SpeciesZones table.

CREATE TABLE SpeciesZones2(species_id INTEGER, zone_id INTEGER, notes TEXT, timestamp INTEGER, FOREIGN KEY(species_id) REFERENCES Taxa(id) ON DELETE CASCADE, FOREIGN KEY(zone_id) REFERENCES Zones(id) ON DELETE CASCADE, UNIQUE(species_id, zone_id));

INSERT INTO SpeciesZones2 SELECT Taxa.id, SpeciesZones.zone_id, SpeciesZones.notes, SpeciesZones.timestamp FROM SpeciesZones, Taxa WHERE SpeciesZones.species_id = (SELECT Species.id FROM Species WHERE Species.name = Taxa.name AND Species.timestamp = Taxa.timestamp);

DROP TABLE SpeciesZones;

ALTER TABLE SpeciesZones2 RENAME TO SpeciesZones;

-- Recreate the ZoneRecords table.

CREATE TABLE ZoneRecords2(species_id INTEGER, zone_id INTEGER, reason TEXT, timestamp INTEGER, record_type INTEGER, FOREIGN KEY(species_id) REFERENCES Taxa(id) ON DELETE CASCADE, FOREIGN KEY(zone_id) REFERENCES Zones(id) ON DELETE CASCADE);

INSERT INTO ZoneRecords2 SELECT Taxa.id, zone_id, reason, ZoneRecords.timestamp, record_type FROM ZoneRecords, Taxa WHERE ZoneRecords.species_id = (SELECT Species.id FROM Species WHERE Species.name = Taxa.name AND Species.timestamp = Taxa.timestamp);

DROP TABLE ZoneRecords;

ALTER TABLE ZoneRecords2 RENAME TO ZoneRecords;

-- Finally, drop all obselete tables.

DROP TABLE Species;
DROP TABLE Genus;
DROP TABLE Family;
DROP TABLE Ord;
DROP TABLE Class;
DROP TABLE Phylum;
DROP TABLE Kingdom;
DROP TABLE Domain;

COMMIT;