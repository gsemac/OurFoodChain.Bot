function register(move)

	move.name = "skill steal";
	move.description = "Swaps a random stat with the opponent.";

	move.pp = 15;
	move.type = type.Offensive;

	move.requires.role = "parasite";
	move.requires.minLevel = 20;

end

function callback(args) 	
	
	r = rand(0, 3);

	if(r == 0) then
		swap(args.user.atk, args.target.atk);
	elseif(r == 1) then
		swap(args.user.def, args.target.def);
	elseif(r == 2) then
		swap(args.user.spd, args.target.spd);
	end

	args.text = "swapping a stat with the opponent";

end