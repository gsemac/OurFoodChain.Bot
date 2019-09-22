function OnRegister(move)

	move.SetName("filter")
	move.SetDescription("Filters through detritus for food, restoring a moderate amount of HP.")
	move.SetType("detritivore")

	move.SetPP(20)
	
	move.Requires.TypeMatch("detritivore")
	move.Requires.MinimumLevel(10)

end

function OnMove(args) 	
	
	args.RecoverPercent(0.2)

end