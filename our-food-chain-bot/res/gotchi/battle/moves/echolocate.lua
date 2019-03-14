function register(move)

	move.name = "echolocate";
	move.description = "Scans the environment using echolocation. Greatly boosts the user's accuracy.";

	move.pp = 1;
	move.requires.match = "\b(echolcation|echolocate)\b";

end

function callback(args) 
	args.user.accuracy = args.user.accuracy * 10;
end