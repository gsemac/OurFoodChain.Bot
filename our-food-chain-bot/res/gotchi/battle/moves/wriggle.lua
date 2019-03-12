function register(move)

	move.name = "wriggle";
	move.description = "The user uselessly wriggles like a worm.";

	move.pp = 40;
	move.requires.max_level = 10;
	move.requires.match = "worm";

end

function callback(args) 
end