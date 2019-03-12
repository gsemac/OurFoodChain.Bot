function register(move)

	move.name = "infest";
	move.description = "Attack by parasitizing the opponent. This move is highly effective against Consumers.";
	move.role = "parasite";

	move.pp = 40;

end;

function callback(args) 	
	args.applyDamage();
end;