function register(move)

	move.name = "All-Out Attack";
	move.description = "Rushes the opponent. Has abysmal accuracy, but deals very high damage.";
	move.role = "predator";

	move.pp = 10;
	move.hit_rate = 0.1;
	move.multiplier = 2.5;

	move.requires.role = "predator";
	move.requires.min_level = 20;

end;

function callback(args) 
	args.applyDamage();
end;