function register(move)

	move.name = "vine strangle";
	move.description = "Wraps numerous vines around the opponent, dealing more damage the higher the user's HP is compared to the opponent.";

	move.pp = 5;
	move.type = type.Offensive;

	move.requires.role = "producer";
	move.requires.minLevel = 30;
	move.requires.match = "vine";

end

function callback(args) 
	
	base_damage = args.BaseDamage() * (args.user.hp / args.target.hp);

	args.target.hp = args.target.hp - args.TotalDamage(base_damage);

end