function OnRegister(move)

	move.SetName("flagella whip")
	move.SetDescription("Whips the opponent with flagella, dealing consistent base damage.")

	move.SetPower(10)
	move.SetPP(20)

	move.Requires.DescriptionMatch("flagella|flagellum")

end