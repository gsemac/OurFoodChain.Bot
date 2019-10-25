-- Adds fields related to gotchi training.
ALTER TABLE Gotchi ADD COLUMN training_ts INTEGER;
ALTER TABLE Gotchi ADD COLUMN training_left INTEGER;