function register(move)

	move.name = "tangle";
	move.description = "Tangles the opponent in vines, lowering their speed.";
	move.role = "producer";

	move.pp = 10;
	move.type = type.Buff;

	move.requires.role = "producer";
	move.requires.match = "vine";

end

function callback(args) 	
	
	args.target.stats.spd = args.target.stats.spd * 0.8;
	args.target.text = "lowering the opponent's speed by 20%";

end