function register(move)

	move.name = "overgrowth";
	move.description = "Accelerates growth, boosting attack by a moderate amount.";
	move.role = "producer";

	move.pp = 25;
	move.type = type.Buff;

end;

function callback(args) 	
	args.user.atk = args.user.atk * 1.2;
end;