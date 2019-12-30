function OnRegister(move)

	move.SetName("Mandi-Bite")
	move.SetDescription("Bites the opponent with crushing mandibles. Has a chance to damage the opponent's carapace, descreasing their DEF.")
	move.SetType("predator")

	move.SetPower(60)
	move.SetPP(15)

	move.Requires.DescriptionMatch("mandible")

end

function OnMove(args) 

	args.DealDamage()

	if(Chance(5)) then
		args.Target.Stats.DebuffPercent("def", 10)
	end

end