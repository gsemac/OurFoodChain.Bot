function OnRegister(move)

	move.SetName("Withdraw")
	move.SetDescription("Boosts defense by a small amount.")

	move.SetPP(10)
	move.SetIgnoreAccuracy(true)
	
	move.Requires.DescriptionMatch("shell|carapace")

end

function OnMove(args) 		

	args.User.Stats.BuffPercent("def", 20)

end