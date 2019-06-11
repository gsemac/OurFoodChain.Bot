function register(move)

	move.name = "nettle";
	move.description = "Attacks the opponent with irritating stingers, decreasing their speed. Does low damage, but never misses.";

	move.pp = 40;
	move.hitRate = 1.0;
	move.canMiss = false;
	move.multiplier = 0.8;
	move.type = type.Offensive;

	move.requires.match = "sting";
	move.requires.minLevel = 10;

end

function callback(args) 

	args.target.stats.spd = args.target.stats.spd * 0.8;

	args.DoDamage();

end