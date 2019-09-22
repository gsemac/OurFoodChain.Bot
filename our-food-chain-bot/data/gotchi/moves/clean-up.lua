function OnRegister(move)

	move.SetName("clean-up")
	move.SetDescription("Nibbles on detritus, restoring a small amount of HP.")
	move.SetType("detritivore")

	move.SetPP(20)

	move.Requires.TypeMatch("detritivore")
	move.Requires.SetMaximumLevel(10)

end

function OnMove(args) 	
	
	args.RecoverPercent(0.1)

end