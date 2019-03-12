function register(move)

	move.name = "desperation";
	move.description = "Used as a last resort when no other moves have PP.";

	move.pp = 0;

	-- This move should be inaccessible normally.
	move.requires.min_level = -1;
	move.requires.max_level = -1;

end

function callback(args) 
	
	damage = args.calculateDamage();
	args.user.hp = args.user.hp - damage;

	args.applyDamage();

	args.text = "lashing out in desperation";
	
end