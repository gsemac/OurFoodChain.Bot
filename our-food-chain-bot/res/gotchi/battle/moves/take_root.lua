function register(move)

	move.name = "Take Root";
	move.description = "Takes root and draws nutrients from the substrate, restoring HP each turn.";
	move.role = "producer";

	move.pp = 10;
	move.type = type.Recovery;

end;

function callback(args) 	
	
	args.text = "dug its roots into the ground";
	args.user.status = "rooted";

end;