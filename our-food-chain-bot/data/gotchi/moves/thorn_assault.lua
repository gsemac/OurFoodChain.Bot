function register(move)

	move.name = "thorn assault";
	move.description = "Assaults the opponent with thorns, dealing a large amount of random base damage.";

	move.pp = 5;
	move.type = type.Offensive;

	move.requires.minLevel = 30;
	move.requires.role = "producer";
	move.requires.match = "\\b(?:thorn[sy]?|spike[sy]?|spiky|needles?)\\b";

end

function callback(args) 	
	args.DoDamage(rand(50, 100));
end