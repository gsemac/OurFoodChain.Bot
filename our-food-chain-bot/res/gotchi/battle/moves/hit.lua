function register(move)
	move.name = "Hit";
	move.description = "A simple attack where the user collides with the opponent.";
	move.pp = 99;
end;

function callback(args) 
	args.applyDamage();
end;