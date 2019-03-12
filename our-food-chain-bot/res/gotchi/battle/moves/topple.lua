function register(move)

	move.name = "topple";
	move.description = "Collapses onto the opponent, dealing heavy damage. However, the user is reduced to 1 HP.";

	move.pp = 1;
	move.multiplier = 5;
	move.critical_rate = 2;
	move.hit_rate = 0.5;

	move.requires.role = "producer";
	move.requires.min_level = 10;
	move.requires.match = "tree|tall|heavy";

end;

function callback(args) 

	args.user.hp = min(1, args.user.hp);

	args.applyDamage();

end;