function register(move)

	move.name = "Take Root";
	move.description = "Takes root and draws nutrients from the substrate, restoring HP each turn.";
	move.role = "producer";

	move.pp = 10;
	move.type = type.Recovery;

	move.requires.role = "producer";
	move.requires.min_level = 30;
	move.requires.match = "root";

end;

function callback(args) 	
	
	args.text = "digging its roots into the ground";
	args.user.status = "rooted";

end;