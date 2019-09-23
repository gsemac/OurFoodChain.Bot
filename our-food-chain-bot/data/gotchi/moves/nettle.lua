function OnRegister(move)

	move.SetName("nettle")
	move.SetDescription("Attacks the opponent with irritating stingers, decreasing their speed. Does low damage, but never misses.")

	move.SetPP(40)
	move.SetIgnoreAccuracy(true)
	move.SetPower(30)

	move.Requires.DescriptionMatch("\\b(sting)")
	move.Requires.MinimumLevel(10)

end

function OnMove(args) 

	args.Target.Stats.Spd = args.Target.Stats.Spd * 0.8

	args.DealDamage()

end