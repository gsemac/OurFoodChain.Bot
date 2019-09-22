function OnRegister(move)

	move.SetName("crush")
	move.SetDescription("Crushes the opponent with force, dealing damage and decreasing their defense. Has a high critical hit ratio.")
	move.SetType("predator")

	move.SetPower(80)
	move.SetPP(5)
	move.SetCriticalRate(2.0)

	move.Requires.DescriptionMatch("\\b(?:crush(?:ing|es)?)\\b")
	move.Requires.MinimumLevel(10)

end