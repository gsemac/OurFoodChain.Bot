function OnRegister(move)

	move.SetName("vine strangle")
	move.SetDescription("Wraps numerous vines around the opponent, dealing more damage the higher the user's HP is compared to the opponent.")

	move.SetPower(100)
	move.SetPP(5)

	move.Requires.TypeMatch("producer").MinimumLevel(30).DescriptionMatch("vine")

end

function callback(args) 
	
	base_damage = args.Power * (args.User.Stats.Hp / args.Target.Stats.Hp)

	args.DealDamage(base_damage)

end