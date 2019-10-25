function OnRegister(move)

	move.SetName("Fin Slash")
	move.SetDescription("Attacks the opponent with the edges of its fins. The faster the user is, the higher the damage.")

	move.SetPP(20)

	move.Requires.DescriptionMatch("\\b(?:fins?)\\b")

end

function OnMove(args) 

	args.DealDamage(args.User.Stats.Spd)

end