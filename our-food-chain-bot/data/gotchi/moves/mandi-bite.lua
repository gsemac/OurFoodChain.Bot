function OnRegister(move)

	move.SetName("Mandi-Bite")
	move.SetDescription("Bites the opponent with crushing mandibles. Has a chance to damage the opponent's carapace, descreasing their DEF.")
	move.SetType("predator")

	move.SetPP(15)

	move.Requires.DescriptionMatch("mandible")

end

function OnMove(args) 

	args.DealDamage()

	if(chance(5)) then
		args.Target.Stats.Def = args.Target.Stats.Def * 0.9;
	end

end