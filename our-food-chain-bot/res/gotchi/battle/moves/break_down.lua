function register(move)

	move.name = "break down";
	move.description = "Resets all of the opponent's stat buffs.";

	move.pp = 1;
	move.type = type.Offensive;

	move.requires.role = "decomposer";
	move.requires.min_level = 20;

end;

function callback(args) 	
	
	args.target.reset();
	args.text = "resetting its opponent's stats";

end;