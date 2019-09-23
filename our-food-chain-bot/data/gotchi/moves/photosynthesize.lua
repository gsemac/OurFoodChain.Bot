function OnRegister(move)

	move.SetName("Photosynthesize")
	move.SetDescription("Regenerates with the help of sunlight, restoring HP.")
	move.SetType("producer")

	move.SetPP(5)
	move.SetIgnoreAccuracy(true)

	move.Requires.TypeMatch("producer")

end

function OnMove(args) 	
	
	if(args.User.StatusName == "shaded") then
		args.SetText("but couldn't get any sun")
	else
		args.RecoverPercent(0.1)
	end

end