function OnRegister(move)

	move.SetName("leaf-bite")
	move.SetDescription("Attacks the opponent with mouthparts. Effective against Producers.")
	move.SetType("base-consumer")

	move.SetPower(60)
	move.SetPP(40)

	move.Requires.TypeMatch("base-consumer")

end