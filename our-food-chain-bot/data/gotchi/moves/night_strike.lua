function register(move)

	move.name = "Night Strike";
	move.description = "Attacks in the night while the opponent is sleeping, completely ignoring defense. Has a 50% chance to fail if the target wakes up.";

	move.pp = 1;
	move.type = type.Offensive;

	move.requires.minLevel = 25;
	move.requires.match = "\\b(?:nocturnal)\\b";

end

function callback(args) 
	
	if(rand(0, 2) == 0) then
		args.text = "but the opponent woke up";
	else 

		old_defense = args.target.stats.def;
		
		args.target.stats.def = 0;
		
		args.DoDamage();
		
		args.target.stats.def = old_defense;

	end

end