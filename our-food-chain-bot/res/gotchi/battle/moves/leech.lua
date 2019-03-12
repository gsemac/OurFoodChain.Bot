function register(move)

	move.name = "Leech";
	move.description = "Leeches some hit points from the opponent, healing the user.";

	move.pp = 25;
	move.type = type.Offensive;

end;

function callback(args) 		

	args.applyDamage();

	args.user.hp = args.user.hp + (args.calculateDamage() / 2);
	args.text = "sapping {damage} hit points";

end;