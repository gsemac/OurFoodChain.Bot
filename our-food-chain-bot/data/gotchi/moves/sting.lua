function register(move)

	move.name = "Sting";
	move.description = "Attacks the opponent with stinger(s). Does low damage, but never misses.";

	move.pp = 40;
	move.hitRate = 1.0;
	move.canMiss = false;
	move.multiplier = 0.8;

	move.requires.match = "\\b(sting)";
	move.requires.maxLevel = 10;

end

function callback(args) 
	args.DoDamage();
end
