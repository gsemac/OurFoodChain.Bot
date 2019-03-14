function register(move)

	move.name = "Tail Slap";
	move.description = "Deals more damage the faster the user is compared to the opponent.";
	
	move.pp = 5;
	move.type = type.Offensive;

	move.requires.match = "tail";

end

function callback(args) 
	
	multiplier = min(2.0, args.user.spd / args.target.spd);
	base_damage = args.BaseDamage() * multiplier;

	args.DoDamage(base_damage);

end