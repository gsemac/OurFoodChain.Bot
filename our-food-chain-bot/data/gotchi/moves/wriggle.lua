function register(move)

	move.name = "wriggle";
	move.description = "The user uselessly wriggles like a worm.";

	move.pp = 40;
	move.requires.maxLevel = 10;
	move.requires.match = "worm";

end

function callback(args) 
end