function OnRegister(move)

	move.SetName("spark")
	move.SetDescription("Shocks the opponent with electricity. This move does little damage, but always strikes first.")

	move.SetPower(25)
	move.SetPP(10)
	move.SetPriority(2)

	move.Requires.MinimumLevel(10).DescriptionMatch("electric|static")

end