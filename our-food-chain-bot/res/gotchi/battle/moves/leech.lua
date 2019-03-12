function register(move)

	move.name = "Leech";
	move.description = "Leeches some hit points from the opponent, healing the user.";

	move.pp = 25;
	move.type = type.Offensive;

	move.requires.role = "parasite";
	move.requires.min_level = 10;
	move.requires.unrestricted_match = "leech|suck|sap";

end;

function callback(args) 		

	args.applyDamage();

	args.user.hp = args.user.hp + (args.calculateDamage() / 2);
	args.text = "sapping {damage} hit points";

end;