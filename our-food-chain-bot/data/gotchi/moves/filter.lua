function register(move)

	move.name = "filter";
	move.description = "Filters through detritus for food, restoring a moderate amount of HP.";
	move.role = "detritivore";

	move.pp = 20;
	move.type = type.Recovery;

	move.requires.role = "detritivore";
	move.requires.minLevel = 10;

end

function callback(args) 	
	args.DoRecoverPercent(0.2);
end