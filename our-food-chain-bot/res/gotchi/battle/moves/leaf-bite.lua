function register(move)

	move.name = "leaf-bite";
	move.description = "Attacks the opponent with mouthparts. Effective against Producers.";
	move.role = "base-consumer";

	move.pp = 40;

end;

function callback(args) 	
	args.applyDamage();
end;