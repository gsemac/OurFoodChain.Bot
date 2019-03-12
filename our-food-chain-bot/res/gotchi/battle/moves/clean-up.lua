function register(move)

	move.name = "clean-up";
	move.description = "Nibbles on detritus, restoring a small amount of HP.";
	move.role = "detritivore";

	move.pp = 20;
	move.type = type.Recovery;

	move.requires.role = "detritivore";
	move.requires.max_level = 10;

end;

function callback(args) 	
	args.recoverPercent(0.1);
end;