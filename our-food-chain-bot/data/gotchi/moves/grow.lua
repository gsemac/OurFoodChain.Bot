function OnRegister(move)

	move.SetName("grow")
	move.SetDescription("Grows larger and raises stats by a small amount.")
	move.SetType("producer")

	move.SetPP(10)
	move.SetIgnoreAccuracy(true)

	move.Requires.TypeMatch("producer")

end

function OnMove(args) 	
	
	if(args.User.StatusName == "shaded") then
		args.SetText("but couldn't get any sun")
	else
		args.User.Stats.BuffPercent(5)
		args.SetText("boosting their stats by 5%")
	end

end