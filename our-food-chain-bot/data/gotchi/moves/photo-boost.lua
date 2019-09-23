function OnRegister(move)

	move.SetName("Photo-Boost")
	move.SetDescription("Regenerates with the help of sunlight, restoring a moderate amount of HP.")
	move.SetType("producer")

	move.SetPP(5)

	move.Requires.TypeMatch("producer")
	move.Requires.MinimumLevel(30)

end

function OnMove(args) 	
	
	if(args.User.StatusName == "shaded") then
		args.SetText("but couldn't get any sun")
	else
		args.RecoverPercent(15)
	end

end