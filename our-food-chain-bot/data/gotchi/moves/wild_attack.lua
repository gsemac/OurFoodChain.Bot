function register(move)

	move.name = "Wild Attack";
	move.description = "Viciously and blindly attacks the opponent. Has low accuracy, but high critical hit rate.";
	move.role = "predator";

	move.pp = 20;
	move.hitRate = 0.5;
	move.criticalRate = 2.0;

	move.requires.role = "predator";
	move.requires.minLevel = 10;

end

function callback(args) 
	args.DoDamage();
end