function register(move)

	move.name = "Wrap";
	move.description = "Tightly wraps tentacles around the opponent. Deals more damage the faster the opponent is compared to the user.";
	
	move.pp = 5;
	move.type = type.Offensive;

	move.requires.match = "tentacle";

end

function callback(args) 
	
	multiplier = min(2.0, args.target.spd / args.user.spd);
	base_damage = args.getBaseDamage() * multiplier;

	args.target.hp = args.target.hp - args.calculateDamage(base_damage);

end