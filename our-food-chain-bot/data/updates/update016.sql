-- User ID is now stored alongside usernames.
-- Usernames are saved so users can come and go from the server and their name won't be lost. However, user IDs are still required for the "ownedby" command to work after username changes.
ALTER TABLE Species ADD COLUMN user_id INTEGER;