function register(move)

	move.name = "Sap Seep";
	move.description = "Seeps sticky sap onto the opponent, decreasing their speed by 30%.";

	move.pp = 5;
	move.type = type.Buff;

	move.requires.role = "producer";
	move.requires.match = "\\b(?:sap|sticky)\\b";

end

function callback(args) 		
	args.target.stats.spd = args.target.stats.spd * 0.7;
end