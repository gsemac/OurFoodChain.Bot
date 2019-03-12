function register(move)

	move.name = "Tail Slap";
	move.description = "Deals more damage the faster the user is compared to the opponent.";
	
	move.pp = 5;
	move.type = type.Offensive;

end;

function callback(args) 
	
	damage = args.calculateDamage();

	args.target.hp = args.target.hp - max(1.0, damage * (((args.user.spd / args.target.spd) / 15.0) + 1.0));

end;