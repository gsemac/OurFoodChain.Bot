function register(move)

	move.name = "overgrowth";
	move.description = "Accelerates growth, boosting attack by a moderate amount.";
	move.role = "producer";

	move.pp = 25;
	move.type = type.Buff;

	move.requires.role = "producer";
	move.requires.minLevel = 30;

end

function callback(args) 	
	args.user.stats.atk = args.user.stats.atk * 1.2;
end