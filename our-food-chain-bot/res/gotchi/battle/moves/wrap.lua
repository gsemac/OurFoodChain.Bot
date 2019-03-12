function register(move)

	move.name = "Wrap";
	move.description = "Tightly wraps tentacles around the opponent. Deals more damage the faster the opponent is compared to the user.";
	
	move.pp = 5;
	move.type = type.Offensive;

end;

function callback(args) 
	
	damage = args.calculateDamage();

	args.target.hp = args.target.hp - max(1.0, damage * ((args.target.spd / args.user.spd) + 1.0));

end;