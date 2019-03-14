function register(move)

	move.name = "clean-up";
	move.description = "Nibbles on detritus, restoring a small amount of HP.";
	move.role = "detritivore";

	move.pp = 20;
	move.type = type.Recovery;

	move.requires.role = "detritivore";
	move.requires.maxLevel = 10;

end

function callback(args) 	
	args.DoRecoverPercent(0.1);
end