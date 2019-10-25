function OnRegister(move)

	move.SetName("Sting")
	move.SetDescription("Attacks the opponent with stinger(s). Does low damage, but never misses.")

	move.SetPower(30)
	move.SetPP(40)
	move.SetIgnoreAccuracy(true)

	move.Requires.DescriptionMatch("\\b(sting)").MaximumLevel(10)

end