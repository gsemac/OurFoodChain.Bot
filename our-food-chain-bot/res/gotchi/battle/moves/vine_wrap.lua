function register(move)

	move.name = "vine wrap";
	move.description = "Tightly wraps vines around the opponent, causing them to take damage every turn.";

	move.pp = 10;
	move.type = type.Offensive;

	move.requires.role = "producer";
	move.requires.min_level = 20;
	move.requires.match = "vine";

end;

function callback(args) 
	
	args.target.status = "vine-wrapped";
	args.text = "wrapping the opponent in vines";

end;