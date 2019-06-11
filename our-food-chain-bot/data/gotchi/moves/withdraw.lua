function register(move)

	move.name = "Withdraw";
	move.description = "Boosts defense by a small amount.";

	move.pp = 10;
	move.type = type.Buff;

	move.requires.match = "shell|carapace";

end

function callback(args) 		
	args.user.stats.def = args.user.stats.def * 1.2;
end