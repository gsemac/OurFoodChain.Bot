function register(move)

	move.name = "All-Out Attack";
	move.description = "Rushes the opponent. Has abysmal accuracy, but deals very high damage.";
	move.role = "predator";

	move.pp = 10;
	move.hitRate = 0.1;
	move.multiplier = 2.5;

	move.requires.role = "predator";
	move.requires.minLevel = 20;

end

function callback(args) 
	args.DoDamage();
end