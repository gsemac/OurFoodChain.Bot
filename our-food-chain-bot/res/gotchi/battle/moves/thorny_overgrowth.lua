function register(move)

	move.name = "thorny overgrowth";
	move.description = "Grows thorny structures surrounding the opponent, causing them to take damage every time they attack.";

	move.pp = 10;
	move.type = type.Offensive;

end;

function callback(args) 
	
	args.target.status = "thorn-surrounded";
	args.text = "surrounding the opponent with thorns";

end;