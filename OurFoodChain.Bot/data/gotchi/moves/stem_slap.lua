function OnRegister(move)

	move.SetName("stem slap")
	move.SetDescription("Slaps the opponent with a stem. Does more damage the faster the user is.")
	move.SetType("producer")

	move.SetPP(20)

	move.Requires.TypeMatch("producer").DescriptionMatch("\\b(?:stems?)\\b")

end

function OnMove(args) 	
	
	args.DealDamage(args.User.Stats.Spd)

end