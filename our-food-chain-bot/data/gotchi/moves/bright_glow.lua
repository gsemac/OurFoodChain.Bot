function register(move)

	move.name = "Bright Glow";
	move.description = "Glows brightly, reducing the opponent's accuracy.";

	move.pp = 10;

	move.requires.match = "glow|\blight\b|bioluminescen(?:t|ce)";

end

function callback(args) 	
	
	amount = 0.05;
	args.target.stats.accuracy = args.target.stats.accuracy * (1.0 - amount);

end