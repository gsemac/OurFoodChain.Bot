function register(move)

	move.name = "Mandi-Bite";
	move.description = "Bites the opponent with crushing mandibles. Has a chance to damage the opponent's carapace, descreasing their DEF.";
	move.role = "predator";

	move.pp = 15;

	move.requires.match = "mandible";

end

function callback(args) 

	args.applyDamage();

	if(rand(0, 6) == 0) then
		args.target.def = args.target.def * 0.9;
	end

end