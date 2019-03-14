function register(move)

	move.name = "Tail Slap";
	move.description = "Deals more damage the faster the user is compared to the opponent.";
	
	move.pp = 5;
	move.type = type.Offensive;

	move.requires.match = "tail";

end;

function callback(args) 
	
	base_damage = args.getBaseDamage() * (args.user.spd / args.target.spd);

	args.target.hp = args.target.hp - args.calculateDamage(base_damage);

end;