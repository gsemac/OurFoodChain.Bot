function register(move)
	move.name = "Bite";
	move.description = "Attacks the opponent with mouthparts. Effective against Consumers, but ineffective against Producers.";
	move.role = "predator";
	move.pp = 40;
end;

function callback(args) 
	args.applyDamage();
end;