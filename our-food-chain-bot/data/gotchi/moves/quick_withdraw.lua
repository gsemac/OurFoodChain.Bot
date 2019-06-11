function register(move)

	move.name = "Quick Withdraw";
	move.description = "The user quickly withdraws into its shell, allowing it to survive the next hit.";

	move.pp = 5;
	move.type = type.Buff;
	move.priority = 2;
	move.canMiss = false;

	move.requires.match = "shell|carapace";
	move.requires.minLevel = 20;

end

function callback(args) 		
	
	args.text = "quickly withdrawing into its shell";
	args.user.status = "withdrawn";

end