function register(move)

	move.name = "Venom Bite";
	move.description = "Bites the opponent, injecting venom into the wound. Has a small chance of poisoning the target.";

	move.role = "predator";
	move.pp = 15;

end;

function callback(args) 

	if(rand(0, 10) == 0) then
		args.target.status = "poisoned";
	end;

	args.applyDamage();

end;