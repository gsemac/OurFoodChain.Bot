-- Create the "TaxonomicRanks" ranks table and populate it with default values.

CREATE TABLE IF NOT EXISTS TaxonomicRanks(id INTEGER PRIMARY KEY AUTOINCREMENT, parent_id INTEGER, name TEXT, FOREIGN KEY(parent_id) REFERENCES TaxonomicRanks(id), UNIQUE(name));

INSERT OR IGNORE INTO TaxonomicRanks(name) VALUES("domain");
INSERT OR IGNORE INTO TaxonomicRanks(parent_id, name) SELECT last_insert_rowid(), "kingdom";
INSERT OR IGNORE INTO TaxonomicRanks(parent_id, name) SELECT last_insert_rowid(), "class";
INSERT OR IGNORE INTO TaxonomicRanks(parent_id, name) SELECT last_insert_rowid(), "order";
INSERT OR IGNORE INTO TaxonomicRanks(parent_id, name) SELECT last_insert_rowid(), "family";
INSERT OR IGNORE INTO TaxonomicRanks(parent_id, name) SELECT last_insert_rowid(), "genus";
INSERT OR IGNORE INTO TaxonomicRanks(parent_id, name) SELECT last_insert_rowid(), "species";

-- Copy users from the "Species" and "Picture" tables into the "Users" table.
-- Species are ordered by ID so that the newest usernames are preserved (not replaced) when inserting.

CREATE TABLE IF NOT EXISTS Users(id INTEGER PRIMARY KEY AUTOINCREMENT, user_id INTEGER, name TEXT, UNIQUE(user_id));

INSERT OR REPLACE INTO Users(user_id, name) SELECT user_id, owner FROM Species GROUP BY owner ORDER BY id ASC;
INSERT INTO Users(name) SELECT artist FROM Picture WHERE artist NOT IN (SELECT name FROM Users) AND artist IS NOT NULL AND artist != "" GROUP BY artist;

-- Rename the "Gallery" table to "Galleries" and "Picture" to "Pictures" for consistency.
-- It's better to do this sooner rather than later since we're going to be referencing them below.

CREATE TABLE IF NOT EXISTS Galleries(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, UNIQUE(name));
CREATE TABLE IF NOT EXISTS Pictures(id INTEGER PRIMARY KEY AUTOINCREMENT, url TEXT, gallery_id INTEGER, name TEXT, description TEXT, user_id INTEGER, timestamp INTEGER, FOREIGN KEY(gallery_id) REFERENCES Galleries(id), FOREIGN KEY(user_id) REFERENCES Users(id), UNIQUE(gallery_id, url));

-- Copy galleries and pictures into the new tables.

INSERT INTO Galleries SELECT * FROM Gallery;
INSERT INTO Pictures(id, url, gallery_id, name, description, user_id) SELECT Picture.id, url, gallery_id, Picture.name, description, Users.id FROM Picture, Users WHERE Picture.artist = Users.name;

DROP TABLE IF EXISTS Picture;
DROP TABLE IF EXISTS Gallery;

-- Create the "Taxa" and "TaxaCommonNames" tables.

CREATE TABLE IF NOT EXISTS Taxa(id INTEGER PRIMARY KEY AUTOINCREMENT, parent_id INTEGER, rank_id INTEGER, rank_order INTEGER, gallery_id INTEGER, user_id INTEGER, common_name_id INTEGER, picture_id INTEGER, name TEXT, description TEXT, timestamp INTEGER, FOREIGN KEY(parent_id) REFERENCES Taxa(id), FOREIGN KEY(rank_id) REFERENCES TaxonomicRanks(id), FOREIGN KEY(gallery_id) REFERENCES Galleries(id), FOREIGN KEY(user_id) REFERENCES Users(id));
CREATE TABLE IF NOT EXISTS TaxaCommonNames(id INTEGER PRIMARY KEY AUTOINCREMENT, taxon_id INTEGER, name TEXT, timestamp INTEGER, FOREIGN KEY(taxon_id) REFERENCES Taxa(id), UNIQUE(taxon_id, name));

-- Copy taxa from the taxa tables into the "Taxa" table.
-- For some tables, this will involve copying some information to other tables first (e.g. galleries, owners).

-- Create galleries for each taxa table.

INSERT OR IGNORE INTO Galleries(name) SELECT("domain" || id) FROM Domain WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url, gallery_id) SELECT Domain.pics, Galleries.id FROM Galleries, Domain WHERE Galleries.name = ("domain" || Domain.id);

INSERT OR IGNORE INTO Galleries(name) SELECT("kingdom" || id) FROM Kingdom WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url, gallery_id) SELECT Kingdom.pics, Galleries.id FROM Galleries, Kingdom WHERE Galleries.name = ("kingdom" || Kingdom.id);

INSERT OR IGNORE INTO Galleries(name) SELECT("phylum" || id) FROM Phylum WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url, gallery_id) SELECT Phylum.pics, Galleries.id FROM Galleries, Phylum WHERE Galleries.name = ("phylum" || Phylum.id);

INSERT OR IGNORE INTO Galleries(name) SELECT("class" || id) FROM Class WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url, gallery_id) SELECT Class.pics, Galleries.id FROM Galleries, Class WHERE Galleries.name = ("class" || Class.id);

INSERT OR IGNORE INTO Galleries(name) SELECT("order" || id) FROM Ord WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url, gallery_id) SELECT Ord.pics, Galleries.id FROM Galleries, Ord WHERE Galleries.name = ("order" || Ord.id);

INSERT OR IGNORE INTO Galleries(name) SELECT("family" || id) FROM Family WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url, gallery_id) SELECT Family.pics, Galleries.id FROM Galleries, Family WHERE Galleries.name = ("family" || Family.id);

INSERT OR IGNORE INTO Galleries(name) SELECT("genus" || id) FROM Genus WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url, gallery_id) SELECT Genus.pics, Galleries.id FROM Galleries, Genus WHERE Galleries.name = ("genus" || Genus.id);

INSERT OR IGNORE INTO Galleries(name) SELECT("species" || id) FROM Species WHERE pics IS NOT NULL AND pics != "";
INSERT OR IGNORE INTO Pictures(url, gallery_id, user_id) SELECT Species.pics, Galleries.id, Users.id FROM Galleries, Species, Users WHERE Galleries.name = ("species" || Species.id) AND Species.user_id = Users.user_id;
