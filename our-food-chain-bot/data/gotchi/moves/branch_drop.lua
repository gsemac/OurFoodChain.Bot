function register(move)

	move.name = "branch drop";
	move.description = "Drops a heavy branch on the target, dealing damage to the opponent. However, the user also takes damage.";

	move.pp = 10;
	move.multiplier = 3;
	move.critical_rate = 1.5;

	move.requires.role = "producer";
	move.requires.minLevel = 5;
	move.requires.match = "tree|branch";

end

function callback(args) 

	args.user.stats.hp = args.user.stats.hp - 10;

	args.DoDamage();

end