﻿function register(move)

	move.name = "Fin Slash";
	move.description = "Attacks the opponent with the edges of its fins. The faster the user is, the higher the damage.";

	move.pp = 20;
	move.requires.match = "\b(fins?)\b";

end

function callback(args) 

	damage = args.calculateDamage();

	args.target.hp = args.target.hp - damage;

end