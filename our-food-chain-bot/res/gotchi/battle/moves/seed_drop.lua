function register(move)

	move.name = "seed drop";
	move.description = "Drops 1-5 seeds onto the opponent, dealing minor damage repeatedly.";
	move.role = "producer";

	move.pp = 25;
	move.type = type.Offensive;
	move.multiplier = 1 / 5;
	move.hit_rate = 0.9;

end

function init(args) 
	args.times = rand(1, 6);
end

function callback(args) 	
	args.applyDamage();
end