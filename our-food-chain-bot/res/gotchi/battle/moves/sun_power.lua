function register(move)

	move.name = "Sun Power";
	move.description = "Grows and boosts stats with the help of sunlight.";
	move.role = "producer";

	move.pp = 5;

	move.Type = type.Buff;
	move.requires.role = "producer";
	move.requires.minLevel = 30;

end

function callback(args) 	
	
	if(args.user.status == "shaded") then
		args.text = "but couldn't get any sun";
	else
		args.user.MultiplyAll(1.10);
		args.text = "boosting their stats by 10%";
	end;

end