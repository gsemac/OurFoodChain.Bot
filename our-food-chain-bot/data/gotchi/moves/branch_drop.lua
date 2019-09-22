function OnRegister(move)

	move.SetName("branch drop")
	move.SetDescription("Drops a heavy branch on the target, dealing damage to the opponent. However, the user also takes damage.")
	move.SetType("producer")

	move.SetPower(75)
	move.SetPP(10)
	move.SetCriticalRate(1.5)

	move.Requires.TypeMatch("producer")
	move.Requires.MinimumLevel(5)
	move.Requires.DescriptionMatch("tree|branch")

end

function OnMove(args) 

	args.User.Stats.Hp = args.User.Stats.Hp - 10

	args.DealDamage()

end