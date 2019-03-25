function register(move)

	move.name = "degrade";
	move.description = "Degrades the opponent, reducing their stats by a small amount.";
	move.role = "decomposer";

	move.pp = 10;

	move.requires.role = "decomposer";
	move.requires.minLevel = 10;

end

function callback(args) 	
	
	args.target.stats.MultiplyAll(0.8);
	args.text = "lowering their opponent's stats by 20%";

end