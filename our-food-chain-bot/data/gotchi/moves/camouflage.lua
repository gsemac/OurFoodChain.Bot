function OnRegister(move)

	move.SetName("camouflage")
	move.SetDescription("Uses camouflage to hide from the opponent, boosting evasion by a small amount.")

	move.SetPP(5)
	
	move.Requires.DescriptionMatch("camouflage")

end

function OnMove(args) 

	args.User.Stats.BuffPercent("eva", 10)

end