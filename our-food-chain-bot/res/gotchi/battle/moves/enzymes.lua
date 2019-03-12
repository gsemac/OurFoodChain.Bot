function register(move)

	move.name = "enzymes";
	move.description = "Attacks by coating the opponent with enzymes encouraging decomposition. This move is highly effective against Producers.";
	move.role = "decomposer";

	move.pp = 40;

end;

function callback(args) 
	args.applyDamage();
end;