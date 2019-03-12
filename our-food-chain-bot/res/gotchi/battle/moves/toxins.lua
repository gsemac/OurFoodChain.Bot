function register(move)

	move.name = "Toxins";
	move.description = "Poisons the opponent, causing them to take damage every turn.";

	move.pp = 10;
	move.type = type.Offensive;

	move.requires.match = "toxin|poison";

end;

function callback(args) 
	
	args.target.status = "poisoned";
	args.text = "poisoning the opponent";

end;