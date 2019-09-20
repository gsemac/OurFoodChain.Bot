function register(move)

	move.SetName("Bite")
	move.SetDescription("Attacks the opponent with mouthparts. Effective against Consumers, but ineffective against Producers.")
	move.SetType("carnivore")

	move.SetPower(60)
	move.SetPP(40)

	move.Requires.TypeMatch("carnivore").Or.DescriptionMatch("teeth|jaws|bite")

end