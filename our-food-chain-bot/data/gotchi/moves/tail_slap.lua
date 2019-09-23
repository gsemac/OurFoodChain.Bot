function OnRegister(move)

	move.SetName("Tail Slap")
	move.SetDescription("Deals more damage the faster the user is compared to the opponent.")
	
	move.SetPP(5)

	move.Requires.DescriptionMatch("tail")

end

function OnMove(args) 
	
	multiplier = Min(2.0, args.User.Stats.Spd / args.Target.Stats.Spd)
	base_damage = args.CalculateDamage() * multiplier

	args.DealDamage(base_damage)

end