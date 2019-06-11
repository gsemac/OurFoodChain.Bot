function register(move)

	move.name = "Hit";
	move.description = "A simple attack where the user collides with the opponent.";
	move.pp = 40;

end

function callback(args) 
	args.DoDamage();
end