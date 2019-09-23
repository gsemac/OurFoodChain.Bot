function OnRegister(move)

	move.SetName("infest")
	move.SetDescription("Attack by parasitizing the opponent. This move is highly effective against Consumers.")
	move.SetType("parasite")

	move.SetPower(45)
	move.SetPP(40)

	move.Requires.TypeMatch("parasite")

end