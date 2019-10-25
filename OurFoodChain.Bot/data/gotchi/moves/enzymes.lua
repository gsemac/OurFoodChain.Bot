function OnRegister(move)

	move.SetName("enzymes")
	move.SetDescription("Attacks by coating the opponent with enzymes encouraging decomposition. This move is highly effective against Producers.")
	move.SetType("decomposer")

	move.SetPower(25)
	move.SetPP(40)

	move.Requires.TypeMatch("decomposer")

end