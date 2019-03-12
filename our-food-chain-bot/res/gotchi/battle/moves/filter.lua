function register(move)

	move.name = "filter";
	move.description = "Filters through detritus for food, restoring a moderate amount of HP.";
	move.role = "detritivore";

	move.pp = 20;
	move.type = type.Recovery;

end;

function callback(args) 	
	args.recoverPercent(0.2);
end;