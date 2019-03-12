function register(move)

	move.name = "Wild Attack";
	move.description = "Viciously and blindly attacks the opponent. Has low accuracy, but high critical hit rate.";
	move.role = "predator";

	move.pp = 20;
	move.hit_rate = 0.5;
	move.critical_rate = 2.0;

end;

function callback(args) 
	args.applyDamage();
end;