function register(move)

	move.name = "break down";
	move.description = "Resets all of the opponent's stat buffs.";

	move.pp = 1;
	move.type = type.Offensive;

	move.requires.role = "decomposer";
	move.requires.minLevel = 20;

end

function callback(args) 	
	
	args.target.Reset();
	args.text = "resetting its opponent's stats";

end