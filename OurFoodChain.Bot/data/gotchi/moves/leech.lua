function OnRegister(move)

	move.SetName("Leech")
	move.SetDescription("Leeches some hit points from the opponent, healing the user.")
	move.SetType("parasite")

	move.SetPP(25)
	move.SetPower(20)

	move.Requires.TypeMatch("parasite").MinimumLevel(10).Or.DescriptionMatch("leech|suck|sap")

end

function OnMove(args) 		

	args.DealDamage()

	args.User.Stats.Hp = args.User.Stats.Hp + (args.CalculateDamage() / 2)

	args.SetText("sapping {damage} hit points")

end