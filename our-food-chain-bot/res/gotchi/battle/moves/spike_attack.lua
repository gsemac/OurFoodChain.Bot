function register(move)

	move.name = "Spike Attack";
	move.description = "Attacks the opponent with a spike. Effective against flying opponents.";

	move.pp = 25;
	move.type = type.Offensive;

	move.requires.match = "spike";

end;

function callback(args) 

	if(args.targetDescriptionMatches("fly|flies")) then
		args.applyDamage(1.2);
	else
		args.applyDamage();
	end;

end;