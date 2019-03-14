function register(move)

	move.name = "desperation";
	move.description = "Used as a last resort when no other moves have PP.";

	move.pp = 0;

	-- This move should be inaccessible normally.
	move.requires.minLevel = -1;
	move.requires.maxLevel = -1;

end

function callback(args) 
	
	damage = args.TotalDamage();
	args.user.hp = args.user.hp - damage;

	args.DoDamage();

	args.text = "lashing out in desperation";
	
end