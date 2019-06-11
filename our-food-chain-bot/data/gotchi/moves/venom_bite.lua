function register(move)

	move.name = "Venom Bite";
	move.description = "Bites the opponent, injecting venom into the wound. Has a small chance of poisoning the target.";

	move.role = "predator";
	move.pp = 15;

	move.requires.role = "predator";
	move.requires.match = "poison|venom";

end

function callback(args) 

	if(chance(10)) then
		args.target.status = "poisoned";
	end;

	args.DoDamage();

end