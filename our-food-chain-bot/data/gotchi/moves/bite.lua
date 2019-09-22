function OnRegister(move)

	move.SetName("Bite")
	move.SetDescription("Attacks the opponent with mouthparts. Effective against Consumers, but ineffective against Producers.")
	move.SetType("predator")

	move.SetPower(60)
	move.SetPP(40)

	move.Requires.TypeMatch("predator").Or.DescriptionMatch("teeth|jaws|bite")

end