function register(move)

	move.name = "scavenge";
	move.description = "Scavenges for something to eat, restoring a random amount of HP.";
	move.role = "scavenger";

	move.pp = 15;
	move.type = type.Recovery;

end;

function callback(args) 
	
	amount = rand(0, 6);

	args.user.hp = args.user.hp + (args.user.max_hp * (amount / 10));

	if(amount == 0) then 
		args.text = "but couldn't find anything";
	end;

end;