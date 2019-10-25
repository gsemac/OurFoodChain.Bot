function OnRegister(move)

	move.SetName("slam")
	move.SetDescription("Strikes the opponent with a heavy appendage. This move deals a lot of damage, but always strikes last.")
	
	move.SetPP(5)
	move.SetPower(80)
	move.SetPriority(-1)

	move.Requires.MinimumLevel(30).DescriptionMatch("(?:heavy|thick)(?:tail|appendage)")

end