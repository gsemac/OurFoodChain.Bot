function register(move)

	move.name = "Blinding Flash";
	move.description = "Flashes bright lights, causing the opponent's next move to miss.";

	move.pp = 5;
	move.priority = 2;
	move.requires.match = "flash|bright light";

end

function callback(args) 	
	
	args.user.status = "blinding";
	args.text = "emitting a blinding bright light!";

end