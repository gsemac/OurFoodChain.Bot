function register(move)
  move.name = "listen"
  move.description = "Takes a moment to listen for the opponent, boosting the user's accuracy."
  
  move.pp = 5
  move.requires.match = "\\b(hear|listen)|\\b(ear)\\b"
end

function callback(args) 
	args.user.stats.accuracy = args.user.stats.accuracy * 2
end
