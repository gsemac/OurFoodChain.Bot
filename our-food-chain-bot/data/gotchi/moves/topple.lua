function OnRegister(move)

	move.SetName("topple")
	move.SetDescription("Collapses onto the opponent, dealing heavy damage. However, the user is reduced to 1 HP.")

	move.SetPower(130)
	move.SetPP(1)
	move.SetCriticalRate(2.0)
	move.SetAccuracy(0.5)

	move.Requires.TypeMatch("producer").MinimumLevel(10).DescriptionMatch("tree|tall|heavy")

end

function OnMove(args) 

	args.User.Stats.Hp = 1

	args.DealDamage()

end