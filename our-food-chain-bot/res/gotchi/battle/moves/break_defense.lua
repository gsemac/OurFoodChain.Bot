function register(move)

	move.name = "Break Defense";
	move.description = "Breaks down the opponent's defense, allowing them to go all-out. Reducing the opponent's Defense to 0, but ups their Attack.";

	move.role = "decomposer";
	move.pp = 5;

	move.requires.role = "decomposer";
	move.requires.minLevel = 30;

end;

function callback(args) 

	args.target.atk = args.target.atk + args.target.def;
	args.target.def = 0;
	args.text = "breaking the opponent's defense";

end;