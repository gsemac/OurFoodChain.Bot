function register(move)

	move.name = "cast shade";
	move.description = "Casts shade over the opponent, preventing them from using Grow or Photosynthesis.";
	move.role = "producer";

	move.pp = 25;
	move.type = type.Offensive;

end;

function callback(args) 	
	
	args.text = "casting shade on the opponent";
	args.target.status = "shaded";

end;