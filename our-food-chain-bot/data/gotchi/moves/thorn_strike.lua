function register(move)

	move.name = "thorn strike";
	move.description = "Strikes the opponent with thorns, dealing a moderate amount of random base damage.";

	move.pp = 10;
	move.type = type.Offensive;

	move.requires.minLevel = 15;
	move.requires.role = "producer";
	move.requires.match = "\\b(?:thorn[sy]?|spike[sy]?|spiky|needles?)\\b";

end

function callback(args) 	
	args.DoDamage(rand(10, 50));
end