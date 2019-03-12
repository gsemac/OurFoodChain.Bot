function register(move)

	move.name = "overgrowth";
	move.description = "Accelerates growth, boosting attack by a moderate amount.";
	move.role = "producer";

	move.pp = 25;
	move.type = type.Buff;

	move.requires.role = "producer";
	move.requires.min_level = 30;

end;

function callback(args) 	
	args.user.atk = args.user.atk * 1.2;
end;