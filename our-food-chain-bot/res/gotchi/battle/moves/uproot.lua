function register(move)

	move.name = "Uproot";
	move.description = "Uproots the opponent, eliminating their ability to use recovery moves. Only works on Producers.";
	move.role = "base-consumer";

	move.pp = 5;
	move.type = type.Offensive;
	move.multiplier = 0.5;

end;

function callback(args) 
	
	if(args.targetHasRole("producer")) then
	
		args.target.status = "heal block";
		args.applyDamage();

	else

		args.text = "but it failed";

	end;

end;