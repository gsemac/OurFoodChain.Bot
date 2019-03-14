function register(move)

	move.name = "grow";
	move.description = "Grows larger and raises stats by a small amount.";
	move.role = "producer";

	move.pp = 10;

	move.requires.role = "producer";

end;

function callback(args) 	
	
	if(args.user.status == "shaded") then
		args.text = "but couldn't get any sun";
	else
		args.user.boostAll(1.05);
		args.text = "boosting their stats by 5%";
	end;

end;