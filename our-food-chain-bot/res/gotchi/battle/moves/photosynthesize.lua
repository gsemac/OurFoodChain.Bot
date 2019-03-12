function register(move)

	move.name = "Photosynthesize";
	move.description = "Regenerates with the help of sunlight, restoring HP.";
	move.role = "producer";

	move.pp = 10;
	move.type = type.Recovery;

end;

function callback(args) 	
	
	if(args.user.status == "shaded") then
		args.text = "but couldn't get any sun";
	else
		args.recoverPercent(0.2);
	end;

end;