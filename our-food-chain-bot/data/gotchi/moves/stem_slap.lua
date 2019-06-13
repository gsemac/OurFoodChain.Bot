function register(move)

	move.name = "stem slap";
	move.description = "Slaps the opponent with a stem. Does more damage the faster the user is.";

	move.pp = 20;
	move.type = type.Offensive;

	move.requires.role = "producer";
	move.requires.match = "\\b(?:stems?)\\b";

end

function callback(args) 	
	args.DoDamage(args.user.stats.spd);
end