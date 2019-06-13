function register(move)

	move.name = "crush";
	move.description = "Crushes the opponent with force, dealing damage and decreasing their defense. Has a high critical hit ratio.";
	move.role = "predator";

	move.pp = 5;
	move.multiplier = 1.5;
	move.criticalRate = 2.0;

	move.requires.match = "\\b(?:crush(?:ing|es)?)\\b";
	move.requires.minLevel = 10;

end

function callback(args) 
	args.DoDamage();
end