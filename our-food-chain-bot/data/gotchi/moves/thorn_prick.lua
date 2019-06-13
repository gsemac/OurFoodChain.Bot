function register(move)

	move.name = "thorn prick";
	move.description = "Pricks the opponent with thorns, dealing a minor amount of random base damage.";

	move.pp = 20;
	move.type = type.Offensive;

	move.requires.role = "producer";
	move.requires.match = "\\b(?:thorn[sy]?|spike[sy]?|spiky|needles?)\\b";

end

function callback(args) 
	args.DoDamage(rand(1, 5));
end