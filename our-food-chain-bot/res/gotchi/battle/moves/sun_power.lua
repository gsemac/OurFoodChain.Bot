function register(move)

	move.name = "Sun Power";
	move.description = "Grows and boosts stats with the help of sunlight.";
	move.role = "producer";

	move.pp = 10;

end;

function callback(args) 	
	
	if(args.user.status == "shaded") then
		args.text = "but couldn't get any sun";
	else
		args.user.boostAll(1.20);
		args.text = "boosting their stats by 20%";
	end;

end;