function OnRegister(move)

	move.SetName("Wild Attack")
	move.SetDescription("Viciously and blindly attacks the opponent. Has low accuracy, but high critical hit rate.")
	move.SetType("predator")

	move.SetPower(65)
	move.SetPP(20)
	move.SetAccuracy(0.5)
	move.SetCriticalRate(2.0)
	
	move.Requires.TypeMatch("predator").MinimumLevel(10)

end