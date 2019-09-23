function OnRegister(move)

	move.SetName("Spike Attack")
	move.SetDescription("Attacks the opponent with a spike. Effective against flying opponents.")

	move.SetPower(40)
	move.SetPP(25)

	move.Requires.DescriptionMatch("spike")

end

function OnMove(args) 

	damage = args.CalculateDamage()
	
	requirements = NewRequirements()
	requirements.DescriptionMatch("fly|flies")

	if(args.Target.TestRequirements(requirements)) then
		damage = damage * 1.2
	end

	args.DealDamage(damage)

end