function OnRegister(move)

	move.SetName("Quick Withdraw")
	move.SetDescription("The user quickly withdraws into its shell, allowing it to survive the next hit.")

	move.SetPP(5)
	move.SetPriority(2)
	move.SetIgnoreAccuracy(true)

	move.Requires.DescriptionMatch("shell|carapace")
	move.Requires.MinimumLevel(20)

end

function OnMove(args) 		
	
	args.SetText("quickly withdrawing into its shell")

	args.User.SetStatus("withdrawn")

end