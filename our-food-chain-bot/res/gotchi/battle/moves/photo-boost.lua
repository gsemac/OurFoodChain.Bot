function register(move)

	move.name = "Photo-Boost";
	move.description = "Regenerates with the help of sunlight, restoring a moderate amount of HP.";
	move.role = "producer";

	move.pp = 5;
	move.type = type.Recovery;

	move.requires.role = "producer";
	move.requires.min_level = 30;

end;

function callback(args) 	
	
	if(args.user.status == "shaded") then
		args.text = "but couldn't get any sun";
	else
		args.recoverPercent(0.4);
	end;

end;