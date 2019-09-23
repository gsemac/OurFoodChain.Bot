function OnRegister(move)

	move.SetName("Withdraw")
	move.SetDescription("Boosts defense by a small amount.")

	move.SetPP(10)
	
	move.Requires.DescriptionMatch("shell|carapace")

end

function OnMove(args) 		

	args.User.Stats.Def = args.User.Stats.Def * 1.2

end