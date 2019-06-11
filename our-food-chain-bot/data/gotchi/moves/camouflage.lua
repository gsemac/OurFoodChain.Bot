function register(move)

	move.name = "camouflage";
	move.description = "Uses camouflage to hide from the opponent, boosting evasion by a small amount.";
	move.pp = 5;
	move.requires.match = "camouflage";

end

function callback(args) 

	args.user.stats.evasion = args.user.stats.evasion + 0.1;

end