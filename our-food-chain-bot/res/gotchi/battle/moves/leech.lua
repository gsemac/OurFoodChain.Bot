function register(move)

	move.name = "Leech";
	move.description = "Leeches some hit points from the opponent, healing the user.";

	move.pp = 25;
	move.type = type.Offensive;

	move.requires.role = "parasite";
	move.requires.minLevel = 10;
	move.requires.unrestrictedMatch = "leech|suck|sap";

end

function callback(args) 		

	args.DoDamage();

	args.user.hp = args.user.hp + (args.TotalDamage() / 2);
	args.text = "sapping {damage} hit points";

end