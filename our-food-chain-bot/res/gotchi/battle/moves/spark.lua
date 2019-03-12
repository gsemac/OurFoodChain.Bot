function register(move)

	move.name = "spark";
	move.description = "Shocks the opponent with electricity. This move does little damage, but always strikes first.";

	move.pp = 10;
	move.type = type.Offensive;
	move.multiplier = 0.3;
	move.priority = 2;

	move.requires.min_level = 10;
	move.requires.match = "electric|static";

end

function callback(args) 
	args.applyDamage();
end