function OnRegister(move)

	move.SetName("leaf-blade")
	move.SetDescription("Slashes the opponent with sharp leaves. High critical rate, but ineffective against Producers.")
	move.SetType("base-consumer")

	move.SetPower(70)
	move.SetCriticalRate(1.2)
	move.SetPP(15)

	move.SetMatchup("producer", 0.5)

	move.Requires.TypeMatch("producer")
	move.Requires.MinimumLevel(20)
	move.Requires.DescriptionMatch("leaf|leaves")

end