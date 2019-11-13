-- Gotchi items are now stored in a user's inventory instead of being used immediately.
-- Create the table for storing a user's inventory.
CREATE TABLE IF NOT EXISTS GotchiInventory(user_id INTEGER, item_id INTEGER, count INTEGER, PRIMARY KEY(user_id, item_id));