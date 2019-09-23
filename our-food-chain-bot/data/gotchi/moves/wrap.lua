function OnRegister(move)

	move.SetName("Wrap")
	move.SetDescription("Tightly wraps tentacles around the opponent. Deals more damage the faster the opponent is compared to the user.")
	
	move.SetPower(55)
	move.SetPP(5)
	
	move.Requires.DescriptionMatch("tentacle")

end

function OnMove(args) 
	
	multiplier = Min(2.0, args.Target.Stats.Spd / args.User.Stats.Spd)
	base_damage = args.Power * multiplier

	args.DealDamage(base_damage)

end