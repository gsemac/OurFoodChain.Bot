function register(move)

	move.name = "Photo-Boost";
	move.description = "Regenerates with the help of sunlight, restoring a moderate amount of HP.";
	move.role = "producer";

	move.pp = 5;
	move.type = type.Recovery;

	move.requires.role = "producer";
	move.requires.minLevel = 30;

end

function callback(args) 	
	
	if(args.user.status == "shaded") then
		args.text = "but couldn't get any sun";
	else
		args.DoRecoverPercent(0.15);
	end;

end