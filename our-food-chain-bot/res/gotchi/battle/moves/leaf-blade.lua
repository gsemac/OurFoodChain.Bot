function register(move)

	move.name = "leaf-blade";
	move.description = "Slashes the opponent with sharp leaves. High critical rate, but ineffective against Producers.";
	move.role = "base-consumer";

	move.critical_rate = 1.2;
	move.pp = 15;

	move.requires.role = "producer";
	move.requires.min_level = 20;
	move.requires.match = "leaf|leaves";

end;

function callback(args) 	

	if(args.targetHasRole("producer")) then
		args.applyDamage(0.5);
	else
		args.applyDamage();
	end;

end;