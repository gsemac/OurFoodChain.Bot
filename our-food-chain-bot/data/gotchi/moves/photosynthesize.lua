function register(move)

	move.name = "Photosynthesize";
	move.description = "Regenerates with the help of sunlight, restoring HP.";
	move.role = "producer";

	move.pp = 5;
	move.type = type.Recovery;
	move.canMiss = false;

	move.requires.role = "producer";

end

function callback(args) 	
	
	if(args.user.status == "shaded") then
		args.text = "but couldn't get any sun";
	else
		args.DoRecoverPercent(0.1);
	end

end