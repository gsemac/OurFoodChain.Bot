function register(move)

	move.name = "leaf-blade";
	move.description = "Slashes the opponent with sharp leaves. High critical rate, but ineffective against Producers.";
	move.role = "base-consumer";

	move.criticalRate = 1.2;
	move.pp = 15;

	move.requires.role = "producer";
	move.requires.minLevel = 20;
	move.requires.match = "leaf|leaves";

end

function callback(args) 	

	if(args.TargetHasRole("producer")) then
		args.DoDamage(args.BaseDamage(), 0.5);
	else
		args.DoDamage();
	end;

end