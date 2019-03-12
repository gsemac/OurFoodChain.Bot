function register(move)

	move.name = "Sting";
	move.description = "Attacks the opponent with stinger(s). Does low damage, but never misses.";

	move.pp = 40;
	move.hit_rate = 1.0;
	move.can_miss = false;
	move.multiplier = 0.8;

end;

function callback(args) 
	args.applyDamage();
end;