function register(move)

	move.name = "helpless facade";
	move.description = "The user appears helpless and harmless, causing the opponent to let their guard down, lowering their DEF.";

	move.pp = 5;
	move.canMiss = false;
	move.requires.match = "blind";

end

function callback(args) 

	args.target.stats.def = args.target.stats.def * 0.8;

end