function register(move)

	move.name = "flagella whip";
	move.description = "Whips the opponent with flagella, dealing consistent base damage.";

	move.pp = 20;
	move.type = type.Offensive;

	move.requires.match = "flagella|flagellum";

end

function callback(args) 
	args.DoDamage(3);
end