function OnRegister(move)

	move.SetName("zap")
	move.SetDescription("Shocks the opponent with electricity. Highly effective against aquatic organisms.")

	move.SetPower(50)
	move.SetPP(20)

	move.SetMatchup("aquatic", 1.2)

	move.Requires.DescriptionMatch("electric|static")

end